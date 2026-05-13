using AiDaily.Domain.Entities;

namespace AiDaily.Application.AiSummaries;

public interface IAiSummaryRepository
{
    Task<AiSummary?> GetByArticleIdAsync(string articleId, CancellationToken cancellationToken);
}
