namespace EMR.Application.Features.Patients.DTOs;

/// <summary>
/// Data transfer object for patient address
/// </summary>
public class PatientAddressDto
{
    /// <summary>
    /// Street address line 1
    /// </summary>
    public string Street { get; init; } = string.Empty;

    /// <summary>
    /// Street address line 2 (optional)
    /// </summary>
    public string? Street2 { get; init; }

    /// <summary>
    /// City
    /// </summary>
    public string City { get; init; } = string.Empty;

    /// <summary>
    /// State or province
    /// </summary>
    public string State { get; init; } = string.Empty;

    /// <summary>
    /// Postal/ZIP code
    /// </summary>
    public string ZipCode { get; init; } = string.Empty;

    /// <summary>
    /// Country
    /// </summary>
    public string Country { get; init; } = string.Empty;
}
