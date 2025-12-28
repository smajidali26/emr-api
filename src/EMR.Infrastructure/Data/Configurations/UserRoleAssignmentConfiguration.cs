using EMR.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EMR.Infrastructure.Data.Configurations;

/// <summary>
/// Entity Framework Core configuration for UserRoleAssignment entity
/// </summary>
public class UserRoleAssignmentConfiguration : IEntityTypeConfiguration<UserRoleAssignment>
{
    public void Configure(EntityTypeBuilder<UserRoleAssignment> builder)
    {
        // Table name
        builder.ToTable("UserRoleAssignments");

        // Primary key
        builder.HasKey(ura => ura.Id);

        // UserId - Required, indexed
        builder.Property(ura => ura.UserId)
            .IsRequired();

        builder.HasIndex(ura => ura.UserId)
            .HasDatabaseName("IX_UserRoleAssignments_UserId");

        // Role - Required, stored as string
        builder.Property(ura => ura.Role)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        // EffectiveFrom - Required
        builder.Property(ura => ura.EffectiveFrom)
            .IsRequired();

        // EffectiveTo - Optional
        builder.Property(ura => ura.EffectiveTo)
            .IsRequired(false);

        // AssignmentReason - Optional
        builder.Property(ura => ura.AssignmentReason)
            .HasMaxLength(500);

        // Composite index for efficient lookups
        builder.HasIndex(ura => new { ura.UserId, ura.Role, ura.EffectiveFrom })
            .HasDatabaseName("IX_UserRoleAssignments_UserId_Role_EffectiveFrom");

        // Audit fields
        builder.Property(ura => ura.CreatedAt).IsRequired();
        builder.Property(ura => ura.CreatedBy).IsRequired().HasMaxLength(255);
        builder.Property(ura => ura.UpdatedAt).IsRequired(false);
        builder.Property(ura => ura.UpdatedBy).HasMaxLength(255);
        builder.Property(ura => ura.IsDeleted).IsRequired().HasDefaultValue(false);
        builder.Property(ura => ura.DeletedAt).IsRequired(false);
        builder.Property(ura => ura.DeletedBy).HasMaxLength(255);
        builder.Property(ura => ura.RowVersion).IsRowVersion();

        // Computed property is ignored
        builder.Ignore(ura => ura.IsActive);

        // Relationships
        builder.HasOne(ura => ura.User)
            .WithMany()
            .HasForeignKey(ura => ura.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
