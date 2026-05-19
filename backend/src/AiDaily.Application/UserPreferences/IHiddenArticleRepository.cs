using AiDaily.Domain.Entities;

namespace AiDaily.Application.UserPreferences;

public interface IHiddenArticleRepository
{
    Task<IReadOnlySet<string>> ListArticleIdsAsync(string userId, CancellationToken cancellationToken);
    Task<bool> ExistsAsync(string userId, string articleId, CancellationToken cancellationToken);
    Task SaveAsync(HiddenArticle hiddenArticle, CancellationToken cancellationToken);
    Task DeleteAsync(string userId, string articleId, CancellationToken cancellationToken);
}
