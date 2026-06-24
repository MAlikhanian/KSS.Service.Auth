-- 009_add_company_access_permissions.sql
--
-- Adds two new permissions for the new /company/access page:
--   • Company.Access.Read
--   • Company.Access.Manage
-- and grants both to SuperAdmin and CompanyAdmin.
--
-- Depends on Common migration 013 (which inserts the
-- Resource row id `019f1000-0000-7003-8000-000000000012`).
--
-- Apply to KSS_Auth_Prod and KSS_Auth_Dev.

SET QUOTED_IDENTIFIER ON;
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @system UNIQUEIDENTIFIER = '00000000-0000-0000-0000-000000000001';
DECLARE @now DATETIME2 = SYSUTCDATETIME();
DECLARE @fa SMALLINT = 12;
DECLARE @en SMALLINT = 10;

DECLARE @moduleCompany    UNIQUEIDENTIFIER = '019f1000-0000-7001-8000-000000000002';
DECLARE @rCompanyAccess   UNIQUEIDENTIFIER = '019f1000-0000-7003-8000-000000000012';

DECLARE @pRead   UNIQUEIDENTIFIER = NEWID();
DECLARE @pManage UNIQUEIDENTIFIER = NEWID();

DECLARE @rSuper   UNIQUEIDENTIFIER = (SELECT Id FROM dbo.[Role] WHERE Code = 'SuperAdmin');
DECLARE @rCompany UNIQUEIDENTIFIER = (SELECT Id FROM dbo.[Role] WHERE Code = 'CompanyAdmin');

IF @rSuper IS NULL   THROW 51011, 'SuperAdmin role not found',   1;
IF @rCompany IS NULL THROW 51012, 'CompanyAdmin role not found', 1;

BEGIN TRANSACTION;

-- ── Step 1: Insert the 2 Permission rows (idempotent)
IF NOT EXISTS (SELECT 1 FROM dbo.Permission WHERE Code = 'Company.Access.Read')
BEGIN
    INSERT INTO dbo.Permission (Id, Code, ModuleId, ResourceId)
    VALUES (@pRead, 'Company.Access.Read', @moduleCompany, @rCompanyAccess);
END
ELSE
BEGIN
    SET @pRead = (SELECT Id FROM dbo.Permission WHERE Code = 'Company.Access.Read');
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Permission WHERE Code = 'Company.Access.Manage')
BEGIN
    INSERT INTO dbo.Permission (Id, Code, ModuleId, ResourceId)
    VALUES (@pManage, 'Company.Access.Manage', @moduleCompany, @rCompanyAccess);
END
ELSE
BEGIN
    SET @pManage = (SELECT Id FROM dbo.Permission WHERE Code = 'Company.Access.Manage');
END;

-- ── Step 2: Translations (fa + en for each permission)
IF NOT EXISTS (SELECT 1 FROM dbo.PermissionTranslation
               WHERE PermissionId = @pRead AND LanguageId = @fa)
    INSERT INTO dbo.PermissionTranslation (PermissionId, LanguageId, Name, Description) VALUES
        (@pRead, @fa, N'مشاهده دسترسی شرکت', N'مشاهده دسترسی‌های اعطا شده روی یک شرکت');

IF NOT EXISTS (SELECT 1 FROM dbo.PermissionTranslation
               WHERE PermissionId = @pRead AND LanguageId = @en)
    INSERT INTO dbo.PermissionTranslation (PermissionId, LanguageId, Name, Description) VALUES
        (@pRead, @en, N'View Company Access', N'View access grants on a company');

IF NOT EXISTS (SELECT 1 FROM dbo.PermissionTranslation
               WHERE PermissionId = @pManage AND LanguageId = @fa)
    INSERT INTO dbo.PermissionTranslation (PermissionId, LanguageId, Name, Description) VALUES
        (@pManage, @fa, N'مدیریت دسترسی شرکت', N'اعطا و حذف دسترسی‌ها روی یک شرکت');

IF NOT EXISTS (SELECT 1 FROM dbo.PermissionTranslation
               WHERE PermissionId = @pManage AND LanguageId = @en)
    INSERT INTO dbo.PermissionTranslation (PermissionId, LanguageId, Name, Description) VALUES
        (@pManage, @en, N'Manage Company Access', N'Grant and revoke access on a company');

-- ── Step 3: Grant both perms to SuperAdmin and CompanyAdmin (4 rows, idempotent)
INSERT INTO dbo.RolePermission (RoleId, PermissionId, CreatedBy, CreatedAt)
SELECT v.RoleId, v.PermissionId, @system, @now
FROM (VALUES
    (@rSuper,   @pRead),
    (@rSuper,   @pManage),
    (@rCompany, @pRead),
    (@rCompany, @pManage)
) AS v(RoleId, PermissionId)
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.RolePermission rp
    WHERE rp.RoleId = v.RoleId AND rp.PermissionId = v.PermissionId
);

COMMIT TRANSACTION;

SELECT r.Code AS Role, p.Code AS Permission
FROM dbo.RolePermission rp
JOIN dbo.[Role] r ON r.Id = rp.RoleId
JOIN dbo.Permission p ON p.Id = rp.PermissionId
WHERE p.Code IN ('Company.Access.Read', 'Company.Access.Manage')
ORDER BY r.Code, p.Code;

PRINT '009_add_company_access_permissions.sql applied successfully.';
