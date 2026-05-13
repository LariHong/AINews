using AiDaily.Application.AiSummaries;
using AiDaily.Domain.Entities;
using System.Collections.Concurrent;

namespace AiDaily.Infrastructure.AI;

public sealed class InMemoryAiReportRepository : IAiReportRepository
{
    private readonly ConcurrentDictionary<string, AiReport> _reports = new(StringComparer.Ordinal);

    public Task<AiReport?> GetByArticleIdAsync(string articleId, CancellationToken cancellationToken)
    {
        _reports.TryGetValue(articleId, out var report);
        return Task.FromResult(report);
    }

    public Task SaveAsync(AiReport report, CancellationToken cancellationToken)
    {
        _reports[report.ArticleId] = report;
        return Task.CompletedTask;
    }
}
