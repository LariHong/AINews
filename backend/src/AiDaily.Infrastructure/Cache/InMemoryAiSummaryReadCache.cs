using AiDaily.Application.AiSummaries;
using System.Collections.Concurrent;

namespace AiDaily.Infrastructure.Cache;

public sealed class InMemoryAiSummaryReadCache : IAiSummaryReadCache
{
    private readonly ConcurrentDictionary<string, AiSummaryDto> _summaries = new(StringComparer.Ordinal);

    public bool TryGet(string articleId, out AiSummaryDto? summary) =>
        _summaries.TryGetValue(articleId, out summary);

    public void Set(string articleId, AiSummaryDto summary) =>
        _summaries[articleId] = summary;

    public void Remove(string articleId) =>
        _summaries.TryRemove(articleId, out _);
}
