using EMR.Infrastructure.Data;
using EMR.Infrastructure.Data.Seeds;
using Microsoft.EntityFrameworkCore;

namespace EMR.Api.Extensions;

/// <summary>
/// Extension methods for database initialization and seeding
/// </summary>
public static class DatabaseExtensions
{
    /// <summary>
    /// Initializes the database with migrations and seeds initial data
    /// </summary>
    public static async Task InitializeDatabaseAsync(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<ApplicationDbContext>>();

        try
        {
            logger.LogInformation("Starting database initialization");

            // Apply any pending migrations
            var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
            if (pendingMigrations.Any())
            {
                logger.LogInformation("Applying {Count} pending migrations", pendingMigrations.Count());
                await context.Database.MigrateAsync();
                logger.LogInformation("Database migrations applied successfully");
            }
            else
            {
                logger.LogInformation("No pending migrations found");
            }

            // Seed roles and permissions
            var roleSeeder = new RoleSeeder(
                context,
                scope.ServiceProvider.GetRequiredService<ILogger<RoleSeeder>>());

            await roleSeeder.SeedAsync();

            logger.LogInformation("Database initialization completed successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while initializing the database");
            throw;
        }
    }
}
