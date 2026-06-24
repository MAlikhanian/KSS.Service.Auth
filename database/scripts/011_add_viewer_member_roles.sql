-- 011_add_viewer_member_roles.sql
--
-- Introduces "Viewer" and "Member" tiers per module on top of the existing
-- module-admin roles. Three tiers per module that has row-level access:
--   • {Module}Admin   (already exists) — Read + Manage, global RoleAccess Level=2
--   • {Module}Viewer  (new)           — Read only,    global RoleAccess Level=1
--   • {Module}Member  (new)           — Read only,    NO global RoleAccess
--                                       (sees only rows granted via Access table)
--
-- Modules with row-level Access (Person, Company) get all three tiers.
-- Modules without row-level Access (Members, CreditRating) get only Viewer
-- (Member doesn't make sense without per-row visibility).
--
-- This script handles the Auth side: Role + RoleTranslation + RolePermission.
-- The matching RoleAccess seeds for Viewers live in the module DBs:
--   KSS.Service.Person   migration 013
--   KSS.Service.Company  migration 002
--
-- Apply with `sqlcmd -f 65001` to KSS_Auth_Prod and KSS_Auth_Dev.

SET QUOTED_IDENTIFIER ON;
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @system UNIQUEIDENTIFIER = '00000000-0000-0000-0000-000000000001';
DECLARE @now DATETIME2 = SYSUTCDATETIME();
DECLARE @fa SMALLINT = 12;
DECLARE @en SMALLINT = 10;

-- Module Ids (shared across Dev + Prod, same Guids in Common DB)
DECLARE @modPerson      UNIQUEIDENTIFIER = '019f1000-0000-7001-8000-000000000001';
DECLARE @modCompany     UNIQUEIDENTIFIER = '019f1000-0000-7001-8000-000000000002';
DECLARE @modMembers     UNIQUEIDENTIFIER = '019f1000-0000-7001-8000-000000000003';
DECLARE @modCreditRate  UNIQUEIDENTIFIER = '019f1000-0000-7001-8000-000000000004';

-- New role Guids (fixed so they're identical across Dev + Prod for cross-DB joins)
DECLARE @rPersonViewer       UNIQUEIDENTIFIER = '019f1100-0000-7100-8000-000000000001';
DECLARE @rPersonMember       UNIQUEIDENTIFIER = '019f1100-0000-7100-8000-000000000002';
DECLARE @rCompanyViewer      UNIQUEIDENTIFIER = '019f1100-0000-7100-8000-000000000003';
DECLARE @rCompanyMember      UNIQUEIDENTIFIER = '019f1100-0000-7100-8000-000000000004';
DECLARE @rMembersViewer      UNIQUEIDENTIFIER = '019f1100-0000-7100-8000-000000000005';
DECLARE @rCreditRatingViewer UNIQUEIDENTIFIER = '019f1100-0000-7100-8000-000000000006';

BEGIN TRANSACTION;

-- ── Step 1: Insert Role rows (idempotent)
IF NOT EXISTS (SELECT 1 FROM dbo.[Role] WHERE Code = 'PersonViewer')
    INSERT INTO dbo.[Role] (Id, Code, ModuleId) VALUES (@rPersonViewer, 'PersonViewer', @modPerson);
IF NOT EXISTS (SELECT 1 FROM dbo.[Role] WHERE Code = 'PersonMember')
    INSERT INTO dbo.[Role] (Id, Code, ModuleId) VALUES (@rPersonMember, 'PersonMember', @modPerson);
IF NOT EXISTS (SELECT 1 FROM dbo.[Role] WHERE Code = 'CompanyViewer')
    INSERT INTO dbo.[Role] (Id, Code, ModuleId) VALUES (@rCompanyViewer, 'CompanyViewer', @modCompany);
IF NOT EXISTS (SELECT 1 FROM dbo.[Role] WHERE Code = 'CompanyMember')
    INSERT INTO dbo.[Role] (Id, Code, ModuleId) VALUES (@rCompanyMember, 'CompanyMember', @modCompany);
IF NOT EXISTS (SELECT 1 FROM dbo.[Role] WHERE Code = 'MembersViewer')
    INSERT INTO dbo.[Role] (Id, Code, ModuleId) VALUES (@rMembersViewer, 'MembersViewer', @modMembers);
IF NOT EXISTS (SELECT 1 FROM dbo.[Role] WHERE Code = 'CreditRatingViewer')
    INSERT INTO dbo.[Role] (Id, Code, ModuleId) VALUES (@rCreditRatingViewer, 'CreditRatingViewer', @modCreditRate);

-- Resolve Ids (in case rows pre-existed with different Guids — keeps idempotency)
SET @rPersonViewer       = (SELECT Id FROM dbo.[Role] WHERE Code = 'PersonViewer');
SET @rPersonMember       = (SELECT Id FROM dbo.[Role] WHERE Code = 'PersonMember');
SET @rCompanyViewer      = (SELECT Id FROM dbo.[Role] WHERE Code = 'CompanyViewer');
SET @rCompanyMember      = (SELECT Id FROM dbo.[Role] WHERE Code = 'CompanyMember');
SET @rMembersViewer      = (SELECT Id FROM dbo.[Role] WHERE Code = 'MembersViewer');
SET @rCreditRatingViewer = (SELECT Id FROM dbo.[Role] WHERE Code = 'CreditRatingViewer');

-- ── Step 2: Translations (fa + en for each role) — one INSERT per row, idempotent

-- PersonViewer
IF NOT EXISTS (SELECT 1 FROM dbo.RoleTranslation WHERE RoleId = @rPersonViewer AND LanguageId = @fa)
    INSERT INTO dbo.RoleTranslation (RoleId, LanguageId, Name, Description) VALUES (@rPersonViewer, @fa, N'بازبین اشخاص', N'مشاهده فقط‌خواندنی همه اشخاص');
IF NOT EXISTS (SELECT 1 FROM dbo.RoleTranslation WHERE RoleId = @rPersonViewer AND LanguageId = @en)
    INSERT INTO dbo.RoleTranslation (RoleId, LanguageId, Name, Description) VALUES (@rPersonViewer, @en, N'Person Viewer', N'Read-only view of all persons');

-- PersonMember
IF NOT EXISTS (SELECT 1 FROM dbo.RoleTranslation WHERE RoleId = @rPersonMember AND LanguageId = @fa)
    INSERT INTO dbo.RoleTranslation (RoleId, LanguageId, Name, Description) VALUES (@rPersonMember, @fa, N'عضو اشخاص', N'مشاهده اشخاصی که به آن‌ها دسترسی اعطا شده');
IF NOT EXISTS (SELECT 1 FROM dbo.RoleTranslation WHERE RoleId = @rPersonMember AND LanguageId = @en)
    INSERT INTO dbo.RoleTranslation (RoleId, LanguageId, Name, Description) VALUES (@rPersonMember, @en, N'Person Member', N'View only persons granted via Access');

-- CompanyViewer
IF NOT EXISTS (SELECT 1 FROM dbo.RoleTranslation WHERE RoleId = @rCompanyViewer AND LanguageId = @fa)
    INSERT INTO dbo.RoleTranslation (RoleId, LanguageId, Name, Description) VALUES (@rCompanyViewer, @fa, N'بازبین شرکت‌ها', N'مشاهده فقط‌خواندنی همه شرکت‌ها');
IF NOT EXISTS (SELECT 1 FROM dbo.RoleTranslation WHERE RoleId = @rCompanyViewer AND LanguageId = @en)
    INSERT INTO dbo.RoleTranslation (RoleId, LanguageId, Name, Description) VALUES (@rCompanyViewer, @en, N'Company Viewer', N'Read-only view of all companies');

-- CompanyMember
IF NOT EXISTS (SELECT 1 FROM dbo.RoleTranslation WHERE RoleId = @rCompanyMember AND LanguageId = @fa)
    INSERT INTO dbo.RoleTranslation (RoleId, LanguageId, Name, Description) VALUES (@rCompanyMember, @fa, N'عضو شرکت‌ها', N'مشاهده شرکت‌هایی که به آن‌ها دسترسی اعطا شده');
IF NOT EXISTS (SELECT 1 FROM dbo.RoleTranslation WHERE RoleId = @rCompanyMember AND LanguageId = @en)
    INSERT INTO dbo.RoleTranslation (RoleId, LanguageId, Name, Description) VALUES (@rCompanyMember, @en, N'Company Member', N'View only companies granted via Access');

-- MembersViewer
IF NOT EXISTS (SELECT 1 FROM dbo.RoleTranslation WHERE RoleId = @rMembersViewer AND LanguageId = @fa)
    INSERT INTO dbo.RoleTranslation (RoleId, LanguageId, Name, Description) VALUES (@rMembersViewer, @fa, N'بازبین اعضا', N'مشاهده فقط‌خواندنی بخش اعضا');
IF NOT EXISTS (SELECT 1 FROM dbo.RoleTranslation WHERE RoleId = @rMembersViewer AND LanguageId = @en)
    INSERT INTO dbo.RoleTranslation (RoleId, LanguageId, Name, Description) VALUES (@rMembersViewer, @en, N'Members Viewer', N'Read-only view of members section');

-- CreditRatingViewer
IF NOT EXISTS (SELECT 1 FROM dbo.RoleTranslation WHERE RoleId = @rCreditRatingViewer AND LanguageId = @fa)
    INSERT INTO dbo.RoleTranslation (RoleId, LanguageId, Name, Description) VALUES (@rCreditRatingViewer, @fa, N'بازبین رتبه‌بندی اعتباری', N'مشاهده فقط‌خواندنی بخش رتبه‌بندی اعتباری');
IF NOT EXISTS (SELECT 1 FROM dbo.RoleTranslation WHERE RoleId = @rCreditRatingViewer AND LanguageId = @en)
    INSERT INTO dbo.RoleTranslation (RoleId, LanguageId, Name, Description) VALUES (@rCreditRatingViewer, @en, N'CreditRating Viewer', N'Read-only view of credit rating section');

-- ── Step 3: Grant Read-only permissions to each role (idempotent)
INSERT INTO dbo.RolePermission (RoleId, PermissionId, CreatedBy, CreatedAt)
SELECT v.RoleId, p.Id, @system, @now
FROM (VALUES
    -- PersonViewer: all Read perms across Person sections
    (@rPersonViewer, 'Person.Information.Read'),
    (@rPersonViewer, 'Person.Assets.Read'),
    (@rPersonViewer, 'Person.Access.Read'),
    (@rPersonViewer, 'Person.Security.Read'),
    (@rPersonViewer, 'Person.Admin.Read'),
    -- PersonMember: only Information.Read (menu+page entry); row-level Access controls visibility
    (@rPersonMember, 'Person.Information.Read'),
    -- CompanyViewer: all Read perms across Company sections
    (@rCompanyViewer, 'Company.Information.Read'),
    (@rCompanyViewer, 'Company.Access.Read'),
    -- CompanyMember: only Information.Read; row-level Access controls visibility
    (@rCompanyMember, 'Company.Information.Read'),
    -- MembersViewer: all Read perms in Members
    (@rMembersViewer, 'Members.Directory.Read'),
    (@rMembersViewer, 'Members.Admin.Read'),
    -- CreditRatingViewer: all Read perms in CreditRating
    (@rCreditRatingViewer, 'CreditRating.Assessment.Read'),
    (@rCreditRatingViewer, 'CreditRating.Admin.Read')
) AS v(RoleId, PermissionCode)
JOIN dbo.Permission p ON p.Code = v.PermissionCode
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.RolePermission rp
    WHERE rp.RoleId = v.RoleId AND rp.PermissionId = p.Id
);

COMMIT TRANSACTION;

-- Report
SELECT r.Code AS Role, COUNT(rp.PermissionId) AS Permissions
FROM dbo.[Role] r
LEFT JOIN dbo.RolePermission rp ON rp.RoleId = r.Id
WHERE r.Code IN ('PersonViewer','PersonMember','CompanyViewer','CompanyMember','MembersViewer','CreditRatingViewer')
GROUP BY r.Code
ORDER BY r.Code;

SELECT r.Code AS Role, rt.LanguageId, rt.Name
FROM dbo.[Role] r
JOIN dbo.RoleTranslation rt ON rt.RoleId = r.Id
WHERE r.Code IN ('PersonViewer','PersonMember','CompanyViewer','CompanyMember','MembersViewer','CreditRatingViewer')
ORDER BY r.Code, rt.LanguageId;

PRINT '011_add_viewer_member_roles.sql applied successfully.';
