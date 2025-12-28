using EMR.Application.Common.Interfaces;
using EMR.Infrastructure.Data;
using EMR.Infrastructure.Projections;
using EMR.Infrastructure.Repositories.ReadModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EMR.Infrastructure.Configuration;

/// <summary>
/// Extension methods for configuring CQRS read model infrastructure
/// </summary>
public static class CqrsConfiguration
{
    /// <summary>
    /// Adds CQRS read model services to the dependency injection container
    /// </summary>
    public static IServiceCollection AddCqrsReadModels(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Read database context (separate from write context)
        services.AddDbContext<ReadDbContext>((serviceProvider, options) =>
        {
            var connectionString = configuration.GetConnectionString("ReadDatabase")
                ?? configuration.GetConnectionString("DefaultConnection");

            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "read");
                npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorCodesToAdd: null);
            });

            // Read models are optimized for queries - no tracking needed
            options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
        });

        // Register read model repositories
        services.AddScoped(typeof(IReadModelRepository<>), typeof(ReadModelRepository<>));
        services.AddScoped<IPatientReadModelRepository, PatientReadModelRepository>();
        services.AddScoped<IPatientDetailReadModelRepository, PatientDetailReadModelRepository>();
        services.AddScoped<IEncounterReadModelRepository, EncounterReadModelRepository>();
        services.AddScoped<IOrderReadModelRepository, OrderReadModelRepository>();
        services.AddScoped<IProviderScheduleReadModelRepository, ProviderScheduleReadModelRepository>();

        // Register projection infrastructure
        services.AddSingleton<IEventualConsistencyManager, EventualConsistencyManager>();
        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();
        services.AddScoped<IProjectionRebuilder, ProjectionRebuilder>();

        // Register read model builders
        services.AddScoped(typeof(Application.Common.Abstractions.IReadModelBuilder<>), typeof(ReadModelBuilder<>));

        return services;
    }

    /// <summary>
    /// Adds separate connection string for read database
    /// Useful for read replicas and CQRS scaling
    /// </summary>
    public static IServiceCollection AddSeparateReadDatabase(
        this IServiceCollection services,
        string readConnectionString)
    {
        if (string.IsNullOrWhiteSpace(readConnectionString))
        {
            throw new ArgumentException("Read connection string cannot be empty", nameof(readConnectionString));
        }

        services.AddDbContext<ReadDbContext>(options =>
        {
            options.UseNpgsql(readConnectionString, npgsqlOptions =>
            {
                npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "read");
                npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorCodesToAdd: null);
            });

            options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
        });

        return services;
    }

    /// <summary>
    /// Ensures read database is created and migrations are applied
    /// Should be called during application startup
    /// </summary>
    public static async Task InitializeReadDatabaseAsync(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReadDbContext>();

        // Apply pending migrations
        await context.Database.MigrateAsync();
    }
}
