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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ConfigureArticles(modelBuilder);
        ConfigureFeedSources(modelBuilder);
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

    private static ValueConverter<IReadOnlyList<string>, string> CreateStringListConverter() =>
        new(
            value => JsonSerializer.Serialize(value, JsonOptions),
            value => JsonSerializer.Deserialize<IReadOnlyList<string>>(value, JsonOptions) ?? Array.Empty<string>());

    private static ValueComparer<IReadOnlyList<string>> CreateStringListComparer() =>
        new(
            (left, right) => left != null && right != null && left.SequenceEqual(right),
            value => value.Aggregate(0, (hash, item) => HashCode.Combine(hash, item.GetHashCode(StringComparison.Ordinal))),
            value => value.ToArray());
}
