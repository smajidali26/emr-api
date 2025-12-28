using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EMR.Infrastructure.EventSourcing.Configurations;

/// <summary>
/// Entity Framework configuration for OutboxMessage.
/// Implements the Transactional Outbox pattern for reliable event publishing.
/// </summary>
public class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("OutboxMessages");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .ValueGeneratedOnAdd();

        // Index for finding unprocessed messages
        builder.HasIndex(e => new { e.IsProcessed, e.CreatedAt })
            .HasDatabaseName("IX_OutboxMessages_IsProcessed_CreatedAt");

        // Index for retry scheduling
        builder.HasIndex(e => new { e.IsProcessed, e.NextRetryAt })
            .HasDatabaseName("IX_OutboxMessages_IsProcessed_NextRetryAt");

        // Index for correlation tracking
        builder.HasIndex(e => e.CorrelationId)
            .HasDatabaseName("IX_OutboxMessages_CorrelationId");

        // Property configurations
        builder.Property(e => e.EventId)
            .IsRequired();

        builder.Property(e => e.EventType)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(e => e.EventData)
            .IsRequired()
            .HasColumnType("jsonb"); // PostgreSQL JSON binary type

        builder.Property(e => e.OccurredAt)
            .IsRequired();

        builder.Property(e => e.CreatedAt)
            .IsRequired();

        builder.Property(e => e.ProcessedAt);

        builder.Property(e => e.IsProcessed)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(e => e.ProcessingAttempts)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(e => e.LastError)
            .HasMaxLength(2000);

        builder.Property(e => e.NextRetryAt);

        builder.Property(e => e.CorrelationId)
            .HasMaxLength(100);
    }
}
