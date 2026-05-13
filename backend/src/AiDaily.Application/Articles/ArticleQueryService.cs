using AiDaily.Domain.Entities;

namespace AiDaily.Application.Articles;

public sealed class ArticleQueryService
{
    private readonly IArticleRepository _articles;

    public ArticleQueryService(IArticleRepository articles)
    {
        _articles = articles;
    }

    public async Task<PaginatedResult<ArticleDto>> GetArticlesAsync(
        ArticleListParams parameters,
        CancellationToken cancellationToken = default)
    {
        var allArticles = await _articles.ListAsync(cancellationToken);
        var filtered = ApplyFilters(allArticles, parameters)
            .OrderByDescending(article => article.PublishedAt)
            .ThenBy(article => article.Id)
            .ToList();

        var totalCount = filtered.Count;
        var startIndex = DecodeCursor(parameters.Cursor);
        if (startIndex > filtered.Count)
        {
            startIndex = filtered.Count;
        }

        var page = filtered
            .Skip(startIndex)
            .Take(parameters.SafeLimit)
            .Select(ToDto)
            .ToList();

        var nextIndex = startIndex + page.Count;
        var hasMore = nextIndex < totalCount;

        return new PaginatedResult<ArticleDto>(
            page,
            hasMore ? EncodeCursor(nextIndex) : null,
            hasMore,
            totalCount);
    }

    public async Task<ArticleDto?> GetArticleAsync(string id, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        var article = await _articles.GetByIdAsync(id, cancellationToken);
        return article is null ? null : ToDto(article);
    }

    private static IEnumerable<Article> ApplyFilters(
        IEnumerable<Article> source,
        ArticleListParams parameters)
    {
        var query = source;

        if (!string.IsNullOrWhiteSpace(parameters.Keyword))
        {
            query = query.Where(article =>
                Contains(article.Title, parameters.Keyword) ||
                Contains(article.Summary, parameters.Keyword));
        }

        if (parameters.ParsedTags.Count > 0)
        {
            query = query.Where(article =>
                article.Tags.Any(tag => parameters.ParsedTags.Contains(tag)));
        }

        if (!string.IsNullOrWhiteSpace(parameters.Source))
        {
            query = query.Where(article =>
                article.SourceName.Contains(parameters.Source, StringComparison.OrdinalIgnoreCase));
        }

        if (parameters.Date is not null)
        {
            query = query.Where(article =>
                DateOnly.FromDateTime(article.PublishedAt.UtcDateTime) == parameters.Date);
        }

        return query;
    }

    private static bool Contains(string? value, string keyword) =>
        value?.Contains(keyword, StringComparison.OrdinalIgnoreCase) == true;

    private static ArticleDto ToDto(Article article) =>
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
            article.IsBookmarked,
            article.ReadTimeMinutes);

    private static string EncodeCursor(int index) =>
        Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(index.ToString()));

    private static int DecodeCursor(string? cursor)
    {
        if (string.IsNullOrWhiteSpace(cursor))
        {
            return 0;
        }

        try
        {
            var decoded = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(cursor));
            return int.TryParse(decoded, out var index) && index >= 0 ? index : 0;
        }
        catch (FormatException)
        {
            return 0;
        }
    }
}
