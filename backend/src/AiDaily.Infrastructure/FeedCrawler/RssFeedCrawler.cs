using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;
using AiDaily.Application.Articles;
using AiDaily.Domain.Entities;

namespace AiDaily.Infrastructure.FeedCrawler;

public sealed class RssFeedCrawler
{
    private readonly HttpClient _httpClient;
    private readonly IArticleRepository _articles;

    public RssFeedCrawler(HttpClient httpClient, IArticleRepository articles)
    {
        _httpClient = httpClient;
        _articles = articles;
    }

    public async Task<FeedCrawlResult> CrawlAsync(
        IEnumerable<FeedSource> sources,
        CancellationToken cancellationToken = default)
    {
        var logs = new List<string>();
        var sourcesVisited = 0;
        var articlesPersisted = 0;

        foreach (var source in sources.Where(item => item.IsEnabled))
        {
            sourcesVisited++;

            try
            {
                using var stream = await _httpClient.GetStreamAsync(source.FeedUrl, cancellationToken);
                var document = await XDocument.LoadAsync(stream, LoadOptions.None, cancellationToken);
                var items = document.Descendants("item").Take(10).ToList();

                foreach (var item in items)
                {
                    var title = ReadValue(item, "title");
                    var link = ReadValue(item, "link");
                    if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(link))
                    {
                        continue;
                    }

                    await _articles.UpsertAsync(new Article
                    {
                        Id = CreateStableId(link),
                        Title = title,
                        Summary = ReadValue(item, "description"),
                        SourceUrl = link,
                        SourceId = source.Id,
                        SourceName = source.Name,
                        Tags = ["rss"],
                        PublishedAt = ParsePublishedAt(ReadValue(item, "pubDate")),
                        HasAiSummary = false,
                        IsBookmarked = false,
                        ReadTimeMinutes = null
                    }, cancellationToken);

                    articlesPersisted++;
                }

                source.LastCrawledAt = DateTimeOffset.UtcNow;
                logs.Add($"Crawled {source.Name}: {items.Count} RSS items read.");
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or System.Xml.XmlException)
            {
                logs.Add($"Crawler skipped {source.Name}: {ex.Message}");
            }
        }

        return new FeedCrawlResult(sourcesVisited, articlesPersisted, logs);
    }

    private static string? ReadValue(XElement item, string name) =>
        item.Elements().FirstOrDefault(element => element.Name.LocalName == name)?.Value.Trim();

    private static DateTimeOffset ParsePublishedAt(string? value) =>
        DateTimeOffset.TryParse(value, out var publishedAt) ? publishedAt : DateTimeOffset.UtcNow;

    private static string CreateStableId(string sourceUrl)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(sourceUrl));
        return $"rss_{Convert.ToHexString(bytes)[..12].ToLowerInvariant()}";
    }
}
