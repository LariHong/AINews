using AiDaily.Domain.Entities;

namespace AiDaily.Application.Bookmarks;

public interface IBookmarkRepository
{
    Task<IReadOnlySet<string>> ListArticleIdsAsync(string userId, CancellationToken cancellationToken);
    Task<bool> ExistsAsync(string userId, string articleId, CancellationToken cancellationToken);
    Task SaveAsync(Bookmark bookmark, CancellationToken cancellationToken);
    Task DeleteAsync(string userId, string articleId, CancellationToken cancellationToken);
}
