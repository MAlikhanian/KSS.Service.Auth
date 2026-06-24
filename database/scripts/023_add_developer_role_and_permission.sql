-- 023_add_developer_role_and_permission.sql
--
-- Adds a new global `Developer` role and a `Developer.Access` permission.
-- The role gates in-development module sections (Cash Advance, Customer Risk,
-- Members Reports) so devs can preview work-in-progress UI without being
-- elevated to SuperAdmin (which carries 23 production permissions).
--
-- Layout follows 011_add_viewer_member_roles.sql exactly: idempotent inserts,
-- fixed Guids identical across Dev + Prod, en + fa translations, soft-delete
-- aware grants.
--
-- Reuses existing System module + System.Security.Role resource Ids — no
-- cross-DB writes into KSS_Common. The Resource taxonomy is internal admin
-- metadata; can be split into a dedicated Developer resource later.
--
-- Apply with `sqlcmd -f 65001 -C` to KSS_Auth_Dev then KSS_Auth_Prod.

SET QUOTED_IDENTIFIER ON;
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @system UNIQUEIDENTIFIER = '00000000-0000-0000-0000-000000000001';
DECLARE @now    DATETIME2       = SYSUTCDATETIME();
DECLARE @fa SMALLINT = 12;
DECLARE @en SMALLINT = 10;

-- ── Reused Ids (shared across Dev + Prod via Common DB)
DECLARE @modSystem  UNIQUEIDENTIFIER = '019F1000-0000-7001-8000-000000000005';
DECLARE @resSecRole UNIQUEIDENTIFIER = '019F1000-0000-7003-8000-000000000050';

-- ── New Ids (fixed so Dev + Prod stay in lock-step)
DECLARE @rDeveloper UNIQUEIDENTIFIER = '019F1100-0000-7100-8000-000000000013';
DECLARE @pDevAccess UNIQUEIDENTIFIER = '019F1100-0000-7102-8000-000000000001';

-- ── Resolve SuperAdmin Id from existing seed (varies between Dev/Prod)
DECLARE @rSuperAdmin UNIQUEIDENTIFIER = (SELECT Id FROM dbo.[Role] WHERE Code = 'SuperAdmin');

BEGIN TRANSACTION;

-- ── Step 1: Developer role (idempotent)
IF NOT EXISTS (SELECT 1 FROM dbo.[Role] WHERE Code = 'Developer')
    INSERT INTO dbo.[Role] (Id, Code, ModuleId) VALUES (@rDeveloper, 'Developer', NULL);

-- Re-resolve in case the row pre-existed under a different Id
SET @rDeveloper = (SELECT Id FROM dbo.[Role] WHERE Code = 'Developer');

-- ── Step 2: Role translations (en + fa)
IF NOT EXISTS (SELECT 1 FROM dbo.RoleTranslation WHERE RoleId = @rDeveloper AND LanguageId = @en)
    INSERT INTO dbo.RoleTranslation (RoleId, LanguageId, Name, Description)
    VALUES (@rDeveloper, @en, N'Developer',
            N'Internal developer — sees in-development modules and template pages');

IF NOT EXISTS (SELECT 1 FROM dbo.RoleTranslation WHERE RoleId = @rDeveloper AND LanguageId = @fa)
    INSERT INTO dbo.RoleTranslation (RoleId, LanguageId, Name, Description)
    VALUES (@rDeveloper, @fa, N'توسعه‌دهنده',
            N'توسعه‌دهنده داخلی — مشاهده ماژول‌های در دست توسعه و صفحات نمونه');

-- ── Step 3: Developer.Access permission (idempotent)
IF NOT EXISTS (SELECT 1 FROM dbo.Permission WHERE Code = 'Developer.Access')
    INSERT INTO dbo.Permission (Id, Code, ModuleId, ResourceId)
    VALUES (@pDevAccess, 'Developer.Access', @modSystem, @resSecRole);

SET @pDevAccess = (SELECT Id FROM dbo.Permission WHERE Code = 'Developer.Access');

-- ── Step 4: Permission translations (en + fa)
IF NOT EXISTS (SELECT 1 FROM dbo.PermissionTranslation WHERE PermissionId = @pDevAccess AND LanguageId = @en)
    INSERT INTO dbo.PermissionTranslation (PermissionId, LanguageId, Name, Description)
    VALUES (@pDevAccess, @en, N'Developer Access',
            N'Grants visibility of in-development modules and developer-only UI');

IF NOT EXISTS (SELECT 1 FROM dbo.PermissionTranslation WHERE PermissionId = @pDevAccess AND LanguageId = @fa)
    INSERT INTO dbo.PermissionTranslation (PermissionId, LanguageId, Name, Description)
    VALUES (@pDevAccess, @fa, N'دسترسی توسعه‌دهنده',
            N'دسترسی به ماژول‌های در حال توسعه و صفحات مخصوص توسعه‌دهنده');

-- ── Step 5: Grants — Developer → Developer.Access, SuperAdmin → Developer.Access
IF NOT EXISTS (
    SELECT 1 FROM dbo.RolePermission
    WHERE RoleId = @rDeveloper AND PermissionId = @pDevAccess AND DeletedAt IS NULL
)
    INSERT INTO dbo.RolePermission (RoleId, PermissionId, CreatedBy, CreatedAt)
    VALUES (@rDeveloper, @pDevAccess, @system, @now);

IF NOT EXISTS (
    SELECT 1 FROM dbo.RolePermission
    WHERE RoleId = @rSuperAdmin AND PermissionId = @pDevAccess AND DeletedAt IS NULL
)
    INSERT INTO dbo.RolePermission (RoleId, PermissionId, CreatedBy, CreatedAt)
    VALUES (@rSuperAdmin, @pDevAccess, @system, @now);

COMMIT TRANSACTION;

PRINT N'Migration 023 applied: Developer role + Developer.Access permission';
