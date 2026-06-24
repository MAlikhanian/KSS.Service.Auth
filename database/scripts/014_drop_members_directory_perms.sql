-- 014_drop_members_directory_perms.sql
--
-- Drops Members.Directory.Read and Members.Directory.Modify. The Members
-- module collapsed to a single Admin section — all 19 menu items now use
-- Members.Admin.* (menu.config.tsx already updated).
--
-- RolePermission and PermissionTranslation rows cascade-delete via the FKs
-- created in migration 007.
--
-- Companion to Common migration 016 (drops the Members.Directory Resource).
--
-- Apply to KSS_Auth_Prod and KSS_Auth_Dev.

SET QUOTED_IDENTIFIER ON;
SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;

DELETE FROM dbo.Permission
WHERE Code IN ('Members.Directory.Read', 'Members.Directory.Modify');

COMMIT TRANSACTION;

SELECT Code FROM dbo.Permission WHERE Code LIKE 'Members.%' ORDER BY Code;

PRINT '014_drop_members_directory_perms.sql applied successfully.';
