using AiDaily.Application.Articles;
using AiDaily.Domain.Entities;

namespace AiDaily.Application.AiSummaries;

public sealed class AiSummaryQueryService
{
    private readonly IArticleRepository _articles;
    private readonly IAiSummaryRepository _summaries;
    private readonly IAiSummaryReadCache _cache;

    public AiSummaryQueryService(
        IArticleRepository articles,
        IAiSummaryRepository summaries,
        IAiSummaryReadCache cache)
    {
        _articles = articles;
        _summaries = summaries;
        _cache = cache;
    }

    public async Task<AiSummaryQueryResult> GetPreviewAsync(
        string articleId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(articleId))
        {
            return AiSummaryQueryResult.ArticleNotFound();
        }

        if (_cache.TryGet(articleId, out var cached) && cached is not null)
        {
            return AiSummaryQueryResult.Found(cached);
        }

        var article = await _articles.GetByIdAsync(articleId, cancellationToken);
        if (article is null)
        {
            return AiSummaryQueryResult.ArticleNotFound();
        }

        var summary = await _summaries.GetByArticleIdAsync(articleId, cancellationToken);
        if (summary is null)
        {
            return AiSummaryQueryResult.SummaryNotFound();
        }

        var dto = ToDto(summary);
        _cache.Set(articleId, dto);

        return AiSummaryQueryResult.Found(dto);
    }

    private static AiSummaryDto ToDto(AiSummary summary) =>
        new(
            summary.ArticleId,
            summary.Highlights,
            summary.ImpactScope,
            summary.Controversy,
            summary.EditorView,
            summary.GeneratedAt);
}

public sealed record AiSummaryQueryResult(AiSummaryQueryStatus Status, AiSummaryDto? Summary)
{
    public static AiSummaryQueryResult Found(AiSummaryDto summary) =>
        new(AiSummaryQueryStatus.Found, summary);

    public static AiSummaryQueryResult ArticleNotFound() =>
        new(AiSummaryQueryStatus.ArticleNotFound, null);

    public static AiSummaryQueryResult SummaryNotFound() =>
        new(AiSummaryQueryStatus.SummaryNotFound, null);
}

public enum AiSummaryQueryStatus
{
    Found,
    ArticleNotFound,
    SummaryNotFound
}
