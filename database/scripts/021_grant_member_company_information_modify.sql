-- =============================================================================
-- Migration 021 — grant CompanyMember the Company.Information.Modify permission
-- =============================================================================
-- CompanyMember currently has only Read permissions (Company.Information.Read,
-- Company.Access.Read), which makes it functionally identical to CompanyViewer
-- at the Auth-permission layer. The intended distinction:
--
--   CompanyViewer  → read-only ACROSS ALL companies (global RoleAccess Level 1)
--   CompanyMember  → read+write on assigned companies ONLY (per-person Access)
--
-- The row-level access check in KSS.Service.Company enforces the "assigned-only"
-- side; this migration completes the Member tier by giving the role the
-- Modify permission claim it needs. Company.Access.Modify is intentionally
-- NOT granted — managing who else can access stays admin-only.
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

DECLARE @InformationModifyPermId UNIQUEIDENTIFIER;
SELECT @InformationModifyPermId = Id
FROM dbo.Permission
WHERE Code = 'Company.Information.Modify';

IF @InformationModifyPermId IS NULL
BEGIN
    RAISERROR ('Permission "Company.Information.Modify" not found in catalog.', 16, 1);
END
ELSE IF NOT EXISTS (
    SELECT 1 FROM dbo.RolePermission
    WHERE RoleId = @CompanyMemberRoleId
      AND PermissionId = @InformationModifyPermId
)
BEGIN
    INSERT INTO dbo.RolePermission (RoleId, PermissionId, CreatedBy, CreatedAt)
    VALUES (@CompanyMemberRoleId, @InformationModifyPermId, @SystemSeed, @Now);
END;

GO
