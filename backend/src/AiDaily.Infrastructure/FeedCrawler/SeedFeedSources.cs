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
            SiteUrl = "https://openai.com/news/"
        },
        new FeedSource
        {
            Id = "huggingface-blog",
            Name = "Hugging Face Blog",
            FeedUrl = "https://huggingface.co/blog/feed.xml",
            SiteUrl = "https://huggingface.co/blog"
        }
    ];
}
