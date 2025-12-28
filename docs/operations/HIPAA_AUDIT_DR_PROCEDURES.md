# HIPAA Audit Logging - Disaster Recovery Procedures

## Overview

This document outlines disaster recovery procedures for the EMR HIPAA Audit Logging system with TimescaleDB. These procedures ensure audit data availability and integrity during system failures.

**Recovery Time Objective (RTO)**: 4 hours
**Recovery Point Objective (RPO)**: 1 hour
**Data Retention**: 7 years (2,555 days)

---

## 1. Disaster Scenarios

### 1.1 Scenario Classifications

| Scenario | Severity | RTO | Description |
|----------|----------|-----|-------------|
| Database Server Failure | Critical | 2h | Primary PostgreSQL server unresponsive |
| Data Corruption | Critical | 4h | Audit data integrity compromised |
| Cloud Region Outage | High | 4h | Entire cloud region unavailable |
| Network Failure | Medium | 1h | API connectivity issues |
| Application Failure | Medium | 30m | EMR API service down |
| TimescaleDB Extension Failure | High | 2h | Hypertable or aggregates corrupted |

### 1.2 Impact Assessment

| Component | Business Impact | Data at Risk |
|-----------|-----------------|--------------|
| Audit Log Writes | New PHI access not logged (compliance violation) | Current session data |
| Audit Log Reads | Compliance dashboard unavailable | None (read-only) |
| 7-Year Queries | Historical reporting unavailable | None |
| Export Functionality | Compliance reports delayed | None |

---

## 2. Backup Strategy

### 2.1 Backup Schedule

| Backup Type | Frequency | Retention | Storage |
|-------------|-----------|-----------|---------|
| Full Database Backup | Daily (2 AM UTC) | 30 days | Azure Blob / AWS S3 |
| Incremental Backup | Hourly | 7 days | Azure Blob / AWS S3 |
| Transaction Log Backup | Every 15 minutes | 24 hours | Azure Blob / AWS S3 |
| TimescaleDB Chunk Backup | Weekly | 90 days | Azure Blob / AWS S3 |

### 2.2 Backup Verification

```bash
#!/bin/bash
# Backup Verification Script

BACKUP_PATH="/backups/emr-audit"
LATEST_BACKUP=$(ls -t $BACKUP_PATH/*.dump | head -1)

echo "Verifying latest backup: $LATEST_BACKUP"

# Check backup file integrity
pg_restore --list $LATEST_BACKUP > /dev/null 2>&1
if [ $? -eq 0 ]; then
    echo "✓ Backup file integrity verified"
else
    echo "✗ Backup file corrupted"
    exit 1
fi

# Verify backup contains AuditLogs table
pg_restore --list $LATEST_BACKUP | grep -q "AuditLogs"
if [ $? -eq 0 ]; then
    echo "✓ AuditLogs table present in backup"
else
    echo "✗ AuditLogs table missing from backup"
    exit 1
fi

# Verify TimescaleDB metadata
pg_restore --list $LATEST_BACKUP | grep -q "timescaledb"
if [ $? -eq 0 ]; then
    echo "✓ TimescaleDB metadata present"
else
    echo "✗ TimescaleDB metadata missing"
    exit 1
fi

echo "Backup verification complete"
```

### 2.3 Backup Commands

```bash
# Full database backup with TimescaleDB
pg_dump -Fc -v \
  --host=$DB_HOST \
  --port=$DB_PORT \
  --username=$DB_USER \
  --dbname=$DB_NAME \
  --file=/backups/emr-audit-$(date +%Y%m%d-%H%M%S).dump

# Backup specific hypertable chunks
psql -c "SELECT timescaledb_pre_restore();"
pg_dump -Fc -t '"AuditLogs"' -f /backups/audit-logs-$(date +%Y%m%d).dump $DB_NAME
psql -c "SELECT timescaledb_post_restore();"
```

---

## 3. Recovery Procedures

### 3.1 Database Server Failure Recovery

**Symptoms:**
- API returns 500/503 errors
- Database connection timeouts
- "Connection refused" errors

**Recovery Steps:**

1. **Assess the Situation** (5 minutes)
   ```bash
   # Check database server status
   pg_isready -h $DB_HOST -p $DB_PORT

   # Check cloud provider status
   az postgres server show --name $SERVER_NAME --resource-group $RG
   ```

2. **Attempt Restart** (10 minutes)
   ```bash
   # Restart PostgreSQL service
   sudo systemctl restart postgresql

   # Or restart cloud managed instance
   az postgres server restart --name $SERVER_NAME --resource-group $RG
   ```

3. **Failover to Replica** (if restart fails, 30 minutes)
   ```bash
   # Promote read replica to primary
   az postgres server replica stop --name $REPLICA_NAME --resource-group $RG

   # Update connection strings in API configuration
   kubectl set env deployment/emr-api DB_HOST=$NEW_PRIMARY_HOST

   # Restart API pods
   kubectl rollout restart deployment/emr-api
   ```

4. **Restore from Backup** (if no replica, 2 hours)
   ```bash
   # Create new PostgreSQL instance
   az postgres server create --name $NEW_SERVER --resource-group $RG ...

   # Install TimescaleDB extension
   psql -c "CREATE EXTENSION IF NOT EXISTS timescaledb CASCADE;"

   # Restore from backup
   pg_restore -v -d $DB_NAME /backups/latest.dump

   # Verify TimescaleDB hypertable
   psql -c "SELECT hypertable_name FROM timescaledb_information.hypertables;"
   ```

5. **Verify Recovery**
   ```bash
   # Check audit log count
   psql -c "SELECT COUNT(*) FROM \"AuditLogs\";"

   # Test API connectivity
   curl -s https://api.example.com/health

   # Verify 7-year query works
   curl -s "https://api.example.com/api/audit/compliance/metrics?fromDate=2018-01-01" \
     -H "Authorization: Bearer $TOKEN"
   ```

### 3.2 Data Corruption Recovery

**Symptoms:**
- Inconsistent query results
- Database constraint violations
- TimescaleDB chunk errors

**Recovery Steps:**

1. **Identify Corruption Scope** (30 minutes)
   ```sql
   -- Check for corrupt chunks
   SELECT chunk_name, range_start, range_end
   FROM timescaledb_information.chunks
   WHERE hypertable_name = 'AuditLogs';

   -- Verify data integrity
   SELECT COUNT(*), MIN("Timestamp"), MAX("Timestamp")
   FROM "AuditLogs";

   -- Check for constraint violations
   SELECT * FROM "AuditLogs"
   WHERE "Timestamp" IS NULL OR "Id" IS NULL;
   ```

2. **Isolate Corrupt Data** (1 hour)
   ```sql
   -- If specific chunk is corrupt, identify date range
   SELECT range_start, range_end
   FROM timescaledb_information.chunks
   WHERE chunk_name = '_hyper_1_XX_chunk';

   -- Create backup of corrupt chunk
   CREATE TABLE audit_logs_backup AS
   SELECT * FROM "AuditLogs"
   WHERE "Timestamp" >= 'YYYY-MM-DD' AND "Timestamp" < 'YYYY-MM-DD';
   ```

3. **Restore from Point-in-Time** (2 hours)
   ```bash
   # Restore to new database from backup
   createdb emr_audit_recovery
   pg_restore -d emr_audit_recovery /backups/pre-corruption.dump

   # Extract missing/corrupt data
   psql -d emr_audit_recovery -c "\COPY (SELECT * FROM \"AuditLogs\" WHERE ...) TO '/tmp/recovered_data.csv' CSV HEADER"

   # Re-import recovered data
   psql -d $DB_NAME -c "\COPY \"AuditLogs\" FROM '/tmp/recovered_data.csv' CSV HEADER"
   ```

4. **Verify Data Integrity**
   ```sql
   -- Recompute continuous aggregates
   CALL refresh_continuous_aggregate('audit_daily_summary', NULL, NULL);
   CALL refresh_continuous_aggregate('audit_user_activity', NULL, NULL);
   CALL refresh_continuous_aggregate('audit_compliance_metrics', NULL, NULL);

   -- Verify counts match
   SELECT
     (SELECT COUNT(*) FROM "AuditLogs") as raw_count,
     (SELECT SUM(event_count) FROM audit_daily_summary) as aggregate_count;
   ```

### 3.3 Cloud Region Outage Recovery

**Symptoms:**
- All services in region unavailable
- DNS resolution failures
- Load balancer health checks failing

**Recovery Steps:**

1. **Confirm Region Outage** (5 minutes)
   - Check cloud provider status page
   - Verify multiple services affected
   - Contact cloud support if needed

2. **Activate DR Region** (1 hour)
   ```bash
   # Update DNS to point to DR region
   az network dns record-set a update \
     --name api --zone-name example.com \
     --resource-group $RG \
     --set "ARecords[0].Ipv4Address=$DR_IP"

   # Start DR database if not already running
   az postgres server start --name $DR_DB_SERVER --resource-group $DR_RG

   # Deploy API to DR region
   kubectl config use-context $DR_CLUSTER
   kubectl apply -f k8s/emr-api/
   ```

3. **Sync Data After Primary Returns** (varies)
   ```bash
   # After primary region recovers, sync any missed data
   pg_dump -t '"AuditLogs"' --data-only \
     -h $DR_HOST -U $USER $DB_NAME | \
     psql -h $PRIMARY_HOST -U $USER $DB_NAME
   ```

### 3.4 TimescaleDB Extension Failure

**Symptoms:**
- Queries on hypertable fail
- Continuous aggregates not refreshing
- Compression policy errors

**Recovery Steps:**

1. **Diagnose Issue** (15 minutes)
   ```sql
   -- Check extension status
   SELECT * FROM pg_extension WHERE extname = 'timescaledb';

   -- Check job status
   SELECT * FROM timescaledb_information.jobs WHERE proc_name LIKE '%policy%';

   -- Check for failed jobs
   SELECT * FROM timescaledb_information.job_stats WHERE last_run_status = 'Failed';
   ```

2. **Repair Policies** (30 minutes)
   ```sql
   -- Recreate compression policy if missing
   SELECT add_compression_policy('"AuditLogs"', INTERVAL '30 days');

   -- Recreate retention policy if missing
   SELECT add_retention_policy('"AuditLogs"', INTERVAL '2555 days');

   -- Manually refresh aggregates
   CALL refresh_continuous_aggregate('audit_daily_summary', NULL, NULL);
   ```

3. **Rebuild Hypertable** (if necessary, 4+ hours)
   ```sql
   -- Backup current data
   CREATE TABLE audit_logs_temp AS SELECT * FROM "AuditLogs";

   -- Drop and recreate hypertable
   DROP TABLE "AuditLogs";
   CREATE TABLE "AuditLogs" (...);
   SELECT create_hypertable('"AuditLogs"', 'Timestamp', chunk_time_interval => INTERVAL '1 month');

   -- Restore data
   INSERT INTO "AuditLogs" SELECT * FROM audit_logs_temp;

   -- Recreate policies and aggregates
   -- (see TimescaleDbConfiguration.cs for SQL)
   ```

---

## 4. DR Testing Procedures

### 4.1 Quarterly DR Test

**Objective**: Verify ability to recover from complete database failure

**Test Steps**:

1. **Pre-Test Preparation**
   - [ ] Notify stakeholders of DR test window
   - [ ] Verify backup availability
   - [ ] Prepare test environment
   - [ ] Document current audit log count for verification

2. **Execute Recovery**
   - [ ] Create test database instance
   - [ ] Restore from latest backup
   - [ ] Install TimescaleDB extension
   - [ ] Verify hypertable structure
   - [ ] Point test API to recovered database

3. **Verification**
   - [ ] Compare record counts
   - [ ] Execute 7-year query
   - [ ] Verify continuous aggregates
   - [ ] Test export functionality
   - [ ] Test new audit log writes

4. **Document Results**
   - [ ] Record recovery time (actual vs. RTO)
   - [ ] Note any data loss (actual vs. RPO)
   - [ ] Document issues encountered
   - [ ] Update procedures as needed

### 4.2 Monthly Backup Verification

```bash
#!/bin/bash
# Monthly Backup Verification Script

echo "=== Monthly Backup Verification ==="
echo "Date: $(date)"

# 1. List recent backups
echo -e "\n1. Recent Backups:"
ls -la /backups/*.dump | tail -5

# 2. Verify latest backup integrity
LATEST=$(ls -t /backups/*.dump | head -1)
echo -e "\n2. Verifying: $LATEST"
pg_restore --list $LATEST > /dev/null && echo "✓ Integrity OK" || echo "✗ Integrity FAILED"

# 3. Test restore to temp database
echo -e "\n3. Test Restore:"
dropdb --if-exists emr_audit_test_restore
createdb emr_audit_test_restore
pg_restore -d emr_audit_test_restore $LATEST

# 4. Verify record count
echo -e "\n4. Record Count:"
psql -d emr_audit_test_restore -c "SELECT COUNT(*) as audit_records FROM \"AuditLogs\";"

# 5. Verify TimescaleDB
echo -e "\n5. TimescaleDB Status:"
psql -d emr_audit_test_restore -c "SELECT hypertable_name FROM timescaledb_information.hypertables;"

# 6. Cleanup
dropdb emr_audit_test_restore
echo -e "\n=== Verification Complete ==="
```

### 4.3 Annual Full DR Exercise

**Scope**: Complete failover to DR region

**Duration**: 4-6 hours (off-peak)

**Participants**:
- DBA Team
- DevOps Team
- Application Team
- Compliance Officer

**Exercise Plan**:

| Time | Activity | Responsible |
|------|----------|-------------|
| T+0 | Announce DR exercise start | Project Manager |
| T+15 | Simulate primary region failure | DevOps |
| T+30 | Execute failover procedures | DBA Team |
| T+90 | Verify DR environment operational | All Teams |
| T+120 | Execute test workload | Application Team |
| T+180 | Verify data integrity | DBA Team |
| T+240 | Failback to primary region | DevOps |
| T+300 | Post-exercise review | All Teams |

---

## 5. Communication Plan

### 5.1 Escalation Matrix

| Severity | Response Time | Notification |
|----------|---------------|--------------|
| Critical | 15 minutes | Page on-call + email leadership |
| High | 30 minutes | Page on-call |
| Medium | 1 hour | Email on-call |
| Low | Next business day | Ticket |

### 5.2 Contact List

| Role | Primary | Backup | Phone |
|------|---------|--------|-------|
| DBA On-Call | | | |
| DevOps On-Call | | | |
| Security Team | | | |
| Compliance Officer | | | |
| IT Director | | | |

### 5.3 Status Update Template

```
HIPAA AUDIT SYSTEM - INCIDENT UPDATE

Incident ID: INC-YYYY-NNNN
Time: [TIMESTAMP]
Status: [INVESTIGATING | IN PROGRESS | RESOLVED]
Severity: [CRITICAL | HIGH | MEDIUM | LOW]

Current State:
[Description of current system state]

Actions Taken:
- [Action 1]
- [Action 2]

Next Steps:
- [Planned action]

ETA to Resolution: [Time estimate]

Impact:
- Audit log writes: [OPERATIONAL | DEGRADED | UNAVAILABLE]
- Audit log reads: [OPERATIONAL | DEGRADED | UNAVAILABLE]
- Compliance reporting: [OPERATIONAL | DEGRADED | UNAVAILABLE]

Contact: [Name] at [Phone/Email]
```

---

## 6. Recovery Checklists

### 6.1 Database Recovery Checklist

- [ ] Identify failure type and scope
- [ ] Notify stakeholders
- [ ] Determine recovery approach (restart/failover/restore)
- [ ] Execute recovery procedure
- [ ] Verify PostgreSQL service running
- [ ] Verify TimescaleDB extension loaded
- [ ] Verify hypertable accessible
- [ ] Verify continuous aggregates working
- [ ] Verify compression policy active
- [ ] Verify retention policy active
- [ ] Test API connectivity
- [ ] Test audit log writes
- [ ] Test audit log reads
- [ ] Test 7-year query
- [ ] Update monitoring/alerting
- [ ] Document incident
- [ ] Conduct post-mortem

### 6.2 Post-Recovery Verification Queries

```sql
-- 1. Basic connectivity
SELECT version();
SELECT * FROM pg_extension WHERE extname = 'timescaledb';

-- 2. Hypertable status
SELECT * FROM timescaledb_information.hypertables
WHERE hypertable_name = 'AuditLogs';

-- 3. Chunk status
SELECT chunk_name, range_start, range_end, is_compressed
FROM timescaledb_information.chunks
WHERE hypertable_name = 'AuditLogs'
ORDER BY range_start DESC LIMIT 10;

-- 4. Record counts
SELECT
  COUNT(*) as total_records,
  MIN("Timestamp") as oldest_record,
  MAX("Timestamp") as newest_record
FROM "AuditLogs";

-- 5. Policy status
SELECT * FROM timescaledb_information.jobs;

-- 6. Continuous aggregate status
SELECT * FROM timescaledb_information.continuous_aggregates;

-- 7. 7-year query test
SELECT COUNT(*)
FROM "AuditLogs"
WHERE "Timestamp" >= NOW() - INTERVAL '2555 days';
```

---

**Document Version**: 1.0
**Last Updated**: 2024-12-28
**Next Review**: Quarterly
**Owner**: IT Operations
