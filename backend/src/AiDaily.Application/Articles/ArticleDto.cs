namespace AiDaily.Application.Articles;

public sealed record ArticleDto(
    string Id,
    string Title,
    string? Summary,
    string SourceUrl,
    string SourceName,
    string? SourceLogoUrl,
    IReadOnlyList<string> Tags,
    DateTimeOffset PublishedAt,
    bool HasAiSummary,
    bool IsBookmarked,
    short? ReadTimeMinutes);
