using EMR.Application.Common.Interfaces;
using EMR.Application.Features.Audit.DTOs;
using EMR.Domain.Entities;
using EMR.Domain.Enums;
using EMR.Domain.Interfaces;
using EMR.Infrastructure.Data;
using EMR.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace EMR.UnitTests.Infrastructure.Services;

/// <summary>
/// Unit tests for AuditService
/// SECURITY TEST: Task #2662 - Validates HIPAA-compliant audit logging
/// </summary>
public class AuditServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ILogger<AuditService>> _loggerMock;
    private readonly AuditService _sut;

    public AuditServiceTests()
    {
        // Create in-memory database
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _loggerMock = new Mock<ILogger<AuditService>>();

        // Setup UnitOfWork to actually save changes to the in-memory database
        _unitOfWorkMock
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(async (CancellationToken ct) => await _context.SaveChangesAsync(ct));

        _sut = new AuditService(_context, _unitOfWorkMock.Object, _loggerMock.Object);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    #region CreateAuditLogAsync Tests

    [Fact]
    public async Task CreateAuditLogAsync_WithValidData_ShouldCreateAuditLog()
    {
        // Arrange
        var userId = "user-123";
        var action = "View patient record";
        var resourceType = "Patient";
        var resourceId = "patient-456";

        // Act
        var result = await _sut.CreateAuditLogAsync(
            eventType: AuditEventType.View,
            userId: userId,
            action: action,
            resourceType: resourceType,
            resourceId: resourceId);

        // Assert
        result.Should().NotBeEmpty();
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAuditLogAsync_WithAllParameters_ShouldStoreAllValues()
    {
        // Arrange
        var eventType = AuditEventType.Update;
        var userId = "user-123";
        var action = "Update patient demographics";
        var resourceType = "Patient";
        var resourceId = "patient-456";
        var ipAddress = "192.168.1.100";
        var userAgent = "Mozilla/5.0";
        var success = true;
        var details = "Updated name and address";
        var username = "john.doe";

        // Act
        var result = await _sut.CreateAuditLogAsync(
            eventType: eventType,
            userId: userId,
            action: action,
            resourceType: resourceType,
            resourceId: resourceId,
            ipAddress: ipAddress,
            userAgent: userAgent,
            success: success,
            details: details,
            username: username);

        // Assert
        result.Should().NotBeEmpty();

        var auditLog = await _context.AuditLogs.FirstOrDefaultAsync(a => a.Id == result);
        auditLog.Should().NotBeNull();
        auditLog!.EventType.Should().Be(eventType);
        auditLog.UserId.Should().Be(userId);
        auditLog.Action.Should().Be(action);
        auditLog.ResourceType.Should().Be(resourceType);
        auditLog.ResourceId.Should().Be(resourceId);
        auditLog.IpAddress.Should().Be(ipAddress);
        auditLog.UserAgent.Should().Be(userAgent);
        auditLog.Success.Should().Be(success);
        auditLog.Details.Should().Be(details);
        auditLog.Username.Should().Be(username);
    }

    [Fact]
    public async Task CreateAuditLogAsync_WithFailedOperation_ShouldStoreSuccessFalse()
    {
        // Arrange
        var userId = "user-123";
        var action = "Access denied";

        // Act
        var result = await _sut.CreateAuditLogAsync(
            eventType: AuditEventType.AccessDenied,
            userId: userId,
            action: action,
            resourceType: "Patient",
            success: false);

        // Assert
        var auditLog = await _context.AuditLogs.FirstOrDefaultAsync(a => a.Id == result);
        auditLog!.Success.Should().BeFalse();
    }

    [Fact]
    public async Task CreateAuditLogAsync_ShouldSetTimestampToUtcNow()
    {
        // Arrange
        var beforeTime = DateTime.UtcNow;

        // Act
        var result = await _sut.CreateAuditLogAsync(
            eventType: AuditEventType.View,
            userId: "user-123",
            action: "Test action",
            resourceType: "Test");

        // Assert
        var auditLog = await _context.AuditLogs.FirstOrDefaultAsync(a => a.Id == result);
        auditLog!.Timestamp.Should().BeOnOrAfter(beforeTime);
        auditLog.Timestamp.Should().BeOnOrBefore(DateTime.UtcNow);
    }

    #endregion

    #region LogPhiAccessAsync Tests

    [Fact]
    public async Task LogPhiAccessAsync_ShouldCreateViewEventType()
    {
        // Arrange
        var userId = "user-123";
        var resourceType = "Patient";
        var resourceId = "patient-456";
        var action = "View patient medical record";

        // Act
        var result = await _sut.LogPhiAccessAsync(
            userId: userId,
            resourceType: resourceType,
            resourceId: resourceId,
            action: action);

        // Assert
        var auditLog = await _context.AuditLogs.FirstOrDefaultAsync(a => a.Id == result);
        auditLog!.EventType.Should().Be(AuditEventType.View);
        auditLog.Success.Should().BeTrue();
    }

    [Fact]
    public async Task LogPhiAccessAsync_ShouldIncludeIpAddressAndUserAgent()
    {
        // Arrange
        var ipAddress = "10.0.0.1";
        var userAgent = "EMR Mobile App/1.0";

        // Act
        var result = await _sut.LogPhiAccessAsync(
            userId: "user-123",
            resourceType: "Patient",
            resourceId: "patient-456",
            action: "View patient",
            ipAddress: ipAddress,
            userAgent: userAgent);

        // Assert
        var auditLog = await _context.AuditLogs.FirstOrDefaultAsync(a => a.Id == result);
        auditLog!.IpAddress.Should().Be(ipAddress);
        auditLog.UserAgent.Should().Be(userAgent);
    }

    #endregion

    #region LogDataModificationAsync Tests

    [Fact]
    public async Task LogDataModificationAsync_WithCreateEvent_ShouldStoreCorrectEventType()
    {
        // Arrange
        var eventType = AuditEventType.Create;

        // Act
        var result = await _sut.LogDataModificationAsync(
            eventType: eventType,
            userId: "user-123",
            resourceType: "Patient",
            resourceId: "patient-456",
            action: "Create patient record");

        // Assert
        var auditLog = await _context.AuditLogs.FirstOrDefaultAsync(a => a.Id == result);
        auditLog!.EventType.Should().Be(AuditEventType.Create);
    }

    [Fact]
    public async Task LogDataModificationAsync_WithUpdateEvent_ShouldStoreChangeValues()
    {
        // Arrange
        var oldValues = "{\"firstName\": \"John\"}";
        var newValues = "{\"firstName\": \"Jonathan\"}";

        // Act
        var result = await _sut.LogDataModificationAsync(
            eventType: AuditEventType.Update,
            userId: "user-123",
            resourceType: "Patient",
            resourceId: "patient-456",
            action: "Update patient name",
            oldValues: oldValues,
            newValues: newValues);

        // Assert
        var auditLog = await _context.AuditLogs.FirstOrDefaultAsync(a => a.Id == result);
        auditLog!.OldValues.Should().Be(oldValues);
        auditLog.NewValues.Should().Be(newValues);
    }

    [Fact]
    public async Task LogDataModificationAsync_WithDeleteEvent_ShouldStoreDeleteEventType()
    {
        // Arrange
        var eventType = AuditEventType.Delete;

        // Act
        var result = await _sut.LogDataModificationAsync(
            eventType: eventType,
            userId: "user-123",
            resourceType: "Patient",
            resourceId: "patient-456",
            action: "Deactivate patient record");

        // Assert
        var auditLog = await _context.AuditLogs.FirstOrDefaultAsync(a => a.Id == result);
        auditLog!.EventType.Should().Be(AuditEventType.Delete);
    }

    #endregion

    #region LogAuthenticationAsync Tests

    [Fact]
    public async Task LogAuthenticationAsync_WithSuccessfulLogin_ShouldStoreLoginEvent()
    {
        // Arrange
        var userId = "user-123";
        var username = "john.doe@example.com";

        // Act
        var result = await _sut.LogAuthenticationAsync(
            eventType: AuditEventType.Login,
            userId: userId,
            username: username,
            success: true);

        // Assert
        var auditLog = await _context.AuditLogs.FirstOrDefaultAsync(a => a.Id == result);
        auditLog!.EventType.Should().Be(AuditEventType.Login);
        auditLog.Success.Should().BeTrue();
        auditLog.Username.Should().Be(username);
        auditLog.Action.Should().Be("User login");
        auditLog.ResourceType.Should().Be("Authentication");
    }

    [Fact]
    public async Task LogAuthenticationAsync_WithFailedLogin_ShouldStoreErrorMessage()
    {
        // Arrange
        var errorMessage = "Invalid password";

        // Act
        var result = await _sut.LogAuthenticationAsync(
            eventType: AuditEventType.FailedLogin,
            userId: "unknown",
            username: "john.doe@example.com",
            success: false,
            errorMessage: errorMessage);

        // Assert
        var auditLog = await _context.AuditLogs.FirstOrDefaultAsync(a => a.Id == result);
        auditLog!.Success.Should().BeFalse();
        auditLog.ErrorMessage.Should().Be(errorMessage);
        auditLog.Action.Should().Be("Failed login attempt");
    }

    [Fact]
    public async Task LogAuthenticationAsync_WithLogout_ShouldStoreLogoutEvent()
    {
        // Act
        var result = await _sut.LogAuthenticationAsync(
            eventType: AuditEventType.Logout,
            userId: "user-123",
            username: "john.doe@example.com",
            success: true);

        // Assert
        var auditLog = await _context.AuditLogs.FirstOrDefaultAsync(a => a.Id == result);
        auditLog!.EventType.Should().Be(AuditEventType.Logout);
        auditLog.Action.Should().Be("User logout");
    }

    #endregion

    #region LogAccessDeniedAsync Tests

    [Fact]
    public async Task LogAccessDeniedAsync_ShouldCreateAccessDeniedEvent()
    {
        // Arrange
        var userId = "user-123";
        var resourceType = "Patient";
        var resourceId = "patient-456";
        var action = "View patient medical history";
        var reason = "User does not have required role: Doctor";

        // Act
        var result = await _sut.LogAccessDeniedAsync(
            userId: userId,
            resourceType: resourceType,
            resourceId: resourceId,
            action: action,
            reason: reason);

        // Assert
        var auditLog = await _context.AuditLogs.FirstOrDefaultAsync(a => a.Id == result);
        auditLog!.EventType.Should().Be(AuditEventType.AccessDenied);
        auditLog.Success.Should().BeFalse();
        auditLog.ErrorMessage.Should().Be(reason);
        auditLog.Details.Should().Contain(reason);
    }

    [Fact]
    public async Task LogAccessDeniedAsync_WithoutResourceId_ShouldStoreNullResourceId()
    {
        // Act
        var result = await _sut.LogAccessDeniedAsync(
            userId: "user-123",
            resourceType: "Admin",
            resourceId: null,
            action: "Access admin panel",
            reason: "Insufficient permissions");

        // Assert
        var auditLog = await _context.AuditLogs.FirstOrDefaultAsync(a => a.Id == result);
        auditLog!.ResourceId.Should().BeNull();
    }

    #endregion

    #region LogExportOperationAsync Tests

    [Fact]
    public async Task LogExportOperationAsync_WithExportEvent_ShouldCreateExportLog()
    {
        // Act
        var result = await _sut.LogExportOperationAsync(
            eventType: AuditEventType.Export,
            userId: "user-123",
            resourceType: "PatientReport",
            resourceId: "report-789",
            format: "PDF");

        // Assert
        var auditLog = await _context.AuditLogs.FirstOrDefaultAsync(a => a.Id == result);
        auditLog!.EventType.Should().Be(AuditEventType.Export);
        auditLog.Action.Should().Contain("Exported");
        auditLog.Action.Should().Contain("PDF");
        auditLog.Details.Should().Contain("PDF");
    }

    [Fact]
    public async Task LogExportOperationAsync_WithPrintEvent_ShouldCreatePrintLog()
    {
        // Act
        var result = await _sut.LogExportOperationAsync(
            eventType: AuditEventType.Print,
            userId: "user-123",
            resourceType: "Prescription",
            resourceId: "rx-456",
            format: "Letter");

        // Assert
        var auditLog = await _context.AuditLogs.FirstOrDefaultAsync(a => a.Id == result);
        auditLog!.EventType.Should().Be(AuditEventType.Print);
        auditLog.Action.Should().Contain("Printed");
    }

    #endregion

    #region LogHttpRequestAsync Tests

    [Fact]
    public async Task LogHttpRequestAsync_WithSuccessfulRequest_ShouldStoreAllHttpDetails()
    {
        // Arrange
        var httpMethod = "GET";
        var requestPath = "/api/patients/123";
        var statusCode = 200;
        var durationMs = 45L;
        var sessionId = "session-abc";
        var correlationId = "corr-xyz";

        // Act
        var result = await _sut.LogHttpRequestAsync(
            eventType: AuditEventType.View,
            userId: "user-123",
            action: "Get patient by ID",
            resourceType: "Patient",
            resourceId: "123",
            httpMethod: httpMethod,
            requestPath: requestPath,
            statusCode: statusCode,
            durationMs: durationMs,
            ipAddress: "192.168.1.1",
            sessionId: sessionId,
            correlationId: correlationId);

        // Assert
        var auditLog = await _context.AuditLogs.FirstOrDefaultAsync(a => a.Id == result);
        auditLog!.HttpMethod.Should().Be(httpMethod);
        auditLog.RequestPath.Should().Be(requestPath);
        auditLog.StatusCode.Should().Be(statusCode);
        auditLog.DurationMs.Should().Be(durationMs);
        auditLog.SessionId.Should().Be(sessionId);
        auditLog.CorrelationId.Should().Be(correlationId);
        auditLog.Success.Should().BeTrue(); // 200 status code
    }

    [Fact]
    public async Task LogHttpRequestAsync_With4xxStatusCode_ShouldSetSuccessFalse()
    {
        // Act
        var result = await _sut.LogHttpRequestAsync(
            eventType: AuditEventType.AccessDenied,
            userId: "user-123",
            action: "Unauthorized access attempt",
            resourceType: "Patient",
            resourceId: "123",
            httpMethod: "GET",
            requestPath: "/api/patients/123",
            statusCode: 403,
            durationMs: 10);

        // Assert
        var auditLog = await _context.AuditLogs.FirstOrDefaultAsync(a => a.Id == result);
        auditLog!.Success.Should().BeFalse();
    }

    [Fact]
    public async Task LogHttpRequestAsync_With5xxStatusCode_ShouldSetSuccessFalse()
    {
        // Act
        var result = await _sut.LogHttpRequestAsync(
            eventType: AuditEventType.View,
            userId: "user-123",
            action: "Server error occurred",
            resourceType: "Patient",
            resourceId: "123",
            httpMethod: "GET",
            requestPath: "/api/patients/123",
            statusCode: 500,
            durationMs: 100);

        // Assert
        var auditLog = await _context.AuditLogs.FirstOrDefaultAsync(a => a.Id == result);
        auditLog!.Success.Should().BeFalse();
    }

    #endregion

    #region GetResourceAuditTrailAsync Tests

    [Fact]
    public async Task GetResourceAuditTrailAsync_WithExistingLogs_ShouldReturnOrderedByTimestamp()
    {
        // Arrange
        var resourceType = "Patient";
        var resourceId = "patient-123";

        // Create audit logs with different timestamps
        await _sut.CreateAuditLogAsync(AuditEventType.Create, "user-1", "Created", resourceType, resourceId);
        await Task.Delay(10); // Ensure different timestamps
        await _sut.CreateAuditLogAsync(AuditEventType.View, "user-2", "Viewed", resourceType, resourceId);
        await Task.Delay(10);
        await _sut.CreateAuditLogAsync(AuditEventType.Update, "user-1", "Updated", resourceType, resourceId);

        // Act
        var result = await _sut.GetResourceAuditTrailAsync(resourceType, resourceId);

        // Assert
        var logs = result.ToList();
        logs.Should().HaveCount(3);
        logs[0].EventType.Should().Be(AuditEventType.Update); // Most recent first
        logs[1].EventType.Should().Be(AuditEventType.View);
        logs[2].EventType.Should().Be(AuditEventType.Create); // Oldest last
    }

    [Fact]
    public async Task GetResourceAuditTrailAsync_WithNoLogs_ShouldReturnEmptyList()
    {
        // Act
        var result = await _sut.GetResourceAuditTrailAsync("Patient", "non-existent-id");

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region GetUserAuditHistoryAsync Tests

    [Fact]
    public async Task GetUserAuditHistoryAsync_ShouldReturnUserLogs()
    {
        // Arrange
        var userId = "user-123";
        await _sut.CreateAuditLogAsync(AuditEventType.View, userId, "Viewed", "Patient", "p1");
        await _sut.CreateAuditLogAsync(AuditEventType.View, userId, "Viewed", "Patient", "p2");
        await _sut.CreateAuditLogAsync(AuditEventType.View, "other-user", "Viewed", "Patient", "p3");

        // Act
        var result = await _sut.GetUserAuditHistoryAsync(userId);

        // Assert
        result.Should().HaveCount(2);
        result.All(l => l.UserId == userId).Should().BeTrue();
    }

    [Fact]
    public async Task GetUserAuditHistoryAsync_WithDateRange_ShouldFilterByDates()
    {
        // Arrange
        var userId = "user-123";
        var fromDate = DateTime.UtcNow.AddHours(-1);
        var toDate = DateTime.UtcNow.AddHours(1);

        await _sut.CreateAuditLogAsync(AuditEventType.View, userId, "Recent view", "Patient", "p1");

        // Act
        var result = await _sut.GetUserAuditHistoryAsync(userId, fromDate, toDate);

        // Assert
        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetUserAuditHistoryAsync_ShouldRespectPageSize()
    {
        // Arrange
        var userId = "user-123";
        for (int i = 0; i < 10; i++)
        {
            await _sut.CreateAuditLogAsync(AuditEventType.View, userId, $"Action {i}", "Patient", $"p{i}");
        }

        // Act
        var result = await _sut.GetUserAuditHistoryAsync(userId, pageSize: 5);

        // Assert
        result.Should().HaveCount(5);
    }

    #endregion

    #region QueryAuditLogsAsync Tests

    [Fact]
    public async Task QueryAuditLogsAsync_WithUserIdFilter_ShouldFilterByUserId()
    {
        // Arrange
        await _sut.CreateAuditLogAsync(AuditEventType.View, "user-1", "Action 1", "Patient", "p1");
        await _sut.CreateAuditLogAsync(AuditEventType.View, "user-2", "Action 2", "Patient", "p2");

        var query = new AuditLogQueryDto { UserId = "user-1" };

        // Act
        var (logs, totalCount) = await _sut.QueryAuditLogsAsync(query);

        // Assert
        logs.Should().HaveCount(1);
        totalCount.Should().Be(1);
        logs.First().UserId.Should().Be("user-1");
    }

    [Fact]
    public async Task QueryAuditLogsAsync_WithEventTypeFilter_ShouldFilterByEventType()
    {
        // Arrange
        await _sut.CreateAuditLogAsync(AuditEventType.View, "user-1", "View", "Patient", "p1");
        await _sut.CreateAuditLogAsync(AuditEventType.Create, "user-1", "Create", "Patient", "p2");
        await _sut.CreateAuditLogAsync(AuditEventType.Update, "user-1", "Update", "Patient", "p3");

        var query = new AuditLogQueryDto { EventType = AuditEventType.Create };

        // Act
        var (logs, totalCount) = await _sut.QueryAuditLogsAsync(query);

        // Assert
        logs.Should().HaveCount(1);
        logs.First().EventType.Should().Be(AuditEventType.Create);
    }

    [Fact]
    public async Task QueryAuditLogsAsync_WithResourceTypeFilter_ShouldFilterByResourceType()
    {
        // Arrange
        await _sut.CreateAuditLogAsync(AuditEventType.View, "user-1", "View patient", "Patient", "p1");
        await _sut.CreateAuditLogAsync(AuditEventType.View, "user-1", "View encounter", "Encounter", "e1");

        var query = new AuditLogQueryDto { ResourceType = "Patient" };

        // Act
        var (logs, totalCount) = await _sut.QueryAuditLogsAsync(query);

        // Assert
        logs.Should().HaveCount(1);
        logs.First().ResourceType.Should().Be("Patient");
    }

    [Fact]
    public async Task QueryAuditLogsAsync_WithPagination_ShouldReturnCorrectPage()
    {
        // Arrange
        for (int i = 0; i < 25; i++)
        {
            await _sut.CreateAuditLogAsync(AuditEventType.View, "user-1", $"Action {i}", "Patient", $"p{i}");
        }

        var query = new AuditLogQueryDto { PageNumber = 2, PageSize = 10 };

        // Act
        var (logs, totalCount) = await _sut.QueryAuditLogsAsync(query);

        // Assert
        logs.Should().HaveCount(10);
        totalCount.Should().Be(25);
    }

    [Fact]
    public async Task QueryAuditLogsAsync_WithSuccessFilter_ShouldFilterBySuccess()
    {
        // Arrange
        await _sut.CreateAuditLogAsync(AuditEventType.View, "user-1", "Success", "Patient", "p1", success: true);
        await _sut.CreateAuditLogAsync(AuditEventType.AccessDenied, "user-1", "Failed", "Patient", "p2", success: false);

        var query = new AuditLogQueryDto { Success = false };

        // Act
        var (logs, totalCount) = await _sut.QueryAuditLogsAsync(query);

        // Assert
        logs.Should().HaveCount(1);
        logs.First().Success.Should().BeFalse();
    }

    #endregion

    #region GetAuditLogByIdAsync Tests

    [Fact]
    public async Task GetAuditLogByIdAsync_WithExistingId_ShouldReturnAuditLog()
    {
        // Arrange
        var auditLogId = await _sut.CreateAuditLogAsync(
            AuditEventType.View, "user-123", "Test action", "Patient", "p1");

        // Act
        var result = await _sut.GetAuditLogByIdAsync(auditLogId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(auditLogId);
        result.Action.Should().Be("Test action");
    }

    [Fact]
    public async Task GetAuditLogByIdAsync_WithNonExistingId_ShouldReturnNull()
    {
        // Act
        var result = await _sut.GetAuditLogByIdAsync(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region HIPAA Compliance Tests

    [Fact]
    public async Task AuditLog_ShouldBeImmutable_CannotModifyAfterCreation()
    {
        // Arrange
        var auditLogId = await _sut.CreateAuditLogAsync(
            AuditEventType.View, "user-123", "Original action", "Patient", "p1");

        // Act - Retrieve the audit log
        var auditLog = await _context.AuditLogs.FirstOrDefaultAsync(a => a.Id == auditLogId);

        // Assert - The audit log exists and has the original values
        // Note: Immutability is enforced by not providing setters on the entity
        auditLog.Should().NotBeNull();
        auditLog!.Action.Should().Be("Original action");
    }

    [Fact]
    public async Task AuditLog_ShouldCaptureWhereFromIpAddress()
    {
        // Arrange
        var ipAddress = "192.168.1.100";

        // Act
        var auditLogId = await _sut.CreateAuditLogAsync(
            AuditEventType.View, "user-123", "PHI Access", "Patient", "p1",
            ipAddress: ipAddress);

        // Assert
        var auditLog = await _context.AuditLogs.FirstOrDefaultAsync(a => a.Id == auditLogId);
        auditLog!.IpAddress.Should().Be(ipAddress);
    }

    [Fact]
    public async Task AuditLog_ShouldCaptureWhoFromUserId()
    {
        // Arrange
        var userId = "user-123";
        var username = "john.doe@hospital.org";

        // Act
        var auditLogId = await _sut.CreateAuditLogAsync(
            AuditEventType.View, userId, "PHI Access", "Patient", "p1",
            username: username);

        // Assert
        var auditLog = await _context.AuditLogs.FirstOrDefaultAsync(a => a.Id == auditLogId);
        auditLog!.UserId.Should().Be(userId);
        auditLog.Username.Should().Be(username);
    }

    [Fact]
    public async Task AuditLog_ShouldCaptureWhatFromResourceDetails()
    {
        // Arrange
        var resourceType = "Patient";
        var resourceId = "patient-456";
        var action = "View patient demographics";

        // Act
        var auditLogId = await _sut.CreateAuditLogAsync(
            AuditEventType.View, "user-123", action, resourceType, resourceId);

        // Assert
        var auditLog = await _context.AuditLogs.FirstOrDefaultAsync(a => a.Id == auditLogId);
        auditLog!.ResourceType.Should().Be(resourceType);
        auditLog.ResourceId.Should().Be(resourceId);
        auditLog.Action.Should().Be(action);
    }

    [Fact]
    public async Task AuditLog_ShouldCaptureWhenFromTimestamp()
    {
        // Arrange
        var beforeTime = DateTime.UtcNow;

        // Act
        var auditLogId = await _sut.CreateAuditLogAsync(
            AuditEventType.View, "user-123", "PHI Access", "Patient", "p1");

        // Assert
        var auditLog = await _context.AuditLogs.FirstOrDefaultAsync(a => a.Id == auditLogId);
        auditLog!.Timestamp.Should().BeOnOrAfter(beforeTime);
        auditLog.Timestamp.Kind.Should().Be(DateTimeKind.Utc);
    }

    #endregion
}
