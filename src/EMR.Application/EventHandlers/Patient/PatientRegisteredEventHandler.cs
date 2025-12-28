using EMR.Application.Common.Events;
using EMR.Domain.Events.Patient;
using MediatR;
using Microsoft.Extensions.Logging;

namespace EMR.Application.EventHandlers.Patient;

/// <summary>
/// Handles the PatientRegisteredEvent to perform side effects and update read models.
/// Implements INotificationHandler for MediatR integration.
/// </summary>
public class PatientRegisteredEventHandler : INotificationHandler<DomainEventNotification<PatientRegisteredEvent>>
{
    private readonly ILogger<PatientRegisteredEventHandler> _logger;

    public PatientRegisteredEventHandler(ILogger<PatientRegisteredEventHandler> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Handles the patient registered event.
    /// This is where you would:
    /// - Update read models/projections
    /// - Send notifications (welcome email, etc.)
    /// - Trigger other business processes
    /// - Update analytics/reporting databases
    /// </summary>
    public async Task Handle(DomainEventNotification<PatientRegisteredEvent> notification, CancellationToken cancellationToken)
    {
        var @event = notification.DomainEvent;

        _logger.LogInformation(
            "Handling PatientRegisteredEvent for patient {PatientId} with MRN {MRN}. Event ID: {EventId}",
            @event.PatientId,
            @event.MedicalRecordNumber,
            @event.EventId);

        try
        {
            // TODO: Update read model for patient queries
            // Example: await _patientReadRepository.CreateAsync(MapToReadModel(notification));

            // TODO: Send welcome notification
            // Example: await _notificationService.SendWelcomeEmailAsync(notification.Email);

            // TODO: Record in audit log
            // Example: await _auditService.LogPatientRegistrationAsync(notification);

            _logger.LogInformation(
                "Successfully processed PatientRegisteredEvent for patient {PatientId}",
                @event.PatientId);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error processing PatientRegisteredEvent for patient {PatientId}",
                @event.PatientId);

            // Re-throw to allow retry mechanisms to handle the failure
            throw;
        }

        await Task.CompletedTask;
    }
}
