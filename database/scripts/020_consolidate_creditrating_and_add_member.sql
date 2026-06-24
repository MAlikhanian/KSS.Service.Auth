-- 020_consolidate_creditrating_and_add_member.sql
--
-- Two changes in one migration:
--   1. Drop CreditRating.Admin.{Read,Modify} permissions (dead — the "Cases"
--      page they were gating now uses CreditRating.Assessment.*)
--   2. Add CreditRatingMember role + grant CreditRating.Assessment.Read
--
-- Cascades remove translations + role grants for the dropped perms.
--
-- Companion to Common migration 020 (drops the Admin Resource).
--
-- Apply with `sqlcmd -f 65001` to KSS_Auth_Prod and KSS_Auth_Dev.

SET QUOTED_IDENTIFIER ON;
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @system UNIQUEIDENTIFIER = '00000000-0000-0000-0000-000000000001';
DECLARE @now DATETIME2 = SYSUTCDATETIME();
DECLARE @fa SMALLINT = 12;
DECLARE @en SMALLINT = 10;

DECLARE @modCreditRating UNIQUEIDENTIFIER = '019f1000-0000-7001-8000-000000000004';
DECLARE @rCreditRatingMember UNIQUEIDENTIFIER = '019f1100-0000-7100-8000-000000000009';

BEGIN TRANSACTION;

-- ── Step 1: Drop dead CreditRating.Admin permissions
DELETE FROM dbo.Permission WHERE Code IN ('CreditRating.Admin.Read', 'CreditRating.Admin.Modify');

-- ── Step 2: Add CreditRatingMember role
IF NOT EXISTS (SELECT 1 FROM dbo.[Role] WHERE Code = 'CreditRatingMember')
    INSERT INTO dbo.[Role] (Id, Code, ModuleId) VALUES (@rCreditRatingMember, 'CreditRatingMember', @modCreditRating);
ELSE SET @rCreditRatingMember = (SELECT Id FROM dbo.[Role] WHERE Code = 'CreditRatingMember');

IF NOT EXISTS (SELECT 1 FROM dbo.RoleTranslation WHERE RoleId = @rCreditRatingMember AND LanguageId = @fa)
    INSERT INTO dbo.RoleTranslation (RoleId, LanguageId, Name, Description)
    VALUES (@rCreditRatingMember, @fa, N'عضو رتبه‌بندی اعتباری', N'مشاهده ارزیابی‌های اعتباری اعطا شده');
IF NOT EXISTS (SELECT 1 FROM dbo.RoleTranslation WHERE RoleId = @rCreditRatingMember AND LanguageId = @en)
    INSERT INTO dbo.RoleTranslation (RoleId, LanguageId, Name, Description)
    VALUES (@rCreditRatingMember, @en, N'CreditRating Member', N'View credit assessments granted via Access');

-- ── Step 3: Grant Assessment.Read to CreditRatingMember
DECLARE @pAssessmentRead UNIQUEIDENTIFIER = (SELECT Id FROM dbo.Permission WHERE Code = 'CreditRating.Assessment.Read');
IF @pAssessmentRead IS NULL THROW 51120, 'CreditRating.Assessment.Read perm not found', 1;

IF NOT EXISTS (
    SELECT 1 FROM dbo.RolePermission
    WHERE RoleId = @rCreditRatingMember AND PermissionId = @pAssessmentRead
)
    INSERT INTO dbo.RolePermission (RoleId, PermissionId, CreatedBy, CreatedAt)
    VALUES (@rCreditRatingMember, @pAssessmentRead, @system, @now);

COMMIT TRANSACTION;

-- Report
SELECT Code FROM dbo.Permission WHERE Code LIKE 'CreditRating.%' ORDER BY Code;
SELECT r.Code AS Role, p.Code AS Permission
FROM dbo.RolePermission rp
JOIN dbo.[Role] r ON r.Id = rp.RoleId
JOIN dbo.Permission p ON p.Id = rp.PermissionId
WHERE p.Code LIKE 'CreditRating.%'
ORDER BY r.Code, p.Code;

PRINT '020_consolidate_creditrating_and_add_member.sql applied successfully.';
