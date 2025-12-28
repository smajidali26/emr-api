using EMR.Application.Abstractions.EventSourcing;
using EMR.Domain.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace EMR.Infrastructure.EventSourcing;

/// <summary>
/// Event publisher implementation using MediatR.
/// Publishes domain events as MediatR notifications for in-process handling.
/// </summary>
public class MediatREventPublisher : IEventPublisher
{
    private readonly IPublisher _publisher;
    private readonly ILogger<MediatREventPublisher> _logger;

    public MediatREventPublisher(
        IPublisher publisher,
        ILogger<MediatREventPublisher> logger)
    {
        _publisher = publisher ?? throw new ArgumentNullException(nameof(publisher));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Publishes a single domain event.
    /// </summary>
    public async Task PublishAsync(
        IDomainEvent domainEvent,
        CancellationToken cancellationToken = default)
    {
        if (domainEvent == null)
        {
            throw new ArgumentNullException(nameof(domainEvent));
        }

        try
        {
            _logger.LogDebug(
                "Publishing domain event {EventType} with ID {EventId}",
                domainEvent.GetType().Name,
                domainEvent.EventId);

            await _publisher.Publish(domainEvent, cancellationToken);

            _logger.LogInformation(
                "Successfully published domain event {EventType} with ID {EventId}",
                domainEvent.GetType().Name,
                domainEvent.EventId);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error publishing domain event {EventType} with ID {EventId}",
                domainEvent.GetType().Name,
                domainEvent.EventId);
            throw;
        }
    }

    /// <summary>
    /// Publishes multiple domain events in order.
    /// </summary>
    public async Task PublishAsync(
        IEnumerable<IDomainEvent> domainEvents,
        CancellationToken cancellationToken = default)
    {
        if (domainEvents == null)
        {
            throw new ArgumentNullException(nameof(domainEvents));
        }

        var eventsList = domainEvents.ToList();
        if (!eventsList.Any())
        {
            return;
        }

        _logger.LogDebug(
            "Publishing {EventCount} domain events",
            eventsList.Count);

        foreach (var domainEvent in eventsList)
        {
            await PublishAsync(domainEvent, cancellationToken);
        }

        _logger.LogInformation(
            "Successfully published {EventCount} domain events",
            eventsList.Count);
    }

    /// <summary>
    /// Publishes events to external message bus.
    /// This is a placeholder - implement with your message bus (RabbitMQ, Azure Service Bus, etc.)
    /// </summary>
    public async Task PublishToMessageBusAsync(
        IDomainEvent domainEvent,
        CancellationToken cancellationToken = default)
    {
        if (domainEvent == null)
        {
            throw new ArgumentNullException(nameof(domainEvent));
        }

        _logger.LogInformation(
            "Publishing domain event {EventType} to message bus (not yet implemented)",
            domainEvent.GetType().Name);

        // TODO: Implement message bus publishing
        // Example:
        // await _messageBus.PublishAsync(domainEvent, cancellationToken);

        await Task.CompletedTask;
    }
}
