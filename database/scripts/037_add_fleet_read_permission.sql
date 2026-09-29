-- 037_add_fleet_read_permission.sql
--
-- Fleet module RBAC: one permission code, granted to SuperAdmin and Developer only.
--   Fleet.Read    view the Fleet section
-- Writes: Permission x1, PermissionTranslation x2 (en + fa, the convention every
-- permission in this table follows), RolePermission x2 (SuperAdmin, Developer).
-- No other role, no UserRole row, no other change.
--
-- Shape mirrors the live Dms.Read rows: Permission (Id, Code, ModuleId, ResourceId)
-- with a per-module Module/Resource id; one en and one fa translation, each with a
-- Name and a Description; grants written by the Auth system id, as Dms.Read's
-- SuperAdmin and Developer grants were.
--
-- Module/Resource Ids are cross-DB catalog metadata: the Auth database has no
-- dbo.Module or dbo.Resource table and no foreign key on either column.
-- Module NN 12 and Resource NN 120 are the next free values after CustomerRisk
-- (module 11 / resource 110), checked free on 2026-09-29 in both Auth catalogs and
-- both Common catalogs. As with modules 08-11, a matching KSS_Common row is optional.
-- Permission Id NN 32 follows CustomerRisk.Search.Read (NN 31), read from the live table.
--
-- Guards, all before any write; the first failure raises severity 16 and sets
-- NOEXEC ON, so no later statement runs:
--   [database]  DB_NAME() is KSS_Auth_Dev and @@SERVERNAME is SEBADB91, read on
--               the live connection
--   [holders]   SuperAdmin and Developer each exist once (binary comparison)
--   [exists]    no permission code containing "fleet" exists (case-insensitive), the
--               new Id is not in use, no translation exists for it, no permission
--               uses module NN 12 or resource NN 120, and no grant of it exists, live
--               or soft-deleted. This script runs once: a hit is a refusal, not a re-run.
-- Writes: INSERT only, one transaction, XACT_ABORT ON. After the inserts the script
-- requires exactly the one permission, its two translations and its two live grants,
-- Permission +1, PermissionTranslation +2, RolePermission +2, UserRole +0 and
-- Role +0; anything else throws and rolls back everything.
--
-- Apply with `sqlcmd -b -f 65001 -C`. The -b is required, so a refusal ends with a
-- non-zero exit.

IF DB_NAME() <> N'KSS_Auth_Dev' OR @@SERVERNAME <> N'SEBADB91'
    BEGIN RAISERROR('037 refused [database]: this script runs only on KSS_Auth_Dev on SEBADB91.', 16, 1); SET NOEXEC ON; END

SET QUOTED_IDENTIFIER ON;
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @system UNIQUEIDENTIFIER = '00000000-0000-0000-0000-000000000001';
DECLARE @now    DATETIME2        = SYSUTCDATETIME();
DECLARE @fa SMALLINT = 12;
DECLARE @en SMALLINT = 10;

-- Fleet module + resource Ids (cross-DB catalog metadata; no local FK)
DECLARE @modFleet UNIQUEIDENTIFIER = '019F1000-0000-7001-8000-000000000012';
DECLARE @resFleet UNIQUEIDENTIFIER = '019F1000-0000-7003-8000-000000000120';

DECLARE @permId UNIQUEIDENTIFIER = '019F1100-0000-7102-8000-000000000032';
DECLARE @code   VARCHAR(100)     = 'Fleet.Read';
DECLARE @enName NVARCHAR(100)    = N'View Fleet';
DECLARE @enDesc NVARCHAR(200)    = N'View the Fleet section';
DECLARE @faName NVARCHAR(100)    = N'مشاهده ناوگان';
DECLARE @faDesc NVARCHAR(200)    = N'مشاهده بخش ناوگان';

DECLARE @superAdmin UNIQUEIDENTIFIER = (SELECT Id FROM dbo.[Role] WHERE Code COLLATE Latin1_General_BIN2 = 'SuperAdmin');
DECLARE @developer  UNIQUEIDENTIFIER = (SELECT Id FROM dbo.[Role] WHERE Code COLLATE Latin1_General_BIN2 = 'Developer');
DECLARE @holderRoles INT = (SELECT COUNT(*) FROM dbo.[Role] WHERE Code COLLATE Latin1_General_BIN2 IN ('SuperAdmin', 'Developer'));

DECLARE @permBefore INT = (SELECT COUNT(*) FROM dbo.Permission);
DECLARE @trBefore   INT = (SELECT COUNT(*) FROM dbo.PermissionTranslation);
DECLARE @rpBefore   INT = (SELECT COUNT(*) FROM dbo.RolePermission);
DECLARE @urBefore   INT = (SELECT COUNT(*) FROM dbo.UserRole);
DECLARE @roleBefore INT = (SELECT COUNT(*) FROM dbo.[Role]);
DECLARE @codeHits   INT = (SELECT COUNT(*) FROM dbo.Permission WHERE Code COLLATE SQL_Latin1_General_CP1_CI_AS LIKE '%fleet%');
DECLARE @idHits     INT = (SELECT COUNT(*) FROM dbo.Permission WHERE Id = @permId);
DECLARE @trHits     INT = (SELECT COUNT(*) FROM dbo.PermissionTranslation WHERE PermissionId = @permId);
DECLARE @modHits    INT = (SELECT COUNT(*) FROM dbo.Permission WHERE ModuleId = @modFleet OR ResourceId = @resFleet);
DECLARE @grantHits  INT = (SELECT COUNT(*) FROM dbo.RolePermission WHERE PermissionId = @permId);

SELECT DB_NAME() AS DatabaseName, @@SERVERNAME AS ServerName, @holderRoles AS HolderRoles,
       @permBefore AS PermissionBefore, @trBefore AS TranslationBefore, @rpBefore AS RolePermBefore, @urBefore AS UserRoleBefore, @roleBefore AS RoleBefore,
       @codeHits AS FleetCodesPresent, @idHits AS IdInUse, @trHits AS TranslationsPresent, @modHits AS ModuleOrResourceInUse, @grantHits AS GrantsPresent;

IF @holderRoles <> 2 OR @superAdmin IS NULL OR @developer IS NULL
    BEGIN RAISERROR('037 refused [holders]: expected SuperAdmin and Developer once each (found %d of 2); nothing was written.', 16, 1, @holderRoles); SET NOEXEC ON; END

IF @codeHits > 0 OR @idHits > 0 OR @trHits > 0 OR @modHits > 0 OR @grantHits > 0
    BEGIN RAISERROR('037 refused [exists]: fleet codes %d, id in use %d, translations %d, module/resource in use %d, grants %d; nothing was written.', 16, 1, @codeHits, @idHits, @trHits, @modHits, @grantHits); SET NOEXEC ON; END

-- >>> WRITES
BEGIN TRANSACTION;

INSERT INTO dbo.Permission (Id, Code, ModuleId, ResourceId)
VALUES (@permId, @code, @modFleet, @resFleet);

INSERT INTO dbo.PermissionTranslation (PermissionId, LanguageId, Name, Description)
VALUES (@permId, @en, @enName, @enDesc),
       (@permId, @fa, @faName, @faDesc);

-- SuperAdmin and Developer ONLY, per the approval: both roles hold every project.
INSERT INTO dbo.RolePermission (RoleId, PermissionId, CreatedBy, CreatedAt)
VALUES (@superAdmin, @permId, @system, @now),
       (@developer,  @permId, @system, @now);

DECLARE @permExact  INT = (SELECT COUNT(*) FROM dbo.Permission
                           WHERE Id = @permId AND Code COLLATE Latin1_General_BIN2 = @code AND ModuleId = @modFleet AND ResourceId = @resFleet);
DECLARE @trExact    INT = (SELECT COUNT(*) FROM dbo.PermissionTranslation
                           WHERE PermissionId = @permId AND ((LanguageId = @en AND Name = @enName AND Description = @enDesc)
                                                          OR (LanguageId = @fa AND Name = @faName AND Description = @faDesc)));
DECLARE @grantExact INT = (SELECT COUNT(*) FROM dbo.RolePermission
                           WHERE PermissionId = @permId AND RoleId IN (@superAdmin, @developer) AND CreatedBy = @system AND DeletedAt IS NULL);
DECLARE @grantAll   INT = (SELECT COUNT(*) FROM dbo.RolePermission WHERE PermissionId = @permId);

IF @permExact <> 1 OR @trExact <> 2 OR @grantExact <> 2 OR @grantAll <> 2
   OR (SELECT COUNT(*) FROM dbo.Permission) <> @permBefore + 1
   OR (SELECT COUNT(*) FROM dbo.PermissionTranslation) <> @trBefore + 2
   OR (SELECT COUNT(*) FROM dbo.RolePermission) <> @rpBefore + 2
   OR (SELECT COUNT(*) FROM dbo.UserRole) <> @urBefore
   OR (SELECT COUNT(*) FROM dbo.[Role]) <> @roleBefore
    THROW 50371, N'037 aborted [exactly]: the rows after the insert are not exactly Fleet.Read, its two translations and its two grants; everything is rolled back.', 1;

COMMIT TRANSACTION;
-- <<< WRITES

PRINT N'Migration 037 applied: Permission (1) Fleet.Read; PermissionTranslation (2); RolePermission (2): SuperAdmin, Developer.';

-- Report: the rows as stored and the totals
SELECT CONVERT(CHAR(36), p.Id) AS Id, p.Code, CONVERT(CHAR(36), p.ModuleId) AS ModuleId, CONVERT(CHAR(36), p.ResourceId) AS ResourceId
FROM dbo.Permission p WHERE p.Id = @permId;

SELECT t.LanguageId, t.Name, t.Description FROM dbo.PermissionTranslation t WHERE t.PermissionId = @permId ORDER BY t.LanguageId;

SELECT r.Code AS RoleCode, CONVERT(CHAR(36), rp.RoleId) AS RoleId, CONVERT(CHAR(36), rp.CreatedBy) AS CreatedBy, rp.CreatedAt
FROM dbo.RolePermission rp JOIN dbo.[Role] r ON r.Id = rp.RoleId WHERE rp.PermissionId = @permId ORDER BY r.Code;

SELECT (SELECT COUNT(*) FROM dbo.Permission) AS PermissionAfter, (SELECT COUNT(*) FROM dbo.PermissionTranslation) AS TranslationAfter,
       (SELECT COUNT(*) FROM dbo.RolePermission) AS RolePermAfter, (SELECT COUNT(*) FROM dbo.UserRole) AS UserRoleAfter;

SET NOEXEC OFF;
