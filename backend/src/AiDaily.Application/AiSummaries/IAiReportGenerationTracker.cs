namespace AiDaily.Application.AiSummaries;

public interface IAiReportGenerationTracker
{
    bool TryBegin(string articleId);
    void Complete(string articleId);
}
