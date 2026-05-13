namespace AiDaily.Infrastructure.FeedCrawler;

public sealed record FeedCrawlResult(int SourcesVisited, int ArticlesPersisted, IReadOnlyList<string> Logs);
