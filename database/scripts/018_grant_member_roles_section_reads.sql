-- 018_grant_member_roles_section_reads.sql
--
-- Grants Member-tier roles every Read permission in their module so the
-- corresponding menu items + page endpoints are reachable. Row-level
-- visibility remains gated by the Access / RoleAccess tables — empty
-- pickers until an admin grants per-row Access.
--
--   PersonMember  ← Person.Assets.Read, Access.Read, Security.Read, Admin.Read
--                   (already had Information.Read)
--   CompanyMember ← Company.Access.Read, Company.Admin.Read
--                   (already had Information.Read)
--
-- Idempotent. Apply with `sqlcmd -f 65001` to KSS_Auth_Prod and KSS_Auth_Dev.

SET QUOTED_IDENTIFIER ON;
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @system UNIQUEIDENTIFIER = '00000000-0000-0000-0000-000000000001';
DECLARE @now DATETIME2 = SYSUTCDATETIME();

DECLARE @rPersonMember  UNIQUEIDENTIFIER = (SELECT Id FROM dbo.[Role] WHERE Code = 'PersonMember');
DECLARE @rCompanyMember UNIQUEIDENTIFIER = (SELECT Id FROM dbo.[Role] WHERE Code = 'CompanyMember');

IF @rPersonMember IS NULL  THROW 51110, 'PersonMember role not found',  1;
IF @rCompanyMember IS NULL THROW 51111, 'CompanyMember role not found', 1;

BEGIN TRANSACTION;

INSERT INTO dbo.RolePermission (RoleId, PermissionId, CreatedBy, CreatedAt)
SELECT v.RoleId, p.Id, @system, @now
FROM (VALUES
    (@rPersonMember,  'Person.Assets.Read'),
    (@rPersonMember,  'Person.Access.Read'),
    (@rPersonMember,  'Person.Security.Read'),
    (@rPersonMember,  'Person.Admin.Read'),
    (@rCompanyMember, 'Company.Access.Read'),
    (@rCompanyMember, 'Company.Admin.Read')
) AS v(RoleId, PermissionCode)
JOIN dbo.Permission p ON p.Code = v.PermissionCode
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.RolePermission rp
    WHERE rp.RoleId = v.RoleId AND rp.PermissionId = p.Id
);

COMMIT TRANSACTION;

SELECT r.Code AS Role, p.Code AS Permission
FROM dbo.RolePermission rp
JOIN dbo.[Role] r ON r.Id = rp.RoleId
JOIN dbo.Permission p ON p.Id = rp.PermissionId
WHERE r.Code IN ('PersonMember', 'CompanyMember')
ORDER BY r.Code, p.Code;

PRINT '018_grant_member_roles_section_reads.sql applied successfully.';
