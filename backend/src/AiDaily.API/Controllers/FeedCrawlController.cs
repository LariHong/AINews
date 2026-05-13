using AiDaily.Application.FeedCrawler;
using Microsoft.AspNetCore.Mvc;

namespace AiDaily.API.Controllers;

[ApiController]
[Route("api/v1/feed-crawl")]
[Produces("application/json")]
public sealed class FeedCrawlController : ControllerBase
{
    private readonly FeedCrawlRunService _feedCrawlRunService;

    public FeedCrawlController(FeedCrawlRunService feedCrawlRunService)
    {
        _feedCrawlRunService = feedCrawlRunService;
    }

    [HttpPost("run")]
    [ProducesResponseType(typeof(ApiResponse<FeedCrawlRunDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<FeedCrawlRunDto>>> Run(
        [FromQuery] string scope = "today",
        CancellationToken cancellationToken = default)
    {
        var result = await _feedCrawlRunService.RunAsync(
            new FeedCrawlRunRequest(scope),
            cancellationToken);

        return Ok(ApiResponse<FeedCrawlRunDto>.Ok(result));
    }
}
