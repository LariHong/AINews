namespace AiDaily.Application.FeedCrawler;

public sealed class FeedCrawlRunState : IFeedCrawlStatusReader
{
    private readonly object _lock = new();
    private FeedCrawlStatusSnapshot _current = new(
        IsSyncing: false,
        SourcesSynced: 0,
        SourceFailures: 0,
        LastCompletedAt: null,
        Message: "Source status pending");

    public FeedCrawlStatusSnapshot Current
    {
        get
        {
            lock (_lock)
            {
                return _current;
            }
        }
    }

    public bool TryStart()
    {
        lock (_lock)
        {
            if (_current.IsSyncing)
            {
                return false;
            }

            _current = _current with
            {
                IsSyncing = true,
                Message = "Syncing sources..."
            };
            return true;
        }
    }

    public void Complete(FeedCrawlResult result, DateTimeOffset completedAt)
    {
        var sourceFailures = result.Logs.Count(log => log.StartsWith("Crawler skipped", StringComparison.OrdinalIgnoreCase));
        var sourcesSynced = Math.Max(0, result.SourcesVisited - sourceFailures);

        lock (_lock)
        {
            _current = new FeedCrawlStatusSnapshot(
                IsSyncing: false,
                SourcesSynced: sourcesSynced,
                SourceFailures: sourceFailures,
                LastCompletedAt: completedAt,
                Message: sourceFailures > 0
                    ? $"{sourcesSynced} sources synced; {sourceFailures} source failed"
                    : $"{sourcesSynced} sources synced");
        }
    }

    public void Fail(string message, DateTimeOffset completedAt)
    {
        lock (_lock)
        {
            _current = new FeedCrawlStatusSnapshot(
                IsSyncing: false,
                SourcesSynced: 0,
                SourceFailures: 1,
                LastCompletedAt: completedAt,
                Message: message);
        }
    }
}
