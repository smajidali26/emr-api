using EMR.Application.Common.Authorization;
using EMR.Domain.Entities;
using EMR.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EMR.Infrastructure.Data.Seeds;

/// <summary>
/// Seeds initial roles and permissions into the database
/// Creates the standard EMR roles with their default permissions
/// </summary>
public class RoleSeeder
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<RoleSeeder> _logger;

    public RoleSeeder(ApplicationDbContext context, ILogger<RoleSeeder> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Seeds all system roles with their default permissions
    /// </summary>
    public async Task SeedAsync()
    {
        try
        {
            _logger.LogInformation("Starting role seeding process");

            // Check if roles already exist
            if (await _context.Roles.AnyAsync())
            {
                _logger.LogInformation("Roles already exist, skipping seed");
                return;
            }

            var systemUser = "System";

            // Create all system roles
            var roles = new[]
            {
                CreateRole(UserRole.Admin, "Administrator", "Full system access with all permissions", systemUser),
                CreateRole(UserRole.Doctor, "Doctor", "Medical professional with patient care privileges", systemUser),
                CreateRole(UserRole.Nurse, "Nurse", "Nursing staff with patient care support privileges", systemUser),
                CreateRole(UserRole.Staff, "Staff", "Administrative and support staff", systemUser),
                CreateRole(UserRole.Patient, "Patient", "Patient with limited access to own records", systemUser)
            };

            await _context.Roles.AddRangeAsync(roles);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Successfully seeded {Count} roles", roles.Length);

            // Assign default permissions to each role
            foreach (var role in roles)
            {
                await AssignDefaultPermissionsAsync(role, systemUser);
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Role seeding completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while seeding roles");
            throw;
        }
    }

    private static Role CreateRole(UserRole roleName, string displayName, string description, string createdBy)
    {
        return new Role(roleName, displayName, description, isSystemRole: true, createdBy);
    }

    private async Task AssignDefaultPermissionsAsync(Role role, string grantedBy)
    {
        var permissions = RolePermissionMatrix.GetPermissionsForRole(role.RoleName);

        foreach (var permission in permissions)
        {
            role.AddPermission(permission, grantedBy);
        }

        _logger.LogInformation(
            "Assigned {Count} permissions to role {RoleName}",
            permissions.Count(),
            role.DisplayName);

        await Task.CompletedTask;
    }
}
