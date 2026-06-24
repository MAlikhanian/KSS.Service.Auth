-- 005_reorder_permission_columns.sql
--
-- Drop+recreate dbo.Permission to fix column order, add ModuleId (denormalized
-- for filtering/sorting), and remove the legacy Group column (now redundant
-- since ModuleId/ResourceId carry typed FKs into Common DB).
--
-- Final column order (12 cols, canonical):
--   Id, Name, Description, ModuleId, ResourceId,
--   CreatedBy, CreatedAt, UpdatedBy, UpdatedAt, DeletedBy, DeletedAt, IsActive
--
-- Apply to KSS_Auth_Prod and KSS_Auth_Dev.

SET QUOTED_IDENTIFIER ON;
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @system UNIQUEIDENTIFIER = '00000000-0000-0000-0000-000000000001';
DECLARE @now DATETIME2 = SYSUTCDATETIME();

DECLARE @commonDb SYSNAME =
    CASE WHEN DB_NAME() = 'KSS_Auth_Prod' THEN N'KSS_Common_Prod'
         ELSE N'KSS_Common_Dev'
    END;
DECLARE @sql NVARCHAR(MAX);

BEGIN TRANSACTION;

-- ── Step 1: Stage existing data
SELECT
    Id, Name, Description, ResourceId,
    CreatedBy, CreatedAt, UpdatedBy, UpdatedAt, DeletedBy, DeletedAt, IsActive
INTO #PermBackup
FROM dbo.Permission;

-- ── Step 2: Resolve ModuleId for each row (cross-DB join with Resource)
CREATE TABLE #PermModule (PermId UNIQUEIDENTIFIER NOT NULL, ModuleId UNIQUEIDENTIFIER NOT NULL);
SET @sql = N'
INSERT INTO #PermModule (PermId, ModuleId)
SELECT p.Id, r.ModuleId
FROM #PermBackup p
JOIN ' + QUOTENAME(@commonDb) + N'.dbo.Resource r ON r.Id = p.ResourceId;
';
EXEC sp_executesql @sql;

-- ── Step 3: Sanity check — every staged row resolved a ModuleId
DECLARE @missing INT = (SELECT COUNT(*) FROM #PermBackup b WHERE NOT EXISTS (SELECT 1 FROM #PermModule m WHERE m.PermId = b.Id));
IF @missing > 0
BEGIN
    DECLARE @msg NVARCHAR(200) = N'Cannot recreate Permission — ' + CAST(@missing AS NVARCHAR) + N' rows have unresolvable ModuleId';
    THROW 51002, @msg, 1;
END

-- ── Step 4: Drop incoming FK from RolePermission (the only one referencing Permission)
ALTER TABLE dbo.RolePermission DROP CONSTRAINT FK_RolePermission_Permission;

-- ── Step 5: Drop and recreate Permission with canonical column order
DROP TABLE dbo.Permission;

CREATE TABLE dbo.Permission (
    Id           UNIQUEIDENTIFIER NOT NULL,
    Name         NVARCHAR(100)    NOT NULL,
    Description  NVARCHAR(200)    NULL,
    ModuleId     UNIQUEIDENTIFIER NOT NULL,
    ResourceId   UNIQUEIDENTIFIER NOT NULL,
    CreatedBy    UNIQUEIDENTIFIER NOT NULL,
    CreatedAt    DATETIME2        NOT NULL,
    UpdatedBy    UNIQUEIDENTIFIER NULL,
    UpdatedAt    DATETIME2        NULL,
    DeletedBy    UNIQUEIDENTIFIER NULL,
    DeletedAt    DATETIME2        NULL,
    IsActive     BIT              NOT NULL CONSTRAINT DF_Permission_IsActive DEFAULT (1),
    CONSTRAINT PK_Permission PRIMARY KEY CLUSTERED (Id)
);

CREATE UNIQUE NONCLUSTERED INDEX IX_Permission_Name        ON dbo.Permission(Name);
CREATE NONCLUSTERED INDEX        IX_Permission_ModuleId    ON dbo.Permission(ModuleId);
CREATE NONCLUSTERED INDEX        IX_Permission_ResourceId  ON dbo.Permission(ResourceId);

-- ── Step 6: Re-insert from stage (dynamic SQL — defers binding to the new schema)
EXEC sp_executesql N'
INSERT INTO dbo.Permission
    (Id, Name, Description, ModuleId, ResourceId, CreatedBy, CreatedAt, UpdatedBy, UpdatedAt, DeletedBy, DeletedAt, IsActive)
SELECT
    b.Id, b.Name, b.Description, m.ModuleId, b.ResourceId,
    b.CreatedBy, b.CreatedAt, b.UpdatedBy, b.UpdatedAt, b.DeletedBy, b.DeletedAt, b.IsActive
FROM #PermBackup b
JOIN #PermModule m ON m.PermId = b.Id;
';

-- ── Step 7: Re-add FK from RolePermission
ALTER TABLE dbo.RolePermission
    ADD CONSTRAINT FK_RolePermission_Permission
    FOREIGN KEY (PermissionId) REFERENCES dbo.Permission(Id) ON DELETE CASCADE;

DROP TABLE #PermBackup;
DROP TABLE #PermModule;

COMMIT TRANSACTION;

-- Verify
SELECT 'permissions', COUNT(*) FROM dbo.Permission;
SELECT 'role_permissions', COUNT(*) FROM dbo.RolePermission;

PRINT '005_reorder_permission_columns.sql applied successfully.';
