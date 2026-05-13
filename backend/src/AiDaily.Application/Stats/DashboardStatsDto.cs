namespace AiDaily.Application.Stats;

public sealed record DashboardStatsDto(
    int TotalArticles,
    int AiSummarizedCount,
    IReadOnlyList<StatsBreakdownItemDto> TagBreakdown,
    IReadOnlyList<StatsBreakdownItemDto> TopSources,
    DateTimeOffset? UpdatedAt,
    DashboardSyncStatusDto SyncStatus);

public sealed record StatsBreakdownItemDto(string Name, int Count);

public sealed record DashboardSyncStatusDto(
    bool IsSyncing,
    int SourcesSynced,
    int SourceFailures,
    string Message);
