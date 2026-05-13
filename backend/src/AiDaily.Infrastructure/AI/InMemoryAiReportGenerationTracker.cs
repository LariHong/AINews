using AiDaily.Application.AiSummaries;
using System.Collections.Concurrent;

namespace AiDaily.Infrastructure.AI;

public sealed class InMemoryAiReportGenerationTracker : IAiReportGenerationTracker
{
    private readonly ConcurrentDictionary<string, byte> _active = new(StringComparer.Ordinal);

    public bool TryBegin(string articleId) => _active.TryAdd(articleId, 0);

    public void Complete(string articleId) => _active.TryRemove(articleId, out _);
}
