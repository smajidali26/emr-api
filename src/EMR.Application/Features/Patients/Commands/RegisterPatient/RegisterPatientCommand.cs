using EMR.Application.Common.Abstractions;
using EMR.Application.Common.DTOs;
using EMR.Application.Features.Patients.DTOs;
using EMR.Domain.Enums;

namespace EMR.Application.Features.Patients.Commands.RegisterPatient;

/// <summary>
/// Command to register a new patient in the EMR system
/// </summary>
public record RegisterPatientCommand : ICommand<ResultDto<PatientDto>>
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
    /// Patient's Social Security Number (optional, encrypted at rest)
    /// </summary>
    public string? SocialSecurityNumber { get; init; }

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
    public MaritalStatus MaritalStatus { get; init; } = MaritalStatus.Unknown;

    /// <summary>
    /// Patient's race
    /// </summary>
    public Race Race { get; init; } = Race.Unknown;

    /// <summary>
    /// Patient's ethnicity
    /// </summary>
    public Ethnicity Ethnicity { get; init; } = Ethnicity.Unknown;

    /// <summary>
    /// Patient's preferred language
    /// </summary>
    public PreferredLanguage PreferredLanguage { get; init; } = PreferredLanguage.English;

    /// <summary>
    /// Emergency contact information
    /// </summary>
    public EmergencyContactDto EmergencyContact { get; init; } = new();
}
