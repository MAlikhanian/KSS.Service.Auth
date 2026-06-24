-- 012_rename_manage_to_modify.sql
--
-- Renames the action suffix `.Manage` → `.Modify` across the entire
-- permission catalog. Affects 12 permissions × Code + 24 translation rows
-- (12 fa + 12 en) × Name + Description.
--
-- Backend & frontend code is updated in parallel to emit/expect `.Modify`.
--
-- Apply with `sqlcmd -f 65001` so the Persian text round-trips correctly.
--
-- Apply to KSS_Auth_Prod and KSS_Auth_Dev.

SET QUOTED_IDENTIFIER ON;
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @fa SMALLINT = 12;
DECLARE @en SMALLINT = 10;

BEGIN TRANSACTION;

-- ── Step 1: Rename Permission.Code (12 rows)
UPDATE dbo.Permission
SET Code = REPLACE(Code, '.Manage', '.Modify')
WHERE Code LIKE '%.Manage';

-- ── Step 2: Update English translations (Name: "Manage X" → "Modify X")
UPDATE pt
SET pt.Name = REPLACE(pt.Name, 'Manage', 'Modify'),
    pt.Description = REPLACE(pt.Description, 'Manage', 'Modify')
FROM dbo.PermissionTranslation pt
JOIN dbo.Permission p ON p.Id = pt.PermissionId
WHERE pt.LanguageId = @en
  AND p.Code LIKE '%.Modify';   -- after step 1, the renamed ones end in .Modify

-- ── Step 3: Update Persian translations (Name/Description: "مدیریت" → "ویرایش")
UPDATE pt
SET pt.Name = REPLACE(pt.Name, N'مدیریت', N'ویرایش'),
    pt.Description = REPLACE(pt.Description, N'مدیریت', N'ویرایش')
FROM dbo.PermissionTranslation pt
JOIN dbo.Permission p ON p.Id = pt.PermissionId
WHERE pt.LanguageId = @fa
  AND p.Code LIKE '%.Modify';

COMMIT TRANSACTION;

SELECT p.Code,
       MAX(CASE WHEN pt.LanguageId = @fa THEN pt.Name END) AS Persian,
       MAX(CASE WHEN pt.LanguageId = @en THEN pt.Name END) AS English
FROM dbo.Permission p
LEFT JOIN dbo.PermissionTranslation pt ON pt.PermissionId = p.Id
WHERE p.Code LIKE '%.Modify'
GROUP BY p.Code
ORDER BY p.Code;

PRINT '012_rename_manage_to_modify.sql applied successfully.';
