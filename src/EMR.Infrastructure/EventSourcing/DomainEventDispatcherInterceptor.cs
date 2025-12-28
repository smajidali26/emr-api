using EMR.Application.Abstractions.EventSourcing;
using EMR.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;

namespace EMR.Infrastructure.EventSourcing;

/// <summary>
/// EF Core interceptor that automatically publishes domain events after SaveChanges.
/// Integrates event sourcing with the application data context.
/// </summary>
public class DomainEventDispatcherInterceptor : SaveChangesInterceptor
{
    private readonly IEventPublisher _eventPublisher;
    private readonly ILogger<DomainEventDispatcherInterceptor> _logger;

    public DomainEventDispatcherInterceptor(
        IEventPublisher eventPublisher,
        ILogger<DomainEventDispatcherInterceptor> logger)
    {
        _eventPublisher = eventPublisher ?? throw new ArgumentNullException(nameof(eventPublisher));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        // Collect domain events before saving
        var domainEvents = CollectDomainEvents(eventData.Context);

        // Store events for publishing after successful save
        if (eventData.Context != null && domainEvents.Any())
        {
            eventData.Context.ChangeTracker.AutoDetectChangesEnabled = false;
        }

        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        var domainEvents = CollectDomainEvents(eventData.Context);

        // Publish events after successful save
        if (domainEvents.Any())
        {
            _logger.LogDebug(
                "Publishing {EventCount} domain events after SaveChanges",
                domainEvents.Count);

            try
            {
                await _eventPublisher.PublishAsync(domainEvents, cancellationToken);

                // Clear domain events from aggregates
                ClearDomainEvents(eventData.Context);

                _logger.LogInformation(
                    "Successfully published {EventCount} domain events",
                    domainEvents.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error publishing domain events after SaveChanges");
                // Don't throw - events are already persisted in event store
                // They can be republished from the event store if needed
            }
        }

        if (eventData.Context != null)
        {
            eventData.Context.ChangeTracker.AutoDetectChangesEnabled = true;
        }

        return await base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    private List<IDomainEvent> CollectDomainEvents(DbContext? context)
    {
        if (context == null)
        {
            return new List<IDomainEvent>();
        }

        var domainEvents = new List<IDomainEvent>();

        var aggregates = context.ChangeTracker
            .Entries<AggregateRoot>()
            .Where(e => e.Entity.DomainEvents.Any())
            .Select(e => e.Entity)
            .ToList();

        foreach (var aggregate in aggregates)
        {
            domainEvents.AddRange(aggregate.DomainEvents);
        }

        return domainEvents;
    }

    private void ClearDomainEvents(DbContext? context)
    {
        if (context == null)
        {
            return;
        }

        var aggregates = context.ChangeTracker
            .Entries<AggregateRoot>()
            .Where(e => e.Entity.DomainEvents.Any())
            .Select(e => e.Entity)
            .ToList();

        foreach (var aggregate in aggregates)
        {
            aggregate.ClearDomainEvents();
        }
    }
}
