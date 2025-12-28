using EMR.Domain.ValueObjects;
using FluentAssertions;

namespace EMR.UnitTests.Domain.ValueObjects;

/// <summary>
/// Unit tests for PatientId value object
/// Tests cover: creation, validation, conversions, and edge cases
/// </summary>
public class PatientIdTests
{
    #region Create from Guid Tests

    [Fact]
    public void Create_WithValidGuid_ShouldSucceed()
    {
        // Arrange
        var guid = Guid.NewGuid();

        // Act
        var patientId = PatientId.Create(guid);

        // Assert
        patientId.Should().NotBeNull();
        patientId.Value.Should().Be(guid);
    }

    [Fact]
    public void Create_WithEmptyGuid_ShouldThrowArgumentException()
    {
        // Arrange
        var emptyGuid = Guid.Empty;

        // Act
        Action act = () => PatientId.Create(emptyGuid);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Patient ID cannot be empty*")
            .And.ParamName.Should().Be("value");
    }

    #endregion

    #region Create from String Tests

    [Fact]
    public void Create_WithValidGuidString_ShouldSucceed()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var guidString = guid.ToString();

        // Act
        var patientId = PatientId.Create(guidString);

        // Assert
        patientId.Should().NotBeNull();
        patientId.Value.Should().Be(guid);
    }

    [Fact]
    public void Create_WithValidGuidStringUppercase_ShouldSucceed()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var guidString = guid.ToString().ToUpperInvariant();

        // Act
        var patientId = PatientId.Create(guidString);

        // Assert
        patientId.Should().NotBeNull();
        patientId.Value.Should().Be(guid);
    }

    [Fact]
    public void Create_WithGuidStringWithBraces_ShouldSucceed()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var guidString = guid.ToString("B"); // {xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx}

        // Act
        var patientId = PatientId.Create(guidString);

        // Assert
        patientId.Should().NotBeNull();
        patientId.Value.Should().Be(guid);
    }

    [Fact]
    public void Create_WithGuidStringWithParentheses_ShouldSucceed()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var guidString = guid.ToString("P"); // (xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx)

        // Act
        var patientId = PatientId.Create(guidString);

        // Assert
        patientId.Should().NotBeNull();
        patientId.Value.Should().Be(guid);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    [InlineData("\n")]
    public void Create_WithNullOrWhitespaceString_ShouldThrowArgumentException(string? invalidString)
    {
        // Act
        Action act = () => PatientId.Create(invalidString!);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Patient ID cannot be null or empty*")
            .And.ParamName.Should().Be("value");
    }

    [Theory]
    [InlineData("not-a-guid")]
    [InlineData("12345")]
    [InlineData("abc-def-ghi")]
    [InlineData("xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx")]
    public void Create_WithInvalidGuidString_ShouldThrowArgumentException(string invalidGuidString)
    {
        // Act
        Action act = () => PatientId.Create(invalidGuidString);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Invalid Patient ID format*")
            .And.ParamName.Should().Be("value");
    }

    [Fact]
    public void Create_WithEmptyGuidString_ShouldThrowArgumentException()
    {
        // Arrange
        var emptyGuidString = Guid.Empty.ToString();

        // Act
        Action act = () => PatientId.Create(emptyGuidString);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Patient ID cannot be empty*");
    }

    #endregion

    #region NewPatientId Tests

    [Fact]
    public void NewPatientId_ShouldCreateValidPatientId()
    {
        // Act
        var patientId = PatientId.NewPatientId();

        // Assert
        patientId.Should().NotBeNull();
        patientId.Value.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void NewPatientId_ShouldCreateUniqueIds()
    {
        // Act
        var ids = new HashSet<Guid>();
        for (int i = 0; i < 1000; i++)
        {
            var patientId = PatientId.NewPatientId();
            ids.Add(patientId.Value);
        }

        // Assert - All IDs should be unique
        ids.Should().HaveCount(1000);
    }

    [Fact]
    public void NewPatientId_CalledMultipleTimes_ShouldReturnDifferentValues()
    {
        // Act
        var id1 = PatientId.NewPatientId();
        var id2 = PatientId.NewPatientId();
        var id3 = PatientId.NewPatientId();

        // Assert
        id1.Should().NotBe(id2);
        id2.Should().NotBe(id3);
        id1.Should().NotBe(id3);
    }

    #endregion

    #region ToString Tests

    [Fact]
    public void ToString_ShouldReturnGuidAsString()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var patientId = PatientId.Create(guid);

        // Act
        var result = patientId.ToString();

        // Assert
        result.Should().Be(guid.ToString());
    }

    #endregion

    #region Implicit Conversion Tests

    [Fact]
    public void ImplicitConversion_ToGuid_ShouldWork()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var patientId = PatientId.Create(guid);

        // Act
        Guid result = patientId;

        // Assert
        result.Should().Be(guid);
    }

    [Fact]
    public void ImplicitConversion_ToString_ShouldWork()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var patientId = PatientId.Create(guid);

        // Act
        string result = patientId;

        // Assert
        result.Should().Be(guid.ToString());
    }

    #endregion

    #region Equality Tests (Record behavior)

    [Fact]
    public void Equality_TwoPatientIdsWithSameGuid_ShouldBeEqual()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var id1 = PatientId.Create(guid);
        var id2 = PatientId.Create(guid);

        // Assert
        id1.Should().Be(id2);
        (id1 == id2).Should().BeTrue();
        id1.Equals(id2).Should().BeTrue();
    }

    [Fact]
    public void Equality_TwoPatientIdsWithDifferentGuids_ShouldNotBeEqual()
    {
        // Arrange
        var id1 = PatientId.NewPatientId();
        var id2 = PatientId.NewPatientId();

        // Assert
        id1.Should().NotBe(id2);
        (id1 != id2).Should().BeTrue();
        id1.Equals(id2).Should().BeFalse();
    }

    [Fact]
    public void Equality_CompareWithNull_ShouldNotBeEqual()
    {
        // Arrange
        var patientId = PatientId.NewPatientId();

        // Assert
        patientId.Equals(null).Should().BeFalse();
        (patientId == null!).Should().BeFalse();
    }

    [Fact]
    public void GetHashCode_SamePatientIds_ShouldHaveSameHashCode()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var id1 = PatientId.Create(guid);
        var id2 = PatientId.Create(guid);

        // Assert
        id1.GetHashCode().Should().Be(id2.GetHashCode());
    }

    [Fact]
    public void GetHashCode_DifferentPatientIds_ShouldHaveDifferentHashCodes()
    {
        // Arrange
        var id1 = PatientId.NewPatientId();
        var id2 = PatientId.NewPatientId();

        // Assert
        id1.GetHashCode().Should().NotBe(id2.GetHashCode());
    }

    #endregion

    #region Collection Tests

    [Fact]
    public void PatientId_CanBeUsedAsKeyInDictionary()
    {
        // Arrange
        var dict = new Dictionary<PatientId, string>();
        var id1 = PatientId.NewPatientId();
        var id2 = PatientId.NewPatientId();

        // Act
        dict[id1] = "Patient 1";
        dict[id2] = "Patient 2";

        // Assert
        dict.Should().HaveCount(2);
        dict[id1].Should().Be("Patient 1");
        dict[id2].Should().Be("Patient 2");
    }

    [Fact]
    public void PatientId_CanBeUsedInHashSet()
    {
        // Arrange
        var hashSet = new HashSet<PatientId>();
        var id1 = PatientId.NewPatientId();
        var id2 = PatientId.NewPatientId();
        var id1Duplicate = PatientId.Create(id1.Value);

        // Act
        hashSet.Add(id1);
        hashSet.Add(id2);
        hashSet.Add(id1Duplicate);

        // Assert
        hashSet.Should().HaveCount(2); // id1Duplicate should not be added
        hashSet.Should().Contain(id1);
        hashSet.Should().Contain(id2);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Create_WithGuidStringWithExtraWhitespace_ShouldThrowArgumentException()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var guidStringWithSpaces = $"  {guid}  ";

        // Act & Assert
        // Note: The implementation doesn't trim, so this should fail
        var act = () => PatientId.Create(guidStringWithSpaces);

        // If implementation trims, this will pass. If not, it will throw.
        // Current implementation doesn't trim based on code review
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_FromString_PreservesOriginalGuid()
    {
        // Arrange
        var originalGuid = Guid.NewGuid();
        var guidString = originalGuid.ToString();

        // Act
        var patientId = PatientId.Create(guidString);

        // Assert
        patientId.Value.Should().Be(originalGuid);
        patientId.ToString().Should().Be(originalGuid.ToString());
    }

    #endregion
}
