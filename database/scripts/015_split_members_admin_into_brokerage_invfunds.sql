-- 015_split_members_admin_into_brokerage_invfunds.sql
--
-- Companion to Common migration 017. Splits the single Members.Admin permission
-- pair into two domain-specific pairs:
--   • Members.Admin.Read    → renamed to Members.Brokerage.Read    (same Permission.Id)
--   • Members.Admin.Modify  → renamed to Members.Brokerage.Modify  (same Permission.Id)
--   • Members.InvestmentFunds.Read    → NEW permission
--   • Members.InvestmentFunds.Modify  → NEW permission
--
-- Role grants preserved + extended:
--   • Anyone holding the renamed Members.Brokerage.Read|Modify (i.e. the old
--     Admin perms) ALSO gets the new Members.InvestmentFunds.Read|Modify so
--     their existing capability is preserved.
--
-- Apply with `sqlcmd -f 65001` to KSS_Auth_Prod and KSS_Auth_Dev.

SET QUOTED_IDENTIFIER ON;
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @system UNIQUEIDENTIFIER = '00000000-0000-0000-0000-000000000001';
DECLARE @now DATETIME2 = SYSUTCDATETIME();
DECLARE @fa SMALLINT = 12;
DECLARE @en SMALLINT = 10;

DECLARE @modMembers       UNIQUEIDENTIFIER = '019f1000-0000-7001-8000-000000000003';
DECLARE @rBrokerage       UNIQUEIDENTIFIER = '019f1000-0000-7003-8000-000000000021';
DECLARE @rInvestmentFunds UNIQUEIDENTIFIER = '019f1000-0000-7003-8000-000000000022';

DECLARE @pInvFundsRead   UNIQUEIDENTIFIER = NEWID();
DECLARE @pInvFundsModify UNIQUEIDENTIFIER = NEWID();

BEGIN TRANSACTION;

-- ── Step 1: Rename Members.Admin.Read → Members.Brokerage.Read
UPDATE dbo.Permission SET Code = 'Members.Brokerage.Read', ResourceId = @rBrokerage
WHERE Code = 'Members.Admin.Read';

UPDATE dbo.Permission SET Code = 'Members.Brokerage.Modify', ResourceId = @rBrokerage
WHERE Code = 'Members.Admin.Modify';

-- ── Step 2: Update translations (replace "Admin"/"اعضا"→"Brokerage"/"کارگزاری‌ها")
DECLARE @pBrokRead   UNIQUEIDENTIFIER = (SELECT Id FROM dbo.Permission WHERE Code = 'Members.Brokerage.Read');
DECLARE @pBrokModify UNIQUEIDENTIFIER = (SELECT Id FROM dbo.Permission WHERE Code = 'Members.Brokerage.Modify');

UPDATE dbo.PermissionTranslation
SET Name = N'مشاهده کارگزاری‌ها', Description = N'مشاهده اطلاعات کارگزاری‌ها'
WHERE PermissionId = @pBrokRead AND LanguageId = @fa;

UPDATE dbo.PermissionTranslation
SET Name = N'View Brokerages', Description = N'View brokerage information'
WHERE PermissionId = @pBrokRead AND LanguageId = @en;

UPDATE dbo.PermissionTranslation
SET Name = N'ویرایش کارگزاری‌ها', Description = N'افزودن، ویرایش و حذف اطلاعات کارگزاری‌ها'
WHERE PermissionId = @pBrokModify AND LanguageId = @fa;

UPDATE dbo.PermissionTranslation
SET Name = N'Modify Brokerages', Description = N'Add, edit, delete brokerage information'
WHERE PermissionId = @pBrokModify AND LanguageId = @en;

-- ── Step 3: Insert new Members.InvestmentFunds.Read|Modify
IF NOT EXISTS (SELECT 1 FROM dbo.Permission WHERE Code = 'Members.InvestmentFunds.Read')
    INSERT INTO dbo.Permission (Id, Code, ModuleId, ResourceId)
    VALUES (@pInvFundsRead, 'Members.InvestmentFunds.Read', @modMembers, @rInvestmentFunds);
ELSE SET @pInvFundsRead = (SELECT Id FROM dbo.Permission WHERE Code = 'Members.InvestmentFunds.Read');

IF NOT EXISTS (SELECT 1 FROM dbo.Permission WHERE Code = 'Members.InvestmentFunds.Modify')
    INSERT INTO dbo.Permission (Id, Code, ModuleId, ResourceId)
    VALUES (@pInvFundsModify, 'Members.InvestmentFunds.Modify', @modMembers, @rInvestmentFunds);
ELSE SET @pInvFundsModify = (SELECT Id FROM dbo.Permission WHERE Code = 'Members.InvestmentFunds.Modify');

-- ── Step 4: Translations for the two new perms
IF NOT EXISTS (SELECT 1 FROM dbo.PermissionTranslation WHERE PermissionId = @pInvFundsRead AND LanguageId = @fa)
    INSERT INTO dbo.PermissionTranslation (PermissionId, LanguageId, Name, Description)
    VALUES (@pInvFundsRead, @fa, N'مشاهده صندوق‌های سرمایه‌گذاری', N'مشاهده اطلاعات صندوق‌ها');
IF NOT EXISTS (SELECT 1 FROM dbo.PermissionTranslation WHERE PermissionId = @pInvFundsRead AND LanguageId = @en)
    INSERT INTO dbo.PermissionTranslation (PermissionId, LanguageId, Name, Description)
    VALUES (@pInvFundsRead, @en, N'View Investment Funds', N'View investment fund information');

IF NOT EXISTS (SELECT 1 FROM dbo.PermissionTranslation WHERE PermissionId = @pInvFundsModify AND LanguageId = @fa)
    INSERT INTO dbo.PermissionTranslation (PermissionId, LanguageId, Name, Description)
    VALUES (@pInvFundsModify, @fa, N'ویرایش صندوق‌های سرمایه‌گذاری', N'افزودن، ویرایش و حذف اطلاعات صندوق‌ها');
IF NOT EXISTS (SELECT 1 FROM dbo.PermissionTranslation WHERE PermissionId = @pInvFundsModify AND LanguageId = @en)
    INSERT INTO dbo.PermissionTranslation (PermissionId, LanguageId, Name, Description)
    VALUES (@pInvFundsModify, @en, N'Modify Investment Funds', N'Add, edit, delete investment fund information');

-- ── Step 5: Grant InvestmentFunds perms to every role that holds the (now renamed)
-- Brokerage perms. SuperAdmin + MembersAdmin should have both Read+Modify;
-- MembersViewer should have Read only.

-- For each role: if it holds Members.Brokerage.Read, also grant Members.InvestmentFunds.Read.
INSERT INTO dbo.RolePermission (RoleId, PermissionId, CreatedBy, CreatedAt)
SELECT rp.RoleId, @pInvFundsRead, @system, @now
FROM dbo.RolePermission rp
WHERE rp.PermissionId = @pBrokRead
  AND NOT EXISTS (
      SELECT 1 FROM dbo.RolePermission rp2
      WHERE rp2.RoleId = rp.RoleId AND rp2.PermissionId = @pInvFundsRead
  );

-- For each role: if it holds Members.Brokerage.Modify, also grant Members.InvestmentFunds.Modify.
INSERT INTO dbo.RolePermission (RoleId, PermissionId, CreatedBy, CreatedAt)
SELECT rp.RoleId, @pInvFundsModify, @system, @now
FROM dbo.RolePermission rp
WHERE rp.PermissionId = @pBrokModify
  AND NOT EXISTS (
      SELECT 1 FROM dbo.RolePermission rp2
      WHERE rp2.RoleId = rp.RoleId AND rp2.PermissionId = @pInvFundsModify
  );

COMMIT TRANSACTION;

-- Report
SELECT p.Code,
       MAX(CASE WHEN pt.LanguageId = @fa THEN pt.Name END) AS Persian,
       MAX(CASE WHEN pt.LanguageId = @en THEN pt.Name END) AS English
FROM dbo.Permission p
LEFT JOIN dbo.PermissionTranslation pt ON pt.PermissionId = p.Id
WHERE p.Code LIKE 'Members.%'
GROUP BY p.Code
ORDER BY p.Code;

SELECT r.Code AS Role, p.Code AS Permission
FROM dbo.RolePermission rp
JOIN dbo.[Role] r ON r.Id = rp.RoleId
JOIN dbo.Permission p ON p.Id = rp.PermissionId
WHERE p.Code LIKE 'Members.%'
ORDER BY r.Code, p.Code;

PRINT '015_split_members_admin_into_brokerage_invfunds.sql applied successfully.';
