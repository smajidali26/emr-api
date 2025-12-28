using EMR.Application.Common.Abstractions;
using EMR.Application.Common.DTOs;
using EMR.Application.Features.Roles.DTOs;
using EMR.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace EMR.Application.Features.Roles.Queries.GetAllPermissions;

/// <summary>
/// Handler for GetAllPermissionsQuery
/// Returns all permissions defined in the Permission enum with descriptions
/// </summary>
public class GetAllPermissionsQueryHandler : IQueryHandler<GetAllPermissionsQuery, ResultDto<IEnumerable<PermissionDto>>>
{
    private readonly ILogger<GetAllPermissionsQueryHandler> _logger;

    public GetAllPermissionsQueryHandler(ILogger<GetAllPermissionsQueryHandler> logger)
    {
        _logger = logger;
    }

    public Task<ResultDto<IEnumerable<PermissionDto>>> Handle(
        GetAllPermissionsQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Fetching all permissions");

            var permissions = Enum.GetValues<Permission>()
                .Select(p => new PermissionDto
                {
                    Permission = p,
                    Name = p.ToString(),
                    Description = GetPermissionDescription(p),
                    Category = GetPermissionCategory(p)
                })
                .OrderBy(p => p.Category)
                .ThenBy(p => p.Name)
                .ToList();

            _logger.LogInformation("Successfully fetched {Count} permissions", permissions.Count);

            return Task.FromResult(ResultDto<IEnumerable<PermissionDto>>.Success(permissions));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching all permissions");
            return Task.FromResult(ResultDto<IEnumerable<PermissionDto>>.Failure("Failed to fetch permissions"));
        }
    }

    private static string GetPermissionDescription(Permission permission)
    {
        return permission switch
        {
            // User Management
            Permission.UsersView => "View user accounts and profiles",
            Permission.UsersCreate => "Create new user accounts",
            Permission.UsersUpdate => "Update existing user accounts",
            Permission.UsersDelete => "Delete user accounts",
            Permission.UsersManageRoles => "Assign and manage user roles",

            // Patient Management
            Permission.PatientsView => "View all patient records",
            Permission.PatientsCreate => "Create new patient records",
            Permission.PatientsUpdate => "Update patient information",
            Permission.PatientsDelete => "Delete patient records",
            Permission.PatientsViewOwn => "View own patient records only",

            // Encounter Management
            Permission.EncountersView => "View clinical encounters",
            Permission.EncountersCreate => "Create new encounters",
            Permission.EncountersUpdate => "Update encounter details",
            Permission.EncountersDelete => "Delete encounters",
            Permission.EncountersClose => "Close and finalize encounters",

            // Order Management
            Permission.OrdersView => "View medical orders",
            Permission.OrdersCreate => "Create new orders",
            Permission.OrdersUpdate => "Update order details",
            Permission.OrdersDelete => "Delete orders",
            Permission.OrdersSign => "Sign and approve orders",
            Permission.OrdersExecute => "Execute and fulfill orders",

            // Vital Signs & Assessments
            Permission.VitalsView => "View vital signs",
            Permission.VitalsCreate => "Record new vital signs",
            Permission.VitalsUpdate => "Update vital sign records",
            Permission.VitalsDelete => "Delete vital sign records",
            Permission.AssessmentsView => "View clinical assessments",
            Permission.AssessmentsCreate => "Create new assessments",
            Permission.AssessmentsUpdate => "Update assessment details",
            Permission.AssessmentsDelete => "Delete assessments",

            // Clinical Notes
            Permission.NotesView => "View clinical notes",
            Permission.NotesCreate => "Create new clinical notes",
            Permission.NotesUpdate => "Update clinical notes",
            Permission.NotesDelete => "Delete clinical notes",
            Permission.NotesSign => "Sign and authenticate notes",

            // Medications
            Permission.MedicationsView => "View medication records",
            Permission.MedicationsCreate => "Prescribe new medications",
            Permission.MedicationsUpdate => "Update medication orders",
            Permission.MedicationsDelete => "Delete medication orders",
            Permission.MedicationsDispense => "Dispense medications",
            Permission.MedicationsAdminister => "Administer medications to patients",

            // Lab Results
            Permission.LabResultsView => "View laboratory results",
            Permission.LabResultsCreate => "Enter new lab results",
            Permission.LabResultsUpdate => "Update lab results",
            Permission.LabResultsDelete => "Delete lab results",
            Permission.LabResultsVerify => "Verify and sign lab results",

            // Imaging
            Permission.ImagingView => "View imaging studies",
            Permission.ImagingCreate => "Order new imaging studies",
            Permission.ImagingUpdate => "Update imaging orders",
            Permission.ImagingDelete => "Delete imaging orders",

            // Scheduling
            Permission.SchedulingView => "View appointments and schedules",
            Permission.SchedulingCreate => "Create new appointments",
            Permission.SchedulingUpdate => "Update appointment details",
            Permission.SchedulingDelete => "Cancel appointments",

            // Billing
            Permission.BillingView => "View billing and claims",
            Permission.BillingCreate => "Create new billing records",
            Permission.BillingUpdate => "Update billing information",
            Permission.BillingDelete => "Delete billing records",

            // Reports & Analytics
            Permission.ReportsView => "View system reports",
            Permission.ReportsGenerate => "Generate new reports",
            Permission.ReportsExport => "Export report data",

            // System Administration
            Permission.SystemSettingsView => "View system settings",
            Permission.SystemSettingsUpdate => "Update system configuration",
            Permission.AuditLogsView => "View audit logs",

            // Role & Permission Management
            Permission.RolesView => "View roles and permissions",
            Permission.RolesCreate => "Create new roles",
            Permission.RolesUpdate => "Update role definitions",
            Permission.RolesDelete => "Delete roles",
            Permission.PermissionsView => "View permission definitions",
            Permission.PermissionsAssign => "Assign permissions to roles",

            _ => permission.ToString()
        };
    }

    private static string GetPermissionCategory(Permission permission)
    {
        return permission switch
        {
            >= Permission.UsersView and <= Permission.UsersManageRoles => "User Management",
            >= Permission.PatientsView and <= Permission.PatientsViewOwn => "Patient Management",
            >= Permission.EncountersView and <= Permission.EncountersClose => "Encounter Management",
            >= Permission.OrdersView and <= Permission.OrdersExecute => "Order Management",
            >= Permission.VitalsView and <= Permission.VitalsDelete => "Vital Signs",
            >= Permission.AssessmentsView and <= Permission.AssessmentsDelete => "Clinical Assessments",
            >= Permission.NotesView and <= Permission.NotesSign => "Clinical Notes",
            >= Permission.MedicationsView and <= Permission.MedicationsAdminister => "Medications",
            >= Permission.LabResultsView and <= Permission.LabResultsVerify => "Laboratory",
            >= Permission.ImagingView and <= Permission.ImagingDelete => "Imaging",
            >= Permission.SchedulingView and <= Permission.SchedulingDelete => "Scheduling",
            >= Permission.BillingView and <= Permission.BillingDelete => "Billing",
            >= Permission.ReportsView and <= Permission.ReportsExport => "Reports & Analytics",
            >= Permission.SystemSettingsView and <= Permission.AuditLogsView => "System Administration",
            >= Permission.RolesView and <= Permission.PermissionsAssign => "Role & Permission Management",
            _ => "Other"
        };
    }
}
