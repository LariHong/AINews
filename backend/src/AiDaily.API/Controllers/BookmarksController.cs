using AiDaily.Application.Articles;
using AiDaily.Application.Bookmarks;
using Microsoft.AspNetCore.Mvc;

namespace AiDaily.API.Controllers;

[ApiController]
[Route("api/v1")]
[Produces("application/json")]
public sealed class BookmarksController : ControllerBase
{
    private const string LocalUserHeader = "X-AI-Daily-Local-User";
    private readonly BookmarkService _bookmarkService;

    public BookmarksController(BookmarkService bookmarkService)
    {
        _bookmarkService = bookmarkService;
    }

    [HttpGet("bookmarks")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<ArticleDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ArticleDto>>>> GetBookmarks(
        CancellationToken cancellationToken = default)
    {
        var userId = GetLocalUserId();
        var articles = await _bookmarkService.ListAsync(userId, cancellationToken);

        return Ok(ApiResponse<IReadOnlyList<ArticleDto>>.Ok(articles));
    }

    [HttpPost("articles/{id}/bookmark")]
    [ProducesResponseType(typeof(ApiResponse<BookmarkResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<BookmarkResponse>>> AddBookmark(
        [FromRoute] string id,
        CancellationToken cancellationToken = default)
    {
        var result = await _bookmarkService.AddAsync(GetLocalUserId(), id, cancellationToken);
        return ToActionResult(result, true);
    }

    [HttpDelete("articles/{id}/bookmark")]
    [ProducesResponseType(typeof(ApiResponse<BookmarkResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<BookmarkResponse>>> DeleteBookmark(
        [FromRoute] string id,
        CancellationToken cancellationToken = default)
    {
        var result = await _bookmarkService.DeleteAsync(GetLocalUserId(), id, cancellationToken);
        return ToActionResult(result, false);
    }

    private string GetLocalUserId() =>
        Request.Headers.TryGetValue(LocalUserHeader, out var value)
            ? value.ToString()
            : string.Empty;

    private ActionResult<ApiResponse<BookmarkResponse>> ToActionResult(
        BookmarkMutationResult result,
        bool isBookmarked)
    {
        return result.Status switch
        {
            BookmarkMutationStatus.Ready => Ok(ApiResponse<BookmarkResponse>.Ok(
                new BookmarkResponse(result.ArticleId ?? string.Empty, isBookmarked))),
            BookmarkMutationStatus.ArticleNotFound => NotFound(ApiErrorResponse.Fail(
                "ARTICLE_NOT_FOUND",
                $"Article '{result.ArticleId}' was not found.")),
            _ => BadRequest(ApiErrorResponse.Fail(
                "LOCAL_USER_REQUIRED",
                "A temporary local user id is required for bookmark mutations."))
        };
    }
}

public sealed record BookmarkResponse(string ArticleId, bool IsBookmarked);
