using EMR.Domain.Enums;

namespace EMR.Application.Common.Authorization;

/// <summary>
/// Permission constants and policy names for authorization
/// Maps permissions to policy names for use in ASP.NET Core authorization
/// </summary>
public static class PermissionConstants
{
    /// <summary>
    /// Policy name prefix for permission-based policies
    /// </summary>
    private const string PermissionPolicyPrefix = "Permission.";

    /// <summary>
    /// Gets the policy name for a specific permission
    /// </summary>
    public static string GetPolicyName(Permission permission)
    {
        return $"{PermissionPolicyPrefix}{permission}";
    }

    /// <summary>
    /// Role-based policy names for backward compatibility
    /// </summary>
    public static class Roles
    {
        public const string Admin = "Admin";
        public const string Doctor = "Doctor";
        public const string Nurse = "Nurse";
        public const string Staff = "Staff";
        public const string Patient = "Patient";
        public const string AdminOrDoctor = "AdminOrDoctor";
        public const string AdminOrDoctorOrNurse = "AdminOrDoctorOrNurse";
        public const string MedicalStaff = "MedicalStaff"; // Doctor or Nurse
    }

    /// <summary>
    /// Permission-based policy names
    /// </summary>
    public static class Policies
    {
        // User Management Policies
        public const string UsersView = "Permission.UsersView";
        public const string UsersCreate = "Permission.UsersCreate";
        public const string UsersUpdate = "Permission.UsersUpdate";
        public const string UsersDelete = "Permission.UsersDelete";
        public const string UsersManageRoles = "Permission.UsersManageRoles";

        // Patient Management Policies
        public const string PatientsView = "Permission.PatientsView";
        public const string PatientsCreate = "Permission.PatientsCreate";
        public const string PatientsUpdate = "Permission.PatientsUpdate";
        public const string PatientsDelete = "Permission.PatientsDelete";
        public const string PatientsViewOwn = "Permission.PatientsViewOwn";

        // Encounter Management Policies
        public const string EncountersView = "Permission.EncountersView";
        public const string EncountersCreate = "Permission.EncountersCreate";
        public const string EncountersUpdate = "Permission.EncountersUpdate";
        public const string EncountersDelete = "Permission.EncountersDelete";
        public const string EncountersClose = "Permission.EncountersClose";

        // Order Management Policies
        public const string OrdersView = "Permission.OrdersView";
        public const string OrdersCreate = "Permission.OrdersCreate";
        public const string OrdersUpdate = "Permission.OrdersUpdate";
        public const string OrdersDelete = "Permission.OrdersDelete";
        public const string OrdersSign = "Permission.OrdersSign";
        public const string OrdersExecute = "Permission.OrdersExecute";

        // Vital Signs & Assessments Policies
        public const string VitalsView = "Permission.VitalsView";
        public const string VitalsCreate = "Permission.VitalsCreate";
        public const string VitalsUpdate = "Permission.VitalsUpdate";
        public const string VitalsDelete = "Permission.VitalsDelete";

        public const string AssessmentsView = "Permission.AssessmentsView";
        public const string AssessmentsCreate = "Permission.AssessmentsCreate";
        public const string AssessmentsUpdate = "Permission.AssessmentsUpdate";
        public const string AssessmentsDelete = "Permission.AssessmentsDelete";

        // Clinical Notes Policies
        public const string NotesView = "Permission.NotesView";
        public const string NotesCreate = "Permission.NotesCreate";
        public const string NotesUpdate = "Permission.NotesUpdate";
        public const string NotesDelete = "Permission.NotesDelete";
        public const string NotesSign = "Permission.NotesSign";

        // Medications Policies
        public const string MedicationsView = "Permission.MedicationsView";
        public const string MedicationsCreate = "Permission.MedicationsCreate";
        public const string MedicationsUpdate = "Permission.MedicationsUpdate";
        public const string MedicationsDelete = "Permission.MedicationsDelete";
        public const string MedicationsDispense = "Permission.MedicationsDispense";
        public const string MedicationsAdminister = "Permission.MedicationsAdminister";

        // Lab Results Policies
        public const string LabResultsView = "Permission.LabResultsView";
        public const string LabResultsCreate = "Permission.LabResultsCreate";
        public const string LabResultsUpdate = "Permission.LabResultsUpdate";
        public const string LabResultsDelete = "Permission.LabResultsDelete";
        public const string LabResultsVerify = "Permission.LabResultsVerify";

        // Imaging Policies
        public const string ImagingView = "Permission.ImagingView";
        public const string ImagingCreate = "Permission.ImagingCreate";
        public const string ImagingUpdate = "Permission.ImagingUpdate";
        public const string ImagingDelete = "Permission.ImagingDelete";

        // Scheduling Policies
        public const string SchedulingView = "Permission.SchedulingView";
        public const string SchedulingCreate = "Permission.SchedulingCreate";
        public const string SchedulingUpdate = "Permission.SchedulingUpdate";
        public const string SchedulingDelete = "Permission.SchedulingDelete";

        // Billing Policies
        public const string BillingView = "Permission.BillingView";
        public const string BillingCreate = "Permission.BillingCreate";
        public const string BillingUpdate = "Permission.BillingUpdate";
        public const string BillingDelete = "Permission.BillingDelete";

        // Reports & Analytics Policies
        public const string ReportsView = "Permission.ReportsView";
        public const string ReportsGenerate = "Permission.ReportsGenerate";
        public const string ReportsExport = "Permission.ReportsExport";

        // System Administration Policies
        public const string SystemSettingsView = "Permission.SystemSettingsView";
        public const string SystemSettingsUpdate = "Permission.SystemSettingsUpdate";
        public const string AuditLogsView = "Permission.AuditLogsView";

        // Role & Permission Management Policies
        public const string RolesView = "Permission.RolesView";
        public const string RolesCreate = "Permission.RolesCreate";
        public const string RolesUpdate = "Permission.RolesUpdate";
        public const string RolesDelete = "Permission.RolesDelete";
        public const string PermissionsView = "Permission.PermissionsView";
        public const string PermissionsAssign = "Permission.PermissionsAssign";
    }
}
