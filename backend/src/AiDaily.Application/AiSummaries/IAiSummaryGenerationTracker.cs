namespace AiDaily.Application.AiSummaries;

public interface IAiSummaryGenerationTracker
{
    bool TryBegin(string articleId);
    void Complete(string articleId);
}
