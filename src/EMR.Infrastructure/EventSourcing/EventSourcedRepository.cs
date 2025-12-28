using EMR.Application.Abstractions.EventSourcing;
using EMR.Domain.Common;
using Microsoft.Extensions.Logging;

namespace EMR.Infrastructure.EventSourcing;

/// <summary>
/// Repository for event-sourced aggregates.
/// Handles loading aggregates from event streams and persisting new events.
/// </summary>
/// <typeparam name="TAggregate">Type of the aggregate root</typeparam>
public class EventSourcedRepository<TAggregate> where TAggregate : AggregateRoot, new()
{
    private readonly IEventStore _eventStore;
    private readonly ISnapshotStore _snapshotStore;
    private readonly ILogger<EventSourcedRepository<TAggregate>> _logger;
    private readonly string _aggregateType;

    public EventSourcedRepository(
        IEventStore eventStore,
        ISnapshotStore snapshotStore,
        ILogger<EventSourcedRepository<TAggregate>> logger)
    {
        _eventStore = eventStore ?? throw new ArgumentNullException(nameof(eventStore));
        _snapshotStore = snapshotStore ?? throw new ArgumentNullException(nameof(snapshotStore));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _aggregateType = typeof(TAggregate).Name;
    }

    /// <summary>
    /// Loads an aggregate from the event store.
    /// Uses snapshots when available for performance.
    /// </summary>
    public async Task<TAggregate?> GetByIdAsync(
        Guid aggregateId,
        CancellationToken cancellationToken = default)
    {
        if (aggregateId == Guid.Empty)
        {
            throw new ArgumentException("Aggregate ID cannot be empty.", nameof(aggregateId));
        }

        try
        {
            var aggregate = new TAggregate();
            var fromVersion = 0;

            // Try to load from snapshot
            var snapshotResult = await _snapshotStore.GetSnapshotAsync<TAggregate>(
                aggregateId,
                cancellationToken);

            if (snapshotResult.HasValue)
            {
                aggregate = snapshotResult.Value.Snapshot;
                fromVersion = snapshotResult.Value.Version;

                _logger.LogDebug(
                    "Loaded {AggregateType} {AggregateId} from snapshot at version {Version}",
                    _aggregateType,
                    aggregateId,
                    fromVersion);
            }

            // Load events from snapshot version onwards
            var events = await _eventStore.GetEventsAsync(
                aggregateId,
                fromVersion > 0 ? fromVersion + 1 : null,
                cancellationToken);

            var eventsList = events.ToList();
            if (!eventsList.Any() && fromVersion == 0)
            {
                _logger.LogWarning(
                    "{AggregateType} with ID {AggregateId} not found",
                    _aggregateType,
                    aggregateId);
                return null;
            }

            // Apply events to aggregate
            if (aggregate != null && eventsList.Any())
            {
                aggregate.LoadFromHistory(eventsList);
            }

            _logger.LogInformation(
                "Loaded {AggregateType} {AggregateId} with {EventCount} events (version {Version})",
                _aggregateType,
                aggregateId,
                eventsList.Count,
                aggregate?.Version ?? 0);

            return aggregate;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error loading {AggregateType} with ID {AggregateId}",
                _aggregateType,
                aggregateId);
            throw;
        }
    }

    /// <summary>
    /// Saves an aggregate by persisting its uncommitted events to the event store.
    /// </summary>
    public async Task SaveAsync(
        TAggregate aggregate,
        CancellationToken cancellationToken = default)
    {
        if (aggregate == null)
        {
            throw new ArgumentNullException(nameof(aggregate));
        }

        var uncommittedEvents = aggregate.DomainEvents.ToList();
        if (!uncommittedEvents.Any())
        {
            _logger.LogDebug(
                "No uncommitted events for {AggregateType} {AggregateId}",
                _aggregateType,
                aggregate.Id);
            return;
        }

        try
        {
            // Calculate expected version (current version minus uncommitted events)
            var expectedVersion = aggregate.Version - uncommittedEvents.Count;

            // Append events to the event store
            await _eventStore.AppendEventsAsync(
                aggregate.Id,
                _aggregateType,
                uncommittedEvents,
                expectedVersion,
                cancellationToken);

            // Check if we should take a snapshot
            var lastSnapshotVersion = 0;
            var snapshotResult = await _snapshotStore.GetSnapshotAsync<TAggregate>(
                aggregate.Id,
                cancellationToken);

            if (snapshotResult.HasValue)
            {
                lastSnapshotVersion = snapshotResult.Value.Version;
            }

            if (_snapshotStore.ShouldTakeSnapshot(aggregate.Version, lastSnapshotVersion))
            {
                await _snapshotStore.SaveSnapshotAsync(
                    aggregate.Id,
                    aggregate,
                    aggregate.Version,
                    cancellationToken);

                _logger.LogInformation(
                    "Created snapshot for {AggregateType} {AggregateId} at version {Version}",
                    _aggregateType,
                    aggregate.Id,
                    aggregate.Version);
            }

            // Clear uncommitted events
            aggregate.ClearDomainEvents();

            _logger.LogInformation(
                "Saved {AggregateType} {AggregateId} with {EventCount} events (version {Version})",
                _aggregateType,
                aggregate.Id,
                uncommittedEvents.Count,
                aggregate.Version);
        }
        catch (ConcurrencyException ex)
        {
            _logger.LogError(
                ex,
                "Concurrency conflict saving {AggregateType} {AggregateId}",
                _aggregateType,
                aggregate.Id);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error saving {AggregateType} {AggregateId}",
                _aggregateType,
                aggregate.Id);
            throw;
        }
    }

    /// <summary>
    /// Checks if an aggregate exists in the event store.
    /// </summary>
    public async Task<bool> ExistsAsync(
        Guid aggregateId,
        CancellationToken cancellationToken = default)
    {
        return await _eventStore.AggregateExistsAsync(aggregateId, cancellationToken);
    }

    /// <summary>
    /// Gets the current version of an aggregate.
    /// </summary>
    public async Task<int> GetVersionAsync(
        Guid aggregateId,
        CancellationToken cancellationToken = default)
    {
        return await _eventStore.GetAggregateVersionAsync(aggregateId, cancellationToken);
    }
}
