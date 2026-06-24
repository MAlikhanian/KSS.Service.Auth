-- 021_rename_assets_to_portfolio_personasset.sql
--
-- Aligns permission + role names with the actual service boundary. The
-- /api/person/asset/* endpoints are served by KSS.Service.Portfolio whose
-- main entity is PersonAsset. The old Person.Assets.{Read,Modify} perms
-- were a misnomer — they belong under a Portfolio module.
--
-- Migration (Option B + Option 2):
--   • Create Portfolio.PersonAsset.{Read,Modify} permissions + translations
--   • Create new PortfolioAdmin / PortfolioMember / PortfolioViewer roles
--   • Grant new perms to: SuperAdmin, Portfolio*, CreditRating*
--     (CreditRating* roles get the new perm directly — the Portfolio
--      service's old CreditRating.Assessment fallback in
--      PermissionAuthorizationFilter is being removed in the same change set)
--   • Drop old Person.Assets.* RolePermission grants
--   • Drop old Person.Assets.* PermissionTranslation rows
--   • Drop old Person.Assets.{Read,Modify} Permission rows
--
-- Depends on Common migration 021 (creates the portfolio Module + PersonAsset Resource).
--
-- Apply with `sqlcmd -f 65001` to KSS_Auth_Prod and KSS_Auth_Dev.

SET QUOTED_IDENTIFIER ON;
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @system UNIQUEIDENTIFIER = '00000000-0000-0000-0000-000000000001';
DECLARE @now DATETIME2 = SYSUTCDATETIME();
DECLARE @fa SMALLINT = 12;
DECLARE @en SMALLINT = 10;

-- ── Module + Resource IDs (must match Common migration 021)
DECLARE @modPortfolio  UNIQUEIDENTIFIER = '019f1000-0000-7001-8000-000000000007';
DECLARE @rPersonAsset  UNIQUEIDENTIFIER = '019f1000-0000-7003-8000-000000000080';

-- ── New permission IDs (fresh GUIDs)
DECLARE @pRead   UNIQUEIDENTIFIER = NEWID();
DECLARE @pModify UNIQUEIDENTIFIER = NEWID();

-- ── New role IDs (next slots in 019f1100-0000-7100-8000-* sequence)
DECLARE @rPortfolioAdmin   UNIQUEIDENTIFIER = '019f1100-0000-7100-8000-000000000010';
DECLARE @rPortfolioMember  UNIQUEIDENTIFIER = '019f1100-0000-7100-8000-000000000011';
DECLARE @rPortfolioViewer  UNIQUEIDENTIFIER = '019f1100-0000-7100-8000-000000000012';

DECLARE @rSuper             UNIQUEIDENTIFIER = (SELECT Id FROM dbo.[Role] WHERE Code = 'SuperAdmin');
DECLARE @rCreditAdmin       UNIQUEIDENTIFIER = (SELECT Id FROM dbo.[Role] WHERE Code = 'CreditRatingAdmin');
DECLARE @rCreditMember      UNIQUEIDENTIFIER = (SELECT Id FROM dbo.[Role] WHERE Code = 'CreditRatingMember');
DECLARE @rCreditViewer      UNIQUEIDENTIFIER = (SELECT Id FROM dbo.[Role] WHERE Code = 'CreditRatingViewer');

IF @rSuper        IS NULL THROW 51200, 'SuperAdmin role not found', 1;
IF @rCreditAdmin  IS NULL THROW 51201, 'CreditRatingAdmin role not found', 1;
IF @rCreditMember IS NULL THROW 51202, 'CreditRatingMember role not found', 1;
IF @rCreditViewer IS NULL THROW 51203, 'CreditRatingViewer role not found', 1;

BEGIN TRANSACTION;

-- ── Step 1: Create the new permissions
IF NOT EXISTS (SELECT 1 FROM dbo.Permission WHERE Code = 'Portfolio.PersonAsset.Read')
    INSERT INTO dbo.Permission (Id, Code, ModuleId, ResourceId)
    VALUES (@pRead, 'Portfolio.PersonAsset.Read', @modPortfolio, @rPersonAsset);
ELSE SET @pRead = (SELECT Id FROM dbo.Permission WHERE Code = 'Portfolio.PersonAsset.Read');

IF NOT EXISTS (SELECT 1 FROM dbo.Permission WHERE Code = 'Portfolio.PersonAsset.Modify')
    INSERT INTO dbo.Permission (Id, Code, ModuleId, ResourceId)
    VALUES (@pModify, 'Portfolio.PersonAsset.Modify', @modPortfolio, @rPersonAsset);
ELSE SET @pModify = (SELECT Id FROM dbo.Permission WHERE Code = 'Portfolio.PersonAsset.Modify');

-- Permission translations
IF NOT EXISTS (SELECT 1 FROM dbo.PermissionTranslation WHERE PermissionId = @pRead AND LanguageId = @fa)
    INSERT INTO dbo.PermissionTranslation (PermissionId, LanguageId, Name, Description)
    VALUES (@pRead, @fa, N'مشاهده دارایی‌های اشخاص', N'مشاهده فهرست و جزئیات دارایی‌های ثبت‌شده برای هر شخص');
IF NOT EXISTS (SELECT 1 FROM dbo.PermissionTranslation WHERE PermissionId = @pRead AND LanguageId = @en)
    INSERT INTO dbo.PermissionTranslation (PermissionId, LanguageId, Name, Description)
    VALUES (@pRead, @en, N'View Person Assets', N'View the list and detail of assets recorded per person');

IF NOT EXISTS (SELECT 1 FROM dbo.PermissionTranslation WHERE PermissionId = @pModify AND LanguageId = @fa)
    INSERT INTO dbo.PermissionTranslation (PermissionId, LanguageId, Name, Description)
    VALUES (@pModify, @fa, N'ویرایش دارایی‌های اشخاص', N'افزودن، ویرایش و حذف دارایی‌های ثبت‌شده برای هر شخص');
IF NOT EXISTS (SELECT 1 FROM dbo.PermissionTranslation WHERE PermissionId = @pModify AND LanguageId = @en)
    INSERT INTO dbo.PermissionTranslation (PermissionId, LanguageId, Name, Description)
    VALUES (@pModify, @en, N'Modify Person Assets', N'Add, edit, delete assets recorded per person');

-- ── Step 2: Create new Portfolio* roles
IF NOT EXISTS (SELECT 1 FROM dbo.[Role] WHERE Code = 'PortfolioAdmin')
    INSERT INTO dbo.[Role] (Id, Code, ModuleId) VALUES (@rPortfolioAdmin, 'PortfolioAdmin', @modPortfolio);
ELSE SET @rPortfolioAdmin = (SELECT Id FROM dbo.[Role] WHERE Code = 'PortfolioAdmin');

IF NOT EXISTS (SELECT 1 FROM dbo.[Role] WHERE Code = 'PortfolioMember')
    INSERT INTO dbo.[Role] (Id, Code, ModuleId) VALUES (@rPortfolioMember, 'PortfolioMember', @modPortfolio);
ELSE SET @rPortfolioMember = (SELECT Id FROM dbo.[Role] WHERE Code = 'PortfolioMember');

IF NOT EXISTS (SELECT 1 FROM dbo.[Role] WHERE Code = 'PortfolioViewer')
    INSERT INTO dbo.[Role] (Id, Code, ModuleId) VALUES (@rPortfolioViewer, 'PortfolioViewer', @modPortfolio);
ELSE SET @rPortfolioViewer = (SELECT Id FROM dbo.[Role] WHERE Code = 'PortfolioViewer');

-- Role translations
IF NOT EXISTS (SELECT 1 FROM dbo.RoleTranslation WHERE RoleId = @rPortfolioAdmin AND LanguageId = @fa)
    INSERT INTO dbo.RoleTranslation (RoleId, LanguageId, Name, Description) VALUES (@rPortfolioAdmin, @fa, N'مدیر پرتفوی', N'مدیریت کامل دارایی‌های اشخاص');
IF NOT EXISTS (SELECT 1 FROM dbo.RoleTranslation WHERE RoleId = @rPortfolioAdmin AND LanguageId = @en)
    INSERT INTO dbo.RoleTranslation (RoleId, LanguageId, Name, Description) VALUES (@rPortfolioAdmin, @en, N'Portfolio Admin', N'Full management of person assets');

IF NOT EXISTS (SELECT 1 FROM dbo.RoleTranslation WHERE RoleId = @rPortfolioMember AND LanguageId = @fa)
    INSERT INTO dbo.RoleTranslation (RoleId, LanguageId, Name, Description) VALUES (@rPortfolioMember, @fa, N'عضو پرتفوی', N'مشاهده فقط‌خواندنی دارایی‌های اشخاص');
IF NOT EXISTS (SELECT 1 FROM dbo.RoleTranslation WHERE RoleId = @rPortfolioMember AND LanguageId = @en)
    INSERT INTO dbo.RoleTranslation (RoleId, LanguageId, Name, Description) VALUES (@rPortfolioMember, @en, N'Portfolio Member', N'Read-only access to person assets');

IF NOT EXISTS (SELECT 1 FROM dbo.RoleTranslation WHERE RoleId = @rPortfolioViewer AND LanguageId = @fa)
    INSERT INTO dbo.RoleTranslation (RoleId, LanguageId, Name, Description) VALUES (@rPortfolioViewer, @fa, N'بازبین پرتفوی', N'مشاهده فقط‌خواندنی دارایی‌های اشخاص');
IF NOT EXISTS (SELECT 1 FROM dbo.RoleTranslation WHERE RoleId = @rPortfolioViewer AND LanguageId = @en)
    INSERT INTO dbo.RoleTranslation (RoleId, LanguageId, Name, Description) VALUES (@rPortfolioViewer, @en, N'Portfolio Viewer', N'Read-only access to person assets');

-- ── Step 3: Grant the new perms (idempotent)
-- SuperAdmin + PortfolioAdmin + CreditRatingAdmin all get Read + Modify.
-- PortfolioMember/Viewer + CreditRatingMember/Viewer get Read only.
INSERT INTO dbo.RolePermission (RoleId, PermissionId, CreatedBy, CreatedAt)
SELECT v.RoleId, v.PermissionId, @system, @now
FROM (VALUES
    (@rSuper,            @pRead),
    (@rSuper,            @pModify),
    (@rPortfolioAdmin,   @pRead),
    (@rPortfolioAdmin,   @pModify),
    (@rPortfolioMember,  @pRead),
    (@rPortfolioViewer,  @pRead),
    (@rCreditAdmin,      @pRead),
    (@rCreditAdmin,      @pModify),
    (@rCreditMember,     @pRead),
    (@rCreditViewer,     @pRead)
) AS v(RoleId, PermissionId)
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.RolePermission rp
    WHERE rp.RoleId = v.RoleId AND rp.PermissionId = v.PermissionId
);

-- ── Step 4: Drop the old Person.Assets.* permissions (cascade-clean)
DECLARE @pOldRead   UNIQUEIDENTIFIER = (SELECT Id FROM dbo.Permission WHERE Code = 'Person.Assets.Read');
DECLARE @pOldModify UNIQUEIDENTIFIER = (SELECT Id FROM dbo.Permission WHERE Code = 'Person.Assets.Modify');

IF @pOldRead IS NOT NULL
BEGIN
    DELETE FROM dbo.RolePermission WHERE PermissionId = @pOldRead;
    DELETE FROM dbo.PermissionTranslation WHERE PermissionId = @pOldRead;
    DELETE FROM dbo.Permission WHERE Id = @pOldRead;
END
IF @pOldModify IS NOT NULL
BEGIN
    DELETE FROM dbo.RolePermission WHERE PermissionId = @pOldModify;
    DELETE FROM dbo.PermissionTranslation WHERE PermissionId = @pOldModify;
    DELETE FROM dbo.Permission WHERE Id = @pOldModify;
END

COMMIT TRANSACTION;

-- Report
SELECT p.Code,
       MAX(CASE WHEN pt.LanguageId = @fa THEN pt.Name END) AS Persian,
       MAX(CASE WHEN pt.LanguageId = @en THEN pt.Name END) AS English
FROM dbo.Permission p
LEFT JOIN dbo.PermissionTranslation pt ON pt.PermissionId = p.Id
WHERE p.Code LIKE 'Portfolio.%' OR p.Code LIKE 'Person.Assets.%'
GROUP BY p.Code
ORDER BY p.Code;

SELECT r.Code AS Role, p.Code AS Permission
FROM dbo.RolePermission rp
JOIN dbo.[Role] r ON r.Id = rp.RoleId
JOIN dbo.Permission p ON p.Id = rp.PermissionId
WHERE p.Code LIKE 'Portfolio.%'
ORDER BY r.Code, p.Code;

PRINT '021_rename_assets_to_portfolio_personasset.sql applied successfully.';
