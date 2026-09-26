-- 035_add_customerrisk_case_search_permissions.sql
--
-- Customer Risk System (CRS) module RBAC: three permission codes.
--   CustomerRisk.Case.Read      view risk cases
--   CustomerRisk.Case.Modify    create and edit risk cases
--   CustomerRisk.Search.Read    search and inquiry
-- No role and no grant: this script writes no RolePermission and no UserRole
-- row, SuperAdmin included. Granting the codes is a separate change.
--
-- Scope switch, the sqlcmd variable WithTranslations, must be exactly 0 or 1:
--   1  Permission x3 plus PermissionTranslation x6 (en + fa for each code), the
--      convention every other permission in this table follows
--   0  Permission x3 only; the codes then carry no display name
-- An unset variable stops sqlcmd before anything runs; any other value refuses.
--
-- Module/Resource Ids are cross-DB catalog metadata: the Auth database has no
-- dbo.Module or dbo.Resource table and no foreign key on either column.
-- Module NN 11 and Resource NN 110 are the next free values after DMS (module
-- 10 / resource 100), checked free on 2026-09-26 in KSS_Auth_Dev and
-- KSS_Common_Dev. As with modules 08-10, a matching KSS_Common row is optional.
-- Permission Ids NN 29-31 follow Dms.Read (NN 28), read from the live table.
--
-- Guards, all before any write; the first failure raises severity 16 and sets
-- NOEXEC ON, so no later statement runs:
--   [database]  DB_NAME() is KSS_Auth_Dev and @@SERVERNAME is SEBADB91, read on
--               the live connection
--   [switch]    WithTranslations is 0 or 1
--   [exists]    none of the three codes exists, compared case-insensitively as
--               UQ_Permission_Code compares them, and none of the three Ids is
--               in use. This script runs once: a hit is a refusal, not a re-run.
-- Writes: INSERT only, one transaction, XACT_ABORT ON. After the inserts the
-- script requires exactly 3 Permission rows with these exact (binary) codes and
-- ids, and exactly 6 or 0 translation rows to match the switch; anything else
-- throws and rolls back everything.
--
-- Dev only. A Prod twin is a separate change.
-- Apply with `sqlcmd -b -f 65001 -C -v WithTranslations=<0|1>`. The -b is
-- required, so a refusal ends with a non-zero exit.

IF DB_NAME() <> N'KSS_Auth_Dev' OR @@SERVERNAME <> N'SEBADB91'
    BEGIN RAISERROR('035 refused [database]: this script runs only on KSS_Auth_Dev on SEBADB91.', 16, 1); SET NOEXEC ON; END

IF N'$(WithTranslations)' NOT IN (N'0', N'1')
    BEGIN RAISERROR('035 refused [switch]: WithTranslations must be exactly 0 or 1.', 16, 1); SET NOEXEC ON; END

SET QUOTED_IDENTIFIER ON;
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @withTr BIT      = CASE WHEN N'$(WithTranslations)' = N'1' THEN 1 ELSE 0 END;
DECLARE @fa SMALLINT     = 12;
DECLARE @en SMALLINT     = 10;
-- U+200C ZERO WIDTH NON-JOINER, spelled out so it is visible in review
DECLARE @zwnj NCHAR(1)   = NCHAR(8204);

-- CRS module + resource Ids (cross-DB catalog metadata; no local FK)
DECLARE @modCrs UNIQUEIDENTIFIER = '019F1000-0000-7001-8000-000000000011';
DECLARE @resCrs UNIQUEIDENTIFIER = '019F1000-0000-7003-8000-000000000110';

DECLARE @perms TABLE (
    Code VARCHAR(100), Id UNIQUEIDENTIFIER,
    EnName NVARCHAR(100), EnDesc NVARCHAR(200),
    FaName NVARCHAR(100), FaDesc NVARCHAR(200)
);
INSERT INTO @perms (Code, Id, EnName, EnDesc, FaName, FaDesc) VALUES
 ('CustomerRisk.Case.Read',   '019F1100-0000-7102-8000-000000000029',
    N'View Risk Cases',     N'View risk cases in the Customer Risk System',
    N'مشاهده پرونده' + @zwnj + N'های ریسک', N'مشاهده پرونده' + @zwnj + N'های ریسک در سامانه استعلام ریسک مشتریان'),
 ('CustomerRisk.Case.Modify', '019F1100-0000-7102-8000-000000000030',
    N'Manage Risk Cases',   N'Create and edit risk cases in the Customer Risk System',
    N'مدیریت پرونده' + @zwnj + N'های ریسک', N'ثبت و ویرایش پرونده' + @zwnj + N'های ریسک در سامانه استعلام ریسک مشتریان'),
 ('CustomerRisk.Search.Read', '019F1100-0000-7102-8000-000000000031',
    N'Search Risk Records', N'Search and inquire customer risk records in the Customer Risk System',
    N'جستجو و استعلام ریسک', N'جستجو و استعلام سوابق ریسک مشتریان در سامانه استعلام ریسک مشتریان');

DECLARE @permBefore INT = (SELECT COUNT(*) FROM dbo.Permission);
DECLARE @trBefore   INT = (SELECT COUNT(*) FROM dbo.PermissionTranslation);
DECLARE @codeHits   INT = (SELECT COUNT(*) FROM dbo.Permission p JOIN @perms x
                             ON p.Code COLLATE SQL_Latin1_General_CP1_CI_AS = x.Code COLLATE SQL_Latin1_General_CP1_CI_AS);
DECLARE @idHits     INT = (SELECT COUNT(*) FROM dbo.Permission p JOIN @perms x ON p.Id = x.Id);

SELECT DB_NAME() AS DatabaseName, @@SERVERNAME AS ServerName, @withTr AS WithTranslations,
       @permBefore AS PermissionBefore, @trBefore AS TranslationBefore, @codeHits AS CodesAlreadyPresent, @idHits AS IdsAlreadyInUse;

IF @codeHits > 0 OR @idHits > 0
    BEGIN RAISERROR('035 refused [exists]: %d of the three codes already exist (case-insensitive) and %d of the three ids are in use; nothing was inserted.', 16, 1, @codeHits, @idHits); SET NOEXEC ON; END

-- >>> WRITES
BEGIN TRANSACTION;

INSERT INTO dbo.Permission (Id, Code, ModuleId, ResourceId)
SELECT x.Id, x.Code, @modCrs, @resCrs FROM @perms x;

IF @withTr = 1
    INSERT INTO dbo.PermissionTranslation (PermissionId, LanguageId, Name, Description)
    SELECT x.Id, @en, x.EnName, x.EnDesc FROM @perms x
    UNION ALL
    SELECT x.Id, @fa, x.FaName, x.FaDesc FROM @perms x;

DECLARE @permExact INT = (SELECT COUNT(*) FROM dbo.Permission p JOIN @perms x
                            ON p.Id = x.Id AND p.Code COLLATE Latin1_General_BIN2 = x.Code
                           AND p.ModuleId = @modCrs AND p.ResourceId = @resCrs);
DECLARE @trNew     INT = (SELECT COUNT(*) FROM dbo.PermissionTranslation t JOIN @perms x ON t.PermissionId = x.Id);
DECLARE @trWant    INT = CASE WHEN @withTr = 1 THEN 6 ELSE 0 END;

IF @permExact <> 3 OR @trNew <> @trWant OR (SELECT COUNT(*) FROM dbo.Permission) <> @permBefore + 3
                    OR (SELECT COUNT(*) FROM dbo.PermissionTranslation) <> @trBefore + @trWant
    THROW 50351, N'035 aborted [exactly]: the rows after the insert are not exactly the three codes and the expected translation rows; everything is rolled back.', 1;

COMMIT TRANSACTION;
-- <<< WRITES

PRINT N'Migration 035 applied: Permission (3) for CustomerRisk; PermissionTranslation (' + CAST(@trWant AS NVARCHAR(2)) + N').';

-- Report: the three rows as stored, their translations, and the totals
SELECT CONVERT(CHAR(36), p.Id) AS Id, p.Code, CONVERT(CHAR(36), p.ModuleId) AS ModuleId, CONVERT(CHAR(36), p.ResourceId) AS ResourceId,
       CASE WHEN p.Code COLLATE Latin1_General_BIN2 = x.Code THEN 'exact' ELSE 'CASE DIFFERS' END AS CodeBytes
FROM dbo.Permission p JOIN @perms x ON p.Id = x.Id
ORDER BY p.Id;

SELECT p.Code, t.LanguageId, t.Name, t.Description
FROM dbo.PermissionTranslation t JOIN dbo.Permission p ON p.Id = t.PermissionId JOIN @perms x ON x.Id = p.Id
ORDER BY p.Id, t.LanguageId;

SELECT (SELECT COUNT(*) FROM dbo.Permission) AS PermissionAfter, (SELECT COUNT(*) FROM dbo.PermissionTranslation) AS TranslationAfter,
       (SELECT COUNT(*) FROM dbo.RolePermission rp JOIN @perms x ON rp.PermissionId = x.Id) AS GrantsOfTheseCodes;

SET NOEXEC OFF;
