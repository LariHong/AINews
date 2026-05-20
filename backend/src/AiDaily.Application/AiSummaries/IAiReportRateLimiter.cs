namespace AiDaily.Application.AiSummaries;

public interface IAiReportRateLimiter
{
    AiReportRateLimitResult TryAcquire(string userId, string articleId);
}

public sealed record AiReportRateLimitResult(bool IsAllowed, DateTimeOffset? RetryAfter)
{
    public static AiReportRateLimitResult Allowed() => new(true, null);

    public static AiReportRateLimitResult Rejected(DateTimeOffset retryAfter) => new(false, retryAfter);
}
