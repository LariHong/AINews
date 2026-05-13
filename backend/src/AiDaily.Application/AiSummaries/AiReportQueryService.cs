using AiDaily.Application.Articles;
using AiDaily.Domain.Entities;

namespace AiDaily.Application.AiSummaries;

public sealed class AiReportQueryService
{
    private readonly IArticleRepository _articles;
    private readonly IAiReportRepository _reports;

    public AiReportQueryService(IArticleRepository articles, IAiReportRepository reports)
    {
        _articles = articles;
        _reports = reports;
    }

    public async Task<AiReportQueryResult> GetReportAsync(string articleId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(articleId))
        {
            return AiReportQueryResult.ArticleNotFound();
        }

        var article = await _articles.GetByIdAsync(articleId, cancellationToken);
        if (article is null)
        {
            return AiReportQueryResult.ArticleNotFound();
        }

        var report = await _reports.GetByArticleIdAsync(articleId, cancellationToken);
        return report is null ? AiReportQueryResult.ReportNotFound() : AiReportQueryResult.Found(ToDto(report));
    }

    internal static AiReportDto ToDto(AiReport report) =>
        new(
            report.ArticleId,
            report.Tldr,
            report.KeyPoints,
            report.Pros,
            report.Cons,
            report.Timeline.Select(item => new AiReportTimelineItemDto(item.Label, item.Description)).ToList(),
            new AiReportScoresDto(report.Scores.Impact, report.Scores.Confidence, report.Scores.Controversy),
            report.RelatedTags,
            report.EditorNote,
            report.Rating,
            report.Provider,
            report.GeneratedAt);
}

public sealed record AiReportQueryResult(AiReportQueryStatus Status, AiReportDto? Report)
{
    public static AiReportQueryResult Found(AiReportDto report) => new(AiReportQueryStatus.Found, report);
    public static AiReportQueryResult ArticleNotFound() => new(AiReportQueryStatus.ArticleNotFound, null);
    public static AiReportQueryResult ReportNotFound() => new(AiReportQueryStatus.ReportNotFound, null);
}

public enum AiReportQueryStatus
{
    Found,
    ArticleNotFound,
    ReportNotFound
}
