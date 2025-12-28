using EMR.Domain.Common;
using EMR.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace EMR.Infrastructure.Data;

/// <summary>
/// Main database context for the EMR application
/// </summary>
public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// Users DbSet
    /// </summary>
    public DbSet<User> Users => Set<User>();

    /// <summary>
    /// Roles DbSet
    /// </summary>
    public DbSet<Role> Roles => Set<Role>();

    /// <summary>
    /// Role Permissions DbSet
    /// </summary>
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();

    /// <summary>
    /// User Role Assignments DbSet
    /// </summary>
    public DbSet<UserRoleAssignment> UserRoleAssignments => Set<UserRoleAssignment>();

    /// <summary>
    /// Resource Authorizations DbSet
    /// </summary>
    public DbSet<ResourceAuthorization> ResourceAuthorizations => Set<ResourceAuthorization>();

    /// <summary>
    /// Audit Logs DbSet - HIPAA-compliant audit trail
    /// </summary>
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    /// <summary>
    /// Patients DbSet
    /// </summary>
    public DbSet<Patient> Patients => Set<Patient>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all entity configurations from this assembly
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        // Configure global query filters for soft delete
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(ISoftDeletable).IsAssignableFrom(entityType.ClrType))
            {
                var parameter = System.Linq.Expressions.Expression.Parameter(entityType.ClrType, "e");
                var property = System.Linq.Expressions.Expression.Property(parameter, nameof(ISoftDeletable.IsDeleted));
                var filterExpression = System.Linq.Expressions.Expression.Lambda(
                    System.Linq.Expressions.Expression.Equal(property, System.Linq.Expressions.Expression.Constant(false)),
                    parameter);

                modelBuilder.Entity(entityType.ClrType).HasQueryFilter(filterExpression);
            }
        }
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Update audit fields before saving
        UpdateAuditFields();

        return await base.SaveChangesAsync(cancellationToken);
    }

    private void UpdateAuditFields()
    {
        var entries = ChangeTracker.Entries<BaseEntity>();

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                // CreatedAt and CreatedBy are set in the BaseEntity constructor
                // but we can override them here if needed
            }
            else if (entry.State == EntityState.Modified)
            {
                // Don't update CreatedAt and CreatedBy for modified entities
                entry.Property(e => e.CreatedAt).IsModified = false;
                entry.Property(e => e.CreatedBy).IsModified = false;
            }
        }
    }
}
