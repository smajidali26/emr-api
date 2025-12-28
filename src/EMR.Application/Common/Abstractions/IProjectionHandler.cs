using EMR.Domain.Common;

namespace EMR.Application.Common.Abstractions;

/// <summary>
/// Handler for processing domain events and updating read models
/// Combines MediatR notification handling with projection logic
/// </summary>
/// <typeparam name="TEvent">The domain event type</typeparam>
public interface IProjectionHandler<in TEvent> where TEvent : IDomainEvent
{
    /// <summary>
    /// Handles the domain event and updates the appropriate read models
    /// </summary>
    /// <param name="domainEvent">The domain event to handle</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task HandleAsync(TEvent domainEvent, CancellationToken cancellationToken = default);
}
