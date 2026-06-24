-- 013_add_system_security_permissions.sql
--
-- Adds 6 new permissions under the System module's Security sub-group:
--   System.Security.Role.Read           / .Modify
--   System.Security.Permission.Read     / .Modify
--   System.Security.RolePermission.Read / .Modify
--
-- 4-part codes intentionally — the existing 3-part convention doesn't carry
-- the System > Security nesting. The auth handler does exact-match string
-- comparison, so 4-part codes are stored and matched identically.
--
-- Depends on Common migration 015 (which inserts the system Module + 3 Resources).
--
-- Grants: SuperAdmin only.
--
-- Apply with `sqlcmd -f 65001` to KSS_Auth_Prod and KSS_Auth_Dev.

SET QUOTED_IDENTIFIER ON;
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @system UNIQUEIDENTIFIER = '00000000-0000-0000-0000-000000000001';
DECLARE @now DATETIME2 = SYSUTCDATETIME();
DECLARE @fa SMALLINT = 12;
DECLARE @en SMALLINT = 10;

DECLARE @modSystem               UNIQUEIDENTIFIER = '019f1000-0000-7001-8000-000000000005';
DECLARE @rSecurityRole           UNIQUEIDENTIFIER = '019f1000-0000-7003-8000-000000000050';
DECLARE @rSecurityPermission     UNIQUEIDENTIFIER = '019f1000-0000-7003-8000-000000000051';
DECLARE @rSecurityRolePermission UNIQUEIDENTIFIER = '019f1000-0000-7003-8000-000000000052';

-- New Permission Guids
DECLARE @pRoleRead       UNIQUEIDENTIFIER = NEWID();
DECLARE @pRoleModify     UNIQUEIDENTIFIER = NEWID();
DECLARE @pPermRead       UNIQUEIDENTIFIER = NEWID();
DECLARE @pPermModify     UNIQUEIDENTIFIER = NEWID();
DECLARE @pRPRead         UNIQUEIDENTIFIER = NEWID();
DECLARE @pRPModify       UNIQUEIDENTIFIER = NEWID();

DECLARE @rSuper UNIQUEIDENTIFIER = (SELECT Id FROM dbo.[Role] WHERE Code = 'SuperAdmin');
IF @rSuper IS NULL THROW 51080, 'SuperAdmin role not found', 1;

BEGIN TRANSACTION;

-- ── Step 1: 6 Permission rows (idempotent — resolve Id if pre-existing)
IF NOT EXISTS (SELECT 1 FROM dbo.Permission WHERE Code = 'System.Security.Role.Read')
    INSERT INTO dbo.Permission (Id, Code, ModuleId, ResourceId)
    VALUES (@pRoleRead, 'System.Security.Role.Read', @modSystem, @rSecurityRole);
ELSE SET @pRoleRead = (SELECT Id FROM dbo.Permission WHERE Code = 'System.Security.Role.Read');

IF NOT EXISTS (SELECT 1 FROM dbo.Permission WHERE Code = 'System.Security.Role.Modify')
    INSERT INTO dbo.Permission (Id, Code, ModuleId, ResourceId)
    VALUES (@pRoleModify, 'System.Security.Role.Modify', @modSystem, @rSecurityRole);
ELSE SET @pRoleModify = (SELECT Id FROM dbo.Permission WHERE Code = 'System.Security.Role.Modify');

IF NOT EXISTS (SELECT 1 FROM dbo.Permission WHERE Code = 'System.Security.Permission.Read')
    INSERT INTO dbo.Permission (Id, Code, ModuleId, ResourceId)
    VALUES (@pPermRead, 'System.Security.Permission.Read', @modSystem, @rSecurityPermission);
ELSE SET @pPermRead = (SELECT Id FROM dbo.Permission WHERE Code = 'System.Security.Permission.Read');

IF NOT EXISTS (SELECT 1 FROM dbo.Permission WHERE Code = 'System.Security.Permission.Modify')
    INSERT INTO dbo.Permission (Id, Code, ModuleId, ResourceId)
    VALUES (@pPermModify, 'System.Security.Permission.Modify', @modSystem, @rSecurityPermission);
ELSE SET @pPermModify = (SELECT Id FROM dbo.Permission WHERE Code = 'System.Security.Permission.Modify');

IF NOT EXISTS (SELECT 1 FROM dbo.Permission WHERE Code = 'System.Security.RolePermission.Read')
    INSERT INTO dbo.Permission (Id, Code, ModuleId, ResourceId)
    VALUES (@pRPRead, 'System.Security.RolePermission.Read', @modSystem, @rSecurityRolePermission);
ELSE SET @pRPRead = (SELECT Id FROM dbo.Permission WHERE Code = 'System.Security.RolePermission.Read');

IF NOT EXISTS (SELECT 1 FROM dbo.Permission WHERE Code = 'System.Security.RolePermission.Modify')
    INSERT INTO dbo.Permission (Id, Code, ModuleId, ResourceId)
    VALUES (@pRPModify, 'System.Security.RolePermission.Modify', @modSystem, @rSecurityRolePermission);
ELSE SET @pRPModify = (SELECT Id FROM dbo.Permission WHERE Code = 'System.Security.RolePermission.Modify');

-- ── Step 2: Translations (fa + en for each — idempotent)
IF NOT EXISTS (SELECT 1 FROM dbo.PermissionTranslation WHERE PermissionId = @pRoleRead AND LanguageId = @fa)
    INSERT INTO dbo.PermissionTranslation (PermissionId, LanguageId, Name, Description) VALUES (@pRoleRead, @fa, N'مشاهده نقش‌ها', N'مشاهده نقش‌های سیستمی');
IF NOT EXISTS (SELECT 1 FROM dbo.PermissionTranslation WHERE PermissionId = @pRoleRead AND LanguageId = @en)
    INSERT INTO dbo.PermissionTranslation (PermissionId, LanguageId, Name, Description) VALUES (@pRoleRead, @en, N'View Roles', N'View system roles');

IF NOT EXISTS (SELECT 1 FROM dbo.PermissionTranslation WHERE PermissionId = @pRoleModify AND LanguageId = @fa)
    INSERT INTO dbo.PermissionTranslation (PermissionId, LanguageId, Name, Description) VALUES (@pRoleModify, @fa, N'ویرایش نقش‌ها', N'افزودن، ویرایش و حذف نقش‌ها');
IF NOT EXISTS (SELECT 1 FROM dbo.PermissionTranslation WHERE PermissionId = @pRoleModify AND LanguageId = @en)
    INSERT INTO dbo.PermissionTranslation (PermissionId, LanguageId, Name, Description) VALUES (@pRoleModify, @en, N'Modify Roles', N'Add, edit, delete roles');

IF NOT EXISTS (SELECT 1 FROM dbo.PermissionTranslation WHERE PermissionId = @pPermRead AND LanguageId = @fa)
    INSERT INTO dbo.PermissionTranslation (PermissionId, LanguageId, Name, Description) VALUES (@pPermRead, @fa, N'مشاهده دسترسی‌ها', N'مشاهده فهرست دسترسی‌ها');
IF NOT EXISTS (SELECT 1 FROM dbo.PermissionTranslation WHERE PermissionId = @pPermRead AND LanguageId = @en)
    INSERT INTO dbo.PermissionTranslation (PermissionId, LanguageId, Name, Description) VALUES (@pPermRead, @en, N'View Permissions', N'View the permission catalog');

IF NOT EXISTS (SELECT 1 FROM dbo.PermissionTranslation WHERE PermissionId = @pPermModify AND LanguageId = @fa)
    INSERT INTO dbo.PermissionTranslation (PermissionId, LanguageId, Name, Description) VALUES (@pPermModify, @fa, N'ویرایش دسترسی‌ها', N'افزودن، ویرایش و حذف دسترسی‌ها');
IF NOT EXISTS (SELECT 1 FROM dbo.PermissionTranslation WHERE PermissionId = @pPermModify AND LanguageId = @en)
    INSERT INTO dbo.PermissionTranslation (PermissionId, LanguageId, Name, Description) VALUES (@pPermModify, @en, N'Modify Permissions', N'Add, edit, delete permissions');

IF NOT EXISTS (SELECT 1 FROM dbo.PermissionTranslation WHERE PermissionId = @pRPRead AND LanguageId = @fa)
    INSERT INTO dbo.PermissionTranslation (PermissionId, LanguageId, Name, Description) VALUES (@pRPRead, @fa, N'مشاهده دسترسی نقش‌ها', N'مشاهده انتساب دسترسی‌ها به نقش‌ها');
IF NOT EXISTS (SELECT 1 FROM dbo.PermissionTranslation WHERE PermissionId = @pRPRead AND LanguageId = @en)
    INSERT INTO dbo.PermissionTranslation (PermissionId, LanguageId, Name, Description) VALUES (@pRPRead, @en, N'View Role-Permissions', N'View role-to-permission assignments');

IF NOT EXISTS (SELECT 1 FROM dbo.PermissionTranslation WHERE PermissionId = @pRPModify AND LanguageId = @fa)
    INSERT INTO dbo.PermissionTranslation (PermissionId, LanguageId, Name, Description) VALUES (@pRPModify, @fa, N'ویرایش دسترسی نقش‌ها', N'انتساب و حذف دسترسی‌ها از نقش‌ها');
IF NOT EXISTS (SELECT 1 FROM dbo.PermissionTranslation WHERE PermissionId = @pRPModify AND LanguageId = @en)
    INSERT INTO dbo.PermissionTranslation (PermissionId, LanguageId, Name, Description) VALUES (@pRPModify, @en, N'Modify Role-Permissions', N'Assign or revoke permissions on roles');

-- ── Step 3: Grant all 6 to SuperAdmin (idempotent)
INSERT INTO dbo.RolePermission (RoleId, PermissionId, CreatedBy, CreatedAt)
SELECT @rSuper, v.PermissionId, @system, @now
FROM (VALUES (@pRoleRead), (@pRoleModify), (@pPermRead), (@pPermModify), (@pRPRead), (@pRPModify)) AS v(PermissionId)
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.RolePermission rp
    WHERE rp.RoleId = @rSuper AND rp.PermissionId = v.PermissionId
);

COMMIT TRANSACTION;

SELECT p.Code,
       MAX(CASE WHEN pt.LanguageId = @fa THEN pt.Name END) AS Persian,
       MAX(CASE WHEN pt.LanguageId = @en THEN pt.Name END) AS English
FROM dbo.Permission p
LEFT JOIN dbo.PermissionTranslation pt ON pt.PermissionId = p.Id
WHERE p.Code LIKE 'System.Security.%'
GROUP BY p.Code
ORDER BY p.Code;

PRINT '013_add_system_security_permissions.sql applied successfully.';
