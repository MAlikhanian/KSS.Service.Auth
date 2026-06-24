-- 002_m2m_audit_columns_PROD.sql
--
-- Add canonical audit clump to M2M tables (RolePermission, UserRole) in KSS_Auth_Prod:
--   * Add 6 audit columns: CreatedBy, CreatedAt, UpdatedBy, UpdatedAt, DeletedBy, DeletedAt
--   * Drop AssignedAt (semantically replaced by CreatedAt)
--   * No IsActive (child pattern — soft-delete via DeletedAt)
--
-- AssignedAt's value is preserved by copying it into CreatedAt before the column is dropped.
--
-- Apply ONLY to KSS_Auth_Prod.

SET QUOTED_IDENTIFIER ON;
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @system_user UNIQUEIDENTIFIER = '00000000-0000-0000-0000-000000000001';

BEGIN TRANSACTION;

-- ── RolePermission ────────────────────────────────────────────────
ALTER TABLE dbo.RolePermission ADD
    CreatedBy UNIQUEIDENTIFIER NULL,
    CreatedAt DATETIME2        NULL,
    UpdatedBy UNIQUEIDENTIFIER NULL,
    UpdatedAt DATETIME2        NULL,
    DeletedBy UNIQUEIDENTIFIER NULL,
    DeletedAt DATETIME2        NULL;

EXEC sp_executesql
    N'UPDATE dbo.RolePermission SET CreatedBy = @sysu, CreatedAt = AssignedAt;',
    N'@sysu UNIQUEIDENTIFIER',
    @sysu = @system_user;

ALTER TABLE dbo.RolePermission ALTER COLUMN CreatedBy UNIQUEIDENTIFIER NOT NULL;
ALTER TABLE dbo.RolePermission ALTER COLUMN CreatedAt DATETIME2        NOT NULL;

ALTER TABLE dbo.RolePermission DROP CONSTRAINT DF_RolePermission_AssignedAt;
ALTER TABLE dbo.RolePermission DROP COLUMN AssignedAt;

-- ── UserRole ──────────────────────────────────────────────────────
ALTER TABLE dbo.UserRole ADD
    CreatedBy UNIQUEIDENTIFIER NULL,
    CreatedAt DATETIME2        NULL,
    UpdatedBy UNIQUEIDENTIFIER NULL,
    UpdatedAt DATETIME2        NULL,
    DeletedBy UNIQUEIDENTIFIER NULL,
    DeletedAt DATETIME2        NULL;

EXEC sp_executesql
    N'UPDATE dbo.UserRole SET CreatedBy = @sysu, CreatedAt = AssignedAt;',
    N'@sysu UNIQUEIDENTIFIER',
    @sysu = @system_user;

ALTER TABLE dbo.UserRole ALTER COLUMN CreatedBy UNIQUEIDENTIFIER NOT NULL;
ALTER TABLE dbo.UserRole ALTER COLUMN CreatedAt DATETIME2        NOT NULL;

ALTER TABLE dbo.UserRole DROP CONSTRAINT DF_UserRole_AssignedAt;
ALTER TABLE dbo.UserRole DROP COLUMN AssignedAt;

COMMIT TRANSACTION;

PRINT '002_m2m_audit_columns_PROD.sql applied successfully.';
