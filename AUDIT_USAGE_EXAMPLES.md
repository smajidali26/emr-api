# HIPAA Audit Logging - Usage Examples

This document provides practical examples of how to use the audit logging system in the EMR application.

## Table of Contents

1. [Automatic Auditing with MediatR](#1-automatic-auditing-with-mediatr)
2. [Controller Action Filters](#2-controller-action-filters)
3. [Manual Audit Logging](#3-manual-audit-logging)
4. [Entity Change Tracking](#4-entity-change-tracking)
5. [Querying Audit Logs](#5-querying-audit-logs)

---

## 1. Automatic Auditing with MediatR

The easiest way to add audit logging is using the `[Auditable]` attribute on MediatR commands and queries.

### Example: Query with PHI Access

```csharp
using EMR.Application.Common.Abstractions;
using EMR.Application.Common.Attributes;
using EMR.Application.Common.DTOs;
using EMR.Application.Features.Patients.DTOs;
using EMR.Domain.Enums;

namespace EMR.Application.Features.Patients.Queries.GetPatientById;

/// <summary>
/// Query to retrieve a patient by ID
/// Marked as auditable because it accesses PHI
/// </summary>
[Auditable(
    AuditEventType.View,
    "Patient",
    "Viewed patient record",
    AccessesPhi = true)]
public sealed class GetPatientByIdQuery : IQuery<ResultDto<PatientDto>>
{
    public Guid Id { get; set; }
}
```

**What happens:**
- `AuditLoggingBehaviour` intercepts the query execution
- Automatically logs: User, Timestamp, ResourceType (Patient), ResourceId (extracted from Id property)
- Logs to both database and Serilog
- PHI access is specially marked for compliance reporting

### Example: Command with Data Modification

```csharp
using EMR.Application.Common.Abstractions;
using EMR.Application.Common.Attributes;
using EMR.Application.Common.DTOs;
using EMR.Domain.Enums;

namespace EMR.Application.Features.Patients.Commands.UpdatePatient;

[Auditable(
    AuditEventType.Update,
    "Patient",
    "Updated patient record",
    AccessesPhi = true)]
public sealed class UpdatePatientCommand : ICommand<ResultDto<bool>>
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }
    // ... other properties
}
```

---

## 2. Controller Action Filters

For direct controller actions (bypassing MediatR), use action filter attributes.

### Example: Patient Controller

```csharp
using EMR.Api.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EMR.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PatientsController : ControllerBase
{
    private readonly IMediator _mediator;

    public PatientsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get patient by ID
    /// </summary>
    [HttpGet("{id}")]
    [PatientAccessLogging] // Specialized filter for patient access
    public async Task<IActionResult> GetPatient(Guid id)
    {
        var query = new GetPatientByIdQuery { Id = id };
        var result = await _mediator.Send(query);

        if (!result.IsSuccess)
            return NotFound(result);

        return Ok(result);
    }

    /// <summary>
    /// Search patients (list view - less restrictive logging)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> SearchPatients([FromQuery] SearchPatientsQuery query)
    {
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Download patient medical records
    /// </summary>
    [HttpGet("{id}/download")]
    [PhiAccessLogging(
        ResourceType = "Patient",
        Action = "Downloaded patient medical records",
        ResourceIdParameter = "id")]
    public async Task<IActionResult> DownloadMedicalRecords(Guid id)
    {
        // Implementation...
        return File(fileBytes, "application/pdf", "medical_records.pdf");
    }
}
```

### Example: Encounter Controller

```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class EncountersController : ControllerBase
{
    [HttpGet("{id}")]
    [EncounterAccessLogging] // Specialized filter for encounters
    public async Task<IActionResult> GetEncounter(Guid id)
    {
        // Implementation...
    }

    [HttpPost]
    [PhiAccessLogging(
        ResourceType = "Encounter",
        Action = "Created new encounter",
        ResourceIdParameter = "id")]
    public async Task<IActionResult> CreateEncounter([FromBody] CreateEncounterCommand command)
    {
        // Implementation...
    }
}
```

---

## 3. Manual Audit Logging

For custom scenarios, inject `IAuditService` directly.

### Example: Export Service

```csharp
using EMR.Application.Common.Interfaces;
using EMR.Domain.Enums;

namespace EMR.Application.Features.Export;

public class PatientExportService
{
    private readonly IAuditService _auditService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IPatientRepository _patientRepository;

    public PatientExportService(
        IAuditService auditService,
        ICurrentUserService currentUserService,
        IPatientRepository patientRepository)
    {
        _auditService = auditService;
        _currentUserService = currentUserService;
        _patientRepository = patientRepository;
    }

    public async Task<byte[]> ExportPatientToPdfAsync(Guid patientId)
    {
        var userId = _currentUserService.GetUserId()?.ToString() ?? "Unknown";
        var ipAddress = _currentUserService.GetIpAddress();
        var userAgent = _currentUserService.GetUserAgent();

        try
        {
            // Generate PDF
            var patient = await _patientRepository.GetByIdAsync(patientId);
            var pdfBytes = GeneratePdf(patient);

            // Log successful export
            await _auditService.LogExportOperationAsync(
                eventType: AuditEventType.Export,
                userId: userId,
                resourceType: "Patient",
                resourceId: patientId.ToString(),
                format: "PDF",
                ipAddress: ipAddress,
                userAgent: userAgent);

            return pdfBytes;
        }
        catch (Exception ex)
        {
            // Log failed export
            await _auditService.LogAccessDeniedAsync(
                userId: userId,
                resourceType: "Patient",
                resourceId: patientId.ToString(),
                action: "Export to PDF",
                reason: ex.Message,
                ipAddress: ipAddress,
                userAgent: userAgent);

            throw;
        }
    }

    private byte[] GeneratePdf(Patient patient)
    {
        // PDF generation logic...
        return Array.Empty<byte>();
    }
}
```

### Example: Authentication Service

```csharp
public class AuthenticationService
{
    private readonly IAuditService _auditService;

    public async Task<LoginResult> LoginAsync(string email, string password, string ipAddress)
    {
        try
        {
            // Authenticate user
            var user = await ValidateCredentials(email, password);

            if (user == null)
            {
                // Log failed login
                await _auditService.LogAuthenticationAsync(
                    eventType: AuditEventType.FailedLogin,
                    userId: email,
                    username: email,
                    success: false,
                    ipAddress: ipAddress,
                    errorMessage: "Invalid credentials");

                return LoginResult.Failed("Invalid credentials");
            }

            // Log successful login
            await _auditService.LogAuthenticationAsync(
                eventType: AuditEventType.Login,
                userId: user.Id.ToString(),
                username: user.Email,
                success: true,
                ipAddress: ipAddress);

            return LoginResult.Success(user);
        }
        catch (Exception ex)
        {
            // Log exception
            await _auditService.LogAuthenticationAsync(
                eventType: AuditEventType.FailedLogin,
                userId: email,
                username: email,
                success: false,
                ipAddress: ipAddress,
                errorMessage: ex.Message);

            throw;
        }
    }

    public async Task LogoutAsync(string userId, string ipAddress)
    {
        await _auditService.LogAuthenticationAsync(
            eventType: AuditEventType.Logout,
            userId: userId,
            username: string.Empty,
            success: true,
            ipAddress: ipAddress);
    }
}
```

---

## 4. Entity Change Tracking

Entities that inherit from `AuditableEntity` get automatic change tracking.

### Example: Patient Entity

```csharp
using EMR.Domain.Common;

namespace EMR.Domain.Entities;

public class Patient : AuditableEntity
{
    // Override to indicate this entity contains PHI
    public override bool ContainsPhi => true;

    // Override for custom resource type
    public override string AuditResourceType => "Patient";

    // Override for custom audit description (without PHI)
    public override string GetAuditDescription()
    {
        return $"Patient record (MRN: {MedicalRecordNumber})";
    }

    // Override to exclude sensitive fields from change tracking
    public override IEnumerable<string> GetAuditExcludedProperties()
    {
        return base.GetAuditExcludedProperties()
            .Concat(new[] { "PasswordHash", "SecurityStamp" });
    }

    // Entity properties
    public string MedicalRecordNumber { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }
    // ... other properties
}
```

**What happens:**
- When Patient is created, modified, or deleted via EF Core
- `AuditInterceptor` automatically creates audit log
- PHI fields (FirstName, LastName, DOB) are masked: "J***n", "D***e", "1990"
- Non-PHI metadata is logged: MRN, Id, Timestamp

### Example: Encounter Entity

```csharp
public class Encounter : AuditableEntity
{
    public override bool ContainsPhi => true;
    public override string AuditResourceType => "Encounter";

    public override string GetAuditDescription()
    {
        return $"Encounter (Date: {EncounterDate:yyyy-MM-dd}, Type: {EncounterType})";
    }

    public Guid PatientId { get; set; }
    public DateTime EncounterDate { get; set; }
    public string EncounterType { get; set; } = string.Empty;
    public string ChiefComplaint { get; set; } = string.Empty;
    // ... other properties
}
```

---

## 5. Querying Audit Logs

### Example: Admin Dashboard

```csharp
using EMR.Application.Features.Audit.DTOs;
using EMR.Application.Features.Audit.Queries.GetAuditLogs;

namespace EMR.Application.Features.Admin;

public class AuditDashboardService
{
    private readonly IMediator _mediator;

    public AuditDashboardService(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get all PHI access in the last 24 hours
    /// </summary>
    public async Task<PagedResultDto<AuditLogDto>> GetRecentPhiAccessAsync()
    {
        var query = new GetAuditLogsQuery
        {
            QueryParams = new AuditLogQueryDto
            {
                EventType = AuditEventType.View,
                FromDate = DateTime.UtcNow.AddHours(-24),
                ToDate = DateTime.UtcNow,
                PageNumber = 1,
                PageSize = 100,
                SortBy = "Timestamp",
                SortDescending = true
            }
        };

        return await _mediator.Send(query);
    }

    /// <summary>
    /// Get all failed access attempts for security monitoring
    /// </summary>
    public async Task<PagedResultDto<AuditLogDto>> GetFailedAccessAttemptsAsync(DateTime fromDate)
    {
        var query = new GetAuditLogsQuery
        {
            QueryParams = new AuditLogQueryDto
            {
                Success = false,
                FromDate = fromDate,
                ToDate = DateTime.UtcNow,
                PageNumber = 1,
                PageSize = 50,
                SortBy = "Timestamp",
                SortDescending = true
            }
        };

        return await _mediator.Send(query);
    }

    /// <summary>
    /// Get audit trail for specific patient
    /// </summary>
    public async Task<IEnumerable<AuditLogDto>> GetPatientAuditTrailAsync(Guid patientId)
    {
        var query = new GetResourceAuditTrailQuery
        {
            ResourceType = "Patient",
            ResourceId = patientId.ToString()
        };

        var result = await _mediator.Send(query);
        return result.Data ?? Enumerable.Empty<AuditLogDto>();
    }

    /// <summary>
    /// Get user's activity for compliance review
    /// </summary>
    public async Task<PagedResultDto<AuditLogDto>> GetUserActivityAsync(
        string userId,
        DateTime fromDate,
        DateTime toDate)
    {
        var query = new GetAuditLogsQuery
        {
            QueryParams = new AuditLogQueryDto
            {
                UserId = userId,
                FromDate = fromDate,
                ToDate = toDate,
                PageNumber = 1,
                PageSize = 100,
                SortBy = "Timestamp",
                SortDescending = true
            }
        };

        return await _mediator.Send(query);
    }
}
```

### Example: API Client Usage

```csharp
// C# HttpClient example
public class AuditApiClient
{
    private readonly HttpClient _httpClient;

    public async Task<PagedResultDto<AuditLogDto>> GetAuditLogsAsync(
        string? userId = null,
        AuditEventType? eventType = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int pageNumber = 1,
        int pageSize = 50)
    {
        var queryParams = new List<string>();

        if (!string.IsNullOrEmpty(userId))
            queryParams.Add($"userId={userId}");

        if (eventType.HasValue)
            queryParams.Add($"eventType={eventType}");

        if (fromDate.HasValue)
            queryParams.Add($"fromDate={fromDate:O}");

        if (toDate.HasValue)
            queryParams.Add($"toDate={toDate:O}");

        queryParams.Add($"pageNumber={pageNumber}");
        queryParams.Add($"pageSize={pageSize}");

        var url = $"/api/audit?{string.Join("&", queryParams)}";

        var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<PagedResultDto<AuditLogDto>>();
    }
}
```

---

## Common Scenarios

### Scenario 1: Doctor Views Patient Record

```csharp
// Automatic via MediatR attribute
[Auditable(AuditEventType.View, "Patient", "Viewed patient demographics", AccessesPhi = true)]
public class GetPatientQuery : IQuery<PatientDto>
{
    public Guid PatientId { get; set; }
}

// Result in audit log:
// EventType: View
// UserId: doctor-id
// ResourceType: Patient
// ResourceId: patient-id
// Action: Viewed patient demographics
// Success: true
// Timestamp: 2024-01-15T10:30:00Z
```

### Scenario 2: Nurse Updates Vital Signs

```csharp
// Automatic via entity change tracking
var encounter = await _encounterRepository.GetByIdAsync(encounterId);
encounter.UpdateVitalSigns(bloodPressure, heartRate, temperature);
await _unitOfWork.SaveChangesAsync();

// Result in audit log:
// EventType: Update
// UserId: nurse-id
// ResourceType: Encounter
// ResourceId: encounter-id
// Action: Updated Encounter
// OldValues: {"BloodPressure":"120/80","HeartRate":72}
// NewValues: {"BloodPressure":"130/85","HeartRate":78}
```

### Scenario 3: Admin Exports Patient List

```csharp
// Manual logging for export operation
await _auditService.LogExportOperationAsync(
    AuditEventType.Export,
    userId: adminId,
    resourceType: "PatientList",
    resourceId: null,
    format: "CSV",
    ipAddress: "192.168.1.100");

// Result in audit log:
// EventType: Export
// UserId: admin-id
// ResourceType: PatientList
// Action: Exported PatientList to CSV
// Format: CSV
```

### Scenario 4: Failed Login Attempt

```csharp
// Authentication service logs failed attempt
await _auditService.LogAuthenticationAsync(
    AuditEventType.FailedLogin,
    userId: attemptedEmail,
    username: attemptedEmail,
    success: false,
    ipAddress: "192.168.1.200",
    errorMessage: "Invalid password");

// Result in audit log:
// EventType: FailedLogin
// UserId: user@email.com
// Action: Failed login attempt
// Success: false
// ErrorMessage: Invalid password
// IpAddress: 192.168.1.200
```

---

## Best Practices

1. **Use Attributes When Possible**: Prefer `[Auditable]` for MediatR and action filters for controllers
2. **Mark PHI Access**: Always set `AccessesPhi = true` for PHI-related operations
3. **Provide Meaningful Actions**: Use descriptive action strings that explain what happened
4. **Don't Log PHI**: Never include patient names, SSN, or sensitive data in audit details
5. **Use Correct Event Types**: Choose appropriate `AuditEventType` for the operation
6. **Handle Errors**: Always log failed access attempts for security monitoring
7. **Include Context**: Provide IP address, user agent, and session IDs when available

## Anti-Patterns (Don't Do This)

### ❌ Logging PHI in Details

```csharp
// WRONG - Includes patient name
await _auditService.CreateAuditLogAsync(
    details: $"Viewed patient: John Doe");

// CORRECT
await _auditService.CreateAuditLogAsync(
    details: $"Viewed patient record");
```

### ❌ Skipping Audit for "Internal" Operations

```csharp
// WRONG - Every PHI access must be audited
public async Task InternalPatientUpdate(Guid patientId)
{
    // No audit logging
    var patient = await _repository.GetByIdAsync(patientId);
}

// CORRECT
[Auditable(AuditEventType.View, "Patient", "Internal system update", AccessesPhi = true)]
public async Task InternalPatientUpdate(Guid patientId)
{
    var patient = await _repository.GetByIdAsync(patientId);
}
```

### ❌ Blocking on Audit Logging

```csharp
// WRONG - Blocks the request
await _auditService.CreateAuditLogAsync(...);

// CORRECT - Fire and forget (handled by pipeline behavior)
_ = Task.Run(async () => await _auditService.CreateAuditLogAsync(...));
```

---

## Testing

### Unit Test Example

```csharp
[Fact]
public async Task GetPatient_ShouldCreateAuditLog()
{
    // Arrange
    var mockAuditService = new Mock<IAuditService>();
    var handler = new GetPatientQueryHandler(mockAuditService.Object);

    // Act
    var result = await handler.Handle(new GetPatientQuery { Id = patientId });

    // Assert
    mockAuditService.Verify(
        x => x.LogPhiAccessAsync(
            It.IsAny<string>(),
            "Patient",
            patientId.ToString(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()),
        Times.Once);
}
```

---

For more information, see [HIPAA_AUDIT_LOGGING.md](./HIPAA_AUDIT_LOGGING.md).
