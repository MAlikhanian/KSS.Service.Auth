-- 032_add_spm_read_permission.sql
--
-- SPM (Sepinud Portfolio Management) module RBAC: a SINGLE section-level
-- permission, SPM.Read, granted to the existing SuperAdmin role. No new role
-- is created and no UserRole grant is made — everyone who already holds
-- SuperAdmin picks it up through their existing UserRole rows.
--
-- SPM.Read is a whole-module gate, not a per-screen permission. The per-screen
-- codes listed in the TODO in KSS.Client/Web/Spm/config/menu.config.tsx
-- (SPM.Request.*, SPM.Order.*, ...) are deliberately NOT created.
--
-- Module/Resource Ids are cross-DB catalog metadata (no local FK — the Auth
-- database has no dbo.Module or dbo.Resource table); a matching KSS_Common
-- Module row is optional and can be backfilled later. Module NN 09 and
-- Resource NN 90 are the next free values after Milan (module 08 / resource 80).
--
-- Layout follows 026/029: idempotent inserts, fixed Guids identical across
-- both catalogs, soft-delete aware grants, en + fa translations.
--
-- Target: the DEV catalog. The assertion below is the guard; a sibling script
-- carries the same body with the assertion changed for the live catalog.
-- Apply with `sqlcmd -f 65001 -C`.

IF DB_NAME() <> N'KSS_Auth_Dev' RAISERROR('WRONG DATABASE', 20, 1) WITH LOG;

SET QUOTED_IDENTIFIER ON;
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @system UNIQUEIDENTIFIER = '00000000-0000-0000-0000-000000000001';
DECLARE @now    DATETIME2        = SYSUTCDATETIME();
DECLARE @fa SMALLINT = 12;
DECLARE @en SMALLINT = 10;

-- New SPM module + resource Ids (cross-DB catalog metadata; no local FK)
DECLARE @modSpm UNIQUEIDENTIFIER = '019F1000-0000-7001-8000-000000000009';
DECLARE @resSpm UNIQUEIDENTIFIER = '019F1000-0000-7003-8000-000000000090';

BEGIN TRANSACTION;

-- ─────────────────────────────────────────────────────────────────────────
-- Permissions
-- ─────────────────────────────────────────────────────────────────────────
DECLARE @perms TABLE (
    Code VARCHAR(100), Id UNIQUEIDENTIFIER,
    EnName NVARCHAR(200), EnDesc NVARCHAR(400),
    FaName NVARCHAR(200), FaDesc NVARCHAR(400)
);
INSERT INTO @perms (Code, Id, EnName, EnDesc, FaName, FaDesc) VALUES
 ('SPM.Read', '019F1100-0000-7102-8000-000000000026', N'View SPM', N'View the SPM investment section', N'مشاهده SPM', N'مشاهده بخش سرمایه‌گذاری SPM');

INSERT INTO dbo.Permission (Id, Code, ModuleId, ResourceId)
SELECT x.Id, x.Code, @modSpm, @resSpm FROM @perms x
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
 -- SuperAdmin: the whole SPM section
 ('SuperAdmin','SPM.Read');

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
WHERE p.Code LIKE 'SPM.%' AND rp.DeletedAt IS NULL
GROUP BY r.Code
ORDER BY r.Code;

PRINT N'Migration 032 applied: SPM permission (1) + roles (0) + grants';
