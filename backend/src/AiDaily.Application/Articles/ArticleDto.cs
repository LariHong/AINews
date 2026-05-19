using AiDaily.Domain.Entities;

namespace AiDaily.Application.Articles;

public sealed record ArticleDto(
    string Id,
    string Title,
    string? Summary,
    string? Content,
    string? ContentText,
    string ContentStatus,
    DateTimeOffset? ContentExtractedAt,
    string SourceUrl,
    string SourceName,
    string? SourceLogoUrl,
    IReadOnlyList<string> Tags,
    DateTimeOffset PublishedAt,
    bool HasAiSummary,
    bool IsBookmarked,
    short? ReadTimeMinutes)
{
    public static ArticleDto FromArticle(Article article, bool isBookmarked) =>
        new(
            article.Id,
            article.Title,
            article.Summary,
            article.Content,
            article.ContentText,
            article.ContentStatus,
            article.ContentExtractedAt,
            article.SourceUrl,
            article.SourceName,
            article.SourceLogoUrl,
            article.Tags,
            article.PublishedAt,
            article.HasAiSummary,
            isBookmarked,
            article.ReadTimeMinutes);
}
