# RBAC/ABAC Usage Examples

This document provides practical examples of using the RBAC/ABAC system in the EMR application.

## Table of Contents
1. [Controller Examples](#controller-examples)
2. [Service Layer Examples](#service-layer-examples)
3. [Resource Authorization Examples](#resource-authorization-examples)
4. [Role Management Examples](#role-management-examples)
5. [Testing Examples](#testing-examples)

## Controller Examples

### Example 1: Simple Permission Check

```csharp
using EMR.Api.Attributes;
using EMR.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/patients")]
[Authorize]
public class PatientsController : ControllerBase
{
    /// <summary>
    /// Get all patients - requires PatientsView permission
    /// </summary>
    [HttpGet]
    [HasPermission(Permission.PatientsView)]
    public async Task<IActionResult> GetAllPatients()
    {
        // Only users with PatientsView permission can access
        var patients = await _patientService.GetAllAsync();
        return Ok(patients);
    }

    /// <summary>
    /// Create a new patient - requires PatientsCreate permission
    /// </summary>
    [HttpPost]
    [HasPermission(Permission.PatientsCreate)]
    public async Task<IActionResult> CreatePatient([FromBody] CreatePatientRequest request)
    {
        // Only users with PatientsCreate permission can access
        var patient = await _patientService.CreateAsync(request);
        return CreatedAtAction(nameof(GetPatientById), new { id = patient.Id }, patient);
    }

    /// <summary>
    /// Delete a patient - requires PatientsDelete permission
    /// </summary>
    [HttpDelete("{id}")]
    [HasPermission(Permission.PatientsDelete)]
    public async Task<IActionResult> DeletePatient(Guid id)
    {
        // Only users with PatientsDelete permission (typically Admin only)
        await _patientService.DeleteAsync(id);
        return NoContent();
    }
}
```

### Example 2: Role-Based Endpoint

```csharp
/// <summary>
/// Get sensitive clinical data - restricted to medical staff
/// </summary>
[HttpGet("sensitive")]
[RequireRole(UserRole.Admin, UserRole.Doctor)]
public async Task<IActionResult> GetSensitiveClinicalData()
{
    // Only Admins and Doctors can access
    var data = await _clinicalService.GetSensitiveDataAsync();
    return Ok(data);
}
```

### Example 3: Multiple Permission Attributes

```csharp
/// <summary>
/// Sign a clinical note - requires both viewing and signing permissions
/// </summary>
[HttpPost("{id}/sign")]
[HasPermission(Permission.NotesView)]
[HasPermission(Permission.NotesSign)]
public async Task<IActionResult> SignClinicalNote(Guid id)
{
    // User must have both permissions
    await _noteService.SignNoteAsync(id);
    return Ok(new { message = "Note signed successfully" });
}
```

## Service Layer Examples

### Example 4: Programmatic Permission Check

```csharp
public class PatientService
{
    private readonly IAuthorizationService _authorizationService;
    private readonly IPatientRepository _patientRepository;
    private readonly ILogger<PatientService> _logger;

    public async Task<PatientDto> GetPatientAsync(Guid patientId)
    {
        // Check if user has permission
        await _authorizationService.RequirePermissionAsync(Permission.PatientsView);

        _logger.LogInformation("User authorized to view patient {PatientId}", patientId);

        var patient = await _patientRepository.GetByIdAsync(patientId);
        if (patient == null)
        {
            throw new EntityNotFoundException($"Patient {patientId} not found");
        }

        return MapToDto(patient);
    }

    public async Task UpdatePatientAsync(Guid patientId, UpdatePatientRequest request)
    {
        // Require update permission
        await _authorizationService.RequirePermissionAsync(Permission.PatientsUpdate);

        // Also check resource-level access for extra security
        await _authorizationService.RequireResourceAccessAsync(
            ResourceType.Patient,
            patientId,
            Permission.PatientsUpdate);

        var patient = await _patientRepository.GetByIdAsync(patientId);
        patient.Update(request.FirstName, request.LastName, /* ... */);

        _patientRepository.Update(patient);
        await _unitOfWork.SaveChangesAsync();
    }
}
```

### Example 5: Conditional Logic Based on Permissions

```csharp
public class EncounterService
{
    public async Task<EncounterDto> GetEncounterDetailsAsync(Guid encounterId)
    {
        await _authorizationService.RequirePermissionAsync(Permission.EncountersView);

        var encounter = await _encounterRepository.GetByIdAsync(encounterId);
        var dto = MapToDto(encounter);

        // Include sensitive billing information only if user has billing view permission
        if (await _authorizationService.HasPermissionAsync(Permission.BillingView))
        {
            dto.BillingDetails = await _billingService.GetEncounterBillingAsync(encounterId);
        }

        // Include confidential notes only for doctors and admins
        if (_authorizationService.HasAnyRole(UserRole.Admin, UserRole.Doctor))
        {
            dto.ConfidentialNotes = await _noteService.GetConfidentialNotesAsync(encounterId);
        }

        return dto;
    }
}
```

### Example 6: Safe Permission Check (Non-Throwing)

```csharp
public async Task<OrderDto> GetOrderWithDetailsAsync(Guid orderId)
{
    // Always require basic view permission
    await _authorizationService.RequirePermissionAsync(Permission.OrdersView);

    var order = await _orderRepository.GetByIdAsync(orderId);
    var dto = MapToDto(order);

    // Safely check for additional permissions without throwing
    var canSign = await _authorizationService.HasPermissionAsync(Permission.OrdersSign);
    dto.CanUserSign = canSign;

    var canExecute = await _authorizationService.HasPermissionAsync(Permission.OrdersExecute);
    dto.CanUserExecute = canExecute;

    return dto;
}
```

## Resource Authorization Examples

### Example 7: Grant Resource Access

```csharp
public class PatientAssignmentService
{
    private readonly IResourceAuthorizationRepository _resourceAuthRepository;

    /// <summary>
    /// Assign a doctor to a patient
    /// </summary>
    public async Task AssignDoctorToPatientAsync(Guid doctorId, Guid patientId)
    {
        // Only admins can assign doctors to patients
        await _authorizationService.RequirePermissionAsync(Permission.UsersManageRoles);

        // Create resource authorization
        var authorization = new ResourceAuthorization(
            userId: doctorId,
            resourceType: ResourceType.Patient,
            resourceId: patientId,
            permission: Permission.PatientsView,
            effectiveFrom: DateTime.UtcNow,
            effectiveTo: null, // No expiration
            reason: "Assigned as primary care physician",
            grantedBy: _currentUserService.UserId.ToString()
        );

        await _resourceAuthRepository.AddAsync(authorization);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation(
            "Assigned doctor {DoctorId} to patient {PatientId}",
            doctorId,
            patientId);
    }

    /// <summary>
    /// Grant temporary access to a patient record
    /// </summary>
    public async Task GrantTemporaryAccessAsync(
        Guid userId,
        Guid patientId,
        TimeSpan duration)
    {
        var authorization = new ResourceAuthorization(
            userId: userId,
            resourceType: ResourceType.Patient,
            resourceId: patientId,
            permission: Permission.PatientsView,
            effectiveFrom: DateTime.UtcNow,
            effectiveTo: DateTime.UtcNow.Add(duration),
            reason: $"Temporary access granted for {duration.TotalHours} hours",
            grantedBy: _currentUserService.UserId.ToString()
        );

        await _resourceAuthRepository.AddAsync(authorization);
        await _unitOfWork.SaveChangesAsync();
    }

    /// <summary>
    /// Revoke doctor's access to a patient
    /// </summary>
    public async Task RevokeDoctorAccessAsync(Guid doctorId, Guid patientId)
    {
        await _authorizationService.RequirePermissionAsync(Permission.UsersManageRoles);

        await _resourceAuthRepository.RevokeResourceAccessAsync(
            userId: doctorId,
            resourceType: ResourceType.Patient,
            resourceId: patientId,
            revokedBy: _currentUserService.UserId.ToString());

        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation(
            "Revoked doctor {DoctorId} access to patient {PatientId}",
            doctorId,
            patientId);
    }
}
```

### Example 8: Check Resource Access

```csharp
public class OrderService
{
    public async Task SignOrderAsync(Guid orderId)
    {
        // First, check role-based permission
        await _authorizationService.RequirePermissionAsync(Permission.OrdersSign);

        var order = await _orderRepository.GetByIdAsync(orderId);

        // Check resource-level access to the patient
        await _authorizationService.RequireResourceAccessAsync(
            ResourceType.Patient,
            order.PatientId,
            Permission.PatientsView);

        // Sign the order
        order.Sign(_currentUserService.UserId.Value);
        _orderRepository.Update(order);
        await _unitOfWork.SaveChangesAsync();
    }
}
```

### Example 9: Filter Results by Resource Access

```csharp
public class PatientQueryService
{
    public async Task<IEnumerable<PatientDto>> GetAccessiblePatientsAsync()
    {
        await _authorizationService.RequirePermissionAsync(Permission.PatientsView);

        // For non-admin users, filter by resource authorization
        if (!_authorizationService.IsAdmin())
        {
            var authorizedPatientIds = await _authorizationService
                .GetAuthorizedResourceIdsAsync(
                    ResourceType.Patient,
                    Permission.PatientsView);

            if (authorizedPatientIds.Any())
            {
                // User has specific patient assignments
                var patients = await _patientRepository
                    .GetByIdsAsync(authorizedPatientIds);
                return patients.Select(MapToDto);
            }
            else
            {
                // No specific assignments, return empty (or all for medical staff)
                if (_authorizationService.HasAnyRole(UserRole.Doctor, UserRole.Nurse))
                {
                    // Medical staff can see all patients by default
                    var allPatients = await _patientRepository.GetAllAsync();
                    return allPatients.Select(MapToDto);
                }

                return Enumerable.Empty<PatientDto>();
            }
        }

        // Admin can see all patients
        var patients = await _patientRepository.GetAllAsync();
        return patients.Select(MapToDto);
    }
}
```

## Role Management Examples

### Example 10: Assign Permissions to a Custom Role

```csharp
[HttpPut("roles/{roleId}/permissions")]
[HasPermission(Permission.PermissionsAssign)]
public async Task<IActionResult> AssignPermissions(Guid roleId)
{
    var permissions = new[]
    {
        Permission.PatientsView,
        Permission.EncountersView,
        Permission.OrdersView,
        Permission.LabResultsView
    };

    var command = new AssignPermissionsToRoleCommand
    {
        RoleId = roleId,
        Permissions = permissions
    };

    var result = await _mediator.Send(command);

    if (!result.IsSuccess)
    {
        return BadRequest(result.ErrorMessage);
    }

    return Ok(new { message = "Permissions assigned successfully" });
}
```

### Example 11: Get User's Effective Permissions

```csharp
[HttpGet("me/permissions")]
public async Task<IActionResult> GetMyPermissions()
{
    var permissions = await _authorizationService.GetUserPermissionsAsync();

    return Ok(new
    {
        userId = _currentUserService.UserId,
        roles = _currentUserService.UserRoles,
        permissions = permissions.Select(p => new
        {
            permission = p.ToString(),
            description = GetPermissionDescription(p)
        })
    });
}
```

## Testing Examples

### Example 12: Unit Test for Permission Checking

```csharp
public class AuthorizationServiceTests
{
    [Fact]
    public async Task Doctor_ShouldHave_PatientsViewPermission()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User(
            email: "doctor@emr.com",
            firstName: "John",
            lastName: "Doe",
            azureAdB2CId: "azure-123",
            roles: new[] { UserRole.Doctor },
            createdBy: "System"
        );

        _userRepository.Setup(x => x.GetByIdAsync(userId, default))
            .ReturnsAsync(user);

        // Act
        var hasPermission = await _authorizationService
            .UserHasPermissionAsync(userId, Permission.PatientsView);

        // Assert
        Assert.True(hasPermission);
    }

    [Fact]
    public async Task Nurse_ShouldNotHave_OrdersSignPermission()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User(
            email: "nurse@emr.com",
            firstName: "Jane",
            lastName: "Smith",
            azureAdB2CId: "azure-456",
            roles: new[] { UserRole.Nurse },
            createdBy: "System"
        );

        _userRepository.Setup(x => x.GetByIdAsync(userId, default))
            .ReturnsAsync(user);

        // Act
        var hasPermission = await _authorizationService
            .UserHasPermissionAsync(userId, Permission.OrdersSign);

        // Assert
        Assert.False(hasPermission);
    }
}
```

### Example 13: Integration Test for Resource Authorization

```csharp
public class ResourceAuthorizationIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task Doctor_CanAccess_AssignedPatient()
    {
        // Arrange
        var doctorId = await CreateDoctorAsync();
        var patientId = await CreatePatientAsync();
        await AssignDoctorToPatientAsync(doctorId, patientId);

        // Act
        var client = _factory.CreateClient();
        await AuthenticateAsDoctorAsync(client, doctorId);
        var response = await client.GetAsync($"/api/patients/{patientId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Doctor_CannotAccess_UnassignedPatient()
    {
        // Arrange
        var doctorId = await CreateDoctorAsync();
        var patientId = await CreatePatientAsync();
        // Note: NOT assigning doctor to patient

        // Act
        var client = _factory.CreateClient();
        await AuthenticateAsDoctorAsync(client, doctorId);
        var response = await client.GetAsync($"/api/patients/{patientId}");

        // Assert - Should be forbidden if resource authorization is strictly enforced
        // Or OK if medical staff has general access
        Assert.True(
            response.StatusCode == HttpStatusCode.OK ||
            response.StatusCode == HttpStatusCode.Forbidden);
    }
}
```

## Advanced Patterns

### Example 14: Authorization Decorator Pattern

```csharp
public interface IPatientService
{
    Task<PatientDto> GetPatientAsync(Guid patientId);
}

public class PatientServiceAuthorizationDecorator : IPatientService
{
    private readonly IPatientService _inner;
    private readonly IAuthorizationService _authorizationService;

    public PatientServiceAuthorizationDecorator(
        IPatientService inner,
        IAuthorizationService authorizationService)
    {
        _inner = inner;
        _authorizationService = authorizationService;
    }

    public async Task<PatientDto> GetPatientAsync(Guid patientId)
    {
        // Enforce authorization before delegating to inner service
        await _authorizationService.RequirePermissionAsync(Permission.PatientsView);
        await _authorizationService.RequireResourceAccessAsync(
            ResourceType.Patient,
            patientId,
            Permission.PatientsView);

        return await _inner.GetPatientAsync(patientId);
    }
}
```

### Example 15: Authorization Policy Builder

```csharp
public class AuthorizationPolicyBuilder
{
    private readonly List<Permission> _requiredPermissions = new();
    private readonly List<UserRole> _requiredRoles = new();

    public AuthorizationPolicyBuilder RequirePermission(Permission permission)
    {
        _requiredPermissions.Add(permission);
        return this;
    }

    public AuthorizationPolicyBuilder RequireRole(UserRole role)
    {
        _requiredRoles.Add(role);
        return this;
    }

    public async Task<bool> EvaluateAsync(IAuthorizationService authService)
    {
        // Check all required roles
        foreach (var role in _requiredRoles)
        {
            if (!authService.HasAnyRole(role))
            {
                return false;
            }
        }

        // Check all required permissions
        foreach (var permission in _requiredPermissions)
        {
            if (!await authService.HasPermissionAsync(permission))
            {
                return false;
            }
        }

        return true;
    }
}

// Usage
var policy = new AuthorizationPolicyBuilder()
    .RequireRole(UserRole.Doctor)
    .RequirePermission(Permission.OrdersSign)
    .RequirePermission(Permission.MedicationsCreate);

if (await policy.EvaluateAsync(_authorizationService))
{
    // User authorized
}
```

## Best Practices Summary

1. **Always use the most specific permission**: Don't use broad permissions when narrow ones exist
2. **Combine role and resource checks**: Use RBAC for general access, ABAC for specific resources
3. **Log authorization failures**: Always log when access is denied for security monitoring
4. **Use non-throwing checks for UI logic**: Use `HasPermissionAsync` for enabling/disabling UI elements
5. **Use throwing checks for operations**: Use `RequirePermissionAsync` to fail fast on unauthorized operations
6. **Test authorization thoroughly**: Include positive and negative test cases
7. **Document permission requirements**: Clearly document what permissions each endpoint/operation requires
