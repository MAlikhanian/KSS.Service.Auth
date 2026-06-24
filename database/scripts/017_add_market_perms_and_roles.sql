-- 017_add_market_perms_and_roles.sql
--
-- Adds Market.Reference.Read / Modify permissions plus a MarketAdmin and
-- MarketViewer role pair. Mirrors the pattern used by Person/Company/Members
-- module admin + viewer roles.
--
-- Grants:
--   • SuperAdmin   → Read + Modify
--   • MarketAdmin  → Read + Modify
--   • MarketViewer → Read only
--
-- Depends on Common migration 018 (creates the market Module + Reference Resource).
--
-- Apply with `sqlcmd -f 65001` to KSS_Auth_Prod and KSS_Auth_Dev.

SET QUOTED_IDENTIFIER ON;
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @system UNIQUEIDENTIFIER = '00000000-0000-0000-0000-000000000001';
DECLARE @now DATETIME2 = SYSUTCDATETIME();
DECLARE @fa SMALLINT = 12;
DECLARE @en SMALLINT = 10;

DECLARE @modMarket    UNIQUEIDENTIFIER = '019f1000-0000-7001-8000-000000000006';
DECLARE @rReference   UNIQUEIDENTIFIER = '019f1000-0000-7003-8000-000000000060';

DECLARE @pRead   UNIQUEIDENTIFIER = NEWID();
DECLARE @pModify UNIQUEIDENTIFIER = NEWID();

DECLARE @rMarketAdmin  UNIQUEIDENTIFIER = '019f1100-0000-7100-8000-000000000007';
DECLARE @rMarketViewer UNIQUEIDENTIFIER = '019f1100-0000-7100-8000-000000000008';

DECLARE @rSuper UNIQUEIDENTIFIER = (SELECT Id FROM dbo.[Role] WHERE Code = 'SuperAdmin');
IF @rSuper IS NULL THROW 51100, 'SuperAdmin role not found', 1;

BEGIN TRANSACTION;

-- ── Step 1: Permissions
IF NOT EXISTS (SELECT 1 FROM dbo.Permission WHERE Code = 'Market.Reference.Read')
    INSERT INTO dbo.Permission (Id, Code, ModuleId, ResourceId)
    VALUES (@pRead, 'Market.Reference.Read', @modMarket, @rReference);
ELSE SET @pRead = (SELECT Id FROM dbo.Permission WHERE Code = 'Market.Reference.Read');

IF NOT EXISTS (SELECT 1 FROM dbo.Permission WHERE Code = 'Market.Reference.Modify')
    INSERT INTO dbo.Permission (Id, Code, ModuleId, ResourceId)
    VALUES (@pModify, 'Market.Reference.Modify', @modMarket, @rReference);
ELSE SET @pModify = (SELECT Id FROM dbo.Permission WHERE Code = 'Market.Reference.Modify');

-- Permission translations (fa + en for each)
IF NOT EXISTS (SELECT 1 FROM dbo.PermissionTranslation WHERE PermissionId = @pRead AND LanguageId = @fa)
    INSERT INTO dbo.PermissionTranslation (PermissionId, LanguageId, Name, Description)
    VALUES (@pRead, @fa, N'مشاهده داده‌های بازار', N'مشاهده داده‌های مرجع بازار (دارایی‌ها، بخش‌ها، انواع)');
IF NOT EXISTS (SELECT 1 FROM dbo.PermissionTranslation WHERE PermissionId = @pRead AND LanguageId = @en)
    INSERT INTO dbo.PermissionTranslation (PermissionId, LanguageId, Name, Description)
    VALUES (@pRead, @en, N'View Market Reference', N'View market reference data (assets, sectors, types)');

IF NOT EXISTS (SELECT 1 FROM dbo.PermissionTranslation WHERE PermissionId = @pModify AND LanguageId = @fa)
    INSERT INTO dbo.PermissionTranslation (PermissionId, LanguageId, Name, Description)
    VALUES (@pModify, @fa, N'ویرایش داده‌های بازار', N'افزودن، ویرایش و حذف داده‌های مرجع بازار');
IF NOT EXISTS (SELECT 1 FROM dbo.PermissionTranslation WHERE PermissionId = @pModify AND LanguageId = @en)
    INSERT INTO dbo.PermissionTranslation (PermissionId, LanguageId, Name, Description)
    VALUES (@pModify, @en, N'Modify Market Reference', N'Add, edit, delete market reference data');

-- ── Step 2: Roles
IF NOT EXISTS (SELECT 1 FROM dbo.[Role] WHERE Code = 'MarketAdmin')
    INSERT INTO dbo.[Role] (Id, Code, ModuleId) VALUES (@rMarketAdmin, 'MarketAdmin', @modMarket);
ELSE SET @rMarketAdmin = (SELECT Id FROM dbo.[Role] WHERE Code = 'MarketAdmin');

IF NOT EXISTS (SELECT 1 FROM dbo.[Role] WHERE Code = 'MarketViewer')
    INSERT INTO dbo.[Role] (Id, Code, ModuleId) VALUES (@rMarketViewer, 'MarketViewer', @modMarket);
ELSE SET @rMarketViewer = (SELECT Id FROM dbo.[Role] WHERE Code = 'MarketViewer');

-- Role translations
IF NOT EXISTS (SELECT 1 FROM dbo.RoleTranslation WHERE RoleId = @rMarketAdmin AND LanguageId = @fa)
    INSERT INTO dbo.RoleTranslation (RoleId, LanguageId, Name, Description) VALUES (@rMarketAdmin, @fa, N'مدیر بازار', N'مدیریت کامل داده‌های مرجع بازار');
IF NOT EXISTS (SELECT 1 FROM dbo.RoleTranslation WHERE RoleId = @rMarketAdmin AND LanguageId = @en)
    INSERT INTO dbo.RoleTranslation (RoleId, LanguageId, Name, Description) VALUES (@rMarketAdmin, @en, N'Market Admin', N'Full management of market reference data');

IF NOT EXISTS (SELECT 1 FROM dbo.RoleTranslation WHERE RoleId = @rMarketViewer AND LanguageId = @fa)
    INSERT INTO dbo.RoleTranslation (RoleId, LanguageId, Name, Description) VALUES (@rMarketViewer, @fa, N'بازبین بازار', N'مشاهده فقط‌خواندنی داده‌های مرجع بازار');
IF NOT EXISTS (SELECT 1 FROM dbo.RoleTranslation WHERE RoleId = @rMarketViewer AND LanguageId = @en)
    INSERT INTO dbo.RoleTranslation (RoleId, LanguageId, Name, Description) VALUES (@rMarketViewer, @en, N'Market Viewer', N'Read-only view of market reference data');

-- ── Step 3: Role-permission grants (idempotent)
INSERT INTO dbo.RolePermission (RoleId, PermissionId, CreatedBy, CreatedAt)
SELECT v.RoleId, v.PermissionId, @system, @now
FROM (VALUES
    (@rSuper,        @pRead),
    (@rSuper,        @pModify),
    (@rMarketAdmin,  @pRead),
    (@rMarketAdmin,  @pModify),
    (@rMarketViewer, @pRead)
) AS v(RoleId, PermissionId)
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.RolePermission rp
    WHERE rp.RoleId = v.RoleId AND rp.PermissionId = v.PermissionId
);

COMMIT TRANSACTION;

-- Report
SELECT p.Code,
       MAX(CASE WHEN pt.LanguageId = @fa THEN pt.Name END) AS Persian,
       MAX(CASE WHEN pt.LanguageId = @en THEN pt.Name END) AS English
FROM dbo.Permission p
LEFT JOIN dbo.PermissionTranslation pt ON pt.PermissionId = p.Id
WHERE p.Code LIKE 'Market.%'
GROUP BY p.Code
ORDER BY p.Code;

SELECT r.Code AS Role, p.Code AS Permission
FROM dbo.RolePermission rp
JOIN dbo.[Role] r ON r.Id = rp.RoleId
JOIN dbo.Permission p ON p.Id = rp.PermissionId
WHERE p.Code LIKE 'Market.%'
ORDER BY r.Code, p.Code;

PRINT '017_add_market_perms_and_roles.sql applied successfully.';
