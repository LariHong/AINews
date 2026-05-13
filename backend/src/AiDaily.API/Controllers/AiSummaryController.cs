using AiDaily.Application.AiSummaries;
using Microsoft.AspNetCore.Mvc;

namespace AiDaily.API.Controllers;

[ApiController]
[Route("api/v1/articles/{articleId}/ai-summary")]
[Produces("application/json")]
public sealed class AiSummaryController : ControllerBase
{
    private readonly AiSummaryQueryService _summaryQueryService;

    public AiSummaryController(AiSummaryQueryService summaryQueryService)
    {
        _summaryQueryService = summaryQueryService;
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
}
