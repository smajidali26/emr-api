using EMR.Domain.Common;

namespace EMR.Domain.Events.Encounter;

/// <summary>
/// Event raised when a patient encounter is completed.
/// Marks the end of a patient's visit to a healthcare facility.
/// </summary>
public sealed record EncounterCompletedEvent : DomainEventBase
{
    /// <summary>
    /// The unique identifier of the encounter
    /// </summary>
    public Guid EncounterId { get; init; }

    /// <summary>
    /// The patient involved in this encounter
    /// </summary>
    public Guid PatientId { get; init; }

    /// <summary>
    /// When the encounter was completed
    /// </summary>
    public DateTime EndTime { get; init; }

    /// <summary>
    /// Outcome of the encounter (Discharged, Admitted, Transferred, etc.)
    /// </summary>
    public string Outcome { get; init; } = string.Empty;

    /// <summary>
    /// Discharge disposition (Home, Another Facility, Deceased, etc.)
    /// </summary>
    public string? DischargeDisposition { get; init; }

    /// <summary>
    /// Summary of the encounter
    /// </summary>
    public string? EncounterSummary { get; init; }

    /// <summary>
    /// Follow-up instructions
    /// </summary>
    public string? FollowUpInstructions { get; init; }

    /// <summary>
    /// Duration of the encounter in minutes
    /// </summary>
    public int DurationMinutes { get; init; }

    /// <summary>
    /// Whether the encounter was completed normally or cancelled
    /// </summary>
    public bool WasCancelled { get; init; }

    /// <summary>
    /// Reason for cancellation if applicable
    /// </summary>
    public string? CancellationReason { get; init; }

    public EncounterCompletedEvent()
    {
    }

    public EncounterCompletedEvent(
        Guid encounterId,
        Guid patientId,
        DateTime endTime,
        string outcome,
        int durationMinutes,
        string? userId = null,
        string? correlationId = null,
        string? causationId = null)
        : base(userId, correlationId, causationId)
    {
        EncounterId = encounterId;
        PatientId = patientId;
        EndTime = endTime;
        Outcome = outcome;
        DurationMinutes = durationMinutes;
    }
}
