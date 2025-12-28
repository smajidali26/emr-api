using EMR.Application.Abstractions.EventSourcing;
using EMR.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EMR.Infrastructure.EventSourcing;

/// <summary>
/// Extension methods for configuring Event Sourcing services.
/// </summary>
public static class EventSourcingServiceCollectionExtensions
{
    /// <summary>
    /// Adds Event Sourcing infrastructure to the service collection.
    /// </summary>
    public static IServiceCollection AddEventSourcing(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Add Event Store DbContext
        var connectionString = configuration.GetConnectionString("EventStoreConnection")
            ?? configuration.GetConnectionString("DefaultConnection");

        services.AddDbContext<EventStoreDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(5),
                    errorCodesToAdd: null);
            });
        });

        // Register Event Sourcing Services
        services.AddSingleton<IEventSerializer, EventSerializer>();
        services.AddScoped<IEventStore, SqlEventStore>();
        services.AddScoped<ISnapshotStore, SqlSnapshotStore>();
        services.AddScoped<IEventPublisher, MediatREventPublisher>();
        services.AddScoped<IEventReplay, EventReplay>();

        // Register Domain Event Dispatcher Interceptor
        services.AddScoped<DomainEventDispatcherInterceptor>();

        // Register Outbox Processor as Hosted Service
        services.AddHostedService<OutboxProcessor>();

        return services;
    }

    /// <summary>
    /// Adds the Event Store database context with the domain event dispatcher interceptor.
    /// Call this in addition to AddEventSourcing if you want automatic event publishing.
    /// </summary>
    public static IServiceCollection AddEventStoreWithInterceptor(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("EventStoreConnection")
            ?? configuration.GetConnectionString("DefaultConnection");

        services.AddDbContext<EventStoreDbContext>((serviceProvider, options) =>
        {
            var interceptor = serviceProvider.GetRequiredService<DomainEventDispatcherInterceptor>();

            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(5),
                    errorCodesToAdd: null);
            })
            .AddInterceptors(interceptor);
        });

        return services;
    }
}
