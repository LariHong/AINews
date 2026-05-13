namespace AiDaily.Domain.Entities;

public sealed class FeedSource
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string FeedUrl { get; init; }
    public string? SiteUrl { get; init; }
    public bool IsEnabled { get; init; } = true;
    public DateTimeOffset? LastCrawledAt { get; set; }
}
