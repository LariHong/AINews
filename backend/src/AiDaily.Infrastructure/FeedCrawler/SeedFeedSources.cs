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
            SiteUrl = "https://venturebeat.com/category/ai/"
        },
        new FeedSource
        {
            Id = "infoq-ai-ml-news",
            Name = "InfoQ AI, ML & Data Engineering",
            FeedUrl = "https://feed.infoq.com/ai-ml-data-eng/news/",
            SiteUrl = "https://www.infoq.com/ai-ml-data-eng/"
        },
        new FeedSource
        {
            Id = "machine-brief",
            Name = "Machine Brief",
            FeedUrl = "https://www.machinebrief.com/rss.xml",
            SiteUrl = "https://www.machinebrief.com/"
        },
        new FeedSource
        {
            Id = "planet-ai",
            Name = "Planet AI",
            FeedUrl = "https://planet-ai.net/rss.xml",
            SiteUrl = "https://planet-ai.net/"
        }
    ];
}
