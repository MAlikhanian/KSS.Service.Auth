-- 025_add_cashadvance_permissions_and_roles.sql
--
-- Cash Advance (تنخواه) module RBAC: 11 permissions (4 section CRUD pairs +
-- 3 workflow action perms) and 6 actor roles (Admin / Requester / CEO /
-- Financial Manager / Financial Expert / Viewer), with en + fa translations
-- and role→permission grants. SuperAdmin receives all 11.
--
-- Section CRUD perms are auto-enforced by the CashAdvance service's
-- [PermissionGroup] filter (CashAdvance.<Section>.Read / .Modify); the 3
-- Approval/Payment perms are enforced on the workflow endpoints via
-- [HasPermission("...")].
--
-- Layout follows 023/024: idempotent inserts, fixed Guids identical across
-- Dev + Prod, soft-delete aware grants. Module/Resource Ids are cross-DB
-- catalog metadata (no local FK) — a matching KSS_Common Module row is
-- optional and can be backfilled later.
--
-- Apply with `sqlcmd -f 65001 -C` to KSS_Auth_Dev (then KSS_Auth_Prod when approved).

SET QUOTED_IDENTIFIER ON;
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @system UNIQUEIDENTIFIER = '00000000-0000-0000-0000-000000000001';
DECLARE @now    DATETIME2        = SYSUTCDATETIME();
DECLARE @fa SMALLINT = 12;
DECLARE @en SMALLINT = 10;

-- New CashAdvance module + resource Ids (cross-DB catalog metadata; no local FK)
DECLARE @modCashAdvance UNIQUEIDENTIFIER = '019F1000-0000-7001-8000-000000000006';
DECLARE @resCashAdvance UNIQUEIDENTIFIER = '019F1000-0000-7003-8000-000000000060';

BEGIN TRANSACTION;

-- ─────────────────────────────────────────────────────────────────────────
-- Roles
-- ─────────────────────────────────────────────────────────────────────────
DECLARE @roles TABLE (
    Code VARCHAR(50), Id UNIQUEIDENTIFIER,
    EnName NVARCHAR(200), EnDesc NVARCHAR(400),
    FaName NVARCHAR(200), FaDesc NVARCHAR(400)
);
INSERT INTO @roles (Code, Id, EnName, EnDesc, FaName, FaDesc) VALUES
 ('CashAdvanceAdmin',            '019F1100-0000-7100-8000-000000000015', N'Cash Advance Admin',            N'Full control of the Cash Advance module',                         N'مدیر تنخواه',            N'مدیریت کامل ماژول تنخواه'),
 ('CashAdvanceRequester',        '019F1100-0000-7100-8000-000000000016', N'Cash Advance Requester',        N'In-charge person who creates charge requests, items and invoices', N'درخواست‌کننده تنخواه',   N'مسئول تنخواه که درخواست‌ها، اقلام و فاکتورها را ثبت می‌کند'),
 ('CashAdvanceCeo',              '019F1100-0000-7100-8000-000000000017', N'Cash Advance CEO',              N'Approves or rejects charge requests and their items',             N'مدیرعامل (تنخواه)',      N'تأیید یا رد درخواست‌ها و اقلام تنخواه'),
 ('CashAdvanceFinancialManager', '019F1100-0000-7100-8000-000000000018', N'Cash Advance Financial Manager',N'Approves the request after CEO and reviews invoice documents',     N'مدیر مالی تنخواه',       N'تأیید درخواست پس از مدیرعامل و بررسی اسناد فاکتور'),
 ('CashAdvanceFinancialExpert',  '019F1100-0000-7100-8000-000000000019', N'Cash Advance Financial Expert', N'Records the payment document and posts the ledger',                N'کارشناس مالی تنخواه',    N'ثبت سند پرداخت و ثبت گردش مالی'),
 ('CashAdvanceViewer',           '019F1100-0000-7100-8000-000000000020', N'Cash Advance Viewer',           N'Read-only access to the Cash Advance module',                      N'بیننده تنخواه',          N'دسترسی فقط‌خواندنی به ماژول تنخواه');

INSERT INTO dbo.[Role] (Id, Code, ModuleId)
SELECT x.Id, x.Code, @modCashAdvance FROM @roles x
WHERE NOT EXISTS (SELECT 1 FROM dbo.[Role] r WHERE r.Code = x.Code);

INSERT INTO dbo.RoleTranslation (RoleId, LanguageId, Name, Description)
SELECT r.Id, @en, x.EnName, x.EnDesc FROM @roles x JOIN dbo.[Role] r ON r.Code = x.Code
WHERE NOT EXISTS (SELECT 1 FROM dbo.RoleTranslation t WHERE t.RoleId = r.Id AND t.LanguageId = @en);

INSERT INTO dbo.RoleTranslation (RoleId, LanguageId, Name, Description)
SELECT r.Id, @fa, x.FaName, x.FaDesc FROM @roles x JOIN dbo.[Role] r ON r.Code = x.Code
WHERE NOT EXISTS (SELECT 1 FROM dbo.RoleTranslation t WHERE t.RoleId = r.Id AND t.LanguageId = @fa);

-- ─────────────────────────────────────────────────────────────────────────
-- Permissions
-- ─────────────────────────────────────────────────────────────────────────
DECLARE @perms TABLE (
    Code VARCHAR(100), Id UNIQUEIDENTIFIER,
    EnName NVARCHAR(200), EnDesc NVARCHAR(400),
    FaName NVARCHAR(200), FaDesc NVARCHAR(400)
);
INSERT INTO @perms (Code, Id, EnName, EnDesc, FaName, FaDesc) VALUES
 ('CashAdvance.Request.Read',                '019F1100-0000-7102-8000-000000000002', N'View Cash Advance Requests',   N'View charge requests and their items',           N'مشاهده درخواست تنخواه',  N'مشاهده درخواست‌ها و اقلام تنخواه'),
 ('CashAdvance.Request.Modify',              '019F1100-0000-7102-8000-000000000003', N'Manage Cash Advance Requests', N'Create, edit and delete charge requests and items',N'مدیریت درخواست تنخواه', N'ثبت، ویرایش و حذف درخواست‌ها و اقلام تنخواه'),
 ('CashAdvance.Invoice.Read',                '019F1100-0000-7102-8000-000000000004', N'View Cash Advance Invoices',   N'View invoices and invoice documents',            N'مشاهده فاکتور تنخواه',   N'مشاهده فاکتورها و اسناد تنخواه'),
 ('CashAdvance.Invoice.Modify',              '019F1100-0000-7102-8000-000000000005', N'Manage Cash Advance Invoices', N'Create, edit and delete invoices and documents', N'مدیریت فاکتور تنخواه',   N'ثبت، ویرایش و حذف فاکتورها و اسناد تنخواه'),
 ('CashAdvance.Ledger.Read',                 '019F1100-0000-7102-8000-000000000006', N'View Cash Advance Ledger',     N'View cash advance financial transactions',       N'مشاهده گردش مالی تنخواه',N'مشاهده تراکنش‌های مالی تنخواه'),
 ('CashAdvance.Ledger.Modify',               '019F1100-0000-7102-8000-000000000007', N'Manage Cash Advance Ledger',   N'Post cash advance financial transactions',       N'مدیریت گردش مالی تنخواه',N'ثبت تراکنش‌های مالی تنخواه'),
 ('CashAdvance.Admin.Read',                  '019F1100-0000-7102-8000-000000000008', N'View Cash Advance Settings',   N'View master data (funds, products, limits)',     N'مشاهده تنظیمات تنخواه',  N'مشاهده داده‌های پایه تنخواه'),
 ('CashAdvance.Admin.Modify',                '019F1100-0000-7102-8000-000000000009', N'Manage Cash Advance Settings', N'Manage master data (funds, products, limits)',   N'مدیریت تنظیمات تنخواه',  N'مدیریت داده‌های پایه تنخواه (تنخواه‌ها، کالاها، سقف‌ها)'),
 ('CashAdvance.Approval.Ceo',                '019F1100-0000-7102-8000-000000000010', N'CEO Approval',                N'Approve or reject charge requests as CEO',       N'تأیید مدیرعامل',         N'تأیید یا رد درخواست تنخواه توسط مدیرعامل'),
 ('CashAdvance.Approval.FinancialManager',   '019F1100-0000-7102-8000-000000000011', N'Financial Manager Approval',  N'Approve or reject requests and invoice documents',N'تأیید مدیر مالی',       N'تأیید یا رد درخواست و اسناد فاکتور توسط مدیر مالی'),
 ('CashAdvance.Payment.Manage',              '019F1100-0000-7102-8000-000000000012', N'Manage Cash Advance Payment',  N'Upload and record the payment document',         N'ثبت پرداخت تنخواه',      N'بارگذاری و ثبت سند پرداخت تنخواه');

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
 -- Admin: everything
 ('CashAdvanceAdmin','CashAdvance.Request.Read'),('CashAdvanceAdmin','CashAdvance.Request.Modify'),
 ('CashAdvanceAdmin','CashAdvance.Invoice.Read'),('CashAdvanceAdmin','CashAdvance.Invoice.Modify'),
 ('CashAdvanceAdmin','CashAdvance.Ledger.Read'),('CashAdvanceAdmin','CashAdvance.Ledger.Modify'),
 ('CashAdvanceAdmin','CashAdvance.Admin.Read'),('CashAdvanceAdmin','CashAdvance.Admin.Modify'),
 ('CashAdvanceAdmin','CashAdvance.Approval.Ceo'),('CashAdvanceAdmin','CashAdvance.Approval.FinancialManager'),
 ('CashAdvanceAdmin','CashAdvance.Payment.Manage'),
 -- Requester (in-charge): create requests/items/invoices, read funds/ledger
 ('CashAdvanceRequester','CashAdvance.Request.Read'),('CashAdvanceRequester','CashAdvance.Request.Modify'),
 ('CashAdvanceRequester','CashAdvance.Invoice.Read'),('CashAdvanceRequester','CashAdvance.Invoice.Modify'),
 ('CashAdvanceRequester','CashAdvance.Ledger.Read'),('CashAdvanceRequester','CashAdvance.Admin.Read'),
 -- CEO: approve requests/items
 ('CashAdvanceCeo','CashAdvance.Request.Read'),('CashAdvanceCeo','CashAdvance.Approval.Ceo'),
 ('CashAdvanceCeo','CashAdvance.Invoice.Read'),('CashAdvanceCeo','CashAdvance.Ledger.Read'),
 ('CashAdvanceCeo','CashAdvance.Admin.Read'),
 -- Financial Manager: approve request + invoice docs
 ('CashAdvanceFinancialManager','CashAdvance.Request.Read'),('CashAdvanceFinancialManager','CashAdvance.Approval.FinancialManager'),
 ('CashAdvanceFinancialManager','CashAdvance.Invoice.Read'),('CashAdvanceFinancialManager','CashAdvance.Invoice.Modify'),
 ('CashAdvanceFinancialManager','CashAdvance.Ledger.Read'),('CashAdvanceFinancialManager','CashAdvance.Admin.Read'),
 -- Financial Expert: record payment + post ledger
 ('CashAdvanceFinancialExpert','CashAdvance.Request.Read'),('CashAdvanceFinancialExpert','CashAdvance.Invoice.Read'),
 ('CashAdvanceFinancialExpert','CashAdvance.Payment.Manage'),('CashAdvanceFinancialExpert','CashAdvance.Ledger.Read'),
 ('CashAdvanceFinancialExpert','CashAdvance.Ledger.Modify'),('CashAdvanceFinancialExpert','CashAdvance.Admin.Read'),
 -- Viewer: all reads
 ('CashAdvanceViewer','CashAdvance.Request.Read'),('CashAdvanceViewer','CashAdvance.Invoice.Read'),
 ('CashAdvanceViewer','CashAdvance.Ledger.Read'),('CashAdvanceViewer','CashAdvance.Admin.Read'),
 -- SuperAdmin: everything
 ('SuperAdmin','CashAdvance.Request.Read'),('SuperAdmin','CashAdvance.Request.Modify'),
 ('SuperAdmin','CashAdvance.Invoice.Read'),('SuperAdmin','CashAdvance.Invoice.Modify'),
 ('SuperAdmin','CashAdvance.Ledger.Read'),('SuperAdmin','CashAdvance.Ledger.Modify'),
 ('SuperAdmin','CashAdvance.Admin.Read'),('SuperAdmin','CashAdvance.Admin.Modify'),
 ('SuperAdmin','CashAdvance.Approval.Ceo'),('SuperAdmin','CashAdvance.Approval.FinancialManager'),
 ('SuperAdmin','CashAdvance.Payment.Manage');

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
SELECT r.Code AS Role, COUNT(*) AS Grants
FROM dbo.RolePermission rp
JOIN dbo.[Role] r ON r.Id = rp.RoleId
JOIN dbo.Permission p ON p.Id = rp.PermissionId
WHERE p.Code LIKE 'CashAdvance.%' AND rp.DeletedAt IS NULL
GROUP BY r.Code
ORDER BY r.Code;

PRINT N'Migration 025 applied: CashAdvance permissions (11) + roles (6) + grants';
