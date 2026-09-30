-- 038_grant_all_customerrisk_to_superadmin_developer.sql
--
-- SuperAdmin and Developer hold EVERY CustomerRisk.* permission. This script adds
-- only the grants that are missing, among the three CRS permission codes:
--   CustomerRisk.Case.Read, CustomerRisk.Case.Modify, CustomerRisk.Search.Read
-- Measured before writing (2026-09-30): the one missing pair is
--   Developer -> CustomerRisk.Search.Read
-- and the script requires exactly that set, so a different state refuses instead of
-- writing something nobody measured.
-- Writes: RolePermission x1 only. CreatedBy is the Auth system id, as every existing
-- CRS grant carries. No other role, no UserRole row, no Permission, no other change.
--
-- Guards, all before any write; the first failure raises severity 16 and sets
-- NOEXEC ON, so no later statement runs:
--   [database]  DB_NAME() is KSS_Auth_Dev and @@SERVERNAME is SEBADB91, read on
--               the live connection
--   [holders]   SuperAdmin and Developer each exist once, the three CRS codes each
--               exist once, and no OTHER CustomerRisk.* permission exists
--   [state]     the missing (role, CRS permission) pairs for the two roles, live or
--               soft-deleted rows counted as present, are exactly the one pair above.
--               This script runs once: a different state is a refusal, not a re-run.
-- Writes: INSERT only, one transaction, XACT_ABORT ON. After the insert the script
-- requires SuperAdmin and Developer to hold all 3 CRS codes live (6 grants),
-- RolePermission +1, Permission +0, Role +0, UserRole +0, and every other role's
-- grants unchanged; anything else throws and rolls back everything.
--
-- Apply with `sqlcmd -b -f 65001 -C`. The -b is required, so a refusal ends with a
-- non-zero exit.

IF DB_NAME() <> N'KSS_Auth_Dev' OR @@SERVERNAME <> N'SEBADB91'
    BEGIN RAISERROR('038 refused [database]: this script runs only on KSS_Auth_Dev on SEBADB91.', 16, 1); SET NOEXEC ON; END

SET QUOTED_IDENTIFIER ON;
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @system UNIQUEIDENTIFIER = '00000000-0000-0000-0000-000000000001';
DECLARE @now    DATETIME2        = SYSUTCDATETIME();

DECLARE @superAdmin UNIQUEIDENTIFIER = (SELECT Id FROM dbo.[Role] WHERE Code COLLATE Latin1_General_BIN2 = 'SuperAdmin');
DECLARE @developer  UNIQUEIDENTIFIER = (SELECT Id FROM dbo.[Role] WHERE Code COLLATE Latin1_General_BIN2 = 'Developer');
DECLARE @holderRoles INT = (SELECT COUNT(*) FROM dbo.[Role] WHERE Code COLLATE Latin1_General_BIN2 IN ('SuperAdmin', 'Developer'));

DECLARE @crs TABLE (Code VARCHAR(100) PRIMARY KEY);
INSERT INTO @crs (Code) VALUES ('CustomerRisk.Case.Read'), ('CustomerRisk.Case.Modify'), ('CustomerRisk.Search.Read');
DECLARE @crsFound INT = (SELECT COUNT(*) FROM dbo.Permission p JOIN @crs x ON p.Code COLLATE Latin1_General_BIN2 = x.Code);
DECLARE @crsOther INT = (SELECT COUNT(*) FROM dbo.Permission p
                         WHERE p.Code COLLATE SQL_Latin1_General_CP1_CI_AS LIKE 'CustomerRisk%'
                           AND NOT EXISTS (SELECT 1 FROM @crs x WHERE p.Code COLLATE Latin1_General_BIN2 = x.Code));

-- every (holder role, CRS permission) pair with no RolePermission row at all
DECLARE @missing TABLE (RoleCode VARCHAR(50), PermCode VARCHAR(100), RoleId UNIQUEIDENTIFIER, PermissionId UNIQUEIDENTIFIER);
INSERT INTO @missing (RoleCode, PermCode, RoleId, PermissionId)
SELECT r.Code, p.Code, r.Id, p.Id
FROM dbo.[Role] r JOIN dbo.Permission p ON p.Code COLLATE Latin1_General_BIN2 IN (SELECT Code FROM @crs)
WHERE r.Code COLLATE Latin1_General_BIN2 IN ('SuperAdmin', 'Developer')
  AND NOT EXISTS (SELECT 1 FROM dbo.RolePermission rp WHERE rp.RoleId = r.Id AND rp.PermissionId = p.Id);
DECLARE @missingAll INT = (SELECT COUNT(*) FROM @missing);
DECLARE @missingExpected INT = (SELECT COUNT(*) FROM @missing
                                WHERE RoleCode COLLATE Latin1_General_BIN2 = 'Developer' AND PermCode COLLATE Latin1_General_BIN2 = 'CustomerRisk.Search.Read');

DECLARE @permBefore INT = (SELECT COUNT(*) FROM dbo.Permission);
DECLARE @rpBefore   INT = (SELECT COUNT(*) FROM dbo.RolePermission);
DECLARE @urBefore   INT = (SELECT COUNT(*) FROM dbo.UserRole);
DECLARE @roleBefore INT = (SELECT COUNT(*) FROM dbo.[Role]);
-- every other role's grants, as a count and a checksum, to prove they do not move
DECLARE @otherCount INT = (SELECT COUNT(*) FROM dbo.RolePermission WHERE RoleId NOT IN (@superAdmin, @developer));
DECLARE @otherSum   INT = (SELECT CHECKSUM_AGG(CHECKSUM(RoleId, PermissionId, CreatedBy, CreatedAt, UpdatedBy, UpdatedAt, DeletedBy, DeletedAt))
                           FROM dbo.RolePermission WHERE RoleId NOT IN (@superAdmin, @developer));

SELECT DB_NAME() AS DatabaseName, @@SERVERNAME AS ServerName, @holderRoles AS HolderRoles, @crsFound AS CrsCodes, @crsOther AS OtherCrsCodes,
       @missingAll AS MissingPairs, @missingExpected AS MissingExpectedPair,
       @permBefore AS PermissionBefore, @rpBefore AS RolePermBefore, @urBefore AS UserRoleBefore, @roleBefore AS RoleBefore, @otherCount AS OtherRoleGrants;

IF @holderRoles <> 2 OR @superAdmin IS NULL OR @developer IS NULL OR @crsFound <> 3 OR @crsOther <> 0
    BEGIN RAISERROR('038 refused [holders]: roles %d of 2, CRS codes %d of 3, other CustomerRisk codes %d (must be 0); nothing was written.', 16, 1, @holderRoles, @crsFound, @crsOther); SET NOEXEC ON; END

IF @missingAll <> 1 OR @missingExpected <> 1
    BEGIN RAISERROR('038 refused [state]: %d missing pairs (expected exactly 1, Developer -> CustomerRisk.Search.Read, found %d); nothing was written.', 16, 1, @missingAll, @missingExpected); SET NOEXEC ON; END

-- >>> WRITES
BEGIN TRANSACTION;

INSERT INTO dbo.RolePermission (RoleId, PermissionId, CreatedBy, CreatedAt)
SELECT m.RoleId, m.PermissionId, @system, @now FROM @missing m;

DECLARE @held INT = (SELECT COUNT(*) FROM dbo.RolePermission rp JOIN dbo.Permission p ON p.Id = rp.PermissionId
                     WHERE rp.RoleId IN (@superAdmin, @developer) AND rp.DeletedAt IS NULL
                       AND p.Code COLLATE Latin1_General_BIN2 IN (SELECT Code FROM @crs));
DECLARE @newRow INT = (SELECT COUNT(*) FROM dbo.RolePermission rp JOIN @missing m ON rp.RoleId = m.RoleId AND rp.PermissionId = m.PermissionId
                       WHERE rp.CreatedBy = @system AND rp.CreatedAt = @now AND rp.DeletedAt IS NULL);

IF @held <> 6 OR @newRow <> 1
   OR (SELECT COUNT(*) FROM dbo.RolePermission) <> @rpBefore + 1
   OR (SELECT COUNT(*) FROM dbo.Permission) <> @permBefore
   OR (SELECT COUNT(*) FROM dbo.UserRole) <> @urBefore
   OR (SELECT COUNT(*) FROM dbo.[Role]) <> @roleBefore
   OR (SELECT COUNT(*) FROM dbo.RolePermission WHERE RoleId NOT IN (@superAdmin, @developer)) <> @otherCount
   OR ISNULL((SELECT CHECKSUM_AGG(CHECKSUM(RoleId, PermissionId, CreatedBy, CreatedAt, UpdatedBy, UpdatedAt, DeletedBy, DeletedAt))
              FROM dbo.RolePermission WHERE RoleId NOT IN (@superAdmin, @developer)), 0) <> ISNULL(@otherSum, 0)
    THROW 50381, N'038 aborted [exactly]: after the insert SuperAdmin and Developer do not hold exactly the 3 CRS codes, or another count or role moved; everything is rolled back.', 1;

COMMIT TRANSACTION;
-- <<< WRITES

PRINT N'Migration 038 applied: RolePermission (1): Developer -> CustomerRisk.Search.Read. SuperAdmin and Developer now hold all 3 CustomerRisk codes.';

SELECT r.Code AS RoleCode, p.Code AS PermCode, CONVERT(CHAR(36), rp.RoleId) AS RoleId, CONVERT(CHAR(36), rp.PermissionId) AS PermissionId,
       CONVERT(CHAR(36), rp.CreatedBy) AS CreatedBy, rp.CreatedAt
FROM dbo.RolePermission rp JOIN dbo.[Role] r ON r.Id = rp.RoleId JOIN dbo.Permission p ON p.Id = rp.PermissionId
WHERE rp.RoleId IN (@superAdmin, @developer) AND p.Code LIKE 'CustomerRisk%'
ORDER BY r.Code, p.Code;

SELECT (SELECT COUNT(*) FROM dbo.Permission) AS PermissionAfter, (SELECT COUNT(*) FROM dbo.RolePermission) AS RolePermAfter,
       (SELECT COUNT(*) FROM dbo.UserRole) AS UserRoleAfter, (SELECT COUNT(*) FROM dbo.[Role]) AS RoleAfter;

SET NOEXEC OFF;
