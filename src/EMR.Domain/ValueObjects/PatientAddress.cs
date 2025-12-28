namespace EMR.Domain.ValueObjects;

/// <summary>
/// Value object representing a patient's address
/// </summary>
public sealed record PatientAddress
{
    public string Street { get; }
    public string? Street2 { get; }
    public string City { get; }
    public string State { get; }
    public string ZipCode { get; }
    public string Country { get; }

    private PatientAddress(
        string street,
        string? street2,
        string city,
        string state,
        string zipCode,
        string country)
    {
        if (string.IsNullOrWhiteSpace(street))
            throw new ArgumentException("Street address is required", nameof(street));

        if (string.IsNullOrWhiteSpace(city))
            throw new ArgumentException("City is required", nameof(city));

        if (string.IsNullOrWhiteSpace(state))
            throw new ArgumentException("State is required", nameof(state));

        if (string.IsNullOrWhiteSpace(zipCode))
            throw new ArgumentException("Zip code is required", nameof(zipCode));

        if (string.IsNullOrWhiteSpace(country))
            throw new ArgumentException("Country is required", nameof(country));

        Street = street.Trim();
        Street2 = string.IsNullOrWhiteSpace(street2) ? null : street2.Trim();
        City = city.Trim();
        State = state.Trim();
        ZipCode = zipCode.Trim();
        Country = country.Trim();
    }

    /// <summary>
    /// Create a new patient address
    /// </summary>
    public static PatientAddress Create(
        string street,
        string? street2,
        string city,
        string state,
        string zipCode,
        string country)
    {
        return new PatientAddress(street, street2, city, state, zipCode, country);
    }

    /// <summary>
    /// Get the full address as a formatted string
    /// </summary>
    public string GetFullAddress()
    {
        var addressLines = new List<string> { Street };

        if (!string.IsNullOrWhiteSpace(Street2))
            addressLines.Add(Street2);

        addressLines.Add($"{City}, {State} {ZipCode}");
        addressLines.Add(Country);

        return string.Join(Environment.NewLine, addressLines);
    }

    public override string ToString() => GetFullAddress();
}
