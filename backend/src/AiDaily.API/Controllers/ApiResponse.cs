namespace AiDaily.API.Controllers;

public sealed record ApiResponse<T>(bool Success, T Data, ResponseMeta Meta)
{
    public static ApiResponse<T> Ok(T data) =>
        new(true, data, new ResponseMeta(DateTimeOffset.UtcNow, CreateRequestId()));

    private static string CreateRequestId() =>
        $"req_{Guid.NewGuid():N}"[..16];
}

public sealed record ResponseMeta(DateTimeOffset Timestamp, string RequestId);

public sealed record ApiErrorResponse(bool Success, ApiError Error, ResponseMeta Meta)
{
    public static ApiErrorResponse Fail(string code, string message) =>
        new(false, new ApiError(code, message), new ResponseMeta(DateTimeOffset.UtcNow, CreateRequestId()));

    private static string CreateRequestId() =>
        $"req_{Guid.NewGuid():N}"[..16];
}

public sealed record ApiError(string Code, string Message);
