namespace AiDaily.Application.Articles;

public sealed record ArticleListParams(
    string? Cursor,
    int Limit,
    string? Keyword,
    string? Tags,
    string? Source,
    DateOnly? Date)
{
    public int SafeLimit => Math.Clamp(Limit <= 0 ? 20 : Limit, 1, 50);

    public IReadOnlySet<string> ParsedTags =>
        string.IsNullOrWhiteSpace(Tags)
            ? new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            : Tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
}
