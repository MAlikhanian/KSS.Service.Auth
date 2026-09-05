-- 031_add_cashadvance_readall_permissions.sql
--
-- Row-level scoping for cash advance: a Requester must only see THEIR OWN charge
-- requests and their own cash flow (ledger).
--
-- Problem: the requests page and the ledger page read the generic list endpoints
-- (/Api/CashAdvanceChargeRequest/ToListAll and /Api/CashAdvanceTransaction/ToListAll),
-- which return EVERY row to anyone holding CashAdvance.Request.Read / Ledger.Read —
-- and all six cash-advance roles hold both. So a requester could see every colleague's
-- requests, amounts and the full ledger.
--
-- Fix: two "see everyone" permissions. The backend returns all rows to callers who hold
-- them, and filters to the caller's own rows (RequesterPersonId / PersonId == caller)
-- for everyone else.
--
--   CashAdvance.Request.ReadAll -> see all charge requests
--   CashAdvance.Ledger.ReadAll  -> see all ledger transactions
--
-- Granted to every cash-advance role EXCEPT CashAdvanceRequester — that omission is
-- precisely what scopes requesters to their own data.
--
-- NOTE: this migration and the backend scoping change must ship together. The migration
-- alone changes nothing; the backend alone would scope everyone (including admins) to
-- their own rows.
--
-- Layout follows 026/029/030: idempotent inserts, fixed Guids identical across
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
-- Permissions (no new roles — existing cash-advance roles are reused)
-- ─────────────────────────────────────────────────────────────────────────
DECLARE @perms TABLE (
    Code VARCHAR(100), Id UNIQUEIDENTIFIER,
    EnName NVARCHAR(200), EnDesc NVARCHAR(400),
    FaName NVARCHAR(200), FaDesc NVARCHAR(400)
);
INSERT INTO @perms (Code, Id, EnName, EnDesc, FaName, FaDesc) VALUES
 ('CashAdvance.Request.ReadAll', '019F1100-0000-7102-8000-000000000024', N'View All Charge Requests', N'View charge requests of every person, not only your own', N'مشاهده همه درخواست‌ها',      N'مشاهده درخواست‌های تنخواه همه اشخاص، نه فقط درخواست‌های خود'),
 ('CashAdvance.Ledger.ReadAll',  '019F1100-0000-7102-8000-000000000025', N'View All Ledger Entries',  N'View ledger transactions of every person, not only your own', N'مشاهده کل گردش مالی', N'مشاهده گردش مالی همه اشخاص، نه فقط گردش مالی خود');

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
-- Grants — everyone EXCEPT CashAdvanceRequester
-- ─────────────────────────────────────────────────────────────────────────
DECLARE @grants TABLE (RoleCode VARCHAR(50), PermCode VARCHAR(100));
INSERT INTO @grants (RoleCode, PermCode) VALUES
 ('CashAdvanceAdmin',            'CashAdvance.Request.ReadAll'),
 ('CashAdvanceCeo',              'CashAdvance.Request.ReadAll'),
 ('CashAdvanceFinancialManager', 'CashAdvance.Request.ReadAll'),
 ('CashAdvanceFinancialExpert',  'CashAdvance.Request.ReadAll'),
 ('CashAdvanceViewer',           'CashAdvance.Request.ReadAll'),
 ('SuperAdmin',                  'CashAdvance.Request.ReadAll'),
 ('CashAdvanceAdmin',            'CashAdvance.Ledger.ReadAll'),
 ('CashAdvanceCeo',              'CashAdvance.Ledger.ReadAll'),
 ('CashAdvanceFinancialManager', 'CashAdvance.Ledger.ReadAll'),
 ('CashAdvanceFinancialExpert',  'CashAdvance.Ledger.ReadAll'),
 ('CashAdvanceViewer',           'CashAdvance.Ledger.ReadAll'),
 ('SuperAdmin',                  'CashAdvance.Ledger.ReadAll');
-- CashAdvanceRequester intentionally omitted: own rows only.

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
SELECT p.Code AS Permission, r.Code AS Role
FROM dbo.RolePermission rp
JOIN dbo.[Role] r ON r.Id = rp.RoleId
JOIN dbo.Permission p ON p.Id = rp.PermissionId
WHERE p.Code LIKE 'CashAdvance.%.ReadAll' AND rp.DeletedAt IS NULL
ORDER BY p.Code, r.Code;

PRINT N'Migration 031 applied: CashAdvance Request/Ledger ReadAll permissions + grants';
