using AiDaily.Domain.Entities;
using AiDaily.Application.Bookmarks;

namespace AiDaily.Application.Articles;

public sealed class ArticleQueryService
{
    private readonly IArticleRepository _articles;
    private readonly IBookmarkRepository _bookmarks;

    public ArticleQueryService(IArticleRepository articles, IBookmarkRepository bookmarks)
    {
        _articles = articles;
        _bookmarks = bookmarks;
    }

    public async Task<PaginatedResult<ArticleDto>> GetArticlesAsync(
        ArticleListParams parameters,
        string userId,
        CancellationToken cancellationToken = default)
    {
        var allArticles = await _articles.ListAsync(cancellationToken);
        var bookmarkIds = await GetBookmarkIdsAsync(userId, cancellationToken);
        var filtered = ApplyFilters(allArticles, parameters)
            .Where(article => string.IsNullOrWhiteSpace(article.RejectionReason))
            .OrderByDescending(article => article.PublishedAt)
            .ThenByDescending(article => article.IngestionScore)
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
            .Select(article => ArticleDto.FromArticle(article, bookmarkIds.Contains(article.Id)))
            .ToList();

        var nextIndex = startIndex + page.Count;
        var hasMore = nextIndex < totalCount;

        return new PaginatedResult<ArticleDto>(
            page,
            hasMore ? EncodeCursor(nextIndex) : null,
            hasMore,
            totalCount);
    }

    public async Task<ArticleDto?> GetArticleAsync(
        string id,
        string userId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        var article = await _articles.GetByIdAsync(id, cancellationToken);
        if (article is null)
        {
            return null;
        }

        var bookmarkIds = await GetBookmarkIdsAsync(userId, cancellationToken);
        return ArticleDto.FromArticle(article, bookmarkIds.Contains(article.Id));
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

    private async Task<IReadOnlySet<string>> GetBookmarkIdsAsync(
        string userId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return new HashSet<string>();
        }

        return await _bookmarks.ListArticleIdsAsync(userId, cancellationToken);
    }

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
