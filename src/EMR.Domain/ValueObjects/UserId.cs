namespace EMR.Domain.ValueObjects;

/// <summary>
/// Value object representing a User identifier
/// </summary>
public sealed record UserId
{
    public Guid Value { get; }

    private UserId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("User ID cannot be empty", nameof(value));
        }

        Value = value;
    }

    /// <summary>
    /// Create a new UserId from a Guid
    /// </summary>
    public static UserId Create(Guid value) => new(value);

    /// <summary>
    /// Create a new UserId from a string
    /// </summary>
    public static UserId Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("User ID cannot be null or empty", nameof(value));
        }

        if (!Guid.TryParse(value, out var guid))
        {
            throw new ArgumentException("Invalid User ID format", nameof(value));
        }

        return new UserId(guid);
    }

    /// <summary>
    /// Create a new unique UserId
    /// </summary>
    public static UserId NewUserId() => new(Guid.NewGuid());

    public override string ToString() => Value.ToString();

    public static implicit operator Guid(UserId userId) => userId.Value;
    public static implicit operator string(UserId userId) => userId.Value.ToString();
}
