-- 007_simplify_permission_tables.sql
--
-- Removes audit clump and IsActive from dbo.Permission and
-- dbo.PermissionTranslation. These two tables are critical reference data
-- maintained only by a DBA via versioned migrations — there's no audit story
-- since end users / services never modify them.
--
-- Final schema:
--   Permission             : Id, Code, ModuleId, ResourceId
--   PermissionTranslation  : PermissionId, LanguageId, Name, Description
--
-- Apply to KSS_Auth_Prod and KSS_Auth_Dev.

SET QUOTED_IDENTIFIER ON;
SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;

-- ── Step 1: Stage data
SELECT Id, Code, ModuleId, ResourceId
INTO #PermBackup
FROM dbo.Permission;

SELECT PermissionId, LanguageId, Name, Description
INTO #PermTransBackup
FROM dbo.PermissionTranslation;

-- ── Step 2: Drop FK from RolePermission and the two tables (Translation cascades anyway)
ALTER TABLE dbo.RolePermission DROP CONSTRAINT FK_RolePermission_Permission;
DROP TABLE dbo.PermissionTranslation;
DROP TABLE dbo.Permission;

-- ── Step 3: Recreate Permission (slim — 4 cols)
CREATE TABLE dbo.Permission (
    Id          UNIQUEIDENTIFIER NOT NULL,
    Code        VARCHAR(100)     NOT NULL,
    ModuleId    UNIQUEIDENTIFIER NOT NULL,
    ResourceId  UNIQUEIDENTIFIER NOT NULL,
    CONSTRAINT PK_Permission PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT UQ_Permission_Code UNIQUE (Code)
);
CREATE NONCLUSTERED INDEX IX_Permission_ModuleId   ON dbo.Permission(ModuleId);
CREATE NONCLUSTERED INDEX IX_Permission_ResourceId ON dbo.Permission(ResourceId);

-- ── Step 4: Recreate PermissionTranslation (slim — 4 cols)
CREATE TABLE dbo.PermissionTranslation (
    PermissionId UNIQUEIDENTIFIER NOT NULL,
    LanguageId   SMALLINT         NOT NULL,
    Name         NVARCHAR(100)    NOT NULL,
    Description  NVARCHAR(200)    NULL,
    CONSTRAINT PK_PermissionTranslation PRIMARY KEY CLUSTERED (PermissionId, LanguageId),
    CONSTRAINT FK_PermissionTranslation_Permission
        FOREIGN KEY (PermissionId) REFERENCES dbo.Permission(Id) ON DELETE CASCADE
);

-- ── Step 5: Re-insert (dynamic SQL — defers binding to the new schema)
EXEC sp_executesql N'
INSERT INTO dbo.Permission (Id, Code, ModuleId, ResourceId)
SELECT Id, Code, ModuleId, ResourceId FROM #PermBackup;

INSERT INTO dbo.PermissionTranslation (PermissionId, LanguageId, Name, Description)
SELECT PermissionId, LanguageId, Name, Description FROM #PermTransBackup;
';

-- ── Step 6: Re-add RolePermission FK
ALTER TABLE dbo.RolePermission
    ADD CONSTRAINT FK_RolePermission_Permission
    FOREIGN KEY (PermissionId) REFERENCES dbo.Permission(Id) ON DELETE CASCADE;

DROP TABLE #PermBackup;
DROP TABLE #PermTransBackup;

COMMIT TRANSACTION;

SELECT 'permissions',         COUNT(*) FROM dbo.Permission;
SELECT 'translations',        COUNT(*) FROM dbo.PermissionTranslation;
SELECT 'role_permissions',    COUNT(*) FROM dbo.RolePermission;

PRINT '007_simplify_permission_tables.sql applied successfully.';
