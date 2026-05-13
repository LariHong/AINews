using AiDaily.Application.Articles;
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
builder.Services.AddScoped<ArticleQueryService>();
builder.Services.AddHttpClient<RssFeedCrawler>();

var app = builder.Build();

app.UseCors("frontend");
app.MapControllers();

app.Run();
