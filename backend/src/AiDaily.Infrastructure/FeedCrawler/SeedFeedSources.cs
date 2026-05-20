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
            Id = "google-ai-blog",
            Name = "Google AI Blog",
            FeedUrl = "https://blog.google/technology/ai/rss/",
            SiteUrl = "https://blog.google/technology/ai/",
            SourceType = "rss",
            TopicScope = "ai-product-research",
            DefaultCandidateLimit = 20,
            SourceQualityTier = "core",
            QualityNotes = "Official Google AI topic feed; useful for Gemini, research, product, and developer ecosystem updates."
        },
        new FeedSource
        {
            Id = "huggingface-blog",
            Name = "Hugging Face Blog",
            FeedUrl = "https://huggingface.co/blog/feed.xml",
            SiteUrl = "https://huggingface.co/blog",
            SourceType = "rss",
            TopicScope = "ai-open-source-models",
            DefaultCandidateLimit = 20,
            SourceQualityTier = "standard",
            QualityNotes = "Official Hugging Face blog; useful for open-source models, datasets, evaluation, and deployment updates."
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
