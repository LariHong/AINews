using AiDaily.Application.Articles;
using AiDaily.Domain.Entities;

namespace AiDaily.Infrastructure.Repositories;

public sealed class InMemoryArticleRepository : IArticleRepository
{
    private readonly List<Article> _articles = SeedArticles.ToList();

    private static readonly IReadOnlyList<Article> SeedArticles =
    [
        new Article
        {
            Id = "art_01JAI001",
            Title = "OpenAI releases new agent tooling for developers",
            Summary = "The update focuses on safer tool use, better tracing, and easier production debugging.",
            SourceUrl = "https://openai.com/news/",
            SourceId = "openai-news",
            SourceName = "OpenAI News",
            Tags = ["model", "product"],
            PublishedAt = DateTimeOffset.Parse("2026-05-13T06:00:00Z"),
            HasAiSummary = true,
            IsBookmarked = false,
            ReadTimeMinutes = 5
        },
        new Article
        {
            Id = "art_01JAI002",
            Title = "Hugging Face publishes compact multimodal benchmark",
            Summary = "The benchmark compares small models across vision, retrieval, and instruction following.",
            SourceUrl = "https://huggingface.co/blog",
            SourceId = "huggingface-blog",
            SourceName = "Hugging Face Blog",
            Tags = ["research", "model"],
            PublishedAt = DateTimeOffset.Parse("2026-05-13T04:30:00Z"),
            HasAiSummary = false,
            IsBookmarked = false,
            ReadTimeMinutes = 7
        },
        new Article
        {
            Id = "art_01JAI003",
            Title = "MIT researchers outline AI safety evaluation gaps",
            Summary = "The report argues that deployment monitoring should be measured alongside pre-release benchmarks.",
            SourceUrl = "https://www.technologyreview.com/topic/artificial-intelligence/",
            SourceId = "mit-tech-review-ai",
            SourceName = "MIT Tech Review AI",
            Tags = ["safety", "research"],
            PublishedAt = DateTimeOffset.Parse("2026-05-11T20:15:00Z"),
            HasAiSummary = true,
            IsBookmarked = true,
            ReadTimeMinutes = 6
        },
        new Article
        {
            Id = "art_01JAI004",
            Title = "Google DeepMind shares robotics policy learning results",
            Summary = "The team reports improved transfer from simulated tasks to real-world manipulation.",
            SourceUrl = "https://deepmind.google/discover/blog/",
            SourceId = "deepmind-blog",
            SourceName = "DeepMind Blog",
            Tags = ["research", "agent"],
            PublishedAt = DateTimeOffset.Parse("2026-05-11T15:45:00Z"),
            HasAiSummary = false,
            IsBookmarked = false,
            ReadTimeMinutes = 8
        }
    ];

    public Task<IReadOnlyList<Article>> ListAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Article>>(_articles.ToList());

    public Task<Article?> GetByIdAsync(string id, CancellationToken cancellationToken) =>
        Task.FromResult(_articles.FirstOrDefault(article => article.Id == id));

    public Task UpsertAsync(Article article, CancellationToken cancellationToken)
    {
        var existingIndex = _articles.FindIndex(item => item.SourceUrl == article.SourceUrl || item.Id == article.Id);
        if (existingIndex >= 0)
        {
            _articles[existingIndex] = article;
        }
        else
        {
            _articles.Add(article);
        }

        return Task.CompletedTask;
    }
}
