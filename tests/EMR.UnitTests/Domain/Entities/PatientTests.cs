using EMR.Domain.Entities;
using EMR.Domain.Enums;
using EMR.Domain.ValueObjects;
using FluentAssertions;

namespace EMR.UnitTests.Domain.Entities;

/// <summary>
/// Unit tests for Patient entity
/// Tests cover: creation, validation, updates, business rules, and security
/// </summary>
public class PatientTests
{
    private readonly PatientAddress _validAddress;
    private readonly EmergencyContact _validEmergencyContact;
    private const string CreatedBy = "test-user";

    public PatientTests()
    {
        _validAddress = PatientAddress.Create(
            "123 Main St",
            null,
            "Springfield",
            "IL",
            "62701",
            "USA");

        _validEmergencyContact = EmergencyContact.Create(
            "Jane Doe",
            "Spouse",
            "555-1234");
    }

    #region Constructor Tests - Valid Data

    [Fact]
    public void Constructor_WithValidData_ShouldSucceed()
    {
        // Arrange
        var dateOfBirth = new DateTime(1990, 5, 15);

        // Act
        var patient = new Patient(
            firstName: "John",
            lastName: "Doe",
            dateOfBirth: dateOfBirth,
            gender: Gender.Male,
            phoneNumber: "555-1234",
            address: _validAddress,
            emergencyContact: _validEmergencyContact,
            createdBy: CreatedBy);

        // Assert
        patient.Should().NotBeNull();
        patient.FirstName.Should().Be("John");
        patient.LastName.Should().Be("Doe");
        patient.DateOfBirth.Should().Be(dateOfBirth.Date);
        patient.Gender.Should().Be(Gender.Male);
        patient.PhoneNumber.Should().Be("555-1234");
        patient.Address.Should().Be(_validAddress);
        patient.EmergencyContact.Should().Be(_validEmergencyContact);
        patient.IsActive.Should().BeTrue();
        patient.MedicalRecordNumber.Should().NotBeNull();
        patient.MedicalRecordNumber.Value.Should().StartWith("MRN-");
    }

    [Fact]
    public void Constructor_WithMiddleName_ShouldSucceed()
    {
        // Act
        var patient = new Patient(
            firstName: "John",
            lastName: "Doe",
            dateOfBirth: new DateTime(1990, 5, 15),
            gender: Gender.Male,
            phoneNumber: "555-1234",
            address: _validAddress,
            emergencyContact: _validEmergencyContact,
            createdBy: CreatedBy,
            middleName: "Michael");

        // Assert
        patient.MiddleName.Should().Be("Michael");
        patient.FullName.Should().Be("John Michael Doe");
    }

    [Fact]
    public void Constructor_WithOptionalFields_ShouldSucceed()
    {
        // Act
        var patient = new Patient(
            firstName: "John",
            lastName: "Doe",
            dateOfBirth: new DateTime(1990, 5, 15),
            gender: Gender.Male,
            phoneNumber: "555-1234",
            address: _validAddress,
            emergencyContact: _validEmergencyContact,
            createdBy: CreatedBy,
            email: "john.doe@example.com",
            alternatePhoneNumber: "555-5678",
            socialSecurityNumber: "123-45-6789",
            maritalStatus: MaritalStatus.Married,
            race: Race.White,
            ethnicity: Ethnicity.NotHispanicOrLatino,
            preferredLanguage: PreferredLanguage.English);

        // Assert
        patient.Email.Should().Be("john.doe@example.com");
        patient.AlternatePhoneNumber.Should().Be("555-5678");
        patient.SocialSecurityNumber.Should().Be("123-45-6789");
        patient.MaritalStatus.Should().Be(MaritalStatus.Married);
        patient.Race.Should().Be(Race.White);
        patient.Ethnicity.Should().Be(Ethnicity.NotHispanicOrLatino);
        patient.PreferredLanguage.Should().Be(PreferredLanguage.English);
    }

    [Fact]
    public void Constructor_TrimsWhitespace_ShouldSucceed()
    {
        // Act
        var patient = new Patient(
            firstName: "  John  ",
            lastName: "  Doe  ",
            dateOfBirth: new DateTime(1990, 5, 15),
            gender: Gender.Male,
            phoneNumber: "  555-1234  ",
            address: _validAddress,
            emergencyContact: _validEmergencyContact,
            createdBy: CreatedBy,
            middleName: "  Michael  ");

        // Assert
        patient.FirstName.Should().Be("John");
        patient.LastName.Should().Be("Doe");
        patient.MiddleName.Should().Be("Michael");
        patient.PhoneNumber.Should().Be("555-1234");
    }

    [Fact]
    public void Constructor_EmailToLowerCase_ShouldSucceed()
    {
        // Act
        var patient = new Patient(
            firstName: "John",
            lastName: "Doe",
            dateOfBirth: new DateTime(1990, 5, 15),
            gender: Gender.Male,
            phoneNumber: "555-1234",
            address: _validAddress,
            emergencyContact: _validEmergencyContact,
            createdBy: CreatedBy,
            email: "John.Doe@EXAMPLE.COM");

        // Assert
        patient.Email.Should().Be("john.doe@example.com");
    }

    [Fact]
    public void Constructor_StoresOnlyDatePart_ShouldSucceed()
    {
        // Arrange
        var dateTimeWithTime = new DateTime(1990, 5, 15, 14, 30, 0);

        // Act
        var patient = new Patient(
            firstName: "John",
            lastName: "Doe",
            dateOfBirth: dateTimeWithTime,
            gender: Gender.Male,
            phoneNumber: "555-1234",
            address: _validAddress,
            emergencyContact: _validEmergencyContact,
            createdBy: CreatedBy);

        // Assert
        patient.DateOfBirth.Should().Be(new DateTime(1990, 5, 15));
        patient.DateOfBirth.TimeOfDay.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void Constructor_GeneratesUniqueMRN_ShouldSucceed()
    {
        // Act
        var patient1 = new Patient("John", "Doe", new DateTime(1990, 5, 15), Gender.Male, "555-1234", _validAddress, _validEmergencyContact, CreatedBy);
        var patient2 = new Patient("Jane", "Smith", new DateTime(1985, 3, 20), Gender.Female, "555-5678", _validAddress, _validEmergencyContact, CreatedBy);

        // Assert
        patient1.MedicalRecordNumber.Should().NotBe(patient2.MedicalRecordNumber);
    }

    #endregion

    #region Constructor Tests - Invalid Data

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithNullOrEmptyFirstName_ShouldThrowArgumentException(string? invalidFirstName)
    {
        // Act
        Action act = () => new Patient(
            firstName: invalidFirstName!,
            lastName: "Doe",
            dateOfBirth: new DateTime(1990, 5, 15),
            gender: Gender.Male,
            phoneNumber: "555-1234",
            address: _validAddress,
            emergencyContact: _validEmergencyContact,
            createdBy: CreatedBy);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*First name is required*");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithNullOrEmptyLastName_ShouldThrowArgumentException(string? invalidLastName)
    {
        // Act
        Action act = () => new Patient(
            firstName: "John",
            lastName: invalidLastName!,
            dateOfBirth: new DateTime(1990, 5, 15),
            gender: Gender.Male,
            phoneNumber: "555-1234",
            address: _validAddress,
            emergencyContact: _validEmergencyContact,
            createdBy: CreatedBy);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Last name is required*");
    }

    [Fact]
    public void Constructor_WithFutureDateOfBirth_ShouldThrowArgumentException()
    {
        // Arrange
        var futureDate = DateTime.UtcNow.Date.AddDays(1);

        // Act
        Action act = () => new Patient(
            firstName: "John",
            lastName: "Doe",
            dateOfBirth: futureDate,
            gender: Gender.Male,
            phoneNumber: "555-1234",
            address: _validAddress,
            emergencyContact: _validEmergencyContact,
            createdBy: CreatedBy);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Date of birth cannot be in the future*");
    }

    [Fact]
    public void Constructor_WithDateOfBirthOver150YearsAgo_ShouldThrowArgumentException()
    {
        // Arrange
        var tooOldDate = DateTime.UtcNow.AddYears(-151);

        // Act
        Action act = () => new Patient(
            firstName: "John",
            lastName: "Doe",
            dateOfBirth: tooOldDate,
            gender: Gender.Male,
            phoneNumber: "555-1234",
            address: _validAddress,
            emergencyContact: _validEmergencyContact,
            createdBy: CreatedBy);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Date of birth is not valid*");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithNullOrEmptyPhoneNumber_ShouldThrowArgumentException(string? invalidPhone)
    {
        // Act
        Action act = () => new Patient(
            firstName: "John",
            lastName: "Doe",
            dateOfBirth: new DateTime(1990, 5, 15),
            gender: Gender.Male,
            phoneNumber: invalidPhone!,
            address: _validAddress,
            emergencyContact: _validEmergencyContact,
            createdBy: CreatedBy);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Phone number is required*");
    }

    [Fact]
    public void Constructor_WithNullAddress_ShouldThrowArgumentNullException()
    {
        // Act
        Action act = () => new Patient(
            firstName: "John",
            lastName: "Doe",
            dateOfBirth: new DateTime(1990, 5, 15),
            gender: Gender.Male,
            phoneNumber: "555-1234",
            address: null!,
            emergencyContact: _validEmergencyContact,
            createdBy: CreatedBy);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("address");
    }

    [Fact]
    public void Constructor_WithNullEmergencyContact_ShouldThrowArgumentNullException()
    {
        // Act
        Action act = () => new Patient(
            firstName: "John",
            lastName: "Doe",
            dateOfBirth: new DateTime(1990, 5, 15),
            gender: Gender.Male,
            phoneNumber: "555-1234",
            address: _validAddress,
            emergencyContact: null!,
            createdBy: CreatedBy);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("emergencyContact");
    }

    #endregion

    #region FullName Property Tests

    [Fact]
    public void FullName_WithoutMiddleName_ShouldReturnFirstAndLastName()
    {
        // Arrange
        var patient = new Patient("John", "Doe", new DateTime(1990, 5, 15), Gender.Male, "555-1234", _validAddress, _validEmergencyContact, CreatedBy);

        // Act & Assert
        patient.FullName.Should().Be("John Doe");
    }

    [Fact]
    public void FullName_WithMiddleName_ShouldReturnAllNames()
    {
        // Arrange
        var patient = new Patient("John", "Doe", new DateTime(1990, 5, 15), Gender.Male, "555-1234", _validAddress, _validEmergencyContact, CreatedBy, middleName: "Michael");

        // Act & Assert
        patient.FullName.Should().Be("John Michael Doe");
    }

    [Fact]
    public void FullName_WithEmptyMiddleName_ShouldReturnFirstAndLastName()
    {
        // Arrange
        var patient = new Patient("John", "Doe", new DateTime(1990, 5, 15), Gender.Male, "555-1234", _validAddress, _validEmergencyContact, CreatedBy, middleName: "");

        // Act & Assert
        patient.FullName.Should().Be("John Doe");
    }

    #endregion

    #region Age Property Tests

    [Fact]
    public void Age_ShouldCalculateCorrectly()
    {
        // Arrange - Patient born 30 years ago
        var dateOfBirth = DateTime.UtcNow.AddYears(-30).Date;
        var patient = new Patient("John", "Doe", dateOfBirth, Gender.Male, "555-1234", _validAddress, _validEmergencyContact, CreatedBy);

        // Act & Assert
        patient.Age.Should().Be(30);
    }

    [Fact]
    public void Age_BeforeBirthday_ShouldNotIncludeCurrentYear()
    {
        // Arrange - Birthday tomorrow
        var today = DateTime.UtcNow.Date;
        var dateOfBirth = today.AddYears(-30).AddDays(1);
        var patient = new Patient("John", "Doe", dateOfBirth, Gender.Male, "555-1234", _validAddress, _validEmergencyContact, CreatedBy);

        // Act & Assert
        patient.Age.Should().Be(29); // Not yet 30
    }

    [Fact]
    public void Age_OnBirthday_ShouldIncludeCurrentYear()
    {
        // Arrange - Birthday today
        var today = DateTime.UtcNow.Date;
        var dateOfBirth = today.AddYears(-30);
        var patient = new Patient("John", "Doe", dateOfBirth, Gender.Male, "555-1234", _validAddress, _validEmergencyContact, CreatedBy);

        // Act & Assert
        patient.Age.Should().Be(30);
    }

    [Fact]
    public void Age_Newborn_ShouldReturnZero()
    {
        // Arrange - Born today
        var today = DateTime.UtcNow.Date;
        var patient = new Patient("Baby", "Doe", today, Gender.Unknown, "555-1234", _validAddress, _validEmergencyContact, CreatedBy);

        // Act & Assert
        patient.Age.Should().Be(0);
    }

    #endregion

    #region UpdateDemographics Tests

    [Fact]
    public void UpdateDemographics_WithValidData_ShouldSucceed()
    {
        // Arrange
        var patient = new Patient("John", "Doe", new DateTime(1990, 5, 15), Gender.Male, "555-1234", _validAddress, _validEmergencyContact, CreatedBy);
        var newAddress = PatientAddress.Create("456 Oak St", null, "Chicago", "IL", "60601", "USA");

        // Act
        patient.UpdateDemographics(
            firstName: "Jane",
            lastName: "Smith",
            dateOfBirth: new DateTime(1992, 3, 20),
            gender: Gender.Female,
            phoneNumber: "555-9999",
            address: newAddress,
            updatedBy: "admin-user",
            email: "jane.smith@example.com");

        // Assert
        patient.FirstName.Should().Be("Jane");
        patient.LastName.Should().Be("Smith");
        patient.DateOfBirth.Should().Be(new DateTime(1992, 3, 20));
        patient.Gender.Should().Be(Gender.Female);
        patient.PhoneNumber.Should().Be("555-9999");
        patient.Email.Should().Be("jane.smith@example.com");
        patient.Address.Should().Be(newAddress);
        patient.UpdatedBy.Should().Be("admin-user");
        patient.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void UpdateDemographics_WithOptionalFields_ShouldUpdateOnlyProvidedFields()
    {
        // Arrange
        var patient = new Patient("John", "Doe", new DateTime(1990, 5, 15), Gender.Male, "555-1234", _validAddress, _validEmergencyContact, CreatedBy,
            maritalStatus: MaritalStatus.Single);

        // Act
        patient.UpdateDemographics(
            firstName: "John",
            lastName: "Doe",
            dateOfBirth: new DateTime(1990, 5, 15),
            gender: Gender.Male,
            phoneNumber: "555-1234",
            address: _validAddress,
            updatedBy: "admin-user",
            maritalStatus: MaritalStatus.Married);

        // Assert
        patient.MaritalStatus.Should().Be(MaritalStatus.Married);
    }

    [Fact]
    public void UpdateDemographics_WithNullOptionalFields_ShouldNotChangeExistingValues()
    {
        // Arrange
        var patient = new Patient("John", "Doe", new DateTime(1990, 5, 15), Gender.Male, "555-1234", _validAddress, _validEmergencyContact, CreatedBy,
            maritalStatus: MaritalStatus.Married);

        // Act
        patient.UpdateDemographics(
            firstName: "John",
            lastName: "Doe",
            dateOfBirth: new DateTime(1990, 5, 15),
            gender: Gender.Male,
            phoneNumber: "555-1234",
            address: _validAddress,
            updatedBy: "admin-user");

        // Assert
        patient.MaritalStatus.Should().Be(MaritalStatus.Married); // Unchanged
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void UpdateDemographics_WithInvalidFirstName_ShouldThrowArgumentException(string? invalidFirstName)
    {
        // Arrange
        var patient = new Patient("John", "Doe", new DateTime(1990, 5, 15), Gender.Male, "555-1234", _validAddress, _validEmergencyContact, CreatedBy);

        // Act
        Action act = () => patient.UpdateDemographics(
            firstName: invalidFirstName!,
            lastName: "Doe",
            dateOfBirth: new DateTime(1990, 5, 15),
            gender: Gender.Male,
            phoneNumber: "555-1234",
            address: _validAddress,
            updatedBy: "admin-user");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*First name is required*");
    }

    #endregion

    #region UpdateSocialSecurityNumber Tests

    [Fact]
    public void UpdateSocialSecurityNumber_WithValidSSN_ShouldSucceed()
    {
        // Arrange
        var patient = new Patient("John", "Doe", new DateTime(1990, 5, 15), Gender.Male, "555-1234", _validAddress, _validEmergencyContact, CreatedBy);

        // Act
        patient.UpdateSocialSecurityNumber("123-45-6789", "admin-user");

        // Assert
        patient.SocialSecurityNumber.Should().Be("123-45-6789");
        patient.UpdatedBy.Should().Be("admin-user");
        patient.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void UpdateSocialSecurityNumber_WithNullSSN_ShouldClearSSN()
    {
        // Arrange
        var patient = new Patient("John", "Doe", new DateTime(1990, 5, 15), Gender.Male, "555-1234", _validAddress, _validEmergencyContact, CreatedBy,
            socialSecurityNumber: "123-45-6789");

        // Act
        patient.UpdateSocialSecurityNumber(null, "admin-user");

        // Assert
        patient.SocialSecurityNumber.Should().BeNull();
    }

    [Fact]
    public void UpdateSocialSecurityNumber_TrimsWhitespace_ShouldSucceed()
    {
        // Arrange
        var patient = new Patient("John", "Doe", new DateTime(1990, 5, 15), Gender.Male, "555-1234", _validAddress, _validEmergencyContact, CreatedBy);

        // Act
        patient.UpdateSocialSecurityNumber("  123-45-6789  ", "admin-user");

        // Assert
        patient.SocialSecurityNumber.Should().Be("123-45-6789");
    }

    #endregion

    #region UpdateEmergencyContact Tests

    [Fact]
    public void UpdateEmergencyContact_WithValidContact_ShouldSucceed()
    {
        // Arrange
        var patient = new Patient("John", "Doe", new DateTime(1990, 5, 15), Gender.Male, "555-1234", _validAddress, _validEmergencyContact, CreatedBy);
        var newContact = EmergencyContact.Create("Bob Smith", "Brother", "555-9999");

        // Act
        patient.UpdateEmergencyContact(newContact, "admin-user");

        // Assert
        patient.EmergencyContact.Should().Be(newContact);
        patient.UpdatedBy.Should().Be("admin-user");
        patient.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void UpdateEmergencyContact_WithNullContact_ShouldThrowArgumentNullException()
    {
        // Arrange
        var patient = new Patient("John", "Doe", new DateTime(1990, 5, 15), Gender.Male, "555-1234", _validAddress, _validEmergencyContact, CreatedBy);

        // Act
        Action act = () => patient.UpdateEmergencyContact(null!, "admin-user");

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("emergencyContact");
    }

    #endregion

    #region Activate/Deactivate Tests

    [Fact]
    public void Activate_WhenInactive_ShouldActivate()
    {
        // Arrange
        var patient = new Patient("John", "Doe", new DateTime(1990, 5, 15), Gender.Male, "555-1234", _validAddress, _validEmergencyContact, CreatedBy);
        patient.Deactivate("admin-user");

        // Act
        patient.Activate("admin-user");

        // Assert
        patient.IsActive.Should().BeTrue();
        patient.UpdatedBy.Should().Be("admin-user");
        patient.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void Activate_WhenAlreadyActive_ShouldNotUpdateTimestamp()
    {
        // Arrange
        var patient = new Patient("John", "Doe", new DateTime(1990, 5, 15), Gender.Male, "555-1234", _validAddress, _validEmergencyContact, CreatedBy);
        var originalUpdatedAt = patient.UpdatedAt;

        // Act
        patient.Activate("admin-user");

        // Assert
        patient.IsActive.Should().BeTrue();
        patient.UpdatedAt.Should().Be(originalUpdatedAt); // No change
    }

    [Fact]
    public void Deactivate_WhenActive_ShouldDeactivate()
    {
        // Arrange
        var patient = new Patient("John", "Doe", new DateTime(1990, 5, 15), Gender.Male, "555-1234", _validAddress, _validEmergencyContact, CreatedBy);

        // Act
        patient.Deactivate("admin-user");

        // Assert
        patient.IsActive.Should().BeFalse();
        patient.UpdatedBy.Should().Be("admin-user");
        patient.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void Deactivate_WhenAlreadyInactive_ShouldNotUpdateTimestamp()
    {
        // Arrange
        var patient = new Patient("John", "Doe", new DateTime(1990, 5, 15), Gender.Male, "555-1234", _validAddress, _validEmergencyContact, CreatedBy);
        patient.Deactivate("admin-user");
        var updatedAt = patient.UpdatedAt;

        // Act
        patient.Deactivate("admin-user");

        // Assert
        patient.IsActive.Should().BeFalse();
        patient.UpdatedAt.Should().Be(updatedAt); // No change
    }

    #endregion

    #region Edge Cases and Security

    [Fact]
    public void Constructor_WithSensitiveData_ShouldStoreSecurely()
    {
        // Arrange & Act
        var patient = new Patient(
            firstName: "John",
            lastName: "Doe",
            dateOfBirth: new DateTime(1990, 5, 15),
            gender: Gender.Male,
            phoneNumber: "555-1234",
            address: _validAddress,
            emergencyContact: _validEmergencyContact,
            createdBy: CreatedBy,
            socialSecurityNumber: "123-45-6789");

        // Assert - SSN is stored as-is (encryption happens at infrastructure layer)
        patient.SocialSecurityNumber.Should().Be("123-45-6789");
    }

    [Fact]
    public void Constructor_WithLeapYearBirthday_ShouldSucceed()
    {
        // Arrange
        var leapYearDate = new DateTime(2000, 2, 29);

        // Act
        var patient = new Patient("John", "Doe", leapYearDate, Gender.Male, "555-1234", _validAddress, _validEmergencyContact, CreatedBy);

        // Assert
        patient.DateOfBirth.Should().Be(leapYearDate);
    }

    [Fact]
    public void UpdateDemographics_PreservesCreatedByAndCreatedAt()
    {
        // Arrange
        var patient = new Patient("John", "Doe", new DateTime(1990, 5, 15), Gender.Male, "555-1234", _validAddress, _validEmergencyContact, CreatedBy);
        var originalCreatedBy = patient.CreatedBy;
        var originalCreatedAt = patient.CreatedAt;

        // Act
        patient.UpdateDemographics("Jane", "Smith", new DateTime(1990, 5, 15), Gender.Female, "555-9999", _validAddress, "different-user");

        // Assert
        patient.CreatedBy.Should().Be(originalCreatedBy);
        patient.CreatedAt.Should().Be(originalCreatedAt);
    }

    [Fact]
    public void Constructor_WithSpecialCharactersInNames_ShouldSucceed()
    {
        // Act
        var patient = new Patient(
            firstName: "José",
            lastName: "O'Brien-García",
            dateOfBirth: new DateTime(1990, 5, 15),
            gender: Gender.Male,
            phoneNumber: "555-1234",
            address: _validAddress,
            emergencyContact: _validEmergencyContact,
            createdBy: CreatedBy);

        // Assert
        patient.FirstName.Should().Be("José");
        patient.LastName.Should().Be("O'Brien-García");
    }

    [Fact]
    public void Constructor_DefaultsToUnknownForOptionalEnums()
    {
        // Act
        var patient = new Patient("John", "Doe", new DateTime(1990, 5, 15), Gender.Male, "555-1234", _validAddress, _validEmergencyContact, CreatedBy);

        // Assert
        patient.MaritalStatus.Should().Be(MaritalStatus.Unknown);
        patient.Race.Should().Be(Race.Unknown);
        patient.Ethnicity.Should().Be(Ethnicity.Unknown);
        patient.PreferredLanguage.Should().Be(PreferredLanguage.English);
    }

    #endregion
}
