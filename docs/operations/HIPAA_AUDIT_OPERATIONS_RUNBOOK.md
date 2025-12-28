# HIPAA Audit Logging - Operations Runbook

## Overview

This runbook provides operational procedures for the EMR HIPAA Audit Logging system with TimescaleDB. It covers daily operations, monitoring, troubleshooting, and maintenance tasks.

---

## 1. System Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                        EMR API Layer                             │
│  ┌──────────────────┐  ┌──────────────────┐  ┌──────────────┐  │
│  │ AuditController  │  │ AuditMiddleware  │  │ AuditService │  │
│  └────────┬─────────┘  └────────┬─────────┘  └──────┬───────┘  │
└───────────┼──────────────────────┼──────────────────┼───────────┘
            │                      │                  │
            ▼                      ▼                  ▼
┌─────────────────────────────────────────────────────────────────┐
│                   PostgreSQL + TimescaleDB                       │
│  ┌────────────────────────────────────────────────────────────┐ │
│  │              AuditLogs (Hypertable)                        │ │
│  │  - Partitioned by Timestamp (1 month chunks)               │ │
│  │  - Compressed after 30 days                                │ │
│  │  - Retained for 7 years (2,555 days)                       │ │
│  └────────────────────────────────────────────────────────────┘ │
│  ┌────────────────────────────────────────────────────────────┐ │
│  │              Continuous Aggregates                          │ │
│  │  - audit_daily_summary                                      │ │
│  │  - audit_user_activity                                      │ │
│  │  - audit_compliance_metrics                                 │ │
│  └────────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────────┘
```

### Key Components

| Component | Purpose | Port |
|-----------|---------|------|
| EMR API | REST API for audit endpoints | 5001 |
| PostgreSQL | Database server | 5432 |
| TimescaleDB | Time-series extension | N/A |

---

## 2. Daily Operations

### 2.1 Morning Health Check

```bash
#!/bin/bash
# Daily Health Check Script - Run at 8 AM

echo "=== EMR Audit System Health Check ==="
echo "Date: $(date)"

# 1. API Health
echo -e "\n1. API Health:"
curl -s -o /dev/null -w "%{http_code}" https://api.example.com/health
echo ""

# 2. Database Connection
echo -e "\n2. Database Status:"
pg_isready -h $DB_HOST -p $DB_PORT

# 3. Audit Log Count (last 24 hours)
echo -e "\n3. Audit Logs (Last 24h):"
psql -h $DB_HOST -U $DB_USER -d $DB_NAME -c \
  "SELECT COUNT(*) as logs_24h FROM \"AuditLogs\" WHERE \"Timestamp\" > NOW() - INTERVAL '24 hours';"

# 4. TimescaleDB Jobs Status
echo -e "\n4. TimescaleDB Jobs:"
psql -h $DB_HOST -U $DB_USER -d $DB_NAME -c \
  "SELECT job_id, proc_name, schedule_interval, last_run_status FROM timescaledb_information.job_stats;"

# 5. Storage Usage
echo -e "\n5. Storage Stats:"
psql -h $DB_HOST -U $DB_USER -d $DB_NAME -c \
  "SELECT hypertable_size('\"AuditLogs\"') as total_size,
          pg_size_pretty(hypertable_size('\"AuditLogs\"')) as formatted_size;"

echo -e "\n=== Health Check Complete ==="
```

### 2.2 Key Metrics to Monitor

| Metric | Normal Range | Alert Threshold | Action |
|--------|--------------|-----------------|--------|
| Audit logs/hour | 100-10,000 | < 10 or > 50,000 | Investigate |
| API response time (p95) | < 500ms | > 2000ms | Scale/optimize |
| Database CPU | < 60% | > 80% | Scale up |
| Database storage | < 80% | > 90% | Add storage |
| Failed logins/hour | < 100 | > 500 | Security review |
| 7-year query time | < 5s | > 10s | Optimize aggregates |

### 2.3 Log Locations

| Log Type | Location | Retention |
|----------|----------|-----------|
| API Application Logs | `/var/log/emr-api/` | 30 days |
| PostgreSQL Logs | `/var/log/postgresql/` | 14 days |
| TimescaleDB Job Logs | `timescaledb_information.job_stats` | N/A |
| Audit Logs (data) | `AuditLogs` hypertable | 7 years |

---

## 3. Common Operations

### 3.1 Query Audit Logs

```sql
-- Recent audit logs
SELECT * FROM "AuditLogs"
WHERE "Timestamp" > NOW() - INTERVAL '1 hour'
ORDER BY "Timestamp" DESC
LIMIT 100;

-- Logs for specific user
SELECT * FROM "AuditLogs"
WHERE "UserId" = 'user-id-here'
AND "Timestamp" > NOW() - INTERVAL '7 days'
ORDER BY "Timestamp" DESC;

-- Failed access attempts
SELECT * FROM "AuditLogs"
WHERE "EventType" = 'AccessDenied'
AND "Timestamp" > NOW() - INTERVAL '24 hours'
ORDER BY "Timestamp" DESC;

-- PHI access by resource
SELECT * FROM "AuditLogs"
WHERE "ResourceType" = 'Patient'
AND "ResourceId" = 'patient-id-here'
ORDER BY "Timestamp" DESC;
```

### 3.2 Generate Compliance Report

```bash
# Export compliance metrics
curl -X GET \
  "https://api.example.com/api/audit/compliance/metrics?fromDate=2024-01-01&toDate=2024-12-31" \
  -H "Authorization: Bearer $ADMIN_TOKEN" \
  -o compliance_report.json

# Export daily summaries
curl -X GET \
  "https://api.example.com/api/audit/daily-summaries?fromDate=2024-01-01&toDate=2024-12-31" \
  -H "Authorization: Bearer $ADMIN_TOKEN" \
  -o daily_summaries.json

# Full audit export (CSV)
curl -X GET \
  "https://api.example.com/api/audit/export/stream?fromDate=2024-01-01&toDate=2024-12-31&format=csv" \
  -H "Authorization: Bearer $ADMIN_TOKEN" \
  -o audit_export.csv
```

### 3.3 Check TimescaleDB Status

```sql
-- Hypertable info
SELECT * FROM timescaledb_information.hypertables
WHERE hypertable_name = 'AuditLogs';

-- Chunk information
SELECT chunk_name, range_start, range_end, is_compressed,
       pg_size_pretty(after_compression_total_bytes) as size
FROM timescaledb_information.chunks
WHERE hypertable_name = 'AuditLogs'
ORDER BY range_start DESC
LIMIT 12;

-- Compression stats
SELECT *
FROM hypertable_compression_stats('"AuditLogs"');

-- Policy status
SELECT job_id, proc_name, schedule_interval,
       last_run_started_at, last_run_status, next_run
FROM timescaledb_information.job_stats
WHERE proc_name LIKE '%policy%';

-- Continuous aggregate status
SELECT view_name, materialization_hypertable_name
FROM timescaledb_information.continuous_aggregates;
```

### 3.4 Refresh Continuous Aggregates

```sql
-- Manual refresh (if needed)
CALL refresh_continuous_aggregate('audit_daily_summary', NULL, NULL);
CALL refresh_continuous_aggregate('audit_user_activity', NULL, NULL);
CALL refresh_continuous_aggregate('audit_compliance_metrics', NULL, NULL);

-- Verify refresh
SELECT view_name, last_refresh_time
FROM timescaledb_information.continuous_aggregate_stats;
```

---

## 4. Troubleshooting

### 4.1 API Returns 500 Error

**Symptoms:**
- `/api/audit` endpoints return HTTP 500
- Error in API logs

**Diagnosis:**
```bash
# Check API logs
tail -f /var/log/emr-api/error.log

# Check database connectivity
pg_isready -h $DB_HOST -p $DB_PORT

# Test database query
psql -h $DB_HOST -U $DB_USER -d $DB_NAME -c "SELECT 1;"
```

**Resolution:**
1. If database unreachable: See Section 4.3
2. If query error: Check for corrupt data
3. If memory error: Restart API service

### 4.2 Slow Query Performance

**Symptoms:**
- 7-year queries take > 5 seconds
- Dashboard loading slowly
- API timeouts

**Diagnosis:**
```sql
-- Check if aggregates are being used
EXPLAIN ANALYZE
SELECT * FROM audit_compliance_metrics
WHERE bucket >= '2018-01-01' AND bucket <= '2024-12-31';

-- Check for missing indexes
SELECT tablename, indexname
FROM pg_indexes
WHERE tablename = 'AuditLogs';

-- Check chunk compression status
SELECT chunk_name, is_compressed
FROM timescaledb_information.chunks
WHERE hypertable_name = 'AuditLogs'
AND is_compressed = false;
```

**Resolution:**
1. Refresh continuous aggregates
2. Verify compression policy running
3. Add missing indexes
4. Increase work_mem for complex queries

### 4.3 Database Connection Issues

**Symptoms:**
- "Connection refused" errors
- Timeout errors
- Pool exhaustion

**Diagnosis:**
```bash
# Check PostgreSQL status
systemctl status postgresql

# Check connections
psql -c "SELECT count(*) FROM pg_stat_activity;"

# Check max connections
psql -c "SHOW max_connections;"
```

**Resolution:**
1. Restart PostgreSQL if not running
2. Increase max_connections if needed
3. Kill idle connections:
   ```sql
   SELECT pg_terminate_backend(pid)
   FROM pg_stat_activity
   WHERE state = 'idle'
   AND query_start < NOW() - INTERVAL '1 hour';
   ```

### 4.4 Missing Audit Logs

**Symptoms:**
- Fewer logs than expected
- Gaps in audit trail
- API access not logged

**Diagnosis:**
```sql
-- Check for gaps
SELECT date_trunc('hour', "Timestamp") as hour, COUNT(*)
FROM "AuditLogs"
WHERE "Timestamp" > NOW() - INTERVAL '24 hours'
GROUP BY 1
ORDER BY 1;

-- Verify middleware is active
-- Check API configuration for AuditLogMiddleware
```

**Resolution:**
1. Verify AuditLogMiddleware in pipeline
2. Check for exceptions in API logs
3. Verify database write permissions
4. Check for transaction rollbacks

### 4.5 TimescaleDB Job Failures

**Symptoms:**
- Compression not happening
- Retention not enforced
- Aggregates stale

**Diagnosis:**
```sql
-- Check job errors
SELECT job_id, proc_name, last_run_status, last_run_duration,
       total_failures, last_run_reason
FROM timescaledb_information.job_stats
WHERE last_run_status = 'Failed';

-- Check job logs
SELECT * FROM timescaledb_information.job_errors
ORDER BY finish_time DESC
LIMIT 10;
```

**Resolution:**
1. Check disk space
2. Verify user permissions
3. Manually run failed job:
   ```sql
   CALL run_job(job_id);
   ```
4. Recreate policy if corrupted

---

## 5. Maintenance Procedures

### 5.1 Weekly Maintenance

```bash
#!/bin/bash
# Weekly Maintenance Script - Run Sunday 2 AM

echo "=== Weekly Maintenance ==="

# 1. Analyze tables for query optimization
psql -c "ANALYZE \"AuditLogs\";"

# 2. Check for bloat
psql -c "SELECT schemaname, tablename,
         pg_size_pretty(pg_total_relation_size(schemaname||'.'||tablename)) as size
         FROM pg_tables WHERE tablename = 'AuditLogs';"

# 3. Verify backups
ls -la /backups/*.dump | tail -7

# 4. Check TimescaleDB license
psql -c "SELECT * FROM timescaledb_information.license;"

echo "=== Maintenance Complete ==="
```

### 5.2 Monthly Maintenance

```sql
-- 1. Reindex for performance (low-traffic window)
REINDEX TABLE "AuditLogs";

-- 2. Verify retention policy
SELECT * FROM timescaledb_information.jobs
WHERE proc_name = 'policy_retention';

-- 3. Check oldest data (should be ~7 years)
SELECT MIN("Timestamp") as oldest_record FROM "AuditLogs";

-- 4. Review compression ratio
SELECT *
FROM hypertable_compression_stats('"AuditLogs"');

-- 5. Vacuum analyze
VACUUM ANALYZE "AuditLogs";
```

### 5.3 Quarterly Maintenance

- [ ] Review and update runbook
- [ ] Conduct DR test
- [ ] Review access permissions
- [ ] Update SSL certificates if needed
- [ ] Review performance trends
- [ ] Capacity planning review

---

## 6. Monitoring & Alerting

### 6.1 Prometheus Metrics

```yaml
# Key metrics to monitor
- emr_audit_logs_total: Total audit log count
- emr_audit_logs_rate: Logs per second
- emr_audit_query_duration_seconds: Query response times
- emr_audit_export_duration_seconds: Export operation times
- emr_audit_errors_total: Error count by type
- pg_database_size_bytes: Database size
- pg_stat_activity_count: Active connections
```

### 6.2 Alert Rules

```yaml
# Prometheus alert rules
groups:
  - name: emr-audit-alerts
    rules:
      - alert: AuditLogWriteFailure
        expr: rate(emr_audit_errors_total{type="write"}[5m]) > 0
        for: 5m
        labels:
          severity: critical
        annotations:
          summary: "Audit log writes failing"

      - alert: SevenYearQuerySlow
        expr: histogram_quantile(0.95, emr_audit_query_duration_seconds{query="seven_year"}) > 5
        for: 10m
        labels:
          severity: warning
        annotations:
          summary: "7-year queries exceeding SLA"

      - alert: DatabaseStorageHigh
        expr: pg_database_size_bytes / pg_database_max_size_bytes > 0.9
        for: 1h
        labels:
          severity: warning
        annotations:
          summary: "Database storage above 90%"

      - alert: CompressionJobFailed
        expr: emr_timescaledb_job_status{job="compression"} == 0
        for: 6h
        labels:
          severity: warning
        annotations:
          summary: "Compression job not running"
```

### 6.3 Dashboard Panels

| Panel | Query | Purpose |
|-------|-------|---------|
| Audit Events/Hour | `rate(emr_audit_logs_total[1h])` | Throughput monitoring |
| Query Latency | `histogram_quantile(0.95, ...)` | Performance monitoring |
| Error Rate | `rate(emr_audit_errors_total[5m])` | Reliability monitoring |
| Storage Growth | `delta(pg_database_size_bytes[24h])` | Capacity planning |

---

## 7. Security Operations

### 7.1 Access Review

```sql
-- Monthly access review query
SELECT DISTINCT "UserId", "UserRole",
       COUNT(*) as access_count,
       MAX("Timestamp") as last_access
FROM "AuditLogs"
WHERE "Timestamp" > NOW() - INTERVAL '30 days'
AND "EventType" IN ('View', 'Export')
GROUP BY "UserId", "UserRole"
ORDER BY access_count DESC;
```

### 7.2 Suspicious Activity Detection

```sql
-- Failed login attempts by user
SELECT "UserId", COUNT(*) as failures
FROM "AuditLogs"
WHERE "EventType" = 'AccessDenied'
AND "Action" = 'Login'
AND "Timestamp" > NOW() - INTERVAL '24 hours'
GROUP BY "UserId"
HAVING COUNT(*) > 5
ORDER BY failures DESC;

-- After-hours access
SELECT *
FROM "AuditLogs"
WHERE EXTRACT(HOUR FROM "Timestamp") NOT BETWEEN 7 AND 19
AND "Timestamp" > NOW() - INTERVAL '24 hours'
AND "EventType" != 'System'
ORDER BY "Timestamp" DESC;

-- Large data exports
SELECT *
FROM "AuditLogs"
WHERE "EventType" = 'Export'
AND "Timestamp" > NOW() - INTERVAL '7 days'
ORDER BY "Timestamp" DESC;
```

### 7.3 Incident Response

1. **Detection**: Alert triggers or manual report
2. **Containment**: Disable affected accounts if needed
3. **Investigation**: Query audit logs for scope
4. **Eradication**: Remove threat access
5. **Recovery**: Restore normal operations
6. **Lessons Learned**: Update procedures

---

## 8. Appendix

### 8.1 Useful SQL Queries

```sql
-- Event type distribution
SELECT "EventType", COUNT(*) as count
FROM "AuditLogs"
WHERE "Timestamp" > NOW() - INTERVAL '30 days'
GROUP BY "EventType"
ORDER BY count DESC;

-- Most accessed resources
SELECT "ResourceType", "ResourceId", COUNT(*) as access_count
FROM "AuditLogs"
WHERE "Timestamp" > NOW() - INTERVAL '30 days'
GROUP BY "ResourceType", "ResourceId"
ORDER BY access_count DESC
LIMIT 20;

-- User activity summary
SELECT "UserId",
       COUNT(*) as total_actions,
       COUNT(DISTINCT DATE("Timestamp")) as active_days
FROM "AuditLogs"
WHERE "Timestamp" > NOW() - INTERVAL '30 days'
GROUP BY "UserId"
ORDER BY total_actions DESC;
```

### 8.2 Configuration Reference

| Setting | Value | Location |
|---------|-------|----------|
| Chunk Interval | 1 month | TimescaleDB |
| Compression Age | 30 days | Compression Policy |
| Retention Period | 2,555 days | Retention Policy |
| Aggregate Refresh | 1 hour | Continuous Aggregate |
| Max Page Size | 100 | API Configuration |
| Export Timeout | 120 seconds | API Configuration |

### 8.3 Contact Information

| Role | Contact | Availability |
|------|---------|--------------|
| DBA On-Call | | 24/7 |
| DevOps On-Call | | 24/7 |
| Application Support | | Business Hours |
| Security Team | | 24/7 |
| Compliance Officer | | Business Hours |

---

**Document Version**: 1.0
**Last Updated**: 2024-12-28
**Owner**: IT Operations
**Review Frequency**: Quarterly
