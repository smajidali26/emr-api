using EMR.Domain.Entities;
using EMR.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EMR.Infrastructure.Data.Configurations;

/// <summary>
/// Entity Framework Core configuration for User entity
/// </summary>
public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        // Table name
        builder.ToTable("Users");

        // Primary key
        builder.HasKey(u => u.Id);

        // Email - Required, unique, indexed
        builder.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(255);

        builder.HasIndex(u => u.Email)
            .IsUnique()
            .HasDatabaseName("IX_Users_Email");

        // FirstName - Required
        builder.Property(u => u.FirstName)
            .IsRequired()
            .HasMaxLength(100);

        // LastName - Required
        builder.Property(u => u.LastName)
            .IsRequired()
            .HasMaxLength(100);

        // AzureAdB2CId - Required, unique, indexed
        builder.Property(u => u.AzureAdB2CId)
            .IsRequired()
            .HasMaxLength(255);

        builder.HasIndex(u => u.AzureAdB2CId)
            .IsUnique()
            .HasDatabaseName("IX_Users_AzureAdB2CId");

        // Roles - Store as JSON array
        builder.Property(u => u.Roles)
            .HasConversion(
                v => string.Join(",", v.Select(r => (int)r)),
                v => v.Split(',', StringSplitOptions.RemoveEmptyEntries)
                      .Select(r => (UserRole)int.Parse(r))
                      .ToList()
            )
            .HasColumnName("Roles")
            .HasMaxLength(500);

        // IsActive - Required, indexed for performance
        builder.Property(u => u.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.HasIndex(u => u.IsActive)
            .HasDatabaseName("IX_Users_IsActive");

        // LastLoginAt - Optional
        builder.Property(u => u.LastLoginAt)
            .IsRequired(false);

        // CreatedAt - Required
        builder.Property(u => u.CreatedAt)
            .IsRequired();

        // CreatedBy - Required
        builder.Property(u => u.CreatedBy)
            .IsRequired()
            .HasMaxLength(255);

        // UpdatedAt - Optional
        builder.Property(u => u.UpdatedAt)
            .IsRequired(false);

        // UpdatedBy - Optional
        builder.Property(u => u.UpdatedBy)
            .HasMaxLength(255);

        // IsDeleted - Required (for soft delete)
        builder.Property(u => u.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);

        builder.HasIndex(u => u.IsDeleted)
            .HasDatabaseName("IX_Users_IsDeleted");

        // DeletedAt - Optional
        builder.Property(u => u.DeletedAt)
            .IsRequired(false);

        // DeletedBy - Optional
        builder.Property(u => u.DeletedBy)
            .HasMaxLength(255);

        // RowVersion - Concurrency token
        builder.Property(u => u.RowVersion)
            .IsRowVersion();

        // Ignore computed property
        builder.Ignore(u => u.FullName);
    }
}
