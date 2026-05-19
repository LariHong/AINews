using AiDaily.Application.Articles;
using AiDaily.Domain.Entities;

namespace AiDaily.Application.UserPreferences;

public sealed class UserPreferenceService
{
    private readonly IArticleRepository _articles;
    private readonly IHiddenArticleRepository _hiddenArticles;
    private readonly TimeProvider _timeProvider;

    public UserPreferenceService(
        IArticleRepository articles,
        IHiddenArticleRepository hiddenArticles,
        TimeProvider timeProvider)
    {
        _articles = articles;
        _hiddenArticles = hiddenArticles;
        _timeProvider = timeProvider;
    }

    public async Task<HiddenArticleMutationResult> HideArticleAsync(
        string userId,
        string articleId,
        string? reason,
        CancellationToken cancellationToken = default)
    {
        if (!IsValidLocalUser(userId))
        {
            return new HiddenArticleMutationResult(HiddenArticleMutationStatus.InvalidUser, null);
        }

        var article = await _articles.GetByIdAsync(articleId, cancellationToken);
        if (article is null)
        {
            return new HiddenArticleMutationResult(HiddenArticleMutationStatus.ArticleNotFound, articleId);
        }

        if (!await _hiddenArticles.ExistsAsync(userId, articleId, cancellationToken))
        {
            await _hiddenArticles.SaveAsync(new HiddenArticle
            {
                UserId = userId,
                ArticleId = articleId,
                Reason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim(),
                CreatedAt = _timeProvider.GetUtcNow()
            }, cancellationToken);
        }

        return new HiddenArticleMutationResult(HiddenArticleMutationStatus.Ready, articleId);
    }

    public async Task<HiddenArticleMutationResult> RestoreArticleAsync(
        string userId,
        string articleId,
        CancellationToken cancellationToken = default)
    {
        if (!IsValidLocalUser(userId))
        {
            return new HiddenArticleMutationResult(HiddenArticleMutationStatus.InvalidUser, null);
        }

        await _hiddenArticles.DeleteAsync(userId, articleId, cancellationToken);
        return new HiddenArticleMutationResult(HiddenArticleMutationStatus.Ready, articleId);
    }

    public async Task<IReadOnlyList<ArticleDto>> ListHiddenArticlesAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        if (!IsValidLocalUser(userId))
        {
            return [];
        }

        var hiddenIds = await _hiddenArticles.ListArticleIdsAsync(userId, cancellationToken);
        var articles = await _articles.ListAsync(cancellationToken);

        return articles
            .Where(article => hiddenIds.Contains(article.Id))
            .OrderByDescending(article => article.PublishedAt)
            .ThenBy(article => article.Id)
            .Select(article => ArticleDto.FromArticle(article, isBookmarked: false))
            .ToList();
    }

    private static bool IsValidLocalUser(string userId) =>
        !string.IsNullOrWhiteSpace(userId) && userId.StartsWith("local_", StringComparison.Ordinal);
}

public sealed record HiddenArticleMutationResult(HiddenArticleMutationStatus Status, string? ArticleId);

public enum HiddenArticleMutationStatus
{
    Ready,
    InvalidUser,
    ArticleNotFound
}
