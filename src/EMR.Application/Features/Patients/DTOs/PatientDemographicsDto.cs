using EMR.Domain.Enums;

namespace EMR.Application.Features.Patients.DTOs;

/// <summary>
/// Data transfer object for patient demographics (used for updates)
/// </summary>
public class PatientDemographicsDto
{
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
    /// Patient's date of birth
    /// </summary>
    public DateTime DateOfBirth { get; init; }

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
    public MaritalStatus? MaritalStatus { get; init; }

    /// <summary>
    /// Patient's race
    /// </summary>
    public Race? Race { get; init; }

    /// <summary>
    /// Patient's ethnicity
    /// </summary>
    public Ethnicity? Ethnicity { get; init; }

    /// <summary>
    /// Patient's preferred language
    /// </summary>
    public PreferredLanguage? PreferredLanguage { get; init; }
}
