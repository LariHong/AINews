using AiDaily.Domain.Entities;

namespace AiDaily.Application.AiSummaries;

public interface IAiReportGenerator
{
    string ProviderName { get; }
    Task<AiReportDraft> GenerateAsync(Article article, CancellationToken cancellationToken);
}
