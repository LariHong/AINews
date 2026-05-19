using AiDaily.Application.Articles;
using AiDaily.Application.UserPreferences;
using Microsoft.AspNetCore.Mvc;

namespace AiDaily.API.Controllers;

[ApiController]
[Route("api/v1/user-preferences")]
[Produces("application/json")]
public sealed class UserPreferencesController : ControllerBase
{
    private const string LocalUserHeader = "X-AI-Daily-Local-User";
    private readonly UserPreferenceService _preferences;

    public UserPreferencesController(UserPreferenceService preferences)
    {
        _preferences = preferences;
    }

    [HttpGet("hidden-articles")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<ArticleDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ArticleDto>>>> GetHiddenArticles(
        CancellationToken cancellationToken = default)
    {
        var articles = await _preferences.ListHiddenArticlesAsync(GetLocalUserId(), cancellationToken);

        return Ok(ApiResponse<IReadOnlyList<ArticleDto>>.Ok(articles));
    }

    [HttpPost("hidden-articles/{id}")]
    [ProducesResponseType(typeof(ApiResponse<HiddenArticleResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<HiddenArticleResponse>>> HideArticle(
        [FromRoute] string id,
        [FromBody] HideArticleRequest? request,
        CancellationToken cancellationToken = default)
    {
        var result = await _preferences.HideArticleAsync(
            GetLocalUserId(),
            id,
            request?.Reason,
            cancellationToken);

        return ToActionResult(result, isHidden: true);
    }

    [HttpDelete("hidden-articles/{id}")]
    [ProducesResponseType(typeof(ApiResponse<HiddenArticleResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<HiddenArticleResponse>>> RestoreArticle(
        [FromRoute] string id,
        CancellationToken cancellationToken = default)
    {
        var result = await _preferences.RestoreArticleAsync(GetLocalUserId(), id, cancellationToken);
        return ToActionResult(result, isHidden: false);
    }

    private string GetLocalUserId() =>
        Request.Headers.TryGetValue(LocalUserHeader, out var value)
            ? value.ToString()
            : string.Empty;

    private ActionResult<ApiResponse<HiddenArticleResponse>> ToActionResult(
        HiddenArticleMutationResult result,
        bool isHidden)
    {
        return result.Status switch
        {
            HiddenArticleMutationStatus.Ready => Ok(ApiResponse<HiddenArticleResponse>.Ok(
                new HiddenArticleResponse(result.ArticleId ?? string.Empty, isHidden))),
            HiddenArticleMutationStatus.ArticleNotFound => NotFound(ApiErrorResponse.Fail(
                "ARTICLE_NOT_FOUND",
                $"Article '{result.ArticleId}' was not found.")),
            _ => BadRequest(ApiErrorResponse.Fail(
                "LOCAL_USER_REQUIRED",
                "A temporary local user id is required for hidden article preferences."))
        };
    }
}

public sealed record HideArticleRequest(string? Reason);

public sealed record HiddenArticleResponse(string ArticleId, bool IsHidden);
