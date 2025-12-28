using EMR.Domain.ValueObjects;
using FluentAssertions;

namespace EMR.UnitTests.Domain.ValueObjects;

/// <summary>
/// Unit tests for PatientAddress value object
/// Tests cover: creation, validation, formatting, and edge cases
/// </summary>
public class PatientAddressTests
{
    #region Create Tests - Valid Data

    [Fact]
    public void Create_WithValidData_ShouldSucceed()
    {
        // Arrange
        var street = "123 Main St";
        var city = "Springfield";
        var state = "IL";
        var zipCode = "62701";
        var country = "USA";

        // Act
        var address = PatientAddress.Create(street, null, city, state, zipCode, country);

        // Assert
        address.Should().NotBeNull();
        address.Street.Should().Be("123 Main St");
        address.Street2.Should().BeNull();
        address.City.Should().Be("Springfield");
        address.State.Should().Be("IL");
        address.ZipCode.Should().Be("62701");
        address.Country.Should().Be("USA");
    }

    [Fact]
    public void Create_WithStreet2_ShouldSucceed()
    {
        // Arrange
        var street = "123 Main St";
        var street2 = "Apt 4B";
        var city = "Springfield";
        var state = "IL";
        var zipCode = "62701";
        var country = "USA";

        // Act
        var address = PatientAddress.Create(street, street2, city, state, zipCode, country);

        // Assert
        address.Should().NotBeNull();
        address.Street.Should().Be("123 Main St");
        address.Street2.Should().Be("Apt 4B");
        address.City.Should().Be("Springfield");
        address.State.Should().Be("IL");
        address.ZipCode.Should().Be("62701");
        address.Country.Should().Be("USA");
    }

    [Fact]
    public void Create_TrimsWhitespace_ShouldSucceed()
    {
        // Arrange
        var street = "  123 Main St  ";
        var street2 = "  Apt 4B  ";
        var city = "  Springfield  ";
        var state = "  IL  ";
        var zipCode = "  62701  ";
        var country = "  USA  ";

        // Act
        var address = PatientAddress.Create(street, street2, city, state, zipCode, country);

        // Assert
        address.Street.Should().Be("123 Main St");
        address.Street2.Should().Be("Apt 4B");
        address.City.Should().Be("Springfield");
        address.State.Should().Be("IL");
        address.ZipCode.Should().Be("62701");
        address.Country.Should().Be("USA");
    }

    [Fact]
    public void Create_WithEmptyStreet2_ShouldSetToNull()
    {
        // Arrange
        var address = PatientAddress.Create(
            "123 Main St",
            "",
            "Springfield",
            "IL",
            "62701",
            "USA");

        // Assert
        address.Street2.Should().BeNull();
    }

    [Fact]
    public void Create_WithWhitespaceStreet2_ShouldSetToNull()
    {
        // Arrange
        var address = PatientAddress.Create(
            "123 Main St",
            "   ",
            "Springfield",
            "IL",
            "62701",
            "USA");

        // Assert
        address.Street2.Should().BeNull();
    }

    #endregion

    #region Create Tests - Invalid Data

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithNullOrEmptyStreet_ShouldThrowArgumentException(string? invalidStreet)
    {
        // Act
        Action act = () => PatientAddress.Create(
            invalidStreet!,
            null,
            "Springfield",
            "IL",
            "62701",
            "USA");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Street address is required*")
            .And.ParamName.Should().Be("street");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithNullOrEmptyCity_ShouldThrowArgumentException(string? invalidCity)
    {
        // Act
        Action act = () => PatientAddress.Create(
            "123 Main St",
            null,
            invalidCity!,
            "IL",
            "62701",
            "USA");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*City is required*")
            .And.ParamName.Should().Be("city");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithNullOrEmptyState_ShouldThrowArgumentException(string? invalidState)
    {
        // Act
        Action act = () => PatientAddress.Create(
            "123 Main St",
            null,
            "Springfield",
            invalidState!,
            "62701",
            "USA");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*State is required*")
            .And.ParamName.Should().Be("state");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithNullOrEmptyZipCode_ShouldThrowArgumentException(string? invalidZipCode)
    {
        // Act
        Action act = () => PatientAddress.Create(
            "123 Main St",
            null,
            "Springfield",
            "IL",
            invalidZipCode!,
            "USA");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Zip code is required*")
            .And.ParamName.Should().Be("zipCode");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithNullOrEmptyCountry_ShouldThrowArgumentException(string? invalidCountry)
    {
        // Act
        Action act = () => PatientAddress.Create(
            "123 Main St",
            null,
            "Springfield",
            "IL",
            "62701",
            invalidCountry!);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Country is required*")
            .And.ParamName.Should().Be("country");
    }

    #endregion

    #region GetFullAddress Tests

    [Fact]
    public void GetFullAddress_WithoutStreet2_ShouldFormatCorrectly()
    {
        // Arrange
        var address = PatientAddress.Create(
            "123 Main St",
            null,
            "Springfield",
            "IL",
            "62701",
            "USA");

        // Act
        var fullAddress = address.GetFullAddress();

        // Assert
        var expectedAddress = $"123 Main St{Environment.NewLine}Springfield, IL 62701{Environment.NewLine}USA";
        fullAddress.Should().Be(expectedAddress);
    }

    [Fact]
    public void GetFullAddress_WithStreet2_ShouldFormatCorrectly()
    {
        // Arrange
        var address = PatientAddress.Create(
            "123 Main St",
            "Apt 4B",
            "Springfield",
            "IL",
            "62701",
            "USA");

        // Act
        var fullAddress = address.GetFullAddress();

        // Assert
        var expectedAddress = $"123 Main St{Environment.NewLine}Apt 4B{Environment.NewLine}Springfield, IL 62701{Environment.NewLine}USA";
        fullAddress.Should().Be(expectedAddress);
    }

    [Fact]
    public void GetFullAddress_ShouldContainAllAddressComponents()
    {
        // Arrange
        var address = PatientAddress.Create(
            "456 Oak Ave",
            "Suite 100",
            "Chicago",
            "IL",
            "60601",
            "United States");

        // Act
        var fullAddress = address.GetFullAddress();

        // Assert
        fullAddress.Should().Contain("456 Oak Ave");
        fullAddress.Should().Contain("Suite 100");
        fullAddress.Should().Contain("Chicago, IL 60601");
        fullAddress.Should().Contain("United States");
    }

    #endregion

    #region ToString Tests

    [Fact]
    public void ToString_ShouldReturnFullAddress()
    {
        // Arrange
        var address = PatientAddress.Create(
            "123 Main St",
            "Apt 4B",
            "Springfield",
            "IL",
            "62701",
            "USA");

        // Act
        var result = address.ToString();

        // Assert
        result.Should().Be(address.GetFullAddress());
    }

    #endregion

    #region Equality Tests (Record behavior)

    [Fact]
    public void Equality_TwoAddressesWithSameValues_ShouldBeEqual()
    {
        // Arrange
        var address1 = PatientAddress.Create("123 Main St", null, "Springfield", "IL", "62701", "USA");
        var address2 = PatientAddress.Create("123 Main St", null, "Springfield", "IL", "62701", "USA");

        // Assert
        address1.Should().Be(address2);
        (address1 == address2).Should().BeTrue();
    }

    [Fact]
    public void Equality_TwoAddressesWithDifferentStreets_ShouldNotBeEqual()
    {
        // Arrange
        var address1 = PatientAddress.Create("123 Main St", null, "Springfield", "IL", "62701", "USA");
        var address2 = PatientAddress.Create("456 Oak Ave", null, "Springfield", "IL", "62701", "USA");

        // Assert
        address1.Should().NotBe(address2);
        (address1 != address2).Should().BeTrue();
    }

    [Fact]
    public void Equality_DifferentStreet2Values_ShouldNotBeEqual()
    {
        // Arrange
        var address1 = PatientAddress.Create("123 Main St", "Apt 4B", "Springfield", "IL", "62701", "USA");
        var address2 = PatientAddress.Create("123 Main St", null, "Springfield", "IL", "62701", "USA");

        // Assert
        address1.Should().NotBe(address2);
    }

    [Fact]
    public void Equality_TrimmedValuesAreEqual_ShouldBeEqual()
    {
        // Arrange
        var address1 = PatientAddress.Create("123 Main St", "Apt 4B", "Springfield", "IL", "62701", "USA");
        var address2 = PatientAddress.Create("  123 Main St  ", "  Apt 4B  ", "  Springfield  ", "  IL  ", "  62701  ", "  USA  ");

        // Assert
        address1.Should().Be(address2);
    }

    [Fact]
    public void GetHashCode_SameAddresses_ShouldHaveSameHashCode()
    {
        // Arrange
        var address1 = PatientAddress.Create("123 Main St", "Apt 4B", "Springfield", "IL", "62701", "USA");
        var address2 = PatientAddress.Create("123 Main St", "Apt 4B", "Springfield", "IL", "62701", "USA");

        // Assert
        address1.GetHashCode().Should().Be(address2.GetHashCode());
    }

    #endregion

    #region Edge Cases and Special Characters

    [Fact]
    public void Create_WithSpecialCharactersInStreet_ShouldSucceed()
    {
        // Arrange
        var address = PatientAddress.Create(
            "123 O'Brien St #5",
            null,
            "Springfield",
            "IL",
            "62701",
            "USA");

        // Assert
        address.Street.Should().Be("123 O'Brien St #5");
    }

    [Fact]
    public void Create_WithUnicodeCharacters_ShouldSucceed()
    {
        // Arrange
        var address = PatientAddress.Create(
            "Rue de la Paix",
            null,
            "Montréal",
            "QC",
            "H3A 1A1",
            "Canada");

        // Assert
        address.Street.Should().Be("Rue de la Paix");
        address.City.Should().Be("Montréal");
    }

    [Fact]
    public void Create_WithLongAddress_ShouldSucceed()
    {
        // Arrange
        var longStreet = "1234567890 Very Long Street Name That Goes On And On And Includes Multiple Words";
        var address = PatientAddress.Create(
            longStreet,
            null,
            "Springfield",
            "IL",
            "62701",
            "USA");

        // Assert
        address.Street.Should().Be(longStreet);
    }

    [Fact]
    public void Create_WithDifferentZipCodeFormats_ShouldPreserveFormat()
    {
        // Test US ZIP+4
        var address1 = PatientAddress.Create("123 Main St", null, "Springfield", "IL", "62701-1234", "USA");
        address1.ZipCode.Should().Be("62701-1234");

        // Test Canadian postal code
        var address2 = PatientAddress.Create("123 Main St", null, "Toronto", "ON", "M5H 2N2", "Canada");
        address2.ZipCode.Should().Be("M5H 2N2");

        // Test UK postcode
        var address3 = PatientAddress.Create("123 Main St", null, "London", "Greater London", "SW1A 1AA", "UK");
        address3.ZipCode.Should().Be("SW1A 1AA");
    }

    [Fact]
    public void Create_WithPOBox_ShouldSucceed()
    {
        // Arrange
        var address = PatientAddress.Create(
            "PO Box 12345",
            null,
            "Springfield",
            "IL",
            "62701",
            "USA");

        // Assert
        address.Street.Should().Be("PO Box 12345");
    }

    [Fact]
    public void Create_WithRuralRouteAddress_ShouldSucceed()
    {
        // Arrange
        var address = PatientAddress.Create(
            "RR 2 Box 123",
            null,
            "Springfield",
            "IL",
            "62701",
            "USA");

        // Assert
        address.Street.Should().Be("RR 2 Box 123");
    }

    [Fact]
    public void Create_WithStateAbbreviations_ShouldPreserveCase()
    {
        // Test lowercase state
        var address1 = PatientAddress.Create("123 Main St", null, "Springfield", "il", "62701", "USA");
        address1.State.Should().Be("il");

        // Test uppercase state
        var address2 = PatientAddress.Create("123 Main St", null, "Springfield", "IL", "62701", "USA");
        address2.State.Should().Be("IL");

        // Test full state name
        var address3 = PatientAddress.Create("123 Main St", null, "Springfield", "Illinois", "62701", "USA");
        address3.State.Should().Be("Illinois");
    }

    [Fact]
    public void Create_InternationalAddress_ShouldSucceed()
    {
        // Arrange - Japanese address
        var address = PatientAddress.Create(
            "1-1-1 Chiyoda",
            null,
            "Tokyo",
            "Tokyo",
            "100-0001",
            "Japan");

        // Assert
        address.Should().NotBeNull();
        address.City.Should().Be("Tokyo");
        address.Country.Should().Be("Japan");
    }

    #endregion

    #region Collection Tests

    [Fact]
    public void PatientAddress_CanBeUsedInHashSet()
    {
        // Arrange
        var hashSet = new HashSet<PatientAddress>();
        var address1 = PatientAddress.Create("123 Main St", null, "Springfield", "IL", "62701", "USA");
        var address2 = PatientAddress.Create("456 Oak Ave", null, "Chicago", "IL", "60601", "USA");
        var address1Duplicate = PatientAddress.Create("123 Main St", null, "Springfield", "IL", "62701", "USA");

        // Act
        hashSet.Add(address1);
        hashSet.Add(address2);
        hashSet.Add(address1Duplicate);

        // Assert
        hashSet.Should().HaveCount(2); // Duplicate should not be added
    }

    [Fact]
    public void PatientAddress_CanBeUsedAsKeyInDictionary()
    {
        // Arrange
        var dict = new Dictionary<PatientAddress, string>();
        var address1 = PatientAddress.Create("123 Main St", null, "Springfield", "IL", "62701", "USA");
        var address2 = PatientAddress.Create("456 Oak Ave", null, "Chicago", "IL", "60601", "USA");

        // Act
        dict[address1] = "Patient 1";
        dict[address2] = "Patient 2";

        // Assert
        dict.Should().HaveCount(2);
        dict[address1].Should().Be("Patient 1");
        dict[address2].Should().Be("Patient 2");
    }

    #endregion
}
