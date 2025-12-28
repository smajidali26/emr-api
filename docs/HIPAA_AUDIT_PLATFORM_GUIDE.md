# EMR HIPAA Audit Logging Platform Guide

## Overview

The EMR HIPAA Audit Logging system provides comprehensive audit trail capabilities for healthcare data access, meeting HIPAA Security Rule requirements for audit controls (§164.312(b)).

**Key Features:**
- Automatic logging of all PHI access
- 7-year data retention (HIPAA requirement)
- TimescaleDB-powered time-series storage
- Real-time compliance dashboard
- Streaming export for compliance reports

---

## Architecture

### System Components

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                              Client Layer                                    │
│  ┌───────────────────┐  ┌───────────────────┐  ┌───────────────────────┐   │
│  │  Admin Dashboard  │  │  Provider Portal  │  │    Patient Portal     │   │
│  │  (React + Recharts)│  │   (React App)     │  │     (React App)       │   │
│  └─────────┬─────────┘  └─────────┬─────────┘  └───────────┬───────────┘   │
└────────────┼──────────────────────┼────────────────────────┼────────────────┘
             │                      │                        │
             ▼                      ▼                        ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                              API Layer                                       │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │                    ASP.NET Core 8.0 Web API                          │   │
│  │  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────────┐  │   │
│  │  │ AuditController │  │ AuditMiddleware │  │ Other Controllers   │  │   │
│  │  │  (Admin Only)   │  │ (All Requests)  │  │                     │  │   │
│  │  └────────┬────────┘  └────────┬────────┘  └──────────┬──────────┘  │   │
│  │           │                    │                      │              │   │
│  │  ┌────────▼─────────────────────────────────────────────────────┐   │   │
│  │  │                    Application Services                       │   │   │
│  │  │  ┌─────────────┐  ┌──────────────────┐  ┌─────────────────┐  │   │   │
│  │  │  │AuditService │  │AuditStatistics   │  │TimescaleDbConfig│  │   │   │
│  │  │  │             │  │Service           │  │                 │  │   │   │
│  │  │  └─────────────┘  └──────────────────┘  └─────────────────┘  │   │   │
│  │  └──────────────────────────────────────────────────────────────┘   │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────────────────┘
             │
             ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                            Data Layer                                        │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │                PostgreSQL 15 + TimescaleDB 2.x                       │   │
│  │  ┌─────────────────────────────────────────────────────────────┐    │   │
│  │  │                AuditLogs (Hypertable)                        │    │   │
│  │  │  - Partitioned by Timestamp (monthly chunks)                 │    │   │
│  │  │  - Compressed after 30 days (~10:1 ratio)                    │    │   │
│  │  │  - Retained for 2,555 days (7 years)                         │    │   │
│  │  └─────────────────────────────────────────────────────────────┘    │   │
│  │  ┌─────────────────────────────────────────────────────────────┐    │   │
│  │  │              Continuous Aggregates                           │    │   │
│  │  │  - audit_daily_summary (daily event counts)                  │    │   │
│  │  │  - audit_user_activity (hourly user activity)                │    │   │
│  │  │  - audit_compliance_metrics (daily compliance KPIs)          │    │   │
│  │  └─────────────────────────────────────────────────────────────┘    │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────────────────┘
```

### Data Flow

1. **Request Received**: API receives authenticated request
2. **Middleware Intercept**: AuditLogMiddleware captures request details
3. **Request Processing**: Controller handles business logic
4. **Audit Log Created**: AuditService writes to database
5. **Response Sent**: Original response returned to client
6. **Aggregates Updated**: TimescaleDB refreshes continuous aggregates (hourly)

---

## API Reference

### Authentication

All audit endpoints require Admin role. Include JWT token in Authorization header:

```
Authorization: Bearer <jwt_token>
```

### Endpoints

#### GET /api/audit

Query paginated audit logs with filters.

**Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| fromDate | string | No | Start date (ISO 8601) |
| toDate | string | No | End date (ISO 8601) |
| userId | string | No | Filter by user ID |
| eventType | string | No | Filter by event type |
| resourceType | string | No | Filter by resource type |
| resourceId | string | No | Filter by resource ID |
| pageNumber | int | No | Page number (default: 1) |
| pageSize | int | No | Items per page (default: 25, max: 100) |

**Response:**
```json
{
  "items": [
    {
      "id": "uuid",
      "timestamp": "2024-12-28T10:30:00Z",
      "userId": "user-123",
      "userName": "Dr. Smith",
      "userRole": "Doctor",
      "eventType": "View",
      "action": "ViewPatientRecord",
      "resourceType": "Patient",
      "resourceId": "patient-456",
      "description": "Viewed patient demographics",
      "ipAddress": "192.168.1.100",
      "userAgent": "Mozilla/5.0...",
      "success": true
    }
  ],
  "totalCount": 1500,
  "pageNumber": 1,
  "pageSize": 25,
  "totalPages": 60
}
```

#### GET /api/audit/compliance/metrics

Get compliance metrics for date range (uses continuous aggregates).

**Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| fromDate | string | Yes | Start date (ISO 8601) |
| toDate | string | Yes | End date (ISO 8601) |

**Response:**
```json
{
  "totalEvents": 150000,
  "successfulEvents": 148500,
  "failedEvents": 1500,
  "successRate": 99.0,
  "eventsByType": {
    "Login": 25000,
    "View": 100000,
    "Create": 15000,
    "Update": 8000,
    "Delete": 500,
    "Export": 1000,
    "AccessDenied": 500
  },
  "topUsers": [
    { "userId": "user-1", "eventCount": 5000 },
    { "userId": "user-2", "eventCount": 4500 }
  ],
  "topResources": [
    { "resourceType": "Patient", "accessCount": 80000 },
    { "resourceType": "Appointment", "accessCount": 50000 }
  ],
  "dateRange": {
    "from": "2024-01-01",
    "to": "2024-12-28",
    "days": 362
  }
}
```

#### GET /api/audit/daily-summaries

Get daily event summaries (uses continuous aggregates).

**Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| fromDate | string | Yes | Start date (ISO 8601) |
| toDate | string | Yes | End date (ISO 8601) |

**Response:**
```json
{
  "summaries": [
    {
      "date": "2024-12-28",
      "totalEvents": 5000,
      "uniqueUsers": 150,
      "eventsByType": {
        "View": 4000,
        "Create": 500,
        "Update": 400,
        "Other": 100
      }
    }
  ]
}
```

#### GET /api/audit/users/{userId}/activity

Get activity timeline for specific user.

**Response:**
```json
{
  "userId": "user-123",
  "userName": "Dr. Smith",
  "activitySummary": {
    "totalEvents": 500,
    "firstActivity": "2024-01-15T08:30:00Z",
    "lastActivity": "2024-12-28T16:45:00Z"
  },
  "recentActivity": [
    {
      "timestamp": "2024-12-28T16:45:00Z",
      "eventType": "View",
      "resourceType": "Patient",
      "description": "Viewed patient record"
    }
  ]
}
```

#### GET /api/audit/resources/{resourceType}/{resourceId}/access

Get access history for specific resource.

**Response:**
```json
{
  "resourceType": "Patient",
  "resourceId": "patient-456",
  "accessHistory": [
    {
      "timestamp": "2024-12-28T10:30:00Z",
      "userId": "user-123",
      "userName": "Dr. Smith",
      "action": "View",
      "success": true
    }
  ],
  "accessSummary": {
    "totalAccesses": 50,
    "uniqueUsers": 5,
    "lastAccess": "2024-12-28T10:30:00Z"
  }
}
```

#### GET /api/audit/storage/stats

Get TimescaleDB storage statistics.

**Response:**
```json
{
  "hypertable": {
    "name": "AuditLogs",
    "numChunks": 84,
    "totalSize": "15 GB",
    "compressedSize": "1.5 GB"
  },
  "compression": {
    "compressionRatio": 10.5,
    "compressedChunks": 80,
    "uncompressedChunks": 4
  },
  "retention": {
    "policyDays": 2555,
    "oldestRecord": "2018-01-15T00:00:00Z",
    "newestRecord": "2024-12-28T16:45:00Z"
  },
  "performance": {
    "averageInsertTime": "5ms",
    "averageQueryTime": "50ms"
  }
}
```

#### GET /api/audit/export/stream

Stream export of audit logs (for large datasets).

**Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| fromDate | string | Yes | Start date (ISO 8601) |
| toDate | string | Yes | End date (ISO 8601) |
| format | string | No | Export format: `csv` or `json` (default: csv) |

**Response:** Streaming file download

---

## Database Schema

### AuditLogs Table

```sql
CREATE TABLE "AuditLogs" (
    "Id" UUID NOT NULL,
    "Timestamp" TIMESTAMP WITH TIME ZONE NOT NULL,
    "UserId" VARCHAR(256) NOT NULL,
    "UserName" VARCHAR(256),
    "UserRole" VARCHAR(100),
    "EventType" VARCHAR(50) NOT NULL,
    "Action" VARCHAR(256) NOT NULL,
    "ResourceType" VARCHAR(100),
    "ResourceId" VARCHAR(256),
    "Description" TEXT,
    "IpAddress" VARCHAR(45),
    "UserAgent" TEXT,
    "AdditionalData" JSONB,
    "Success" BOOLEAN NOT NULL DEFAULT true,
    "ErrorMessage" TEXT,
    "CorrelationId" VARCHAR(256),
    PRIMARY KEY ("Timestamp", "Id")
);

-- Convert to TimescaleDB hypertable
SELECT create_hypertable('"AuditLogs"', 'Timestamp', chunk_time_interval => INTERVAL '1 month');

-- Compression settings
ALTER TABLE "AuditLogs" SET (
    timescaledb.compress,
    timescaledb.compress_segmentby = 'UserId, ResourceType',
    timescaledb.compress_orderby = 'Timestamp DESC'
);

-- Policies
SELECT add_compression_policy('"AuditLogs"', INTERVAL '30 days');
SELECT add_retention_policy('"AuditLogs"', INTERVAL '2555 days');
```

### Continuous Aggregates

```sql
-- Daily summary aggregate
CREATE MATERIALIZED VIEW audit_daily_summary
WITH (timescaledb.continuous) AS
SELECT
    time_bucket('1 day', "Timestamp") AS bucket,
    COUNT(*) AS event_count,
    COUNT(DISTINCT "UserId") AS unique_users,
    COUNT(*) FILTER (WHERE "EventType" = 'View') AS view_count,
    COUNT(*) FILTER (WHERE "EventType" = 'Create') AS create_count,
    COUNT(*) FILTER (WHERE "EventType" = 'Update') AS update_count,
    COUNT(*) FILTER (WHERE "EventType" = 'Delete') AS delete_count,
    COUNT(*) FILTER (WHERE "Success" = false) AS failure_count
FROM "AuditLogs"
GROUP BY bucket;
```

---

## Configuration

### Environment Variables

| Variable | Description | Default |
|----------|-------------|---------|
| `ConnectionStrings__DefaultConnection` | PostgreSQL connection string | Required |
| `TimescaleDb__ChunkInterval` | Chunk time interval | `1 month` |
| `TimescaleDb__CompressionAge` | Days before compression | `30` |
| `TimescaleDb__RetentionDays` | Retention period in days | `2555` |
| `Audit__MaxPageSize` | Maximum page size for queries | `100` |
| `Audit__ExportTimeoutSeconds` | Export operation timeout | `120` |

### appsettings.json

```json
{
  "TimescaleDb": {
    "ChunkInterval": "1 month",
    "CompressionAgeDays": 30,
    "RetentionDays": 2555,
    "RefreshAggregatesHours": 1
  },
  "Audit": {
    "MaxPageSize": 100,
    "DefaultPageSize": 25,
    "ExportTimeoutSeconds": 120,
    "EnableDetailedLogging": false
  }
}
```

---

## Integration Guide

### Recording Audit Events

The system automatically logs all API requests via middleware. For custom audit events:

```csharp
public class MyService
{
    private readonly IAuditService _auditService;

    public async Task DoSomethingAsync()
    {
        // Your business logic here

        // Log custom audit event
        await _auditService.LogAsync(new AuditLogEntry
        {
            EventType = "CustomAction",
            Action = "PerformedCustomOperation",
            ResourceType = "CustomResource",
            ResourceId = "resource-123",
            Description = "User performed custom operation",
            Success = true
        });
    }
}
```

### Querying Audit Data

```csharp
public class ComplianceReportService
{
    private readonly IAuditStatisticsService _statsService;

    public async Task<ComplianceReport> GenerateReportAsync(DateRange range)
    {
        var metrics = await _statsService.GetComplianceMetricsAsync(
            range.Start, range.End);

        return new ComplianceReport
        {
            TotalEvents = metrics.TotalEvents,
            SuccessRate = metrics.SuccessRate,
            // ...
        };
    }
}
```

---

## Performance Considerations

### Query Optimization

1. **Use Date Filters**: Always include `fromDate` and `toDate` to leverage partitioning
2. **Limit Page Size**: Use pagination, max 100 items per page
3. **Use Aggregates**: For metrics, use continuous aggregates endpoints
4. **7-Year Queries**: Designed to complete in < 5 seconds using aggregates

### Capacity Planning

| Records/Day | Monthly Storage | 7-Year Storage | Compressed |
|-------------|-----------------|----------------|------------|
| 10,000 | ~300 MB | ~25 GB | ~2.5 GB |
| 100,000 | ~3 GB | ~250 GB | ~25 GB |
| 1,000,000 | ~30 GB | ~2.5 TB | ~250 GB |

---

## Security

### Access Control

- All endpoints require authentication
- Audit endpoints require `Admin` role
- Users can view only their own activity via `/users/{userId}/activity`

### Data Protection

- TLS 1.2+ for all API traffic
- Encryption at rest (PostgreSQL TDE)
- Audit logs are immutable (no UPDATE/DELETE)
- Sensitive data masked in logs

---

## Troubleshooting

### Common Issues

| Issue | Cause | Solution |
|-------|-------|----------|
| Slow queries | Missing date filter | Add fromDate/toDate |
| 500 errors | Database connection | Check connection string |
| Empty results | Wrong date range | Verify date format (ISO 8601) |
| Export timeout | Large dataset | Use smaller date range |

### Support

- Operations Runbook: `docs/operations/HIPAA_AUDIT_OPERATIONS_RUNBOOK.md`
- DR Procedures: `docs/operations/HIPAA_AUDIT_DR_PROCEDURES.md`
- Security Tests: `docs/security/HIPAA_AUDIT_SECURITY_TESTS.md`

---

**Version**: 1.0
**Last Updated**: 2024-12-28
