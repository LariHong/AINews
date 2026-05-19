namespace AiDaily.Domain.Entities;

public sealed class Bookmark
{
    public required string UserId { get; init; }
    public required string ArticleId { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}
