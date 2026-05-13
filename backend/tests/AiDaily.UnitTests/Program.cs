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
