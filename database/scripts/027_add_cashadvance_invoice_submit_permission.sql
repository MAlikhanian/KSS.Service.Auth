-- 027_add_cashadvance_invoice_submit_permission.sql
--
-- Adds ONE new Cash Advance permission — CashAdvance.Invoice.Submit — used by the
-- requester (in-charge) to submit an invoice for approval (stage 1 of the two-stage
-- invoice workflow). Granted to CashAdvanceRequester (the requester) plus the two
-- "everything" roles CashAdvanceAdmin and SuperAdmin, matching migration 025's grant
-- pattern. Enforced downstream by the CashAdvance service via
-- [HasPermission("CashAdvance.Invoice.Submit")]; Auth only mints the claim.
--
-- Idempotent (NOT EXISTS guards), fixed Guids identical across Dev + Prod, soft-delete
-- aware grants. Permission id 019F1100-0000-7102-8000-000000000017 is the next free slot
-- after migration 026 (Project) consumed ...013–...016.
--
-- Apply with `sqlcmd -f 65001 -C` to KSS_Auth_Dev (then KSS_Auth_Prod when approved).

SET QUOTED_IDENTIFIER ON;
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @system UNIQUEIDENTIFIER = '00000000-0000-0000-0000-000000000001';
DECLARE @now    DATETIME2        = SYSUTCDATETIME();
DECLARE @fa SMALLINT = 12;
DECLARE @en SMALLINT = 10;

DECLARE @modCashAdvance UNIQUEIDENTIFIER = '019F1000-0000-7001-8000-000000000006';
DECLARE @resCashAdvance UNIQUEIDENTIFIER = '019F1000-0000-7003-8000-000000000060';

DECLARE @permId UNIQUEIDENTIFIER = '019F1100-0000-7102-8000-000000000017';
DECLARE @permCode VARCHAR(100) = 'CashAdvance.Invoice.Submit';

BEGIN TRANSACTION;

-- Permission catalog row
INSERT INTO dbo.Permission (Id, Code, ModuleId, ResourceId)
SELECT @permId, @permCode, @modCashAdvance, @resCashAdvance
WHERE NOT EXISTS (SELECT 1 FROM dbo.Permission p WHERE p.Code = @permCode);

-- English translation
INSERT INTO dbo.PermissionTranslation (PermissionId, LanguageId, Name, Description)
SELECT p.Id, @en, N'Submit Cash Advance Invoice', N'Submit an invoice for approval'
FROM dbo.Permission p
WHERE p.Code = @permCode
  AND NOT EXISTS (SELECT 1 FROM dbo.PermissionTranslation t WHERE t.PermissionId = p.Id AND t.LanguageId = @en);

-- Persian translation
INSERT INTO dbo.PermissionTranslation (PermissionId, LanguageId, Name, Description)
SELECT p.Id, @fa, N'ارسال فاکتور تنخواه', N'ارسال فاکتور برای تأیید'
FROM dbo.Permission p
WHERE p.Code = @permCode
  AND NOT EXISTS (SELECT 1 FROM dbo.PermissionTranslation t WHERE t.PermissionId = p.Id AND t.LanguageId = @fa);

-- Grants (role → permission), soft-delete aware
DECLARE @grants TABLE (RoleCode VARCHAR(50), PermCode VARCHAR(100));
INSERT INTO @grants (RoleCode, PermCode) VALUES
 ('CashAdvanceRequester', 'CashAdvance.Invoice.Submit'),
 ('CashAdvanceAdmin',     'CashAdvance.Invoice.Submit'),
 ('SuperAdmin',           'CashAdvance.Invoice.Submit');

INSERT INTO dbo.RolePermission (RoleId, PermissionId, CreatedBy, CreatedAt)
SELECT r.Id, p.Id, @system, @now
FROM @grants g
JOIN dbo.[Role] r ON r.Code = g.RoleCode
JOIN dbo.Permission p ON p.Code = g.PermCode
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.RolePermission rp
    WHERE rp.RoleId = r.Id AND rp.PermissionId = p.Id AND rp.DeletedAt IS NULL
);

COMMIT TRANSACTION;

-- Report
SELECT r.Code AS Role, p.Code AS Permission
FROM dbo.RolePermission rp
JOIN dbo.[Role] r ON r.Id = rp.RoleId
JOIN dbo.Permission p ON p.Id = rp.PermissionId
WHERE p.Code = 'CashAdvance.Invoice.Submit' AND rp.DeletedAt IS NULL
ORDER BY r.Code;

PRINT N'Migration 027 applied: CashAdvance.Invoice.Submit permission + 3 grants';
