-- 019_drop_unused_admin_perms.sql
--
-- Removes 4 dead permissions that no menu item, no controller, and no
-- backend code references:
--   • Person.Admin.Read     / Person.Admin.Modify
--   • Company.Admin.Read    / Company.Admin.Modify
--
-- The original `/admin` page was moved to `/system/security/*` (gated by
-- System.Security.* perms), and the per-module admin pages those codes
-- were supposed to gate were never built. CreditRating.Admin.* stays —
-- it still gates /credit-rating/list. Members.Admin is already gone
-- (split into Brokerage + InvestmentFunds in migration 015).
--
-- Cascades remove PermissionTranslation + RolePermission rows.
--
-- Companion to Common migration 019 (drops the matching Resources).
--
-- Apply to KSS_Auth_Prod and KSS_Auth_Dev.

SET QUOTED_IDENTIFIER ON;
SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;

DELETE FROM dbo.Permission
WHERE Code IN (
    'Person.Admin.Read',
    'Person.Admin.Modify',
    'Company.Admin.Read',
    'Company.Admin.Modify'
);

COMMIT TRANSACTION;

SELECT Code FROM dbo.Permission
WHERE Code LIKE 'Person.%' OR Code LIKE 'Company.%'
ORDER BY Code;

PRINT '019_drop_unused_admin_perms.sql applied successfully.';
