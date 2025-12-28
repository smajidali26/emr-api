using EMR.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EMR.Infrastructure.Data.Configurations;

/// <summary>
/// Entity Framework Core configuration for ResourceAuthorization entity
/// </summary>
public class ResourceAuthorizationConfiguration : IEntityTypeConfiguration<ResourceAuthorization>
{
    public void Configure(EntityTypeBuilder<ResourceAuthorization> builder)
    {
        // Table name
        builder.ToTable("ResourceAuthorizations");

        // Primary key
        builder.HasKey(ra => ra.Id);

        // UserId - Required, indexed
        builder.Property(ra => ra.UserId)
            .IsRequired();

        builder.HasIndex(ra => ra.UserId)
            .HasDatabaseName("IX_ResourceAuthorizations_UserId");

        // ResourceType - Required, stored as string
        builder.Property(ra => ra.ResourceType)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        // ResourceId - Required, indexed
        builder.Property(ra => ra.ResourceId)
            .IsRequired();

        builder.HasIndex(ra => ra.ResourceId)
            .HasDatabaseName("IX_ResourceAuthorizations_ResourceId");

        // Permission - Required, stored as string
        builder.Property(ra => ra.Permission)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(100);

        // EffectiveFrom - Required
        builder.Property(ra => ra.EffectiveFrom)
            .IsRequired();

        // EffectiveTo - Optional
        builder.Property(ra => ra.EffectiveTo)
            .IsRequired(false);

        // Reason - Optional
        builder.Property(ra => ra.Reason)
            .HasMaxLength(500);

        // Composite index for efficient authorization checks
        builder.HasIndex(ra => new { ra.UserId, ra.ResourceType, ra.ResourceId, ra.Permission })
            .HasDatabaseName("IX_ResourceAuthorizations_UserId_ResourceType_ResourceId_Permission");

        // Index for resource-based queries
        builder.HasIndex(ra => new { ra.ResourceType, ra.ResourceId })
            .HasDatabaseName("IX_ResourceAuthorizations_ResourceType_ResourceId");

        // Audit fields
        builder.Property(ra => ra.CreatedAt).IsRequired();
        builder.Property(ra => ra.CreatedBy).IsRequired().HasMaxLength(255);
        builder.Property(ra => ra.UpdatedAt).IsRequired(false);
        builder.Property(ra => ra.UpdatedBy).HasMaxLength(255);
        builder.Property(ra => ra.IsDeleted).IsRequired().HasDefaultValue(false);
        builder.Property(ra => ra.DeletedAt).IsRequired(false);
        builder.Property(ra => ra.DeletedBy).HasMaxLength(255);
        builder.Property(ra => ra.RowVersion).IsRowVersion();

        // Computed property is ignored
        builder.Ignore(ra => ra.IsActive);

        // Relationships
        builder.HasOne(ra => ra.User)
            .WithMany()
            .HasForeignKey(ra => ra.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
