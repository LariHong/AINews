using AiDaily.Application.UserPreferences;
using AiDaily.Domain.Entities;

namespace AiDaily.Infrastructure.Repositories;

public sealed class InMemoryHiddenArticleRepository : IHiddenArticleRepository
{
    private readonly object _syncRoot = new();
    private readonly List<HiddenArticle> _hiddenArticles = [];

    public Task<IReadOnlySet<string>> ListArticleIdsAsync(string userId, CancellationToken cancellationToken)
    {
        lock (_syncRoot)
        {
            return Task.FromResult<IReadOnlySet<string>>(
                _hiddenArticles
                    .Where(item => item.UserId == userId)
                    .Select(item => item.ArticleId)
                    .ToHashSet(StringComparer.Ordinal));
        }
    }

    public Task<bool> ExistsAsync(string userId, string articleId, CancellationToken cancellationToken)
    {
        lock (_syncRoot)
        {
            return Task.FromResult(_hiddenArticles.Any(item =>
                item.UserId == userId &&
                item.ArticleId == articleId));
        }
    }

    public Task SaveAsync(HiddenArticle hiddenArticle, CancellationToken cancellationToken)
    {
        lock (_syncRoot)
        {
            if (!_hiddenArticles.Any(item =>
                item.UserId == hiddenArticle.UserId &&
                item.ArticleId == hiddenArticle.ArticleId))
            {
                _hiddenArticles.Add(hiddenArticle);
            }
        }

        return Task.CompletedTask;
    }

    public Task DeleteAsync(string userId, string articleId, CancellationToken cancellationToken)
    {
        lock (_syncRoot)
        {
            _hiddenArticles.RemoveAll(item =>
                item.UserId == userId &&
                item.ArticleId == articleId);
        }

        return Task.CompletedTask;
    }
}
