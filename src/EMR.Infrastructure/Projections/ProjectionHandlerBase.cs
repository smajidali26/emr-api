using EMR.Application.Common.Abstractions;
using EMR.Domain.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace EMR.Infrastructure.Projections;

/// <summary>
/// Base class for projection handlers that update read models from domain events
/// Implements both INotificationHandler (MediatR) and IProjectionHandler
/// </summary>
/// <typeparam name="TEvent">The domain event type</typeparam>
public abstract class ProjectionHandlerBase<TEvent> : INotificationHandler<TEvent>, IProjectionHandler<TEvent>
    where TEvent : IDomainEvent
{
    protected readonly ILogger Logger;

    protected ProjectionHandlerBase(ILogger logger)
    {
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// MediatR notification handler - delegates to HandleAsync
    /// </summary>
    public async Task Handle(TEvent notification, CancellationToken cancellationToken)
    {
        await HandleAsync(notification, cancellationToken);
    }

    /// <summary>
    /// Handles the domain event and updates read models
    /// Override this in derived classes to implement projection logic
    /// </summary>
    public async Task HandleAsync(TEvent domainEvent, CancellationToken cancellationToken = default)
    {
        try
        {
            Logger.LogInformation(
                "Processing projection for event {EventType} (EventId: {EventId})",
                typeof(TEvent).Name,
                domainEvent.EventId);

            await ProjectAsync(domainEvent, cancellationToken);

            Logger.LogInformation(
                "Successfully processed projection for event {EventType} (EventId: {EventId})",
                typeof(TEvent).Name,
                domainEvent.EventId);
        }
        catch (Exception ex)
        {
            Logger.LogError(
                ex,
                "Error processing projection for event {EventType} (EventId: {EventId}): {ErrorMessage}",
                typeof(TEvent).Name,
                domainEvent.EventId,
                ex.Message);

            // Re-throw to allow retry mechanism to handle
            throw;
        }
    }

    /// <summary>
    /// Override this method to implement the actual projection logic
    /// </summary>
    protected abstract Task ProjectAsync(TEvent domainEvent, CancellationToken cancellationToken);
}
