using EMR.Application.Common.DTOs;
using EMR.Application.Common.Exceptions;
using EMR.Application.Common.Interfaces;
using EMR.Application.Features.Auth.Commands.RegisterUser;
using EMR.Application.Features.Auth.DTOs;
using EMR.Domain.Entities;
using EMR.Domain.Enums;
using EMR.Domain.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace EMR.UnitTests.Application.Features.Auth.Commands.RegisterUser;

/// <summary>
/// Unit tests for RegisterUserCommandHandler
/// Tests cover: successful registration, error handling, audit logging, and security
/// </summary>
public class RegisterUserCommandHandlerTests
{
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<ICurrentUserService> _mockCurrentUserService;
    private readonly Mock<IAuditLogger> _mockAuditLogger;
    private readonly Mock<ILogger<RegisterUserCommandHandler>> _mockLogger;
    private readonly RegisterUserCommandHandler _handler;

    public RegisterUserCommandHandlerTests()
    {
        _mockUserRepository = new Mock<IUserRepository>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockCurrentUserService = new Mock<ICurrentUserService>();
        _mockAuditLogger = new Mock<IAuditLogger>();
        _mockLogger = new Mock<ILogger<RegisterUserCommandHandler>>();

        _handler = new RegisterUserCommandHandler(
            _mockUserRepository.Object,
            _mockUnitOfWork.Object,
            _mockCurrentUserService.Object,
            _mockAuditLogger.Object,
            _mockLogger.Object);

        // Setup default current user service behavior
        _mockCurrentUserService.Setup(x => x.GetIpAddress()).Returns("127.0.0.1");
        _mockCurrentUserService.Setup(x => x.GetUserEmail()).Returns("admin@example.com");
    }

    #region Successful Registration Tests

    [Fact]
    public async Task Handle_WithValidCommand_ShouldSucceed()
    {
        // Arrange
        var command = CreateValidCommand();
        _mockUnitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Email.Should().Be(command.Email);
        result.Data.FirstName.Should().Be(command.FirstName);
        result.Data.LastName.Should().Be(command.LastName);
        result.Data.Roles.Should().BeEquivalentTo(command.Roles);
    }

    [Fact]
    public async Task Handle_WithValidCommand_ShouldAddUserToRepository()
    {
        // Arrange
        var command = CreateValidCommand();
        User? capturedUser = null;
        _mockUserRepository.Setup(x => x.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Callback<User, CancellationToken>((user, _) => capturedUser = user)
            .ReturnsAsync((User user, CancellationToken _) => user);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        capturedUser.Should().NotBeNull();
        capturedUser!.Email.Should().Be(command.Email.ToLowerInvariant());
        capturedUser.FirstName.Should().Be(command.FirstName);
        capturedUser.LastName.Should().Be(command.LastName);
        capturedUser.AzureAdB2CId.Should().Be(command.AzureAdB2CId);
        capturedUser.Roles.Should().BeEquivalentTo(command.Roles);
    }

    [Fact]
    public async Task Handle_WithValidCommand_ShouldSaveChanges()
    {
        // Arrange
        var command = CreateValidCommand();

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithValidCommand_ShouldLogAuditSuccess()
    {
        // Arrange
        var command = CreateValidCommand();

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _mockAuditLogger.Verify(
            x => x.LogUserRegistrationAsync(
                It.IsAny<string>(),
                command.Email,
                command.Roles,
                "127.0.0.1",
                true,
                null,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithValidCommand_ShouldReturnUserDto()
    {
        // Arrange
        var command = CreateValidCommand();

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Data.Should().NotBeNull();
        result.Data.Should().BeOfType<UserDto>();
        result.Data!.Id.Should().NotBe(Guid.Empty);
        result.Data.Email.Should().Be(command.Email);
        result.Data.FullName.Should().Be($"{command.FirstName} {command.LastName}");
        result.Data.IsActive.Should().BeTrue();
        result.Data.LastLoginAt.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WithNoCurrentUser_ShouldUseSystemAsCreatedBy()
    {
        // Arrange
        var command = CreateValidCommand();
        _mockCurrentUserService.Setup(x => x.GetUserEmail()).Returns((string?)null);
        User? capturedUser = null;
        _mockUserRepository.Setup(x => x.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Callback<User, CancellationToken>((user, _) => capturedUser = user)
            .ReturnsAsync((User user, CancellationToken _) => user);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        capturedUser.Should().NotBeNull();
        capturedUser!.CreatedBy.Should().Be("system");
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task Handle_WithDuplicateEmail_ShouldReturnFailure()
    {
        // Arrange
        var command = CreateValidCommand();
        _mockUnitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DuplicateEntityException("User with this email already exists", "Email"));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("User with this email already exists");
    }

    [Fact]
    public async Task Handle_WithDuplicateEmail_ShouldLogAuditFailure()
    {
        // Arrange
        var command = CreateValidCommand();
        _mockUnitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DuplicateEntityException("User with this email already exists", "Email"));

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _mockAuditLogger.Verify(
            x => x.LogUserRegistrationAsync(
                "N/A",
                command.Email,
                command.Roles,
                "127.0.0.1",
                false,
                "User with this email already exists",
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithArgumentException_ShouldReturnFailure()
    {
        // Arrange
        var command = CreateValidCommand();
        _mockUnitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException("Invalid data"));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Invalid data");
    }

    [Fact]
    public async Task Handle_WithUnexpectedException_ShouldReturnGenericFailure()
    {
        // Arrange
        var command = CreateValidCommand();
        _mockUnitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("An error occurred while registering the user. Please try again.");
    }

    [Fact]
    public async Task Handle_WithUnexpectedException_ShouldLogError()
    {
        // Arrange
        var command = CreateValidCommand();
        var exception = new InvalidOperationException("Database error");
        _mockUnitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region Security and Audit Tests

    [Fact]
    public async Task Handle_ShouldCaptureIpAddress()
    {
        // Arrange
        var command = CreateValidCommand();
        var expectedIp = "192.168.1.100";
        _mockCurrentUserService.Setup(x => x.GetIpAddress()).Returns(expectedIp);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _mockAuditLogger.Verify(
            x => x.LogUserRegistrationAsync(
                It.IsAny<string>(),
                command.Email,
                command.Roles,
                expectedIp,
                true,
                null,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithNullIpAddress_ShouldStillSucceed()
    {
        // Arrange
        var command = CreateValidCommand();
        _mockCurrentUserService.Setup(x => x.GetIpAddress()).Returns((string?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _mockAuditLogger.Verify(
            x => x.LogUserRegistrationAsync(
                It.IsAny<string>(),
                command.Email,
                command.Roles,
                null,
                true,
                null,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldLogInformationMessages()
    {
        // Arrange
        var command = CreateValidCommand();

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    #endregion

    #region Data Integrity Tests

    [Fact]
    public async Task Handle_ShouldNormalizeEmailToLowerCase()
    {
        // Arrange
        var command = CreateValidCommand() with { Email = "TEST@EXAMPLE.COM" };
        User? capturedUser = null;
        _mockUserRepository.Setup(x => x.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Callback<User, CancellationToken>((user, _) => capturedUser = user)
            .ReturnsAsync((User user, CancellationToken _) => user);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        capturedUser.Should().NotBeNull();
        capturedUser!.Email.Should().Be("test@example.com");
    }

    [Fact]
    public async Task Handle_ShouldTrimWhitespaceFromNames()
    {
        // Arrange
        var command = CreateValidCommand() with
        {
            FirstName = "  John  ",
            LastName = "  Doe  "
        };
        User? capturedUser = null;
        _mockUserRepository.Setup(x => x.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Callback<User, CancellationToken>((user, _) => capturedUser = user)
            .ReturnsAsync((User user, CancellationToken _) => user);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        capturedUser.Should().NotBeNull();
        capturedUser!.FirstName.Should().Be("John");
        capturedUser.LastName.Should().Be("Doe");
    }

    [Fact]
    public async Task Handle_WithMultipleRoles_ShouldPreserveAllRoles()
    {
        // Arrange
        var command = CreateValidCommand() with
        {
            Roles = new List<UserRole> { UserRole.Doctor, UserRole.Nurse, UserRole.Admin }
        };
        User? capturedUser = null;
        _mockUserRepository.Setup(x => x.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Callback<User, CancellationToken>((user, _) => capturedUser = user)
            .ReturnsAsync((User user, CancellationToken _) => user);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        capturedUser.Should().NotBeNull();
        capturedUser!.Roles.Should().HaveCount(3);
        capturedUser.Roles.Should().Contain(UserRole.Doctor);
        capturedUser.Roles.Should().Contain(UserRole.Nurse);
        capturedUser.Roles.Should().Contain(UserRole.Admin);
    }

    #endregion

    #region Cancellation Token Tests

    [Fact]
    public async Task Handle_ShouldPassCancellationTokenToRepository()
    {
        // Arrange
        var command = CreateValidCommand();
        var cancellationToken = new CancellationToken();

        // Act
        await _handler.Handle(command, cancellationToken);

        // Assert
        _mockUserRepository.Verify(
            x => x.AddAsync(It.IsAny<User>(), cancellationToken),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldPassCancellationTokenToUnitOfWork()
    {
        // Arrange
        var command = CreateValidCommand();
        var cancellationToken = new CancellationToken();

        // Act
        await _handler.Handle(command, cancellationToken);

        // Assert
        _mockUnitOfWork.Verify(
            x => x.SaveChangesAsync(cancellationToken),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldPassCancellationTokenToAuditLogger()
    {
        // Arrange
        var command = CreateValidCommand();
        var cancellationToken = new CancellationToken();

        // Act
        await _handler.Handle(command, cancellationToken);

        // Assert
        _mockAuditLogger.Verify(
            x => x.LogUserRegistrationAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<List<UserRole>>(),
                It.IsAny<string?>(),
                It.IsAny<bool>(),
                It.IsAny<string?>(),
                cancellationToken),
            Times.Once);
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
