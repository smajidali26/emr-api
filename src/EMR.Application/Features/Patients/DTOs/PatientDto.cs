using EMR.Domain.Enums;

namespace EMR.Application.Features.Patients.DTOs;

/// <summary>
/// Data transfer object for patient information
/// </summary>
public class PatientDto
{
    /// <summary>
    /// Unique identifier for the patient
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// Medical Record Number (MRN)
    /// </summary>
    public string MedicalRecordNumber { get; init; } = string.Empty;

    /// <summary>
    /// Patient's first name
    /// </summary>
    public string FirstName { get; init; } = string.Empty;

    /// <summary>
    /// Patient's middle name (optional)
    /// </summary>
    public string? MiddleName { get; init; }

    /// <summary>
    /// Patient's last name
    /// </summary>
    public string LastName { get; init; } = string.Empty;

    /// <summary>
    /// Patient's full name
    /// </summary>
    public string FullName { get; init; } = string.Empty;

    /// <summary>
    /// Patient's date of birth
    /// </summary>
    public DateTime DateOfBirth { get; init; }

    /// <summary>
    /// Patient's age in years
    /// </summary>
    public int Age { get; init; }

    /// <summary>
    /// Patient's gender
    /// </summary>
    public Gender Gender { get; init; }

    /// <summary>
    /// Patient's primary phone number
    /// </summary>
    public string PhoneNumber { get; init; } = string.Empty;

    /// <summary>
    /// Patient's alternate phone number (optional)
    /// </summary>
    public string? AlternatePhoneNumber { get; init; }

    /// <summary>
    /// Patient's email address (optional)
    /// </summary>
    public string? Email { get; init; }

    /// <summary>
    /// Patient's residential address
    /// </summary>
    public PatientAddressDto Address { get; init; } = new();

    /// <summary>
    /// Patient's marital status
    /// </summary>
    public MaritalStatus MaritalStatus { get; init; }

    /// <summary>
    /// Patient's race
    /// </summary>
    public Race Race { get; init; }

    /// <summary>
    /// Patient's ethnicity
    /// </summary>
    public Ethnicity Ethnicity { get; init; }

    /// <summary>
    /// Patient's preferred language
    /// </summary>
    public PreferredLanguage PreferredLanguage { get; init; }

    /// <summary>
    /// Emergency contact information
    /// </summary>
    public EmergencyContactDto EmergencyContact { get; init; } = new();

    /// <summary>
    /// Indicates if the patient is active
    /// </summary>
    public bool IsActive { get; init; }

    /// <summary>
    /// Date and time when the patient was created (UTC)
    /// </summary>
    public DateTime CreatedAt { get; init; }

    /// <summary>
    /// Date and time when the patient was last updated (UTC)
    /// </summary>
    public DateTime? UpdatedAt { get; init; }
}
