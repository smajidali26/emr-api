using EMR.Domain.Common;
using EMR.Domain.Enums;
using EMR.Domain.ValueObjects;

namespace EMR.Domain.Entities;

/// <summary>
/// Represents a patient in the EMR system with full demographics and PHI data
/// HIPAA Compliance: Contains Protected Health Information (PHI)
/// </summary>
public class Patient : BaseEntity
{
    /// <summary>
    /// Medical Record Number (MRN) - unique patient identifier
    /// </summary>
    public PatientIdentifier MedicalRecordNumber { get; private set; }

    /// <summary>
    /// Patient's first name
    /// </summary>
    public string FirstName { get; private set; } = string.Empty;

    /// <summary>
    /// Patient's middle name (optional)
    /// </summary>
    public string? MiddleName { get; private set; }

    /// <summary>
    /// Patient's last name
    /// </summary>
    public string LastName { get; private set; } = string.Empty;

    /// <summary>
    /// Patient's date of birth
    /// </summary>
    public DateTime DateOfBirth { get; private set; }

    /// <summary>
    /// Patient's gender
    /// </summary>
    public Gender Gender { get; private set; }

    /// <summary>
    /// Patient's Social Security Number (encrypted at rest)
    /// PHI - HIPAA protected
    /// </summary>
    public string? SocialSecurityNumber { get; private set; }

    /// <summary>
    /// Patient's primary phone number
    /// </summary>
    public string PhoneNumber { get; private set; } = string.Empty;

    /// <summary>
    /// Patient's alternate phone number (optional)
    /// </summary>
    public string? AlternatePhoneNumber { get; private set; }

    /// <summary>
    /// Patient's email address (optional)
    /// </summary>
    public string? Email { get; private set; }

    /// <summary>
    /// Patient's residential address
    /// </summary>
    public PatientAddress Address { get; private set; }

    /// <summary>
    /// Patient's marital status
    /// </summary>
    public MaritalStatus MaritalStatus { get; private set; }

    /// <summary>
    /// Patient's race
    /// </summary>
    public Race Race { get; private set; }

    /// <summary>
    /// Patient's ethnicity
    /// </summary>
    public Ethnicity Ethnicity { get; private set; }

    /// <summary>
    /// Patient's preferred language for communication
    /// </summary>
    public PreferredLanguage PreferredLanguage { get; private set; }

    /// <summary>
    /// Emergency contact information
    /// </summary>
    public EmergencyContact EmergencyContact { get; private set; }

    /// <summary>
    /// Indicates if the patient is active in the system
    /// </summary>
    public bool IsActive { get; private set; } = true;

    /// <summary>
    /// Patient's full name
    /// </summary>
    public string FullName =>
        string.IsNullOrWhiteSpace(MiddleName)
            ? $"{FirstName} {LastName}"
            : $"{FirstName} {MiddleName} {LastName}";

    /// <summary>
    /// Calculate patient's age in years
    /// </summary>
    public int Age
    {
        get
        {
            var today = DateTime.UtcNow;
            var age = today.Year - DateOfBirth.Year;
            if (DateOfBirth.Date > today.AddYears(-age)) age--;
            return age;
        }
    }

    // Private constructor for EF Core
    private Patient()
    {
        MedicalRecordNumber = null!;
        Address = null!;
        EmergencyContact = null!;
    }

    /// <summary>
    /// Creates a new patient instance
    /// </summary>
    public Patient(
        string firstName,
        string lastName,
        DateTime dateOfBirth,
        Gender gender,
        string phoneNumber,
        PatientAddress address,
        EmergencyContact emergencyContact,
        string createdBy,
        string? middleName = null,
        string? email = null,
        string? alternatePhoneNumber = null,
        string? socialSecurityNumber = null,
        MaritalStatus maritalStatus = MaritalStatus.Unknown,
        Race race = Race.Unknown,
        Ethnicity ethnicity = Ethnicity.Unknown,
        PreferredLanguage preferredLanguage = PreferredLanguage.English)
    {
        ValidatePatientData(firstName, lastName, dateOfBirth, phoneNumber);

        MedicalRecordNumber = PatientIdentifier.Generate();
        FirstName = firstName.Trim();
        MiddleName = string.IsNullOrWhiteSpace(middleName) ? null : middleName.Trim();
        LastName = lastName.Trim();
        DateOfBirth = dateOfBirth.Date; // Store only date part
        Gender = gender;
        PhoneNumber = phoneNumber.Trim();
        AlternatePhoneNumber = string.IsNullOrWhiteSpace(alternatePhoneNumber) ? null : alternatePhoneNumber.Trim();
        Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim().ToLowerInvariant();
        Address = address ?? throw new ArgumentNullException(nameof(address));
        EmergencyContact = emergencyContact ?? throw new ArgumentNullException(nameof(emergencyContact));
        SocialSecurityNumber = string.IsNullOrWhiteSpace(socialSecurityNumber) ? null : socialSecurityNumber.Trim();
        MaritalStatus = maritalStatus;
        Race = race;
        Ethnicity = ethnicity;
        PreferredLanguage = preferredLanguage;
        CreatedBy = createdBy;
    }

    /// <summary>
    /// Update patient demographics
    /// </summary>
    public void UpdateDemographics(
        string firstName,
        string lastName,
        DateTime dateOfBirth,
        Gender gender,
        string phoneNumber,
        PatientAddress address,
        string updatedBy,
        string? middleName = null,
        string? email = null,
        string? alternatePhoneNumber = null,
        MaritalStatus? maritalStatus = null,
        Race? race = null,
        Ethnicity? ethnicity = null,
        PreferredLanguage? preferredLanguage = null)
    {
        ValidatePatientData(firstName, lastName, dateOfBirth, phoneNumber);

        FirstName = firstName.Trim();
        MiddleName = string.IsNullOrWhiteSpace(middleName) ? null : middleName.Trim();
        LastName = lastName.Trim();
        DateOfBirth = dateOfBirth.Date;
        Gender = gender;
        PhoneNumber = phoneNumber.Trim();
        AlternatePhoneNumber = string.IsNullOrWhiteSpace(alternatePhoneNumber) ? null : alternatePhoneNumber.Trim();
        Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim().ToLowerInvariant();
        Address = address ?? throw new ArgumentNullException(nameof(address));

        if (maritalStatus.HasValue)
            MaritalStatus = maritalStatus.Value;

        if (race.HasValue)
            Race = race.Value;

        if (ethnicity.HasValue)
            Ethnicity = ethnicity.Value;

        if (preferredLanguage.HasValue)
            PreferredLanguage = preferredLanguage.Value;

        MarkAsUpdated(updatedBy);
    }

    /// <summary>
    /// Update patient's SSN (encrypted at rest)
    /// </summary>
    public void UpdateSocialSecurityNumber(string? ssn, string updatedBy)
    {
        SocialSecurityNumber = string.IsNullOrWhiteSpace(ssn) ? null : ssn.Trim();
        MarkAsUpdated(updatedBy);
    }

    /// <summary>
    /// Update emergency contact information
    /// </summary>
    public void UpdateEmergencyContact(EmergencyContact emergencyContact, string updatedBy)
    {
        EmergencyContact = emergencyContact ?? throw new ArgumentNullException(nameof(emergencyContact));
        MarkAsUpdated(updatedBy);
    }

    /// <summary>
    /// Activate patient account
    /// </summary>
    public void Activate(string updatedBy)
    {
        if (!IsActive)
        {
            IsActive = true;
            MarkAsUpdated(updatedBy);
        }
    }

    /// <summary>
    /// Deactivate patient account
    /// </summary>
    public void Deactivate(string updatedBy)
    {
        if (IsActive)
        {
            IsActive = false;
            MarkAsUpdated(updatedBy);
        }
    }

    /// <summary>
    /// Validate patient data
    /// </summary>
    private static void ValidatePatientData(string firstName, string lastName, DateTime dateOfBirth, string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(firstName))
            throw new ArgumentException("First name is required", nameof(firstName));

        if (string.IsNullOrWhiteSpace(lastName))
            throw new ArgumentException("Last name is required", nameof(lastName));

        if (dateOfBirth > DateTime.UtcNow.Date)
            throw new ArgumentException("Date of birth cannot be in the future", nameof(dateOfBirth));

        if (dateOfBirth < DateTime.UtcNow.AddYears(-150))
            throw new ArgumentException("Date of birth is not valid", nameof(dateOfBirth));

        if (string.IsNullOrWhiteSpace(phoneNumber))
            throw new ArgumentException("Phone number is required", nameof(phoneNumber));
    }
}
