using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EMR.Infrastructure.EventSourcing.Configurations;

/// <summary>
/// Entity Framework configuration for SnapshotEntry.
/// </summary>
public class SnapshotEntryConfiguration : IEntityTypeConfiguration<SnapshotEntry>
{
    public void Configure(EntityTypeBuilder<SnapshotEntry> builder)
    {
        builder.ToTable("Snapshots");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .ValueGeneratedOnAdd();

        // Index for aggregate lookup
        builder.HasIndex(e => new { e.AggregateId, e.Version })
            .HasDatabaseName("IX_Snapshots_AggregateId_Version");

        // Property configurations
        builder.Property(e => e.AggregateId)
            .IsRequired();

        builder.Property(e => e.AggregateType)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(e => e.Version)
            .IsRequired();

        builder.Property(e => e.SnapshotData)
            .IsRequired()
            .HasColumnType("jsonb"); // PostgreSQL JSON binary type

        builder.Property(e => e.CreatedAt)
            .IsRequired();

        builder.Property(e => e.SnapshotType)
            .HasMaxLength(500)
            .IsRequired();
    }
}
