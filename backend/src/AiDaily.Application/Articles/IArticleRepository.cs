using AiDaily.Domain.Entities;

namespace AiDaily.Application.Articles;

public interface IArticleRepository
{
    Task<IReadOnlyList<Article>> ListAsync(CancellationToken cancellationToken);
    Task<Article?> GetByIdAsync(string id, CancellationToken cancellationToken);
    Task UpsertAsync(Article article, CancellationToken cancellationToken);
}
