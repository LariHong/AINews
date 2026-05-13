using AiDaily.Application.Articles;
using AiDaily.Domain.Entities;
using System.Runtime.CompilerServices;

namespace AiDaily.Application.AiSummaries;

public sealed class AiReportGenerationService
{
    private readonly IArticleRepository _articles;
    private readonly IAiReportRepository _reports;
    private readonly IAiReportGenerator _generator;
    private readonly IAiReportGenerationTracker _tracker;

    public AiReportGenerationService(
        IArticleRepository articles,
        IAiReportRepository reports,
        IAiReportGenerator generator,
        IAiReportGenerationTracker tracker)
    {
        _articles = articles;
        _reports = reports;
        _generator = generator;
        _tracker = tracker;
    }

    public async Task<AiReportGenerationStartResult> StartAsync(
        string articleId,
        bool force,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(articleId))
        {
            return AiReportGenerationStartResult.ArticleNotFound();
        }

        var article = await _articles.GetByIdAsync(articleId, cancellationToken);
        if (article is null)
        {
            return AiReportGenerationStartResult.ArticleNotFound();
        }

        var existing = await _reports.GetByArticleIdAsync(articleId, cancellationToken);
        if (existing is not null && !force)
        {
            return AiReportGenerationStartResult.Ready(StreamExistingAsync(existing, cancellationToken));
        }

        if (!_tracker.TryBegin(articleId))
        {
            return AiReportGenerationStartResult.InProgress();
        }

        return AiReportGenerationStartResult.Ready(GenerateAsync(article, cancellationToken));
    }

    private async IAsyncEnumerable<AiReportStreamEvent> StreamExistingAsync(
        AiReport report,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        yield return AiReportStreamEvent.Started("cached");
        await Task.Yield();
        cancellationToken.ThrowIfCancellationRequested();
        yield return AiReportStreamEvent.ReportReady(AiReportQueryService.ToDto(report));
        yield return AiReportStreamEvent.Completed(report.ArticleId);
    }

    private async IAsyncEnumerable<AiReportStreamEvent> GenerateAsync(
        Article article,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        try
        {
            yield return AiReportStreamEvent.Started(_generator.ProviderName);
            yield return AiReportStreamEvent.Status("Analyzing article context.");

            AiReportDraft? draft = null;
            AiReportStreamEvent? providerError = null;

            try
            {
                draft = await _generator.GenerateAsync(article, cancellationToken);
            }
            catch (InvalidOperationException exception) when (
                exception.Message is "AI_PROVIDER_NOT_CONFIGURED"
                    or "AI_REPORT_INVALID_FORMAT"
                    or "AI_PROVIDER_AUTH_FAILED"
                    or "AI_PROVIDER_RATE_LIMITED"
                    or "AI_PROVIDER_MODEL_UNAVAILABLE"
                    or "AI_PROVIDER_REQUEST_FAILED")
            {
                providerError = AiReportStreamEvent.Error(exception.Message, ToProviderErrorMessage(exception.Message));
            }
            catch (Exception)
            {
                providerError = AiReportStreamEvent.Error(
                    "AI_PROVIDER_REQUEST_FAILED",
                    "AI provider request failed. Check backend logs and provider configuration.");
            }

            if (providerError is not null || draft is null)
            {
                yield return providerError ?? AiReportStreamEvent.Error("AI_REPORT_INVALID_FORMAT", "AI provider returned no report.");
                yield break;
            }

            yield return AiReportStreamEvent.Status("Validating structured report.");
            draft = AiReportDraftNormalizer.Normalize(draft, article);

            if (!AiReportValidation.TryValidate(draft, out var validationError))
            {
                yield return AiReportStreamEvent.Error("AI_REPORT_INVALID_FORMAT", validationError);
                yield break;
            }

            var report = new AiReport
            {
                Id = $"rep_{Guid.NewGuid():N}"[..16],
                ArticleId = article.Id,
                Tldr = draft.Tldr,
                KeyPoints = draft.KeyPoints.ToList(),
                Pros = draft.Pros.ToList(),
                Cons = draft.Cons.ToList(),
                Timeline = draft.Timeline.Select(item => new AiReportTimelineItem(item.Label, item.Description)).ToList(),
                Scores = new AiReportScores(draft.Scores.Impact, draft.Scores.Confidence, draft.Scores.Controversy),
                RelatedTags = draft.RelatedTags.ToList(),
                EditorNote = draft.EditorNote,
                Rating = draft.Rating,
                Provider = _generator.ProviderName,
                GeneratedAt = DateTimeOffset.UtcNow
            };

            await _reports.SaveAsync(report, cancellationToken);

            yield return AiReportStreamEvent.ReportReady(AiReportQueryService.ToDto(report));
            yield return AiReportStreamEvent.Completed(article.Id);
        }
        finally
        {
            _tracker.Complete(article.Id);
        }
    }

    private static string ToProviderErrorMessage(string code) =>
        code switch
        {
            "AI_PROVIDER_NOT_CONFIGURED" => "AI provider API key is not configured.",
            "AI_REPORT_INVALID_FORMAT" => "AI provider returned a report that does not match the required schema.",
            "AI_PROVIDER_AUTH_FAILED" => "AI provider rejected the API key or project permissions.",
            "AI_PROVIDER_RATE_LIMITED" => "AI provider rate limit or quota was reached.",
            "AI_PROVIDER_MODEL_UNAVAILABLE" => "AI provider model is unavailable or the request shape was rejected.",
            _ => "AI provider request failed."
        };
}

public sealed record AiReportGenerationStartResult(
    AiReportGenerationStartStatus Status,
    IAsyncEnumerable<AiReportStreamEvent>? Stream)
{
    public static AiReportGenerationStartResult Ready(IAsyncEnumerable<AiReportStreamEvent> stream) =>
        new(AiReportGenerationStartStatus.Ready, stream);

    public static AiReportGenerationStartResult ArticleNotFound() =>
        new(AiReportGenerationStartStatus.ArticleNotFound, null);

    public static AiReportGenerationStartResult InProgress() =>
        new(AiReportGenerationStartStatus.InProgress, null);
}

public enum AiReportGenerationStartStatus
{
    Ready,
    ArticleNotFound,
    InProgress
}

public sealed record AiReportStreamEvent(string Type, string? Message, string? Code, AiReportDto? Report)
{
    public static AiReportStreamEvent Started(string provider) =>
        new("started", $"AI report generation started with {provider}.", null, null);

    public static AiReportStreamEvent Status(string message) =>
        new("status", message, null, null);

    public static AiReportStreamEvent ReportReady(AiReportDto report) =>
        new("report", null, null, report);

    public static AiReportStreamEvent Completed(string articleId) =>
        new("completed", $"AI report generation completed for {articleId}.", null, null);

    public static AiReportStreamEvent Error(string code, string message) =>
        new("error", message, code, null);
}
