namespace EMR.Domain.ValueObjects;

/// <summary>
/// Value object representing a Patient Medical Record Number (MRN)
/// MRN Format: MRN-YYYYMMDD-XXXXXX (e.g., MRN-20240115-123456)
/// </summary>
public sealed record PatientIdentifier
{
    private const string MrnPrefix = "MRN";
    private const int SequenceLength = 6;

    public string Value { get; }

    private PatientIdentifier(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("MRN cannot be empty", nameof(value));
        }

        if (!IsValidFormat(value))
        {
            throw new ArgumentException($"Invalid MRN format. Expected format: {MrnPrefix}-YYYYMMDD-XXXXXX", nameof(value));
        }

        Value = value.ToUpperInvariant();
    }

    /// <summary>
    /// Create a PatientIdentifier from an existing MRN string
    /// </summary>
    public static PatientIdentifier Create(string mrn) => new(mrn);

    /// <summary>
    /// Generate a new unique MRN
    /// Format: MRN-YYYYMMDD-XXXXXX where XXXXXX is a random 6-digit number
    /// </summary>
    public static PatientIdentifier Generate()
    {
        var datePart = DateTime.UtcNow.ToString("yyyyMMdd");
        var sequencePart = GenerateRandomSequence();
        var mrn = $"{MrnPrefix}-{datePart}-{sequencePart}";

        return new PatientIdentifier(mrn);
    }

    /// <summary>
    /// Validates the MRN format
    /// </summary>
    private static bool IsValidFormat(string mrn)
    {
        if (string.IsNullOrWhiteSpace(mrn))
            return false;

        var parts = mrn.Split('-');
        if (parts.Length != 3)
            return false;

        if (!parts[0].Equals(MrnPrefix, StringComparison.OrdinalIgnoreCase))
            return false;

        // Validate date part (YYYYMMDD)
        if (parts[1].Length != 8 || !parts[1].All(char.IsDigit))
            return false;

        // Validate sequence part (6 digits)
        if (parts[2].Length != SequenceLength || !parts[2].All(char.IsDigit))
            return false;

        // Validate date is a valid date
        if (!DateTime.TryParseExact(parts[1], "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out _))
            return false;

        return true;
    }

    /// <summary>
    /// Generates a cryptographically secure random 6-digit sequence number
    /// SECURITY FIX: Uses RandomNumberGenerator instead of System.Random
    /// to prevent predictable MRN generation (Assigned: Daniel Lee)
    /// </summary>
    private static string GenerateRandomSequence()
    {
        using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
        byte[] randomBytes = new byte[4];
        rng.GetBytes(randomBytes);
        var sequence = (BitConverter.ToUInt32(randomBytes, 0) % 999998) + 1; // 1 to 999999
        return sequence.ToString().PadLeft(SequenceLength, '0');
    }

    public override string ToString() => Value;

    public static implicit operator string(PatientIdentifier identifier) => identifier.Value;
}
