using AiDaily.Domain.Entities;

namespace AiDaily.Application.FeedCrawler;

public interface IFeedCrawler
{
    Task<FeedCrawlResult> CrawlAsync(
        IEnumerable<FeedSource> sources,
        CancellationToken cancellationToken = default);
}

public interface IFeedSourceCatalog
{
    IReadOnlyList<FeedSource> GetEnabledSources();
}

public interface IFeedSourceMetadataRepository
{
    Task SaveAsync(FeedSource source, CancellationToken cancellationToken = default);
}

public interface IFeedCrawlStatusReader
{
    FeedCrawlStatusSnapshot Current { get; }
}

public sealed record FeedCrawlResult(
    int SourcesVisited,
    int ArticlesPersisted,
    IReadOnlyList<string> Logs);

public sealed record FeedCrawlStatusSnapshot(
    bool IsSyncing,
    int SourcesSynced,
    int SourceFailures,
    DateTimeOffset? LastCompletedAt,
    string Message);

public sealed record FeedCrawlRunRequest(string Scope);

public sealed record FeedCrawlRunDto(
    string Scope,
    string Status,
    int SourcesVisited,
    int ArticlesPersisted,
    int SourceFailures,
    IReadOnlyList<string> Logs,
    DateTimeOffset CompletedAt);
