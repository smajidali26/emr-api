using EMR.Application.Common.Interfaces;
using EMR.Domain.Entities;
using EMR.Domain.Enums;
using EMR.Infrastructure.Data;
using EMR.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace EMR.UnitTests.Infrastructure.Services;

/// <summary>
/// Unit tests for AuditStatisticsService
/// HIPAA COMPLIANCE: Task #834 - Tests for compliance metrics and statistics
/// Tests the fallback path to direct database queries (in-memory doesn't support TimescaleDB)
/// </summary>
public class AuditStatisticsServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<ILogger<AuditStatisticsService>> _loggerMock;
    private readonly AuditStatisticsService _sut;

    public AuditStatisticsServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _loggerMock = new Mock<ILogger<AuditStatisticsService>>();

        _sut = new AuditStatisticsService(_context, _loggerMock.Object);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    #region GetDailySummaryAsync Tests

    [Fact]
    public async Task GetDailySummaryAsync_WithNoEvents_ShouldReturnZeroValues()
    {
        // Arrange
        var date = DateTime.UtcNow.Date;

        // Act
        var result = await _sut.GetDailySummaryAsync(date);

        // Assert
        result.Date.Should().Be(date);
        result.TotalEvents.Should().Be(0);
        result.SuccessfulEvents.Should().Be(0);
        result.FailedEvents.Should().Be(0);
        result.UniqueUsers.Should().Be(0);
    }

    [Fact]
    public async Task GetDailySummaryAsync_WithEvents_ShouldReturnCorrectCounts()
    {
        // Arrange
        var date = DateTime.UtcNow.Date;
        await SeedAuditLogsAsync(date, successCount: 5, failCount: 2, userIds: ["user-1", "user-2"]);

        // Act
        var result = await _sut.GetDailySummaryAsync(date);

        // Assert
        result.Date.Should().Be(date);
        result.TotalEvents.Should().Be(7);
        result.SuccessfulEvents.Should().Be(5);
        result.FailedEvents.Should().Be(2);
        result.UniqueUsers.Should().Be(2);
    }

    [Fact]
    public async Task GetDailySummaryAsync_ShouldOnlyIncludeEventsFromSpecifiedDay()
    {
        // Arrange
        var today = DateTime.UtcNow.Date;
        var yesterday = today.AddDays(-1);

        await SeedAuditLogsAsync(today, successCount: 3, failCount: 0, userIds: ["user-1"]);
        await SeedAuditLogsAsync(yesterday, successCount: 5, failCount: 0, userIds: ["user-2"]);

        // Act
        var result = await _sut.GetDailySummaryAsync(today);

        // Assert
        result.TotalEvents.Should().Be(3);
    }

    #endregion

    #region GetDailySummariesAsync Tests

    [Fact]
    public async Task GetDailySummariesAsync_WithMultipleDays_ShouldReturnSummariesForEachDay()
    {
        // Arrange
        var today = DateTime.UtcNow.Date;
        var yesterday = today.AddDays(-1);
        var twoDaysAgo = today.AddDays(-2);

        await SeedAuditLogsAsync(today, successCount: 3, failCount: 0, userIds: ["user-1"]);
        await SeedAuditLogsAsync(yesterday, successCount: 5, failCount: 1, userIds: ["user-2"]);
        await SeedAuditLogsAsync(twoDaysAgo, successCount: 2, failCount: 0, userIds: ["user-3"]);

        // Act
        var result = await _sut.GetDailySummariesAsync(twoDaysAgo, today);

        // Assert
        result.Should().HaveCount(3);
        result.Should().BeInAscendingOrder(s => s.Date);
    }

    [Fact]
    public async Task GetDailySummariesAsync_WithEmptyRange_ShouldReturnEmptyList()
    {
        // Arrange
        var startDate = DateTime.UtcNow.Date.AddDays(-10);
        var endDate = DateTime.UtcNow.Date.AddDays(-5);

        // Act
        var result = await _sut.GetDailySummariesAsync(startDate, endDate);

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region GetUserActivityAsync Tests

    [Fact]
    public async Task GetUserActivityAsync_WithUserEvents_ShouldReturnCorrectSummary()
    {
        // Arrange
        var userId = "user-123";
        var today = DateTime.UtcNow.Date;
        var startDate = today.AddDays(-7);

        await CreateAuditLogAsync(userId, "john.doe", AuditEventType.View, today);
        await CreateAuditLogAsync(userId, "john.doe", AuditEventType.View, today);
        await CreateAuditLogAsync(userId, "john.doe", AuditEventType.Create, today);
        await CreateAuditLogAsync(userId, "john.doe", AuditEventType.Update, today);
        await CreateAuditLogAsync(userId, "john.doe", AuditEventType.Delete, today);
        await CreateAuditLogAsync(userId, "john.doe", AuditEventType.AccessDenied, today, success: false);

        // Act
        var result = await _sut.GetUserActivityAsync(userId, startDate, today);

        // Assert
        result.UserId.Should().Be(userId);
        result.Username.Should().Be("john.doe");
        result.TotalActions.Should().Be(6);
        result.ViewCount.Should().Be(2);
        result.CreateCount.Should().Be(1);
        result.UpdateCount.Should().Be(1);
        result.DeleteCount.Should().Be(1);
        result.FailedActions.Should().Be(1);
    }

    [Fact]
    public async Task GetUserActivityAsync_WithNoEvents_ShouldReturnEmptySummary()
    {
        // Arrange
        var userId = "non-existent-user";
        var today = DateTime.UtcNow.Date;

        // Act
        var result = await _sut.GetUserActivityAsync(userId, today.AddDays(-7), today);

        // Assert
        result.UserId.Should().Be(userId);
        result.TotalActions.Should().Be(0);
        result.FirstActivity.Should().BeNull();
        result.LastActivity.Should().BeNull();
    }

    [Fact]
    public async Task GetUserActivityAsync_ShouldTrackResourceTypesAccessed()
    {
        // Arrange
        var userId = "user-123";
        var today = DateTime.UtcNow.Date;

        await CreateAuditLogAsync(userId, "john", AuditEventType.View, today, resourceType: "Patient");
        await CreateAuditLogAsync(userId, "john", AuditEventType.View, today, resourceType: "Encounter");
        await CreateAuditLogAsync(userId, "john", AuditEventType.View, today, resourceType: "Prescription");
        await CreateAuditLogAsync(userId, "john", AuditEventType.View, today, resourceType: "Patient"); // Duplicate

        // Act
        var result = await _sut.GetUserActivityAsync(userId, today.AddDays(-1), today);

        // Assert
        result.ResourceTypesAccessed.Should().Be(3);
    }

    #endregion

    #region GetTopActiveUsersAsync Tests

    [Fact]
    public async Task GetTopActiveUsersAsync_ShouldReturnUsersOrderedByActivityCount()
    {
        // Arrange
        var today = DateTime.UtcNow.Date;

        // User 1: 5 actions
        for (int i = 0; i < 5; i++)
            await CreateAuditLogAsync("user-1", "user1", AuditEventType.View, today);

        // User 2: 10 actions
        for (int i = 0; i < 10; i++)
            await CreateAuditLogAsync("user-2", "user2", AuditEventType.View, today);

        // User 3: 3 actions
        for (int i = 0; i < 3; i++)
            await CreateAuditLogAsync("user-3", "user3", AuditEventType.View, today);

        // Act
        var result = await _sut.GetTopActiveUsersAsync(today.AddDays(-1), today, limit: 3);

        // Assert
        result.Should().HaveCount(3);
        result[0].UserId.Should().Be("user-2");
        result[0].TotalActions.Should().Be(10);
        result[1].UserId.Should().Be("user-1");
        result[1].TotalActions.Should().Be(5);
        result[2].UserId.Should().Be("user-3");
        result[2].TotalActions.Should().Be(3);
    }

    [Fact]
    public async Task GetTopActiveUsersAsync_ShouldRespectLimitParameter()
    {
        // Arrange
        var today = DateTime.UtcNow.Date;

        for (int i = 0; i < 20; i++)
            await CreateAuditLogAsync($"user-{i}", $"user{i}", AuditEventType.View, today);

        // Act
        var result = await _sut.GetTopActiveUsersAsync(today.AddDays(-1), today, limit: 5);

        // Assert
        result.Should().HaveCount(5);
    }

    #endregion

    #region GetResourceAccessAsync Tests

    [Fact]
    public async Task GetResourceAccessAsync_WithAccessHistory_ShouldReturnCorrectSummary()
    {
        // Arrange
        var resourceType = "Patient";
        var resourceId = "patient-123";

        await CreateAuditLogAsync("user-1", "u1", AuditEventType.View, DateTime.UtcNow, resourceType, resourceId);
        await CreateAuditLogAsync("user-1", "u1", AuditEventType.View, DateTime.UtcNow, resourceType, resourceId);
        await CreateAuditLogAsync("user-2", "u2", AuditEventType.Update, DateTime.UtcNow, resourceType, resourceId);
        await CreateAuditLogAsync("user-3", "u3", AuditEventType.View, DateTime.UtcNow, resourceType, resourceId);

        // Act
        var result = await _sut.GetResourceAccessAsync(resourceType, resourceId);

        // Assert
        result.ResourceType.Should().Be(resourceType);
        result.ResourceId.Should().Be(resourceId);
        result.TotalAccesses.Should().Be(4);
        result.UniqueUsers.Should().Be(3);
        result.ViewCount.Should().Be(3);
        result.ModificationCount.Should().Be(1);
        result.LastAccessed.Should().NotBeNull();
    }

    [Fact]
    public async Task GetResourceAccessAsync_WithNoHistory_ShouldReturnEmptySummary()
    {
        // Act
        var result = await _sut.GetResourceAccessAsync("Patient", "non-existent-id");

        // Assert
        result.ResourceType.Should().Be("Patient");
        result.ResourceId.Should().Be("non-existent-id");
        result.TotalAccesses.Should().Be(0);
        result.UniqueUsers.Should().Be(0);
        result.LastAccessed.Should().BeNull();
    }

    #endregion

    #region GetComplianceMetricsAsync Tests

    [Fact]
    public async Task GetComplianceMetricsAsync_ShouldCountPhiAccess()
    {
        // Arrange
        var today = DateTime.UtcNow.Date;

        await CreateAuditLogAsync("user-1", "u1", AuditEventType.View, today, "Patient", "p1");
        await CreateAuditLogAsync("user-1", "u1", AuditEventType.View, today, "Patient", "p2");
        await CreateAuditLogAsync("user-1", "u1", AuditEventType.View, today, "Settings", "s1");

        // Act
        var result = await _sut.GetComplianceMetricsAsync(today.AddDays(-1), today);

        // Assert
        result.TotalAuditEvents.Should().Be(3);
        result.PhiAccessCount.Should().Be(2);
    }

    [Fact]
    public async Task GetComplianceMetricsAsync_ShouldCountAccessDenied()
    {
        // Arrange
        var today = DateTime.UtcNow.Date;

        await CreateAuditLogAsync("user-1", "u1", AuditEventType.AccessDenied, today, success: false);
        await CreateAuditLogAsync("user-1", "u1", AuditEventType.AccessDenied, today, success: false);
        await CreateAuditLogAsync("user-1", "u1", AuditEventType.View, today);

        // Act
        var result = await _sut.GetComplianceMetricsAsync(today.AddDays(-1), today);

        // Assert
        result.AccessDeniedCount.Should().Be(2);
    }

    [Fact]
    public async Task GetComplianceMetricsAsync_ShouldCountAuthEvents()
    {
        // Arrange
        var today = DateTime.UtcNow.Date;

        await CreateAuditLogAsync("user-1", "u1", AuditEventType.Login, today);
        await CreateAuditLogAsync("user-2", "u2", AuditEventType.Login, today);
        await CreateAuditLogAsync("user-3", "u3", AuditEventType.FailedLogin, today, success: false);
        await CreateAuditLogAsync("user-1", "u1", AuditEventType.Logout, today);

        // Act
        var result = await _sut.GetComplianceMetricsAsync(today.AddDays(-1), today);

        // Assert
        result.AuthEventCount.Should().Be(3); // Login + FailedLogin (Logout doesn't count)
        result.FailedLoginCount.Should().Be(1);
    }

    [Fact]
    public async Task GetComplianceMetricsAsync_ShouldCountExportPrintEvents()
    {
        // Arrange
        var today = DateTime.UtcNow.Date;

        await CreateAuditLogAsync("user-1", "u1", AuditEventType.Export, today);
        await CreateAuditLogAsync("user-1", "u1", AuditEventType.Print, today);
        await CreateAuditLogAsync("user-1", "u1", AuditEventType.Print, today);

        // Act
        var result = await _sut.GetComplianceMetricsAsync(today.AddDays(-1), today);

        // Assert
        result.ExportPrintCount.Should().Be(3);
    }

    [Fact]
    public async Task GetComplianceMetricsAsync_ShouldCountUniqueUsersAndSessions()
    {
        // Arrange
        var today = DateTime.UtcNow.Date;

        await CreateAuditLogAsync("user-1", "u1", AuditEventType.View, today, ipAddress: "192.168.1.1", sessionId: "session-1");
        await CreateAuditLogAsync("user-1", "u1", AuditEventType.View, today, ipAddress: "192.168.1.1", sessionId: "session-1");
        await CreateAuditLogAsync("user-2", "u2", AuditEventType.View, today, ipAddress: "192.168.1.2", sessionId: "session-2");
        await CreateAuditLogAsync("user-3", "u3", AuditEventType.View, today, ipAddress: "192.168.1.3", sessionId: "session-3");

        // Act
        var result = await _sut.GetComplianceMetricsAsync(today.AddDays(-1), today);

        // Assert
        result.ActiveUsers.Should().Be(3);
        result.UniqueIpAddresses.Should().Be(3);
        result.UniqueSessions.Should().Be(3);
    }

    [Fact]
    public async Task GetComplianceMetricsAsync_WithNoEvents_ShouldReturnZeroValues()
    {
        // Arrange
        var today = DateTime.UtcNow.Date;

        // Act
        var result = await _sut.GetComplianceMetricsAsync(today.AddDays(-1), today);

        // Assert
        result.TotalAuditEvents.Should().Be(0);
        result.PhiAccessCount.Should().Be(0);
        result.AccessDeniedCount.Should().Be(0);
        result.AuthEventCount.Should().Be(0);
        result.FailedLoginCount.Should().Be(0);
        result.ExportPrintCount.Should().Be(0);
        result.ActiveUsers.Should().Be(0);
    }

    #endregion

    #region GetHourlyActivityTrendAsync Tests

    [Fact]
    public async Task GetHourlyActivityTrendAsync_ShouldReturnAllHours()
    {
        // Arrange
        var today = DateTime.UtcNow.Date;

        // Act
        var result = await _sut.GetHourlyActivityTrendAsync(today);

        // Assert
        result.Should().HaveCount(24);
        result.Select(h => h.Hour).Should().BeEquivalentTo(Enumerable.Range(0, 24));
    }

    [Fact]
    public async Task GetHourlyActivityTrendAsync_ShouldCountEventsPerHour()
    {
        // Arrange
        var today = DateTime.UtcNow.Date;
        var hour9am = today.AddHours(9);
        var hour2pm = today.AddHours(14);

        // 3 events at 9 AM
        await CreateAuditLogAsync("user-1", "u1", AuditEventType.View, hour9am);
        await CreateAuditLogAsync("user-1", "u1", AuditEventType.View, hour9am.AddMinutes(15));
        await CreateAuditLogAsync("user-2", "u2", AuditEventType.View, hour9am.AddMinutes(30));

        // 2 events at 2 PM
        await CreateAuditLogAsync("user-1", "u1", AuditEventType.View, hour2pm);
        await CreateAuditLogAsync("user-3", "u3", AuditEventType.View, hour2pm.AddMinutes(20));

        // Act
        var result = await _sut.GetHourlyActivityTrendAsync(today);

        // Assert
        var hour9 = result.First(h => h.Hour == 9);
        hour9.EventCount.Should().Be(3);
        hour9.UniqueUsers.Should().Be(2);

        var hour14 = result.First(h => h.Hour == 14);
        hour14.EventCount.Should().Be(2);
        hour14.UniqueUsers.Should().Be(2);

        // Other hours should be zero
        var hour0 = result.First(h => h.Hour == 0);
        hour0.EventCount.Should().Be(0);
        hour0.UniqueUsers.Should().Be(0);
    }

    #endregion

    #region Helper Methods

    private async Task SeedAuditLogsAsync(DateTime date, int successCount, int failCount, string[] userIds)
    {
        var random = new Random();
        var allUsers = userIds.ToList();

        for (int i = 0; i < successCount; i++)
        {
            var userId = allUsers[random.Next(allUsers.Count)];
            await CreateAuditLogAsync(userId, userId, AuditEventType.View, date.AddHours(random.Next(24)));
        }

        for (int i = 0; i < failCount; i++)
        {
            var userId = allUsers[random.Next(allUsers.Count)];
            await CreateAuditLogAsync(userId, userId, AuditEventType.AccessDenied, date.AddHours(random.Next(24)), success: false);
        }
    }

    private async Task CreateAuditLogAsync(
        string userId,
        string username,
        AuditEventType eventType,
        DateTime timestamp,
        string resourceType = "Patient",
        string? resourceId = null,
        bool success = true,
        string? ipAddress = null,
        string? sessionId = null)
    {
        var auditLog = new AuditLog(
            eventType: eventType,
            userId: userId,
            action: $"{eventType} action",
            resourceType: resourceType,
            resourceId: resourceId ?? Guid.NewGuid().ToString(),
            ipAddress: ipAddress ?? "127.0.0.1",
            success: success,
            username: username);

        auditLog.SetTrackingIds(sessionId ?? Guid.NewGuid().ToString(), Guid.NewGuid().ToString());
        auditLog.SetHttpDetails("GET", "/api/test", 200, 50);

        // Use reflection to set the timestamp for testing
        var timestampProperty = typeof(AuditLog).GetProperty("Timestamp");
        timestampProperty?.SetValue(auditLog, timestamp);

        _context.AuditLogs.Add(auditLog);
        await _context.SaveChangesAsync();
    }

    #endregion
}
