namespace AiDaily.Domain.Entities;

public sealed class AiReport
{
    public required string Id { get; init; }
    public required string ArticleId { get; init; }
    public required string Tldr { get; init; }
    public IReadOnlyList<string> KeyPoints { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> Pros { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> Cons { get; init; } = Array.Empty<string>();
    public IReadOnlyList<AiReportTimelineItem> Timeline { get; init; } = Array.Empty<AiReportTimelineItem>();
    public required AiReportScores Scores { get; init; }
    public IReadOnlyList<string> RelatedTags { get; init; } = Array.Empty<string>();
    public required string EditorNote { get; init; }
    public required string Rating { get; init; }
    public required string Provider { get; init; }
    public DateTimeOffset GeneratedAt { get; init; }
}

public sealed record AiReportTimelineItem(string Label, string Description);

public sealed record AiReportScores(int Impact, int Confidence, int Controversy);
