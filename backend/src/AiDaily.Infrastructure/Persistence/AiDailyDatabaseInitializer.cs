using AiDaily.Infrastructure.FeedCrawler;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AiDaily.Infrastructure.Persistence;

public static class AiDailyDatabaseInitializer
{
    public static async Task InitializeAsync(
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken = default)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AiDailyDbContext>();
        await dbContext.Database.EnsureCreatedAsync(cancellationToken);

        var feedSources = scope.ServiceProvider.GetRequiredService<EfCoreFeedSourceCatalog>();
        await feedSources.SeedDefaultsAsync(cancellationToken);
    }
}
