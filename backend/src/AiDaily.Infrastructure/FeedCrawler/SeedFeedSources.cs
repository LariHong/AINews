using AiDaily.Domain.Entities;

namespace AiDaily.Infrastructure.FeedCrawler;

public static class SeedFeedSources
{
    public static IReadOnlyList<FeedSource> All { get; } =
    [
        new FeedSource
        {
            Id = "venturebeat-ai",
            Name = "VentureBeat AI",
            FeedUrl = "https://venturebeat.com/category/ai/feed/",
            SiteUrl = "https://venturebeat.com/category/ai/",
            SourceType = "rss",
            TopicScope = "ai-news",
            DefaultCandidateLimit = 30,
            SourceQualityTier = "core",
            QualityNotes = "AI category feed with product, startup, and enterprise AI news."
        },
        new FeedSource
        {
            Id = "infoq-ai-ml-news",
            Name = "InfoQ AI, ML & Data Engineering",
            FeedUrl = "https://feed.infoq.com/ai-ml-data-eng/news/",
            SiteUrl = "https://www.infoq.com/ai-ml-data-eng/",
            SourceType = "rss",
            TopicScope = "ai-ml-data-engineering",
            DefaultCandidateLimit = 25,
            SourceQualityTier = "core",
            QualityNotes = "Topic feed focused on AI, ML, and data engineering news."
        },
        new FeedSource
        {
            Id = "machine-brief",
            Name = "Machine Brief",
            FeedUrl = "https://www.machinebrief.com/rss.xml",
            SiteUrl = "https://www.machinebrief.com/",
            SourceType = "rss",
            TopicScope = "ai-newsletter",
            DefaultCandidateLimit = 20,
            SourceQualityTier = "watch",
            QualityNotes = "Newsletter-style source; filter housekeeping and sponsor posts aggressively."
        },
        new FeedSource
        {
            Id = "planet-ai",
            Name = "Planet AI",
            FeedUrl = "https://planet-ai.net/rss.xml",
            SiteUrl = "https://planet-ai.net/",
            SourceType = "rss",
            TopicScope = "ai-aggregation",
            DefaultCandidateLimit = 25,
            SourceQualityTier = "watch",
            QualityNotes = "Aggregator source; candidate depth helps recover useful AI posts after low-value items."
        }
    ];
}
