namespace AiDaily.Application.AiSummaries;

public static class AiReportValidation
{
    private static readonly HashSet<string> ValidRatings = new(StringComparer.OrdinalIgnoreCase)
    {
        "low-impact",
        "medium-impact",
        "high-impact",
        "watchlist"
    };

    public static bool TryValidate(AiReportDraft draft, out string error)
    {
        if (string.IsNullOrWhiteSpace(draft.Tldr))
        {
            error = "TL;DR is required.";
            return false;
        }

        if (!HasItems(draft.KeyPoints, 1, 5) || !HasItems(draft.Pros, 1, 4) || !HasItems(draft.Cons, 1, 4))
        {
            error = "Report lists must include a bounded number of non-empty items.";
            return false;
        }

        if (!HasItems(draft.RelatedTags, 1, 8))
        {
            error = "Related tags must include one to eight non-empty items.";
            return false;
        }

        if (draft.Timeline.Count > 5 || draft.Timeline.Any(item =>
                string.IsNullOrWhiteSpace(item.Label) || string.IsNullOrWhiteSpace(item.Description)))
        {
            error = "Timeline items must be complete and bounded.";
            return false;
        }

        if (!IsScore(draft.Scores.Impact) || !IsScore(draft.Scores.Confidence) || !IsScore(draft.Scores.Controversy))
        {
            error = "Scores must be between 0 and 100.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(draft.EditorNote) || !ValidRatings.Contains(draft.Rating))
        {
            error = "Editor note and a valid rating are required.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    private static bool HasItems(IReadOnlyList<string> items, int min, int max) =>
        items.Count >= min && items.Count <= max && items.All(item => !string.IsNullOrWhiteSpace(item));

    private static bool IsScore(int score) => score is >= 0 and <= 100;
}
