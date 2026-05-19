using AiDaily.Application.FeedCrawler;
using AiDaily.Domain.Entities;

namespace AiDaily.Infrastructure.FeedCrawler;

public sealed class SeedFeedSourceCatalog : IFeedSourceCatalog
{
    public IReadOnlyList<FeedSource> GetEnabledSources() =>
        SeedFeedSources.All
            .Where(source => source.IsEnabled)
            .Select(Clone)
            .ToList();

    private static FeedSource Clone(FeedSource source) =>
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
            LastCrawledAt = source.LastCrawledAt
        };
}
