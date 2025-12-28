namespace EMR.Application.Features.Patients.DTOs;

/// <summary>
/// Data transfer object for emergency contact information
/// </summary>
public class EmergencyContactDto
{
    /// <summary>
    /// Emergency contact's full name
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Relationship to patient
    /// </summary>
    public string Relationship { get; init; } = string.Empty;

    /// <summary>
    /// Primary phone number
    /// </summary>
    public string PhoneNumber { get; init; } = string.Empty;

    /// <summary>
    /// Alternate phone number (optional)
    /// </summary>
    public string? AlternatePhoneNumber { get; init; }
}
