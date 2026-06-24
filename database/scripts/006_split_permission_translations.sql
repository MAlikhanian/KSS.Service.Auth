-- 006_split_permission_translations.sql
--
-- Splits Permission into:
--   * Permission.Code           — machine identity used in JWT claims (was 'Name')
--   * PermissionTranslation     — per-language Name + Description (new table)
--
-- Drops the old Permission.Name and Permission.Description columns.
--
-- Apply to KSS_Auth_Prod and KSS_Auth_Dev.

SET QUOTED_IDENTIFIER ON;
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @system UNIQUEIDENTIFIER = '00000000-0000-0000-0000-000000000001';
DECLARE @now DATETIME2 = SYSUTCDATETIME();
DECLARE @fa SMALLINT = 12;
DECLARE @en SMALLINT = 10;

BEGIN TRANSACTION;

-- ── Step 1: Stage existing data
SELECT
    Id, Name AS Code, Description, ModuleId, ResourceId,
    CreatedBy, CreatedAt, UpdatedBy, UpdatedAt, DeletedBy, DeletedAt, IsActive
INTO #PermBackup
FROM dbo.Permission;

-- ── Step 2: Drop FK + table
ALTER TABLE dbo.RolePermission DROP CONSTRAINT FK_RolePermission_Permission;
DROP TABLE dbo.Permission;

-- ── Step 3: Recreate Permission with Code instead of Name/Description
CREATE TABLE dbo.Permission (
    Id           UNIQUEIDENTIFIER NOT NULL,
    Code         VARCHAR(100)     NOT NULL,
    ModuleId     UNIQUEIDENTIFIER NOT NULL,
    ResourceId   UNIQUEIDENTIFIER NOT NULL,
    CreatedBy    UNIQUEIDENTIFIER NOT NULL,
    CreatedAt    DATETIME2        NOT NULL,
    UpdatedBy    UNIQUEIDENTIFIER NULL,
    UpdatedAt    DATETIME2        NULL,
    DeletedBy    UNIQUEIDENTIFIER NULL,
    DeletedAt    DATETIME2        NULL,
    IsActive     BIT              NOT NULL CONSTRAINT DF_Permission_IsActive DEFAULT (1),
    CONSTRAINT PK_Permission PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT UQ_Permission_Code UNIQUE (Code)
);

CREATE NONCLUSTERED INDEX IX_Permission_ModuleId   ON dbo.Permission(ModuleId);
CREATE NONCLUSTERED INDEX IX_Permission_ResourceId ON dbo.Permission(ResourceId);

-- ── Step 4: Re-insert from stage (dynamic SQL — defers binding to the new schema)
EXEC sp_executesql N'
INSERT INTO dbo.Permission
    (Id, Code, ModuleId, ResourceId, CreatedBy, CreatedAt, UpdatedBy, UpdatedAt, DeletedBy, DeletedAt, IsActive)
SELECT
    Id, Code, ModuleId, ResourceId, CreatedBy, CreatedAt, UpdatedBy, UpdatedAt, DeletedBy, DeletedAt, IsActive
FROM #PermBackup;
';

-- ── Step 5: Re-add RolePermission FK
ALTER TABLE dbo.RolePermission
    ADD CONSTRAINT FK_RolePermission_Permission
    FOREIGN KEY (PermissionId) REFERENCES dbo.Permission(Id) ON DELETE CASCADE;

-- ── Step 6: Create PermissionTranslation
CREATE TABLE dbo.PermissionTranslation (
    PermissionId UNIQUEIDENTIFIER NOT NULL,
    LanguageId   SMALLINT         NOT NULL,
    Name         NVARCHAR(100)    NOT NULL,
    Description  NVARCHAR(200)    NULL,
    CreatedBy    UNIQUEIDENTIFIER NOT NULL,
    CreatedAt    DATETIME2        NOT NULL,
    UpdatedBy    UNIQUEIDENTIFIER NULL,
    UpdatedAt    DATETIME2        NULL,
    DeletedBy    UNIQUEIDENTIFIER NULL,
    DeletedAt    DATETIME2        NULL,
    CONSTRAINT PK_PermissionTranslation PRIMARY KEY CLUSTERED (PermissionId, LanguageId),
    CONSTRAINT FK_PermissionTranslation_Permission
        FOREIGN KEY (PermissionId) REFERENCES dbo.Permission(Id) ON DELETE CASCADE
);

-- ── Step 7: Seed translations for all 22 permissions (FA + EN)
-- Helper map: hard-coded per Code so values are deterministic across envs.
DECLARE @t TABLE (
    Code         VARCHAR(100),
    LanguageId   SMALLINT,
    Name         NVARCHAR(100),
    Description  NVARCHAR(200)
);

INSERT INTO @t VALUES
    -- Person
    ('Person.Information.Read',     @fa, N'مشاهده اطلاعات شخص',     N'مشاهده بخش اطلاعات شخص'),
    ('Person.Information.Read',     @en, N'View Person Information', N'View the Person information section'),
    ('Person.Information.Manage',   @fa, N'مدیریت اطلاعات شخص',     N'افزودن، ویرایش و حذف در بخش اطلاعات شخص'),
    ('Person.Information.Manage',   @en, N'Manage Person Information', N'Create, update and delete in the Person information section'),
    ('Person.Assets.Read',          @fa, N'مشاهده دارایی‌ها',         N'مشاهده دارایی‌های شخص'),
    ('Person.Assets.Read',          @en, N'View Person Assets',       N'View Person assets'),
    ('Person.Assets.Manage',        @fa, N'مدیریت دارایی‌ها',         N'افزودن و ویرایش دارایی‌های شخص'),
    ('Person.Assets.Manage',        @en, N'Manage Person Assets',     N'Create, update and delete Person assets'),
    ('Person.Access.Read',          @fa, N'مشاهده دسترسی‌ها',         N'مشاهده دسترسی‌های اعطا شده روی پروفایل'),
    ('Person.Access.Read',          @en, N'View Person Access Grants', N'View access grants on a Person profile'),
    ('Person.Access.Manage',        @fa, N'مدیریت دسترسی‌ها',         N'اعطا و حذف دسترسی روی پروفایل'),
    ('Person.Access.Manage',        @en, N'Manage Person Access Grants', N'Grant and revoke access on a Person profile'),
    ('Person.Security.Read',        @fa, N'مشاهده امنیت حساب',       N'مشاهده اطلاعات امنیتی حساب کاربر'),
    ('Person.Security.Read',        @en, N'View Account Security',    N'View account security state'),
    ('Person.Security.Manage',      @fa, N'مدیریت امنیت حساب',       N'تغییر رمز، قفل، تأیید ایمیل/تلفن، خاتمه نشست'),
    ('Person.Security.Manage',      @en, N'Manage Account Security',  N'Reset password, lock, verify, revoke sessions'),
    ('Person.Admin.Read',           @fa, N'مشاهده پنل مدیریت اشخاص', N'مشاهده پنل مدیریت بخش اشخاص'),
    ('Person.Admin.Read',           @en, N'View Person Admin',        N'View the Persons admin pages'),
    ('Person.Admin.Manage',         @fa, N'مدیریت پنل اشخاص',         N'مدیریت پنل بخش اشخاص'),
    ('Person.Admin.Manage',         @en, N'Manage Person Admin',      N'Manage the Persons admin pages'),

    -- Company
    ('Company.Information.Read',    @fa, N'مشاهده اطلاعات شرکت',    N'مشاهده بخش اطلاعات شرکت'),
    ('Company.Information.Read',    @en, N'View Company Information', N'View the Company information section'),
    ('Company.Information.Manage',  @fa, N'مدیریت اطلاعات شرکت',    N'افزودن، ویرایش و حذف در بخش اطلاعات شرکت'),
    ('Company.Information.Manage',  @en, N'Manage Company Information', N'Create, update and delete in the Company information section'),
    ('Company.Admin.Read',          @fa, N'مشاهده پنل مدیریت شرکت‌ها', N'مشاهده پنل مدیریت بخش شرکت‌ها'),
    ('Company.Admin.Read',          @en, N'View Company Admin',      N'View the Companies admin pages'),
    ('Company.Admin.Manage',        @fa, N'مدیریت پنل شرکت‌ها',       N'مدیریت پنل بخش شرکت‌ها'),
    ('Company.Admin.Manage',        @en, N'Manage Company Admin',    N'Manage the Companies admin pages'),

    -- Members
    ('Members.Directory.Read',      @fa, N'مشاهده فهرست اعضا',     N'مشاهده فهرست اعضا'),
    ('Members.Directory.Read',      @en, N'View Members Directory', N'View the Members directory'),
    ('Members.Directory.Manage',    @fa, N'مدیریت فهرست اعضا',     N'افزودن و ویرایش اعضا'),
    ('Members.Directory.Manage',    @en, N'Manage Members Directory', N'Create, update and delete members'),
    ('Members.Admin.Read',          @fa, N'مشاهده پنل مدیریت اعضا', N'مشاهده پنل مدیریت بخش اعضا'),
    ('Members.Admin.Read',          @en, N'View Members Admin',     N'View the Members admin pages'),
    ('Members.Admin.Manage',        @fa, N'مدیریت پنل اعضا',         N'مدیریت پنل بخش اعضا'),
    ('Members.Admin.Manage',        @en, N'Manage Members Admin',   N'Manage the Members admin pages'),

    -- CreditRating
    ('CreditRating.Assessment.Read',   @fa, N'مشاهده ارزیابی‌ها',     N'مشاهده ارزیابی‌های رتبه‌بندی اعتباری'),
    ('CreditRating.Assessment.Read',   @en, N'View Credit Assessments', N'View credit-rating assessments'),
    ('CreditRating.Assessment.Manage', @fa, N'مدیریت ارزیابی‌ها',     N'ایجاد و ویرایش ارزیابی‌های رتبه‌بندی'),
    ('CreditRating.Assessment.Manage', @en, N'Manage Credit Assessments', N'Create, update and delete credit-rating assessments'),
    ('CreditRating.Admin.Read',        @fa, N'مشاهده پنل رتبه‌بندی',    N'مشاهده پنل مدیریت رتبه‌بندی اعتباری'),
    ('CreditRating.Admin.Read',        @en, N'View CreditRating Admin', N'View the Credit Rating admin pages'),
    ('CreditRating.Admin.Manage',      @fa, N'مدیریت پنل رتبه‌بندی',    N'مدیریت پنل بخش رتبه‌بندی اعتباری'),
    ('CreditRating.Admin.Manage',      @en, N'Manage CreditRating Admin', N'Manage the Credit Rating admin pages');

-- Materialize @t into a temp table so the dynamic SQL can see it.
SELECT * INTO #TransSeed FROM @t;

EXEC sp_executesql N'
INSERT INTO dbo.PermissionTranslation (PermissionId, LanguageId, Name, Description, CreatedBy, CreatedAt)
SELECT p.Id, t.LanguageId, t.Name, t.Description, @sys, @t_now
FROM #TransSeed t
JOIN dbo.Permission p ON p.Code = t.Code;
', N'@sys UNIQUEIDENTIFIER, @t_now DATETIME2', @sys = @system, @t_now = @now;

DROP TABLE #PermBackup;
DROP TABLE #TransSeed;

COMMIT TRANSACTION;

SELECT 'permissions',         COUNT(*) FROM dbo.Permission;
SELECT 'translations',        COUNT(*) FROM dbo.PermissionTranslation;
SELECT 'role_permissions',    COUNT(*) FROM dbo.RolePermission;

PRINT '006_split_permission_translations.sql applied successfully.';
