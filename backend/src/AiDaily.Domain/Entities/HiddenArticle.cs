namespace AiDaily.Domain.Entities;

public sealed class HiddenArticle
{
    public required string UserId { get; init; }
    public required string ArticleId { get; init; }
    public string? Reason { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}
