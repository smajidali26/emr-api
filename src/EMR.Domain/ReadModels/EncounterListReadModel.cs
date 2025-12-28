namespace EMR.Domain.ReadModels;

/// <summary>
/// Read model optimized for encounter history and lists
/// Denormalized for query performance
/// </summary>
public class EncounterListReadModel : BaseReadModel
{
    /// <summary>
    /// Patient ID
    /// </summary>
    public Guid PatientId { get; set; }

    /// <summary>
    /// Patient MRN (denormalized for search)
    /// </summary>
    public string PatientMRN { get; set; } = string.Empty;

    /// <summary>
    /// Patient name (denormalized)
    /// </summary>
    public string PatientName { get; set; } = string.Empty;

    /// <summary>
    /// Encounter number (unique identifier)
    /// </summary>
    public string EncounterNumber { get; set; } = string.Empty;

    /// <summary>
    /// Encounter type (Outpatient, Inpatient, Emergency, etc.)
    /// </summary>
    public string EncounterType { get; set; } = string.Empty;

    /// <summary>
    /// Encounter status (Scheduled, In Progress, Completed, Cancelled)
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Scheduled/Start date and time
    /// </summary>
    public DateTime ScheduledAt { get; set; }

    /// <summary>
    /// Actual start date and time
    /// </summary>
    public DateTime? StartedAt { get; set; }

    /// <summary>
    /// Completed/End date and time
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Primary provider ID
    /// </summary>
    public Guid ProviderId { get; set; }

    /// <summary>
    /// Primary provider name (denormalized)
    /// </summary>
    public string ProviderName { get; set; } = string.Empty;

    /// <summary>
    /// Provider specialty (denormalized)
    /// </summary>
    public string ProviderSpecialty { get; set; } = string.Empty;

    /// <summary>
    /// Department/Location
    /// </summary>
    public string Department { get; set; } = string.Empty;

    /// <summary>
    /// Facility name
    /// </summary>
    public string Facility { get; set; } = string.Empty;

    /// <summary>
    /// Chief complaint
    /// </summary>
    public string ChiefComplaint { get; set; } = string.Empty;

    /// <summary>
    /// Visit reason
    /// </summary>
    public string? VisitReason { get; set; }

    /// <summary>
    /// Primary diagnosis code
    /// </summary>
    public string? PrimaryDiagnosisCode { get; set; }

    /// <summary>
    /// Primary diagnosis description
    /// </summary>
    public string? PrimaryDiagnosisDescription { get; set; }

    /// <summary>
    /// Number of diagnoses
    /// </summary>
    public int DiagnosisCount { get; set; }

    /// <summary>
    /// Number of procedures
    /// </summary>
    public int ProcedureCount { get; set; }

    /// <summary>
    /// Number of orders
    /// </summary>
    public int OrderCount { get; set; }

    /// <summary>
    /// Has clinical notes
    /// </summary>
    public bool HasClinicalNotes { get; set; }

    /// <summary>
    /// Has prescriptions
    /// </summary>
    public bool HasPrescriptions { get; set; }

    /// <summary>
    /// Admission type (for inpatient encounters)
    /// </summary>
    public string? AdmissionType { get; set; }

    /// <summary>
    /// Discharge disposition (for completed encounters)
    /// </summary>
    public string? DischargeDisposition { get; set; }

    /// <summary>
    /// Length of stay in days (for inpatient)
    /// </summary>
    public int? LengthOfStay { get; set; }

    /// <summary>
    /// Insurance authorization number
    /// </summary>
    public string? AuthorizationNumber { get; set; }

    /// <summary>
    /// Billing status
    /// </summary>
    public string BillingStatus { get; set; } = "Pending";

    /// <summary>
    /// Is encounter locked/finalized
    /// </summary>
    public bool IsLocked { get; set; }

    /// <summary>
    /// When the encounter was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Search text for full-text search
    /// </summary>
    public string SearchText { get; set; } = string.Empty;
}
