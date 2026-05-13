using AiDaily.Application.AiSummaries;
using AiDaily.Application.Articles;
using AiDaily.Domain.Entities;
using AiDaily.Infrastructure.AI;
using AiDaily.Infrastructure.Cache;
using AiDaily.Infrastructure.FeedCrawler;
using AiDaily.Infrastructure.Repositories;
using System.Net;

var repository = new InMemoryArticleRepository();
var service = new ArticleQueryService(repository);

await ShouldFilterByKeyword();
await ShouldFilterByTagAndPaginate();
await ShouldGetArticleById();
await ShouldReturnNullForMissingArticle();
await ShouldCrawlRssIntoArticles();
await ShouldGetAiSummaryPreview();
await ShouldReturnSummaryNotFound();
await ShouldCacheAiSummaryPreview();
await ShouldGenerateAndReadAiReport();
await ShouldRejectInvalidAiReportDraft();
await ShouldNormalizeSparseAiReportDraft();

Console.WriteLine("AiDaily.UnitTests passed");

async Task ShouldFilterByKeyword()
{
    var result = await service.GetArticlesAsync(new ArticleListParams(null, 20, "safety", null, null, null));

    Assert(result.TotalCount == 1, "keyword filter should return one safety article");
    Assert(result.Items[0].SourceName == "MIT Tech Review AI", "keyword filter should return MIT article");
}

async Task ShouldFilterByTagAndPaginate()
{
    var firstPage = await service.GetArticlesAsync(new ArticleListParams(null, 1, null, "research", null, null));
    var secondPage = await service.GetArticlesAsync(new ArticleListParams(firstPage.Cursor, 1, null, "research", null, null));

    Assert(firstPage.Items.Count == 1, "first page should contain one item");
    Assert(firstPage.HasMore, "first page should report more items");
    Assert(secondPage.Items.Count == 1, "second page should contain one item");
    Assert(firstPage.Items[0].Id != secondPage.Items[0].Id, "cursor should advance to a different article");
}

async Task ShouldGetArticleById()
{
    var article = await service.GetArticleAsync("art_01JAI001");

    Assert(article is not null, "detail query should return an article");
    Assert(article!.SourceUrl == "https://openai.com/news/", "detail query should return the requested article");
}

async Task ShouldReturnNullForMissingArticle()
{
    var article = await service.GetArticleAsync("missing");

    Assert(article is null, "detail query should return null for missing article");
}

async Task ShouldCrawlRssIntoArticles()
{
    const string rss = """
        <rss version="2.0">
          <channel>
            <item>
              <title>Example AI feed story</title>
              <link>https://example.com/ai-feed-story</link>
              <description>Imported from RSS.</description>
              <pubDate>Tue, 12 May 2026 06:30:00 GMT</pubDate>
            </item>
          </channel>
        </rss>
        """;

    using var httpClient = new HttpClient(new StaticRssHandler(rss));
    var crawler = new RssFeedCrawler(httpClient, repository);
    var result = await crawler.CrawlAsync([SeedFeedSources.All[0]]);
    var articles = await repository.ListAsync(CancellationToken.None);
    var imported = articles.FirstOrDefault(article => article.SourceUrl == "https://example.com/ai-feed-story");

    Assert(result.SourcesVisited == 1, "crawler should visit one configured source");
    Assert(result.ArticlesPersisted == 1, "crawler should persist one RSS item");
    Assert(imported is not null, "crawler should persist article by stable source URL id");
    Assert(imported!.SourceUrl == "https://example.com/ai-feed-story", "crawler should preserve source_url");
}

async Task ShouldGetAiSummaryPreview()
{
    var summaryService = new AiSummaryQueryService(
        repository,
        new InMemoryAiSummaryRepository(),
        new InMemoryAiSummaryReadCache());

    var result = await summaryService.GetPreviewAsync("art_01JAI001");

    Assert(result.Status == AiSummaryQueryStatus.Found, "summary query should find seeded summary");
    Assert(result.Summary?.Highlights.Count > 0, "summary preview should include highlights");
    Assert(result.Summary?.ImpactScope.Length > 0, "summary preview should include impact scope");
}

async Task ShouldReturnSummaryNotFound()
{
    var summaryService = new AiSummaryQueryService(
        repository,
        new InMemoryAiSummaryRepository(),
        new InMemoryAiSummaryReadCache());

    var result = await summaryService.GetPreviewAsync("art_01JAI002");

    Assert(result.Status == AiSummaryQueryStatus.SummaryNotFound, "article without summary should return summary-not-found status");
}

async Task ShouldCacheAiSummaryPreview()
{
    var summaryRepository = new CountingSummaryRepository();
    var summaryService = new AiSummaryQueryService(
        repository,
        summaryRepository,
        new InMemoryAiSummaryReadCache());

    var first = await summaryService.GetPreviewAsync("art_01JAI001");
    var second = await summaryService.GetPreviewAsync("art_01JAI001");

    Assert(first.Status == AiSummaryQueryStatus.Found, "first summary query should find summary");
    Assert(second.Status == AiSummaryQueryStatus.Found, "second summary query should find cached summary");
    Assert(summaryRepository.ReadCount == 1, "summary repository should only be read once when cache is warm");
}

async Task ShouldGenerateAndReadAiReport()
{
    var reportRepository = new InMemoryAiReportRepository();
    var generationService = new AiReportGenerationService(
        repository,
        reportRepository,
        new StubAiReportGenerator(),
        new InMemoryAiReportGenerationTracker());
    var queryService = new AiReportQueryService(repository, reportRepository);

    var start = await generationService.StartAsync("art_01JAI001", force: false);

    Assert(start.Status == AiReportGenerationStartStatus.Ready, "report generation should start for an existing article");

    await foreach (var streamEvent in start.Stream!)
    {
        Assert(streamEvent.Type != "error", "stub report generation should not emit an error event");
    }

    var result = await queryService.GetReportAsync("art_01JAI001");

    Assert(result.Status == AiReportQueryStatus.Found, "generated report should be readable");
    Assert(result.Report?.KeyPoints.Count > 0, "generated report should include key points");
    Assert(result.Report?.Scores.Impact is >= 0 and <= 100, "generated report scores should be bounded");
}

Task ShouldRejectInvalidAiReportDraft()
{
    var invalid = new AiReportDraft(
        "",
        [],
        [],
        [],
        [],
        new AiReportScoresDto(101, -1, 50),
        [],
        "",
        "surprise");

    Assert(!AiReportValidation.TryValidate(invalid, out _), "invalid report drafts should be rejected");
    return Task.CompletedTask;
}

Task ShouldNormalizeSparseAiReportDraft()
{
    var article = new Article
    {
        Id = "art_sparse",
        Title = "Sparse provider response",
        Summary = "Provider returned too little structure.",
        SourceUrl = "https://example.com/sparse",
        SourceName = "Example",
        Tags = ["model"],
        PublishedAt = DateTimeOffset.Parse("2026-05-13T00:00:00Z")
    };
    var sparse = new AiReportDraft(
        "",
        [],
        [],
        [],
        [],
        new AiReportScoresDto(150, -10, 40),
        [],
        "",
        "unknown");

    var normalized = AiReportDraftNormalizer.Normalize(sparse, article);

    Assert(AiReportValidation.TryValidate(normalized, out _), "sparse provider drafts should normalize into valid reports");
    Assert(normalized.Scores.Impact == 100, "impact score should be clamped");
    Assert(normalized.Scores.Confidence == 0, "confidence score should be clamped");
    Assert(normalized.Rating == "watchlist", "invalid rating should fall back to watchlist");
    return Task.CompletedTask;
}

static void Assert(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}

internal sealed class StaticRssHandler : HttpMessageHandler
{
    private readonly string _rss;

    public StaticRssHandler(string rss)
    {
        _rss = rss;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
        Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(_rss)
        });
}

internal sealed class CountingSummaryRepository : IAiSummaryRepository
{
    public int ReadCount { get; private set; }

    public Task<AiSummary?> GetByArticleIdAsync(string articleId, CancellationToken cancellationToken)
    {
        ReadCount++;

        return Task.FromResult<AiSummary?>(new AiSummary
        {
            Id = "sum_counting",
            ArticleId = articleId,
            Highlights = ["Cached highlight"],
            ImpactScope = "Cache validation",
            Controversy = "None",
            EditorView = "Use cached previews for repeated reads.",
            GeneratedAt = DateTimeOffset.Parse("2026-05-12T08:00:00Z")
        });
    }
}
