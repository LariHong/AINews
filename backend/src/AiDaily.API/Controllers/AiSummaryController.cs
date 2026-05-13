using AiDaily.Application.AiSummaries;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace AiDaily.API.Controllers;

[ApiController]
[Route("api/v1/articles/{articleId}/ai-summary")]
[Produces("application/json")]
public sealed class AiSummaryController : ControllerBase
{
    private readonly AiSummaryQueryService _summaryQueryService;
    private readonly AiReportQueryService _reportQueryService;
    private readonly AiReportGenerationService _reportGenerationService;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public AiSummaryController(
        AiSummaryQueryService summaryQueryService,
        AiReportQueryService reportQueryService,
        AiReportGenerationService reportGenerationService)
    {
        _summaryQueryService = summaryQueryService;
        _reportQueryService = reportQueryService;
        _reportGenerationService = reportGenerationService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<AiSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<AiSummaryDto>>> GetAiSummary(
        [FromRoute] string articleId,
        CancellationToken cancellationToken = default)
    {
        var result = await _summaryQueryService.GetPreviewAsync(articleId, cancellationToken);

        return result.Status switch
        {
            AiSummaryQueryStatus.Found => Ok(ApiResponse<AiSummaryDto>.Ok(result.Summary!)),
            AiSummaryQueryStatus.ArticleNotFound => NotFound(ApiErrorResponse.Fail(
                "ARTICLE_NOT_FOUND",
                $"Article '{articleId}' was not found.")),
            _ => NotFound(ApiErrorResponse.Fail(
                "AI_SUMMARY_NOT_FOUND",
                $"AI summary for article '{articleId}' is not available yet."))
        };
    }

    [HttpGet("~/api/v1/articles/{articleId}/ai-report")]
    [ProducesResponseType(typeof(ApiResponse<AiReportDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<AiReportDto>>> GetAiReport(
        [FromRoute] string articleId,
        CancellationToken cancellationToken = default)
    {
        var result = await _reportQueryService.GetReportAsync(articleId, cancellationToken);

        return result.Status switch
        {
            AiReportQueryStatus.Found => Ok(ApiResponse<AiReportDto>.Ok(result.Report!)),
            AiReportQueryStatus.ArticleNotFound => NotFound(ApiErrorResponse.Fail(
                "ARTICLE_NOT_FOUND",
                $"Article '{articleId}' was not found.")),
            _ => NotFound(ApiErrorResponse.Fail(
                "AI_REPORT_NOT_FOUND",
                $"AI report for article '{articleId}' is not available yet."))
        };
    }

    [HttpPost("generate")]
    [Produces("text/event-stream")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> GenerateAiReport(
        [FromRoute] string articleId,
        [FromQuery] bool force = false,
        CancellationToken cancellationToken = default)
    {
        var result = await _reportGenerationService.StartAsync(articleId, force, cancellationToken);

        if (result.Status == AiReportGenerationStartStatus.ArticleNotFound)
        {
            return NotFound(ApiErrorResponse.Fail(
                "ARTICLE_NOT_FOUND",
                $"Article '{articleId}' was not found."));
        }

        if (result.Status == AiReportGenerationStartStatus.InProgress)
        {
            return Conflict(ApiErrorResponse.Fail(
                "AI_GENERATION_IN_PROGRESS",
                $"AI report generation for article '{articleId}' is already running."));
        }

        Response.Headers.CacheControl = "no-cache";
        Response.Headers.Connection = "keep-alive";
        Response.ContentType = "text/event-stream";

        await foreach (var streamEvent in result.Stream!.WithCancellation(cancellationToken))
        {
            await Response.WriteAsync($"event: {streamEvent.Type}\n", cancellationToken);
            await Response.WriteAsync($"data: {JsonSerializer.Serialize(streamEvent, JsonOptions)}\n\n", cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);
        }

        return new EmptyResult();
    }
}
