using AiDaily.Application.Articles;
using AiDaily.Domain.Entities;
using AiDaily.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AiDaily.Infrastructure.Repositories;

public sealed class EfCoreArticleRepository : IArticleRepository
{
    private readonly AiDailyDbContext _dbContext;

    public EfCoreArticleRepository(AiDailyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<Article>> ListAsync(CancellationToken cancellationToken) =>
        await _dbContext.Articles
            .AsNoTracking()
            .ToListAsync(cancellationToken);

    public async Task<Article?> GetByIdAsync(string id, CancellationToken cancellationToken) =>
        await _dbContext.Articles
            .AsNoTracking()
            .FirstOrDefaultAsync(article => article.Id == id, cancellationToken);

    public async Task UpsertAsync(Article article, CancellationToken cancellationToken)
    {
        var existing = await _dbContext.Articles
            .FirstOrDefaultAsync(item => item.Id == article.Id || item.SourceUrl == article.SourceUrl, cancellationToken);

        if (existing is null)
        {
            _dbContext.Articles.Add(article);
        }
        else if (existing.Id == article.Id)
        {
            _dbContext.Entry(existing).CurrentValues.SetValues(article);
        }
        else
        {
            _dbContext.Articles.Remove(existing);
            _dbContext.Articles.Add(article);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
