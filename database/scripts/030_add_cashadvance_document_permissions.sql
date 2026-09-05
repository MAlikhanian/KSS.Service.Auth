-- 030_add_cashadvance_document_permissions.sql
--
-- FIX: Cash Advance file upload was broken for every role except SuperAdmin,
-- CashAdvanceAdmin and CashAdvanceRequester.
--
-- Cause: DocumentController (the generic file-metadata table used by BOTH upload
-- locations — invoice/factor document AND payment receipt) was decorated
-- [PermissionGroup("Request")], so creating a Document row demanded
-- `CashAdvance.Request.Modify` — a charge-request-AUTHORING permission.
-- The Financial Expert (who holds CashAdvance.Payment.Manage and whose job is to
-- record payments) and the Financial Manager (CashAdvance.Invoice.Modify) never
-- held it, so their uploads were rejected with HTTP 403 even though the UI
-- correctly offered them the upload button.
--
-- Fix: give documents their OWN permission pair and re-gate the controller with
-- [PermissionGroup("Document")] (see KSS.Service.CashAdvance/KSS.Api/Controller/DocumentController.cs).
--
--   CashAdvance.Document.Read   -> granted to EVERY cash-advance role, so viewing and
--                                  downloading documents keeps working exactly as it did
--                                  when reads rode on CashAdvance.Request.Read.
--   CashAdvance.Document.Modify -> granted only to the roles that legitimately upload:
--                                  Admin, Requester, FinancialExpert, FinancialManager.
--                                  Ceo and Viewer stay read-only by design.
--
-- NOTE: this migration and the DocumentController change must ship together —
-- applying only one of them leaves the upload broken.
--
-- Layout follows 026/029: idempotent inserts, fixed Guids identical across
-- Dev + Prod, soft-delete aware grants.
--
-- Apply with `sqlcmd -f 65001 -C` to KSS_Auth_Dev, then KSS_Auth_Prod.

SET QUOTED_IDENTIFIER ON;
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @system UNIQUEIDENTIFIER = '00000000-0000-0000-0000-000000000001';
DECLARE @now    DATETIME2        = SYSUTCDATETIME();
DECLARE @fa SMALLINT = 12;
DECLARE @en SMALLINT = 10;

-- Existing CashAdvance module + resource Ids (same as migration 025)
DECLARE @modCashAdvance UNIQUEIDENTIFIER = '019F1000-0000-7001-8000-000000000006';
DECLARE @resCashAdvance UNIQUEIDENTIFIER = '019F1000-0000-7003-8000-000000000060';

BEGIN TRANSACTION;

-- ─────────────────────────────────────────────────────────────────────────
-- Permissions (no new roles — the existing cash-advance roles are reused)
-- ─────────────────────────────────────────────────────────────────────────
DECLARE @perms TABLE (
    Code VARCHAR(100), Id UNIQUEIDENTIFIER,
    EnName NVARCHAR(200), EnDesc NVARCHAR(400),
    FaName NVARCHAR(200), FaDesc NVARCHAR(400)
);
INSERT INTO @perms (Code, Id, EnName, EnDesc, FaName, FaDesc) VALUES
 ('CashAdvance.Document.Read',   '019F1100-0000-7102-8000-000000000022', N'View Cash Advance Documents',   N'View and download cash advance documents (invoice documents and payment receipts)', N'مشاهده اسناد تنخواه', N'مشاهده و دریافت اسناد تنخواه (فاکتور و رسید پرداخت)'),
 ('CashAdvance.Document.Modify', '019F1100-0000-7102-8000-000000000023', N'Manage Cash Advance Documents', N'Upload, replace and delete cash advance documents (invoice documents and payment receipts)', N'مدیریت اسناد تنخواه', N'بارگذاری، جایگزینی و حذف اسناد تنخواه (فاکتور و رسید پرداخت)');

INSERT INTO dbo.Permission (Id, Code, ModuleId, ResourceId)
SELECT x.Id, x.Code, @modCashAdvance, @resCashAdvance FROM @perms x
WHERE NOT EXISTS (SELECT 1 FROM dbo.Permission p WHERE p.Code = x.Code);

INSERT INTO dbo.PermissionTranslation (PermissionId, LanguageId, Name, Description)
SELECT p.Id, @en, x.EnName, x.EnDesc FROM @perms x JOIN dbo.Permission p ON p.Code = x.Code
WHERE NOT EXISTS (SELECT 1 FROM dbo.PermissionTranslation t WHERE t.PermissionId = p.Id AND t.LanguageId = @en);

INSERT INTO dbo.PermissionTranslation (PermissionId, LanguageId, Name, Description)
SELECT p.Id, @fa, x.FaName, x.FaDesc FROM @perms x JOIN dbo.Permission p ON p.Code = x.Code
WHERE NOT EXISTS (SELECT 1 FROM dbo.PermissionTranslation t WHERE t.PermissionId = p.Id AND t.LanguageId = @fa);

-- ─────────────────────────────────────────────────────────────────────────
-- Grants (role → permission), soft-delete aware
-- ─────────────────────────────────────────────────────────────────────────
DECLARE @grants TABLE (RoleCode VARCHAR(50), PermCode VARCHAR(100));
INSERT INTO @grants (RoleCode, PermCode) VALUES
 -- Read: every cash-advance role (preserves the viewing/downloading they had via Request.Read)
 ('CashAdvanceAdmin',            'CashAdvance.Document.Read'),
 ('CashAdvanceRequester',        'CashAdvance.Document.Read'),
 ('CashAdvanceFinancialExpert',  'CashAdvance.Document.Read'),
 ('CashAdvanceFinancialManager', 'CashAdvance.Document.Read'),
 ('CashAdvanceCeo',              'CashAdvance.Document.Read'),
 ('CashAdvanceViewer',           'CashAdvance.Document.Read'),
 ('SuperAdmin',                  'CashAdvance.Document.Read'),
 -- Modify: only the roles that legitimately upload files
 ('CashAdvanceAdmin',            'CashAdvance.Document.Modify'),
 ('CashAdvanceRequester',        'CashAdvance.Document.Modify'),  -- uploads invoice/factor documents
 ('CashAdvanceFinancialExpert',  'CashAdvance.Document.Modify'),  -- uploads payment receipts (holds Payment.Manage)
 ('CashAdvanceFinancialManager', 'CashAdvance.Document.Modify'),  -- attaches corrected invoice documents
 ('SuperAdmin',                  'CashAdvance.Document.Modify');

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
WHERE p.Code LIKE 'CashAdvance.Document.%' AND rp.DeletedAt IS NULL
ORDER BY p.Code, r.Code;

PRINT N'Migration 030 applied: CashAdvance.Document.Read/Modify permissions + grants';
