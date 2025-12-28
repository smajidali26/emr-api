# Role-Based Access Control (RBAC) & Attribute-Based Access Control (ABAC) Implementation

## Overview

This document describes the comprehensive RBAC/ABAC implementation for the EMR system, providing fine-grained authorization control at both role and resource levels.

## Architecture

The implementation follows Clean Architecture principles with authorization concerns separated across layers:

### Domain Layer (`EMR.Domain`)

#### Entities
- **Role**: Represents a system role (Admin, Doctor, Nurse, Staff, Patient)
- **RolePermission**: Maps permissions to roles (many-to-many)
- **UserRoleAssignment**: Tracks user role assignments with temporal validity
- **ResourceAuthorization**: Provides resource-level access control (ABAC)

#### Enums
- **Permission**: Granular permissions (e.g., PatientsView, OrdersCreate, NotesSign)
- **UserRole**: System roles (Admin, Doctor, Nurse, Staff, Patient)
- **ResourceType**: Types of protected resources (Patient, Encounter, Order, etc.)

### Application Layer (`EMR.Application`)

#### Interfaces
- **IAuthorizationService**: Core authorization service for permission and resource checks
- Permission checks: `HasPermissionAsync`, `UserHasPermissionAsync`
- Resource access: `HasResourceAccessAsync`, `UserHasResourceAccessAsync`
- Validation: `RequirePermissionAsync`, `RequireResourceAccessAsync`

#### Permission System
- **PermissionConstants**: Policy names and constants
- **RolePermissionMatrix**: Default permission mappings for each role
- Permission categories: User Management, Patient Management, Encounter Management, Orders, Medications, Lab Results, etc.

#### CQRS
- **Queries**: GetAllRoles, GetRoleById, GetAllPermissions
- **Commands**: AssignPermissionsToRole
- **DTOs**: RoleDto, PermissionDto

### Infrastructure Layer (`EMR.Infrastructure`)

#### Authorization Implementation
- **AuthorizationService**: Implements IAuthorizationService with role and resource-level checks
- **PermissionAuthorizationHandler**: ASP.NET Core authorization handler for permission-based policies
- **ResourceAuthorizationHandler**: Handler for attribute-based resource authorization
- **AuthorizationAuditMiddleware**: Logs all authorization decisions for security monitoring

#### Repositories
- **RoleRepository**: CRUD operations for roles with permission loading
- **ResourceAuthorizationRepository**: Manages resource-level permissions

#### Database Configuration
- EF Core configurations for all authorization entities
- Composite indexes for efficient authorization queries
- Soft delete support with audit trails

#### Data Seeding
- **RoleSeeder**: Seeds system roles with default permissions on startup
- Automatic migration and seed on application start

### API Layer (`EMR.Api`)

#### Custom Attributes
- **HasPermissionAttribute**: Apply permission-based authorization to endpoints
  ```csharp
  [HasPermission(Permission.PatientsView)]
  public async Task<IActionResult> GetPatients() { ... }
  ```

- **RequireRoleAttribute**: Apply role-based authorization
  ```csharp
  [RequireRole(UserRole.Admin, UserRole.Doctor)]
  public async Task<IActionResult> GetSensitiveData() { ... }
  ```

#### Controllers
- **RolesController**: Manage roles and permissions (Admin only)
  - `GET /api/roles` - List all roles
  - `GET /api/roles/{id}` - Get role details
  - `GET /api/roles/permissions` - List all permissions
  - `PUT /api/roles/{id}/permissions` - Assign permissions to role

## Permission Matrix

### Admin
- Full access to all system features
- User management, role management, system settings
- All clinical and administrative permissions

### Doctor
- **Patients**: View, Create, Update
- **Encounters**: View, Create, Update, Close
- **Orders**: View, Create, Update, Sign
- **Notes**: View, Create, Update, Sign
- **Medications**: View, Create, Update
- **Lab Results**: View, Verify
- **Vitals/Assessments**: View, Create
- **Scheduling**: View, Create, Update
- **Billing**: View only

### Nurse
- **Patients**: View only
- **Encounters**: View only
- **Orders**: View, Execute
- **Vitals/Assessments**: Full CRUD
- **Notes**: View, Create
- **Medications**: View, Administer
- **Lab Results**: View only
- **Scheduling**: View only

### Staff
- **Patients**: View only
- **Scheduling**: Full CRUD
- **Billing**: View, Create, Update

### Patient
- **Own Records**: View only
- Access limited to their own patient data
- Can view encounters, orders, medications, lab results, imaging related to them
- Can create appointments

## Usage Examples

### Endpoint Protection with Permissions

```csharp
[HttpGet]
[HasPermission(Permission.PatientsView)]
public async Task<IActionResult> GetPatients()
{
    // Only users with PatientsView permission can access
}

[HttpPost]
[HasPermission(Permission.PatientsCreate)]
public async Task<IActionResult> CreatePatient([FromBody] CreatePatientRequest request)
{
    // Only users with PatientsCreate permission can access
}
```

### Role-Based Protection

```csharp
[HttpGet("sensitive")]
[RequireRole(UserRole.Admin, UserRole.Doctor)]
public async Task<IActionResult> GetSensitiveData()
{
    // Only Admins and Doctors can access
}
```

### Programmatic Permission Checks

```csharp
public class PatientService
{
    private readonly IAuthorizationService _authorizationService;

    public async Task<PatientDto> GetPatientAsync(Guid patientId)
    {
        // Check permission
        await _authorizationService.RequirePermissionAsync(Permission.PatientsView);

        // Check resource-level access
        await _authorizationService.RequireResourceAccessAsync(
            ResourceType.Patient,
            patientId,
            Permission.PatientsView);

        // Proceed with operation
    }
}
```

### Conditional Logic Based on Permissions

```csharp
public async Task<IActionResult> GetPatientDetails(Guid patientId)
{
    var patient = await _patientRepository.GetByIdAsync(patientId);

    // Include sensitive data only if user has specific permission
    if (await _authorizationService.HasPermissionAsync(Permission.PatientsViewSensitive))
    {
        patient.SensitiveData = await LoadSensitiveDataAsync(patientId);
    }

    return Ok(patient);
}
```

## Resource-Level Authorization (ABAC)

Resource-level authorization allows fine-grained control where users can only access specific resource instances.

### Example: Doctor assigned to specific patients

```csharp
// Grant doctor access to a specific patient
var authorization = new ResourceAuthorization(
    userId: doctorId,
    resourceType: ResourceType.Patient,
    resourceId: patientId,
    permission: Permission.PatientsView,
    effectiveFrom: DateTime.UtcNow,
    effectiveTo: null, // No expiration
    reason: "Primary care physician assignment",
    grantedBy: "System"
);

await _resourceAuthorizationRepository.AddAsync(authorization);
```

### Checking Resource Access

```csharp
// Check if doctor has access to specific patient
var hasAccess = await _authorizationService.HasResourceAccessAsync(
    ResourceType.Patient,
    patientId,
    Permission.PatientsView);

if (!hasAccess)
{
    return Forbid();
}
```

## Authorization Hierarchy

1. **Role-Based Permissions**: User has permission through their role
2. **Resource-Level Permissions**: User has specific access to resource instances
3. **Admin Override**: Admin role has access to all resources

The authorization check follows this logic:
- If user is Admin → Grant access
- If user has required permission through role → Check resource-level access
- If resource-level authorization exists → Grant access
- If user is Doctor/Nurse with general permission → Grant access (unless explicitly restricted)
- Otherwise → Deny access

## Security Features

### Audit Logging
All authorization decisions are logged via `AuthorizationAuditMiddleware`:
- User ID and roles
- Endpoint accessed
- Success/failure status
- IP address and user agent
- Timestamp and duration

### Temporal Validity
Both `UserRoleAssignment` and `ResourceAuthorization` support:
- `EffectiveFrom`: When the assignment/authorization becomes active
- `EffectiveTo`: When it expires (null = no expiration)
- Active/inactive status based on current time

### Soft Delete
All authorization entities support soft delete:
- Maintains audit trail
- Can be restored if needed
- Excluded from queries by default

## Database Schema

### Roles Table
- Id (PK)
- RoleName (Unique)
- DisplayName
- Description
- IsSystemRole
- Audit fields (CreatedAt, CreatedBy, etc.)

### RolePermissions Table
- Id (PK)
- RoleId (FK)
- Permission
- Audit fields
- Index: (RoleId, Permission)

### UserRoleAssignments Table
- Id (PK)
- UserId (FK)
- Role
- EffectiveFrom
- EffectiveTo
- AssignmentReason
- Audit fields
- Index: (UserId, Role, EffectiveFrom)

### ResourceAuthorizations Table
- Id (PK)
- UserId (FK)
- ResourceType
- ResourceId
- Permission
- EffectiveFrom
- EffectiveTo
- Reason
- Audit fields
- Index: (UserId, ResourceType, ResourceId, Permission)
- Index: (ResourceType, ResourceId)

## Migration and Seeding

On application startup:
1. Pending migrations are automatically applied
2. System roles are seeded if they don't exist
3. Default permissions are assigned to each role

To create a new migration:
```bash
cd src/EMR.Infrastructure
dotnet ef migrations add AddRBACSupport --startup-project ../EMR.Api --context ApplicationDbContext
```

## Best Practices

1. **Use Permission-Based Authorization**: Prefer `HasPermission` over role checks for flexibility
2. **Check Resource Access**: Always verify resource-level access for sensitive operations
3. **Audit Critical Actions**: Log important authorization decisions
4. **Principle of Least Privilege**: Grant minimum required permissions
5. **Time-Bound Access**: Use temporal validity for temporary access grants
6. **Combine Role and Resource Checks**: Use both RBAC and ABAC for comprehensive security

## Testing Authorization

```csharp
[Fact]
public async Task Doctor_CanView_AssignedPatients()
{
    // Arrange
    var doctorId = Guid.NewGuid();
    var patientId = Guid.NewGuid();

    // Grant resource access
    await _resourceAuthRepository.AddAsync(new ResourceAuthorization(
        doctorId, ResourceType.Patient, patientId,
        Permission.PatientsView, DateTime.UtcNow, null, "Test", "System"));

    // Act
    var hasAccess = await _authorizationService.UserHasResourceAccessAsync(
        doctorId, ResourceType.Patient, patientId, Permission.PatientsView);

    // Assert
    Assert.True(hasAccess);
}
```

## Future Enhancements

1. **Dynamic Role Creation**: Allow admins to create custom roles
2. **Permission Groups**: Group related permissions for easier assignment
3. **Delegation**: Allow users to delegate permissions temporarily
4. **Context-Based Access**: Consider time, location, device in authorization decisions
5. **Permission Inheritance**: Hierarchical permission structures
6. **Data Filters**: Automatic query filtering based on user permissions

## Support and Troubleshooting

### Common Issues

**Issue**: User has role but can't access endpoint
- **Solution**: Check if permission is assigned to the role in RolePermissionMatrix
- **Solution**: Verify JWT contains correct role claims

**Issue**: Permission checks failing unexpectedly
- **Solution**: Check authorization audit logs for detailed error information
- **Solution**: Verify user is active (`IsActive = true`)

**Issue**: Resource authorization not working
- **Solution**: Ensure ResourceAuthorization record exists and is active
- **Solution**: Check EffectiveFrom/EffectiveTo dates

### Debug Mode
Enable detailed authorization logging in `appsettings.Development.json`:
```json
{
  "Logging": {
    "LogLevel": {
      "EMR.Infrastructure.Authorization": "Debug"
    }
  }
}
```

## Compliance

This implementation supports:
- **HIPAA**: Audit logging of all access attempts
- **GDPR**: Granular consent and access control
- **SOC 2**: Comprehensive authorization and audit trails
- **Role Separation**: Segregation of duties enforcement
