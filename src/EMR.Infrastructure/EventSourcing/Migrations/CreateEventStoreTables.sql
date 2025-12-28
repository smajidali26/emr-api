-- Migration: Create Event Store Tables
-- Description: Creates tables for Event Sourcing infrastructure
-- Database: PostgreSQL

-- =====================================================
-- Event Store Table
-- =====================================================
CREATE TABLE IF NOT EXISTS "EventStore" (
    "Id" BIGSERIAL PRIMARY KEY,
    "EventId" UUID NOT NULL,
    "AggregateId" UUID NOT NULL,
    "AggregateType" VARCHAR(200) NOT NULL,
    "EventType" VARCHAR(500) NOT NULL,
    "Version" INTEGER NOT NULL,
    "EventVersion" INTEGER NOT NULL DEFAULT 1,
    "EventData" JSONB NOT NULL,
    "Metadata" JSONB,
    "OccurredAt" TIMESTAMP WITHOUT TIME ZONE NOT NULL,
    "PersistedAt" TIMESTAMP WITHOUT TIME ZONE NOT NULL,
    "UserId" VARCHAR(100),
    "CorrelationId" VARCHAR(100),
    "CausationId" VARCHAR(100),
    "SequenceNumber" BIGSERIAL NOT NULL
);

-- Create indexes for Event Store
CREATE UNIQUE INDEX IF NOT EXISTS "IX_EventStore_EventId"
    ON "EventStore" ("EventId");

CREATE UNIQUE INDEX IF NOT EXISTS "IX_EventStore_AggregateId_Version"
    ON "EventStore" ("AggregateId", "Version");

CREATE INDEX IF NOT EXISTS "IX_EventStore_OccurredAt"
    ON "EventStore" ("OccurredAt");

CREATE INDEX IF NOT EXISTS "IX_EventStore_CorrelationId"
    ON "EventStore" ("CorrelationId");

CREATE INDEX IF NOT EXISTS "IX_EventStore_EventType"
    ON "EventStore" ("EventType");

CREATE INDEX IF NOT EXISTS "IX_EventStore_SequenceNumber"
    ON "EventStore" ("SequenceNumber");

-- Add comment to Event Store table
COMMENT ON TABLE "EventStore" IS 'Stores all domain events for event sourcing';
COMMENT ON COLUMN "EventStore"."EventId" IS 'Unique identifier for the event';
COMMENT ON COLUMN "EventStore"."AggregateId" IS 'ID of the aggregate that generated this event';
COMMENT ON COLUMN "EventStore"."Version" IS 'Version of the aggregate after this event';
COMMENT ON COLUMN "EventStore"."SequenceNumber" IS 'Global sequence number for event ordering';

-- =====================================================
-- Snapshots Table
-- =====================================================
CREATE TABLE IF NOT EXISTS "Snapshots" (
    "Id" BIGSERIAL PRIMARY KEY,
    "AggregateId" UUID NOT NULL,
    "AggregateType" VARCHAR(200) NOT NULL,
    "Version" INTEGER NOT NULL,
    "SnapshotData" JSONB NOT NULL,
    "CreatedAt" TIMESTAMP WITHOUT TIME ZONE NOT NULL,
    "SnapshotType" VARCHAR(500) NOT NULL
);

-- Create indexes for Snapshots
CREATE INDEX IF NOT EXISTS "IX_Snapshots_AggregateId_Version"
    ON "Snapshots" ("AggregateId", "Version");

-- Add comment to Snapshots table
COMMENT ON TABLE "Snapshots" IS 'Stores aggregate snapshots for performance optimization';
COMMENT ON COLUMN "Snapshots"."Version" IS 'Aggregate version when snapshot was taken';

-- =====================================================
-- Outbox Messages Table
-- =====================================================
CREATE TABLE IF NOT EXISTS "OutboxMessages" (
    "Id" BIGSERIAL PRIMARY KEY,
    "EventId" UUID NOT NULL,
    "EventType" VARCHAR(500) NOT NULL,
    "EventData" JSONB NOT NULL,
    "OccurredAt" TIMESTAMP WITHOUT TIME ZONE NOT NULL,
    "CreatedAt" TIMESTAMP WITHOUT TIME ZONE NOT NULL,
    "ProcessedAt" TIMESTAMP WITHOUT TIME ZONE,
    "IsProcessed" BOOLEAN NOT NULL DEFAULT FALSE,
    "ProcessingAttempts" INTEGER NOT NULL DEFAULT 0,
    "LastError" VARCHAR(2000),
    "NextRetryAt" TIMESTAMP WITHOUT TIME ZONE,
    "CorrelationId" VARCHAR(100)
);

-- Create indexes for Outbox Messages
CREATE INDEX IF NOT EXISTS "IX_OutboxMessages_IsProcessed_CreatedAt"
    ON "OutboxMessages" ("IsProcessed", "CreatedAt");

CREATE INDEX IF NOT EXISTS "IX_OutboxMessages_IsProcessed_NextRetryAt"
    ON "OutboxMessages" ("IsProcessed", "NextRetryAt");

CREATE INDEX IF NOT EXISTS "IX_OutboxMessages_CorrelationId"
    ON "OutboxMessages" ("CorrelationId");

-- Add comment to Outbox Messages table
COMMENT ON TABLE "OutboxMessages" IS 'Implements transactional outbox pattern for reliable event publishing';
COMMENT ON COLUMN "OutboxMessages"."IsProcessed" IS 'Whether the message has been successfully published';
COMMENT ON COLUMN "OutboxMessages"."ProcessingAttempts" IS 'Number of attempts to process this message';

-- =====================================================
-- Grant permissions (adjust as needed)
-- =====================================================
-- GRANT SELECT, INSERT ON "EventStore" TO emr_app_user;
-- GRANT SELECT, INSERT, UPDATE, DELETE ON "Snapshots" TO emr_app_user;
-- GRANT SELECT, INSERT, UPDATE ON "OutboxMessages" TO emr_app_user;
