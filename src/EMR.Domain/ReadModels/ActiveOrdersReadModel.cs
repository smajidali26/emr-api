namespace EMR.Domain.ReadModels;

/// <summary>
/// Read model optimized for order worklists
/// Denormalized for query performance
/// </summary>
public class ActiveOrdersReadModel : BaseReadModel
{
    /// <summary>
    /// Patient ID
    /// </summary>
    public Guid PatientId { get; set; }

    /// <summary>
    /// Patient MRN (denormalized)
    /// </summary>
    public string PatientMRN { get; set; } = string.Empty;

    /// <summary>
    /// Patient name (denormalized)
    /// </summary>
    public string PatientName { get; set; } = string.Empty;

    /// <summary>
    /// Patient age (denormalized)
    /// </summary>
    public int PatientAge { get; set; }

    /// <summary>
    /// Patient gender (denormalized)
    /// </summary>
    public string PatientGender { get; set; } = string.Empty;

    /// <summary>
    /// Encounter ID
    /// </summary>
    public Guid EncounterId { get; set; }

    /// <summary>
    /// Encounter number (denormalized)
    /// </summary>
    public string EncounterNumber { get; set; } = string.Empty;

    /// <summary>
    /// Order number (unique identifier)
    /// </summary>
    public string OrderNumber { get; set; } = string.Empty;

    /// <summary>
    /// Order type (Lab, Radiology, Medication, Procedure, etc.)
    /// </summary>
    public string OrderType { get; set; } = string.Empty;

    /// <summary>
    /// Order category (for grouping)
    /// </summary>
    public string OrderCategory { get; set; } = string.Empty;

    /// <summary>
    /// Order description
    /// </summary>
    public string OrderDescription { get; set; } = string.Empty;

    /// <summary>
    /// Order status (Pending, In Progress, Completed, Cancelled)
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Priority (Routine, STAT, Urgent, ASAP)
    /// </summary>
    public string Priority { get; set; } = "Routine";

    /// <summary>
    /// When the order was placed
    /// </summary>
    public DateTime OrderedAt { get; set; }

    /// <summary>
    /// Ordering provider ID
    /// </summary>
    public Guid OrderingProviderId { get; set; }

    /// <summary>
    /// Ordering provider name (denormalized)
    /// </summary>
    public string OrderingProviderName { get; set; } = string.Empty;

    /// <summary>
    /// Scheduled date/time for the order
    /// </summary>
    public DateTime? ScheduledFor { get; set; }

    /// <summary>
    /// When the order was started
    /// </summary>
    public DateTime? StartedAt { get; set; }

    /// <summary>
    /// When the order was completed
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Performing department
    /// </summary>
    public string? PerformingDepartment { get; set; }

    /// <summary>
    /// Performing location
    /// </summary>
    public string? PerformingLocation { get; set; }

    /// <summary>
    /// Assigned to user/tech ID
    /// </summary>
    public Guid? AssignedToId { get; set; }

    /// <summary>
    /// Assigned to user/tech name
    /// </summary>
    public string? AssignedToName { get; set; }

    /// <summary>
    /// Clinical indication/reason
    /// </summary>
    public string? ClinicalIndication { get; set; }

    /// <summary>
    /// Special instructions
    /// </summary>
    public string? SpecialInstructions { get; set; }

    /// <summary>
    /// Requires authorization
    /// </summary>
    public bool RequiresAuthorization { get; set; }

    /// <summary>
    /// Authorization status
    /// </summary>
    public string? AuthorizationStatus { get; set; }

    /// <summary>
    /// Authorization number
    /// </summary>
    public string? AuthorizationNumber { get; set; }

    /// <summary>
    /// Has results
    /// </summary>
    public bool HasResults { get; set; }

    /// <summary>
    /// Results status (Preliminary, Final, Amended)
    /// </summary>
    public string? ResultsStatus { get; set; }

    /// <summary>
    /// Critical flag
    /// </summary>
    public bool IsCritical { get; set; }

    /// <summary>
    /// Alert flags
    /// </summary>
    public List<string> AlertFlags { get; set; } = new();

    /// <summary>
    /// Estimated completion time
    /// </summary>
    public DateTime? EstimatedCompletionTime { get; set; }

    /// <summary>
    /// Department the patient is in (denormalized)
    /// </summary>
    public string? PatientLocation { get; set; }

    /// <summary>
    /// Room number (denormalized)
    /// </summary>
    public string? PatientRoom { get; set; }

    /// <summary>
    /// Age of order in hours (calculated)
    /// </summary>
    public double AgeInHours { get; set; }

    /// <summary>
    /// Is overdue based on priority and scheduled time
    /// </summary>
    public bool IsOverdue { get; set; }

    /// <summary>
    /// Search text for full-text search
    /// </summary>
    public string SearchText { get; set; } = string.Empty;

    /// <summary>
    /// When the order was created in the system
    /// </summary>
    public DateTime CreatedAt { get; set; }
}
