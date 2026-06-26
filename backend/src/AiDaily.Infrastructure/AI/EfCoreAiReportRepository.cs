using AiDaily.Application.AiSummaries;
using AiDaily.Domain.Entities;
using AiDaily.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AiDaily.Infrastructure.AI;

public sealed class EfCoreAiReportRepository : IAiReportRepository
{
    private readonly AiDailyDbContext _dbContext;

    public EfCoreAiReportRepository(AiDailyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<AiReport?> GetByArticleIdAsync(string articleId, CancellationToken cancellationToken) =>
        await _dbContext.AiReports
            .AsNoTracking()
            .FirstOrDefaultAsync(report => report.ArticleId == articleId, cancellationToken);

    public async Task SaveAsync(AiReport report, CancellationToken cancellationToken)
    {
        var existing = await _dbContext.AiReports
            .FirstOrDefaultAsync(item => item.ArticleId == report.ArticleId, cancellationToken);

        if (existing is null)
        {
            _dbContext.AiReports.Add(report);
        }
        else if (existing.Id != report.Id)
        {
            _dbContext.AiReports.Remove(existing);
            _dbContext.AiReports.Add(report);
        }
        else
        {
            _dbContext.Entry(existing).CurrentValues.SetValues(report);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
