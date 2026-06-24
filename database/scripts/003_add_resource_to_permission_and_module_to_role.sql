-- 003_add_resource_to_permission_and_module_to_role.sql
--
-- Categorizes Permissions and Roles by Module/Resource lookups (in KSS_Common).
--   * Permission gains ResourceId uniqueidentifier NOT NULL (FK by value, cross-DB).
--   * Role gains ModuleId uniqueidentifier NULL (NULL = global / cross-module role).
--
-- Backfill: parses Permission.Name (format "<Module>.<Resource>.<Action>")
-- and resolves to KSS_Common.Module.Id + KSS_Common.Resource.Id.
-- Any module/resource referenced by an existing permission but missing from Common
-- is auto-created with a fresh Guid v7 so no Permission row is left orphaned.
--
-- Apply to KSS_Auth_Prod and KSS_Auth_Dev (KSS_Common_* must already have migration 011 applied).

SET QUOTED_IDENTIFIER ON;
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @system_user UNIQUEIDENTIFIER = '00000000-0000-0000-0000-000000000001';
DECLARE @now DATETIME2 = SYSUTCDATETIME();
DECLARE @fa SMALLINT = 12;
DECLARE @en SMALLINT = 10;

BEGIN TRANSACTION;

-- ── Step 1: Add columns
IF COL_LENGTH('dbo.Permission', 'ResourceId') IS NULL
    ALTER TABLE dbo.Permission ADD ResourceId UNIQUEIDENTIFIER NULL;

IF COL_LENGTH('dbo.[Role]', 'ModuleId') IS NULL
    ALTER TABLE dbo.[Role] ADD ModuleId UNIQUEIDENTIFIER NULL;

-- ── Step 2: Pick the right Common DB based on environment
DECLARE @commonDb SYSNAME =
    CASE WHEN DB_NAME() = 'KSS_Auth_Prod' THEN N'KSS_Common_Prod'
         ELSE N'KSS_Common_Dev'
    END;
DECLARE @sql NVARCHAR(MAX);

-- ── Step 3: Build a temp catalog of existing Modules and Resources from Common
CREATE TABLE #CommonModule (Id UNIQUEIDENTIFIER NOT NULL, CodeLower VARCHAR(30) NOT NULL);
CREATE TABLE #CommonResource (Id UNIQUEIDENTIFIER NOT NULL, ModuleId UNIQUEIDENTIFIER NOT NULL, CodeLower VARCHAR(50) NOT NULL);

SET @sql = N'INSERT INTO #CommonModule (Id, CodeLower) SELECT Id, LOWER(Code) FROM ' + QUOTENAME(@commonDb) + N'.dbo.Module;';
EXEC sp_executesql @sql;

SET @sql = N'INSERT INTO #CommonResource (Id, ModuleId, CodeLower) SELECT Id, ModuleId, LOWER(Code) FROM ' + QUOTENAME(@commonDb) + N'.dbo.Resource;';
EXEC sp_executesql @sql;

-- ── Step 4: Distill distinct (module, resource) pairs from Permission.Name
-- Format expected: "<Module>.<Resource>.<Action>"
WITH parsed AS (
    SELECT
        p.Id AS PermissionId,
        LOWER(LEFT(p.Name, CHARINDEX('.', p.Name) - 1)) AS ModuleCodeLower,
        LOWER(SUBSTRING(p.Name,
                        CHARINDEX('.', p.Name) + 1,
                        CHARINDEX('.', p.Name, CHARINDEX('.', p.Name) + 1) - CHARINDEX('.', p.Name) - 1)) AS ResourceCodeLower
    FROM dbo.Permission p
    WHERE p.Name LIKE '%.%.%'
)
SELECT DISTINCT ModuleCodeLower, ResourceCodeLower
INTO #DistinctMR
FROM parsed;

-- ── Step 5: Auto-create missing Modules in Common
DECLARE @newModuleSql NVARCHAR(MAX) = N'';
SELECT @newModuleSql = @newModuleSql +
    N'INSERT INTO ' + QUOTENAME(@commonDb) + N'.dbo.Module (Id, Code, CreatedBy, CreatedAt) VALUES (NEWID(), ''' + d.ModuleCodeLower + N''', ''' + CAST(@system_user AS VARCHAR(36)) + N''', ''' + CONVERT(VARCHAR(33), @now, 126) + N''');' + CHAR(10)
FROM (SELECT DISTINCT ModuleCodeLower FROM #DistinctMR) d
WHERE NOT EXISTS (SELECT 1 FROM #CommonModule cm WHERE cm.CodeLower = d.ModuleCodeLower);

IF LEN(@newModuleSql) > 0
BEGIN
    EXEC sp_executesql @newModuleSql;
    -- Refresh the temp catalog
    DELETE FROM #CommonModule;
    SET @sql = N'INSERT INTO #CommonModule (Id, CodeLower) SELECT Id, LOWER(Code) FROM ' + QUOTENAME(@commonDb) + N'.dbo.Module;';
    EXEC sp_executesql @sql;
END

-- ── Step 6: Auto-create missing Resources
DECLARE @newResSql NVARCHAR(MAX) = N'';
SELECT @newResSql = @newResSql +
    N'INSERT INTO ' + QUOTENAME(@commonDb) + N'.dbo.Resource (Id, ModuleId, Code, CreatedBy, CreatedAt) VALUES (NEWID(), ''' + CAST(cm.Id AS VARCHAR(36)) + N''', ''' + d.ResourceCodeLower + N''', ''' + CAST(@system_user AS VARCHAR(36)) + N''', ''' + CONVERT(VARCHAR(33), @now, 126) + N''');' + CHAR(10)
FROM #DistinctMR d
JOIN #CommonModule cm ON cm.CodeLower = d.ModuleCodeLower
WHERE NOT EXISTS (
    SELECT 1 FROM #CommonResource cr
    WHERE cr.ModuleId = cm.Id AND cr.CodeLower = d.ResourceCodeLower
);

IF LEN(@newResSql) > 0
BEGIN
    EXEC sp_executesql @newResSql;
    -- Refresh the temp catalog
    DELETE FROM #CommonResource;
    SET @sql = N'INSERT INTO #CommonResource (Id, ModuleId, CodeLower) SELECT Id, ModuleId, LOWER(Code) FROM ' + QUOTENAME(@commonDb) + N'.dbo.Resource;';
    EXEC sp_executesql @sql;
END

-- ── Step 7: Backfill Permission.ResourceId (dynamic SQL — defers column binding
-- until after the ALTER TABLE ADD ResourceId in Step 1 has taken effect).
EXEC sp_executesql N'
UPDATE p
SET p.ResourceId = cr.Id
FROM dbo.Permission p
CROSS APPLY (
    SELECT
        LOWER(LEFT(p.Name, CHARINDEX(''.'', p.Name) - 1)) AS ModuleCodeLower,
        LOWER(SUBSTRING(p.Name,
                        CHARINDEX(''.'', p.Name) + 1,
                        CHARINDEX(''.'', p.Name, CHARINDEX(''.'', p.Name) + 1) - CHARINDEX(''.'', p.Name) - 1)) AS ResourceCodeLower
) parsed
JOIN #CommonModule cm   ON cm.CodeLower = parsed.ModuleCodeLower
JOIN #CommonResource cr ON cr.ModuleId  = cm.Id AND cr.CodeLower = parsed.ResourceCodeLower
WHERE p.Name LIKE ''%.%.%'';
';

-- ── Step 8: Set NOT NULL on ResourceId
DECLARE @nullCount INT;
EXEC sp_executesql N'SELECT @cnt = COUNT(*) FROM dbo.Permission WHERE ResourceId IS NULL', N'@cnt INT OUTPUT', @cnt = @nullCount OUTPUT;
IF @nullCount > 0
BEGIN
    DECLARE @msg NVARCHAR(200) = N'Cannot set NOT NULL on Permission.ResourceId — ' + CAST(@nullCount AS NVARCHAR) + N' rows still NULL';
    THROW 51001, @msg, 1;
END

ALTER TABLE dbo.Permission ALTER COLUMN ResourceId UNIQUEIDENTIFIER NOT NULL;

DROP TABLE #CommonModule;
DROP TABLE #CommonResource;
DROP TABLE #DistinctMR;

COMMIT TRANSACTION;

PRINT '003_add_resource_to_permission_and_module_to_role.sql applied successfully.';
