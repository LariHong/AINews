using AiDaily.Application.AiSummaries;
using AiDaily.Domain.Entities;

namespace AiDaily.Infrastructure.AI;

public sealed class StubAiSummaryGenerator : IAiSummaryGenerator
{
    public string ProviderName => "stub";
    public string PromptVersion => "quick-summary-v1";

    public Task<AiSummaryDraft> GenerateAsync(Article article, CancellationToken cancellationToken)
    {
        var primaryTag = article.Tags.FirstOrDefault() ?? "AI";
        var inputBasis = article.ContentStatus == "full_content_ready"
            ? "full imported source text"
            : "imported RSS summary and source metadata";

        var draft = new AiSummaryDraft(
            [
                $"{article.Title} is relevant to teams tracking {primaryTag}.",
                $"This quick summary is based on {inputBasis}.",
                $"Readers should verify operational decisions against {article.SourceName}."
            ],
            $"Most relevant to {primaryTag} readers, product teams, and AI news monitoring workflows.",
            article.ContentStatus == "full_content_ready"
                ? "The main uncertainty is whether the source claims hold up across follow-up coverage."
                : "Because full source content is not available, this summary avoids claiming full-article analysis.",
            article.ContentStatus == "full_content_ready"
                ? "Useful as a quick preview before reading the full source."
                : "Useful as a summary-backed preview, not a substitute for the full original article.");

        return Task.FromResult(draft);
    }
}
