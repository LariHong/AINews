namespace AiDaily.Application.AiSummaries;

public sealed record AiReportDraft(
    string Tldr,
    IReadOnlyList<string> KeyPoints,
    IReadOnlyList<string> Pros,
    IReadOnlyList<string> Cons,
    IReadOnlyList<AiReportTimelineItemDto> Timeline,
    AiReportScoresDto Scores,
    IReadOnlyList<string> RelatedTags,
    string EditorNote,
    string Rating);
