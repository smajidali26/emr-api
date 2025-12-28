namespace EMR.Domain.ValueObjects;

/// <summary>
/// Value object representing a patient's emergency contact information
/// </summary>
public sealed record EmergencyContact
{
    public string Name { get; }
    public string Relationship { get; }
    public string PhoneNumber { get; }
    public string? AlternatePhoneNumber { get; }

    private EmergencyContact(
        string name,
        string relationship,
        string phoneNumber,
        string? alternatePhoneNumber)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Emergency contact name is required", nameof(name));

        if (string.IsNullOrWhiteSpace(relationship))
            throw new ArgumentException("Emergency contact relationship is required", nameof(relationship));

        if (string.IsNullOrWhiteSpace(phoneNumber))
            throw new ArgumentException("Emergency contact phone number is required", nameof(phoneNumber));

        Name = name.Trim();
        Relationship = relationship.Trim();
        PhoneNumber = phoneNumber.Trim();
        AlternatePhoneNumber = string.IsNullOrWhiteSpace(alternatePhoneNumber) ? null : alternatePhoneNumber.Trim();
    }

    /// <summary>
    /// Create a new emergency contact
    /// </summary>
    public static EmergencyContact Create(
        string name,
        string relationship,
        string phoneNumber,
        string? alternatePhoneNumber = null)
    {
        return new EmergencyContact(name, relationship, phoneNumber, alternatePhoneNumber);
    }

    public override string ToString() => $"{Name} ({Relationship}) - {PhoneNumber}";
}
