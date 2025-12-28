using EMR.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EMR.Infrastructure.Data.Configurations;

/// <summary>
/// Entity Framework configuration for AuditLog entity
/// Enforces HIPAA requirements: immutability, retention, indexing for queries
///
/// TIMESCALEDB ENHANCEMENT:
/// - Composite primary key (Timestamp, Id) required for hypertable partitioning
/// - Optimized indexes for time-range queries
/// - 7-year retention policy (2,555 days) for HIPAA compliance
/// </summary>
public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs");

        // TIMESCALEDB: Composite primary key with Timestamp first
        // TimescaleDB requires the time partitioning column in the primary key
        builder.HasKey(a => new { a.Timestamp, a.Id });

        // Required fields
        builder.Property(a => a.EventType)
            .IsRequired();

        builder.Property(a => a.UserId)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(a => a.Username)
            .HasMaxLength(256);

        builder.Property(a => a.Timestamp)
            .IsRequired();

        builder.Property(a => a.ResourceType)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(a => a.ResourceId)
            .HasMaxLength(100);

        builder.Property(a => a.Action)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(a => a.Details)
            .HasMaxLength(2000);

        builder.Property(a => a.IpAddress)
            .HasMaxLength(45); // IPv6 max length

        builder.Property(a => a.UserAgent)
            .HasMaxLength(500);

        builder.Property(a => a.Success)
            .IsRequired();

        builder.Property(a => a.ErrorMessage)
            .HasMaxLength(2000);

        builder.Property(a => a.HttpMethod)
            .HasMaxLength(10);

        builder.Property(a => a.RequestPath)
            .HasMaxLength(500);

        builder.Property(a => a.SessionId)
            .HasMaxLength(100);

        builder.Property(a => a.CorrelationId)
            .HasMaxLength(100);

        // JSON columns for change tracking
        builder.Property(a => a.OldValues)
            .HasColumnType("jsonb"); // PostgreSQL JSONB for efficient querying

        builder.Property(a => a.NewValues)
            .HasColumnType("jsonb");

        // Indexes for common query patterns
        // Critical for HIPAA compliance officer queries
        builder.HasIndex(a => a.UserId)
            .HasDatabaseName("IX_AuditLogs_UserId");

        builder.HasIndex(a => a.Timestamp)
            .HasDatabaseName("IX_AuditLogs_Timestamp");

        builder.HasIndex(a => a.EventType)
            .HasDatabaseName("IX_AuditLogs_EventType");

        builder.HasIndex(a => new { a.ResourceType, a.ResourceId })
            .HasDatabaseName("IX_AuditLogs_Resource");

        builder.HasIndex(a => a.IpAddress)
            .HasDatabaseName("IX_AuditLogs_IpAddress");

        builder.HasIndex(a => a.SessionId)
            .HasDatabaseName("IX_AuditLogs_SessionId");

        builder.HasIndex(a => a.CorrelationId)
            .HasDatabaseName("IX_AuditLogs_CorrelationId");

        // Composite index for date range queries
        builder.HasIndex(a => new { a.Timestamp, a.EventType })
            .HasDatabaseName("IX_AuditLogs_Timestamp_EventType");

        // Composite index for user activity tracking
        builder.HasIndex(a => new { a.UserId, a.Timestamp })
            .HasDatabaseName("IX_AuditLogs_UserId_Timestamp");

        // CRITICAL: No delete or update operations allowed
        // Audit logs are immutable for HIPAA compliance
        // This will be enforced at the database level via migration constraints
    }
}
