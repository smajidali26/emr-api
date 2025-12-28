namespace EMR.Domain.Enums;

/// <summary>
/// Granular permissions in the EMR system
/// Each permission represents a specific action on a resource type
/// </summary>
public enum Permission
{
    // User Management
    UsersView = 1,
    UsersCreate = 2,
    UsersUpdate = 3,
    UsersDelete = 4,
    UsersManageRoles = 5,

    // Patient Management
    PatientsView = 10,
    PatientsCreate = 11,
    PatientsUpdate = 12,
    PatientsDelete = 13,
    PatientsViewOwn = 14, // Patient can view their own records

    // Encounter Management
    EncountersView = 20,
    EncountersCreate = 21,
    EncountersUpdate = 22,
    EncountersDelete = 23,
    EncountersClose = 24,

    // Order Management
    OrdersView = 30,
    OrdersCreate = 31,
    OrdersUpdate = 32,
    OrdersDelete = 33,
    OrdersSign = 34,
    OrdersExecute = 35,

    // Vital Signs & Assessments
    VitalsView = 40,
    VitalsCreate = 41,
    VitalsUpdate = 42,
    VitalsDelete = 43,

    AssessmentsView = 50,
    AssessmentsCreate = 51,
    AssessmentsUpdate = 52,
    AssessmentsDelete = 53,

    // Clinical Notes
    NotesView = 60,
    NotesCreate = 61,
    NotesUpdate = 62,
    NotesDelete = 63,
    NotesSign = 64,

    // Medications
    MedicationsView = 70,
    MedicationsCreate = 71,
    MedicationsUpdate = 72,
    MedicationsDelete = 73,
    MedicationsDispense = 74,
    MedicationsAdminister = 75,

    // Lab Results
    LabResultsView = 80,
    LabResultsCreate = 81,
    LabResultsUpdate = 82,
    LabResultsDelete = 83,
    LabResultsVerify = 84,

    // Imaging
    ImagingView = 90,
    ImagingCreate = 91,
    ImagingUpdate = 92,
    ImagingDelete = 93,

    // Scheduling
    SchedulingView = 100,
    SchedulingCreate = 101,
    SchedulingUpdate = 102,
    SchedulingDelete = 103,

    // Billing
    BillingView = 110,
    BillingCreate = 111,
    BillingUpdate = 112,
    BillingDelete = 113,

    // Reports & Analytics
    ReportsView = 120,
    ReportsGenerate = 121,
    ReportsExport = 122,

    // System Administration
    SystemSettingsView = 130,
    SystemSettingsUpdate = 131,
    AuditLogsView = 132,

    // Role & Permission Management
    RolesView = 140,
    RolesCreate = 141,
    RolesUpdate = 142,
    RolesDelete = 143,
    PermissionsView = 144,
    PermissionsAssign = 145
}
