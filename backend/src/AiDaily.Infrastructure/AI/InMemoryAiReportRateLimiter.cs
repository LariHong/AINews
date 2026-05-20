using AiDaily.Application.AiSummaries;
using System.Collections.Concurrent;

namespace AiDaily.Infrastructure.AI;

public sealed class InMemoryAiReportRateLimiter : IAiReportRateLimiter
{
    private static readonly TimeSpan Window = TimeSpan.FromMinutes(15);
    private const int MaxAttemptsPerWindow = 1;

    private readonly ConcurrentDictionary<string, Queue<DateTimeOffset>> _attempts = new(StringComparer.Ordinal);
    private readonly TimeProvider _timeProvider;

    public InMemoryAiReportRateLimiter(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
    }

    public AiReportRateLimitResult TryAcquire(string userId, string articleId)
    {
        var normalizedUserId = string.IsNullOrWhiteSpace(userId) ? "anonymous" : userId.Trim();
        var key = $"{normalizedUserId}:{articleId}";
        var now = _timeProvider.GetUtcNow();
        var attempts = _attempts.GetOrAdd(key, _ => new Queue<DateTimeOffset>());

        lock (attempts)
        {
            while (attempts.Count > 0 && now - attempts.Peek() >= Window)
            {
                attempts.Dequeue();
            }

            if (attempts.Count >= MaxAttemptsPerWindow)
            {
                return AiReportRateLimitResult.Rejected(attempts.Peek().Add(Window));
            }

            attempts.Enqueue(now);
            return AiReportRateLimitResult.Allowed();
        }
    }
}
