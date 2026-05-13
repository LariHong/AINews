using AiDaily.Application.Articles;
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
