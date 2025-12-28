namespace EMR.Domain.Common;

/// <summary>
/// Marker interface for aggregate roots that can raise domain events
/// </summary>
public interface IAggregateRoot
{
    /// <summary>
    /// Collection of domain events raised by this aggregate
    /// </summary>
    IReadOnlyCollection<IDomainEvent> DomainEvents { get; }

    /// <summary>
    /// Clears all domain events
    /// </summary>
    void ClearDomainEvents();
}
