using AiDaily.Domain.Entities;

namespace AiDaily.Infrastructure.FeedCrawler;

public static class SeedFeedSources
{
    public static IReadOnlyList<FeedSource> All { get; } =
    [
        new FeedSource
        {
            Id = "openai-news",
            Name = "OpenAI News",
            FeedUrl = "https://openai.com/news/rss.xml",
            SiteUrl = "https://openai.com/news/",
            SourceType = "rss",
            TopicScope = "ai-lab-product-research-policy",
            DefaultCandidateLimit = 20,
            SourceQualityTier = "core",
            QualityNotes = "Official OpenAI feed; prioritize product, research, safety, policy, and infrastructure updates."
        },
        new FeedSource
        {
            Id = "google-deepmind-blog",
            Name = "Google DeepMind Blog",
            FeedUrl = "https://deepmind.google/blog/rss.xml",
            SiteUrl = "https://deepmind.google/blog/",
            SourceType = "rss",
            TopicScope = "ai-research-lab",
            DefaultCandidateLimit = 20,
            SourceQualityTier = "core",
            QualityNotes = "Official Google DeepMind research and product updates; high signal for model, safety, and research coverage."
        },
        new FeedSource
        {
            Id = "microsoft-ai-blog",
            Name = "Microsoft AI Blog",
            FeedUrl = "https://blogs.microsoft.com/ai/feed/",
            SiteUrl = "https://blogs.microsoft.com/ai/",
            SourceType = "rss",
            TopicScope = "ai-product-policy-engineering",
            DefaultCandidateLimit = 20,
            SourceQualityTier = "core",
            QualityNotes = "Official Microsoft AI blog; useful for enterprise AI, Copilot, responsible AI, and product strategy updates."
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
            SourceQualityTier = "standard",
            QualityNotes = "High-quality engineering topic feed; keep as a non-official source with stricter threshold than official lab/product feeds."
        }
    ];
}
