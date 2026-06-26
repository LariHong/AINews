using AiDaily.Application.Bookmarks;
using AiDaily.Domain.Entities;
using AiDaily.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AiDaily.Infrastructure.Repositories;

public sealed class EfCoreBookmarkRepository : IBookmarkRepository
{
    private readonly AiDailyDbContext _dbContext;

    public EfCoreBookmarkRepository(AiDailyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlySet<string>> ListArticleIdsAsync(string userId, CancellationToken cancellationToken)
    {
        var ids = await _dbContext.Bookmarks
            .AsNoTracking()
            .Where(bookmark => bookmark.UserId == userId)
            .Select(bookmark => bookmark.ArticleId)
            .ToListAsync(cancellationToken);

        return ids.ToHashSet(StringComparer.Ordinal);
    }

    public async Task<bool> ExistsAsync(string userId, string articleId, CancellationToken cancellationToken) =>
        await _dbContext.Bookmarks
            .AsNoTracking()
            .AnyAsync(bookmark => bookmark.UserId == userId && bookmark.ArticleId == articleId, cancellationToken);

    public async Task SaveAsync(Bookmark bookmark, CancellationToken cancellationToken)
    {
        if (!await ExistsAsync(bookmark.UserId, bookmark.ArticleId, cancellationToken))
        {
            _dbContext.Bookmarks.Add(bookmark);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task DeleteAsync(string userId, string articleId, CancellationToken cancellationToken)
    {
        var existing = await _dbContext.Bookmarks
            .FirstOrDefaultAsync(bookmark => bookmark.UserId == userId && bookmark.ArticleId == articleId, cancellationToken);

        if (existing is not null)
        {
            _dbContext.Bookmarks.Remove(existing);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
