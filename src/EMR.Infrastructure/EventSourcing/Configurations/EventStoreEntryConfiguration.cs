using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EMR.Infrastructure.EventSourcing.Configurations;

/// <summary>
/// Entity Framework configuration for EventStoreEntry.
/// Configures the event store table with appropriate indexes for performance.
/// </summary>
public class EventStoreEntryConfiguration : IEntityTypeConfiguration<EventStoreEntry>
{
    public void Configure(EntityTypeBuilder<EventStoreEntry> builder)
    {
        builder.ToTable("EventStore");

        builder.HasKey(e => e.Id);

        // Use database-generated sequence for Id
        builder.Property(e => e.Id)
            .ValueGeneratedOnAdd();

        // EventId should be unique
        builder.HasIndex(e => e.EventId)
            .IsUnique();

        // Index for aggregate lookup - most common query pattern
        builder.HasIndex(e => new { e.AggregateId, e.Version })
            .IsUnique()
            .HasDatabaseName("IX_EventStore_AggregateId_Version");

        // Index for time-based queries
        builder.HasIndex(e => e.OccurredAt)
            .HasDatabaseName("IX_EventStore_OccurredAt");

        // Index for correlation tracking
        builder.HasIndex(e => e.CorrelationId)
            .HasDatabaseName("IX_EventStore_CorrelationId");

        // Index for event type queries
        builder.HasIndex(e => e.EventType)
            .HasDatabaseName("IX_EventStore_EventType");

        // Index for global ordering
        builder.HasIndex(e => e.SequenceNumber)
            .HasDatabaseName("IX_EventStore_SequenceNumber");

        // Property configurations
        builder.Property(e => e.EventId)
            .IsRequired();

        builder.Property(e => e.AggregateId)
            .IsRequired();

        builder.Property(e => e.AggregateType)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(e => e.EventType)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(e => e.Version)
            .IsRequired();

        builder.Property(e => e.EventVersion)
            .IsRequired()
            .HasDefaultValue(1);

        builder.Property(e => e.EventData)
            .IsRequired()
            .HasColumnType("jsonb"); // PostgreSQL JSON binary type

        builder.Property(e => e.Metadata)
            .HasColumnType("jsonb");

        builder.Property(e => e.OccurredAt)
            .IsRequired();

        builder.Property(e => e.PersistedAt)
            .IsRequired();

        builder.Property(e => e.UserId)
            .HasMaxLength(100);

        builder.Property(e => e.CorrelationId)
            .HasMaxLength(100);

        builder.Property(e => e.CausationId)
            .HasMaxLength(100);

        builder.Property(e => e.SequenceNumber)
            .IsRequired()
            .ValueGeneratedOnAdd();
    }
}
