using EMR.Domain.Common;

namespace EMR.Domain.Events.Patient;

/// <summary>
/// Event raised when patient demographic information is updated.
/// Captures changes to patient's personal information.
/// </summary>
public sealed record PatientDemographicsUpdatedEvent : DomainEventBase
{
    /// <summary>
    /// The unique identifier of the patient
    /// </summary>
    public Guid PatientId { get; init; }

    /// <summary>
    /// Updated first name (null if not changed)
    /// </summary>
    public string? FirstName { get; init; }

    /// <summary>
    /// Updated last name (null if not changed)
    /// </summary>
    public string? LastName { get; init; }

    /// <summary>
    /// Updated email (null if not changed)
    /// </summary>
    public string? Email { get; init; }

    /// <summary>
    /// Updated phone number (null if not changed)
    /// </summary>
    public string? PhoneNumber { get; init; }

    /// <summary>
    /// Updated address (null if not changed)
    /// </summary>
    public string? Address { get; init; }

    /// <summary>
    /// Updated emergency contact name (null if not changed)
    /// </summary>
    public string? EmergencyContactName { get; init; }

    /// <summary>
    /// Updated emergency contact phone (null if not changed)
    /// </summary>
    public string? EmergencyContactPhone { get; init; }

    /// <summary>
    /// Dictionary of previous values for audit purposes
    /// </summary>
    public IDictionary<string, string>? PreviousValues { get; init; }

    /// <summary>
    /// Reason for the update
    /// </summary>
    public string? UpdateReason { get; init; }

    public PatientDemographicsUpdatedEvent()
    {
    }

    public PatientDemographicsUpdatedEvent(
        Guid patientId,
        string? userId = null,
        string? correlationId = null,
        string? causationId = null)
        : base(userId, correlationId, causationId)
    {
        PatientId = patientId;
    }
}
