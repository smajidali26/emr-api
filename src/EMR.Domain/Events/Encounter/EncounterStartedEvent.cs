using EMR.Domain.Common;

namespace EMR.Domain.Events.Encounter;

/// <summary>
/// Event raised when a new patient encounter is started.
/// An encounter represents a patient's visit to a healthcare facility.
/// </summary>
public sealed record EncounterStartedEvent : DomainEventBase
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
    /// The healthcare provider responsible for this encounter
    /// </summary>
    public Guid ProviderId { get; init; }

    /// <summary>
    /// Type of encounter (Inpatient, Outpatient, Emergency, etc.)
    /// </summary>
    public string EncounterType { get; init; } = string.Empty;

    /// <summary>
    /// When the encounter started
    /// </summary>
    public DateTime StartTime { get; init; }

    /// <summary>
    /// Facility or location where encounter is taking place
    /// </summary>
    public string? FacilityId { get; init; }

    /// <summary>
    /// Department where encounter is taking place
    /// </summary>
    public string? Department { get; init; }

    /// <summary>
    /// Reason for the encounter/visit
    /// </summary>
    public string? ChiefComplaint { get; init; }

    /// <summary>
    /// Priority level of the encounter
    /// </summary>
    public string? Priority { get; init; }

    /// <summary>
    /// Admission source (Emergency, Referral, etc.)
    /// </summary>
    public string? AdmissionSource { get; init; }

    public EncounterStartedEvent()
    {
    }

    public EncounterStartedEvent(
        Guid encounterId,
        Guid patientId,
        Guid providerId,
        string encounterType,
        DateTime startTime,
        string? userId = null,
        string? correlationId = null,
        string? causationId = null)
        : base(userId, correlationId, causationId)
    {
        EncounterId = encounterId;
        PatientId = patientId;
        ProviderId = providerId;
        EncounterType = encounterType;
        StartTime = startTime;
    }
}
