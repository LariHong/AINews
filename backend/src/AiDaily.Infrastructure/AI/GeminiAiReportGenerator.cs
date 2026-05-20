using AiDaily.Application.AiSummaries;
using AiDaily.Domain.Entities;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace AiDaily.Infrastructure.AI;

public sealed class GeminiAiReportGenerator : IAiReportGenerator
{
    public const int MaxPromptContentCharacters = 12000;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;
    private readonly AiProviderOptions _options;

    public GeminiAiReportGenerator(HttpClient httpClient, AiProviderOptions options)
    {
        _httpClient = httpClient;
        _options = options;
    }

    public string ProviderName => "gemini";

    public async Task<AiReportDraft> GenerateAsync(Article article, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            throw new InvalidOperationException("AI_PROVIDER_NOT_CONFIGURED");
        }

        var prompt = BuildPrompt(article);
        var endpoint = $"https://generativelanguage.googleapis.com/v1beta/models/{Uri.EscapeDataString(_options.Model)}:generateContent";
        var request = new GeminiRequest(
            [new GeminiContent([new GeminiPart(prompt)])],
            new GeminiGenerationConfig("application/json", 0.2));

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = JsonContent.Create(request, options: JsonOptions)
        };
        httpRequest.Headers.Add("x-goog-api-key", _options.ApiKey);

        using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(ToProviderErrorCode(response.StatusCode));
        }

        var payload = await response.Content.ReadFromJsonAsync<GeminiResponse>(JsonOptions, cancellationToken);
        var text = payload?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new InvalidOperationException("AI_REPORT_INVALID_FORMAT");
        }

        var draft = JsonSerializer.Deserialize<AiReportDraft>(text, JsonOptions);
        return draft ?? throw new InvalidOperationException("AI_REPORT_INVALID_FORMAT");
    }

    public static string BuildPrompt(Article article)
    {
        var contentText = article.ContentText ?? article.Summary ?? "No article content was imported.";
        var boundedContent = contentText.Length > MaxPromptContentCharacters
            ? contentText[..MaxPromptContentCharacters]
            : contentText;

        return
        $$"""
        You are generating a structured AI news report for a frontend UI.
        Use only the article data below. Do not invent dates, URLs, authors, companies, or claims that are not supported.
        Return valid JSON only. No markdown.

        Required JSON shape:
        {
          "tldr": "one concise sentence",
          "keyPoints": ["1-5 factual points"],
          "pros": ["1-4 upside items"],
          "cons": ["1-4 risk items"],
          "timeline": [{ "label": "date or source-backed label", "description": "source-backed description" }],
          "scores": { "impact": 0, "confidence": 0, "controversy": 0 },
          "relatedTags": ["1-8 tags"],
          "editorNote": "short editorial note",
          "rating": "low-impact | medium-impact | high-impact | watchlist"
        }

        Article:
        title: {{article.Title}}
        summary: {{article.Summary}}
        sourceName: {{article.SourceName}}
        sourceUrl: {{article.SourceUrl}}
        publishedAt: {{article.PublishedAt:O}}
        tags: {{string.Join(", ", article.Tags)}}
        contentStatus: {{article.ContentStatus}}
        contentText: {{boundedContent}}
        """;
    }

    private static string ToProviderErrorCode(HttpStatusCode statusCode) =>
        statusCode switch
        {
            HttpStatusCode.BadRequest => "AI_PROVIDER_MODEL_UNAVAILABLE",
            HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden => "AI_PROVIDER_AUTH_FAILED",
            (HttpStatusCode)429 => "AI_PROVIDER_RATE_LIMITED",
            _ => "AI_PROVIDER_REQUEST_FAILED"
        };

    private sealed record GeminiRequest(IReadOnlyList<GeminiContent> Contents, GeminiGenerationConfig GenerationConfig);
    private sealed record GeminiGenerationConfig(string ResponseMimeType, double Temperature);
    private sealed record GeminiContent(IReadOnlyList<GeminiPart> Parts);
    private sealed record GeminiPart(string Text);
    private sealed record GeminiResponse(IReadOnlyList<GeminiCandidate>? Candidates);
    private sealed record GeminiCandidate(GeminiContent? Content);
}
