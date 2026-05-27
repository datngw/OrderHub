using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OrderHub.Infrastructure.Persistence;
using OrderHub.Infrastructure.Persistence.Seed;

namespace OrderHub.Infrastructure.BackgroundServices;

public sealed class DatabaseMigrationHostedService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DatabaseMigrationHostedService> _logger;

    public DatabaseMigrationHostedService(
        IServiceProvider serviceProvider,
        ILogger<DatabaseMigrationHostedService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken ct)
    {
        const int maxRetries = 3;
        var delay = TimeSpan.FromSeconds(5);

        for (var attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<OrderHubDbContext>();

                _logger.LogInformation("Applying pending migrations (attempt {Attempt}/{Max})...", attempt, maxRetries);
                await dbContext.Database.MigrateAsync(ct);
                _logger.LogInformation("Migrations applied successfully");

                _logger.LogInformation("Seeding database...");
                DataSeeder.Seed(dbContext);
                _logger.LogInformation("Database seeded successfully");

                return;
            }
            catch (Exception ex) when (attempt < maxRetries)
            {
                _logger.LogWarning(ex, "Migration attempt {Attempt} failed, retrying in {Delay}s...", attempt, delay.TotalSeconds);
                await Task.Delay(delay, ct);
                delay *= 2;
            }
        }
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}
