namespace EMR.Domain.ReadModels;

/// <summary>
/// Read model optimized for patient detail view
/// Contains comprehensive patient information
/// </summary>
public class PatientDetailReadModel : BaseReadModel
{
    /// <summary>
    /// Medical Record Number
    /// </summary>
    public string MRN { get; set; } = string.Empty;

    /// <summary>
    /// Patient's first name
    /// </summary>
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// Patient's middle name
    /// </summary>
    public string? MiddleName { get; set; }

    /// <summary>
    /// Patient's last name
    /// </summary>
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// Full name
    /// </summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// Preferred name
    /// </summary>
    public string? PreferredName { get; set; }

    /// <summary>
    /// Date of birth
    /// </summary>
    public DateTime DateOfBirth { get; set; }

    /// <summary>
    /// Age
    /// </summary>
    public int Age { get; set; }

    /// <summary>
    /// Gender
    /// </summary>
    public string Gender { get; set; } = string.Empty;

    /// <summary>
    /// Biological sex
    /// </summary>
    public string? BiologicalSex { get; set; }

    /// <summary>
    /// Preferred pronouns
    /// </summary>
    public string? PreferredPronouns { get; set; }

    /// <summary>
    /// Social Security Number (encrypted/masked)
    /// </summary>
    public string? SSN { get; set; }

    /// <summary>
    /// Primary phone number
    /// </summary>
    public string? PrimaryPhone { get; set; }

    /// <summary>
    /// Secondary phone number
    /// </summary>
    public string? SecondaryPhone { get; set; }

    /// <summary>
    /// Email address
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Preferred contact method
    /// </summary>
    public string? PreferredContactMethod { get; set; }

    /// <summary>
    /// Current status
    /// </summary>
    public string Status { get; set; } = "Active";

    /// <summary>
    /// Primary care provider
    /// </summary>
    public ProviderInfo? PrimaryCareProvider { get; set; }

    /// <summary>
    /// Address information
    /// </summary>
    public AddressInfo? Address { get; set; }

    /// <summary>
    /// Emergency contact information
    /// </summary>
    public EmergencyContactInfo? EmergencyContact { get; set; }

    /// <summary>
    /// Primary insurance
    /// </summary>
    public InsuranceInfo? PrimaryInsurance { get; set; }

    /// <summary>
    /// Secondary insurance
    /// </summary>
    public InsuranceInfo? SecondaryInsurance { get; set; }

    /// <summary>
    /// Active allergies (denormalized list)
    /// </summary>
    public List<AllergyInfo> ActiveAllergies { get; set; } = new();

    /// <summary>
    /// Active medications (denormalized list)
    /// </summary>
    public List<MedicationInfo> ActiveMedications { get; set; } = new();

    /// <summary>
    /// Active problems/diagnoses (denormalized list)
    /// </summary>
    public List<ProblemInfo> ActiveProblems { get; set; } = new();

    /// <summary>
    /// Recent vital signs
    /// </summary>
    public VitalSignsInfo? RecentVitals { get; set; }

    /// <summary>
    /// Last visit information
    /// </summary>
    public VisitInfo? LastVisit { get; set; }

    /// <summary>
    /// Next scheduled appointment
    /// </summary>
    public AppointmentInfo? NextAppointment { get; set; }

    /// <summary>
    /// Patient alerts/flags
    /// </summary>
    public List<AlertInfo> Alerts { get; set; } = new();

    /// <summary>
    /// Preferred language
    /// </summary>
    public string? PreferredLanguage { get; set; }

    /// <summary>
    /// Race
    /// </summary>
    public string? Race { get; set; }

    /// <summary>
    /// Ethnicity
    /// </summary>
    public string? Ethnicity { get; set; }

    /// <summary>
    /// Marital status
    /// </summary>
    public string? MaritalStatus { get; set; }

    /// <summary>
    /// Blood type
    /// </summary>
    public string? BloodType { get; set; }

    /// <summary>
    /// Organ donor status
    /// </summary>
    public bool? IsOrganDonor { get; set; }

    /// <summary>
    /// Advance directive on file
    /// </summary>
    public bool HasAdvanceDirective { get; set; }

    /// <summary>
    /// When the patient record was created
    /// </summary>
    public DateTime CreatedAt { get; set; }
}

#region Nested Information Classes

public class ProviderInfo
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Specialty { get; set; } = string.Empty;
    public string? Phone { get; set; }
}

public class AddressInfo
{
    public string Street1 { get; set; } = string.Empty;
    public string? Street2 { get; set; }
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string Country { get; set; } = "USA";
}

public class EmergencyContactInfo
{
    public string Name { get; set; } = string.Empty;
    public string Relationship { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? AlternatePhone { get; set; }
}

public class InsuranceInfo
{
    public string Provider { get; set; } = string.Empty;
    public string PolicyNumber { get; set; } = string.Empty;
    public string GroupNumber { get; set; } = string.Empty;
    public DateTime? EffectiveDate { get; set; }
    public DateTime? ExpirationDate { get; set; }
}

public class AllergyInfo
{
    public Guid Id { get; set; }
    public string Allergen { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string Reaction { get; set; } = string.Empty;
    public DateTime OnsetDate { get; set; }
}

public class MedicationInfo
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Dosage { get; set; } = string.Empty;
    public string Frequency { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string PrescribedBy { get; set; } = string.Empty;
}

public class ProblemInfo
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime OnsetDate { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class VitalSignsInfo
{
    public DateTime MeasuredAt { get; set; }
    public string? BloodPressure { get; set; }
    public double? HeartRate { get; set; }
    public double? Temperature { get; set; }
    public double? RespiratoryRate { get; set; }
    public double? OxygenSaturation { get; set; }
    public double? Weight { get; set; }
    public double? Height { get; set; }
    public double? BMI { get; set; }
}

public class VisitInfo
{
    public Guid Id { get; set; }
    public DateTime VisitDate { get; set; }
    public string VisitType { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public string ChiefComplaint { get; set; } = string.Empty;
}

public class AppointmentInfo
{
    public Guid Id { get; set; }
    public DateTime ScheduledAt { get; set; }
    public string AppointmentType { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
}

public class AlertInfo
{
    public Guid Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

#endregion
