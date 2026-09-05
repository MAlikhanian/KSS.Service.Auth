-- 026_add_project_permissions_and_roles.sql
--
-- Project (پروژه) module RBAC: 4 permissions (2 section CRUD pairs:
-- Project.Project.* and Project.Worksite.*) and 2 roles (Admin / Viewer),
-- with en + fa translations and role→permission grants. SuperAdmin gets all 4.
--
-- Section CRUD perms are auto-enforced by the Project service's
-- [PermissionGroup] filter (Project.<Section>.Read / .Modify).
--
-- Layout follows 025: idempotent inserts, fixed Guids identical across
-- Dev + Prod, soft-delete aware grants. Module/Resource Ids are cross-DB
-- catalog metadata (no local FK); a matching KSS_Common Module row is
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

-- New Project module + resource Ids (cross-DB catalog metadata; no local FK)
DECLARE @modProject UNIQUEIDENTIFIER = '019F1000-0000-7001-8000-000000000007';
DECLARE @resProject UNIQUEIDENTIFIER = '019F1000-0000-7003-8000-000000000070';

BEGIN TRANSACTION;

-- ─────────────────────────────────────────────────────────────────────────
-- Roles
-- ─────────────────────────────────────────────────────────────────────────
DECLARE @roles TABLE (
    Code VARCHAR(50), Id UNIQUEIDENTIFIER,
    EnName NVARCHAR(200), EnDesc NVARCHAR(400),
    FaName NVARCHAR(200), FaDesc NVARCHAR(400)
);
INSERT INTO @roles (Code, Id, EnName, EnDesc, FaName, FaDesc) VALUES
 ('ProjectAdmin',  '019F1100-0000-7100-8000-000000000021', N'Project Admin',  N'Full control of projects and worksites',          N'مدیر پروژه',    N'مدیریت کامل پروژه‌ها و کارگاه‌ها'),
 ('ProjectViewer', '019F1100-0000-7100-8000-000000000022', N'Project Viewer', N'Read-only access to projects and worksites',      N'بیننده پروژه',  N'دسترسی فقط‌خواندنی به پروژه‌ها و کارگاه‌ها');

INSERT INTO dbo.[Role] (Id, Code, ModuleId)
SELECT x.Id, x.Code, @modProject FROM @roles x
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
 ('Project.Project.Read',    '019F1100-0000-7102-8000-000000000013', N'View Projects',    N'View projects',                       N'مشاهده پروژه',  N'مشاهده پروژه‌ها'),
 ('Project.Project.Modify',  '019F1100-0000-7102-8000-000000000014', N'Manage Projects',  N'Create, edit and delete projects',    N'مدیریت پروژه', N'ثبت، ویرایش و حذف پروژه‌ها'),
 ('Project.Worksite.Read',   '019F1100-0000-7102-8000-000000000015', N'View Worksites',   N'View worksites',                      N'مشاهده کارگاه', N'مشاهده کارگاه‌ها'),
 ('Project.Worksite.Modify', '019F1100-0000-7102-8000-000000000016', N'Manage Worksites', N'Create, edit and delete worksites',   N'مدیریت کارگاه', N'ثبت، ویرایش و حذف کارگاه‌ها');

INSERT INTO dbo.Permission (Id, Code, ModuleId, ResourceId)
SELECT x.Id, x.Code, @modProject, @resProject FROM @perms x
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
 -- Admin: everything
 ('ProjectAdmin','Project.Project.Read'),('ProjectAdmin','Project.Project.Modify'),
 ('ProjectAdmin','Project.Worksite.Read'),('ProjectAdmin','Project.Worksite.Modify'),
 -- Viewer: reads
 ('ProjectViewer','Project.Project.Read'),('ProjectViewer','Project.Worksite.Read'),
 -- SuperAdmin: everything
 ('SuperAdmin','Project.Project.Read'),('SuperAdmin','Project.Project.Modify'),
 ('SuperAdmin','Project.Worksite.Read'),('SuperAdmin','Project.Worksite.Modify');

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
WHERE p.Code LIKE 'Project.%' AND rp.DeletedAt IS NULL
GROUP BY r.Code
ORDER BY r.Code;

PRINT N'Migration 026 applied: Project permissions (4) + roles (2) + grants';
