using EMR.Domain.Common;

namespace EMR.Domain.Events.Patient;

/// <summary>
/// Event raised when a new patient is registered in the system.
/// This is the initial event in a patient's event stream.
/// </summary>
public sealed record PatientRegisteredEvent : DomainEventBase
{
    /// <summary>
    /// The unique identifier of the patient
    /// </summary>
    public Guid PatientId { get; init; }

    /// <summary>
    /// Medical Record Number assigned to the patient
    /// </summary>
    public string MedicalRecordNumber { get; init; } = string.Empty;

    /// <summary>
    /// Patient's first name
    /// </summary>
    public string FirstName { get; init; } = string.Empty;

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
    public string Gender { get; init; } = string.Empty;

    /// <summary>
    /// Patient's contact information (email)
    /// </summary>
    public string? Email { get; init; }

    /// <summary>
    /// Patient's contact information (phone)
    /// </summary>
    public string? PhoneNumber { get; init; }

    /// <summary>
    /// Patient's address
    /// </summary>
    public string? Address { get; init; }

    /// <summary>
    /// Emergency contact name
    /// </summary>
    public string? EmergencyContactName { get; init; }

    /// <summary>
    /// Emergency contact phone number
    /// </summary>
    public string? EmergencyContactPhone { get; init; }

    /// <summary>
    /// Registration facility/location
    /// </summary>
    public string? RegistrationFacility { get; init; }

    public PatientRegisteredEvent()
    {
    }

    public PatientRegisteredEvent(
        Guid patientId,
        string medicalRecordNumber,
        string firstName,
        string lastName,
        DateTime dateOfBirth,
        string gender,
        string? userId = null,
        string? correlationId = null,
        string? causationId = null)
        : base(userId, correlationId, causationId)
    {
        PatientId = patientId;
        MedicalRecordNumber = medicalRecordNumber;
        FirstName = firstName;
        LastName = lastName;
        DateOfBirth = dateOfBirth;
        Gender = gender;
    }
}
