namespace EMR.Domain.ReadModels;

/// <summary>
/// Read model optimized for patient lists and search operations
/// Denormalized for query performance
/// </summary>
public class PatientSummaryReadModel : BaseReadModel
{
    /// <summary>
    /// Medical Record Number (unique identifier)
    /// </summary>
    public string MRN { get; set; } = string.Empty;

    /// <summary>
    /// Patient's first name
    /// </summary>
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// Patient's last name
    /// </summary>
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// Patient's full name (denormalized for search)
    /// </summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// Date of birth
    /// </summary>
    public DateTime DateOfBirth { get; set; }

    /// <summary>
    /// Patient's age (calculated field for performance)
    /// </summary>
    public int Age { get; set; }

    /// <summary>
    /// Gender (Male, Female, Other, Unknown)
    /// </summary>
    public string Gender { get; set; } = string.Empty;

    /// <summary>
    /// Primary phone number
    /// </summary>
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// Email address
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Current status (Active, Inactive, Deceased)
    /// </summary>
    public string Status { get; set; } = "Active";

    /// <summary>
    /// Primary care provider name (denormalized)
    /// </summary>
    public string? PrimaryCareProvider { get; set; }

    /// <summary>
    /// Primary care provider ID
    /// </summary>
    public Guid? PrimaryCareProviderId { get; set; }

    /// <summary>
    /// Last visit date
    /// </summary>
    public DateTime? LastVisitDate { get; set; }

    /// <summary>
    /// Number of active alerts/flags
    /// </summary>
    public int ActiveAlertsCount { get; set; }

    /// <summary>
    /// Has active allergies flag
    /// </summary>
    public bool HasActiveAllergies { get; set; }

    /// <summary>
    /// Has active medications flag
    /// </summary>
    public bool HasActiveMedications { get; set; }

    /// <summary>
    /// Full address text (denormalized for search)
    /// </summary>
    public string? FullAddress { get; set; }

    /// <summary>
    /// City
    /// </summary>
    public string? City { get; set; }

    /// <summary>
    /// State/Province
    /// </summary>
    public string? State { get; set; }

    /// <summary>
    /// Postal/ZIP code
    /// </summary>
    public string? PostalCode { get; set; }

    /// <summary>
    /// Insurance provider name (primary)
    /// </summary>
    public string? PrimaryInsurance { get; set; }

    /// <summary>
    /// When the patient record was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Search field combining multiple text fields for full-text search
    /// </summary>
    public string SearchText { get; set; } = string.Empty;
}
