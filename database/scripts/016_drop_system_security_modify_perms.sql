-- 016_drop_system_security_modify_perms.sql
--
-- Removes the 3 Modify permissions for the System.Security section:
--   • System.Security.Role.Modify
--   • System.Security.Permission.Modify
--   • System.Security.RolePermission.Modify
--
-- Roles, Permissions, RolePermissions are DBA-managed reference data.
-- The UI is read-only; the only legitimate way to modify these tables
-- is via versioned migrations executed by a DBA.
--
-- Cascades remove the related PermissionTranslation + RolePermission rows.
--
-- Apply to KSS_Auth_Prod and KSS_Auth_Dev.

SET QUOTED_IDENTIFIER ON;
SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;

DELETE FROM dbo.Permission
WHERE Code IN (
    'System.Security.Role.Modify',
    'System.Security.Permission.Modify',
    'System.Security.RolePermission.Modify'
);

COMMIT TRANSACTION;

SELECT Code FROM dbo.Permission WHERE Code LIKE 'System.Security.%' ORDER BY Code;

PRINT '016_drop_system_security_modify_perms.sql applied successfully.';
