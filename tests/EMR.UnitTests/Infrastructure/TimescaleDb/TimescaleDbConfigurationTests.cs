using EMR.Domain.Entities;
using EMR.Domain.Enums;
using EMR.Infrastructure.Data;
using EMR.Infrastructure.TimescaleDb;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace EMR.UnitTests.Infrastructure.TimescaleDb;

/// <summary>
/// Unit tests for TimescaleDbConfiguration
/// HIPAA COMPLIANCE: Task #834 - Tests for TimescaleDB configuration and retention policies
/// Note: Tests run against in-memory database which doesn't support TimescaleDB,
/// so they primarily test fallback behavior and compliance checking
/// </summary>
public class TimescaleDbConfigurationTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<ILogger<TimescaleDbConfiguration>> _loggerMock;
    private readonly TimescaleDbConfiguration _sut;

    public TimescaleDbConfigurationTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _loggerMock = new Mock<ILogger<TimescaleDbConfiguration>>();

        _sut = new TimescaleDbConfiguration(_context, _loggerMock.Object);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    #region CheckRetentionComplianceAsync Tests

    [Fact]
    public async Task CheckRetentionComplianceAsync_WithNoRecords_ShouldReturnCompliantWithMessage()
    {
        // Act
        var result = await _sut.CheckRetentionComplianceAsync();

        // Assert
        result.IsCompliant.Should().BeTrue();
        result.ComplianceMessage.Should().Contain("No audit records found");
        result.TotalRecords.Should().Be(0);
        result.EarliestRecord.Should().BeNull();
        result.LatestRecord.Should().BeNull();
    }

    [Fact]
    public async Task CheckRetentionComplianceAsync_WithRecentRecords_ShouldBeCompliant()
    {
        // Arrange
        var now = DateTime.UtcNow;
        await CreateAuditLogAsync(now.AddDays(-30));
        await CreateAuditLogAsync(now.AddDays(-15));
        await CreateAuditLogAsync(now);

        // Act
        var result = await _sut.CheckRetentionComplianceAsync();

        // Assert
        result.IsCompliant.Should().BeTrue();
        result.ComplianceMessage.Should().Contain("HIPAA compliant");
        result.TotalRecords.Should().Be(3);
        result.ActualRetentionDays.Should().BeInRange(29, 31);
        result.RetentionDays.Should().Be(2555); // 7 years
    }

    [Fact]
    public async Task CheckRetentionComplianceAsync_ShouldReturnCorrectOldestAndNewestRecords()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var oldest = now.AddDays(-100);
        var newest = now;

        await CreateAuditLogAsync(oldest);
        await CreateAuditLogAsync(now.AddDays(-50));
        await CreateAuditLogAsync(newest);

        // Act
        var result = await _sut.CheckRetentionComplianceAsync();

        // Assert
        result.EarliestRecord.Should().BeCloseTo(oldest, TimeSpan.FromSeconds(1));
        result.LatestRecord.Should().BeCloseTo(newest, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task CheckRetentionComplianceAsync_RetentionDaysShouldBe2555()
    {
        // Act
        var result = await _sut.CheckRetentionComplianceAsync();

        // Assert - Verify HIPAA 7-year requirement
        result.RetentionDays.Should().Be(2555);
    }

    #endregion

    #region GetHypertableInfoAsync Tests (Non-Hypertable Fallback)

    [Fact]
    public async Task GetHypertableInfoAsync_WhenNotHypertable_ShouldReturnFallbackInfo()
    {
        // Arrange
        await CreateAuditLogAsync(DateTime.UtcNow);
        await CreateAuditLogAsync(DateTime.UtcNow);
        await CreateAuditLogAsync(DateTime.UtcNow);

        // Act
        var result = await _sut.GetHypertableInfoAsync();

        // Assert - In-memory DB is not a hypertable
        result.IsHypertable.Should().BeFalse();
        result.HypertableName.Should().Be("AuditLogs");
        result.SchemaName.Should().Be("public");
        result.TotalRows.Should().Be(3);
        result.CompressionEnabled.Should().BeFalse();
        result.RetentionPolicyEnabled.Should().BeFalse();
    }

    [Fact]
    public async Task GetHypertableInfoAsync_WhenNotHypertable_ShouldShowZeroChunks()
    {
        // Act
        var result = await _sut.GetHypertableInfoAsync();

        // Assert
        result.NumChunks.Should().Be(0);
        result.ChunkTimeInterval.Should().Be(TimeSpan.Zero);
    }

    #endregion

    #region GetCompressionStatsAsync Tests (Non-Hypertable Fallback)

    [Fact]
    public async Task GetCompressionStatsAsync_WhenNotHypertable_ShouldReturnZeroStats()
    {
        // Act
        var result = await _sut.GetCompressionStatsAsync();

        // Assert
        result.UncompressedBytes.Should().Be(0);
        result.CompressedBytes.Should().Be(0);
        result.CompressionRatio.Should().Be(0);
        result.CompressedChunks.Should().Be(0);
        result.UncompressedChunks.Should().Be(0);
        result.LastCompressionRun.Should().BeNull();
    }

    #endregion

    #region GetChunkInfoAsync Tests (Non-Hypertable Fallback)

    [Fact]
    public async Task GetChunkInfoAsync_WhenNotHypertable_ShouldReturnEmptyList()
    {
        // Act
        var result = await _sut.GetChunkInfoAsync();

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region CompressOldChunksAsync Tests (Non-Hypertable Fallback)

    [Fact]
    public async Task CompressOldChunksAsync_WhenNotHypertable_ShouldReturnZeroAndLogWarning()
    {
        // Act
        var result = await _sut.CompressOldChunksAsync(TimeSpan.FromDays(30));

        // Assert
        result.Should().Be(0);
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Cannot compress chunks")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region InitializeHypertableAsync Tests

    [Fact]
    public async Task InitializeHypertableAsync_WhenDatabaseDoesNotSupportRawSql_ShouldThrowAndLogError()
    {
        // Act & Assert
        // In-memory database doesn't support raw SQL queries, so this will throw
        var act = async () => await _sut.InitializeHypertableAsync();

        // The method throws because in-memory DB doesn't support SqlQueryRaw
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Relational-specific methods*");

        // Verify error was logged
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to initialize")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region Interface Implementation Tests

    [Fact]
    public void TimescaleDbConfiguration_ShouldImplementITimescaleDbConfiguration()
    {
        // Assert
        _sut.Should().BeAssignableTo<ITimescaleDbConfiguration>();
    }

    [Fact]
    public async Task AllMethods_ShouldHandleEmptyDatabase()
    {
        // Act & Assert - None of these should throw
        var hypertableInfo = await _sut.GetHypertableInfoAsync();
        hypertableInfo.Should().NotBeNull();

        var compressionStats = await _sut.GetCompressionStatsAsync();
        compressionStats.Should().NotBeNull();

        var complianceStatus = await _sut.CheckRetentionComplianceAsync();
        complianceStatus.Should().NotBeNull();

        var chunks = await _sut.GetChunkInfoAsync();
        chunks.Should().NotBeNull();

        var compressedCount = await _sut.CompressOldChunksAsync(TimeSpan.FromDays(30));
        compressedCount.Should().Be(0);
    }

    #endregion

    #region DTO Validation Tests

    [Fact]
    public async Task HypertableInfo_ShouldHaveCorrectDefaultValues()
    {
        // Act
        var result = await _sut.GetHypertableInfoAsync();

        // Assert
        result.HypertableName.Should().NotBeNullOrEmpty();
        result.SchemaName.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task RetentionComplianceStatus_ShouldContainAllRequiredFields()
    {
        // Arrange
        await CreateAuditLogAsync(DateTime.UtcNow);

        // Act
        var result = await _sut.CheckRetentionComplianceAsync();

        // Assert
        result.IsCompliant.Should().BeTrue();
        result.ComplianceMessage.Should().NotBeNullOrEmpty();
        result.RetentionDays.Should().BeGreaterThan(0);
        result.TotalRecords.Should().BeGreaterThan(0);
    }

    #endregion

    #region Helper Methods

    private async Task CreateAuditLogAsync(DateTime timestamp)
    {
        var auditLog = new AuditLog(
            eventType: AuditEventType.View,
            userId: "test-user",
            action: "Test action",
            resourceType: "Patient",
            resourceId: Guid.NewGuid().ToString(),
            ipAddress: "127.0.0.1",
            success: true,
            username: "testuser");

        auditLog.SetTrackingIds(Guid.NewGuid().ToString(), Guid.NewGuid().ToString());

        // Use reflection to set the timestamp for testing
        var timestampProperty = typeof(AuditLog).GetProperty("Timestamp");
        timestampProperty?.SetValue(auditLog, timestamp);

        _context.AuditLogs.Add(auditLog);
        await _context.SaveChangesAsync();
    }

    #endregion
}
