# RBAC/ABAC Database Migration Guide

## Overview

This guide covers the database migration process for Feature 53 (RBAC/ABAC implementation).

## Migration Summary

**Migration Name**: `AddRBACSupport`
**Tables Added**: 4 new tables
**Total Columns**: ~60 columns across all tables
**Indexes**: 15+ indexes for performance
**Initial Data**: 5 system roles + ~200 role-permission mappings

## Tables Created

### 1. Roles
Primary table for role definitions.

```sql
CREATE TABLE "Roles" (
    "Id" UUID PRIMARY KEY,
    "RoleName" VARCHAR(50) NOT NULL,
    "DisplayName" VARCHAR(100) NOT NULL,
    "Description" VARCHAR(500) NOT NULL,
    "IsSystemRole" BOOLEAN NOT NULL DEFAULT false,
    "CreatedAt" TIMESTAMP NOT NULL,
    "CreatedBy" VARCHAR(255) NOT NULL,
    "UpdatedAt" TIMESTAMP NULL,
    "UpdatedBy" VARCHAR(255) NULL,
    "IsDeleted" BOOLEAN NOT NULL DEFAULT false,
    "DeletedAt" TIMESTAMP NULL,
    "DeletedBy" VARCHAR(255) NULL,
    "RowVersion" BYTEA NOT NULL
);

CREATE UNIQUE INDEX "IX_Roles_RoleName" ON "Roles" ("RoleName");
CREATE INDEX "IX_Roles_IsSystemRole" ON "Roles" ("IsSystemRole");
CREATE INDEX "IX_Roles_IsDeleted" ON "Roles" ("IsDeleted");
```

**Initial Data**: 5 system roles
- Admin (89 permissions)
- Doctor (31 permissions)
- Nurse (15 permissions)
- Staff (6 permissions)
- Patient (10 permissions)

### 2. RolePermissions
Many-to-many mapping between roles and permissions.

```sql
CREATE TABLE "RolePermissions" (
    "Id" UUID PRIMARY KEY,
    "RoleId" UUID NOT NULL,
    "Permission" VARCHAR(100) NOT NULL,
    "CreatedAt" TIMESTAMP NOT NULL,
    "CreatedBy" VARCHAR(255) NOT NULL,
    "UpdatedAt" TIMESTAMP NULL,
    "UpdatedBy" VARCHAR(255) NULL,
    "IsDeleted" BOOLEAN NOT NULL DEFAULT false,
    "DeletedAt" TIMESTAMP NULL,
    "DeletedBy" VARCHAR(255) NULL,
    "RowVersion" BYTEA NOT NULL,
    FOREIGN KEY ("RoleId") REFERENCES "Roles"("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_RolePermissions_RoleId" ON "RolePermissions" ("RoleId");
CREATE INDEX "IX_RolePermissions_RoleId_Permission" ON "RolePermissions" ("RoleId", "Permission");
```

**Initial Data**: ~200 role-permission mappings based on RolePermissionMatrix

### 3. UserRoleAssignments
Tracks user role assignments with temporal validity.

```sql
CREATE TABLE "UserRoleAssignments" (
    "Id" UUID PRIMARY KEY,
    "UserId" UUID NOT NULL,
    "Role" VARCHAR(50) NOT NULL,
    "EffectiveFrom" TIMESTAMP NOT NULL,
    "EffectiveTo" TIMESTAMP NULL,
    "AssignmentReason" VARCHAR(500) NULL,
    "CreatedAt" TIMESTAMP NOT NULL,
    "CreatedBy" VARCHAR(255) NOT NULL,
    "UpdatedAt" TIMESTAMP NULL,
    "UpdatedBy" VARCHAR(255) NULL,
    "IsDeleted" BOOLEAN NOT NULL DEFAULT false,
    "DeletedAt" TIMESTAMP NULL,
    "DeletedBy" VARCHAR(255) NULL,
    "RowVersion" BYTEA NOT NULL,
    FOREIGN KEY ("UserId") REFERENCES "Users"("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_UserRoleAssignments_UserId" ON "UserRoleAssignments" ("UserId");
CREATE INDEX "IX_UserRoleAssignments_UserId_Role_EffectiveFrom"
    ON "UserRoleAssignments" ("UserId", "Role", "EffectiveFrom");
```

**Initial Data**: None (populated as users are assigned roles)

### 4. ResourceAuthorizations
Resource-level permissions for ABAC.

```sql
CREATE TABLE "ResourceAuthorizations" (
    "Id" UUID PRIMARY KEY,
    "UserId" UUID NOT NULL,
    "ResourceType" VARCHAR(50) NOT NULL,
    "ResourceId" UUID NOT NULL,
    "Permission" VARCHAR(100) NOT NULL,
    "EffectiveFrom" TIMESTAMP NOT NULL,
    "EffectiveTo" TIMESTAMP NULL,
    "Reason" VARCHAR(500) NULL,
    "CreatedAt" TIMESTAMP NOT NULL,
    "CreatedBy" VARCHAR(255) NOT NULL,
    "UpdatedAt" TIMESTAMP NULL,
    "UpdatedBy" VARCHAR(255) NULL,
    "IsDeleted" BOOLEAN NOT NULL DEFAULT false,
    "DeletedAt" TIMESTAMP NULL,
    "DeletedBy" VARCHAR(255) NULL,
    "RowVersion" BYTEA NOT NULL,
    FOREIGN KEY ("UserId") REFERENCES "Users"("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_ResourceAuthorizations_UserId" ON "ResourceAuthorizations" ("UserId");
CREATE INDEX "IX_ResourceAuthorizations_ResourceId" ON "ResourceAuthorizations" ("ResourceId");
CREATE INDEX "IX_ResourceAuthorizations_ResourceType_ResourceId"
    ON "ResourceAuthorizations" ("ResourceType", "ResourceId");
CREATE INDEX "IX_ResourceAuthorizations_UserId_ResourceType_ResourceId_Permission"
    ON "ResourceAuthorizations" ("UserId", "ResourceType", "ResourceId", "Permission");
```

**Initial Data**: None (populated as resource-level permissions are granted)

## Creating the Migration

### Prerequisites
- .NET 10 SDK installed
- EF Core tools installed: `dotnet tool install --global dotnet-ef`
- PostgreSQL database accessible

### Step 1: Create Migration

```bash
cd D:\code-source\EMR\source\emr-api\src\EMR.Infrastructure
dotnet ef migrations add AddRBACSupport --startup-project ../EMR.Api --context ApplicationDbContext
```

This creates three files:
1. `YYYYMMDDHHMMSS_AddRBACSupport.cs` - Migration up/down methods
2. `YYYYMMDDHHMMSS_AddRBACSupport.Designer.cs` - Migration metadata
3. `ApplicationDbContextModelSnapshot.cs` - Updated snapshot

### Step 2: Review Migration

Inspect the generated migration file to ensure:
- All 4 tables are created
- All indexes are defined
- Foreign keys are set up correctly
- Column types match specifications

### Step 3: Apply Migration

#### Automatic (Recommended for Development)
The migration is automatically applied on application startup via `DatabaseExtensions.InitializeDatabaseAsync()`.

#### Manual (Production)
```bash
cd D:\code-source\EMR\source\emr-api\src\EMR.Infrastructure
dotnet ef database update --startup-project ../EMR.Api --context ApplicationDbContext
```

### Step 4: Verify Migration

```sql
-- Check if tables exist
SELECT table_name FROM information_schema.tables
WHERE table_schema = 'public'
AND table_name IN ('Roles', 'RolePermissions', 'UserRoleAssignments', 'ResourceAuthorizations');

-- Check role data
SELECT "RoleName", "DisplayName", "IsSystemRole" FROM "Roles";

-- Check permission counts per role
SELECT r."RoleName", COUNT(rp."Permission") as PermissionCount
FROM "Roles" r
LEFT JOIN "RolePermissions" rp ON r."Id" = rp."RoleId"
WHERE rp."IsDeleted" = false
GROUP BY r."RoleName";
```

Expected output:
```
Admin       | 89
Doctor      | 31
Nurse       | 15
Staff       | 6
Patient     | 10
```

## Data Seeding

### Automatic Seeding
On first application start, the `RoleSeeder` automatically:
1. Checks if roles exist
2. If not, creates 5 system roles
3. Assigns default permissions from `RolePermissionMatrix`
4. Logs completion

### Manual Seeding (if needed)
The seeder is idempotent and safe to run multiple times:

```csharp
using var scope = app.Services.CreateScope();
var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
var logger = scope.ServiceProvider.GetRequiredService<ILogger<RoleSeeder>>();
var seeder = new RoleSeeder(context, logger);
await seeder.SeedAsync();
```

### Verify Seeding
Check application logs for:
```
[Information] Starting role seeding process
[Information] Successfully seeded 5 roles
[Information] Assigned 89 permissions to role Administrator
[Information] Assigned 31 permissions to role Doctor
[Information] Assigned 15 permissions to role Nurse
[Information] Assigned 6 permissions to role Staff
[Information] Assigned 10 permissions to role Patient
[Information] Role seeding completed successfully
```

## Rolling Back

### Rollback to Previous Migration
```bash
dotnet ef database update PreviousMigrationName --startup-project ../EMR.Api
```

### Remove Migration (before applying)
```bash
dotnet ef migrations remove --startup-project ../EMR.Api
```

### Manual Rollback
If needed, drop tables manually:

```sql
-- Drop tables in reverse dependency order
DROP TABLE IF EXISTS "ResourceAuthorizations";
DROP TABLE IF EXISTS "UserRoleAssignments";
DROP TABLE IF EXISTS "RolePermissions";
DROP TABLE IF EXISTS "Roles";
```

## Production Deployment Checklist

### Pre-Deployment
- [ ] Review migration SQL in generated files
- [ ] Test migration on development database
- [ ] Test migration on staging database
- [ ] Backup production database
- [ ] Schedule maintenance window (if needed)
- [ ] Notify users of potential downtime

### Deployment Steps
1. **Backup Database**
   ```bash
   pg_dump -h hostname -U username -d emr_db > backup_before_rbac_$(date +%Y%m%d).sql
   ```

2. **Apply Migration**
   ```bash
   dotnet ef database update --startup-project ../EMR.Api --context ApplicationDbContext
   ```

3. **Verify Tables Created**
   ```sql
   SELECT COUNT(*) FROM "Roles"; -- Should be 5
   SELECT COUNT(*) FROM "RolePermissions"; -- Should be ~200
   ```

4. **Start Application**
   - Application will run seeder on startup
   - Monitor logs for successful seeding

5. **Smoke Test**
   - Test role management endpoints
   - Test permission-protected endpoints with different roles
   - Verify authorization is working

### Post-Deployment
- [ ] Verify all roles seeded correctly
- [ ] Test permission checks are working
- [ ] Monitor authorization audit logs
- [ ] Check application performance
- [ ] Confirm no authorization errors in logs

## Troubleshooting

### Issue: Migration fails with "table already exists"
**Cause**: Migration was partially applied
**Solution**:
```sql
-- Check which tables exist
SELECT table_name FROM information_schema.tables
WHERE table_schema = 'public'
AND table_name LIKE '%Role%';

-- Drop existing tables and re-run migration
DROP TABLE IF EXISTS "ResourceAuthorizations";
DROP TABLE IF EXISTS "UserRoleAssignments";
DROP TABLE IF EXISTS "RolePermissions";
DROP TABLE IF EXISTS "Roles";
```

### Issue: Roles not seeding
**Cause**: Database connection issue or seeder error
**Solution**: Check application logs for errors, verify database connection

### Issue: Foreign key constraint violation
**Cause**: Attempting to delete a role with associated permissions
**Solution**: Use soft delete instead of hard delete, or cascade delete is already configured

### Issue: Permission checks failing after migration
**Cause**: Role claims not matching database roles
**Solution**: Verify JWT contains correct role claims, check User.Roles mapping

## Performance Considerations

### Index Usage
The migration creates several indexes for performance:
- **RoleName**: Fast lookup by role name
- **RoleId + Permission**: Fast permission checks
- **UserId + ResourceType + ResourceId + Permission**: Fast resource authorization
- **ResourceType + ResourceId**: Fast resource-based queries

### Query Performance
Expected query times (on properly indexed database):
- Role permission check: < 5ms
- Resource authorization check: < 10ms
- Get all permissions for user: < 20ms

### Monitoring
Monitor these queries for performance:
```sql
-- Slow query log
SELECT * FROM pg_stat_statements
WHERE query LIKE '%RolePermissions%' OR query LIKE '%ResourceAuthorizations%'
ORDER BY total_time DESC;
```

## Database Size Estimates

### Initial Size
- Roles: ~5 KB (5 roles)
- RolePermissions: ~20 KB (~200 records)
- UserRoleAssignments: Grows with users (estimate 1 KB per user)
- ResourceAuthorizations: Grows with assignments (estimate 500 bytes per assignment)

### Growth Projections
For 10,000 users:
- UserRoleAssignments: ~10 MB (avg 1 role per user)
- ResourceAuthorizations: ~50 MB (avg 100 resource assignments per user)

Total estimated growth: ~60 MB for 10,000 users (negligible)

## Backup Strategy

### Before Migration
```bash
# Full backup
pg_dump -h hostname -U username -d emr_db -F c > emr_backup_pre_rbac.dump

# Schema only backup
pg_dump -h hostname -U username -d emr_db --schema-only > emr_schema_pre_rbac.sql
```

### After Migration
```bash
# Backup new tables
pg_dump -h hostname -U username -d emr_db -t Roles -t RolePermissions \
  -t UserRoleAssignments -t ResourceAuthorizations > rbac_tables_backup.sql
```

## Maintenance

### Regular Tasks
1. **Monitor table sizes**: Check growth of ResourceAuthorizations
2. **Clean expired authorizations**: Soft-delete old resource authorizations
3. **Audit permission assignments**: Review who has what access
4. **Performance tuning**: Monitor and optimize slow queries

### Cleanup Script
```sql
-- Soft delete expired resource authorizations (older than 1 year)
UPDATE "ResourceAuthorizations"
SET "IsDeleted" = true,
    "DeletedAt" = NOW(),
    "DeletedBy" = 'System-Cleanup'
WHERE "EffectiveTo" < NOW() - INTERVAL '1 year'
AND "IsDeleted" = false;
```

## Support

For issues with migration:
1. Check application logs in `logs/emr-*.log`
2. Check PostgreSQL logs for database errors
3. Review migration files in `EMR.Infrastructure/Data/Migrations/`
4. Consult `RBAC_IMPLEMENTATION.md` for architecture details

## Summary

The RBAC migration adds 4 tables with proper indexing, foreign keys, and audit support. The migration is designed to be:
- **Safe**: Idempotent seeding, soft deletes
- **Performant**: Comprehensive indexing
- **Auditable**: Complete audit trail
- **Maintainable**: Clean schema with proper constraints

The migration should complete in seconds on most databases and requires no manual intervention beyond the standard deployment process.
