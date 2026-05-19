using AiDaily.Application.AiSummaries;
using AiDaily.Domain.Entities;

namespace AiDaily.Infrastructure.AI;

public sealed class InMemoryAiSummaryRepository : IAiSummaryRepository
{
    private readonly List<AiSummary> _summaries =
    [
        new AiSummary
        {
            Id = "sum_01JAI001",
            ArticleId = "art_01JAI001",
            Highlights =
            [
                "Developer agent tooling is moving from demos into production operations.",
                "Tracing and safer tool execution are becoming baseline platform features.",
                "Teams adopting agents should review debugging and approval workflows early."
            ],
            ImpactScope = "Developer platforms, product teams, and internal automation groups.",
            Controversy = "More capable agents can reduce manual work, but tool permissions and auditability remain pressure points.",
            EditorView = "Worth piloting in a constrained workflow before broad rollout.",
            Provider = "seed",
            PromptVersion = "quick-summary-seed-v1",
            GeneratedAt = DateTimeOffset.Parse("2026-05-12T08:15:00Z")
        },
        new AiSummary
        {
            Id = "sum_01JAI003",
            ArticleId = "art_01JAI003",
            Highlights =
            [
                "Evaluation quality is shifting toward deployment monitoring, not only pre-release tests.",
                "Safety teams may need continuous measurement after models reach users.",
                "Benchmark gaps can hide operational risks until real traffic appears."
            ],
            ImpactScope = "AI safety, governance, model evaluation, and compliance programs.",
            Controversy = "The industry still lacks agreement on which post-deployment signals should count as reliable safety evidence.",
            EditorView = "High-signal framing for teams building model release checklists.",
            Provider = "seed",
            PromptVersion = "quick-summary-seed-v1",
            GeneratedAt = DateTimeOffset.Parse("2026-05-11T22:00:00Z")
        }
    ];

    public Task<AiSummary?> GetByArticleIdAsync(string articleId, CancellationToken cancellationToken) =>
        Task.FromResult(_summaries.FirstOrDefault(summary => summary.ArticleId == articleId));

    public Task SaveAsync(AiSummary summary, CancellationToken cancellationToken)
    {
        var existingIndex = _summaries.FindIndex(item => item.ArticleId == summary.ArticleId);
        if (existingIndex >= 0)
        {
            _summaries[existingIndex] = summary;
        }
        else
        {
            _summaries.Add(summary);
        }

        return Task.CompletedTask;
    }
}
