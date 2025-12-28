using EMR.Domain.ValueObjects;
using FluentAssertions;

namespace EMR.UnitTests.Domain.ValueObjects;

/// <summary>
/// Unit tests for EmergencyContact value object
/// Tests cover: creation, validation, formatting, and edge cases
/// </summary>
public class EmergencyContactTests
{
    #region Create Tests - Valid Data

    [Fact]
    public void Create_WithValidData_ShouldSucceed()
    {
        // Arrange
        var name = "John Doe";
        var relationship = "Spouse";
        var phoneNumber = "555-1234";

        // Act
        var contact = EmergencyContact.Create(name, relationship, phoneNumber);

        // Assert
        contact.Should().NotBeNull();
        contact.Name.Should().Be("John Doe");
        contact.Relationship.Should().Be("Spouse");
        contact.PhoneNumber.Should().Be("555-1234");
        contact.AlternatePhoneNumber.Should().BeNull();
    }

    [Fact]
    public void Create_WithAlternatePhoneNumber_ShouldSucceed()
    {
        // Arrange
        var name = "Jane Smith";
        var relationship = "Mother";
        var phoneNumber = "555-1234";
        var alternatePhone = "555-5678";

        // Act
        var contact = EmergencyContact.Create(name, relationship, phoneNumber, alternatePhone);

        // Assert
        contact.Should().NotBeNull();
        contact.Name.Should().Be("Jane Smith");
        contact.Relationship.Should().Be("Mother");
        contact.PhoneNumber.Should().Be("555-1234");
        contact.AlternatePhoneNumber.Should().Be("555-5678");
    }

    [Fact]
    public void Create_TrimsWhitespace_ShouldSucceed()
    {
        // Arrange
        var name = "  John Doe  ";
        var relationship = "  Spouse  ";
        var phoneNumber = "  555-1234  ";
        var alternatePhone = "  555-5678  ";

        // Act
        var contact = EmergencyContact.Create(name, relationship, phoneNumber, alternatePhone);

        // Assert
        contact.Name.Should().Be("John Doe");
        contact.Relationship.Should().Be("Spouse");
        contact.PhoneNumber.Should().Be("555-1234");
        contact.AlternatePhoneNumber.Should().Be("555-5678");
    }

    [Fact]
    public void Create_WithEmptyAlternatePhone_ShouldSetToNull()
    {
        // Arrange
        var contact = EmergencyContact.Create("John Doe", "Spouse", "555-1234", "");

        // Assert
        contact.AlternatePhoneNumber.Should().BeNull();
    }

    [Fact]
    public void Create_WithWhitespaceAlternatePhone_ShouldSetToNull()
    {
        // Arrange
        var contact = EmergencyContact.Create("John Doe", "Spouse", "555-1234", "   ");

        // Assert
        contact.AlternatePhoneNumber.Should().BeNull();
    }

    [Fact]
    public void Create_WithNullAlternatePhone_ShouldSucceed()
    {
        // Arrange
        var contact = EmergencyContact.Create("John Doe", "Spouse", "555-1234", null);

        // Assert
        contact.AlternatePhoneNumber.Should().BeNull();
    }

    #endregion

    #region Create Tests - Invalid Data

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithNullOrEmptyName_ShouldThrowArgumentException(string? invalidName)
    {
        // Act
        Action act = () => EmergencyContact.Create(invalidName!, "Spouse", "555-1234");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Emergency contact name is required*")
            .And.ParamName.Should().Be("name");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithNullOrEmptyRelationship_ShouldThrowArgumentException(string? invalidRelationship)
    {
        // Act
        Action act = () => EmergencyContact.Create("John Doe", invalidRelationship!, "555-1234");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Emergency contact relationship is required*")
            .And.ParamName.Should().Be("relationship");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithNullOrEmptyPhoneNumber_ShouldThrowArgumentException(string? invalidPhone)
    {
        // Act
        Action act = () => EmergencyContact.Create("John Doe", "Spouse", invalidPhone!);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Emergency contact phone number is required*")
            .And.ParamName.Should().Be("phoneNumber");
    }

    #endregion

    #region ToString Tests

    [Fact]
    public void ToString_WithoutAlternatePhone_ShouldFormatCorrectly()
    {
        // Arrange
        var contact = EmergencyContact.Create("John Doe", "Spouse", "555-1234");

        // Act
        var result = contact.ToString();

        // Assert
        result.Should().Be("John Doe (Spouse) - 555-1234");
    }

    [Fact]
    public void ToString_WithAlternatePhone_ShouldFormatCorrectly()
    {
        // Arrange
        var contact = EmergencyContact.Create("Jane Smith", "Mother", "555-1234", "555-5678");

        // Act
        var result = contact.ToString();

        // Assert
        // Note: ToString doesn't include alternate phone based on implementation
        result.Should().Be("Jane Smith (Mother) - 555-1234");
    }

    #endregion

    #region Equality Tests (Record behavior)

    [Fact]
    public void Equality_TwoContactsWithSameValues_ShouldBeEqual()
    {
        // Arrange
        var contact1 = EmergencyContact.Create("John Doe", "Spouse", "555-1234");
        var contact2 = EmergencyContact.Create("John Doe", "Spouse", "555-1234");

        // Assert
        contact1.Should().Be(contact2);
        (contact1 == contact2).Should().BeTrue();
    }

    [Fact]
    public void Equality_TwoContactsWithDifferentNames_ShouldNotBeEqual()
    {
        // Arrange
        var contact1 = EmergencyContact.Create("John Doe", "Spouse", "555-1234");
        var contact2 = EmergencyContact.Create("Jane Doe", "Spouse", "555-1234");

        // Assert
        contact1.Should().NotBe(contact2);
        (contact1 != contact2).Should().BeTrue();
    }

    [Fact]
    public void Equality_DifferentRelationships_ShouldNotBeEqual()
    {
        // Arrange
        var contact1 = EmergencyContact.Create("John Doe", "Spouse", "555-1234");
        var contact2 = EmergencyContact.Create("John Doe", "Father", "555-1234");

        // Assert
        contact1.Should().NotBe(contact2);
    }

    [Fact]
    public void Equality_DifferentPhoneNumbers_ShouldNotBeEqual()
    {
        // Arrange
        var contact1 = EmergencyContact.Create("John Doe", "Spouse", "555-1234");
        var contact2 = EmergencyContact.Create("John Doe", "Spouse", "555-5678");

        // Assert
        contact1.Should().NotBe(contact2);
    }

    [Fact]
    public void Equality_DifferentAlternatePhones_ShouldNotBeEqual()
    {
        // Arrange
        var contact1 = EmergencyContact.Create("John Doe", "Spouse", "555-1234", "555-9999");
        var contact2 = EmergencyContact.Create("John Doe", "Spouse", "555-1234", null);

        // Assert
        contact1.Should().NotBe(contact2);
    }

    [Fact]
    public void Equality_TrimmedValuesAreEqual_ShouldBeEqual()
    {
        // Arrange
        var contact1 = EmergencyContact.Create("John Doe", "Spouse", "555-1234", "555-5678");
        var contact2 = EmergencyContact.Create("  John Doe  ", "  Spouse  ", "  555-1234  ", "  555-5678  ");

        // Assert
        contact1.Should().Be(contact2);
    }

    [Fact]
    public void GetHashCode_SameContacts_ShouldHaveSameHashCode()
    {
        // Arrange
        var contact1 = EmergencyContact.Create("John Doe", "Spouse", "555-1234");
        var contact2 = EmergencyContact.Create("John Doe", "Spouse", "555-1234");

        // Assert
        contact1.GetHashCode().Should().Be(contact2.GetHashCode());
    }

    #endregion

    #region Edge Cases and Real-World Scenarios

    [Fact]
    public void Create_WithMultipleNameParts_ShouldSucceed()
    {
        // Arrange
        var contact = EmergencyContact.Create(
            "Mary Jane Watson-Parker",
            "Spouse",
            "555-1234");

        // Assert
        contact.Name.Should().Be("Mary Jane Watson-Parker");
    }

    [Fact]
    public void Create_WithVariousRelationshipTypes_ShouldSucceed()
    {
        // Test common relationships
        var relationships = new[] { "Spouse", "Parent", "Child", "Sibling", "Friend", "Other", "Legal Guardian" };

        foreach (var relationship in relationships)
        {
            var contact = EmergencyContact.Create("John Doe", relationship, "555-1234");
            contact.Relationship.Should().Be(relationship);
        }
    }

    [Fact]
    public void Create_WithInternationalPhoneNumber_ShouldSucceed()
    {
        // Arrange - Phone number with country code
        var contact = EmergencyContact.Create(
            "John Doe",
            "Spouse",
            "+1 (555) 123-4567");

        // Assert
        contact.PhoneNumber.Should().Be("+1 (555) 123-4567");
    }

    [Fact]
    public void Create_WithVariousPhoneFormats_ShouldPreserveFormat()
    {
        // Test different phone number formats
        var formats = new[]
        {
            "555-1234",
            "(555) 123-4567",
            "555.123.4567",
            "1-800-555-1234",
            "+1-555-123-4567",
            "5551234567"
        };

        foreach (var format in formats)
        {
            var contact = EmergencyContact.Create("John Doe", "Spouse", format);
            contact.PhoneNumber.Should().Be(format);
        }
    }

    [Fact]
    public void Create_WithUnicodeCharactersInName_ShouldSucceed()
    {
        // Arrange - Names with accents and special characters
        var contact = EmergencyContact.Create(
            "José García-Márquez",
            "Spouse",
            "555-1234");

        // Assert
        contact.Name.Should().Be("José García-Márquez");
    }

    [Fact]
    public void Create_WithLongName_ShouldSucceed()
    {
        // Arrange
        var longName = "Elizabeth Alexandra Mary Windsor-Mountbatten-von-Saxe-Coburg";
        var contact = EmergencyContact.Create(longName, "Spouse", "555-1234");

        // Assert
        contact.Name.Should().Be(longName);
    }

    [Fact]
    public void Create_WithSpecialCharactersInName_ShouldSucceed()
    {
        // Arrange
        var contact = EmergencyContact.Create(
            "O'Brien-Smith, Jr.",
            "Father",
            "555-1234");

        // Assert
        contact.Name.Should().Be("O'Brien-Smith, Jr.");
    }

    [Fact]
    public void Create_CaseInsensitiveRelationship_ShouldPreserveCase()
    {
        // Test that case is preserved
        var contact1 = EmergencyContact.Create("John Doe", "spouse", "555-1234");
        contact1.Relationship.Should().Be("spouse");

        var contact2 = EmergencyContact.Create("John Doe", "SPOUSE", "555-1234");
        contact2.Relationship.Should().Be("SPOUSE");
    }

    [Fact]
    public void Create_WithExtensionInPhoneNumber_ShouldSucceed()
    {
        // Arrange
        var contact = EmergencyContact.Create(
            "John Doe",
            "Spouse",
            "555-1234 ext. 123");

        // Assert
        contact.PhoneNumber.Should().Be("555-1234 ext. 123");
    }

    #endregion

    #region Collection Tests

    [Fact]
    public void EmergencyContact_CanBeUsedInHashSet()
    {
        // Arrange
        var hashSet = new HashSet<EmergencyContact>();
        var contact1 = EmergencyContact.Create("John Doe", "Spouse", "555-1234");
        var contact2 = EmergencyContact.Create("Jane Smith", "Mother", "555-5678");
        var contact1Duplicate = EmergencyContact.Create("John Doe", "Spouse", "555-1234");

        // Act
        hashSet.Add(contact1);
        hashSet.Add(contact2);
        hashSet.Add(contact1Duplicate);

        // Assert
        hashSet.Should().HaveCount(2); // Duplicate should not be added
    }

    [Fact]
    public void EmergencyContact_CanBeUsedAsKeyInDictionary()
    {
        // Arrange
        var dict = new Dictionary<EmergencyContact, string>();
        var contact1 = EmergencyContact.Create("John Doe", "Spouse", "555-1234");
        var contact2 = EmergencyContact.Create("Jane Smith", "Mother", "555-5678");

        // Act
        dict[contact1] = "Primary";
        dict[contact2] = "Secondary";

        // Assert
        dict.Should().HaveCount(2);
        dict[contact1].Should().Be("Primary");
        dict[contact2].Should().Be("Secondary");
    }

    #endregion
}
