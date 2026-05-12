namespace AiDaily.Application.Articles;

public sealed record PaginatedResult<T>(
    IReadOnlyList<T> Items,
    string? Cursor,
    bool HasMore,
    int TotalCount);
