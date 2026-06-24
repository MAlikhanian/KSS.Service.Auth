-- 010_fix_company_access_translations_utf.sql
--
-- Migration 009 inserted the Persian translations for the new
-- Company.Access.Read/Manage permissions via sqlcmd WITHOUT -f 65001.
-- sqlcmd defaulted to Windows-1252 and stored the UTF-8 bytes as
-- garbled NVARCHAR (e.g. "Ù…Ø¯ÛŒØ±ÛŒØª" instead of "مدیریت").
--
-- This script overwrites those rows with the proper Persian text.
-- Apply with `sqlcmd -f 65001` so the encoding is correct this time.
--
-- Apply to KSS_Auth_Prod and KSS_Auth_Dev.

SET QUOTED_IDENTIFIER ON;
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @fa SMALLINT = 12;

BEGIN TRANSACTION;

UPDATE pt
SET pt.Name = N'مشاهده دسترسی شرکت',
    pt.Description = N'مشاهده دسترسی‌های اعطا شده روی یک شرکت'
FROM dbo.PermissionTranslation pt
JOIN dbo.Permission p ON p.Id = pt.PermissionId
WHERE p.Code = 'Company.Access.Read' AND pt.LanguageId = @fa;

UPDATE pt
SET pt.Name = N'مدیریت دسترسی شرکت',
    pt.Description = N'اعطا و حذف دسترسی‌ها روی یک شرکت'
FROM dbo.PermissionTranslation pt
JOIN dbo.Permission p ON p.Id = pt.PermissionId
WHERE p.Code = 'Company.Access.Manage' AND pt.LanguageId = @fa;

COMMIT TRANSACTION;

SELECT p.Code, pt.LanguageId, pt.Name, pt.Description
FROM dbo.Permission p
JOIN dbo.PermissionTranslation pt ON pt.PermissionId = p.Id
WHERE p.Code LIKE 'Company.Access.%' AND pt.LanguageId = @fa
ORDER BY p.Code;

PRINT '010_fix_company_access_translations_utf.sql applied successfully.';
