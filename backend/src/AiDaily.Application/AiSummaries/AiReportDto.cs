namespace AiDaily.Application.AiSummaries;

public sealed record AiReportDto(
    string ArticleId,
    string Tldr,
    IReadOnlyList<string> KeyPoints,
    IReadOnlyList<string> Pros,
    IReadOnlyList<string> Cons,
    IReadOnlyList<AiReportTimelineItemDto> Timeline,
    AiReportScoresDto Scores,
    IReadOnlyList<string> RelatedTags,
    string EditorNote,
    string Rating,
    string Provider,
    DateTimeOffset GeneratedAt);

public sealed record AiReportTimelineItemDto(string Label, string Description);

public sealed record AiReportScoresDto(int Impact, int Confidence, int Controversy);
