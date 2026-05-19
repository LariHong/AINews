using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;
using AiDaily.Application.Articles;
using AiDaily.Application.FeedCrawler;
using AiDaily.Domain.Entities;
using AiDaily.Infrastructure.ContentExtraction;

namespace AiDaily.Infrastructure.FeedCrawler;

public sealed class RssFeedCrawler : IFeedCrawler
{
    private readonly HttpClient _httpClient;
    private readonly IArticleRepository _articles;
    private readonly IArticleContentExtractor? _contentExtractor;

    public RssFeedCrawler(
        HttpClient httpClient,
        IArticleRepository articles,
        IArticleContentExtractor? contentExtractor = null)
    {
        _httpClient = httpClient;
        _articles = articles;
        _contentExtractor = contentExtractor;
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
                var candidateLimit = Math.Clamp(source.DefaultCandidateLimit, 10, 100);
                var items = document.Descendants("item").Take(candidateLimit).ToList();

                foreach (var item in items)
                {
                    var title = ReadValue(item, "title");
                    var link = ReadValue(item, "link");
                    if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(link))
                    {
                        continue;
                    }

                    var summary = ReadValue(item, "description");
                    var quality = FeedArticleQualityFilter.Evaluate(source, title, summary, link);
                    if (quality.RejectionReason is not null)
                    {
                        logs.Add($"Rejected {source.Name}: {title} ({quality.RejectionReason}).");
                        continue;
                    }

                    var content = _contentExtractor is null
                        ? ArticleContentExtractionResult.SummaryFallback(summary)
                        : await _contentExtractor.ExtractAsync(link, summary, cancellationToken);

                    await _articles.UpsertAsync(new Article
                    {
                        Id = CreateStableId(link),
                        Title = title,
                        Summary = summary,
                        Content = content.Content,
                        ContentText = content.ContentText,
                        ContentStatus = content.Status,
                        ContentExtractedAt = content.ExtractedAt,
                        SourceUrl = link,
                        SourceId = source.Id,
                        SourceName = source.Name,
                        Tags = quality.MatchedKeywords.Count > 0
                            ? quality.MatchedKeywords
                            : ["rss"],
                        IngestionScore = quality.Score,
                        MatchedKeywords = quality.MatchedKeywords,
                        SourceQualityTier = source.SourceQualityTier,
                        PublishedAt = ParsePublishedAt(ReadValue(item, "pubDate")),
                        HasAiSummary = false,
                        IsBookmarked = false,
                        ReadTimeMinutes = null
                    }, cancellationToken);

                    articlesPersisted++;
                }

                source.LastCrawledAt = DateTimeOffset.UtcNow;
                logs.Add($"Crawled {source.Name}: {items.Count} RSS candidates read.");
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
