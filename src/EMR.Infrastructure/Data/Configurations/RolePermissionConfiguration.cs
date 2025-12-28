using EMR.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EMR.Infrastructure.Data.Configurations;

/// <summary>
/// Entity Framework Core configuration for RolePermission entity
/// </summary>
public class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> builder)
    {
        // Table name
        builder.ToTable("RolePermissions");

        // Primary key
        builder.HasKey(rp => rp.Id);

        // RoleId - Required, indexed
        builder.Property(rp => rp.RoleId)
            .IsRequired();

        builder.HasIndex(rp => rp.RoleId)
            .HasDatabaseName("IX_RolePermissions_RoleId");

        // Permission - Required, stored as string
        builder.Property(rp => rp.Permission)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(100);

        // Composite index for efficient lookups
        builder.HasIndex(rp => new { rp.RoleId, rp.Permission })
            .HasDatabaseName("IX_RolePermissions_RoleId_Permission");

        // Audit fields
        builder.Property(rp => rp.CreatedAt).IsRequired();
        builder.Property(rp => rp.CreatedBy).IsRequired().HasMaxLength(255);
        builder.Property(rp => rp.UpdatedAt).IsRequired(false);
        builder.Property(rp => rp.UpdatedBy).HasMaxLength(255);
        builder.Property(rp => rp.IsDeleted).IsRequired().HasDefaultValue(false);
        builder.Property(rp => rp.DeletedAt).IsRequired(false);
        builder.Property(rp => rp.DeletedBy).HasMaxLength(255);
        builder.Property(rp => rp.RowVersion).IsRowVersion();

        // Relationships are configured in RoleConfiguration
    }
}
