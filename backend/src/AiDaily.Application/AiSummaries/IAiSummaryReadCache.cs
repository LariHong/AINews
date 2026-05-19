namespace AiDaily.Application.AiSummaries;

public interface IAiSummaryReadCache
{
    bool TryGet(string articleId, out AiSummaryDto? summary);
    void Set(string articleId, AiSummaryDto summary);
    void Remove(string articleId);
}
