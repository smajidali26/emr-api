using EMR.Domain.Enums;

namespace EMR.Application.Common.Authorization;

/// <summary>
/// Defines the default permission matrix for each role
/// Used to seed role permissions during system initialization
/// </summary>
public static class RolePermissionMatrix
{
    /// <summary>
    /// Gets all permissions for a specific role
    /// </summary>
    public static IEnumerable<Permission> GetPermissionsForRole(UserRole role)
    {
        return role switch
        {
            UserRole.Admin => GetAdminPermissions(),
            UserRole.Doctor => GetDoctorPermissions(),
            UserRole.Nurse => GetNursePermissions(),
            UserRole.Staff => GetStaffPermissions(),
            UserRole.Patient => GetPatientPermissions(),
            _ => Enumerable.Empty<Permission>()
        };
    }

    /// <summary>
    /// Admin has full access to all system features
    /// </summary>
    private static IEnumerable<Permission> GetAdminPermissions()
    {
        return new[]
        {
            // User Management - Full Access
            Permission.UsersView,
            Permission.UsersCreate,
            Permission.UsersUpdate,
            Permission.UsersDelete,
            Permission.UsersManageRoles,

            // Patient Management - Full Access
            Permission.PatientsView,
            Permission.PatientsCreate,
            Permission.PatientsUpdate,
            Permission.PatientsDelete,

            // Encounter Management - Full Access
            Permission.EncountersView,
            Permission.EncountersCreate,
            Permission.EncountersUpdate,
            Permission.EncountersDelete,
            Permission.EncountersClose,

            // Order Management - Full Access
            Permission.OrdersView,
            Permission.OrdersCreate,
            Permission.OrdersUpdate,
            Permission.OrdersDelete,
            Permission.OrdersSign,
            Permission.OrdersExecute,

            // Vitals & Assessments - Full Access
            Permission.VitalsView,
            Permission.VitalsCreate,
            Permission.VitalsUpdate,
            Permission.VitalsDelete,
            Permission.AssessmentsView,
            Permission.AssessmentsCreate,
            Permission.AssessmentsUpdate,
            Permission.AssessmentsDelete,

            // Clinical Notes - Full Access
            Permission.NotesView,
            Permission.NotesCreate,
            Permission.NotesUpdate,
            Permission.NotesDelete,
            Permission.NotesSign,

            // Medications - Full Access
            Permission.MedicationsView,
            Permission.MedicationsCreate,
            Permission.MedicationsUpdate,
            Permission.MedicationsDelete,
            Permission.MedicationsDispense,
            Permission.MedicationsAdminister,

            // Lab Results - Full Access
            Permission.LabResultsView,
            Permission.LabResultsCreate,
            Permission.LabResultsUpdate,
            Permission.LabResultsDelete,
            Permission.LabResultsVerify,

            // Imaging - Full Access
            Permission.ImagingView,
            Permission.ImagingCreate,
            Permission.ImagingUpdate,
            Permission.ImagingDelete,

            // Scheduling - Full Access
            Permission.SchedulingView,
            Permission.SchedulingCreate,
            Permission.SchedulingUpdate,
            Permission.SchedulingDelete,

            // Billing - Full Access
            Permission.BillingView,
            Permission.BillingCreate,
            Permission.BillingUpdate,
            Permission.BillingDelete,

            // Reports & Analytics - Full Access
            Permission.ReportsView,
            Permission.ReportsGenerate,
            Permission.ReportsExport,

            // System Administration - Full Access
            Permission.SystemSettingsView,
            Permission.SystemSettingsUpdate,
            Permission.AuditLogsView,

            // Role & Permission Management - Full Access
            Permission.RolesView,
            Permission.RolesCreate,
            Permission.RolesUpdate,
            Permission.RolesDelete,
            Permission.PermissionsView,
            Permission.PermissionsAssign
        };
    }

    /// <summary>
    /// Doctor has read/write access to patients, encounters, orders, and notes
    /// </summary>
    private static IEnumerable<Permission> GetDoctorPermissions()
    {
        return new[]
        {
            // User Management - View only
            Permission.UsersView,

            // Patient Management - Full CRUD
            Permission.PatientsView,
            Permission.PatientsCreate,
            Permission.PatientsUpdate,

            // Encounter Management - Full CRUD
            Permission.EncountersView,
            Permission.EncountersCreate,
            Permission.EncountersUpdate,
            Permission.EncountersClose,

            // Order Management - Full access
            Permission.OrdersView,
            Permission.OrdersCreate,
            Permission.OrdersUpdate,
            Permission.OrdersSign,

            // Vitals & Assessments - View and Create
            Permission.VitalsView,
            Permission.VitalsCreate,
            Permission.AssessmentsView,
            Permission.AssessmentsCreate,
            Permission.AssessmentsUpdate,

            // Clinical Notes - Full access
            Permission.NotesView,
            Permission.NotesCreate,
            Permission.NotesUpdate,
            Permission.NotesSign,

            // Medications - Full access
            Permission.MedicationsView,
            Permission.MedicationsCreate,
            Permission.MedicationsUpdate,

            // Lab Results - View and Verify
            Permission.LabResultsView,
            Permission.LabResultsVerify,

            // Imaging - View and Create
            Permission.ImagingView,
            Permission.ImagingCreate,

            // Scheduling - View and Create
            Permission.SchedulingView,
            Permission.SchedulingCreate,
            Permission.SchedulingUpdate,

            // Billing - View only
            Permission.BillingView,

            // Reports - View only
            Permission.ReportsView
        };
    }

    /// <summary>
    /// Nurse has read access to patients, write access to vitals/assessments, read access to orders
    /// </summary>
    private static IEnumerable<Permission> GetNursePermissions()
    {
        return new[]
        {
            // Patient Management - Read only
            Permission.PatientsView,

            // Encounter Management - View only
            Permission.EncountersView,

            // Order Management - View and Execute
            Permission.OrdersView,
            Permission.OrdersExecute,

            // Vitals & Assessments - Full CRUD
            Permission.VitalsView,
            Permission.VitalsCreate,
            Permission.VitalsUpdate,
            Permission.AssessmentsView,
            Permission.AssessmentsCreate,
            Permission.AssessmentsUpdate,

            // Clinical Notes - View and Create
            Permission.NotesView,
            Permission.NotesCreate,

            // Medications - View and Administer
            Permission.MedicationsView,
            Permission.MedicationsAdminister,

            // Lab Results - View only
            Permission.LabResultsView,

            // Imaging - View only
            Permission.ImagingView,

            // Scheduling - View only
            Permission.SchedulingView
        };
    }

    /// <summary>
    /// Staff has read access to patients and scheduling access
    /// </summary>
    private static IEnumerable<Permission> GetStaffPermissions()
    {
        return new[]
        {
            // Patient Management - Read only
            Permission.PatientsView,

            // Scheduling - Full CRUD
            Permission.SchedulingView,
            Permission.SchedulingCreate,
            Permission.SchedulingUpdate,
            Permission.SchedulingDelete,

            // Billing - View and Create
            Permission.BillingView,
            Permission.BillingCreate,
            Permission.BillingUpdate
        };
    }

    /// <summary>
    /// Patient can only read their own records
    /// </summary>
    private static IEnumerable<Permission> GetPatientPermissions()
    {
        return new[]
        {
            // Patient Management - View own records only
            Permission.PatientsViewOwn,

            // Encounter Management - View own
            Permission.EncountersView,

            // Order Management - View own
            Permission.OrdersView,

            // Vitals - View own
            Permission.VitalsView,

            // Assessments - View own
            Permission.AssessmentsView,

            // Clinical Notes - View own
            Permission.NotesView,

            // Medications - View own
            Permission.MedicationsView,

            // Lab Results - View own
            Permission.LabResultsView,

            // Imaging - View own
            Permission.ImagingView,

            // Scheduling - View and Create own
            Permission.SchedulingView,
            Permission.SchedulingCreate
        };
    }

    /// <summary>
    /// Checks if a role has a specific permission based on the matrix
    /// </summary>
    public static bool RoleHasPermission(UserRole role, Permission permission)
    {
        return GetPermissionsForRole(role).Contains(permission);
    }
}
