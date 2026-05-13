using AiDaily.Domain.Entities;

namespace AiDaily.Application.AiSummaries;

public static class AiReportDraftNormalizer
{
    private static readonly HashSet<string> ValidRatings = new(StringComparer.OrdinalIgnoreCase)
    {
        "low-impact",
        "medium-impact",
        "high-impact",
        "watchlist"
    };

    public static AiReportDraft Normalize(AiReportDraft draft, Article article)
    {
        var primaryTag = article.Tags.FirstOrDefault() ?? "AI";
        var tldr = FirstText(draft.Tldr)
            ?? $"{article.Title} is relevant to {primaryTag} readers and should be verified against the source.";

        var keyPoints = Bounded(
            draft.KeyPoints,
            5,
            article.Summary ?? "The source did not provide an imported summary.",
            $"{article.SourceName} is the source of record for this article.",
            $"Follow-up should verify claims against {article.SourceUrl}.");

        var pros = Bounded(
            draft.Pros,
            4,
            "Provides a structured first pass for readers.",
            "Helps compare this story with related AI news.");

        var cons = Bounded(
            draft.Cons,
            4,
            "The generated analysis depends on the imported article data.",
            "Source claims should be verified before operational decisions.");

        var timeline = draft.Timeline
            .Where(item => !string.IsNullOrWhiteSpace(item.Label) && !string.IsNullOrWhiteSpace(item.Description))
            .Take(5)
            .ToList();

        if (timeline.Count == 0)
        {
            timeline.Add(new AiReportTimelineItemDto(
                article.PublishedAt.ToString("yyyy-MM-dd"),
                $"{article.SourceName} published the story."));
        }

        var relatedTags = Bounded(draft.RelatedTags, 8, article.Tags.DefaultIfEmpty(primaryTag).ToArray());
        var editorNote = FirstText(draft.EditorNote)
            ?? "Use this report as a first-pass reading aid, then confirm important details in the source.";
        var rating = ValidRatings.Contains(draft.Rating) ? draft.Rating.ToLowerInvariant() : "watchlist";

        return new AiReportDraft(
            tldr,
            keyPoints,
            pros,
            cons,
            timeline,
            new AiReportScoresDto(
                Clamp(draft.Scores.Impact),
                Clamp(draft.Scores.Confidence),
                Clamp(draft.Scores.Controversy)),
            relatedTags,
            editorNote,
            rating);
    }

    private static IReadOnlyList<string> Bounded(IReadOnlyList<string> values, int max, params string[] fallback)
    {
        var cleaned = values
            .Select(FirstText)
            .Where(value => value is not null)
            .Select(value => value!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(max)
            .ToList();

        foreach (var item in fallback)
        {
            if (cleaned.Count > 0) break;
            var text = FirstText(item);
            if (text is not null) cleaned.Add(text);
        }

        return cleaned;
    }

    private static string? FirstText(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static int Clamp(int value) => Math.Clamp(value, 0, 100);
}
