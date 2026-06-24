-- 008_split_role_translations.sql
--
-- Same treatment as the Permission split: Role becomes a slim catalog
-- (Id, Code, ModuleId) with localized Name + Description in a new
-- RoleTranslation table. No audit columns since Role is DBA-only and
-- never modified by application code.
--
-- Apply to KSS_Auth_Prod and KSS_Auth_Dev.

SET QUOTED_IDENTIFIER ON;
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @fa SMALLINT = 12;
DECLARE @en SMALLINT = 10;

BEGIN TRANSACTION;

-- ── Step 1: Stage existing Role data + derive Code from Name (strip spaces).
SELECT
    Id,
    REPLACE(Name, ' ', '') AS Code,
    Name AS OldName,
    Description AS OldDescription,
    ModuleId
INTO #RoleBackup
FROM dbo.[Role];

-- ── Step 2: Drop FKs from RolePermission and UserRole
ALTER TABLE dbo.RolePermission DROP CONSTRAINT FK_RolePermission_Role;
ALTER TABLE dbo.UserRole       DROP CONSTRAINT FK_UserRole_Role;

-- ── Step 3: Drop and recreate Role with slim 3-column schema
DROP TABLE dbo.[Role];

CREATE TABLE dbo.[Role] (
    Id        UNIQUEIDENTIFIER NOT NULL,
    Code      VARCHAR(50)      NOT NULL,
    ModuleId  UNIQUEIDENTIFIER NULL,
    CONSTRAINT PK_Role PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT UQ_Role_Code UNIQUE (Code)
);
CREATE NONCLUSTERED INDEX IX_Role_ModuleId ON dbo.[Role](ModuleId);

-- ── Step 4: Re-insert (dynamic SQL — defers binding to the new schema)
EXEC sp_executesql N'
INSERT INTO dbo.[Role] (Id, Code, ModuleId)
SELECT Id, Code, ModuleId FROM #RoleBackup;
';

-- ── Step 5: Create RoleTranslation
CREATE TABLE dbo.RoleTranslation (
    RoleId       UNIQUEIDENTIFIER NOT NULL,
    LanguageId   SMALLINT         NOT NULL,
    Name         NVARCHAR(100)    NOT NULL,
    Description  NVARCHAR(200)    NULL,
    CONSTRAINT PK_RoleTranslation PRIMARY KEY CLUSTERED (RoleId, LanguageId),
    CONSTRAINT FK_RoleTranslation_Role
        FOREIGN KEY (RoleId) REFERENCES dbo.[Role](Id) ON DELETE CASCADE
);

-- ── Step 6: Seed translations (FA + EN)
DECLARE @t TABLE (Code VARCHAR(50), LanguageId SMALLINT, Name NVARCHAR(100), Description NVARCHAR(200));
INSERT INTO @t VALUES
    ('SuperAdmin',         @fa, N'مدیر کل',                    N'دسترسی کامل به همه ماژول‌ها'),
    ('SuperAdmin',         @en, N'Super Admin',                N'Full access across all modules'),
    ('PersonAdmin',        @fa, N'مدیر اشخاص',                  N'مدیریت کامل ماژول اشخاص'),
    ('PersonAdmin',        @en, N'Person Admin',               N'Manages everything in the Person module'),
    ('CompanyAdmin',       @fa, N'مدیر شرکت‌ها',                 N'مدیریت کامل ماژول شرکت‌ها'),
    ('CompanyAdmin',       @en, N'Company Admin',              N'Manages everything in the Company module'),
    ('MembersAdmin',       @fa, N'مدیر اعضا',                   N'مدیریت کامل ماژول اعضا'),
    ('MembersAdmin',       @en, N'Members Admin',              N'Manages everything in the Members module'),
    ('CreditRatingAdmin',  @fa, N'مدیر رتبه‌بندی اعتباری',       N'مدیریت کامل ماژول رتبه‌بندی اعتباری'),
    ('CreditRatingAdmin',  @en, N'CreditRating Admin',         N'Manages everything in the CreditRating module'),
    ('User',               @fa, N'کاربر',                      N'کاربر عادی'),
    ('User',               @en, N'User',                       N'Regular user');

-- Materialize @t into temp so dynamic SQL can see it.
SELECT * INTO #RoleTransSeed FROM @t;

EXEC sp_executesql N'
INSERT INTO dbo.RoleTranslation (RoleId, LanguageId, Name, Description)
SELECT r.Id, t.LanguageId, t.Name, t.Description
FROM #RoleTransSeed t
JOIN dbo.[Role] r ON r.Code = t.Code;
';

-- ── Step 7: Re-add FKs from RolePermission and UserRole
ALTER TABLE dbo.RolePermission
    ADD CONSTRAINT FK_RolePermission_Role
    FOREIGN KEY (RoleId) REFERENCES dbo.[Role](Id) ON DELETE CASCADE;

ALTER TABLE dbo.UserRole
    ADD CONSTRAINT FK_UserRole_Role
    FOREIGN KEY (RoleId) REFERENCES dbo.[Role](Id) ON DELETE CASCADE;

DROP TABLE #RoleBackup;
DROP TABLE #RoleTransSeed;

COMMIT TRANSACTION;

SELECT 'roles',              COUNT(*) FROM dbo.[Role];
SELECT 'role_translations',  COUNT(*) FROM dbo.RoleTranslation;
SELECT 'role_permissions',   COUNT(*) FROM dbo.RolePermission;
SELECT 'user_roles',         COUNT(*) FROM dbo.UserRole;

PRINT '008_split_role_translations.sql applied successfully.';
