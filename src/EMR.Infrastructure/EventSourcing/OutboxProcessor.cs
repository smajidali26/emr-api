using EMR.Application.Abstractions.EventSourcing;
using EMR.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EMR.Infrastructure.EventSourcing;

/// <summary>
/// Background service that processes outbox messages for reliable event publishing.
/// Implements the Transactional Outbox pattern.
/// </summary>
public class OutboxProcessor : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OutboxProcessor> _logger;
    private readonly TimeSpan _processingInterval = TimeSpan.FromSeconds(5);
    private readonly int _batchSize = 100;
    private readonly int _maxRetries = 5;

    public OutboxProcessor(
        IServiceProvider serviceProvider,
        ILogger<OutboxProcessor> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Outbox processor started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessOutboxMessagesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing outbox messages");
            }

            await Task.Delay(_processingInterval, stoppingToken);
        }

        _logger.LogInformation("Outbox processor stopped");
    }

    private async Task ProcessOutboxMessagesAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<EventStoreDbContext>();
        var eventPublisher = scope.ServiceProvider.GetRequiredService<IEventPublisher>();
        var serializer = scope.ServiceProvider.GetRequiredService<IEventSerializer>();

        // Get unprocessed messages or messages due for retry
        var messages = await context.OutboxMessages
            .Where(m => !m.IsProcessed &&
                       m.ProcessingAttempts < _maxRetries &&
                       (m.NextRetryAt == null || m.NextRetryAt <= DateTime.UtcNow))
            .OrderBy(m => m.CreatedAt)
            .Take(_batchSize)
            .ToListAsync(cancellationToken);

        if (!messages.Any())
        {
            return;
        }

        _logger.LogInformation(
            "Processing {MessageCount} outbox messages",
            messages.Count);

        foreach (var message in messages)
        {
            try
            {
                // Deserialize the event
                var domainEvent = serializer.Deserialize(message.EventData, message.EventType);

                // Publish to message bus
                await eventPublisher.PublishToMessageBusAsync(domainEvent, cancellationToken);

                // Mark as processed
                message.IsProcessed = true;
                message.ProcessedAt = DateTime.UtcNow;
                message.ProcessingAttempts++;

                _logger.LogDebug(
                    "Successfully processed outbox message {MessageId} for event {EventId}",
                    message.Id,
                    message.EventId);
            }
            catch (Exception ex)
            {
                message.ProcessingAttempts++;
                message.LastError = ex.Message;
                message.NextRetryAt = CalculateNextRetryTime(message.ProcessingAttempts);

                _logger.LogError(
                    ex,
                    "Error processing outbox message {MessageId} (attempt {Attempt}/{MaxRetries})",
                    message.Id,
                    message.ProcessingAttempts,
                    _maxRetries);
            }
        }

        await context.SaveChangesAsync(cancellationToken);

        var successCount = messages.Count(m => m.IsProcessed);
        _logger.LogInformation(
            "Processed {SuccessCount}/{TotalCount} outbox messages successfully",
            successCount,
            messages.Count);
    }

    private DateTime CalculateNextRetryTime(int attemptNumber)
    {
        // Exponential backoff: 1min, 2min, 4min, 8min, 16min
        var delayMinutes = Math.Pow(2, attemptNumber - 1);
        return DateTime.UtcNow.AddMinutes(delayMinutes);
    }
}
