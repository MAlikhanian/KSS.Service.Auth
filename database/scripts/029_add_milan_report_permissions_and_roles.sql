-- 029_add_milan_report_permissions_and_roles.sql
--
-- Milan (میلان — MabnaERP reports bridge, KSS.Service.Report.MPF_ERP_Mabna) module RBAC:
-- 1 read permission (Milan.Report.Read) and 2 roles (Admin / Viewer), with en + fa
-- translations and role→permission grants. SuperAdmin gets it too.
--
-- The Milan reports (customer Rial statement, صورت وضعیت ریالی) are read-only; more
-- report permissions can be added as the module grows.
--
-- Layout follows 026: idempotent inserts, fixed Guids identical across Dev + Prod,
-- soft-delete aware grants. Module/Resource Ids are cross-DB catalog metadata (no local FK).
--
-- Apply with `sqlcmd -f 65001 -C` to KSS_Auth_Dev (then KSS_Auth_Prod when approved).

SET QUOTED_IDENTIFIER ON;
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @system UNIQUEIDENTIFIER = '00000000-0000-0000-0000-000000000001';
DECLARE @now    DATETIME2        = SYSUTCDATETIME();
DECLARE @fa SMALLINT = 12;
DECLARE @en SMALLINT = 10;

-- New Milan module + resource Ids (cross-DB catalog metadata; no local FK)
DECLARE @modMilan UNIQUEIDENTIFIER = '019F1000-0000-7001-8000-000000000008';
DECLARE @resMilan UNIQUEIDENTIFIER = '019F1000-0000-7003-8000-000000000080';

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
 ('MilanAdmin',  '019F1100-0000-7100-8000-000000000024', N'Milan Admin',  N'Full access to Milan (MabnaERP) reports', N'مدیر میلان',   N'دسترسی کامل به گزارش‌های میلان'),
 ('MilanViewer', '019F1100-0000-7100-8000-000000000025', N'Milan Viewer', N'Read-only access to Milan reports',       N'بیننده میلان', N'دسترسی فقط‌خواندنی به گزارش‌های میلان');

INSERT INTO dbo.[Role] (Id, Code, ModuleId)
SELECT x.Id, x.Code, @modMilan FROM @roles x
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
 ('Milan.Report.Read', '019F1100-0000-7102-8000-000000000021', N'View Milan Reports', N'View Milan (MabnaERP) reports such as the customer Rial statement', N'مشاهده گزارش‌های میلان', N'مشاهده گزارش‌های میلان مانند صورت وضعیت ریالی مشتری');

INSERT INTO dbo.Permission (Id, Code, ModuleId, ResourceId)
SELECT x.Id, x.Code, @modMilan, @resMilan FROM @perms x
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
 ('MilanAdmin','Milan.Report.Read'),
 ('MilanViewer','Milan.Report.Read'),
 ('SuperAdmin','Milan.Report.Read');

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
WHERE p.Code LIKE 'Milan.%' AND rp.DeletedAt IS NULL
GROUP BY r.Code
ORDER BY r.Code;

PRINT N'Migration 029 applied: Milan permission (1) + roles (2) + grants';
