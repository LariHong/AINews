using AiDaily.Application.Articles;
using AiDaily.Application.AiSummaries;
using AiDaily.Infrastructure.AI;
using AiDaily.Infrastructure.Cache;
using AiDaily.Infrastructure.FeedCrawler;
using AiDaily.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("frontend", policy =>
        policy.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin());
});
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSingleton<IArticleRepository, InMemoryArticleRepository>();
builder.Services.AddSingleton<IAiSummaryRepository, InMemoryAiSummaryRepository>();
builder.Services.AddSingleton<IAiSummaryReadCache, InMemoryAiSummaryReadCache>();
builder.Services.AddScoped<ArticleQueryService>();
builder.Services.AddScoped<AiSummaryQueryService>();
builder.Services.AddHttpClient<RssFeedCrawler>();

var app = builder.Build();

app.UseCors("frontend");
app.MapControllers();

app.Run();
