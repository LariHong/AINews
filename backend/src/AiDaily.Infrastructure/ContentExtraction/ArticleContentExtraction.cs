using System.Net;
using System.Text.RegularExpressions;

namespace AiDaily.Infrastructure.ContentExtraction;

public interface IArticleContentExtractor
{
    Task<ArticleContentExtractionResult> ExtractAsync(
        string sourceUrl,
        string? fallbackSummary,
        CancellationToken cancellationToken);
}

public sealed record ArticleContentExtractionResult(
    string? Content,
    string? ContentText,
    string Status,
    DateTimeOffset? ExtractedAt)
{
    public static ArticleContentExtractionResult SummaryFallback(string? summary) =>
        new(summary, summary, "summary_fallback", null);

    public static ArticleContentExtractionResult ExtractionFailed(string? summary) =>
        new(summary, summary, "extraction_failed", DateTimeOffset.UtcNow);
}

public sealed class HtmlArticleContentExtractor : IArticleContentExtractor
{
    private const int MinimumFullContentCharacters = 320;
    private const int MinimumFullContentWords = 45;

    private static readonly Regex RemovedBlocks = new(
        @"<(script|style|nav|header|footer|aside|form|svg|noscript|button)\b[^>]*>.*?</\1>",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

    private static readonly Regex AttributeNoiseBlocks = new(
        @"<(div|section|aside)[^>]*(class|id)=[""'][^""']*(cookie|subscribe|related|sidebar|promo|newsletter|share)[^""']*[""'][^>]*>.*?</\1>",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

    private static readonly Regex PrimaryContentBlock = new(
        @"<(article|main)\b[^>]*>(.*?)</\1>",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

    private static readonly Regex Tags = new("<[^>]+>", RegexOptions.Compiled);
    private static readonly Regex Whitespace = new(@"\s+", RegexOptions.Compiled);

    private static readonly string[] NoisePhrases =
    [
        "accept cookies",
        "cookie settings",
        "privacy policy",
        "subscribe",
        "sign up",
        "related posts",
        "recommended stories",
        "share this",
        "advertisement"
    ];

    private readonly HttpClient _httpClient;

    public HtmlArticleContentExtractor(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<ArticleContentExtractionResult> ExtractAsync(
        string sourceUrl,
        string? fallbackSummary,
        CancellationToken cancellationToken)
    {
        if (!Uri.TryCreate(sourceUrl, UriKind.Absolute, out var uri))
        {
            return ArticleContentExtractionResult.SummaryFallback(fallbackSummary);
        }

        try
        {
            var html = await _httpClient.GetStringAsync(uri, cancellationToken);
            var cleanText = ExtractReadableText(html);

            if (!IsHighQualityFullContent(cleanText, fallbackSummary))
            {
                return ArticleContentExtractionResult.SummaryFallback(fallbackSummary);
            }

            return new ArticleContentExtractionResult(
                cleanText,
                cleanText,
                "full_content_ready",
                DateTimeOffset.UtcNow);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or InvalidOperationException)
        {
            return ArticleContentExtractionResult.ExtractionFailed(fallbackSummary);
        }
    }

    public static string ExtractReadableText(string html)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return string.Empty;
        }

        var primaryMatch = PrimaryContentBlock.Match(html);
        var sourceHtml = primaryMatch.Success ? primaryMatch.Groups[2].Value : html;
        var withoutBlocks = RemovedBlocks.Replace(sourceHtml, " ");
        withoutBlocks = AttributeNoiseBlocks.Replace(withoutBlocks, " ");
        var withBreaks = Regex.Replace(
            withoutBlocks,
            @"</?(p|br|div|section|article|li|h[1-6])\b[^>]*>",
            " ",
            RegexOptions.IgnoreCase);
        var withoutTags = Tags.Replace(withBreaks, " ");
        var decoded = WebUtility.HtmlDecode(withoutTags);

        return Whitespace.Replace(decoded, " ").Trim();
    }

    private static bool IsHighQualityFullContent(string cleanText, string? fallbackSummary)
    {
        if (string.IsNullOrWhiteSpace(cleanText))
        {
            return false;
        }

        var wordCount = cleanText.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
        if (cleanText.Length < MinimumFullContentCharacters || wordCount < MinimumFullContentWords)
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(fallbackSummary))
        {
            var normalizedText = NormalizeForComparison(cleanText);
            var normalizedFallback = NormalizeForComparison(fallbackSummary);
            if (normalizedText.Equals(normalizedFallback, StringComparison.OrdinalIgnoreCase) ||
                (normalizedText.Contains(normalizedFallback, StringComparison.OrdinalIgnoreCase) &&
                 cleanText.Length <= fallbackSummary.Length + 120))
            {
                return false;
            }
        }

        var noiseHits = NoisePhrases.Count(phrase =>
            cleanText.Contains(phrase, StringComparison.OrdinalIgnoreCase));
        return noiseHits < 2;
    }

    private static string NormalizeForComparison(string value) =>
        Whitespace.Replace(value.Trim(), " ");
}
