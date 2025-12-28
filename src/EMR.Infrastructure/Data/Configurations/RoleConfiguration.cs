using EMR.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EMR.Infrastructure.Data.Configurations;

/// <summary>
/// Entity Framework Core configuration for Role entity
/// </summary>
public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        // Table name
        builder.ToTable("Roles");

        // Primary key
        builder.HasKey(r => r.Id);

        // RoleName - Required, unique, indexed
        builder.Property(r => r.RoleName)
            .IsRequired()
            .HasConversion<string>();

        builder.HasIndex(r => r.RoleName)
            .IsUnique()
            .HasDatabaseName("IX_Roles_RoleName");

        // DisplayName - Required
        builder.Property(r => r.DisplayName)
            .IsRequired()
            .HasMaxLength(100);

        // Description - Required
        builder.Property(r => r.Description)
            .IsRequired()
            .HasMaxLength(500);

        // IsSystemRole - Required
        builder.Property(r => r.IsSystemRole)
            .IsRequired()
            .HasDefaultValue(false);

        builder.HasIndex(r => r.IsSystemRole)
            .HasDatabaseName("IX_Roles_IsSystemRole");

        // Audit fields
        builder.Property(r => r.CreatedAt).IsRequired();
        builder.Property(r => r.CreatedBy).IsRequired().HasMaxLength(255);
        builder.Property(r => r.UpdatedAt).IsRequired(false);
        builder.Property(r => r.UpdatedBy).HasMaxLength(255);
        builder.Property(r => r.IsDeleted).IsRequired().HasDefaultValue(false);
        builder.Property(r => r.DeletedAt).IsRequired(false);
        builder.Property(r => r.DeletedBy).HasMaxLength(255);
        builder.Property(r => r.RowVersion).IsRowVersion();

        // Relationships
        builder.HasMany(r => r.Permissions)
            .WithOne(rp => rp.Role)
            .HasForeignKey(rp => rp.RoleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
