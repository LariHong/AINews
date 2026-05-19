namespace AiDaily.Domain.Entities;

public sealed class FeedSource
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string FeedUrl { get; init; }
    public string? SiteUrl { get; init; }
    public string SourceType { get; init; } = "rss";
    public string TopicScope { get; init; } = "ai";
    public int DefaultCandidateLimit { get; init; } = 25;
    public string SourceQualityTier { get; init; } = "standard";
    public string? QualityNotes { get; init; }
    public bool IsEnabled { get; init; } = true;
    public DateTimeOffset? LastCrawledAt { get; set; }
}
