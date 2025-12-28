# RBAC/ABAC Quick Reference Guide

## Quick Start

### Protect an Endpoint

```csharp
using EMR.Api.Attributes;
using EMR.Domain.Enums;

// Permission-based protection
[HasPermission(Permission.PatientsView)]
public async Task<IActionResult> GetPatients() { }

// Role-based protection
[RequireRole(UserRole.Admin, UserRole.Doctor)]
public async Task<IActionResult> GetSensitiveData() { }

// Multiple permissions (AND logic)
[HasPermission(Permission.NotesView)]
[HasPermission(Permission.NotesSign)]
public async Task<IActionResult> SignNote(Guid id) { }
```

### Check Permissions in Code

```csharp
// Check permission (returns bool)
var hasPermission = await _authorizationService.HasPermissionAsync(Permission.PatientsView);

// Require permission (throws if denied)
await _authorizationService.RequirePermissionAsync(Permission.PatientsCreate);

// Check resource access
var hasAccess = await _authorizationService.HasResourceAccessAsync(
    ResourceType.Patient, patientId, Permission.PatientsView);

// Require resource access (throws if denied)
await _authorizationService.RequireResourceAccessAsync(
    ResourceType.Patient, patientId, Permission.PatientsUpdate);
```

### Get User Information

```csharp
// Get current user ID
var userId = _authorizationService.GetCurrentUserId();

// Get current user roles
var roles = _authorizationService.GetCurrentUserRoles();

// Check if user has specific role
var isAdmin = _authorizationService.IsAdmin();
var isDoctor = _authorizationService.HasAnyRole(UserRole.Doctor);

// Get all user permissions
var permissions = await _authorizationService.GetUserPermissionsAsync();
```

## Permission Categories

### User Management (5)
- `Permission.UsersView` - View user accounts
- `Permission.UsersCreate` - Create new users
- `Permission.UsersUpdate` - Update user info
- `Permission.UsersDelete` - Delete users
- `Permission.UsersManageRoles` - Assign roles

### Patient Management (5)
- `Permission.PatientsView` - View all patients
- `Permission.PatientsCreate` - Create patients
- `Permission.PatientsUpdate` - Update patient info
- `Permission.PatientsDelete` - Delete patients
- `Permission.PatientsViewOwn` - View own record (Patient role)

### Encounter Management (5)
- `Permission.EncountersView` - View encounters
- `Permission.EncountersCreate` - Create encounters
- `Permission.EncountersUpdate` - Update encounters
- `Permission.EncountersDelete` - Delete encounters
- `Permission.EncountersClose` - Close/finalize encounters

### Order Management (6)
- `Permission.OrdersView` - View orders
- `Permission.OrdersCreate` - Create orders
- `Permission.OrdersUpdate` - Update orders
- `Permission.OrdersDelete` - Delete orders
- `Permission.OrdersSign` - Sign/approve orders
- `Permission.OrdersExecute` - Execute/fulfill orders

### Clinical Notes (5)
- `Permission.NotesView` - View notes
- `Permission.NotesCreate` - Create notes
- `Permission.NotesUpdate` - Update notes
- `Permission.NotesDelete` - Delete notes
- `Permission.NotesSign` - Sign notes

### Medications (6)
- `Permission.MedicationsView` - View medications
- `Permission.MedicationsCreate` - Prescribe medications
- `Permission.MedicationsUpdate` - Update prescriptions
- `Permission.MedicationsDelete` - Delete prescriptions
- `Permission.MedicationsDispense` - Dispense medications
- `Permission.MedicationsAdminister` - Administer to patients

### Lab Results (5)
- `Permission.LabResultsView` - View lab results
- `Permission.LabResultsCreate` - Enter lab results
- `Permission.LabResultsUpdate` - Update results
- `Permission.LabResultsDelete` - Delete results
- `Permission.LabResultsVerify` - Verify/sign results

### Other Categories
- **Vitals & Assessments** (8): Full CRUD for each
- **Imaging** (4): View, Create, Update, Delete
- **Scheduling** (4): Full CRUD
- **Billing** (4): Full CRUD
- **Reports** (3): View, Generate, Export
- **System Admin** (3): Settings, Audit Logs
- **Role Management** (6): Full CRUD + Assign

## Role Capabilities

### Admin
✓ Everything (all 89 permissions)

### Doctor
✓ Patients: View, Create, Update
✓ Encounters: View, Create, Update, Close
✓ Orders: View, Create, Update, Sign
✓ Notes: View, Create, Update, Sign
✓ Medications: View, Create, Update
✓ Lab Results: View, Verify
✓ Vitals/Assessments: View, Create
✓ Scheduling: View, Create, Update
✓ Billing: View only

### Nurse
✓ Patients: View only
✓ Encounters: View only
✓ Orders: View, Execute
✓ Vitals/Assessments: Full CRUD
✓ Notes: View, Create
✓ Medications: View, Administer
✓ Lab Results: View only

### Staff
✓ Patients: View only
✓ Scheduling: Full CRUD
✓ Billing: View, Create, Update

### Patient
✓ Own records: View only
✓ Own appointments: View, Create

## Common Patterns

### Controller Authorization
```csharp
[ApiController]
[Route("api/patients")]
[Authorize] // Require authentication
public class PatientsController : ControllerBase
{
    [HttpGet]
    [HasPermission(Permission.PatientsView)]
    public async Task<IActionResult> GetAll() { }

    [HttpPost]
    [HasPermission(Permission.PatientsCreate)]
    public async Task<IActionResult> Create([FromBody] CreatePatientRequest req) { }

    [HttpPut("{id}")]
    [HasPermission(Permission.PatientsUpdate)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePatientRequest req) { }

    [HttpDelete("{id}")]
    [HasPermission(Permission.PatientsDelete)]
    public async Task<IActionResult> Delete(Guid id) { }
}
```

### Service Authorization
```csharp
public class PatientService
{
    public async Task<PatientDto> GetPatientAsync(Guid patientId)
    {
        // Require permission
        await _authorizationService.RequirePermissionAsync(Permission.PatientsView);

        // Check resource access for ABAC
        await _authorizationService.RequireResourceAccessAsync(
            ResourceType.Patient, patientId, Permission.PatientsView);

        return await _patientRepository.GetByIdAsync(patientId);
    }

    public async Task UpdatePatientAsync(Guid patientId, UpdatePatientRequest request)
    {
        await _authorizationService.RequirePermissionAsync(Permission.PatientsUpdate);
        await _authorizationService.RequireResourceAccessAsync(
            ResourceType.Patient, patientId, Permission.PatientsUpdate);

        var patient = await _patientRepository.GetByIdAsync(patientId);
        patient.Update(request);
        await _unitOfWork.SaveChangesAsync();
    }
}
```

### Conditional UI Logic
```csharp
public async Task<PatientDetailsDto> GetPatientDetailsAsync(Guid patientId)
{
    await _authorizationService.RequirePermissionAsync(Permission.PatientsView);

    var patient = await _patientRepository.GetByIdAsync(patientId);
    var dto = MapToDto(patient);

    // Include sensitive data only if authorized
    if (await _authorizationService.HasPermissionAsync(Permission.BillingView))
    {
        dto.BillingInfo = await _billingService.GetPatientBillingAsync(patientId);
    }

    // Add action permissions for UI
    dto.CanEdit = await _authorizationService.HasPermissionAsync(Permission.PatientsUpdate);
    dto.CanDelete = await _authorizationService.HasPermissionAsync(Permission.PatientsDelete);

    return dto;
}
```

## Resource Authorization (ABAC)

### Grant Access to Specific Resource
```csharp
// Assign doctor to patient
var authorization = new ResourceAuthorization(
    userId: doctorId,
    resourceType: ResourceType.Patient,
    resourceId: patientId,
    permission: Permission.PatientsView,
    effectiveFrom: DateTime.UtcNow,
    effectiveTo: null, // No expiration
    reason: "Primary care physician",
    grantedBy: currentUserId.ToString()
);
await _resourceAuthRepository.AddAsync(authorization);
await _unitOfWork.SaveChangesAsync();
```

### Grant Temporary Access
```csharp
// Grant 24-hour access
var authorization = new ResourceAuthorization(
    userId: consultantId,
    resourceType: ResourceType.Patient,
    resourceId: patientId,
    permission: Permission.PatientsView,
    effectiveFrom: DateTime.UtcNow,
    effectiveTo: DateTime.UtcNow.AddHours(24),
    reason: "Consultation requested",
    grantedBy: currentUserId.ToString()
);
await _resourceAuthRepository.AddAsync(authorization);
```

### Revoke Access
```csharp
await _resourceAuthRepository.RevokeResourceAccessAsync(
    userId: userId,
    resourceType: ResourceType.Patient,
    resourceId: patientId,
    revokedBy: currentUserId.ToString()
);
await _unitOfWork.SaveChangesAsync();
```

### Check Resource Access
```csharp
var hasAccess = await _authorizationService.HasResourceAccessAsync(
    ResourceType.Patient,
    patientId,
    Permission.PatientsView
);

if (!hasAccess)
{
    return Forbid();
}
```

## API Endpoints

### Role Management (Admin Only)

```http
GET /api/roles
Authorization: Bearer {token}
Response: Array of RoleDto

GET /api/roles/{id}
Authorization: Bearer {token}
Response: RoleDto

GET /api/roles/permissions
Authorization: Bearer {token}
Response: Array of PermissionDto

PUT /api/roles/{id}/permissions
Authorization: Bearer {token}
Content-Type: application/json
Body: { "permissions": [1, 2, 3, ...] }
Response: Success message
```

## Authorization Flow

```
1. User makes request with JWT token
   ↓
2. ASP.NET Core validates JWT (Authentication)
   ↓
3. Authorization Policy is evaluated
   ↓
4. PermissionAuthorizationHandler checks user's permissions
   ↓
5. AuthorizationService queries RolePermissionMatrix
   ↓
6. If Admin → Grant access
   If has permission via role → Grant access
   If has resource-level access → Grant access
   Otherwise → Deny access
   ↓
7. Authorization decision is logged via middleware
   ↓
8. Request proceeds (if authorized) or returns 403 Forbidden
```

## Testing Checklist

- [ ] Admin can access all endpoints
- [ ] Doctor can access clinical endpoints
- [ ] Nurse has limited access (no signing)
- [ ] Staff can manage scheduling/billing only
- [ ] Patient can only view own records
- [ ] Unauthorized requests return 403
- [ ] Authorization is logged in audit trail
- [ ] Resource authorization works correctly
- [ ] Temporal access expires properly

## Troubleshooting

**403 Forbidden when user should have access:**
1. Check JWT contains correct role claims
2. Verify user has expected roles in database
3. Check RolePermissionMatrix for permission mapping
4. Review authorization audit logs

**Permission check always fails:**
1. Verify user is active (`IsActive = true`)
2. Check ICurrentUserService is returning correct user ID
3. Ensure AuthorizationService is registered in DI

**Resource authorization not working:**
1. Verify ResourceAuthorization record exists
2. Check EffectiveFrom/EffectiveTo dates
3. Ensure record is not soft-deleted
4. Review repository query logic

## Performance Tips

1. **Cache permission checks**: Consider caching user permissions for request duration
2. **Eager load permissions**: Use `Include()` when loading roles
3. **Use indexes**: Database has composite indexes for efficient queries
4. **Avoid N+1**: Batch permission checks when possible

## Security Best Practices

1. ✓ Always use most specific permission available
2. ✓ Combine RBAC and ABAC for sensitive operations
3. ✓ Use throwing methods (`Require*`) for critical paths
4. ✓ Use non-throwing methods (`Has*`) for UI logic
5. ✓ Log all authorization failures
6. ✓ Grant minimum required permissions (least privilege)
7. ✓ Use temporal validity for temporary access
8. ✓ Regular audit permission assignments

## Need Help?

- **Documentation**: `docs/RBAC_IMPLEMENTATION.md`
- **Examples**: `docs/RBAC_USAGE_EXAMPLES.md`
- **Logs**: Check `logs/emr-*.log` for authorization events
- **Audit Trail**: Query `AuditLogs` table for access history
