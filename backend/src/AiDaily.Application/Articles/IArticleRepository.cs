using AiDaily.Domain.Entities;

namespace AiDaily.Application.Articles;

public interface IArticleRepository
{
    Task<IReadOnlyList<Article>> ListAsync(CancellationToken cancellationToken);
}
