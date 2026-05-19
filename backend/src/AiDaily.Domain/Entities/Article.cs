namespace AiDaily.Domain.Entities;

public sealed class Article
{
    public required string Id { get; init; }
    public required string Title { get; init; }
    public string? Summary { get; init; }
    public string? Content { get; init; }
    public string? ContentText { get; init; }
    public string ContentStatus { get; init; } = "summary_fallback";
    public DateTimeOffset? ContentExtractedAt { get; init; }
    public required string SourceUrl { get; init; }
    public string? SourceId { get; init; }
    public required string SourceName { get; init; }
    public string? SourceLogoUrl { get; init; }
    public IReadOnlyList<string> Tags { get; init; } = Array.Empty<string>();
    public int IngestionScore { get; init; } = 0;
    public string? RejectionReason { get; init; }
    public IReadOnlyList<string> MatchedKeywords { get; init; } = Array.Empty<string>();
    public string SourceQualityTier { get; init; } = "standard";
    public DateTimeOffset PublishedAt { get; init; }
    public bool HasAiSummary { get; init; }
    public bool IsBookmarked { get; init; }
    public short? ReadTimeMinutes { get; init; }
}
