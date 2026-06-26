using AiDaily.Application.UserPreferences;
using AiDaily.Domain.Entities;
using AiDaily.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AiDaily.Infrastructure.Repositories;

public sealed class EfCoreHiddenArticleRepository : IHiddenArticleRepository
{
    private readonly AiDailyDbContext _dbContext;

    public EfCoreHiddenArticleRepository(AiDailyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlySet<string>> ListArticleIdsAsync(string userId, CancellationToken cancellationToken)
    {
        var ids = await _dbContext.HiddenArticles
            .AsNoTracking()
            .Where(hiddenArticle => hiddenArticle.UserId == userId)
            .Select(hiddenArticle => hiddenArticle.ArticleId)
            .ToListAsync(cancellationToken);

        return ids.ToHashSet(StringComparer.Ordinal);
    }

    public async Task<bool> ExistsAsync(string userId, string articleId, CancellationToken cancellationToken) =>
        await _dbContext.HiddenArticles
            .AsNoTracking()
            .AnyAsync(hiddenArticle => hiddenArticle.UserId == userId && hiddenArticle.ArticleId == articleId, cancellationToken);

    public async Task SaveAsync(HiddenArticle hiddenArticle, CancellationToken cancellationToken)
    {
        if (!await ExistsAsync(hiddenArticle.UserId, hiddenArticle.ArticleId, cancellationToken))
        {
            _dbContext.HiddenArticles.Add(hiddenArticle);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task DeleteAsync(string userId, string articleId, CancellationToken cancellationToken)
    {
        var existing = await _dbContext.HiddenArticles
            .FirstOrDefaultAsync(hiddenArticle =>
                hiddenArticle.UserId == userId &&
                hiddenArticle.ArticleId == articleId,
                cancellationToken);

        if (existing is not null)
        {
            _dbContext.HiddenArticles.Remove(existing);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
