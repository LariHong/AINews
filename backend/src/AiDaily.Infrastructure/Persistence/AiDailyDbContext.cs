using System.Text.Json;
using AiDaily.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace AiDaily.Infrastructure.Persistence;

public sealed class AiDailyDbContext : DbContext
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public AiDailyDbContext(DbContextOptions<AiDailyDbContext> options)
        : base(options)
    {
    }

    public DbSet<Article> Articles => Set<Article>();
    public DbSet<FeedSource> FeedSources => Set<FeedSource>();
    public DbSet<AiSummary> AiSummaries => Set<AiSummary>();
    public DbSet<AiReport> AiReports => Set<AiReport>();
    public DbSet<Bookmark> Bookmarks => Set<Bookmark>();
    public DbSet<HiddenArticle> HiddenArticles => Set<HiddenArticle>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ConfigureArticles(modelBuilder);
        ConfigureFeedSources(modelBuilder);
        ConfigureAiSummaries(modelBuilder);
        ConfigureAiReports(modelBuilder);
        ConfigureBookmarks(modelBuilder);
        ConfigureHiddenArticles(modelBuilder);
    }

    private static void ConfigureArticles(ModelBuilder modelBuilder)
    {
        var listConverter = CreateStringListConverter();
        var listComparer = CreateStringListComparer();

        modelBuilder.Entity<Article>(entity =>
        {
            entity.ToTable("articles");
            entity.HasKey(article => article.Id);
            entity.HasIndex(article => article.SourceUrl).IsUnique();

            entity.Property(article => article.Id).HasColumnName("id").HasMaxLength(80);
            entity.Property(article => article.Title).HasColumnName("title").HasMaxLength(500).IsRequired();
            entity.Property(article => article.Summary).HasColumnName("summary");
            entity.Property(article => article.Content).HasColumnName("content");
            entity.Property(article => article.ContentText).HasColumnName("content_text");
            entity.Property(article => article.ContentStatus).HasColumnName("content_status").HasMaxLength(80).IsRequired();
            entity.Property(article => article.ContentExtractedAt).HasColumnName("content_extracted_at");
            entity.Property(article => article.SourceUrl).HasColumnName("source_url").HasMaxLength(1000).IsRequired();
            entity.Property(article => article.SourceId).HasColumnName("source_id").HasMaxLength(120);
            entity.Property(article => article.SourceName).HasColumnName("source_name").HasMaxLength(240).IsRequired();
            entity.Property(article => article.SourceLogoUrl).HasColumnName("source_logo_url").HasMaxLength(1000);
            entity.Property(article => article.Tags)
                .HasColumnName("tags")
                .HasConversion(listConverter)
                .Metadata.SetValueComparer(listComparer);
            entity.Property(article => article.IngestionScore).HasColumnName("ingestion_score");
            entity.Property(article => article.RejectionReason).HasColumnName("rejection_reason").HasMaxLength(120);
            entity.Property(article => article.MatchedKeywords)
                .HasColumnName("matched_keywords")
                .HasConversion(listConverter)
                .Metadata.SetValueComparer(listComparer);
            entity.Property(article => article.SourceQualityTier).HasColumnName("source_quality_tier").HasMaxLength(80).IsRequired();
            entity.Property(article => article.PublishedAt).HasColumnName("published_at").IsRequired();
            entity.Property(article => article.HasAiSummary).HasColumnName("has_ai_summary");
            entity.Property(article => article.IsBookmarked).HasColumnName("is_bookmarked");
            entity.Property(article => article.ReadTimeMinutes).HasColumnName("read_time_minutes");
        });
    }

    private static void ConfigureFeedSources(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<FeedSource>(entity =>
        {
            entity.ToTable("feed_sources");
            entity.HasKey(source => source.Id);
            entity.HasIndex(source => source.FeedUrl).IsUnique();

            entity.Property(source => source.Id).HasColumnName("id").HasMaxLength(120);
            entity.Property(source => source.Name).HasColumnName("name").HasMaxLength(240).IsRequired();
            entity.Property(source => source.FeedUrl).HasColumnName("feed_url").HasMaxLength(1000).IsRequired();
            entity.Property(source => source.SiteUrl).HasColumnName("site_url").HasMaxLength(1000);
            entity.Property(source => source.SourceType).HasColumnName("source_type").HasMaxLength(80).IsRequired();
            entity.Property(source => source.TopicScope).HasColumnName("topic_scope").HasMaxLength(120).IsRequired();
            entity.Property(source => source.DefaultCandidateLimit).HasColumnName("default_candidate_limit");
            entity.Property(source => source.SourceQualityTier).HasColumnName("source_quality_tier").HasMaxLength(80).IsRequired();
            entity.Property(source => source.QualityNotes).HasColumnName("quality_notes");
            entity.Property(source => source.IsEnabled).HasColumnName("is_enabled");
            entity.Property(source => source.LastCrawledAt).HasColumnName("last_crawled_at");
        });
    }

    private static void ConfigureAiSummaries(ModelBuilder modelBuilder)
    {
        var listConverter = CreateStringListConverter();
        var listComparer = CreateStringListComparer();

        modelBuilder.Entity<AiSummary>(entity =>
        {
            entity.ToTable("ai_summaries");
            entity.HasKey(summary => summary.Id);
            entity.HasIndex(summary => summary.ArticleId).IsUnique();

            entity.Property(summary => summary.Id).HasColumnName("id").HasMaxLength(120);
            entity.Property(summary => summary.ArticleId).HasColumnName("article_id").HasMaxLength(80).IsRequired();
            entity.Property(summary => summary.Highlights)
                .HasColumnName("highlights")
                .HasConversion(listConverter)
                .Metadata.SetValueComparer(listComparer);
            entity.Property(summary => summary.ImpactScope).HasColumnName("impact_scope").IsRequired();
            entity.Property(summary => summary.Controversy).HasColumnName("controversy").IsRequired();
            entity.Property(summary => summary.EditorView).HasColumnName("editor_view").IsRequired();
            entity.Property(summary => summary.Provider).HasColumnName("provider").HasMaxLength(80).IsRequired();
            entity.Property(summary => summary.PromptVersion).HasColumnName("prompt_version").HasMaxLength(120).IsRequired();
            entity.Property(summary => summary.GeneratedAt).HasColumnName("generated_at");
        });
    }

    private static void ConfigureAiReports(ModelBuilder modelBuilder)
    {
        var stringListConverter = CreateStringListConverter();
        var stringListComparer = CreateStringListComparer();
        var timelineConverter = CreateJsonConverter<IReadOnlyList<AiReportTimelineItem>>();
        var timelineComparer = CreateJsonComparer<IReadOnlyList<AiReportTimelineItem>>();
        var scoresConverter = CreateJsonConverter<AiReportScores>();
        var scoresComparer = CreateJsonComparer<AiReportScores>();

        modelBuilder.Entity<AiReport>(entity =>
        {
            entity.ToTable("ai_reports");
            entity.HasKey(report => report.Id);
            entity.HasIndex(report => report.ArticleId).IsUnique();

            entity.Property(report => report.Id).HasColumnName("id").HasMaxLength(120);
            entity.Property(report => report.ArticleId).HasColumnName("article_id").HasMaxLength(80).IsRequired();
            entity.Property(report => report.Tldr).HasColumnName("tldr").IsRequired();
            entity.Property(report => report.KeyPoints)
                .HasColumnName("key_points")
                .HasConversion(stringListConverter)
                .Metadata.SetValueComparer(stringListComparer);
            entity.Property(report => report.Pros)
                .HasColumnName("pros")
                .HasConversion(stringListConverter)
                .Metadata.SetValueComparer(stringListComparer);
            entity.Property(report => report.Cons)
                .HasColumnName("cons")
                .HasConversion(stringListConverter)
                .Metadata.SetValueComparer(stringListComparer);
            entity.Property(report => report.Timeline)
                .HasColumnName("timeline")
                .HasConversion(timelineConverter)
                .Metadata.SetValueComparer(timelineComparer);
            entity.Property(report => report.Scores)
                .HasColumnName("scores")
                .HasConversion(scoresConverter)
                .Metadata.SetValueComparer(scoresComparer);
            entity.Property(report => report.RelatedTags)
                .HasColumnName("related_tags")
                .HasConversion(stringListConverter)
                .Metadata.SetValueComparer(stringListComparer);
            entity.Property(report => report.EditorNote).HasColumnName("editor_note").IsRequired();
            entity.Property(report => report.Rating).HasColumnName("rating").HasMaxLength(80).IsRequired();
            entity.Property(report => report.Provider).HasColumnName("provider").HasMaxLength(80).IsRequired();
            entity.Property(report => report.GeneratedAt).HasColumnName("generated_at");
        });
    }

    private static void ConfigureBookmarks(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Bookmark>(entity =>
        {
            entity.ToTable("bookmarks");
            entity.HasKey(bookmark => new { bookmark.UserId, bookmark.ArticleId });

            entity.Property(bookmark => bookmark.UserId).HasColumnName("user_id").HasMaxLength(120);
            entity.Property(bookmark => bookmark.ArticleId).HasColumnName("article_id").HasMaxLength(80);
            entity.Property(bookmark => bookmark.CreatedAt).HasColumnName("created_at");
        });
    }

    private static void ConfigureHiddenArticles(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<HiddenArticle>(entity =>
        {
            entity.ToTable("hidden_articles");
            entity.HasKey(hiddenArticle => new { hiddenArticle.UserId, hiddenArticle.ArticleId });

            entity.Property(hiddenArticle => hiddenArticle.UserId).HasColumnName("user_id").HasMaxLength(120);
            entity.Property(hiddenArticle => hiddenArticle.ArticleId).HasColumnName("article_id").HasMaxLength(80);
            entity.Property(hiddenArticle => hiddenArticle.Reason).HasColumnName("reason").HasMaxLength(120);
            entity.Property(hiddenArticle => hiddenArticle.CreatedAt).HasColumnName("created_at");
        });
    }

    private static ValueConverter<IReadOnlyList<string>, string> CreateStringListConverter() =>
        new(
            value => JsonSerializer.Serialize(value, JsonOptions),
            value => JsonSerializer.Deserialize<IReadOnlyList<string>>(value, JsonOptions) ?? Array.Empty<string>());

    private static ValueComparer<IReadOnlyList<string>> CreateStringListComparer() =>
        new(
            (left, right) => left != null && right != null && left.SequenceEqual(right),
            value => value.Aggregate(0, (hash, item) => HashCode.Combine(hash, item.GetHashCode(StringComparison.Ordinal))),
            value => value.ToArray());

    private static ValueConverter<T, string> CreateJsonConverter<T>() =>
        new(
            value => JsonSerializer.Serialize(value, JsonOptions),
            value => JsonSerializer.Deserialize<T>(value, JsonOptions)!);

    private static ValueComparer<T> CreateJsonComparer<T>() =>
        new(
            (left, right) => JsonSerializer.Serialize(left, JsonOptions) == JsonSerializer.Serialize(right, JsonOptions),
            value => JsonSerializer.Serialize(value, JsonOptions).GetHashCode(StringComparison.Ordinal),
            value => JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(value, JsonOptions), JsonOptions)!);
}
