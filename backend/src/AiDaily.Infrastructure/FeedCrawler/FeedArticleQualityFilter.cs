using AiDaily.Domain.Entities;

namespace AiDaily.Infrastructure.FeedCrawler;

internal static class FeedArticleQualityFilter
{
    private static readonly string[] AiKeywords =
    [
        "ai",
        "artificial intelligence",
        "agent",
        "agents",
        "benchmark",
        "deep learning",
        "generative",
        "llm",
        "machine learning",
        "model",
        "multimodal",
        "neural",
        "openai",
        "prompt",
        "reasoning",
        "robotics",
        "safety"
    ];

    private static readonly (string Keyword, string Reason)[] RejectionKeywords =
    [
        ("apply now", "job_posting"),
        ("career", "job_posting"),
        ("hiring", "job_posting"),
        ("job", "job_posting"),
        ("register", "event_only"),
        ("tickets", "event_only"),
        ("webinar", "event_only"),
        ("sponsor", "sponsor"),
        ("sponsored", "sponsor"),
        ("unsubscribe", "newsletter_housekeeping"),
        ("release notes", "release_note_index"),
        ("changelog", "release_note_index"),
        ("index", "index_page")
    ];

    public static FeedArticleQualityResult Evaluate(
        FeedSource source,
        string title,
        string? summary,
        string sourceUrl)
    {
        var haystack = $"{title} {summary} {source.Name} {source.TopicScope}".ToLowerInvariant();
        var contentLength = $"{title} {summary}".Trim().Length;
        var matchedKeywords = AiKeywords
            .Where(keyword => haystack.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (contentLength < 32)
        {
            return FeedArticleQualityResult.Rejected("too_short", matchedKeywords);
        }

        var rejection = RejectionKeywords
            .FirstOrDefault(item => haystack.Contains(item.Keyword, StringComparison.OrdinalIgnoreCase));
        if (rejection.Reason is not null)
        {
            return FeedArticleQualityResult.Rejected(rejection.Reason, matchedKeywords);
        }

        if (matchedKeywords.Count == 0)
        {
            return FeedArticleQualityResult.Rejected("not_ai_related", matchedKeywords);
        }

        var tierBonus = source.SourceQualityTier switch
        {
            "core" => 20,
            "watch" => 10,
            _ => 5
        };
        var score = 50 + tierBonus + Math.Min(matchedKeywords.Count * 8, 30);

        if (Uri.TryCreate(sourceUrl, UriKind.Absolute, out var uri) &&
            uri.AbsolutePath.Contains("tag", StringComparison.OrdinalIgnoreCase))
        {
            score += 5;
        }

        return FeedArticleQualityResult.Accepted(Math.Min(score, 100), matchedKeywords);
    }
}

internal sealed record FeedArticleQualityResult(
    int Score,
    string? RejectionReason,
    IReadOnlyList<string> MatchedKeywords)
{
    public static FeedArticleQualityResult Accepted(int score, IReadOnlyList<string> matchedKeywords) =>
        new(score, null, matchedKeywords);

    public static FeedArticleQualityResult Rejected(string reason, IReadOnlyList<string> matchedKeywords) =>
        new(0, reason, matchedKeywords);
}
