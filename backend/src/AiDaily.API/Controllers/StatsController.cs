using AiDaily.Application.Stats;
using Microsoft.AspNetCore.Mvc;

namespace AiDaily.API.Controllers;

[ApiController]
[Route("api/v1/stats")]
[Produces("application/json")]
public sealed class StatsController : ControllerBase
{
    private readonly DashboardStatsQueryService _dashboardStats;

    public StatsController(DashboardStatsQueryService dashboardStats)
    {
        _dashboardStats = dashboardStats;
    }

    [HttpGet("today")]
    [ProducesResponseType(typeof(ApiResponse<DashboardStatsDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<DashboardStatsDto>>> GetToday(CancellationToken cancellationToken)
    {
        var stats = await _dashboardStats.GetTodayAsync(cancellationToken);
        return Ok(ApiResponse<DashboardStatsDto>.Ok(stats));
    }
}
