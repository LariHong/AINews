using AiDaily.Application.Bookmarks;
using AiDaily.Domain.Entities;

namespace AiDaily.Infrastructure.Repositories;

public sealed class InMemoryBookmarkRepository : IBookmarkRepository
{
    private readonly object _syncRoot = new();
    private readonly List<Bookmark> _bookmarks = [];

    public Task<IReadOnlySet<string>> ListArticleIdsAsync(string userId, CancellationToken cancellationToken)
    {
        lock (_syncRoot)
        {
            return Task.FromResult<IReadOnlySet<string>>(
                _bookmarks
                    .Where(bookmark => bookmark.UserId == userId)
                    .Select(bookmark => bookmark.ArticleId)
                    .ToHashSet(StringComparer.Ordinal));
        }
    }

    public Task<bool> ExistsAsync(string userId, string articleId, CancellationToken cancellationToken)
    {
        lock (_syncRoot)
        {
            return Task.FromResult(_bookmarks.Any(bookmark =>
                bookmark.UserId == userId &&
                bookmark.ArticleId == articleId));
        }
    }

    public Task SaveAsync(Bookmark bookmark, CancellationToken cancellationToken)
    {
        lock (_syncRoot)
        {
            if (!_bookmarks.Any(item =>
                item.UserId == bookmark.UserId &&
                item.ArticleId == bookmark.ArticleId))
            {
                _bookmarks.Add(bookmark);
            }
        }

        return Task.CompletedTask;
    }

    public Task DeleteAsync(string userId, string articleId, CancellationToken cancellationToken)
    {
        lock (_syncRoot)
        {
            _bookmarks.RemoveAll(bookmark =>
                bookmark.UserId == userId &&
                bookmark.ArticleId == articleId);
        }

        return Task.CompletedTask;
    }
}
