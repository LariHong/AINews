using AiDaily.API.Controllers;
using AiDaily.Application.AiSummaries;
using AiDaily.Application.Articles;
using AiDaily.Application.Bookmarks;
using AiDaily.Application.FeedCrawler;
using AiDaily.Application.Stats;
using AiDaily.Application.UserPreferences;
using AiDaily.Domain.Entities;
using AiDaily.Infrastructure.AI;
using AiDaily.Infrastructure.Cache;
using AiDaily.Infrastructure.ContentExtraction;
using AiDaily.Infrastructure.FeedCrawler;
using AiDaily.Infrastructure.Persistence;
using AiDaily.Infrastructure.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Text.Json;

var repository = new InMemoryArticleRepository();
var summaryRepository = new InMemoryAiSummaryRepository();
var bookmarkRepository = new InMemoryBookmarkRepository();
var hiddenArticleRepository = new InMemoryHiddenArticleRepository();
var service = new ArticleQueryService(repository, summaryRepository, bookmarkRepository, hiddenArticleRepository);

await ShouldFilterByKeyword();
await ShouldFilterByTagAndPaginate();
await ShouldGetTodayDashboardStats();
await ShouldKeepArticleListQueriesReadOnly();
await ShouldRunExplicitFeedSync();
await ShouldGetArticleById();
await ShouldReturnNullForMissingArticle();
await ShouldSanitizeExtractedArticleContent();
await ShouldFallbackForLowQualityHtmlContent();
await ShouldUseSummaryFallbackWhenSourceBlocksExtraction();
await ShouldFallbackWhenContentExtractionFails();
await ShouldCrawlRssIntoArticles();
await ShouldPersistArticlesAcrossDbContextRestart();
await ShouldPersistFeedMetadataAcrossDbContextRestart();
await ShouldScanBeyondFirstTenLowValueCandidates();
await ShouldRejectCandidateWhenOnlySourceMetadataContainsAi();
await ShouldAcceptCoreSourceShortNamedProductTitle();
await ShouldRejectWeakWatchSourceCandidate();
await ShouldExcludeRejectedArticlesFromArticleList();
await ShouldUseQualityBeforeRecencyWithinArticleListDay();
await ShouldGetAiSummaryPreview();
await ShouldReturnSummaryNotFound();
await ShouldCacheAiSummaryPreview();
await ShouldGenerateAiSummaryOnceWhenMissing();
await ShouldReuseExistingAiSummaryWithoutProviderCall();
await ShouldForceRegenerateAiSummaryAndRefreshCache();
await ShouldKeepOldAiSummaryCacheWhenForceRegenerationFails();
await ShouldPreventConcurrentAiSummaryGeneration();
await ShouldProjectGeneratedSummaryAvailabilityIntoArticlesAndStats();
await ShouldGenerateAndReadAiReport();
await ShouldRateLimitAiReportPerUserAndArticle();
await ShouldWriteVersionedMvpSseContract();
await ShouldReturnDocumentedRateLimitError();
await ShouldBoundGeminiReportPromptContent();
await ShouldMarkFallbackReportPromptAsNotFullContent();
await ShouldRejectInvalidAiReportDraft();
await ShouldNormalizeSparseAiReportDraft();
await ShouldBookmarkArticleForLocalUser();
await ShouldKeepBookmarksScopedToLocalUser();
await ShouldHideArticleForLocalUser();
await ShouldRestoreHiddenArticle();

Console.WriteLine("AiDaily.UnitTests passed");

async Task ShouldFilterByKeyword()
{
    var result = await service.GetArticlesAsync(new ArticleListParams(null, 20, "safety", null, null, null), "local_test");

    Assert(result.TotalCount == 1, "keyword filter should return one safety article");
    Assert(result.Items[0].SourceName == "MIT Tech Review AI", "keyword filter should return MIT article");
}

async Task ShouldFilterByTagAndPaginate()
{
    var firstPage = await service.GetArticlesAsync(new ArticleListParams(null, 1, null, "research", null, null), "local_test");
    var secondPage = await service.GetArticlesAsync(new ArticleListParams(firstPage.Cursor, 1, null, "research", null, null), "local_test");

    Assert(firstPage.Items.Count == 1, "first page should contain one item");
    Assert(firstPage.HasMore, "first page should report more items");
    Assert(secondPage.Items.Count == 1, "second page should contain one item");
    Assert(firstPage.Items[0].Id != secondPage.Items[0].Id, "cursor should advance to a different article");
}

async Task ShouldGetTodayDashboardStats()
{
    var statsService = new DashboardStatsQueryService(
        repository,
        summaryRepository,
        new FeedCrawlRunState(),
        new FixedTimeProvider(DateTimeOffset.Parse("2026-05-13T10:00:00Z")));

    var stats = await statsService.GetTodayAsync();

    Assert(stats.TotalArticles == 2, "today stats should count only today's articles");
    Assert(stats.AiSummarizedCount == 1, "today stats should count AI summarized articles");
    Assert(stats.TagBreakdown.Any(item => item.Name == "model" && item.Count == 2), "today stats should include tag breakdown");
    Assert(stats.TopSources.Count == 2, "today stats should include top sources");
    Assert(stats.SyncStatus.SourcesSynced == 2, "today stats should include source sync count");
}

async Task ShouldKeepArticleListQueriesReadOnly()
{
    var crawler = new CountingFeedCrawler();
    var queryService = new ArticleQueryService(repository, summaryRepository, bookmarkRepository, hiddenArticleRepository);

    _ = await queryService.GetArticlesAsync(new ArticleListParams(null, 20, null, null, null, null), "local_test");

    Assert(crawler.Calls == 0, "article list queries should not trigger RSS crawler writes");
}

async Task ShouldRunExplicitFeedSync()
{
    var crawler = new CountingFeedCrawler();
    var state = new FeedCrawlRunState();
    var syncService = new FeedCrawlRunService(
        crawler,
        new StaticFeedSourceCatalog(),
        state,
        new FixedTimeProvider(DateTimeOffset.Parse("2026-05-13T10:00:00Z")));

    var result = await syncService.RunAsync(new FeedCrawlRunRequest("today"));

    Assert(crawler.Calls == 1, "explicit feed sync endpoint service should invoke the crawler");
    Assert(result.Status == "completed", "explicit feed sync should complete");
    Assert(state.Current.SourcesSynced == 1, "explicit feed sync should update source sync status");
}

async Task ShouldGetArticleById()
{
    var article = await service.GetArticleAsync("art_01JAI001", "local_test");

    Assert(article is not null, "detail query should return an article");
    Assert(article!.SourceUrl == "https://openai.com/news/", "detail query should return the requested article");
    Assert(article.ContentStatus == "full_content_ready", "detail query should include content enrichment status");
    Assert(article.ContentText?.Contains("safer tool use") == true, "detail query should include readable content text");
}

async Task ShouldReturnNullForMissingArticle()
{
    var article = await service.GetArticleAsync("missing", "local_test");

    Assert(article is null, "detail query should return null for missing article");
}

async Task ShouldSanitizeExtractedArticleContent()
{
    const string html = """
        <html>
          <head><style>.hidden{display:none}</style><script>alert('xss')</script></head>
          <body>
            <nav>Home Pricing</nav>
            <article>
              <div class="cookie-banner">Accept cookies before reading this site.</div>
              <h1>Readable AI article</h1>
              <p>OpenAI researchers published a detailed model safety benchmark for evaluating agent behavior in production-like tool workflows.</p>
              <p>The article explains how teams compare LLM reasoning, tool-use reliability, and safety outcomes across repeated evaluation runs.</p>
              <p>Engineering leaders can use the clean source text to decide whether the benchmark is relevant for deployment monitoring and release gates.</p>
              <p>Product teams also get concrete context about model evaluation tradeoffs, limitations, and follow-up work needed before operational rollout.</p>
              <aside>Related posts and share buttons should not appear in extracted text.</aside>
            </article>
          </body>
        </html>
        """;

    using var httpClient = new HttpClient(new StaticRssHandler(html));
    var extractor = new HtmlArticleContentExtractor(httpClient);
    var result = await extractor.ExtractAsync("https://example.com/story", "Fallback summary", CancellationToken.None);

    Assert(result.Status == "full_content_ready", "extractor should mark readable HTML as full content");
    Assert(result.ContentText?.Contains("Readable AI article") == true, "extractor should preserve readable text");
    Assert(result.ContentText?.Contains("production-like tool workflows") == true, "extractor should preserve main article text");
    Assert(result.ContentText?.Contains("alert") == false, "extractor should remove script content");
    Assert(result.ContentText?.Contains("Home Pricing") == false, "extractor should remove navigation content");
    Assert(result.ContentText?.Contains("Accept cookies") == false, "extractor should remove cookie banner content");
    Assert(result.ContentText?.Contains("Related posts") == false, "extractor should remove sidebar content");
}

async Task ShouldFallbackForLowQualityHtmlContent()
{
    const string html = """
        <html>
          <body>
            <nav>Home Pricing Docs</nav>
            <article>
              <h1>AI update</h1>
              <p>Fallback summary</p>
              <div class="related-posts">Related posts and recommended stories.</div>
              <div class="cookie-banner">Accept cookies and subscribe to continue.</div>
            </article>
          </body>
        </html>
        """;

    using var httpClient = new HttpClient(new StaticRssHandler(html));
    var extractor = new HtmlArticleContentExtractor(httpClient);
    var result = await extractor.ExtractAsync("https://example.com/short-story", "Fallback summary", CancellationToken.None);

    Assert(result.Status == "summary_fallback", "low-quality HTML should not be marked as full content");
    Assert(result.ContentText == "Fallback summary", "low-quality HTML should preserve the RSS summary fallback");
}

async Task ShouldUseSummaryFallbackWhenSourceBlocksExtraction()
{
    using var httpClient = new HttpClient(new StatusCodeHandler(HttpStatusCode.Forbidden));
    var extractor = new HtmlArticleContentExtractor(httpClient);
    var result = await extractor.ExtractAsync("https://example.com/blocked", "Fallback summary", CancellationToken.None);

    Assert(result.Status == "summary_fallback", "blocked source pages should use summary fallback instead of surfacing extraction failure");
    Assert(result.ContentText == "Fallback summary", "blocked source fallback should preserve the RSS summary");
}

async Task ShouldFallbackWhenContentExtractionFails()
{
    using var httpClient = new HttpClient(new ThrowingHandler());
    var extractor = new HtmlArticleContentExtractor(httpClient);
    var result = await extractor.ExtractAsync("https://example.com/fails", "Fallback summary", CancellationToken.None);

    Assert(result.Status == "extraction_failed", "extractor should mark failed fetches without throwing");
    Assert(result.ContentText == "Fallback summary", "extractor should preserve summary fallback on failure");
}

async Task ShouldCrawlRssIntoArticles()
{
    const string rss = """
        <rss version="2.0">
          <channel>
            <item>
              <title>OpenAI model safety benchmark expands agent evaluation</title>
              <link>https://example.com/ai-feed-story</link>
              <description>Researchers compare LLM reasoning and safety behavior across production agent workflows.</description>
              <pubDate>Tue, 12 May 2026 06:30:00 GMT</pubDate>
            </item>
          </channel>
        </rss>
        """;

    using var httpClient = new HttpClient(new StaticRssHandler(rss));
    var crawler = new RssFeedCrawler(httpClient, repository);
    var result = await crawler.CrawlAsync([SeedFeedSources.All[0]]);
    var articles = await repository.ListAsync(CancellationToken.None);
    var imported = articles.FirstOrDefault(article => article.SourceUrl == "https://example.com/ai-feed-story");

    Assert(result.SourcesVisited == 1, "crawler should visit one configured source");
    Assert(result.ArticlesPersisted == 1, "crawler should persist one RSS item");
    Assert(imported is not null, "crawler should persist article by stable source URL id");
    Assert(imported!.SourceUrl == "https://example.com/ai-feed-story", "crawler should preserve source_url");
    Assert(imported.IngestionScore > 0, "crawler should save ingestion score metadata");
    Assert(imported.MatchedKeywords.Count > 0, "crawler should save matched keyword metadata");
    Assert(imported.SourceQualityTier.Length > 0, "crawler should save source quality tier metadata");
}

async Task ShouldPersistArticlesAcrossDbContextRestart()
{
    await using var connection = new SqliteConnection("DataSource=:memory:");
    await connection.OpenAsync();
    var options = new DbContextOptionsBuilder<AiDailyDbContext>()
        .UseSqlite(connection)
        .Options;

    await using (var dbContext = new AiDailyDbContext(options))
    {
        await dbContext.Database.EnsureCreatedAsync();
        var dbRepository = new EfCoreArticleRepository(dbContext);

        await dbRepository.UpsertAsync(new Article
        {
            Id = "rss_persist_1",
            Title = "OpenAI model persistence baseline",
            Summary = "RSS sync should survive API restarts.",
            Content = "Full content",
            ContentText = "Full content text",
            ContentStatus = "full_content_ready",
            ContentExtractedAt = DateTimeOffset.Parse("2026-05-13T08:00:00Z"),
            SourceUrl = "https://example.com/persisted-ai-story",
            SourceId = "example-feed",
            SourceName = "Example Feed",
            Tags = ["openai", "model"],
            IngestionScore = 91,
            MatchedKeywords = ["openai", "model"],
            SourceQualityTier = "core",
            PublishedAt = DateTimeOffset.Parse("2026-05-13T07:30:00Z"),
            HasAiSummary = false,
            IsBookmarked = false,
            ReadTimeMinutes = 4
        }, CancellationToken.None);

        await dbRepository.UpsertAsync(new Article
        {
            Id = "rss_persist_2",
            Title = "Updated OpenAI model persistence baseline",
            Summary = "The same source URL should update instead of duplicating.",
            Content = "Updated content",
            ContentText = "Updated content text",
            ContentStatus = "summary_fallback",
            ContentExtractedAt = DateTimeOffset.Parse("2026-05-13T08:05:00Z"),
            SourceUrl = "https://example.com/persisted-ai-story",
            SourceId = "example-feed",
            SourceName = "Example Feed",
            Tags = ["openai"],
            IngestionScore = 82,
            MatchedKeywords = ["openai"],
            SourceQualityTier = "core",
            PublishedAt = DateTimeOffset.Parse("2026-05-13T07:30:00Z"),
            HasAiSummary = false,
            IsBookmarked = false,
            ReadTimeMinutes = 3
        }, CancellationToken.None);
    }

    await using (var restartedContext = new AiDailyDbContext(options))
    {
        var sourceUrlIndex = restartedContext.Model.FindEntityType(typeof(Article))
            ?.GetIndexes()
            .FirstOrDefault(index => index.Properties.Any(property => property.Name == nameof(Article.SourceUrl)));
        var restartedRepository = new EfCoreArticleRepository(restartedContext);
        var articles = await restartedRepository.ListAsync(CancellationToken.None);
        var article = articles.Single(item => item.SourceUrl == "https://example.com/persisted-ai-story");

        Assert(sourceUrlIndex?.IsUnique == true, "Article.SourceUrl should have a unique EF index");
        Assert(articles.Count(item => item.SourceUrl == "https://example.com/persisted-ai-story") == 1, "DB article repository should upsert by source URL");
        Assert(article.Id == "rss_persist_2", "DB article repository should keep the latest article identity for a source URL");
        Assert(article.ContentStatus == "summary_fallback", "DB article repository should persist content status");
        Assert(article.ContentText == "Updated content text", "DB article repository should persist content text");
        Assert(article.ContentExtractedAt is not null, "DB article repository should persist content extraction timestamp");
        Assert(article.IngestionScore == 82, "DB article repository should persist ingestion score");
        Assert(article.MatchedKeywords.SequenceEqual(["openai"]), "DB article repository should persist matched keywords");
        Assert(article.SourceQualityTier == "core", "DB article repository should persist source quality tier");
    }
}

async Task ShouldPersistFeedMetadataAcrossDbContextRestart()
{
    await using var connection = new SqliteConnection("DataSource=:memory:");
    await connection.OpenAsync();
    var options = new DbContextOptionsBuilder<AiDailyDbContext>()
        .UseSqlite(connection)
        .Options;

    var crawledAt = DateTimeOffset.Parse("2026-05-13T09:00:00Z");
    await using (var dbContext = new AiDailyDbContext(options))
    {
        await dbContext.Database.EnsureCreatedAsync();
        var catalog = new EfCoreFeedSourceCatalog(dbContext);
        await catalog.SaveAsync(new FeedSource
        {
            Id = "persisted-feed",
            Name = "Persisted Feed",
            FeedUrl = "https://example.com/rss.xml",
            SiteUrl = "https://example.com",
            SourceType = "rss",
            TopicScope = "ai-news",
            DefaultCandidateLimit = 15,
            SourceQualityTier = "core",
            QualityNotes = "Persistence test source.",
            IsEnabled = true,
            LastCrawledAt = crawledAt
        }, CancellationToken.None);
    }

    await using (var restartedContext = new AiDailyDbContext(options))
    {
        var feedUrlIndex = restartedContext.Model.FindEntityType(typeof(FeedSource))
            ?.GetIndexes()
            .FirstOrDefault(index => index.Properties.Any(property => property.Name == nameof(FeedSource.FeedUrl)));
        var restartedCatalog = new EfCoreFeedSourceCatalog(restartedContext);
        var sources = restartedCatalog.GetEnabledSources();
        var source = sources.Single(item => item.Id == "persisted-feed");

        Assert(feedUrlIndex?.IsUnique == true, "FeedSource.FeedUrl should have a unique EF index");
        Assert(source.FeedUrl == "https://example.com/rss.xml", "DB feed catalog should persist feed URL metadata");
        Assert(source.LastCrawledAt == crawledAt, "DB feed catalog should persist last crawled timestamp");
        Assert(source.SourceQualityTier == "core", "DB feed catalog should persist source quality tier");
    }
}

async Task ShouldScanBeyondFirstTenLowValueCandidates()
{
    const string rss = """
        <rss version="2.0">
          <channel>
            <item><title>Hiring backend engineer for platform team</title><link>https://example.com/job-01</link><description>Apply now for a full time role.</description></item>
            <item><title>Hiring product manager for developer tools</title><link>https://example.com/job-02</link><description>Apply now for a full time role.</description></item>
            <item><title>Register for vendor webinar next week</title><link>https://example.com/event-03</link><description>Tickets and sponsor details for attendees.</description></item>
            <item><title>Weekly newsletter housekeeping update</title><link>https://example.com/newsletter-04</link><description>Unsubscribe and preference center links.</description></item>
            <item><title>Release notes index for website changes</title><link>https://example.com/index-05</link><description>A changelog index without article analysis.</description></item>
            <item><title>Hiring data analyst for operations team</title><link>https://example.com/job-06</link><description>Apply now for a full time role.</description></item>
            <item><title>Register for sponsor meetup</title><link>https://example.com/event-07</link><description>Tickets and sponsor details for attendees.</description></item>
            <item><title>Weekly newsletter housekeeping reminder</title><link>https://example.com/newsletter-08</link><description>Unsubscribe and preference center links.</description></item>
            <item><title>Release notes index archive</title><link>https://example.com/index-09</link><description>A changelog index without article analysis.</description></item>
            <item><title>Hiring customer success lead</title><link>https://example.com/job-10</link><description>Apply now for a full time role.</description></item>
            <item>
              <title>OpenAI model safety benchmark improves agent evaluations</title>
              <link>https://example.com/ai-benchmark-11</link>
              <description>Researchers compare LLM agent behavior with new safety and reasoning tests.</description>
              <pubDate>Tue, 12 May 2026 07:30:00 GMT</pubDate>
            </item>
          </channel>
        </rss>
        """;

    var localRepository = new InMemoryArticleRepository();
    using var httpClient = new HttpClient(new StaticRssHandler(rss));
    var crawler = new RssFeedCrawler(httpClient, localRepository);
    var result = await crawler.CrawlAsync([
        new FeedSource
        {
            Id = "quality-test",
            Name = "Quality Test Feed",
            FeedUrl = "https://example.com/rss.xml",
            TopicScope = "ai-news",
            DefaultCandidateLimit = 12,
            SourceQualityTier = "core"
        }
    ]);
    var articles = await localRepository.ListAsync(CancellationToken.None);
    var imported = articles.FirstOrDefault(article => article.SourceUrl == "https://example.com/ai-benchmark-11");

    Assert(result.ArticlesPersisted == 1, "crawler should continue past the first ten low-value candidates");
    Assert(result.Logs.Count(log => log.StartsWith("Rejected", StringComparison.OrdinalIgnoreCase)) == 10, "crawler should log deterministic rejections");
    Assert(imported is not null, "crawler should persist the later relevant AI article");
    Assert(imported!.IngestionScore > 0, "persisted article should include quality score");
}

async Task ShouldRejectCandidateWhenOnlySourceMetadataContainsAi()
{
    const string rss = """
        <rss version="2.0">
          <channel>
            <item>
              <title>Quarterly enterprise software pricing roundup</title>
              <link>https://example.com/pricing-roundup</link>
              <description>Vendors updated seat packaging and procurement terms for annual contracts.</description>
              <pubDate>Tue, 12 May 2026 07:30:00 GMT</pubDate>
            </item>
          </channel>
        </rss>
        """;

    var localRepository = new InMemoryArticleRepository();
    using var httpClient = new HttpClient(new StaticRssHandler(rss));
    var crawler = new RssFeedCrawler(httpClient, localRepository);
    var result = await crawler.CrawlAsync([
        new FeedSource
        {
            Id = "source-metadata-ai",
            Name = "Example AI Coverage",
            FeedUrl = "https://example.com/rss.xml",
            TopicScope = "ai-news",
            DefaultCandidateLimit = 10,
            SourceQualityTier = "core"
        }
    ]);
    var articles = await localRepository.ListAsync(CancellationToken.None);

    Assert(result.ArticlesPersisted == 0, "source metadata containing AI should not make a candidate relevant without content or URL signal");
    Assert(result.Logs.Any(log => log.Contains("not_ai_related", StringComparison.OrdinalIgnoreCase)), "crawler should log deterministic not-ai rejection");
    Assert(articles.All(article => article.SourceUrl != "https://example.com/pricing-roundup"), "irrelevant candidate should not be persisted");
}

async Task ShouldAcceptCoreSourceShortNamedProductTitle()
{
    const string rss = """
        <rss version="2.0">
          <channel>
            <item>
              <title>Introducing Gemini Omni</title>
              <link>https://deepmind.google/blog/introducing-gemini-omni/</link>
              <description></description>
              <pubDate>Tue, 12 May 2026 07:30:00 GMT</pubDate>
            </item>
          </channel>
        </rss>
        """;

    var localRepository = new InMemoryArticleRepository();
    using var httpClient = new HttpClient(new StaticRssHandler(rss));
    var crawler = new RssFeedCrawler(httpClient, localRepository);
    var result = await crawler.CrawlAsync([
        new FeedSource
        {
            Id = "core-source",
            Name = "Core Source",
            FeedUrl = "https://example.com/rss.xml",
            TopicScope = "ai-research-lab",
            DefaultCandidateLimit = 10,
            SourceQualityTier = "core"
        }
    ]);
    var articles = await localRepository.ListAsync(CancellationToken.None);

    Assert(result.ArticlesPersisted == 1, "core official sources should accept short titles with strong named AI product signals");
    Assert(articles.Any(article => article.SourceUrl == "https://deepmind.google/blog/introducing-gemini-omni/"), "accepted short named-product candidate should be persisted");
}

async Task ShouldRejectWeakWatchSourceCandidate()
{
    const string rss = """
        <rss version="2.0">
          <channel>
            <item>
              <title>AI platform update</title>
              <link>https://example.com/ai-platform-update</link>
              <description>AI teams share a short enterprise update about internal plans and general industry momentum.</description>
              <pubDate>Tue, 12 May 2026 07:30:00 GMT</pubDate>
            </item>
          </channel>
        </rss>
        """;

    var localRepository = new InMemoryArticleRepository();
    using var httpClient = new HttpClient(new StaticRssHandler(rss));
    var crawler = new RssFeedCrawler(httpClient, localRepository);
    var result = await crawler.CrawlAsync([
        new FeedSource
        {
            Id = "watch-source",
            Name = "Watch Source",
            FeedUrl = "https://example.com/rss.xml",
            TopicScope = "ai-newsletter",
            DefaultCandidateLimit = 10,
            SourceQualityTier = "watch"
        }
    ]);
    var articles = await localRepository.ListAsync(CancellationToken.None);

    Assert(result.ArticlesPersisted == 0, "watch sources should reject generic AI-only candidates");
    Assert(result.Logs.Any(log => log.Contains("weak_watch_source_signal", StringComparison.OrdinalIgnoreCase)), "watch source rejection should be deterministic");
    Assert(articles.All(article => article.SourceUrl != "https://example.com/ai-platform-update"), "weak watch source candidate should not be persisted");
}

async Task ShouldExcludeRejectedArticlesFromArticleList()
{
    var localRepository = new InMemoryArticleRepository();
    await localRepository.UpsertAsync(new Article
    {
        Id = "rejected_job",
        Title = "AI hiring roundup",
        Summary = "A jobs-only article should be hidden from the normal reader feed.",
        SourceUrl = "https://example.com/rejected-job",
        SourceName = "Example",
        Tags = ["ai"],
        PublishedAt = DateTimeOffset.Parse("2026-05-14T00:00:00Z"),
        IngestionScore = 0,
        RejectionReason = "job_posting",
        MatchedKeywords = ["ai"],
        SourceQualityTier = "watch",
        HasAiSummary = false,
        IsBookmarked = false
    }, CancellationToken.None);

    var localService = new ArticleQueryService(
        localRepository,
        new InMemoryAiSummaryRepository(),
        new InMemoryBookmarkRepository(),
        new InMemoryHiddenArticleRepository());
    var result = await localService.GetArticlesAsync(new ArticleListParams(null, 20, null, null, null, null), "local_test");

    Assert(result.Items.All(article => article.Id != "rejected_job"), "rejected articles should not appear in the normal article list");
}

async Task ShouldUseQualityBeforeRecencyWithinArticleListDay()
{
    var localRepository = new InMemoryArticleRepository();

    await localRepository.UpsertAsync(new Article
    {
        Id = "quality_low",
        Title = "AI platform update",
        Summary = "A relevant but lower-confidence AI platform update.",
        SourceUrl = "https://example.com/quality-low",
        SourceName = "Example",
        Tags = ["ai"],
        PublishedAt = DateTimeOffset.Parse("2026-05-15T11:00:00Z"),
        IngestionScore = 55,
        MatchedKeywords = ["ai"],
        SourceQualityTier = "watch",
        HasAiSummary = false,
        IsBookmarked = false
    }, CancellationToken.None);

    await localRepository.UpsertAsync(new Article
    {
        Id = "quality_high",
        Title = "OpenAI benchmark improves model safety evaluations",
        Summary = "A relevant AI model safety article with stronger ingestion metadata.",
        SourceUrl = "https://example.com/quality-high",
        SourceName = "Example",
        Tags = ["ai", "model", "safety"],
        PublishedAt = DateTimeOffset.Parse("2026-05-15T09:00:00Z"),
        IngestionScore = 95,
        MatchedKeywords = ["openai", "model", "safety"],
        SourceQualityTier = "core",
        HasAiSummary = false,
        IsBookmarked = false
    }, CancellationToken.None);

    var localService = new ArticleQueryService(
        localRepository,
        new InMemoryAiSummaryRepository(),
        new InMemoryBookmarkRepository(),
        new InMemoryHiddenArticleRepository());
    var result = await localService.GetArticlesAsync(new ArticleListParams(null, 2, null, null, null, null), "local_test");

    Assert(result.Items[0].Id == "quality_high", "article list should rank high-quality same-day articles before lower-quality newer articles");
    Assert(result.Items.All(article => article.Id != "quality_low"), "below-threshold articles should not appear in the normal reader feed");
}

async Task ShouldGetAiSummaryPreview()
{
    var summaryService = new AiSummaryQueryService(
        repository,
        new InMemoryAiSummaryRepository(),
        new InMemoryAiSummaryReadCache());

    var result = await summaryService.GetPreviewAsync("art_01JAI001");

    Assert(result.Status == AiSummaryQueryStatus.Found, "summary query should find seeded summary");
    Assert(result.Summary?.Highlights.Count > 0, "summary preview should include highlights");
    Assert(result.Summary?.ImpactScope.Length > 0, "summary preview should include impact scope");
    Assert(result.Summary?.Provider == "seed", "summary preview should include provider metadata");
    Assert(result.Summary?.PromptVersion == "quick-summary-seed-v1", "summary preview should include prompt version");
}

async Task ShouldReturnSummaryNotFound()
{
    var summaryService = new AiSummaryQueryService(
        repository,
        new InMemoryAiSummaryRepository(),
        new InMemoryAiSummaryReadCache());

    var result = await summaryService.GetPreviewAsync("art_01JAI002");

    Assert(result.Status == AiSummaryQueryStatus.SummaryNotFound, "article without summary should return summary-not-found status");
}

async Task ShouldCacheAiSummaryPreview()
{
    var summaryRepository = new CountingSummaryRepository();
    var summaryService = new AiSummaryQueryService(
        repository,
        summaryRepository,
        new InMemoryAiSummaryReadCache());

    var first = await summaryService.GetPreviewAsync("art_01JAI001");
    var second = await summaryService.GetPreviewAsync("art_01JAI001");

    Assert(first.Status == AiSummaryQueryStatus.Found, "first summary query should find summary");
    Assert(second.Status == AiSummaryQueryStatus.Found, "second summary query should find cached summary");
    Assert(summaryRepository.ReadCount == 1, "summary repository should only be read once when cache is warm");
}

async Task ShouldGenerateAiSummaryOnceWhenMissing()
{
    var summaryRepository = new EmptySummaryRepository();
    var generator = new CountingSummaryGenerator();
    var cache = new InMemoryAiSummaryReadCache();
    var generationService = new AiSummaryGenerationService(
        repository,
        summaryRepository,
        generator,
        new InMemoryAiSummaryGenerationTracker(),
        cache);
    var queryService = new AiSummaryQueryService(repository, summaryRepository, cache);

    var generated = await generationService.GenerateAsync("art_01JAI002", force: false);
    var queried = await queryService.GetPreviewAsync("art_01JAI002");

    Assert(generated.Status == AiSummaryGenerationStatus.Ready, "summary generation should be ready for existing articles");
    Assert(generated.WasGenerated, "missing summary should be generated");
    Assert(generator.Calls == 1, "missing summary should call the provider once");
    Assert(summaryRepository.SaveCount == 1, "generated summary should be persisted");
    Assert(queried.Status == AiSummaryQueryStatus.Found, "generated summary should be readable");
    Assert(queried.Summary?.Provider == "counting", "generated summary should include provider metadata");
    Assert(queried.Summary?.PromptVersion == "quick-summary-test-v1", "generated summary should include prompt version");
    Assert(queried.Summary?.EditorView.Contains("summary/source metadata") == true, "fallback summary should not claim full source analysis");
}

async Task ShouldReuseExistingAiSummaryWithoutProviderCall()
{
    var summaryRepository = new InMemoryAiSummaryRepository();
    var generator = new CountingSummaryGenerator();
    var generationService = new AiSummaryGenerationService(
        repository,
        summaryRepository,
        generator,
        new InMemoryAiSummaryGenerationTracker(),
        new InMemoryAiSummaryReadCache());

    var result = await generationService.GenerateAsync("art_01JAI001", force: false);

    Assert(result.Status == AiSummaryGenerationStatus.Ready, "existing summary generation request should return ready");
    Assert(!result.WasGenerated, "existing summary should be reused when force is false");
    Assert(generator.Calls == 0, "existing summary should not call the provider when force is false");
}

async Task ShouldForceRegenerateAiSummaryAndRefreshCache()
{
    var summaryRepository = new InMemoryAiSummaryRepository();
    var generator = new CountingSummaryGenerator();
    var cache = new InMemoryAiSummaryReadCache();
    var generationService = new AiSummaryGenerationService(
        repository,
        summaryRepository,
        generator,
        new InMemoryAiSummaryGenerationTracker(),
        cache);
    var queryService = new AiSummaryQueryService(repository, summaryRepository, cache);

    var generated = await generationService.GenerateAsync("art_01JAI001", force: true);
    var queried = await queryService.GetPreviewAsync("art_01JAI001");

    Assert(generated.WasGenerated, "force should regenerate an existing summary");
    Assert(generator.Calls == 1, "force regeneration should call the provider");
    Assert(queried.Summary?.Provider == "counting", "cache should contain regenerated summary metadata");
}

async Task ShouldKeepOldAiSummaryCacheWhenForceRegenerationFails()
{
    var summaries = new InMemoryAiSummaryRepository();
    var cache = new InMemoryAiSummaryReadCache();
    var queryService = new AiSummaryQueryService(repository, summaries, cache);
    var first = await queryService.GetPreviewAsync("art_01JAI001");
    var generationService = new AiSummaryGenerationService(
        repository,
        summaries,
        new FailingSummaryGenerator("AI_PROVIDER_RATE_LIMITED"),
        new InMemoryAiSummaryGenerationTracker(),
        cache);

    var failed = await generationService.GenerateAsync("art_01JAI001", force: true);
    var afterFailure = await queryService.GetPreviewAsync("art_01JAI001");

    Assert(first.Status == AiSummaryQueryStatus.Found, "precondition should warm the old summary cache");
    Assert(failed.Status == AiSummaryGenerationStatus.ProviderFailed, "provider failure should be mapped at domain level");
    Assert(failed.ErrorCode == "AI_PROVIDER_RATE_LIMITED", "provider failure should preserve documented error code");
    Assert(afterFailure.Summary?.Provider == "seed", "failed force regeneration should not evict the old cached summary");
}

async Task ShouldPreventConcurrentAiSummaryGeneration()
{
    var summaries = new EmptySummaryRepository();
    var generator = new BlockingSummaryGenerator();
    var generationService = new AiSummaryGenerationService(
        repository,
        summaries,
        generator,
        new InMemoryAiSummaryGenerationTracker(),
        new InMemoryAiSummaryReadCache());

    var first = generationService.GenerateAsync("art_01JAI002", force: false);
    await generator.Started.Task;
    var second = await generationService.GenerateAsync("art_01JAI002", force: false);
    generator.Release.SetResult();
    var firstResult = await first;

    Assert(second.Status == AiSummaryGenerationStatus.InProgress, "parallel summary generation should be rejected by tracker");
    Assert(firstResult.Status == AiSummaryGenerationStatus.Ready, "first summary generation should complete");
    Assert(generator.Calls == 1, "parallel summary generation should not call provider twice");
}

async Task ShouldProjectGeneratedSummaryAvailabilityIntoArticlesAndStats()
{
    var localRepository = new InMemoryArticleRepository();
    var summaries = new EmptySummaryRepository();
    var generationService = new AiSummaryGenerationService(
        localRepository,
        summaries,
        new CountingSummaryGenerator(),
        new InMemoryAiSummaryGenerationTracker(),
        new InMemoryAiSummaryReadCache());
    var queryService = new ArticleQueryService(
        localRepository,
        summaries,
        new InMemoryBookmarkRepository(),
        new InMemoryHiddenArticleRepository());
    var statsService = new DashboardStatsQueryService(
        localRepository,
        summaries,
        new FeedCrawlRunState(),
        new FixedTimeProvider(DateTimeOffset.Parse("2026-05-13T10:00:00Z")));

    var generated = await generationService.GenerateAsync("art_01JAI002", force: false);
    var article = await queryService.GetArticleAsync("art_01JAI002", "local_reader");
    var list = await queryService.GetArticlesAsync(new ArticleListParams(null, 20, null, null, null, null), "local_reader");
    var stats = await statsService.GetTodayAsync();

    Assert(generated.Status == AiSummaryGenerationStatus.Ready, "summary generation should succeed before projection checks");
    Assert(article?.HasAiSummary == true, "article detail should project generated summary availability");
    Assert(list.Items.Single(item => item.Id == "art_01JAI002").HasAiSummary, "article list should project generated summary availability");
    Assert(stats.AiSummarizedCount == 2, "dashboard stats should count generated summaries from summary repository");
}

async Task ShouldGenerateAndReadAiReport()
{
    var reportRepository = new InMemoryAiReportRepository();
    var generationService = new AiReportGenerationService(
        repository,
        reportRepository,
        new StubAiReportGenerator(),
        new InMemoryAiReportGenerationTracker(),
        new InMemoryAiReportRateLimiter(new FixedTimeProvider(DateTimeOffset.Parse("2026-05-13T10:00:00Z"))));
    var queryService = new AiReportQueryService(repository, reportRepository);

    var start = await generationService.StartAsync("art_01JAI001", "local_reader", force: false);

    Assert(start.Status == AiReportGenerationStartStatus.Ready, "report generation should start for an existing article");

    await foreach (var streamEvent in start.Stream!)
    {
        Assert(streamEvent.Type != "error", "stub report generation should not emit an error event");
    }

    var result = await queryService.GetReportAsync("art_01JAI001");

    Assert(result.Status == AiReportQueryStatus.Found, "generated report should be readable");
    Assert(result.Report?.KeyPoints.Count > 0, "generated report should include key points");
    Assert(result.Report?.Scores.Impact is >= 0 and <= 100, "generated report scores should be bounded");
}

async Task ShouldRateLimitAiReportPerUserAndArticle()
{
    var reportRepository = new EmptyReportRepository();
    var generationService = new AiReportGenerationService(
        repository,
        reportRepository,
        new StubAiReportGenerator(),
        new InMemoryAiReportGenerationTracker(),
        new InMemoryAiReportRateLimiter(new FixedTimeProvider(DateTimeOffset.Parse("2026-05-13T10:00:00Z"))));

    var first = await generationService.StartAsync("art_01JAI002", "local_reader", force: true);
    await foreach (var _ in first.Stream!)
    {
    }
    var second = await generationService.StartAsync("art_01JAI002", "local_reader", force: true);
    var otherUser = await generationService.StartAsync("art_01JAI002", "local_other", force: true);

    Assert(first.Status == AiReportGenerationStartStatus.Ready, "first generation attempt should be allowed");
    Assert(second.Status == AiReportGenerationStartStatus.RateLimited, "same user/article should be rate limited");
    Assert(second.RetryAfter is not null, "rate limited attempts should expose retry-after");
    Assert(otherUser.Status == AiReportGenerationStartStatus.Ready, "rate limit should stay scoped to user and article");
}

async Task ShouldWriteVersionedMvpSseContract()
{
    var controller = CreateAiSummaryController(
        new InMemoryAiReportRepository(),
        new InMemoryAiReportRateLimiter(new FixedTimeProvider(DateTimeOffset.Parse("2026-05-13T10:00:00Z"))));
    var httpContext = new DefaultHttpContext();
    await using var body = new MemoryStream();
    httpContext.Response.Body = body;
    httpContext.Request.Headers["X-AI-Daily-Local-User"] = "local_reader";
    controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

    await controller.GenerateAiReport("art_01JAI001", force: true);

    body.Position = 0;
    var text = await new StreamReader(body).ReadToEndAsync();

    Assert(httpContext.Response.ContentType == "text/event-stream", "SSE response should use text/event-stream");
    Assert(httpContext.Response.Headers.CacheControl == "no-cache", "SSE response should disable response caching");
    Assert(text.Contains("event: started"), "SSE stream should include the versioned MVP started event");
    Assert(text.Contains("event: status"), "SSE stream should include the versioned MVP status event");
    Assert(text.Contains("event: report"), "SSE stream should include the versioned MVP report event");
    Assert(text.Contains("event: completed"), "SSE stream should include the versioned MVP completed event");
    Assert(!text.Contains("event: chunk"), "SSE stream should not silently switch to the spec chunk event shape");
}

async Task ShouldReturnDocumentedRateLimitError()
{
    var rateLimiter = new InMemoryAiReportRateLimiter(new FixedTimeProvider(DateTimeOffset.Parse("2026-05-13T10:00:00Z")));
    var warmup = CreateAiSummaryController(new EmptyReportRepository(), rateLimiter);
    var warmupContext = new DefaultHttpContext();
    await using var warmupBody = new MemoryStream();
    warmupContext.Response.Body = warmupBody;
    warmupContext.Request.Headers["X-AI-Daily-Local-User"] = "local_reader";
    warmup.ControllerContext = new ControllerContext { HttpContext = warmupContext };
    _ = await warmup.GenerateAiReport("art_01JAI002", force: true);

    var controller = CreateAiSummaryController(
        new EmptyReportRepository(),
        rateLimiter);
    var httpContext = new DefaultHttpContext();
    httpContext.Request.Headers["X-AI-Daily-Local-User"] = "local_reader";
    controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

    var limited = await controller.GenerateAiReport("art_01JAI002", force: true);

    var result = limited as ObjectResult;
    var error = result?.Value as ApiErrorResponse;

    Assert(result?.StatusCode == StatusCodes.Status429TooManyRequests, "rate limited generation should return HTTP 429");
    Assert(error?.Error.Code == "AI_RATE_LIMIT_EXCEEDED", "rate limited generation should return the documented error code");
    Assert(httpContext.Response.Headers.RetryAfter.Count == 1, "rate limited generation should include Retry-After");
}

AiSummaryController CreateAiSummaryController(
    IAiReportRepository reportRepository,
    IAiReportRateLimiter rateLimiter)
{
    var summaryRepository = new InMemoryAiSummaryRepository();
    var summaryCache = new InMemoryAiSummaryReadCache();

    return new AiSummaryController(
        new AiSummaryQueryService(repository, summaryRepository, summaryCache),
        new AiSummaryGenerationService(
            repository,
            summaryRepository,
            new CountingSummaryGenerator(),
            new InMemoryAiSummaryGenerationTracker(),
            summaryCache),
        new AiReportQueryService(repository, reportRepository),
        new AiReportGenerationService(
            repository,
            reportRepository,
            new StubAiReportGenerator(),
            new InMemoryAiReportGenerationTracker(),
            rateLimiter));
}

Task ShouldBoundGeminiReportPromptContent()
{
    var article = new Article
    {
        Id = "art_long",
        Title = "Long source article",
        Summary = "Fallback summary",
        SourceUrl = "https://example.com/long",
        SourceName = "Example",
        Tags = ["model"],
        PublishedAt = DateTimeOffset.Parse("2026-05-13T00:00:00Z"),
        ContentStatus = "full_content_ready",
        ContentText = new string('A', GeminiAiReportGenerator.MaxPromptContentCharacters + 500)
    };

    var prompt = GeminiAiReportGenerator.BuildPrompt(article);

    Assert(prompt.Contains(new string('A', GeminiAiReportGenerator.MaxPromptContentCharacters)), "prompt should include bounded source content");
    Assert(prompt.Contains("contentBasis: full imported source text"), "full content prompts should identify full source basis");
    Assert(!prompt.Contains(new string('A', GeminiAiReportGenerator.MaxPromptContentCharacters + 1)), "prompt should cap source content deterministically");
    return Task.CompletedTask;
}

Task ShouldMarkFallbackReportPromptAsNotFullContent()
{
    var article = new Article
    {
        Id = "art_fallback",
        Title = "Fallback source article",
        Summary = "Fallback summary only",
        SourceUrl = "https://example.com/fallback",
        SourceName = "Example",
        Tags = ["model"],
        PublishedAt = DateTimeOffset.Parse("2026-05-13T00:00:00Z"),
        ContentStatus = "summary_fallback",
        ContentText = "Fallback summary only"
    };

    var prompt = GeminiAiReportGenerator.BuildPrompt(article);

    Assert(prompt.Contains("summary/source metadata fallback; do not claim full-article analysis"), "fallback prompts should not claim full source content");
    return Task.CompletedTask;
}

Task ShouldRejectInvalidAiReportDraft()
{
    var invalid = new AiReportDraft(
        "",
        [],
        [],
        [],
        [],
        new AiReportScoresDto(101, -1, 50),
        [],
        "",
        "surprise");

    Assert(!AiReportValidation.TryValidate(invalid, out _), "invalid report drafts should be rejected");
    return Task.CompletedTask;
}

Task ShouldNormalizeSparseAiReportDraft()
{
    var article = new Article
    {
        Id = "art_sparse",
        Title = "Sparse provider response",
        Summary = "Provider returned too little structure.",
        SourceUrl = "https://example.com/sparse",
        SourceName = "Example",
        Tags = ["model"],
        PublishedAt = DateTimeOffset.Parse("2026-05-13T00:00:00Z")
    };
    var sparse = new AiReportDraft(
        "",
        [],
        [],
        [],
        [],
        new AiReportScoresDto(150, -10, 40),
        [],
        "",
        "unknown");

    var normalized = AiReportDraftNormalizer.Normalize(sparse, article);

    Assert(AiReportValidation.TryValidate(normalized, out _), "sparse provider drafts should normalize into valid reports");
    Assert(normalized.Scores.Impact == 100, "impact score should be clamped");
    Assert(normalized.Scores.Confidence == 0, "confidence score should be clamped");
    Assert(normalized.Rating == "watchlist", "invalid rating should fall back to watchlist");
    return Task.CompletedTask;
}

async Task ShouldBookmarkArticleForLocalUser()
{
    var localRepository = new InMemoryArticleRepository();
    var localBookmarks = new InMemoryBookmarkRepository();
    var bookmarkService = new BookmarkService(
        localRepository,
        localBookmarks,
        new FixedTimeProvider(DateTimeOffset.Parse("2026-05-13T10:00:00Z")));
    var queryService = new ArticleQueryService(
        localRepository,
        new InMemoryAiSummaryRepository(),
        localBookmarks,
        new InMemoryHiddenArticleRepository());

    var saved = await bookmarkService.AddAsync("local_reader", "art_01JAI001");
    var article = await queryService.GetArticleAsync("art_01JAI001", "local_reader");
    var bookmarks = await bookmarkService.ListAsync("local_reader");

    Assert(saved.Status == BookmarkMutationStatus.Ready, "bookmark mutation should succeed for a local user");
    Assert(article?.IsBookmarked == true, "article detail should include local user's bookmark state");
    Assert(bookmarks.Count == 1 && bookmarks[0].Id == "art_01JAI001", "bookmark list should return saved article");
}

async Task ShouldKeepBookmarksScopedToLocalUser()
{
    var localRepository = new InMemoryArticleRepository();
    var localBookmarks = new InMemoryBookmarkRepository();
    var bookmarkService = new BookmarkService(
        localRepository,
        localBookmarks,
        new FixedTimeProvider(DateTimeOffset.Parse("2026-05-13T10:00:00Z")));
    var queryService = new ArticleQueryService(
        localRepository,
        new InMemoryAiSummaryRepository(),
        localBookmarks,
        new InMemoryHiddenArticleRepository());

    await bookmarkService.AddAsync("local_reader_a", "art_01JAI001");

    var articleForOtherUser = await queryService.GetArticleAsync("art_01JAI001", "local_reader_b");
    var otherUserBookmarks = await bookmarkService.ListAsync("local_reader_b");

    Assert(articleForOtherUser?.IsBookmarked == false, "bookmark state should not leak between local users");
    Assert(otherUserBookmarks.Count == 0, "bookmark list should stay scoped to the local user id");
}

async Task ShouldHideArticleForLocalUser()
{
    var localRepository = new InMemoryArticleRepository();
    var localHiddenArticles = new InMemoryHiddenArticleRepository();
    var preferenceService = new UserPreferenceService(
        localRepository,
        localHiddenArticles,
        new FixedTimeProvider(DateTimeOffset.Parse("2026-05-13T10:00:00Z")));
    var queryService = new ArticleQueryService(
        localRepository,
        new InMemoryAiSummaryRepository(),
        new InMemoryBookmarkRepository(),
        localHiddenArticles);

    var hidden = await preferenceService.HideArticleAsync("local_reader", "art_01JAI001", "not_interested");
    var result = await queryService.GetArticlesAsync(new ArticleListParams(null, 20, null, null, null, null), "local_reader");
    var otherUserResult = await queryService.GetArticlesAsync(new ArticleListParams(null, 20, null, null, null, null), "local_other");
    var hiddenList = await preferenceService.ListHiddenArticlesAsync("local_reader");

    Assert(hidden.Status == HiddenArticleMutationStatus.Ready, "hidden article mutation should succeed for a local user");
    Assert(result.Items.All(article => article.Id != "art_01JAI001"), "hidden articles should not appear in the current user's normal list");
    Assert(otherUserResult.Items.Any(article => article.Id == "art_01JAI001"), "hidden state should not affect other local users");
    Assert(hiddenList.Count == 1 && hiddenList[0].Id == "art_01JAI001", "hidden list should expose restorable articles");
}

async Task ShouldRestoreHiddenArticle()
{
    var localRepository = new InMemoryArticleRepository();
    var localHiddenArticles = new InMemoryHiddenArticleRepository();
    var preferenceService = new UserPreferenceService(
        localRepository,
        localHiddenArticles,
        new FixedTimeProvider(DateTimeOffset.Parse("2026-05-13T10:00:00Z")));
    var queryService = new ArticleQueryService(
        localRepository,
        new InMemoryAiSummaryRepository(),
        new InMemoryBookmarkRepository(),
        localHiddenArticles);

    await preferenceService.HideArticleAsync("local_reader", "art_01JAI001", null);
    await preferenceService.RestoreArticleAsync("local_reader", "art_01JAI001");
    var result = await queryService.GetArticlesAsync(new ArticleListParams(null, 20, null, null, null, null), "local_reader");

    Assert(result.Items.Any(article => article.Id == "art_01JAI001"), "restored hidden articles should return to the normal article list");
}

static void Assert(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}

internal sealed class StaticRssHandler : HttpMessageHandler
{
    private readonly string _rss;

    public StaticRssHandler(string rss)
    {
        _rss = rss;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
        Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(_rss)
        });
}

internal sealed class ThrowingHandler : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
        throw new HttpRequestException("network unavailable");
}

internal sealed class StatusCodeHandler : HttpMessageHandler
{
    private readonly HttpStatusCode _statusCode;

    public StatusCodeHandler(HttpStatusCode statusCode)
    {
        _statusCode = statusCode;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
        Task.FromResult(new HttpResponseMessage(_statusCode));
}

internal sealed class FixedTimeProvider : TimeProvider
{
    private readonly DateTimeOffset _utcNow;

    public FixedTimeProvider(DateTimeOffset utcNow)
    {
        _utcNow = utcNow;
    }

    public override DateTimeOffset GetUtcNow() => _utcNow;
}

internal sealed class CountingSummaryRepository : IAiSummaryRepository
{
    public int ReadCount { get; private set; }

    public Task<AiSummary?> GetByArticleIdAsync(string articleId, CancellationToken cancellationToken)
    {
        ReadCount++;

        return Task.FromResult<AiSummary?>(new AiSummary
        {
            Id = "sum_counting",
            ArticleId = articleId,
            Highlights = ["Cached highlight"],
            ImpactScope = "Cache validation",
            Controversy = "None",
            EditorView = "Use cached previews for repeated reads.",
            Provider = "counting",
            PromptVersion = "quick-summary-test-v1",
            GeneratedAt = DateTimeOffset.Parse("2026-05-12T08:00:00Z")
        });
    }

    public Task SaveAsync(AiSummary summary, CancellationToken cancellationToken) =>
        Task.CompletedTask;

    public Task<IReadOnlySet<string>> ListArticleIdsWithSummariesAsync(
        IEnumerable<string> articleIds,
        CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlySet<string>>(articleIds.ToHashSet(StringComparer.Ordinal));
}

internal sealed class EmptySummaryRepository : IAiSummaryRepository
{
    private AiSummary? _summary;

    public int SaveCount { get; private set; }

    public Task<AiSummary?> GetByArticleIdAsync(string articleId, CancellationToken cancellationToken) =>
        Task.FromResult(_summary?.ArticleId == articleId ? _summary : null);

    public Task<IReadOnlySet<string>> ListArticleIdsWithSummariesAsync(
        IEnumerable<string> articleIds,
        CancellationToken cancellationToken)
    {
        var requested = articleIds.ToHashSet(StringComparer.Ordinal);
        return Task.FromResult<IReadOnlySet<string>>(
            _summary is not null && requested.Contains(_summary.ArticleId)
                ? new HashSet<string>([_summary.ArticleId], StringComparer.Ordinal)
                : new HashSet<string>(StringComparer.Ordinal));
    }

    public Task SaveAsync(AiSummary summary, CancellationToken cancellationToken)
    {
        _summary = summary;
        SaveCount++;
        return Task.CompletedTask;
    }
}

internal sealed class EmptyReportRepository : IAiReportRepository
{
    public Task<AiReport?> GetByArticleIdAsync(string articleId, CancellationToken cancellationToken) =>
        Task.FromResult<AiReport?>(null);

    public Task SaveAsync(AiReport report, CancellationToken cancellationToken) =>
        Task.CompletedTask;
}

internal sealed class CountingSummaryGenerator : IAiSummaryGenerator
{
    public int Calls { get; private set; }
    public string ProviderName => "counting";
    public string PromptVersion => "quick-summary-test-v1";

    public Task<AiSummaryDraft> GenerateAsync(Article article, CancellationToken cancellationToken)
    {
        Calls++;
        var isFullContent = article.ContentStatus == "full_content_ready";

        return Task.FromResult(new AiSummaryDraft(
            ["Generated quick summary"],
            "Test impact scope",
            "Test controversy",
            isFullContent
                ? "Generated from full imported source text."
                : "Generated from summary/source metadata without claiming full source analysis."));
    }
}

internal sealed class FailingSummaryGenerator : IAiSummaryGenerator
{
    private readonly string _errorCode;

    public FailingSummaryGenerator(string errorCode)
    {
        _errorCode = errorCode;
    }

    public string ProviderName => "failing";
    public string PromptVersion => "quick-summary-test-v1";

    public Task<AiSummaryDraft> GenerateAsync(Article article, CancellationToken cancellationToken) =>
        throw new InvalidOperationException(_errorCode);
}

internal sealed class BlockingSummaryGenerator : IAiSummaryGenerator
{
    public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public int Calls { get; private set; }
    public string ProviderName => "blocking";
    public string PromptVersion => "quick-summary-test-v1";

    public async Task<AiSummaryDraft> GenerateAsync(Article article, CancellationToken cancellationToken)
    {
        Calls++;
        Started.TrySetResult();
        await Release.Task.WaitAsync(cancellationToken);
        return new AiSummaryDraft(
            ["Generated quick summary"],
            "Test impact scope",
            "Test controversy",
            "Generated from summary/source metadata without claiming full source analysis.");
    }
}

internal sealed class CountingFeedCrawler : IFeedCrawler
{
    public int Calls { get; private set; }

    public Task<FeedCrawlResult> CrawlAsync(
        IEnumerable<FeedSource> sources,
        CancellationToken cancellationToken = default)
    {
        Calls++;
        return Task.FromResult(new FeedCrawlResult(1, 1, ["Crawled Example: 1 RSS items read."]));
    }
}

internal sealed class StaticFeedSourceCatalog : IFeedSourceCatalog
{
    public IReadOnlyList<FeedSource> GetEnabledSources() =>
    [
        new FeedSource
        {
            Id = "example",
            Name = "Example",
            FeedUrl = "https://example.com/rss.xml",
            SiteUrl = "https://example.com"
        }
    ];
}
