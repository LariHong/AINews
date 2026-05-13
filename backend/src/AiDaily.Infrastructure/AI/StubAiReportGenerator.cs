using AiDaily.Application.AiSummaries;
using AiDaily.Domain.Entities;

namespace AiDaily.Infrastructure.AI;

public sealed class StubAiReportGenerator : IAiReportGenerator
{
    public string ProviderName => "stub";

    public Task<AiReportDraft> GenerateAsync(Article article, CancellationToken cancellationToken)
    {
        var primaryTag = article.Tags.FirstOrDefault() ?? "AI";
        var published = article.PublishedAt.ToString("yyyy-MM-dd");

        var report = new AiReportDraft(
            $"{article.Title} matters because it connects {primaryTag} news to near-term product and engineering decisions.",
            [
                article.Summary ?? "The imported article did not include a source summary.",
                $"{article.SourceName} is the source of record for this indexed story.",
                $"Teams tracking {primaryTag} should verify the original article before making roadmap choices."
            ],
            [
                "Gives readers a fast structured view of why the story matters.",
                "Creates a reusable baseline report that can be refreshed when provider mode changes."
            ],
            [
                "The generated analysis is only as complete as the imported article metadata.",
                "Facts beyond the indexed source should be verified before operational use."
            ],
            [
                new AiReportTimelineItemDto(published, $"{article.SourceName} published the story."),
                new AiReportTimelineItemDto("Indexed", "AI Daily imported the article and queued it for report generation.")
            ],
            new AiReportScoresDto(72, 68, 35),
            article.Tags.Count > 0 ? article.Tags : [primaryTag],
            "Treat this as a useful first-pass report, then open the source for final confirmation.",
            "watchlist");

        return Task.FromResult(report);
    }
}
