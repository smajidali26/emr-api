using EMR.Domain.Enums;

namespace EMR.Application.Features.Patients.DTOs;

/// <summary>
/// Data transfer object for patient search results (lightweight version)
/// </summary>
public class PatientSearchResultDto
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
    /// Patient's email address (optional)
    /// </summary>
    public string? Email { get; init; }

    /// <summary>
    /// Indicates if the patient is active
    /// </summary>
    public bool IsActive { get; init; }
}
