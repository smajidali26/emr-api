using System.Collections.Concurrent;

namespace EMR.Domain.Common;

/// <summary>
/// Base class for aggregate roots in DDD.
/// Aggregates are the primary unit of consistency and transaction boundaries.
/// This implementation supports event sourcing by maintaining a collection of uncommitted domain events.
/// </summary>
public abstract class AggregateRoot : BaseEntity
{
    private readonly ConcurrentQueue<IDomainEvent> _domainEvents = new();

    /// <summary>
    /// Version number for the aggregate (incremented with each event)
    /// Used for optimistic concurrency control in event sourcing
    /// </summary>
    public int Version { get; protected set; }

    /// <summary>
    /// Gets all uncommitted domain events that have been raised by this aggregate
    /// </summary>
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.ToArray();

    /// <summary>
    /// Adds a domain event to the uncommitted events collection.
    /// Events will be published after the aggregate is successfully persisted.
    /// </summary>
    /// <param name="domainEvent">The domain event to add</param>
    protected void AddDomainEvent(IDomainEvent domainEvent)
    {
        if (domainEvent == null)
        {
            throw new ArgumentNullException(nameof(domainEvent));
        }

        _domainEvents.Enqueue(domainEvent);
        Version++;
    }

    /// <summary>
    /// Clears all uncommitted domain events.
    /// This should be called after events have been successfully published.
    /// </summary>
    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }

    /// <summary>
    /// Applies an event to the aggregate state without adding it to uncommitted events.
    /// Used when replaying events from the event store.
    /// </summary>
    /// <param name="domainEvent">The event to apply</param>
    protected virtual void ApplyEvent(IDomainEvent domainEvent)
    {
        // Override in derived classes to apply event-specific state changes
        Version++;
    }

    /// <summary>
    /// Loads the aggregate from a history of events.
    /// Used for event sourcing reconstruction.
    /// </summary>
    /// <param name="history">Collection of historical events</param>
    public void LoadFromHistory(IEnumerable<IDomainEvent> history)
    {
        if (history == null)
        {
            throw new ArgumentNullException(nameof(history));
        }

        foreach (var domainEvent in history)
        {
            ApplyEvent(domainEvent);
        }
    }

    /// <summary>
    /// Raises a new domain event and applies it to the aggregate state.
    /// This is the primary method for state changes in event-sourced aggregates.
    /// </summary>
    /// <param name="domainEvent">The event to raise and apply</param>
    protected void RaiseEvent(IDomainEvent domainEvent)
    {
        ApplyEvent(domainEvent);
        AddDomainEvent(domainEvent);
    }
}
