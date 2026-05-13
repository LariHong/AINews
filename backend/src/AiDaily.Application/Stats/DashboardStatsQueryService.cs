using AiDaily.Application.Articles;
using AiDaily.Domain.Entities;

namespace AiDaily.Application.Stats;

public sealed class DashboardStatsQueryService
{
    private readonly IArticleRepository _articles;
    private readonly TimeProvider _timeProvider;

    public DashboardStatsQueryService(IArticleRepository articles, TimeProvider timeProvider)
    {
        _articles = articles;
        _timeProvider = timeProvider;
    }

    public async Task<DashboardStatsDto> GetTodayAsync(CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(_timeProvider.GetUtcNow().UtcDateTime);
        var allArticles = await _articles.ListAsync(cancellationToken);
        var todayArticles = allArticles
            .Where(article => DateOnly.FromDateTime(article.PublishedAt.UtcDateTime) == today)
            .OrderByDescending(article => article.PublishedAt)
            .ToList();

        return new DashboardStatsDto(
            todayArticles.Count,
            todayArticles.Count(article => article.HasAiSummary),
            BuildBreakdown(todayArticles.SelectMany(article => article.Tags)),
            BuildBreakdown(todayArticles.Select(article => article.SourceName)),
            todayArticles.Count == 0 ? null : todayArticles.Max(article => article.PublishedAt),
            BuildSyncStatus(todayArticles));
    }

    private static IReadOnlyList<StatsBreakdownItemDto> BuildBreakdown(IEnumerable<string> values) =>
        values
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .GroupBy(value => value, StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(group => group.Count())
            .ThenBy(group => group.Key)
            .Take(5)
            .Select(group => new StatsBreakdownItemDto(group.Key, group.Count()))
            .ToList();

    private static DashboardSyncStatusDto BuildSyncStatus(IReadOnlyList<Article> articles)
    {
        var sourceCount = articles
            .Select(article => article.SourceId ?? article.SourceName)
            .Where(source => !string.IsNullOrWhiteSpace(source))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();

        return new DashboardSyncStatusDto(
            IsSyncing: false,
            SourcesSynced: sourceCount,
            SourceFailures: 0,
            Message: sourceCount == 0
                ? "No sources synced today"
                : $"{sourceCount} sources synced");
    }
}
