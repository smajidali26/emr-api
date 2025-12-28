namespace EMR.Domain.ValueObjects;

/// <summary>
/// Value object representing a Patient identifier
/// </summary>
public sealed record PatientId
{
    public Guid Value { get; }

    private PatientId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("Patient ID cannot be empty", nameof(value));
        }

        Value = value;
    }

    /// <summary>
    /// Create a new PatientId from a Guid
    /// </summary>
    public static PatientId Create(Guid value) => new(value);

    /// <summary>
    /// Create a new PatientId from a string
    /// </summary>
    public static PatientId Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Patient ID cannot be null or empty", nameof(value));
        }

        if (!Guid.TryParse(value, out var guid))
        {
            throw new ArgumentException("Invalid Patient ID format", nameof(value));
        }

        return new PatientId(guid);
    }

    /// <summary>
    /// Create a new unique PatientId
    /// </summary>
    public static PatientId NewPatientId() => new(Guid.NewGuid());

    public override string ToString() => Value.ToString();

    public static implicit operator Guid(PatientId patientId) => patientId.Value;
    public static implicit operator string(PatientId patientId) => patientId.Value.ToString();
}
