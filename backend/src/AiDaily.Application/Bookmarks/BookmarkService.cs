using AiDaily.Application.Articles;
using AiDaily.Domain.Entities;

namespace AiDaily.Application.Bookmarks;

public sealed class BookmarkService
{
    private readonly IArticleRepository _articles;
    private readonly IBookmarkRepository _bookmarks;
    private readonly TimeProvider _timeProvider;

    public BookmarkService(
        IArticleRepository articles,
        IBookmarkRepository bookmarks,
        TimeProvider timeProvider)
    {
        _articles = articles;
        _bookmarks = bookmarks;
        _timeProvider = timeProvider;
    }

    public async Task<BookmarkMutationResult> AddAsync(
        string userId,
        string articleId,
        CancellationToken cancellationToken = default)
    {
        if (!IsValidLocalUser(userId))
        {
            return new BookmarkMutationResult(BookmarkMutationStatus.InvalidUser, null);
        }

        var article = await _articles.GetByIdAsync(articleId, cancellationToken);
        if (article is null)
        {
            return new BookmarkMutationResult(BookmarkMutationStatus.ArticleNotFound, articleId);
        }

        if (!await _bookmarks.ExistsAsync(userId, articleId, cancellationToken))
        {
            await _bookmarks.SaveAsync(new Bookmark
            {
                UserId = userId,
                ArticleId = articleId,
                CreatedAt = _timeProvider.GetUtcNow()
            }, cancellationToken);
        }

        return new BookmarkMutationResult(BookmarkMutationStatus.Ready, articleId);
    }

    public async Task<BookmarkMutationResult> DeleteAsync(
        string userId,
        string articleId,
        CancellationToken cancellationToken = default)
    {
        if (!IsValidLocalUser(userId))
        {
            return new BookmarkMutationResult(BookmarkMutationStatus.InvalidUser, null);
        }

        await _bookmarks.DeleteAsync(userId, articleId, cancellationToken);
        return new BookmarkMutationResult(BookmarkMutationStatus.Ready, articleId);
    }

    public async Task<IReadOnlyList<ArticleDto>> ListAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        if (!IsValidLocalUser(userId))
        {
            return [];
        }

        var bookmarkIds = await _bookmarks.ListArticleIdsAsync(userId, cancellationToken);
        var articles = await _articles.ListAsync(cancellationToken);

        return articles
            .Where(article => bookmarkIds.Contains(article.Id))
            .Where(article => string.IsNullOrWhiteSpace(article.RejectionReason))
            .OrderByDescending(article => article.PublishedAt)
            .ThenBy(article => article.Id)
            .Select(article => ArticleDto.FromArticle(article, isBookmarked: true))
            .ToList();
    }

    private static bool IsValidLocalUser(string userId) =>
        !string.IsNullOrWhiteSpace(userId) && userId.StartsWith("local_", StringComparison.Ordinal);
}

public sealed record BookmarkMutationResult(BookmarkMutationStatus Status, string? ArticleId);

public enum BookmarkMutationStatus
{
    Ready,
    InvalidUser,
    ArticleNotFound
}
