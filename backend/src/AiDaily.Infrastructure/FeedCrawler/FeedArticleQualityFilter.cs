using AiDaily.Domain.Entities;

namespace AiDaily.Infrastructure.FeedCrawler;

internal static class FeedArticleQualityFilter
{
    private const int MinimumAcceptedScore = 70;

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
        var titleText = title.Trim();
        var summaryText = summary?.Trim() ?? string.Empty;
        var contentHaystack = $"{titleText} {summaryText}".ToLowerInvariant();
        var relevanceHaystack = $"{titleText} {summaryText} {sourceUrl}".ToLowerInvariant();
        var contentLength = $"{title} {summary}".Trim().Length;
        var matchedKeywords = AiKeywords
            .Where(keyword => ContainsKeyword(relevanceHaystack, keyword))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (contentLength < 48 || summaryText.Length < 24)
        {
            return FeedArticleQualityResult.Rejected("too_short", matchedKeywords);
        }

        var rejection = RejectionKeywords
            .FirstOrDefault(item => contentHaystack.Contains(item.Keyword, StringComparison.OrdinalIgnoreCase));
        if (rejection.Reason is not null)
        {
            return FeedArticleQualityResult.Rejected(rejection.Reason, matchedKeywords);
        }

        if (matchedKeywords.Count == 0)
        {
            return FeedArticleQualityResult.Rejected("not_ai_related", matchedKeywords);
        }

        var titleMatches = matchedKeywords.Count(keyword => ContainsKeyword(titleText, keyword));
        var summaryMatches = matchedKeywords.Count(keyword => ContainsKeyword(summaryText, keyword));
        var urlMatches = matchedKeywords.Count(keyword => ContainsKeyword(sourceUrl, keyword));
        var strongSignalMatches = matchedKeywords
            .Where(keyword => !keyword.Equals("ai", StringComparison.OrdinalIgnoreCase))
            .Count(keyword =>
                ContainsKeyword(titleText, keyword) ||
                ContainsKeyword(summaryText, keyword));
        var hasGenericAiOnlySignal = matchedKeywords.Count == 1 &&
            matchedKeywords[0].Equals("ai", StringComparison.OrdinalIgnoreCase);

        var tierBonus = source.SourceQualityTier switch
        {
            "core" => 15,
            "watch" => 8,
            _ => 0
        };
        var score = 45 +
            tierBonus +
            Math.Min(titleMatches * 12, 30) +
            Math.Min(summaryMatches * 8, 24) +
            Math.Min(urlMatches * 4, 8) +
            Math.Min(strongSignalMatches * 6, 18);

        if (hasGenericAiOnlySignal)
        {
            score -= 12;
        }

        if (Uri.TryCreate(sourceUrl, UriKind.Absolute, out var uri) &&
            uri.AbsolutePath.Contains("tag", StringComparison.OrdinalIgnoreCase))
        {
            score += 5;
        }

        score = Math.Min(score, 100);
        return score >= MinimumAcceptedScore
            ? FeedArticleQualityResult.Accepted(score, matchedKeywords)
            : FeedArticleQualityResult.Rejected("below_quality_threshold", matchedKeywords);
    }

    private static bool ContainsKeyword(string value, string keyword)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var index = value.IndexOf(keyword, StringComparison.OrdinalIgnoreCase);
        while (index >= 0)
        {
            var before = index == 0 ? '\0' : value[index - 1];
            var afterIndex = index + keyword.Length;
            var after = afterIndex >= value.Length ? '\0' : value[afterIndex];
            if (!IsKeywordCharacter(before) && !IsKeywordCharacter(after))
            {
                return true;
            }

            index = value.IndexOf(keyword, index + keyword.Length, StringComparison.OrdinalIgnoreCase);
        }

        return false;
    }

    private static bool IsKeywordCharacter(char value) =>
        char.IsAsciiLetterOrDigit(value);
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
