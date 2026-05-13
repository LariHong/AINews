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
    private static readonly Regex RemovedBlocks = new(
        @"<(script|style|nav|header|footer|aside|form|svg|noscript)\b[^>]*>.*?</\1>",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

    private static readonly Regex Tags = new("<[^>]+>", RegexOptions.Compiled);
    private static readonly Regex Whitespace = new(@"\s+", RegexOptions.Compiled);

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

            if (string.IsNullOrWhiteSpace(cleanText))
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

        var withoutBlocks = RemovedBlocks.Replace(html, " ");
        var withBreaks = Regex.Replace(
            withoutBlocks,
            @"</?(p|br|div|section|article|li|h[1-6])\b[^>]*>",
            " ",
            RegexOptions.IgnoreCase);
        var withoutTags = Tags.Replace(withBreaks, " ");
        var decoded = WebUtility.HtmlDecode(withoutTags);

        return Whitespace.Replace(decoded, " ").Trim();
    }
}
