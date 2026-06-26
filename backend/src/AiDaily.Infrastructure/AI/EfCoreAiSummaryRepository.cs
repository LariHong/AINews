using AiDaily.Application.AiSummaries;
using AiDaily.Domain.Entities;
using AiDaily.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AiDaily.Infrastructure.AI;

public sealed class EfCoreAiSummaryRepository : IAiSummaryRepository
{
    private readonly AiDailyDbContext _dbContext;

    public EfCoreAiSummaryRepository(AiDailyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<AiSummary?> GetByArticleIdAsync(string articleId, CancellationToken cancellationToken) =>
        await _dbContext.AiSummaries
            .AsNoTracking()
            .FirstOrDefaultAsync(summary => summary.ArticleId == articleId, cancellationToken);

    public async Task<IReadOnlySet<string>> ListArticleIdsWithSummariesAsync(
        IEnumerable<string> articleIds,
        CancellationToken cancellationToken)
    {
        var requested = articleIds.ToHashSet(StringComparer.Ordinal);
        var existingIds = await _dbContext.AiSummaries
            .AsNoTracking()
            .Where(summary => requested.Contains(summary.ArticleId))
            .Select(summary => summary.ArticleId)
            .ToListAsync(cancellationToken);

        return existingIds.ToHashSet(StringComparer.Ordinal);
    }

    public async Task SaveAsync(AiSummary summary, CancellationToken cancellationToken)
    {
        var existing = await _dbContext.AiSummaries
            .FirstOrDefaultAsync(item => item.ArticleId == summary.ArticleId, cancellationToken);

        if (existing is null)
        {
            _dbContext.AiSummaries.Add(summary);
        }
        else if (existing.Id != summary.Id)
        {
            _dbContext.AiSummaries.Remove(existing);
            _dbContext.AiSummaries.Add(summary);
        }
        else
        {
            _dbContext.Entry(existing).CurrentValues.SetValues(summary);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
