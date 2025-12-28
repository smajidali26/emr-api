using EMR.Domain.Common;

namespace EMR.Application.Common.Abstractions;

/// <summary>
/// Base interface for projections that update read models from domain events
/// </summary>
/// <typeparam name="TEvent">The domain event type</typeparam>
public interface IProjection<in TEvent> where TEvent : IDomainEvent
{
    /// <summary>
    /// Projects the domain event to update the read model
    /// </summary>
    /// <param name="domainEvent">The domain event to project</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task ProjectAsync(TEvent domainEvent, CancellationToken cancellationToken = default);
}
