using AiDaily.Domain.Entities;

namespace AiDaily.Application.AiSummaries;

public interface IAiSummaryGenerator
{
    string ProviderName { get; }
    string PromptVersion { get; }
    Task<AiSummaryDraft> GenerateAsync(Article article, CancellationToken cancellationToken);
}

public sealed record AiSummaryDraft(
    IReadOnlyList<string> Highlights,
    string ImpactScope,
    string Controversy,
    string EditorView);
