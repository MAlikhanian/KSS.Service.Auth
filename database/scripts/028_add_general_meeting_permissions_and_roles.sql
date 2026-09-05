-- 028_add_general_meeting_permissions_and_roles.sql
--
-- General Meeting (مجمع عمومی) feature RBAC: 3 permissions —
-- Members.Meeting.Read, Members.Meeting.Modify, Members.Election.Manage —
-- plus a single MeetingManager role granted all three, with en + fa
-- translations and role→permission grants. SuperAdmin receives all 3.
--
-- Members.Meeting.Read / .Modify are auto-enforced by the Members service's
-- [PermissionGroup("Meeting")] filter (hard-coded "Members" prefix in
-- PermissionAuthorizationFilter); Members.Election.Manage is enforced
-- explicitly via [HasPermission("Members.Election.Manage")] on the
-- candidate/ballot/tally endpoints.
--
-- These permissions are added under the EXISTING Members module (Id
-- …0003 — the same module used by Members.Brokerage.* / Members.Admin.* /
-- Members.InvestmentFunds.* in 004/011/015/024), NOT a new module: the
-- General Meeting feature lives inside KSS.Service.SEBA_ERP_Members and its
-- permission codes keep the existing "Members." prefix, so per the
-- established convention in this folder (a new Module row is only created
-- when a permission set introduces a NEW prefix — see 017/025/026/021 vs.
-- 009/015/024 reusing an existing module) it belongs on the existing
-- module. Only a new "Meeting" Resource Id is added under that module.
--
-- Layout follows 025/026: idempotent inserts, fixed Guids identical across
-- Dev + Prod, soft-delete aware grants. Module/Resource Ids are cross-DB
-- catalog metadata (no local FK) — a matching KSS_Common Resource row is
-- optional and can be backfilled later.
--
-- Apply with `sqlcmd -f 65001 -C` to KSS_Auth_Dev (then KSS_Auth_Prod when approved).

SET QUOTED_IDENTIFIER ON;
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @system UNIQUEIDENTIFIER = '00000000-0000-0000-0000-000000000001';
DECLARE @now    DATETIME2        = SYSUTCDATETIME();
DECLARE @fa SMALLINT = 12;
DECLARE @en SMALLINT = 10;

-- Existing Members module Id (reused — see header note) + new Meeting
-- resource Id (cross-DB catalog metadata; no local FK)
DECLARE @modMembers UNIQUEIDENTIFIER = '019F1000-0000-7001-8000-000000000003';
DECLARE @resMeeting UNIQUEIDENTIFIER = '019F1000-0000-7003-8000-000000000023';

BEGIN TRANSACTION;

-- ─────────────────────────────────────────────────────────────────────────
-- Role
-- ─────────────────────────────────────────────────────────────────────────
DECLARE @roles TABLE (
    Code VARCHAR(50), Id UNIQUEIDENTIFIER,
    EnName NVARCHAR(200), EnDesc NVARCHAR(400),
    FaName NVARCHAR(200), FaDesc NVARCHAR(400)
);
INSERT INTO @roles (Code, Id, EnName, EnDesc, FaName, FaDesc) VALUES
 ('MeetingManager', '019F1100-0000-7100-8000-000000000023', N'Meeting Manager', N'Full management of general meetings, agenda items and elections', N'مدیر مجمع', N'مدیریت کامل مجامع عمومی، دستور جلسات و انتخابات');

INSERT INTO dbo.[Role] (Id, Code, ModuleId)
SELECT x.Id, x.Code, @modMembers FROM @roles x
WHERE NOT EXISTS (SELECT 1 FROM dbo.[Role] r WHERE r.Code = x.Code);

INSERT INTO dbo.RoleTranslation (RoleId, LanguageId, Name, Description)
SELECT r.Id, @en, x.EnName, x.EnDesc FROM @roles x JOIN dbo.[Role] r ON r.Code = x.Code
WHERE NOT EXISTS (SELECT 1 FROM dbo.RoleTranslation t WHERE t.RoleId = r.Id AND t.LanguageId = @en);

INSERT INTO dbo.RoleTranslation (RoleId, LanguageId, Name, Description)
SELECT r.Id, @fa, x.FaName, x.FaDesc FROM @roles x JOIN dbo.[Role] r ON r.Code = x.Code
WHERE NOT EXISTS (SELECT 1 FROM dbo.RoleTranslation t WHERE t.RoleId = r.Id AND t.LanguageId = @fa);

-- ─────────────────────────────────────────────────────────────────────────
-- Permissions
-- ─────────────────────────────────────────────────────────────────────────
DECLARE @perms TABLE (
    Code VARCHAR(100), Id UNIQUEIDENTIFIER,
    EnName NVARCHAR(200), EnDesc NVARCHAR(400),
    FaName NVARCHAR(200), FaDesc NVARCHAR(400)
);
INSERT INTO @perms (Code, Id, EnName, EnDesc, FaName, FaDesc) VALUES
 ('Members.Meeting.Read',    '019F1100-0000-7102-8000-000000000018', N'View General Meetings',   N'View general meetings, agenda items and their details',       N'مشاهده مجامع عمومی', N'مشاهده مجامع عمومی، دستور جلسات و جزئیات آن‌ها'),
 ('Members.Meeting.Modify',  '019F1100-0000-7102-8000-000000000019', N'Manage General Meetings', N'Create, edit and delete general meetings and agenda items',   N'مدیریت مجامع عمومی', N'ثبت، ویرایش و حذف مجامع عمومی و دستور جلسات'),
 ('Members.Election.Manage', '019F1100-0000-7102-8000-000000000020', N'Manage Elections',        N'Manage candidate registration, ballots and election results', N'مدیریت انتخابات',   N'مدیریت ثبت‌نام نامزدها، آرا و نتایج انتخابات');

INSERT INTO dbo.Permission (Id, Code, ModuleId, ResourceId)
SELECT x.Id, x.Code, @modMembers, @resMeeting FROM @perms x
WHERE NOT EXISTS (SELECT 1 FROM dbo.Permission p WHERE p.Code = x.Code);

INSERT INTO dbo.PermissionTranslation (PermissionId, LanguageId, Name, Description)
SELECT p.Id, @en, x.EnName, x.EnDesc FROM @perms x JOIN dbo.Permission p ON p.Code = x.Code
WHERE NOT EXISTS (SELECT 1 FROM dbo.PermissionTranslation t WHERE t.PermissionId = p.Id AND t.LanguageId = @en);

INSERT INTO dbo.PermissionTranslation (PermissionId, LanguageId, Name, Description)
SELECT p.Id, @fa, x.FaName, x.FaDesc FROM @perms x JOIN dbo.Permission p ON p.Code = x.Code
WHERE NOT EXISTS (SELECT 1 FROM dbo.PermissionTranslation t WHERE t.PermissionId = p.Id AND t.LanguageId = @fa);

-- ─────────────────────────────────────────────────────────────────────────
-- Grants (role → permission), soft-delete aware
-- ─────────────────────────────────────────────────────────────────────────
DECLARE @grants TABLE (RoleCode VARCHAR(50), PermCode VARCHAR(100));
INSERT INTO @grants (RoleCode, PermCode) VALUES
 -- MeetingManager: everything
 ('MeetingManager','Members.Meeting.Read'),('MeetingManager','Members.Meeting.Modify'),
 ('MeetingManager','Members.Election.Manage'),
 -- SuperAdmin: everything
 ('SuperAdmin','Members.Meeting.Read'),('SuperAdmin','Members.Meeting.Modify'),
 ('SuperAdmin','Members.Election.Manage');

INSERT INTO dbo.RolePermission (RoleId, PermissionId, CreatedBy, CreatedAt)
SELECT r.Id, p.Id, @system, @now
FROM @grants g
JOIN dbo.[Role] r ON r.Code = g.RoleCode
JOIN dbo.Permission p ON p.Code = g.PermCode
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.RolePermission rp
    WHERE rp.RoleId = r.Id AND rp.PermissionId = p.Id AND rp.DeletedAt IS NULL
);

COMMIT TRANSACTION;

-- Report
SELECT r.Code AS Role, COUNT(*) AS Grants
FROM dbo.RolePermission rp
JOIN dbo.[Role] r ON r.Id = rp.RoleId
JOIN dbo.Permission p ON p.Id = rp.PermissionId
WHERE p.Code IN ('Members.Meeting.Read', 'Members.Meeting.Modify', 'Members.Election.Manage') AND rp.DeletedAt IS NULL
GROUP BY r.Code
ORDER BY r.Code;

PRINT N'Migration 028 applied: General Meeting permissions (3) + MeetingManager role + grants';
