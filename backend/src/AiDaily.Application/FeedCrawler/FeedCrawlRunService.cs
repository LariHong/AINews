namespace AiDaily.Application.FeedCrawler;

public sealed class FeedCrawlRunService
{
    private readonly IFeedCrawler _crawler;
    private readonly IFeedSourceCatalog _sources;
    private readonly FeedCrawlRunState _state;
    private readonly TimeProvider _timeProvider;

    public FeedCrawlRunService(
        IFeedCrawler crawler,
        IFeedSourceCatalog sources,
        FeedCrawlRunState state,
        TimeProvider timeProvider)
    {
        _crawler = crawler;
        _sources = sources;
        _state = state;
        _timeProvider = timeProvider;
    }

    public async Task<FeedCrawlRunDto> RunAsync(
        FeedCrawlRunRequest request,
        CancellationToken cancellationToken = default)
    {
        var scope = NormalizeScope(request.Scope);
        if (!_state.TryStart())
        {
            var snapshot = _state.Current;
            return new FeedCrawlRunDto(
                scope,
                "already_running",
                snapshot.SourcesSynced,
                0,
                snapshot.SourceFailures,
                [snapshot.Message],
                _timeProvider.GetUtcNow());
        }

        try
        {
            var result = await _crawler.CrawlAsync(_sources.GetEnabledSources(), cancellationToken);
            var completedAt = _timeProvider.GetUtcNow();
            _state.Complete(result, completedAt);

            return new FeedCrawlRunDto(
                scope,
                "completed",
                result.SourcesVisited,
                result.ArticlesPersisted,
                result.Logs.Count(log => log.StartsWith("Crawler skipped", StringComparison.OrdinalIgnoreCase)),
                result.Logs,
                completedAt);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            var completedAt = _timeProvider.GetUtcNow();
            _state.Fail("Feed sync failed", completedAt);

            return new FeedCrawlRunDto(
                scope,
                "failed",
                0,
                0,
                1,
                [ex.Message],
                completedAt);
        }
    }

    private static string NormalizeScope(string? scope) =>
        string.Equals(scope, "today", StringComparison.OrdinalIgnoreCase) ? "today" : "today";
}
