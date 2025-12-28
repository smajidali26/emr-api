using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EMR.Infrastructure.Data.Migrations
{
    /// <summary>
    /// Migration to convert AuditLogs table to TimescaleDB hypertable
    ///
    /// HIPAA COMPLIANCE:
    /// - 7-year retention policy (2,555 days) per 45 CFR 164.530(j)
    /// - Compression after 30 days for storage optimization
    /// - Continuous aggregates for compliance reporting
    ///
    /// PREREQUISITES:
    /// - TimescaleDB extension must be installed on PostgreSQL server
    /// - Run: CREATE EXTENSION IF NOT EXISTS timescaledb;
    /// </summary>
    public partial class TimescaleDbAuditLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Step 1: Ensure TimescaleDB extension is available
            migrationBuilder.Sql(@"
                CREATE EXTENSION IF NOT EXISTS timescaledb CASCADE;
            ");

            // Step 2: Backup existing data and recreate table with composite primary key
            // TimescaleDB requires the time partitioning column in the primary key
            migrationBuilder.Sql(@"
                -- Rename existing table for backup
                ALTER TABLE ""AuditLogs"" RENAME TO ""AuditLogs_Backup"";
            ");

            // Step 3: Create new table with composite primary key
            migrationBuilder.Sql(@"
                CREATE TABLE ""AuditLogs"" (
                    ""Timestamp"" TIMESTAMP WITH TIME ZONE NOT NULL,
                    ""Id"" UUID NOT NULL,
                    ""EventType"" INTEGER NOT NULL,
                    ""UserId"" VARCHAR(100) NOT NULL,
                    ""Username"" VARCHAR(256),
                    ""ResourceType"" VARCHAR(100) NOT NULL,
                    ""ResourceId"" VARCHAR(100),
                    ""IpAddress"" VARCHAR(45),
                    ""UserAgent"" VARCHAR(500),
                    ""Action"" VARCHAR(500) NOT NULL,
                    ""Details"" VARCHAR(2000),
                    ""Success"" BOOLEAN NOT NULL,
                    ""ErrorMessage"" VARCHAR(2000),
                    ""HttpMethod"" VARCHAR(10),
                    ""RequestPath"" VARCHAR(500),
                    ""StatusCode"" INTEGER,
                    ""DurationMs"" BIGINT,
                    ""SessionId"" VARCHAR(100),
                    ""CorrelationId"" VARCHAR(100),
                    ""OldValues"" JSONB,
                    ""NewValues"" JSONB,
                    PRIMARY KEY (""Timestamp"", ""Id"")
                );
            ");

            // Step 4: Convert to hypertable with 1-month chunk interval
            migrationBuilder.Sql(@"
                SELECT create_hypertable(
                    '""AuditLogs""',
                    'Timestamp',
                    chunk_time_interval => INTERVAL '1 month',
                    if_not_exists => TRUE
                );
            ");

            // Step 5: Restore data from backup
            migrationBuilder.Sql(@"
                INSERT INTO ""AuditLogs"" (
                    ""Timestamp"", ""Id"", ""EventType"", ""UserId"", ""Username"",
                    ""ResourceType"", ""ResourceId"", ""IpAddress"", ""UserAgent"",
                    ""Action"", ""Details"", ""Success"", ""ErrorMessage"",
                    ""HttpMethod"", ""RequestPath"", ""StatusCode"", ""DurationMs"",
                    ""SessionId"", ""CorrelationId"", ""OldValues"", ""NewValues""
                )
                SELECT
                    ""Timestamp"", ""Id"", ""EventType"", ""UserId"", ""Username"",
                    ""ResourceType"", ""ResourceId"", ""IpAddress"", ""UserAgent"",
                    ""Action"", ""Details"", ""Success"", ""ErrorMessage"",
                    ""HttpMethod"", ""RequestPath"", ""StatusCode"", ""DurationMs"",
                    ""SessionId"", ""CorrelationId"", ""OldValues"", ""NewValues""
                FROM ""AuditLogs_Backup"";
            ");

            // Step 6: Drop backup table
            migrationBuilder.Sql(@"
                DROP TABLE ""AuditLogs_Backup"";
            ");

            // Step 7: Recreate indexes for common query patterns
            migrationBuilder.Sql(@"
                CREATE INDEX ""IX_AuditLogs_UserId"" ON ""AuditLogs"" (""UserId"");
                CREATE INDEX ""IX_AuditLogs_EventType"" ON ""AuditLogs"" (""EventType"");
                CREATE INDEX ""IX_AuditLogs_Resource"" ON ""AuditLogs"" (""ResourceType"", ""ResourceId"");
                CREATE INDEX ""IX_AuditLogs_IpAddress"" ON ""AuditLogs"" (""IpAddress"");
                CREATE INDEX ""IX_AuditLogs_SessionId"" ON ""AuditLogs"" (""SessionId"");
                CREATE INDEX ""IX_AuditLogs_CorrelationId"" ON ""AuditLogs"" (""CorrelationId"");
                CREATE INDEX ""IX_AuditLogs_Timestamp_EventType"" ON ""AuditLogs"" (""Timestamp"" DESC, ""EventType"");
                CREATE INDEX ""IX_AuditLogs_UserId_Timestamp"" ON ""AuditLogs"" (""UserId"", ""Timestamp"" DESC);
            ");

            // Step 8: Enable compression
            migrationBuilder.Sql(@"
                ALTER TABLE ""AuditLogs"" SET (
                    timescaledb.compress,
                    timescaledb.compress_segmentby = 'UserId, ResourceType',
                    timescaledb.compress_orderby = 'Timestamp DESC'
                );
            ");

            // Step 9: Add compression policy - compress chunks older than 30 days
            migrationBuilder.Sql(@"
                SELECT add_compression_policy('""AuditLogs""', INTERVAL '30 days');
            ");

            // Step 10: Add retention policy - 7 years (2,555 days) for HIPAA compliance
            migrationBuilder.Sql(@"
                SELECT add_retention_policy('""AuditLogs""', INTERVAL '2555 days');
            ");

            // Step 11: Create continuous aggregates for compliance dashboards

            // Daily summary aggregate
            migrationBuilder.Sql(@"
                CREATE MATERIALIZED VIEW audit_daily_summary
                WITH (timescaledb.continuous) AS
                SELECT
                    time_bucket('1 day', ""Timestamp"") AS bucket,
                    ""EventType"",
                    ""ResourceType"",
                    COUNT(*) AS total_events,
                    COUNT(*) FILTER (WHERE ""Success"" = true) AS successful_events,
                    COUNT(*) FILTER (WHERE ""Success"" = false) AS failed_events,
                    COUNT(DISTINCT ""UserId"") AS unique_users,
                    AVG(""DurationMs"") AS avg_duration_ms,
                    MAX(""DurationMs"") AS max_duration_ms
                FROM ""AuditLogs""
                GROUP BY bucket, ""EventType"", ""ResourceType""
                WITH NO DATA;

                SELECT add_continuous_aggregate_policy('audit_daily_summary',
                    start_offset => INTERVAL '3 days',
                    end_offset => INTERVAL '1 hour',
                    schedule_interval => INTERVAL '1 hour');
            ");

            // User activity aggregate
            migrationBuilder.Sql(@"
                CREATE MATERIALIZED VIEW audit_user_activity
                WITH (timescaledb.continuous) AS
                SELECT
                    time_bucket('1 hour', ""Timestamp"") AS bucket,
                    ""UserId"",
                    ""Username"",
                    COUNT(*) AS total_actions,
                    COUNT(DISTINCT ""ResourceType"") AS resource_types_accessed,
                    COUNT(*) FILTER (WHERE ""EventType"" = 0) AS view_count,
                    COUNT(*) FILTER (WHERE ""EventType"" = 1) AS create_count,
                    COUNT(*) FILTER (WHERE ""EventType"" = 2) AS update_count,
                    COUNT(*) FILTER (WHERE ""EventType"" = 3) AS delete_count,
                    COUNT(*) FILTER (WHERE ""Success"" = false) AS failed_actions
                FROM ""AuditLogs""
                GROUP BY bucket, ""UserId"", ""Username""
                WITH NO DATA;

                SELECT add_continuous_aggregate_policy('audit_user_activity',
                    start_offset => INTERVAL '2 hours',
                    end_offset => INTERVAL '30 minutes',
                    schedule_interval => INTERVAL '30 minutes');
            ");

            // Resource access aggregate
            migrationBuilder.Sql(@"
                CREATE MATERIALIZED VIEW audit_resource_access
                WITH (timescaledb.continuous) AS
                SELECT
                    time_bucket('1 day', ""Timestamp"") AS bucket,
                    ""ResourceType"",
                    ""ResourceId"",
                    COUNT(*) AS total_accesses,
                    COUNT(DISTINCT ""UserId"") AS unique_users,
                    COUNT(*) FILTER (WHERE ""EventType"" = 0) AS view_count,
                    COUNT(*) FILTER (WHERE ""EventType"" = 2) AS modification_count,
                    MAX(""Timestamp"") AS last_accessed
                FROM ""AuditLogs""
                WHERE ""ResourceId"" IS NOT NULL
                GROUP BY bucket, ""ResourceType"", ""ResourceId""
                WITH NO DATA;

                SELECT add_continuous_aggregate_policy('audit_resource_access',
                    start_offset => INTERVAL '3 days',
                    end_offset => INTERVAL '1 hour',
                    schedule_interval => INTERVAL '1 hour');
            ");

            // HIPAA compliance metrics aggregate
            migrationBuilder.Sql(@"
                CREATE MATERIALIZED VIEW audit_compliance_metrics
                WITH (timescaledb.continuous) AS
                SELECT
                    time_bucket('1 day', ""Timestamp"") AS bucket,
                    COUNT(*) AS total_audit_events,
                    COUNT(*) FILTER (WHERE ""ResourceType"" = 'Patient') AS phi_access_count,
                    COUNT(*) FILTER (WHERE ""EventType"" = 8) AS access_denied_count,
                    COUNT(*) FILTER (WHERE ""EventType"" IN (4, 5)) AS auth_event_count,
                    COUNT(*) FILTER (WHERE ""EventType"" = 5) AS failed_login_count,
                    COUNT(*) FILTER (WHERE ""EventType"" IN (6, 7)) AS export_print_count,
                    COUNT(DISTINCT ""UserId"") AS active_users,
                    COUNT(DISTINCT ""IpAddress"") AS unique_ip_addresses,
                    COUNT(DISTINCT ""SessionId"") AS unique_sessions
                FROM ""AuditLogs""
                GROUP BY bucket
                WITH NO DATA;

                SELECT add_continuous_aggregate_policy('audit_compliance_metrics',
                    start_offset => INTERVAL '3 days',
                    end_offset => INTERVAL '1 hour',
                    schedule_interval => INTERVAL '1 hour');
            ");

            // Step 12: Create function to verify HIPAA retention compliance
            migrationBuilder.Sql(@"
                CREATE OR REPLACE FUNCTION check_audit_retention_compliance()
                RETURNS TABLE (
                    earliest_record TIMESTAMP WITH TIME ZONE,
                    latest_record TIMESTAMP WITH TIME ZONE,
                    total_records BIGINT,
                    retention_days INTEGER,
                    is_compliant BOOLEAN,
                    compliance_message TEXT
                ) AS $$
                DECLARE
                    min_ts TIMESTAMP WITH TIME ZONE;
                    max_ts TIMESTAMP WITH TIME ZONE;
                    record_count BIGINT;
                    days_retained INTEGER;
                BEGIN
                    SELECT MIN(""Timestamp""), MAX(""Timestamp""), COUNT(*)
                    INTO min_ts, max_ts, record_count
                    FROM ""AuditLogs"";

                    IF min_ts IS NOT NULL THEN
                        days_retained := EXTRACT(DAY FROM (max_ts - min_ts));
                    ELSE
                        days_retained := 0;
                    END IF;

                    RETURN QUERY
                    SELECT
                        min_ts,
                        max_ts,
                        record_count,
                        days_retained,
                        days_retained <= 2555 AS is_compliant,
                        CASE
                            WHEN record_count = 0 THEN 'No audit records found'
                            WHEN days_retained <= 2555 THEN 'HIPAA compliant - records within 7-year retention'
                            ELSE 'WARNING: Records older than 7 years detected'
                        END;
                END;
                $$ LANGUAGE plpgsql;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove continuous aggregates
            migrationBuilder.Sql(@"
                DROP MATERIALIZED VIEW IF EXISTS audit_compliance_metrics CASCADE;
                DROP MATERIALIZED VIEW IF EXISTS audit_resource_access CASCADE;
                DROP MATERIALIZED VIEW IF EXISTS audit_user_activity CASCADE;
                DROP MATERIALIZED VIEW IF EXISTS audit_daily_summary CASCADE;
            ");

            // Remove compliance function
            migrationBuilder.Sql(@"
                DROP FUNCTION IF EXISTS check_audit_retention_compliance();
            ");

            // Remove policies (must be done before converting back)
            migrationBuilder.Sql(@"
                SELECT remove_retention_policy('""AuditLogs""', if_exists => true);
                SELECT remove_compression_policy('""AuditLogs""', if_exists => true);
            ");

            // Backup hypertable data
            migrationBuilder.Sql(@"
                CREATE TABLE ""AuditLogs_Backup"" AS SELECT * FROM ""AuditLogs"";
            ");

            // Drop hypertable
            migrationBuilder.Sql(@"
                DROP TABLE ""AuditLogs"" CASCADE;
            ");

            // Recreate as regular table with original primary key
            migrationBuilder.Sql(@"
                CREATE TABLE ""AuditLogs"" (
                    ""Id"" UUID PRIMARY KEY,
                    ""EventType"" INTEGER NOT NULL,
                    ""UserId"" VARCHAR(100) NOT NULL,
                    ""Username"" VARCHAR(256),
                    ""Timestamp"" TIMESTAMP WITH TIME ZONE NOT NULL,
                    ""ResourceType"" VARCHAR(100) NOT NULL,
                    ""ResourceId"" VARCHAR(100),
                    ""IpAddress"" VARCHAR(45),
                    ""UserAgent"" VARCHAR(500),
                    ""Action"" VARCHAR(500) NOT NULL,
                    ""Details"" VARCHAR(2000),
                    ""Success"" BOOLEAN NOT NULL,
                    ""ErrorMessage"" VARCHAR(2000),
                    ""HttpMethod"" VARCHAR(10),
                    ""RequestPath"" VARCHAR(500),
                    ""StatusCode"" INTEGER,
                    ""DurationMs"" BIGINT,
                    ""SessionId"" VARCHAR(100),
                    ""CorrelationId"" VARCHAR(100),
                    ""OldValues"" JSONB,
                    ""NewValues"" JSONB
                );
            ");

            // Restore data
            migrationBuilder.Sql(@"
                INSERT INTO ""AuditLogs""
                SELECT * FROM ""AuditLogs_Backup"";
            ");

            // Drop backup
            migrationBuilder.Sql(@"
                DROP TABLE ""AuditLogs_Backup"";
            ");

            // Recreate original indexes
            migrationBuilder.Sql(@"
                CREATE INDEX ""IX_AuditLogs_UserId"" ON ""AuditLogs"" (""UserId"");
                CREATE INDEX ""IX_AuditLogs_Timestamp"" ON ""AuditLogs"" (""Timestamp"");
                CREATE INDEX ""IX_AuditLogs_EventType"" ON ""AuditLogs"" (""EventType"");
                CREATE INDEX ""IX_AuditLogs_Resource"" ON ""AuditLogs"" (""ResourceType"", ""ResourceId"");
                CREATE INDEX ""IX_AuditLogs_IpAddress"" ON ""AuditLogs"" (""IpAddress"");
                CREATE INDEX ""IX_AuditLogs_SessionId"" ON ""AuditLogs"" (""SessionId"");
                CREATE INDEX ""IX_AuditLogs_CorrelationId"" ON ""AuditLogs"" (""CorrelationId"");
                CREATE INDEX ""IX_AuditLogs_Timestamp_EventType"" ON ""AuditLogs"" (""Timestamp"", ""EventType"");
                CREATE INDEX ""IX_AuditLogs_UserId_Timestamp"" ON ""AuditLogs"" (""UserId"", ""Timestamp"");
            ");
        }
    }
}
