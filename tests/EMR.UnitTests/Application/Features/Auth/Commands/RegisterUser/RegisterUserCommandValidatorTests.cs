using EMR.Application.Features.Auth.Commands.RegisterUser;
using EMR.Domain.Enums;
using FluentAssertions;
using FluentValidation.TestHelper;

namespace EMR.UnitTests.Application.Features.Auth.Commands.RegisterUser;

/// <summary>
/// Unit tests for RegisterUserCommandValidator
/// Tests cover: email validation, name validation, role validation, business rules, and security
/// </summary>
public class RegisterUserCommandValidatorTests
{
    private readonly RegisterUserCommandValidator _validator;

    public RegisterUserCommandValidatorTests()
    {
        _validator = new RegisterUserCommandValidator();
    }

    #region Email Validation Tests

    [Fact]
    public void Validate_WithValidEmail_ShouldNotHaveError()
    {
        // Arrange
        var command = CreateValidCommand();

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Email);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithEmptyEmail_ShouldHaveError(string emptyEmail)
    {
        // Arrange
        var command = CreateValidCommand() with { Email = emptyEmail };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Email)
            .WithErrorMessage("Email is required.");
    }

    [Theory]
    [InlineData("invalid-email")]
    [InlineData("@example.com")]
    [InlineData("user@")]
    [InlineData("user")]
    [InlineData("user.example.com")]
    [InlineData("user @example.com")]
    public void Validate_WithInvalidEmailFormat_ShouldHaveError(string invalidEmail)
    {
        // Arrange
        var command = CreateValidCommand() with { Email = invalidEmail };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Email)
            .WithErrorMessage("Email must be a valid email address.");
    }

    [Fact]
    public void Validate_WithEmailExceeding255Characters_ShouldHaveError()
    {
        // Arrange
        var longEmail = new string('a', 250) + "@example.com"; // 262 characters
        var command = CreateValidCommand() with { Email = longEmail };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Email)
            .WithErrorMessage("Email must not exceed 255 characters.");
    }

    [Theory]
    [InlineData("user@example.com")]
    [InlineData("user.name@example.com")]
    [InlineData("user+tag@example.co.uk")]
    [InlineData("user_name@subdomain.example.com")]
    [InlineData("123@example.com")]
    public void Validate_WithValidEmailFormats_ShouldNotHaveError(string validEmail)
    {
        // Arrange
        var command = CreateValidCommand() with { Email = validEmail };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Email);
    }

    #endregion

    #region FirstName Validation Tests

    [Fact]
    public void Validate_WithValidFirstName_ShouldNotHaveError()
    {
        // Arrange
        var command = CreateValidCommand();

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.FirstName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithEmptyFirstName_ShouldHaveError(string emptyFirstName)
    {
        // Arrange
        var command = CreateValidCommand() with { FirstName = emptyFirstName };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.FirstName)
            .WithErrorMessage("First name is required.");
    }

    [Fact]
    public void Validate_WithFirstNameExceeding100Characters_ShouldHaveError()
    {
        // Arrange
        var longFirstName = new string('A', 101);
        var command = CreateValidCommand() with { FirstName = longFirstName };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.FirstName)
            .WithErrorMessage("First name must not exceed 100 characters.");
    }

    [Theory]
    [InlineData("John")]
    [InlineData("Mary-Jane")]
    [InlineData("José")]
    [InlineData("O'Brien")]
    public void Validate_WithValidFirstNames_ShouldNotHaveError(string validFirstName)
    {
        // Arrange
        var command = CreateValidCommand() with { FirstName = validFirstName };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.FirstName);
    }

    #endregion

    #region LastName Validation Tests

    [Fact]
    public void Validate_WithValidLastName_ShouldNotHaveError()
    {
        // Arrange
        var command = CreateValidCommand();

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.LastName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithEmptyLastName_ShouldHaveError(string emptyLastName)
    {
        // Arrange
        var command = CreateValidCommand() with { LastName = emptyLastName };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.LastName)
            .WithErrorMessage("Last name is required.");
    }

    [Fact]
    public void Validate_WithLastNameExceeding100Characters_ShouldHaveError()
    {
        // Arrange
        var longLastName = new string('A', 101);
        var command = CreateValidCommand() with { LastName = longLastName };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.LastName)
            .WithErrorMessage("Last name must not exceed 100 characters.");
    }

    #endregion

    #region AzureAdB2CId Validation Tests

    [Fact]
    public void Validate_WithValidAzureId_ShouldNotHaveError()
    {
        // Arrange
        var command = CreateValidCommand();

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.AzureAdB2CId);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithEmptyAzureId_ShouldHaveError(string emptyAzureId)
    {
        // Arrange
        var command = CreateValidCommand() with { AzureAdB2CId = emptyAzureId };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.AzureAdB2CId)
            .WithErrorMessage("Azure AD B2C ID is required.");
    }

    [Fact]
    public void Validate_WithAzureIdExceeding255Characters_ShouldHaveError()
    {
        // Arrange
        var longAzureId = new string('a', 256);
        var command = CreateValidCommand() with { AzureAdB2CId = longAzureId };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.AzureAdB2CId)
            .WithErrorMessage("Azure AD B2C ID must not exceed 255 characters.");
    }

    [Theory]
    [InlineData("not-a-guid")]
    [InlineData("12345")]
    [InlineData("invalid-format")]
    [InlineData("xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx")]
    public void Validate_WithInvalidGuidFormat_ShouldHaveError(string invalidGuid)
    {
        // Arrange
        var command = CreateValidCommand() with { AzureAdB2CId = invalidGuid };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.AzureAdB2CId)
            .WithErrorMessage("Azure AD B2C ID must be a valid GUID format.");
    }

    [Theory]
    [InlineData("12345678-1234-1234-1234-123456789abc")]
    [InlineData("ABCDEF12-3456-7890-ABCD-EF1234567890")]
    [InlineData("{12345678-1234-1234-1234-123456789abc}")]
    [InlineData("(12345678-1234-1234-1234-123456789abc)")]
    public void Validate_WithValidGuidFormats_ShouldNotHaveError(string validGuid)
    {
        // Arrange
        var command = CreateValidCommand() with { AzureAdB2CId = validGuid };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.AzureAdB2CId);
    }

    #endregion

    #region Roles Validation Tests

    [Fact]
    public void Validate_WithValidRoles_ShouldNotHaveError()
    {
        // Arrange
        var command = CreateValidCommand();

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Roles);
    }

    [Fact]
    public void Validate_WithEmptyRoles_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand() with { Roles = new List<UserRole>() };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Roles)
            .WithErrorMessage("At least one role is required.");
    }

    [Fact]
    public void Validate_WithDuplicateRoles_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand() with
        {
            Roles = new List<UserRole> { UserRole.Doctor, UserRole.Nurse, UserRole.Doctor }
        };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Roles)
            .WithErrorMessage("Duplicate roles are not allowed.");
    }

    [Fact]
    public void Validate_WithPatientAndDoctorRoles_ShouldHaveError()
    {
        // Arrange - Patient role cannot be combined with Doctor
        var command = CreateValidCommand() with
        {
            Roles = new List<UserRole> { UserRole.Patient, UserRole.Doctor }
        };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Roles)
            .WithErrorMessage("Patient role cannot be combined with Doctor, Nurse, Staff, or Admin roles.");
    }

    [Fact]
    public void Validate_WithPatientAndNurseRoles_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand() with
        {
            Roles = new List<UserRole> { UserRole.Patient, UserRole.Nurse }
        };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Roles)
            .WithErrorMessage("Patient role cannot be combined with Doctor, Nurse, Staff, or Admin roles.");
    }

    [Fact]
    public void Validate_WithPatientAndStaffRoles_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand() with
        {
            Roles = new List<UserRole> { UserRole.Patient, UserRole.Staff }
        };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Roles)
            .WithErrorMessage("Patient role cannot be combined with Doctor, Nurse, Staff, or Admin roles.");
    }

    [Fact]
    public void Validate_WithPatientAndAdminRoles_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand() with
        {
            Roles = new List<UserRole> { UserRole.Patient, UserRole.Admin }
        };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Roles)
            .WithErrorMessage("Patient role cannot be combined with Doctor, Nurse, Staff, or Admin roles.");
    }

    [Fact]
    public void Validate_WithPatientRoleOnly_ShouldNotHaveError()
    {
        // Arrange - Patient can be alone
        var command = CreateValidCommand() with
        {
            Roles = new List<UserRole> { UserRole.Patient }
        };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Roles);
    }

    [Fact]
    public void Validate_WithDoctorAndNurseRoles_ShouldNotHaveError()
    {
        // Arrange - Medical roles can be combined
        var command = CreateValidCommand() with
        {
            Roles = new List<UserRole> { UserRole.Doctor, UserRole.Nurse }
        };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Roles);
    }

    [Fact]
    public void Validate_WithDoctorAndAdminRoles_ShouldNotHaveError()
    {
        // Arrange - Doctor can also be Admin
        var command = CreateValidCommand() with
        {
            Roles = new List<UserRole> { UserRole.Doctor, UserRole.Admin }
        };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Roles);
    }

    [Fact]
    public void Validate_WithAllMedicalRoles_ShouldNotHaveError()
    {
        // Arrange
        var command = CreateValidCommand() with
        {
            Roles = new List<UserRole> { UserRole.Doctor, UserRole.Nurse, UserRole.Staff, UserRole.Admin }
        };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Roles);
    }

    #endregion

    #region Complete Validation Tests

    [Fact]
    public void Validate_WithAllValidData_ShouldPass()
    {
        // Arrange
        var command = CreateValidCommand();

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithMultipleErrors_ShouldReturnAllErrors()
    {
        // Arrange
        var command = new RegisterUserCommand
        {
            Email = "", // Invalid
            FirstName = "", // Invalid
            LastName = "", // Invalid
            AzureAdB2CId = "", // Invalid
            Roles = new List<UserRole>() // Invalid
        };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Email);
        result.ShouldHaveValidationErrorFor(x => x.FirstName);
        result.ShouldHaveValidationErrorFor(x => x.LastName);
        result.ShouldHaveValidationErrorFor(x => x.AzureAdB2CId);
        result.ShouldHaveValidationErrorFor(x => x.Roles);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Validate_WithEmailAtExactly255Characters_ShouldNotHaveError()
    {
        // Arrange - Email at max length
        var localPart = new string('a', 240);
        var email = $"{localPart}@example.com"; // Exactly 255 characters
        var command = CreateValidCommand() with { Email = email };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Validate_WithNameAtExactly100Characters_ShouldNotHaveError()
    {
        // Arrange
        var name = new string('A', 100);
        var command = CreateValidCommand() with { FirstName = name, LastName = name };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.FirstName);
        result.ShouldNotHaveValidationErrorFor(x => x.LastName);
    }

    [Fact]
    public void Validate_WithGuidInDifferentFormats_ShouldAllBeValid()
    {
        // Test different GUID formats are all accepted
        var guidFormats = new[]
        {
            "12345678-1234-1234-1234-123456789abc", // Standard
            "{12345678-1234-1234-1234-123456789abc}", // With braces
            "(12345678-1234-1234-1234-123456789abc)", // With parentheses
            "12345678123412341234123456789abc" // No dashes
        };

        foreach (var guidFormat in guidFormats)
        {
            var command = CreateValidCommand() with { AzureAdB2CId = guidFormat };
            var result = _validator.TestValidate(command);
            result.ShouldNotHaveValidationErrorFor(x => x.AzureAdB2CId);
        }
    }

    #endregion

    #region Helper Methods

    private static RegisterUserCommand CreateValidCommand()
    {
        return new RegisterUserCommand
        {
            Email = "test@example.com",
            FirstName = "John",
            LastName = "Doe",
            AzureAdB2CId = "12345678-1234-1234-1234-123456789abc",
            Roles = new List<UserRole> { UserRole.Doctor }
        };
    }

    #endregion
}
