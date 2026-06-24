-- 024_add_member_report_role.sql
--
-- Adds a Members-scoped `MemberReport` role and grants it the existing read
-- permissions the Members-Reports section needs to render data
-- (Members.Brokerage.Read + Members.InvestmentFunds.Read). The role gates the
-- Members Reports menu section so report access can be granted without elevating
-- a user to SuperAdmin or Developer.
--
-- Layout follows 023_add_developer_role_and_permission.sql: idempotent inserts,
-- fixed Guids identical across Dev + Prod, en + fa translations, soft-delete
-- aware grants. No new permissions are created here — only existing ones are
-- granted to the new role.
--
-- Apply with `sqlcmd -f 65001 -C` to KSS_Auth_Dev then KSS_Auth_Prod.

SET QUOTED_IDENTIFIER ON;
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @system UNIQUEIDENTIFIER = '00000000-0000-0000-0000-000000000001';
DECLARE @now    DATETIME2       = SYSUTCDATETIME();
DECLARE @fa SMALLINT = 12;
DECLARE @en SMALLINT = 10;

-- ── Reused module Id (Members) — shared across Dev + Prod via Common DB
DECLARE @modMembers UNIQUEIDENTIFIER = '019F1000-0000-7001-8000-000000000003';

-- ── New Id (fixed so Dev + Prod stay in lock-step)
DECLARE @rMemberReport UNIQUEIDENTIFIER = '019F1100-0000-7100-8000-000000000014';

BEGIN TRANSACTION;

-- ── Step 1: MemberReport role (idempotent), scoped to the Members module
IF NOT EXISTS (SELECT 1 FROM dbo.[Role] WHERE Code = 'MemberReport')
    INSERT INTO dbo.[Role] (Id, Code, ModuleId) VALUES (@rMemberReport, 'MemberReport', @modMembers);

-- Re-resolve in case the row pre-existed under a different Id
SET @rMemberReport = (SELECT Id FROM dbo.[Role] WHERE Code = 'MemberReport');

-- ── Step 2: Role translations (en + fa)
IF NOT EXISTS (SELECT 1 FROM dbo.RoleTranslation WHERE RoleId = @rMemberReport AND LanguageId = @en)
    INSERT INTO dbo.RoleTranslation (RoleId, LanguageId, Name, Description)
    VALUES (@rMemberReport, @en, N'Member Report',
            N'Read-only access to the Members Reports section');

IF NOT EXISTS (SELECT 1 FROM dbo.RoleTranslation WHERE RoleId = @rMemberReport AND LanguageId = @fa)
    INSERT INTO dbo.RoleTranslation (RoleId, LanguageId, Name, Description)
    VALUES (@rMemberReport, @fa, N'گزارش اعضا',
            N'دسترسی فقط‌خواندنی به بخش گزارش‌های اعضا');

-- ── Step 3: Resolve existing permission Ids (by Code; vary between Dev/Prod)
DECLARE @pBrokerageRead UNIQUEIDENTIFIER = (SELECT Id FROM dbo.Permission WHERE Code = 'Members.Brokerage.Read');
DECLARE @pFundsRead     UNIQUEIDENTIFIER = (SELECT Id FROM dbo.Permission WHERE Code = 'Members.InvestmentFunds.Read');

-- ── Step 4: Grants — MemberReport → Members read permissions (soft-delete aware)
IF @pBrokerageRead IS NOT NULL AND NOT EXISTS (
    SELECT 1 FROM dbo.RolePermission
    WHERE RoleId = @rMemberReport AND PermissionId = @pBrokerageRead AND DeletedAt IS NULL
)
    INSERT INTO dbo.RolePermission (RoleId, PermissionId, CreatedBy, CreatedAt)
    VALUES (@rMemberReport, @pBrokerageRead, @system, @now);

IF @pFundsRead IS NOT NULL AND NOT EXISTS (
    SELECT 1 FROM dbo.RolePermission
    WHERE RoleId = @rMemberReport AND PermissionId = @pFundsRead AND DeletedAt IS NULL
)
    INSERT INTO dbo.RolePermission (RoleId, PermissionId, CreatedBy, CreatedAt)
    VALUES (@rMemberReport, @pFundsRead, @system, @now);

COMMIT TRANSACTION;

PRINT N'Migration 024 applied: MemberReport role (Members-scoped) + Members read grants';
