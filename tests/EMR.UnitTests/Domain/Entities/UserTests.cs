using EMR.Domain.Entities;
using EMR.Domain.Enums;
using FluentAssertions;

namespace EMR.UnitTests.Domain.Entities;

/// <summary>
/// Unit tests for User entity
/// Tests cover: creation, validation, role management, business rules, and security
/// </summary>
public class UserTests
{
    private const string ValidEmail = "test@example.com";
    private const string ValidFirstName = "John";
    private const string ValidLastName = "Doe";
    private const string ValidAzureId = "12345678-1234-1234-1234-123456789abc";
    private const string CreatedBy = "system";

    #region Constructor Tests - Valid Data

    [Fact]
    public void Constructor_WithValidData_ShouldSucceed()
    {
        // Arrange
        var roles = new List<UserRole> { UserRole.Doctor };

        // Act
        var user = new User(
            email: ValidEmail,
            firstName: ValidFirstName,
            lastName: ValidLastName,
            azureAdB2CId: ValidAzureId,
            roles: roles,
            createdBy: CreatedBy);

        // Assert
        user.Should().NotBeNull();
        user.Email.Should().Be("test@example.com");
        user.FirstName.Should().Be("John");
        user.LastName.Should().Be("Doe");
        user.AzureAdB2CId.Should().Be(ValidAzureId);
        user.Roles.Should().BeEquivalentTo(roles);
        user.IsActive.Should().BeTrue();
        user.LastLoginAt.Should().BeNull();
        user.CreatedBy.Should().Be(CreatedBy);
    }

    [Fact]
    public void Constructor_EmailToLowerCase_ShouldSucceed()
    {
        // Act
        var user = new User(
            email: "Test@EXAMPLE.COM",
            firstName: ValidFirstName,
            lastName: ValidLastName,
            azureAdB2CId: ValidAzureId,
            roles: new List<UserRole> { UserRole.Doctor },
            createdBy: CreatedBy);

        // Assert
        user.Email.Should().Be("test@example.com");
    }

    [Fact]
    public void Constructor_TrimsWhitespace_ShouldSucceed()
    {
        // Act
        var user = new User(
            email: "  test@example.com  ",
            firstName: "  John  ",
            lastName: "  Doe  ",
            azureAdB2CId: "  " + ValidAzureId + "  ",
            roles: new List<UserRole> { UserRole.Doctor },
            createdBy: CreatedBy);

        // Assert
        user.Email.Should().Be("test@example.com");
        user.FirstName.Should().Be("John");
        user.LastName.Should().Be("Doe");
        user.AzureAdB2CId.Should().Be(ValidAzureId);
    }

    [Fact]
    public void Constructor_WithMultipleRoles_ShouldSucceed()
    {
        // Arrange
        var roles = new List<UserRole> { UserRole.Doctor, UserRole.Admin };

        // Act
        var user = new User(ValidEmail, ValidFirstName, ValidLastName, ValidAzureId, roles, CreatedBy);

        // Assert
        user.Roles.Should().HaveCount(2);
        user.Roles.Should().Contain(UserRole.Doctor);
        user.Roles.Should().Contain(UserRole.Admin);
    }

    #endregion

    #region Constructor Tests - Invalid Data

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithNullOrEmptyEmail_ShouldThrowArgumentException(string? invalidEmail)
    {
        // Act
        Action act = () => new User(
            email: invalidEmail!,
            firstName: ValidFirstName,
            lastName: ValidLastName,
            azureAdB2CId: ValidAzureId,
            roles: new List<UserRole> { UserRole.Doctor },
            createdBy: CreatedBy);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Email cannot be empty*");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithNullOrEmptyFirstName_ShouldThrowArgumentException(string? invalidFirstName)
    {
        // Act
        Action act = () => new User(
            email: ValidEmail,
            firstName: invalidFirstName!,
            lastName: ValidLastName,
            azureAdB2CId: ValidAzureId,
            roles: new List<UserRole> { UserRole.Doctor },
            createdBy: CreatedBy);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*First name cannot be empty*");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithNullOrEmptyLastName_ShouldThrowArgumentException(string? invalidLastName)
    {
        // Act
        Action act = () => new User(
            email: ValidEmail,
            firstName: ValidFirstName,
            lastName: invalidLastName!,
            azureAdB2CId: ValidAzureId,
            roles: new List<UserRole> { UserRole.Doctor },
            createdBy: CreatedBy);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Last name cannot be empty*");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithNullOrEmptyAzureId_ShouldThrowArgumentException(string? invalidAzureId)
    {
        // Act
        Action act = () => new User(
            email: ValidEmail,
            firstName: ValidFirstName,
            lastName: ValidLastName,
            azureAdB2CId: invalidAzureId!,
            roles: new List<UserRole> { UserRole.Doctor },
            createdBy: CreatedBy);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Azure AD B2C ID cannot be empty*");
    }

    [Fact]
    public void Constructor_WithNullRoles_ShouldThrowArgumentNullException()
    {
        // Act
        Action act = () => new User(
            email: ValidEmail,
            firstName: ValidFirstName,
            lastName: ValidLastName,
            azureAdB2CId: ValidAzureId,
            roles: null!,
            createdBy: CreatedBy);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("roles");
    }

    [Fact]
    public void Constructor_WithEmptyRoles_ShouldThrowArgumentException()
    {
        // Act
        Action act = () => new User(
            email: ValidEmail,
            firstName: ValidFirstName,
            lastName: ValidLastName,
            azureAdB2CId: ValidAzureId,
            roles: new List<UserRole>(),
            createdBy: CreatedBy);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*User must have at least one role*");
    }

    #endregion

    #region FullName Property Tests

    [Fact]
    public void FullName_ShouldReturnFirstAndLastName()
    {
        // Arrange
        var user = new User(ValidEmail, "John", "Doe", ValidAzureId, new List<UserRole> { UserRole.Doctor }, CreatedBy);

        // Act & Assert
        user.FullName.Should().Be("John Doe");
    }

    #endregion

    #region UpdateProfile Tests

    [Fact]
    public void UpdateProfile_WithValidData_ShouldSucceed()
    {
        // Arrange
        var user = new User(ValidEmail, ValidFirstName, ValidLastName, ValidAzureId, new List<UserRole> { UserRole.Doctor }, CreatedBy);

        // Act
        user.UpdateProfile("Jane", "Smith", "admin-user");

        // Assert
        user.FirstName.Should().Be("Jane");
        user.LastName.Should().Be("Smith");
        user.FullName.Should().Be("Jane Smith");
        user.UpdatedBy.Should().Be("admin-user");
        user.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void UpdateProfile_TrimsWhitespace_ShouldSucceed()
    {
        // Arrange
        var user = new User(ValidEmail, ValidFirstName, ValidLastName, ValidAzureId, new List<UserRole> { UserRole.Doctor }, CreatedBy);

        // Act
        user.UpdateProfile("  Jane  ", "  Smith  ", "admin-user");

        // Assert
        user.FirstName.Should().Be("Jane");
        user.LastName.Should().Be("Smith");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void UpdateProfile_WithInvalidFirstName_ShouldThrowArgumentException(string? invalidFirstName)
    {
        // Arrange
        var user = new User(ValidEmail, ValidFirstName, ValidLastName, ValidAzureId, new List<UserRole> { UserRole.Doctor }, CreatedBy);

        // Act
        Action act = () => user.UpdateProfile(invalidFirstName!, "Smith", "admin-user");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*First name cannot be empty*");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void UpdateProfile_WithInvalidLastName_ShouldThrowArgumentException(string? invalidLastName)
    {
        // Arrange
        var user = new User(ValidEmail, ValidFirstName, ValidLastName, ValidAzureId, new List<UserRole> { UserRole.Doctor }, CreatedBy);

        // Act
        Action act = () => user.UpdateProfile("Jane", invalidLastName!, "admin-user");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Last name cannot be empty*");
    }

    #endregion

    #region UpdateRoles Tests

    [Fact]
    public void UpdateRoles_WithValidRoles_ShouldSucceed()
    {
        // Arrange
        var user = new User(ValidEmail, ValidFirstName, ValidLastName, ValidAzureId, new List<UserRole> { UserRole.Doctor }, CreatedBy);
        var newRoles = new List<UserRole> { UserRole.Admin, UserRole.Doctor };

        // Act
        user.UpdateRoles(newRoles, "admin-user");

        // Assert
        user.Roles.Should().BeEquivalentTo(newRoles);
        user.UpdatedBy.Should().Be("admin-user");
        user.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void UpdateRoles_ReplacesAllExistingRoles_ShouldSucceed()
    {
        // Arrange
        var user = new User(ValidEmail, ValidFirstName, ValidLastName, ValidAzureId, new List<UserRole> { UserRole.Doctor, UserRole.Nurse }, CreatedBy);
        var newRoles = new List<UserRole> { UserRole.Staff };

        // Act
        user.UpdateRoles(newRoles, "admin-user");

        // Assert
        user.Roles.Should().HaveCount(1);
        user.Roles.Should().Contain(UserRole.Staff);
        user.Roles.Should().NotContain(UserRole.Doctor);
        user.Roles.Should().NotContain(UserRole.Nurse);
    }

    [Fact]
    public void UpdateRoles_WithNullRoles_ShouldThrowArgumentNullException()
    {
        // Arrange
        var user = new User(ValidEmail, ValidFirstName, ValidLastName, ValidAzureId, new List<UserRole> { UserRole.Doctor }, CreatedBy);

        // Act
        Action act = () => user.UpdateRoles(null!, "admin-user");

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("roles");
    }

    [Fact]
    public void UpdateRoles_WithEmptyRoles_ShouldThrowArgumentException()
    {
        // Arrange
        var user = new User(ValidEmail, ValidFirstName, ValidLastName, ValidAzureId, new List<UserRole> { UserRole.Doctor }, CreatedBy);

        // Act
        Action act = () => user.UpdateRoles(new List<UserRole>(), "admin-user");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*User must have at least one role*");
    }

    #endregion

    #region AddRole Tests

    [Fact]
    public void AddRole_WhenRoleDoesNotExist_ShouldAddRole()
    {
        // Arrange
        var user = new User(ValidEmail, ValidFirstName, ValidLastName, ValidAzureId, new List<UserRole> { UserRole.Doctor }, CreatedBy);

        // Act
        user.AddRole(UserRole.Admin, "admin-user");

        // Assert
        user.Roles.Should().Contain(UserRole.Admin);
        user.Roles.Should().Contain(UserRole.Doctor);
        user.UpdatedBy.Should().Be("admin-user");
        user.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void AddRole_WhenRoleAlreadyExists_ShouldNotDuplicate()
    {
        // Arrange
        var user = new User(ValidEmail, ValidFirstName, ValidLastName, ValidAzureId, new List<UserRole> { UserRole.Doctor }, CreatedBy);
        var originalUpdatedAt = user.UpdatedAt;

        // Act
        user.AddRole(UserRole.Doctor, "admin-user");

        // Assert
        user.Roles.Should().HaveCount(1);
        user.Roles.Should().Contain(UserRole.Doctor);
        user.UpdatedAt.Should().Be(originalUpdatedAt); // No update since role already exists
    }

    #endregion

    #region RemoveRole Tests

    [Fact]
    public void RemoveRole_WithMultipleRoles_ShouldRemoveRole()
    {
        // Arrange
        var user = new User(ValidEmail, ValidFirstName, ValidLastName, ValidAzureId, new List<UserRole> { UserRole.Doctor, UserRole.Admin }, CreatedBy);

        // Act
        user.RemoveRole(UserRole.Admin, "admin-user");

        // Assert
        user.Roles.Should().NotContain(UserRole.Admin);
        user.Roles.Should().Contain(UserRole.Doctor);
        user.UpdatedBy.Should().Be("admin-user");
        user.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void RemoveRole_WhenOnlyOneRole_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var user = new User(ValidEmail, ValidFirstName, ValidLastName, ValidAzureId, new List<UserRole> { UserRole.Doctor }, CreatedBy);

        // Act
        Action act = () => user.RemoveRole(UserRole.Doctor, "admin-user");

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*User must have at least one role*");
    }

    [Fact]
    public void RemoveRole_WhenRoleDoesNotExist_ShouldNotChangeRoles()
    {
        // Arrange
        var user = new User(ValidEmail, ValidFirstName, ValidLastName, ValidAzureId, new List<UserRole> { UserRole.Doctor, UserRole.Nurse }, CreatedBy);
        var originalUpdatedAt = user.UpdatedAt;

        // Act
        user.RemoveRole(UserRole.Admin, "admin-user");

        // Assert
        user.Roles.Should().HaveCount(2);
        user.UpdatedAt.Should().Be(originalUpdatedAt); // No update since role didn't exist
    }

    #endregion

    #region HasRole Tests

    [Fact]
    public void HasRole_WhenUserHasRole_ShouldReturnTrue()
    {
        // Arrange
        var user = new User(ValidEmail, ValidFirstName, ValidLastName, ValidAzureId, new List<UserRole> { UserRole.Doctor }, CreatedBy);

        // Act & Assert
        user.HasRole(UserRole.Doctor).Should().BeTrue();
    }

    [Fact]
    public void HasRole_WhenUserDoesNotHaveRole_ShouldReturnFalse()
    {
        // Arrange
        var user = new User(ValidEmail, ValidFirstName, ValidLastName, ValidAzureId, new List<UserRole> { UserRole.Doctor }, CreatedBy);

        // Act & Assert
        user.HasRole(UserRole.Admin).Should().BeFalse();
    }

    #endregion

    #region HasAnyRole Tests

    [Fact]
    public void HasAnyRole_WhenUserHasOneOfTheRoles_ShouldReturnTrue()
    {
        // Arrange
        var user = new User(ValidEmail, ValidFirstName, ValidLastName, ValidAzureId, new List<UserRole> { UserRole.Doctor }, CreatedBy);

        // Act & Assert
        user.HasAnyRole(UserRole.Admin, UserRole.Doctor).Should().BeTrue();
    }

    [Fact]
    public void HasAnyRole_WhenUserHasNoneOfTheRoles_ShouldReturnFalse()
    {
        // Arrange
        var user = new User(ValidEmail, ValidFirstName, ValidLastName, ValidAzureId, new List<UserRole> { UserRole.Doctor }, CreatedBy);

        // Act & Assert
        user.HasAnyRole(UserRole.Admin, UserRole.Staff).Should().BeFalse();
    }

    [Fact]
    public void HasAnyRole_WithMultipleMatches_ShouldReturnTrue()
    {
        // Arrange
        var user = new User(ValidEmail, ValidFirstName, ValidLastName, ValidAzureId, new List<UserRole> { UserRole.Doctor, UserRole.Admin }, CreatedBy);

        // Act & Assert
        user.HasAnyRole(UserRole.Admin, UserRole.Doctor).Should().BeTrue();
    }

    #endregion

    #region Activate/Deactivate Tests

    [Fact]
    public void Activate_WhenInactive_ShouldActivate()
    {
        // Arrange
        var user = new User(ValidEmail, ValidFirstName, ValidLastName, ValidAzureId, new List<UserRole> { UserRole.Doctor }, CreatedBy);
        user.Deactivate("admin-user");

        // Act
        user.Activate("admin-user");

        // Assert
        user.IsActive.Should().BeTrue();
        user.UpdatedBy.Should().Be("admin-user");
        user.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void Activate_WhenAlreadyActive_ShouldNotUpdateTimestamp()
    {
        // Arrange
        var user = new User(ValidEmail, ValidFirstName, ValidLastName, ValidAzureId, new List<UserRole> { UserRole.Doctor }, CreatedBy);
        var originalUpdatedAt = user.UpdatedAt;

        // Act
        user.Activate("admin-user");

        // Assert
        user.IsActive.Should().BeTrue();
        user.UpdatedAt.Should().Be(originalUpdatedAt);
    }

    [Fact]
    public void Deactivate_WhenActive_ShouldDeactivate()
    {
        // Arrange
        var user = new User(ValidEmail, ValidFirstName, ValidLastName, ValidAzureId, new List<UserRole> { UserRole.Doctor }, CreatedBy);

        // Act
        user.Deactivate("admin-user");

        // Assert
        user.IsActive.Should().BeFalse();
        user.UpdatedBy.Should().Be("admin-user");
        user.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void Deactivate_WhenAlreadyInactive_ShouldNotUpdateTimestamp()
    {
        // Arrange
        var user = new User(ValidEmail, ValidFirstName, ValidLastName, ValidAzureId, new List<UserRole> { UserRole.Doctor }, CreatedBy);
        user.Deactivate("admin-user");
        var updatedAt = user.UpdatedAt;

        // Act
        user.Deactivate("admin-user");

        // Assert
        user.IsActive.Should().BeFalse();
        user.UpdatedAt.Should().Be(updatedAt);
    }

    #endregion

    #region UpdateLastLogin Tests

    [Fact]
    public void UpdateLastLogin_ShouldSetLastLoginTimestamp()
    {
        // Arrange
        var user = new User(ValidEmail, ValidFirstName, ValidLastName, ValidAzureId, new List<UserRole> { UserRole.Doctor }, CreatedBy);
        var beforeUpdate = DateTime.UtcNow;

        // Act
        user.UpdateLastLogin();

        // Assert
        user.LastLoginAt.Should().NotBeNull();
        user.LastLoginAt.Should().BeOnOrAfter(beforeUpdate);
        user.LastLoginAt.Should().BeOnOrBefore(DateTime.UtcNow);
    }

    [Fact]
    public void UpdateLastLogin_CalledMultipleTimes_ShouldUpdateTimestamp()
    {
        // Arrange
        var user = new User(ValidEmail, ValidFirstName, ValidLastName, ValidAzureId, new List<UserRole> { UserRole.Doctor }, CreatedBy);
        user.UpdateLastLogin();
        var firstLogin = user.LastLoginAt;

        // Act
        Thread.Sleep(10); // Small delay to ensure different timestamp
        user.UpdateLastLogin();

        // Assert
        user.LastLoginAt.Should().NotBeNull();
        user.LastLoginAt.Should().BeAfter(firstLogin!.Value);
    }

    #endregion

    #region Edge Cases and Security

    [Fact]
    public void Constructor_WithSpecialCharactersInNames_ShouldSucceed()
    {
        // Act
        var user = new User(
            ValidEmail,
            "José-María",
            "O'Brien",
            ValidAzureId,
            new List<UserRole> { UserRole.Doctor },
            CreatedBy);

        // Assert
        user.FirstName.Should().Be("José-María");
        user.LastName.Should().Be("O'Brien");
    }

    [Fact]
    public void Constructor_EmailIsCaseInsensitive_ShouldStoreAsLowercase()
    {
        // Act
        var user1 = new User("TEST@EXAMPLE.COM", ValidFirstName, ValidLastName, ValidAzureId, new List<UserRole> { UserRole.Doctor }, CreatedBy);
        var user2 = new User("test@example.com", ValidFirstName, ValidLastName, ValidAzureId, new List<UserRole> { UserRole.Doctor }, CreatedBy);

        // Assert
        user1.Email.Should().Be(user2.Email);
    }

    [Fact]
    public void UpdateProfile_PreservesEmailAndAzureId()
    {
        // Arrange
        var user = new User(ValidEmail, ValidFirstName, ValidLastName, ValidAzureId, new List<UserRole> { UserRole.Doctor }, CreatedBy);
        var originalEmail = user.Email;
        var originalAzureId = user.AzureAdB2CId;

        // Act
        user.UpdateProfile("Jane", "Smith", "admin-user");

        // Assert
        user.Email.Should().Be(originalEmail);
        user.AzureAdB2CId.Should().Be(originalAzureId);
    }

    [Fact]
    public void UpdateRoles_PreservesOtherProperties()
    {
        // Arrange
        var user = new User(ValidEmail, ValidFirstName, ValidLastName, ValidAzureId, new List<UserRole> { UserRole.Doctor }, CreatedBy);
        user.UpdateLastLogin();
        var lastLogin = user.LastLoginAt;

        // Act
        user.UpdateRoles(new List<UserRole> { UserRole.Admin }, "admin-user");

        // Assert
        user.Email.Should().Be(ValidEmail);
        user.FirstName.Should().Be(ValidFirstName);
        user.LastLoginAt.Should().Be(lastLogin);
    }

    [Fact]
    public void Roles_IsReadOnlyCollection_CannotBeModifiedDirectly()
    {
        // Arrange
        var user = new User(ValidEmail, ValidFirstName, ValidLastName, ValidAzureId, new List<UserRole> { UserRole.Doctor }, CreatedBy);

        // Act & Assert
        user.Roles.Should().BeAssignableTo<IReadOnlyCollection<UserRole>>();
        // The Roles property returns a read-only collection, preventing direct modification
    }

    #endregion
}
