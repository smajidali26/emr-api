using EMR.Domain.Common;
using EMR.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EMR.Infrastructure.Projections;

/// <summary>
/// Dispatches domain events to projection handlers
/// Intercepts SaveChanges to publish events after successful persistence
/// </summary>
public interface IDomainEventDispatcher
{
    /// <summary>
    /// Dispatches all domain events from aggregates in the current context
    /// </summary>
    Task DispatchEventsAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Implementation of domain event dispatcher
/// </summary>
public class DomainEventDispatcher : IDomainEventDispatcher
{
    private readonly ApplicationDbContext _writeContext;
    private readonly IMediator _mediator;
    private readonly ILogger<DomainEventDispatcher> _logger;

    public DomainEventDispatcher(
        ApplicationDbContext writeContext,
        IMediator mediator,
        ILogger<DomainEventDispatcher> logger)
    {
        _writeContext = writeContext ?? throw new ArgumentNullException(nameof(writeContext));
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task DispatchEventsAsync(CancellationToken cancellationToken = default)
    {
        // Get all entities with domain events
        var aggregatesWithEvents = _writeContext.ChangeTracker
            .Entries<AggregateRoot>()
            .Where(e => e.Entity.DomainEvents.Any())
            .ToList();

        if (!aggregatesWithEvents.Any())
        {
            return;
        }

        // Collect all domain events
        var domainEvents = aggregatesWithEvents
            .SelectMany(e => e.Entity.DomainEvents)
            .ToList();

        _logger.LogInformation(
            "Dispatching {EventCount} domain events from {AggregateCount} aggregates",
            domainEvents.Count,
            aggregatesWithEvents.Count);

        // Clear events from aggregates (they've been collected)
        foreach (var aggregate in aggregatesWithEvents)
        {
            aggregate.Entity.ClearDomainEvents();
        }

        // Publish events to handlers (including projection handlers)
        foreach (var domainEvent in domainEvents)
        {
            try
            {
                _logger.LogDebug(
                    "Publishing domain event {EventType} (EventId: {EventId})",
                    domainEvent.GetType().Name,
                    domainEvent.EventId);

                await _mediator.Publish(domainEvent, cancellationToken);

                _logger.LogDebug(
                    "Successfully published domain event {EventType} (EventId: {EventId})",
                    domainEvent.GetType().Name,
                    domainEvent.EventId);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error publishing domain event {EventType} (EventId: {EventId}): {ErrorMessage}",
                    domainEvent.GetType().Name,
                    domainEvent.EventId,
                    ex.Message);

                // Continue with other events even if one fails
                // Failed events will be tracked by EventualConsistencyManager
            }
        }
    }
}
