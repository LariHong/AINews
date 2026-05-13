using AiDaily.Application.Articles;
using AiDaily.Application.AiSummaries;
using AiDaily.Application.Stats;
using AiDaily.Infrastructure.AI;
using AiDaily.API.Middleware;
using AiDaily.Infrastructure.Cache;
using AiDaily.Infrastructure.ContentExtraction;
using AiDaily.Infrastructure.FeedCrawler;
using AiDaily.Infrastructure.Repositories;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

builder.Services.AddCors(options =>
{
    options.AddPolicy("frontend", policy =>
        policy.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin());
});
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.Configure<AiProviderOptions>(options =>
{
    builder.Configuration.GetSection("AiProvider").Bind(options);
    options.ApiKey = NormalizeApiKey(options.ApiKey ?? builder.Configuration["GEMINI_API_KEY"]);
});
builder.Services.AddSingleton(serviceProvider =>
    serviceProvider.GetRequiredService<IOptions<AiProviderOptions>>().Value);
builder.Services.AddSingleton<IArticleRepository, InMemoryArticleRepository>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<IAiSummaryRepository, InMemoryAiSummaryRepository>();
builder.Services.AddSingleton<IAiSummaryReadCache, InMemoryAiSummaryReadCache>();
builder.Services.AddSingleton<IAiReportRepository, InMemoryAiReportRepository>();
builder.Services.AddSingleton<IAiReportGenerationTracker, InMemoryAiReportGenerationTracker>();
builder.Services.AddScoped<ArticleQueryService>();
builder.Services.AddScoped<DashboardStatsQueryService>();
builder.Services.AddScoped<AiSummaryQueryService>();
builder.Services.AddScoped<AiReportQueryService>();
builder.Services.AddScoped<AiReportGenerationService>();
builder.Services.AddHttpClient<RssFeedCrawler>();
builder.Services.AddHttpClient<IArticleContentExtractor, HtmlArticleContentExtractor>();
builder.Services.AddHttpClient<GeminiAiReportGenerator>();
builder.Services.AddScoped<StubAiReportGenerator>();
builder.Services.AddScoped<IAiReportGenerator>(serviceProvider =>
{
    var options = serviceProvider.GetRequiredService<IOptions<AiProviderOptions>>().Value;
    return string.Equals(options.Mode, "Gemini", StringComparison.OrdinalIgnoreCase)
        ? serviceProvider.GetRequiredService<GeminiAiReportGenerator>()
        : serviceProvider.GetRequiredService<StubAiReportGenerator>();
});

var app = builder.Build();

app.UseCors("frontend");
app.UseMiddleware<ApiExceptionHandlingMiddleware>();
app.MapControllers();

app.Run();

static string? NormalizeApiKey(string? apiKey) =>
    string.IsNullOrWhiteSpace(apiKey) || apiKey == "請交換APIKEY"
        ? null
        : apiKey;
