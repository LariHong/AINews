using AiDaily.Application.FeedCrawler;
using AiDaily.Domain.Entities;
using AiDaily.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AiDaily.Infrastructure.FeedCrawler;

public sealed class EfCoreFeedSourceCatalog : IFeedSourceCatalog, IFeedSourceMetadataRepository
{
    private static readonly string[] RetiredSeedSourceIds =
    [
        "venturebeat-ai",
        "machine-brief",
        "planet-ai"
    ];

    private readonly AiDailyDbContext _dbContext;

    public EfCoreFeedSourceCatalog(AiDailyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public IReadOnlyList<FeedSource> GetEnabledSources()
    {
        SeedIfEmpty();

        return _dbContext.FeedSources
            .AsNoTracking()
            .Where(source => source.IsEnabled)
            .OrderBy(source => source.Id)
            .ToList();
    }

    public async Task SaveAsync(FeedSource source, CancellationToken cancellationToken = default)
    {
        var existing = await _dbContext.FeedSources
            .FirstOrDefaultAsync(item => item.Id == source.Id || item.FeedUrl == source.FeedUrl, cancellationToken);

        if (existing is null)
        {
            _dbContext.FeedSources.Add(Clone(source));
        }
        else if (existing.Id == source.Id)
        {
            _dbContext.Entry(existing).CurrentValues.SetValues(source);
        }
        else
        {
            _dbContext.FeedSources.Remove(existing);
            _dbContext.FeedSources.Add(Clone(source));
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task SeedDefaultsAsync(CancellationToken cancellationToken = default)
    {
        foreach (var seed in SeedFeedSources.All)
        {
            var existing = await _dbContext.FeedSources
                .FirstOrDefaultAsync(item => item.Id == seed.Id || item.FeedUrl == seed.FeedUrl, cancellationToken);

            if (existing is null)
            {
                _dbContext.FeedSources.Add(Clone(seed));
                continue;
            }

            if (existing.Id != seed.Id)
            {
                _dbContext.FeedSources.Remove(existing);
                _dbContext.FeedSources.Add(Clone(seed, existing.LastCrawledAt));
                continue;
            }

            _dbContext.Entry(existing).CurrentValues.SetValues(Clone(seed, existing.LastCrawledAt));
        }

        var retiredSources = await _dbContext.FeedSources
            .Where(source => RetiredSeedSourceIds.Contains(source.Id) && source.IsEnabled)
            .ToListAsync(cancellationToken);
        foreach (var retiredSource in retiredSources)
        {
            _dbContext.Entry(retiredSource).Property(source => source.IsEnabled).CurrentValue = false;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private void SeedIfEmpty()
    {
        if (_dbContext.FeedSources.Any())
        {
            return;
        }

        _dbContext.FeedSources.AddRange(SeedFeedSources.All.Select(source => Clone(source)));
        _dbContext.SaveChanges();
    }

    private static FeedSource Clone(FeedSource source, DateTimeOffset? lastCrawledAt = null) =>
        new()
        {
            Id = source.Id,
            Name = source.Name,
            FeedUrl = source.FeedUrl,
            SiteUrl = source.SiteUrl,
            SourceType = source.SourceType,
            TopicScope = source.TopicScope,
            DefaultCandidateLimit = source.DefaultCandidateLimit,
            SourceQualityTier = source.SourceQualityTier,
            QualityNotes = source.QualityNotes,
            IsEnabled = source.IsEnabled,
            LastCrawledAt = lastCrawledAt ?? source.LastCrawledAt
        };
}
