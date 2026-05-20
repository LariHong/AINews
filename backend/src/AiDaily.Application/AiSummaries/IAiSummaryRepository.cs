using AiDaily.Domain.Entities;

namespace AiDaily.Application.AiSummaries;

public interface IAiSummaryRepository
{
    Task<AiSummary?> GetByArticleIdAsync(string articleId, CancellationToken cancellationToken);
    Task<IReadOnlySet<string>> ListArticleIdsWithSummariesAsync(
        IEnumerable<string> articleIds,
        CancellationToken cancellationToken);
    Task SaveAsync(AiSummary summary, CancellationToken cancellationToken);
}
