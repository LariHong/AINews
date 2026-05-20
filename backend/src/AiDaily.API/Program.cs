using AiDaily.Application.Articles;
using AiDaily.Application.AiSummaries;
using AiDaily.Application.Bookmarks;
using AiDaily.Application.FeedCrawler;
using AiDaily.Application.Stats;
using AiDaily.Application.UserPreferences;
using AiDaily.Infrastructure.AI;
using AiDaily.API.Middleware;
using AiDaily.Infrastructure.Cache;
using AiDaily.Infrastructure.ContentExtraction;
using AiDaily.Infrastructure.FeedCrawler;
using AiDaily.Infrastructure.Persistence;
using AiDaily.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
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
var articleRepositoryMode = builder.Configuration["Persistence:ArticleRepository"] ?? "InMemory";
if (UsesDatabaseArticleRepository(articleRepositoryMode))
{
    var connectionString = builder.Configuration.GetConnectionString("AiDaily")
        ?? "Host=localhost;Port=5432;Database=ai_daily;Username=postgres;Password=postgres";

    builder.Services.AddDbContext<AiDailyDbContext>(options => options.UseNpgsql(connectionString));
    builder.Services.AddScoped<IArticleRepository, EfCoreArticleRepository>();
    builder.Services.AddScoped<EfCoreFeedSourceCatalog>();
    builder.Services.AddScoped<IFeedSourceCatalog>(serviceProvider =>
        serviceProvider.GetRequiredService<EfCoreFeedSourceCatalog>());
    builder.Services.AddScoped<IFeedSourceMetadataRepository>(serviceProvider =>
        serviceProvider.GetRequiredService<EfCoreFeedSourceCatalog>());
}
else
{
    builder.Services.AddSingleton<IArticleRepository, InMemoryArticleRepository>();
    builder.Services.AddSingleton<IFeedSourceCatalog, SeedFeedSourceCatalog>();
}

builder.Services.AddSingleton<IBookmarkRepository, InMemoryBookmarkRepository>();
builder.Services.AddSingleton<IHiddenArticleRepository, InMemoryHiddenArticleRepository>();
builder.Services.AddSingleton<FeedCrawlRunState>();
builder.Services.AddSingleton<IFeedCrawlStatusReader>(serviceProvider =>
    serviceProvider.GetRequiredService<FeedCrawlRunState>());
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<IAiSummaryRepository, InMemoryAiSummaryRepository>();
builder.Services.AddSingleton<IAiSummaryReadCache, InMemoryAiSummaryReadCache>();
builder.Services.AddScoped<IAiSummaryGenerator, StubAiSummaryGenerator>();
builder.Services.AddSingleton<IAiReportRepository, InMemoryAiReportRepository>();
builder.Services.AddSingleton<IAiReportGenerationTracker, InMemoryAiReportGenerationTracker>();
builder.Services.AddSingleton<IAiReportRateLimiter, InMemoryAiReportRateLimiter>();
builder.Services.AddScoped<ArticleQueryService>();
builder.Services.AddScoped<BookmarkService>();
builder.Services.AddScoped<UserPreferenceService>();
builder.Services.AddScoped<FeedCrawlRunService>();
builder.Services.AddScoped<DashboardStatsQueryService>();
builder.Services.AddScoped<AiSummaryQueryService>();
builder.Services.AddScoped<AiSummaryGenerationService>();
builder.Services.AddScoped<AiReportQueryService>();
builder.Services.AddScoped<AiReportGenerationService>();
builder.Services.AddHttpClient<IFeedCrawler, RssFeedCrawler>();
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

if (UsesDatabaseArticleRepository(articleRepositoryMode))
{
    await AiDailyDatabaseInitializer.InitializeAsync(app.Services);
}

app.UseCors("frontend");
app.UseMiddleware<ApiExceptionHandlingMiddleware>();
app.MapControllers();

app.Run();

static string? NormalizeApiKey(string? apiKey) =>
    string.IsNullOrWhiteSpace(apiKey) || apiKey == "請交換APIKEY"
        ? null
        : apiKey;

static bool UsesDatabaseArticleRepository(string mode) =>
    string.Equals(mode, "Db", StringComparison.OrdinalIgnoreCase) ||
    string.Equals(mode, "Database", StringComparison.OrdinalIgnoreCase) ||
    string.Equals(mode, "Postgres", StringComparison.OrdinalIgnoreCase) ||
    string.Equals(mode, "PostgreSQL", StringComparison.OrdinalIgnoreCase);
