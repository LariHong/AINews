using AiDaily.Application.Articles;
using AiDaily.Domain.Entities;

namespace AiDaily.Application.AiSummaries;

public sealed class AiSummaryGenerationService
{
    private readonly IArticleRepository _articles;
    private readonly IAiSummaryRepository _summaries;
    private readonly IAiSummaryGenerator _generator;
    private readonly IAiSummaryReadCache _cache;

    public AiSummaryGenerationService(
        IArticleRepository articles,
        IAiSummaryRepository summaries,
        IAiSummaryGenerator generator,
        IAiSummaryReadCache cache)
    {
        _articles = articles;
        _summaries = summaries;
        _generator = generator;
        _cache = cache;
    }

    public async Task<AiSummaryGenerationResult> GenerateAsync(
        string articleId,
        bool force,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(articleId))
        {
            return AiSummaryGenerationResult.ArticleNotFound();
        }

        var article = await _articles.GetByIdAsync(articleId, cancellationToken);
        if (article is null)
        {
            return AiSummaryGenerationResult.ArticleNotFound();
        }

        var existing = await _summaries.GetByArticleIdAsync(articleId, cancellationToken);
        if (existing is not null && !force)
        {
            var existingDto = ToDto(existing);
            _cache.Set(articleId, existingDto);
            return AiSummaryGenerationResult.Ready(existingDto, WasGenerated: false);
        }

        _cache.Remove(articleId);

        var draft = await _generator.GenerateAsync(article, cancellationToken);
        var summary = new AiSummary
        {
            Id = $"sum_{Guid.NewGuid():N}"[..16],
            ArticleId = article.Id,
            Highlights = draft.Highlights.ToList(),
            ImpactScope = draft.ImpactScope,
            Controversy = draft.Controversy,
            EditorView = draft.EditorView,
            Provider = _generator.ProviderName,
            PromptVersion = _generator.PromptVersion,
            GeneratedAt = DateTimeOffset.UtcNow
        };

        await _summaries.SaveAsync(summary, cancellationToken);

        var dto = ToDto(summary);
        _cache.Set(articleId, dto);

        return AiSummaryGenerationResult.Ready(dto, WasGenerated: true);
    }

    private static AiSummaryDto ToDto(AiSummary summary) =>
        new(
            summary.ArticleId,
            summary.Highlights,
            summary.ImpactScope,
            summary.Controversy,
            summary.EditorView,
            summary.Provider,
            summary.PromptVersion,
            summary.GeneratedAt);
}

public sealed record AiSummaryGenerationResult(
    AiSummaryGenerationStatus Status,
    AiSummaryDto? Summary,
    bool WasGenerated)
{
    public static AiSummaryGenerationResult Ready(AiSummaryDto summary, bool WasGenerated) =>
        new(AiSummaryGenerationStatus.Ready, summary, WasGenerated);

    public static AiSummaryGenerationResult ArticleNotFound() =>
        new(AiSummaryGenerationStatus.ArticleNotFound, null, WasGenerated: false);
}

public enum AiSummaryGenerationStatus
{
    Ready,
    ArticleNotFound
}
