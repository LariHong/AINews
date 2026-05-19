using AiDaily.Application.Articles;
using Microsoft.AspNetCore.Mvc;

namespace AiDaily.API.Controllers;

[ApiController]
[Route("api/v1/articles")]
[Produces("application/json")]
public sealed class ArticlesController : ControllerBase
{
    private const string LocalUserHeader = "X-AI-Daily-Local-User";
    private readonly ArticleQueryService _articleQueryService;

    public ArticlesController(ArticleQueryService articleQueryService)
    {
        _articleQueryService = articleQueryService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<ArticleListResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<ArticleListResponse>>> GetArticles(
        [FromQuery] string? cursor,
        [FromQuery] int limit = 20,
        [FromQuery] string? keyword = null,
        [FromQuery] string? tags = null,
        [FromQuery] string? source = null,
        [FromQuery] DateOnly? date = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _articleQueryService.GetArticlesAsync(
            new ArticleListParams(cursor, limit, keyword, tags, source, date),
            GetLocalUserId(),
            cancellationToken);

        var response = new ArticleListResponse(
            result.Items,
            new PaginationResponse(result.Cursor, result.HasMore, result.TotalCount));

        return Ok(ApiResponse<ArticleListResponse>.Ok(response));
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<ArticleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ArticleDto>>> GetArticle(
        [FromRoute] string id,
        CancellationToken cancellationToken = default)
    {
        var article = await _articleQueryService.GetArticleAsync(id, GetLocalUserId(), cancellationToken);
        if (article is null)
        {
            return NotFound(ApiErrorResponse.Fail(
                "ARTICLE_NOT_FOUND",
                $"Article '{id}' was not found."));
        }

        return Ok(ApiResponse<ArticleDto>.Ok(article));
    }

    private string GetLocalUserId() =>
        Request.Headers.TryGetValue(LocalUserHeader, out var value)
            ? value.ToString()
            : string.Empty;
}

public sealed record ArticleListResponse(
    IReadOnlyList<ArticleDto> Items,
    PaginationResponse Pagination);

public sealed record PaginationResponse(string? Cursor, bool HasMore, int TotalCount);
