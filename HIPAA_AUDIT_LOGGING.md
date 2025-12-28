# HIPAA Audit Logging - Implementation Guide

## Overview

This document describes the HIPAA-compliant audit logging system implemented in the EMR API. The system tracks **WHO** accessed **WHAT**, **WHEN**, and from **WHERE** to meet HIPAA Technical Safeguards requirements.

## Architecture

The audit logging system follows Clean Architecture principles across four layers:

### 1. Domain Layer (EMR.Domain)

**Core Entities:**
- `AuditLog` - Immutable audit log entity with comprehensive tracking fields
- `AuditEventType` - Enum defining all auditable actions (View, Create, Update, Delete, etc.)
- `AuditableEntity` - Base class for entities requiring automatic audit trail

**Key Files:**
- `D:\code-source\EMR\source\emr-api\src\EMR.Domain\Entities\AuditLog.cs`
- `D:\code-source\EMR\source\emr-api\src\EMR.Domain\Enums\AuditEventType.cs`
- `D:\code-source\EMR\source\emr-api\src\EMR.Domain\Common\AuditableEntity.cs`

### 2. Application Layer (EMR.Application)

**Interfaces & DTOs:**
- `IAuditService` - Service interface for audit operations
- `AuditLogDto` - Data transfer object for audit log records
- `AuditLogQueryDto` - Query parameters for filtering audit logs

**Pipeline Behaviors:**
- `AuditLoggingBehaviour<TRequest, TResponse>` - MediatR pipeline for automatic command/query auditing
- `AuditableAttribute` - Attribute to mark commands/queries for auditing

**Queries:**
- `GetAuditLogsQuery` - Retrieve audit logs with filtering and pagination
- `GetResourceAuditTrailQuery` - Get complete audit trail for specific resource

**Key Files:**
- `D:\code-source\EMR\source\emr-api\src\EMR.Application\Common\Interfaces\IAuditService.cs`
- `D:\code-source\EMR\source\emr-api\src\EMR.Application\Common\Behaviours\AuditLoggingBehaviour.cs`
- `D:\code-source\EMR\source\emr-api\src\EMR.Application\Features\Audit\`

### 3. Infrastructure Layer (EMR.Infrastructure)

**Services:**
- `AuditService` - Implementation of IAuditService with database persistence and Serilog logging

**Interceptors:**
- `AuditInterceptor` - EF Core SaveChanges interceptor for automatic change tracking with PHI masking

**Configuration:**
- `AuditLogConfiguration` - EF Core entity configuration with indexes for HIPAA compliance queries
- `SerilogConfiguration` - Structured logging configuration with separate audit log retention (6 years)

**Key Files:**
- `D:\code-source\EMR\source\emr-api\src\EMR.Infrastructure\Services\AuditService.cs`
- `D:\code-source\EMR\source\emr-api\src\EMR.Infrastructure\Data\Interceptors\AuditInterceptor.cs`
- `D:\code-source\EMR\source\emr-api\src\EMR.Infrastructure\Logging\SerilogConfiguration.cs`

### 4. API Layer (EMR.Api)

**Middleware:**
- `AuditLoggingMiddleware` - HTTP request/response audit logging

**Controllers:**
- `AuditController` - Admin-only endpoints for audit log queries and compliance reporting

**Filters:**
- `PhiAccessLoggingAttribute` - Action filter for PHI access logging
- `PatientAccessLoggingAttribute` - Specialized filter for patient record access
- `EncounterAccessLoggingAttribute` - Specialized filter for encounter access

**Key Files:**
- `D:\code-source\EMR\source\emr-api\src\EMR.Api\Middleware\AuditLoggingMiddleware.cs`
- `D:\code-source\EMR\source\emr-api\src\EMR.Api\Controllers\AuditController.cs`
- `D:\code-source\EMR\source\emr-api\src\EMR.Api\Filters\PhiAccessLoggingAttribute.cs`

## HIPAA Compliance Features

### 1. Comprehensive Tracking

Every audit log captures:
- **WHO**: UserId, Username, IP Address, User Agent
- **WHAT**: ResourceType, ResourceId, Action, EventType
- **WHEN**: Timestamp (UTC with millisecond precision)
- **WHERE**: IP Address, Session ID, Correlation ID
- **HOW**: HTTP Method, Request Path, Status Code, Duration

### 2. Immutability

- Audit logs are **write-once, read-many**
- No UPDATE or DELETE operations allowed
- Database constraints enforce immutability
- Separate audit database table with restricted permissions

### 3. PHI Protection

- **Automatic PHI masking** in change tracking
- Sanitizes sensitive fields (names, SSN, DOB, addresses, etc.)
- Only metadata logged, never actual PHI values
- Example: "FirstName" value "John" becomes "J***n"

### 4. Retention Policy

- **6-year retention** for HIPAA compliance
- Serilog configuration maintains 2,190 days of logs
- Separate JSON logs for SIEM integration
- Automatic log rotation by day

### 5. Security & Access Control

- Admin-only access to audit logs
- Role-based authorization on all audit endpoints
- Audit log access is itself audited
- Tamper-evident logging with correlation IDs

## Usage Examples

### 1. Automatic Auditing with MediatR

Mark your commands/queries with `[Auditable]` attribute:

```csharp
[Auditable(AuditEventType.View, "Patient", "Viewed patient record", AccessesPhi = true)]
public class GetPatientByIdQuery : IQuery<ResultDto<PatientDto>>
{
    public Guid Id { get; set; }
}
```

The `AuditLoggingBehaviour` will automatically create audit logs.

### 2. Manual Audit Logging

Inject `IAuditService` to create custom audit logs:

```csharp
public class MyService
{
    private readonly IAuditService _auditService;

    public async Task ExportPatientData(Guid patientId)
    {
        await _auditService.LogExportOperationAsync(
            eventType: AuditEventType.Export,
            userId: _currentUser.Id,
            resourceType: "Patient",
            resourceId: patientId.ToString(),
            format: "PDF");
    }
}
```

### 3. PHI Access Logging with Action Filters

Apply to controller actions that access PHI:

```csharp
[HttpGet("{id}")]
[PatientAccessLogging] // Automatically logs PHI access
public async Task<IActionResult> GetPatient(Guid id)
{
    var query = new GetPatientByIdQuery { Id = id };
    var result = await _mediator.Send(query);
    return Ok(result);
}
```

### 4. Automatic Entity Change Tracking

Inherit from `AuditableEntity` for automatic audit trail:

```csharp
public class Patient : AuditableEntity
{
    public override bool ContainsPhi => true;
    public override string AuditResourceType => "Patient";

    // Entity properties...
}
```

The `AuditInterceptor` will automatically log all changes.

### 5. Querying Audit Logs (Admin Only)

```http
GET /api/audit?userId=123&eventType=View&fromDate=2024-01-01&pageSize=50
Authorization: Bearer {admin-token}
```

Response:
```json
{
  "items": [
    {
      "id": "uuid",
      "eventType": "View",
      "userId": "123",
      "username": "doctor@hospital.com",
      "timestamp": "2024-01-15T10:30:00Z",
      "resourceType": "Patient",
      "resourceId": "patient-uuid",
      "ipAddress": "192.168.1.100",
      "action": "Viewed patient record",
      "success": true
    }
  ],
  "totalCount": 1250,
  "pageNumber": 1,
  "pageSize": 50
}
```

### 6. Getting Resource Audit Trail

```http
GET /api/audit/trail/Patient/{patientId}
Authorization: Bearer {admin-token}
```

Returns complete history of all access and modifications to the patient record.

## Database Schema

The `AuditLogs` table includes:

**Indexes for Performance:**
- `IX_AuditLogs_UserId` - User activity tracking
- `IX_AuditLogs_Timestamp` - Time-based queries
- `IX_AuditLogs_EventType` - Filter by event type
- `IX_AuditLogs_Resource` - Resource-based queries (composite: ResourceType + ResourceId)
- `IX_AuditLogs_IpAddress` - IP-based investigations
- `IX_AuditLogs_SessionId` - Session correlation
- `IX_AuditLogs_CorrelationId` - Distributed tracing
- `IX_AuditLogs_Timestamp_EventType` - Combined filters
- `IX_AuditLogs_UserId_Timestamp` - User activity timeline

**JSONB Columns:**
- `OldValues` - Previous values (sanitized)
- `NewValues` - Updated values (sanitized)

## Serilog Integration

### Log File Structure

```
Logs/
├── application-{Date}.log          # All application logs (30 days retention)
├── audit/
│   ├── audit-{Date}.log           # Human-readable audit logs (6 years)
│   └── audit-{Date}.json          # JSON audit logs for SIEM (6 years)
├── errors/
│   └── error-{Date}.log           # Error logs (90 days)
└── security/
    └── security-{Date}.log        # Security events (1 year)
```

### Audit Log Format

**Structured Logging:**
```
2024-01-15 10:30:15.123 -05:00 [INF] AUDIT | EventType: View | User: 123 |
Resource: Patient/patient-uuid | Action: Viewed patient record |
IP: 192.168.1.100 | Success: True | Timestamp: 2024-01-15T15:30:15Z
```

**JSON Format (for SIEM):**
```json
{
  "@t": "2024-01-15T15:30:15.123Z",
  "@mt": "AUDIT | EventType: {EventType} | User: {UserId} | Resource: {ResourceType}/{ResourceId}",
  "EventType": "View",
  "UserId": "123",
  "ResourceType": "Patient",
  "ResourceId": "patient-uuid",
  "Action": "Viewed patient record",
  "IpAddress": "192.168.1.100",
  "Success": true,
  "Timestamp": "2024-01-15T15:30:15Z"
}
```

## Migration Required

After implementing this feature, run migrations to create the AuditLogs table:

```bash
cd D:\code-source\EMR\source\emr-api\src\EMR.Infrastructure
dotnet ef migrations add AddAuditLogging --startup-project ..\EMR.Api
dotnet ef database update --startup-project ..\EMR.Api
```

## Configuration

### appsettings.json

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "System": "Warning"
      }
    }
  },
  "Logging": {
    "LogPath": "Logs"
  }
}
```

### Program.cs (Serilog Setup)

```csharp
// Configure Serilog
Log.Logger = SerilogConfiguration.CreateLogger(
    environment: builder.Environment.EnvironmentName,
    logPath: "Logs");

builder.Host.UseSerilog();
```

### Middleware Registration

```csharp
// In Program.cs or Startup.cs
app.UseAuditLogging(); // Add after UseAuthentication()
```

## Security Considerations

### 1. Access Control

- Only users with `Admin` role can access audit logs
- Audit log queries are themselves audited
- Use HTTPS for all audit API calls

### 2. Data Protection

- PHI is automatically masked in change tracking
- Never log actual patient data (names, SSN, etc.)
- IP addresses and timestamps are sufficient for investigations

### 3. Tamper Protection

- Audit logs are immutable (no UPDATE/DELETE)
- Use database-level constraints to prevent modification
- Consider write-once storage or blockchain for critical deployments

### 4. SIEM Integration

- Ship logs to external SIEM (Azure Monitor, Splunk, etc.)
- Use JSON format for structured parsing
- Enable real-time alerting for suspicious patterns

## Compliance Checklist

- [x] Tracks WHO (UserId, Username, IP Address)
- [x] Tracks WHAT (ResourceType, ResourceId, Action)
- [x] Tracks WHEN (Timestamp with milliseconds)
- [x] Tracks WHERE (IP Address, Location data)
- [x] PHI access logging (View, Export, Print)
- [x] Data modification tracking (Create, Update, Delete)
- [x] Authentication event logging (Login, Logout, Failed attempts)
- [x] Immutable logs (no delete/update)
- [x] 6-year retention policy
- [x] Admin-only access to logs
- [x] PHI masking in audit data
- [x] Comprehensive indexing for queries
- [x] SIEM-ready JSON logging
- [x] Distributed tracing support (Correlation IDs)

## Performance Considerations

1. **Asynchronous Logging**: Audit logging is non-blocking (fire-and-forget in pipeline behavior)
2. **Indexing**: Comprehensive indexes on AuditLogs table for fast queries
3. **Selective Persistence**: Not all HTTP requests are persisted to database (e.g., health checks)
4. **Log Rotation**: Daily rotation prevents large file sizes
5. **Query Limits**: Default page size of 50, max 1000 to prevent performance issues

## Troubleshooting

### Audit logs not appearing

1. Check that `IAuditService` is registered in DI
2. Verify `AuditInterceptor` is added to DbContext
3. Ensure `AuditLoggingBehaviour` is registered as MediatR pipeline
4. Check Serilog configuration and log file permissions

### PHI appearing in logs

1. Review `AuditInterceptor.SanitizeValue()` method
2. Add sensitive field patterns to PHI patterns list
3. Verify entity inherits from `AuditableEntity`
4. Check `GetAuditExcludedProperties()` implementation

### Performance issues

1. Verify database indexes are created
2. Check query page size limits
3. Review log retention policy (reduce if needed)
4. Consider archiving old audit logs to separate storage

## Future Enhancements

1. **Real-time Alerting**: Integrate with Azure Monitor for suspicious activity alerts
2. **Advanced Analytics**: ML-based anomaly detection on audit patterns
3. **Blockchain Integration**: Immutable audit trail using blockchain
4. **Export to PDF**: Generate compliance reports for auditors
5. **Audit Log Encryption**: Encrypt audit logs at rest
6. **Multi-tenant Support**: Separate audit logs per organization

## Support

For questions or issues with the audit logging system, contact the development team or refer to:
- HIPAA Technical Safeguards: https://www.hhs.gov/hipaa/for-professionals/security/index.html
- Clean Architecture documentation
- Entity Framework Core documentation
