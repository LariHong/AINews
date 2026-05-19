namespace AiDaily.Domain.Entities;

public sealed class AiSummary
{
    public required string Id { get; init; }
    public required string ArticleId { get; init; }
    public IReadOnlyList<string> Highlights { get; init; } = Array.Empty<string>();
    public required string ImpactScope { get; init; }
    public required string Controversy { get; init; }
    public required string EditorView { get; init; }
    public required string Provider { get; init; }
    public required string PromptVersion { get; init; }
    public DateTimeOffset GeneratedAt { get; init; }
}
