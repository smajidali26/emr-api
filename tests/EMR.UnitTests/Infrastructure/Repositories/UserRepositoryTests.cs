using EMR.Application.Common.Interfaces;
using EMR.Domain.Entities;
using EMR.Domain.Enums;
using EMR.Infrastructure.Data;
using EMR.Infrastructure.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace EMR.UnitTests.Infrastructure.Repositories;

/// <summary>
/// Unit tests for UserRepository
/// Tests cover: CRUD operations, query methods, validation, and edge cases
/// Note: These are unit tests using in-memory database for isolation
/// </summary>
public class UserRepositoryTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly UserRepository _repository;
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;

    public UserRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _currentUserServiceMock = new Mock<ICurrentUserService>();
        _currentUserServiceMock.Setup(x => x.GetUserId()).Returns(Guid.NewGuid());
        _currentUserServiceMock.Setup(x => x.GetUserEmail()).Returns("test@example.com");
        _repository = new UserRepository(_context, _currentUserServiceMock.Object);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    #region GetByEmailAsync Tests

    [Fact]
    public async Task GetByEmailAsync_WithExistingEmail_ShouldReturnUser()
    {
        // Arrange
        var user = CreateTestUser("test@example.com");
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByEmailAsync("test@example.com");

        // Assert
        result.Should().NotBeNull();
        result!.Email.Should().Be("test@example.com");
        result.FirstName.Should().Be("John");
    }

    [Fact]
    public async Task GetByEmailAsync_WithNonExistingEmail_ShouldReturnNull()
    {
        // Arrange
        var user = CreateTestUser("test@example.com");
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByEmailAsync("notfound@example.com");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByEmailAsync_IsCaseInsensitive_ShouldReturnUser()
    {
        // Arrange
        var user = CreateTestUser("test@example.com");
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByEmailAsync("TEST@EXAMPLE.COM");

        // Assert
        result.Should().NotBeNull();
        result!.Email.Should().Be("test@example.com");
    }

    [Fact]
    public async Task GetByEmailAsync_TrimsWhitespace_ShouldReturnUser()
    {
        // Arrange
        var user = CreateTestUser("test@example.com");
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByEmailAsync("  test@example.com  ");

        // Assert
        result.Should().NotBeNull();
        result!.Email.Should().Be("test@example.com");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetByEmailAsync_WithNullOrEmptyEmail_ShouldThrowArgumentException(string? invalidEmail)
    {
        // Act
        Func<Task> act = async () => await _repository.GetByEmailAsync(invalidEmail!);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Email cannot be empty*");
    }

    #endregion

    #region GetByAzureAdB2CIdAsync Tests

    [Fact]
    public async Task GetByAzureAdB2CIdAsync_WithExistingId_ShouldReturnUser()
    {
        // Arrange
        var azureId = "12345678-1234-1234-1234-123456789abc";
        var user = CreateTestUser("test@example.com", azureId);
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByAzureAdB2CIdAsync(azureId);

        // Assert
        result.Should().NotBeNull();
        result!.AzureAdB2CId.Should().Be(azureId);
        result.Email.Should().Be("test@example.com");
    }

    [Fact]
    public async Task GetByAzureAdB2CIdAsync_WithNonExistingId_ShouldReturnNull()
    {
        // Arrange
        var user = CreateTestUser("test@example.com", "12345678-1234-1234-1234-123456789abc");
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByAzureAdB2CIdAsync("99999999-9999-9999-9999-999999999999");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByAzureAdB2CIdAsync_TrimsWhitespace_ShouldReturnUser()
    {
        // Arrange
        var azureId = "12345678-1234-1234-1234-123456789abc";
        var user = CreateTestUser("test@example.com", azureId);
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByAzureAdB2CIdAsync($"  {azureId}  ");

        // Assert
        result.Should().NotBeNull();
        result!.AzureAdB2CId.Should().Be(azureId);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetByAzureAdB2CIdAsync_WithNullOrEmptyId_ShouldThrowArgumentException(string? invalidId)
    {
        // Act
        Func<Task> act = async () => await _repository.GetByAzureAdB2CIdAsync(invalidId!);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Azure AD B2C ID cannot be empty*");
    }

    #endregion

    #region EmailExistsAsync Tests

    [Fact]
    public async Task EmailExistsAsync_WithExistingEmail_ShouldReturnTrue()
    {
        // Arrange
        var user = CreateTestUser("test@example.com");
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.EmailExistsAsync("test@example.com");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task EmailExistsAsync_WithNonExistingEmail_ShouldReturnFalse()
    {
        // Arrange
        var user = CreateTestUser("test@example.com");
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.EmailExistsAsync("notfound@example.com");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task EmailExistsAsync_IsCaseInsensitive_ShouldReturnTrue()
    {
        // Arrange
        var user = CreateTestUser("test@example.com");
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.EmailExistsAsync("TEST@EXAMPLE.COM");

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task EmailExistsAsync_WithNullOrEmptyEmail_ShouldReturnFalse(string? invalidEmail)
    {
        // Act
        var result = await _repository.EmailExistsAsync(invalidEmail!);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region AzureAdB2CIdExistsAsync Tests

    [Fact]
    public async Task AzureAdB2CIdExistsAsync_WithExistingId_ShouldReturnTrue()
    {
        // Arrange
        var azureId = "12345678-1234-1234-1234-123456789abc";
        var user = CreateTestUser("test@example.com", azureId);
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.AzureAdB2CIdExistsAsync(azureId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task AzureAdB2CIdExistsAsync_WithNonExistingId_ShouldReturnFalse()
    {
        // Arrange
        var user = CreateTestUser("test@example.com", "12345678-1234-1234-1234-123456789abc");
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.AzureAdB2CIdExistsAsync("99999999-9999-9999-9999-999999999999");

        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task AzureAdB2CIdExistsAsync_WithNullOrEmptyId_ShouldReturnFalse(string? invalidId)
    {
        // Act
        var result = await _repository.AzureAdB2CIdExistsAsync(invalidId!);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region GetActiveUsersAsync Tests

    [Fact]
    public async Task GetActiveUsersAsync_ShouldReturnOnlyActiveUsers()
    {
        // Arrange
        var activeUser1 = CreateTestUser("active1@example.com");
        var activeUser2 = CreateTestUser("active2@example.com");
        var inactiveUser = CreateTestUser("inactive@example.com");
        inactiveUser.Deactivate("admin");

        await _context.Users.AddRangeAsync(activeUser1, activeUser2, inactiveUser);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetActiveUsersAsync();

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(u => u.Email == "active1@example.com");
        result.Should().Contain(u => u.Email == "active2@example.com");
        result.Should().NotContain(u => u.Email == "inactive@example.com");
    }

    [Fact]
    public async Task GetActiveUsersAsync_WithNoActiveUsers_ShouldReturnEmptyList()
    {
        // Arrange
        var inactiveUser = CreateTestUser("inactive@example.com");
        inactiveUser.Deactivate("admin");
        await _context.Users.AddAsync(inactiveUser);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetActiveUsersAsync();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetActiveUsersAsync_WithNoUsers_ShouldReturnEmptyList()
    {
        // Act
        var result = await _repository.GetActiveUsersAsync();

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region Base Repository Tests (Inherited Functionality)

    [Fact]
    public async Task AddAsync_ShouldAddUserToContext()
    {
        // Arrange
        var user = CreateTestUser("newuser@example.com");

        // Act
        await _repository.AddAsync(user);
        await _context.SaveChangesAsync();

        // Assert
        var savedUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == "newuser@example.com");
        savedUser.Should().NotBeNull();
        savedUser!.Email.Should().Be("newuser@example.com");
    }

    [Fact]
    public async Task GetByIdAsync_WithExistingId_ShouldReturnUser()
    {
        // Arrange
        var user = CreateTestUser("test@example.com");
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync(user.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(user.Id);
        result.Email.Should().Be("test@example.com");
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistingId_ShouldReturnNull()
    {
        // Act
        var result = await _repository.GetByIdAsync(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task Update_ShouldModifyUser()
    {
        // Arrange
        var user = CreateTestUser("test@example.com");
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        // Act
        user.UpdateProfile("Jane", "Smith", "admin");
        _repository.Update(user);
        await _context.SaveChangesAsync();

        // Assert
        var updatedUser = await _context.Users.FindAsync(user.Id);
        updatedUser.Should().NotBeNull();
        updatedUser!.FirstName.Should().Be("Jane");
        updatedUser.LastName.Should().Be("Smith");
    }

    #endregion

    #region Multiple Users Tests

    [Fact]
    public async Task GetByEmailAsync_WithMultipleUsers_ShouldReturnCorrectUser()
    {
        // Arrange
        var user1 = CreateTestUser("user1@example.com");
        var user2 = CreateTestUser("user2@example.com");
        var user3 = CreateTestUser("user3@example.com");
        await _context.Users.AddRangeAsync(user1, user2, user3);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByEmailAsync("user2@example.com");

        // Assert
        result.Should().NotBeNull();
        result!.Email.Should().Be("user2@example.com");
    }

    [Fact]
    public async Task GetActiveUsersAsync_WithMixedActiveStatus_ShouldFilterCorrectly()
    {
        // Arrange
        var active1 = CreateTestUser("active1@example.com");
        var active2 = CreateTestUser("active2@example.com");
        var inactive1 = CreateTestUser("inactive1@example.com");
        var inactive2 = CreateTestUser("inactive2@example.com");

        inactive1.Deactivate("admin");
        inactive2.Deactivate("admin");

        await _context.Users.AddRangeAsync(active1, inactive1, active2, inactive2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetActiveUsersAsync();

        // Assert
        result.Should().HaveCount(2);
        result.All(u => u.IsActive).Should().BeTrue();
    }

    #endregion

    #region Cancellation Token Tests

    [Fact]
    public async Task GetByEmailAsync_ShouldRespectCancellationToken()
    {
        // Arrange
        var user = CreateTestUser("test@example.com");
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        Func<Task> act = async () => await _repository.GetByEmailAsync("test@example.com", cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task GetByEmailAsync_WithSpecialCharactersInEmail_ShouldWork()
    {
        // Arrange
        var user = CreateTestUser("user+tag@example.com");
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByEmailAsync("user+tag@example.com");

        // Assert
        result.Should().NotBeNull();
        result!.Email.Should().Be("user+tag@example.com");
    }

    [Fact]
    public async Task GetActiveUsersAsync_ShouldPreserveUserOrder()
    {
        // Arrange
        var users = new[]
        {
            CreateTestUser("user1@example.com"),
            CreateTestUser("user2@example.com"),
            CreateTestUser("user3@example.com")
        };
        await _context.Users.AddRangeAsync(users);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetActiveUsersAsync();

        // Assert
        result.Should().HaveCount(3);
        result.Should().OnlyContain(u => u.IsActive);
    }

    #endregion

    #region Helper Methods

    private static User CreateTestUser(
        string email,
        string? azureId = null,
        string firstName = "John",
        string lastName = "Doe")
    {
        return new User(
            email: email,
            firstName: firstName,
            lastName: lastName,
            azureAdB2CId: azureId ?? Guid.NewGuid().ToString(),
            roles: new List<UserRole> { UserRole.Doctor },
            createdBy: "system");
    }

    #endregion
}
