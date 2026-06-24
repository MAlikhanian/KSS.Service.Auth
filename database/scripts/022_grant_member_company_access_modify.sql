-- =============================================================================
-- Migration 022 — grant CompanyMember the Company.Access.Modify permission
-- =============================================================================
-- Migration 021 added Company.Information.Modify so a CompanyMember can edit
-- the data of companies they have a per-person dbo.Access row for.
--
-- This migration completes the same pattern for the Access section: with
-- Company.Access.Modify on the role plus a per-person dbo.Access row at
-- (SectionId=2, Level=2) on a target company, a CompanyMember can grant /
-- revoke access on THAT company only. Companies without such a row stay
-- locked — the per-row data check in AccessService still blocks them.
--
-- IMPORTANT: looks up Permission by Code, NOT by hardcoded GUID. Permission
-- catalog GUIDs diverge between environments — Code is the stable identifier.
--
-- Idempotent — safe to run multiple times.
-- =============================================================================
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
SET NOCOUNT ON;
GO

DECLARE @CompanyMemberRoleId UNIQUEIDENTIFIER = '019F1100-0000-7100-8000-000000000004';
DECLARE @SystemSeed          UNIQUEIDENTIFIER = '00000000-0000-0000-0000-000000000001';
DECLARE @Now                 DATETIME2        = SYSUTCDATETIME();

DECLARE @AccessModifyPermId UNIQUEIDENTIFIER;
SELECT @AccessModifyPermId = Id
FROM dbo.Permission
WHERE Code = 'Company.Access.Modify';

IF @AccessModifyPermId IS NULL
BEGIN
    RAISERROR ('Permission "Company.Access.Modify" not found in catalog.', 16, 1);
END
ELSE IF NOT EXISTS (
    SELECT 1 FROM dbo.RolePermission
    WHERE RoleId = @CompanyMemberRoleId
      AND PermissionId = @AccessModifyPermId
)
BEGIN
    INSERT INTO dbo.RolePermission (RoleId, PermissionId, CreatedBy, CreatedAt)
    VALUES (@CompanyMemberRoleId, @AccessModifyPermId, @SystemSeed, @Now);
END;

GO
