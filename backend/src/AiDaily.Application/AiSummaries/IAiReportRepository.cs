using AiDaily.Domain.Entities;

namespace AiDaily.Application.AiSummaries;

public interface IAiReportRepository
{
    Task<AiReport?> GetByArticleIdAsync(string articleId, CancellationToken cancellationToken);
    Task SaveAsync(AiReport report, CancellationToken cancellationToken);
}
