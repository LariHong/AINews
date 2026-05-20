using AiDaily.Application.Articles;
using AiDaily.Domain.Entities;

namespace AiDaily.Application.AiSummaries;

public sealed class AiSummaryGenerationService
{
    private readonly IArticleRepository _articles;
    private readonly IAiSummaryRepository _summaries;
    private readonly IAiSummaryGenerator _generator;
    private readonly IAiSummaryGenerationTracker _tracker;
    private readonly IAiSummaryReadCache _cache;

    public AiSummaryGenerationService(
        IArticleRepository articles,
        IAiSummaryRepository summaries,
        IAiSummaryGenerator generator,
        IAiSummaryGenerationTracker tracker,
        IAiSummaryReadCache cache)
    {
        _articles = articles;
        _summaries = summaries;
        _generator = generator;
        _tracker = tracker;
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

        if (!_tracker.TryBegin(articleId))
        {
            return AiSummaryGenerationResult.InProgress();
        }

        try
        {
            var draft = await GenerateDraftAsync(article, cancellationToken);
            if (draft.ErrorCode is not null)
            {
                return AiSummaryGenerationResult.ProviderFailed(draft.ErrorCode, draft.ErrorMessage!);
            }

            var summary = new AiSummary
            {
                Id = $"sum_{Guid.NewGuid():N}"[..16],
                ArticleId = article.Id,
                Highlights = draft.Value!.Highlights.ToList(),
                ImpactScope = draft.Value.ImpactScope,
                Controversy = draft.Value.Controversy,
                EditorView = draft.Value.EditorView,
                Provider = _generator.ProviderName,
                PromptVersion = _generator.PromptVersion,
                GeneratedAt = DateTimeOffset.UtcNow
            };

            await _summaries.SaveAsync(summary, cancellationToken);

            var dto = ToDto(summary);
            _cache.Set(articleId, dto);

            return AiSummaryGenerationResult.Ready(dto, WasGenerated: true);
        }
        finally
        {
            _tracker.Complete(articleId);
        }
    }

    private async Task<AiSummaryDraftResult> GenerateDraftAsync(
        Article article,
        CancellationToken cancellationToken)
    {
        try
        {
            return AiSummaryDraftResult.Ready(await _generator.GenerateAsync(article, cancellationToken));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (InvalidOperationException exception) when (IsKnownProviderError(exception.Message))
        {
            return AiSummaryDraftResult.Failed(exception.Message, ToProviderErrorMessage(exception.Message));
        }
        catch (Exception)
        {
            return AiSummaryDraftResult.Failed(
                "AI_PROVIDER_REQUEST_FAILED",
                "AI summary provider request failed. Check backend logs and provider configuration.");
        }
    }

    private static bool IsKnownProviderError(string code) =>
        code is "AI_PROVIDER_NOT_CONFIGURED"
            or "AI_PROVIDER_AUTH_FAILED"
            or "AI_PROVIDER_RATE_LIMITED"
            or "AI_PROVIDER_MODEL_UNAVAILABLE"
            or "AI_PROVIDER_REQUEST_FAILED";

    private static string ToProviderErrorMessage(string code) =>
        code switch
        {
            "AI_PROVIDER_NOT_CONFIGURED" => "AI summary provider credentials are not configured.",
            "AI_PROVIDER_AUTH_FAILED" => "AI summary provider rejected the configured credentials or project permissions.",
            "AI_PROVIDER_RATE_LIMITED" => "AI summary provider rate limit or quota was reached.",
            "AI_PROVIDER_MODEL_UNAVAILABLE" => "AI summary provider model is unavailable or the request shape was rejected.",
            _ => "AI summary provider request failed."
        };

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
    bool WasGenerated,
    string? ErrorCode = null,
    string? ErrorMessage = null)
{
    public static AiSummaryGenerationResult Ready(AiSummaryDto summary, bool WasGenerated) =>
        new(AiSummaryGenerationStatus.Ready, summary, WasGenerated);

    public static AiSummaryGenerationResult ArticleNotFound() =>
        new(AiSummaryGenerationStatus.ArticleNotFound, null, WasGenerated: false);

    public static AiSummaryGenerationResult InProgress() =>
        new(AiSummaryGenerationStatus.InProgress, null, WasGenerated: false);

    public static AiSummaryGenerationResult ProviderFailed(string code, string message) =>
        new(AiSummaryGenerationStatus.ProviderFailed, null, WasGenerated: false, code, message);
}

public enum AiSummaryGenerationStatus
{
    Ready,
    ArticleNotFound,
    InProgress,
    ProviderFailed
}

internal sealed record AiSummaryDraftResult(AiSummaryDraft? Value, string? ErrorCode, string? ErrorMessage)
{
    public static AiSummaryDraftResult Ready(AiSummaryDraft draft) => new(draft, null, null);
    public static AiSummaryDraftResult Failed(string code, string message) => new(null, code, message);
}
