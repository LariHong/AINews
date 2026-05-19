namespace AiDaily.Application.AiSummaries;

public sealed record AiSummaryDto(
    string ArticleId,
    IReadOnlyList<string> Highlights,
    string ImpactScope,
    string Controversy,
    string EditorView,
    string Provider,
    string PromptVersion,
    DateTimeOffset GeneratedAt);
