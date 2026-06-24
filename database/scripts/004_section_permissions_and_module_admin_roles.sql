-- 004_section_permissions_and_module_admin_roles.sql
--
-- Wipes the old fine-grained CRUD permissions and replaces them with
-- section-level permissions matching the navigation structure. Also adds
-- per-module admin roles (Person Admin, Company Admin, Members Admin,
-- CreditRating Admin) alongside a renamed Super Admin.
--
-- Cross-DB operations on KSS_Common (run alongside Common migration 012).
--
-- Apply ONLY to KSS_Auth_Prod and KSS_Auth_Dev. Common DB will be cleaned by
-- this script directly (cross-DB) so it stays in sync.

SET QUOTED_IDENTIFIER ON;
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @system UNIQUEIDENTIFIER = '00000000-0000-0000-0000-000000000001';
DECLARE @now DATETIME2 = SYSUTCDATETIME();
DECLARE @fa SMALLINT = 12;
DECLARE @en SMALLINT = 10;

-- Pick the right Common DB based on which Auth DB we're running in.
DECLARE @commonDb SYSNAME =
    CASE WHEN DB_NAME() = 'KSS_Auth_Prod' THEN N'KSS_Common_Prod'
         ELSE N'KSS_Common_Dev'
    END;
DECLARE @sql NVARCHAR(MAX);

-- The 4 main module Guids (already seeded in Common migration 011)
DECLARE @modulePerson      UNIQUEIDENTIFIER = '019f1000-0000-7001-8000-000000000001';
DECLARE @moduleCompany     UNIQUEIDENTIFIER = '019f1000-0000-7001-8000-000000000002';
DECLARE @moduleMembers     UNIQUEIDENTIFIER = '019f1000-0000-7001-8000-000000000003';
DECLARE @moduleCreditRate  UNIQUEIDENTIFIER = '019f1000-0000-7001-8000-000000000004';

-- Section Resource Guids (will be inserted in Common below)
DECLARE @rPersonInfo      UNIQUEIDENTIFIER = '019f1000-0000-7003-8000-000000000001';
DECLARE @rPersonAssets    UNIQUEIDENTIFIER = '019f1000-0000-7003-8000-000000000002';
DECLARE @rPersonAccess    UNIQUEIDENTIFIER = '019f1000-0000-7003-8000-000000000003';
DECLARE @rPersonSecurity  UNIQUEIDENTIFIER = '019f1000-0000-7003-8000-000000000004';
DECLARE @rPersonAdmin     UNIQUEIDENTIFIER = '019f1000-0000-7003-8000-000000000005';
DECLARE @rCompanyInfo     UNIQUEIDENTIFIER = '019f1000-0000-7003-8000-000000000010';
DECLARE @rCompanyAdmin    UNIQUEIDENTIFIER = '019f1000-0000-7003-8000-000000000011';
DECLARE @rMembersDir      UNIQUEIDENTIFIER = '019f1000-0000-7003-8000-000000000020';
DECLARE @rMembersAdmin    UNIQUEIDENTIFIER = '019f1000-0000-7003-8000-000000000021';
DECLARE @rCRAssessment    UNIQUEIDENTIFIER = '019f1000-0000-7003-8000-000000000030';
DECLARE @rCRAdmin         UNIQUEIDENTIFIER = '019f1000-0000-7003-8000-000000000031';

BEGIN TRANSACTION;

-- ── Step 1: Wipe old permissions in Auth DB
DELETE FROM dbo.RolePermission;
DELETE FROM dbo.Permission;

-- ── Step 2: Wipe old Resources in Common DB (and remove auto-created modules)
SET @sql = N'
DELETE FROM ' + QUOTENAME(@commonDb) + N'.dbo.ResourceTranslation;
DELETE FROM ' + QUOTENAME(@commonDb) + N'.dbo.Resource;
DELETE FROM ' + QUOTENAME(@commonDb) + N'.dbo.ModuleTranslation
WHERE ModuleId NOT IN (
    ''019f1000-0000-7001-8000-000000000001'',
    ''019f1000-0000-7001-8000-000000000002'',
    ''019f1000-0000-7001-8000-000000000003'',
    ''019f1000-0000-7001-8000-000000000004''
);
DELETE FROM ' + QUOTENAME(@commonDb) + N'.dbo.Module
WHERE Id NOT IN (
    ''019f1000-0000-7001-8000-000000000001'',
    ''019f1000-0000-7001-8000-000000000002'',
    ''019f1000-0000-7001-8000-000000000003'',
    ''019f1000-0000-7001-8000-000000000004''
);
';
EXEC sp_executesql @sql;

-- ── Step 3: Insert section Resources in Common
SET @sql = N'
DECLARE @sys UNIQUEIDENTIFIER = ''00000000-0000-0000-0000-000000000001'';
DECLARE @t DATETIME2 = SYSUTCDATETIME();

-- Person sections
INSERT INTO ' + QUOTENAME(@commonDb) + N'.dbo.Resource (Id, ModuleId, Code, CreatedBy, CreatedAt) VALUES
    (''019f1000-0000-7003-8000-000000000001'', ''019f1000-0000-7001-8000-000000000001'', ''Information'', @sys, @t),
    (''019f1000-0000-7003-8000-000000000002'', ''019f1000-0000-7001-8000-000000000001'', ''Assets'',      @sys, @t),
    (''019f1000-0000-7003-8000-000000000003'', ''019f1000-0000-7001-8000-000000000001'', ''Access'',      @sys, @t),
    (''019f1000-0000-7003-8000-000000000004'', ''019f1000-0000-7001-8000-000000000001'', ''Security'',    @sys, @t),
    (''019f1000-0000-7003-8000-000000000005'', ''019f1000-0000-7001-8000-000000000001'', ''Admin'',       @sys, @t);
-- Company sections
INSERT INTO ' + QUOTENAME(@commonDb) + N'.dbo.Resource (Id, ModuleId, Code, CreatedBy, CreatedAt) VALUES
    (''019f1000-0000-7003-8000-000000000010'', ''019f1000-0000-7001-8000-000000000002'', ''Information'', @sys, @t),
    (''019f1000-0000-7003-8000-000000000011'', ''019f1000-0000-7001-8000-000000000002'', ''Admin'',       @sys, @t);
-- Members sections
INSERT INTO ' + QUOTENAME(@commonDb) + N'.dbo.Resource (Id, ModuleId, Code, CreatedBy, CreatedAt) VALUES
    (''019f1000-0000-7003-8000-000000000020'', ''019f1000-0000-7001-8000-000000000003'', ''Directory'', @sys, @t),
    (''019f1000-0000-7003-8000-000000000021'', ''019f1000-0000-7001-8000-000000000003'', ''Admin'',     @sys, @t);
-- CreditRating sections
INSERT INTO ' + QUOTENAME(@commonDb) + N'.dbo.Resource (Id, ModuleId, Code, CreatedBy, CreatedAt) VALUES
    (''019f1000-0000-7003-8000-000000000030'', ''019f1000-0000-7001-8000-000000000004'', ''Assessment'', @sys, @t),
    (''019f1000-0000-7003-8000-000000000031'', ''019f1000-0000-7001-8000-000000000004'', ''Admin'',      @sys, @t);

-- Translations
INSERT INTO ' + QUOTENAME(@commonDb) + N'.dbo.ResourceTranslation (ResourceId, LanguageId, Name, CreatedBy, CreatedAt) VALUES
    (''019f1000-0000-7003-8000-000000000001'', 12, N''اطلاعات شخص'', @sys, @t),
    (''019f1000-0000-7003-8000-000000000001'', 10, N''Information'', @sys, @t),
    (''019f1000-0000-7003-8000-000000000002'', 12, N''دارایی‌ها'',    @sys, @t),
    (''019f1000-0000-7003-8000-000000000002'', 10, N''Assets'',      @sys, @t),
    (''019f1000-0000-7003-8000-000000000003'', 12, N''دسترسی‌ها'',    @sys, @t),
    (''019f1000-0000-7003-8000-000000000003'', 10, N''Access'',      @sys, @t),
    (''019f1000-0000-7003-8000-000000000004'', 12, N''امنیت'',        @sys, @t),
    (''019f1000-0000-7003-8000-000000000004'', 10, N''Security'',    @sys, @t),
    (''019f1000-0000-7003-8000-000000000005'', 12, N''مدیریت'',       @sys, @t),
    (''019f1000-0000-7003-8000-000000000005'', 10, N''Admin'',       @sys, @t),
    (''019f1000-0000-7003-8000-000000000010'', 12, N''اطلاعات شرکت'', @sys, @t),
    (''019f1000-0000-7003-8000-000000000010'', 10, N''Information'', @sys, @t),
    (''019f1000-0000-7003-8000-000000000011'', 12, N''مدیریت'',       @sys, @t),
    (''019f1000-0000-7003-8000-000000000011'', 10, N''Admin'',       @sys, @t),
    (''019f1000-0000-7003-8000-000000000020'', 12, N''فهرست اعضا'', @sys, @t),
    (''019f1000-0000-7003-8000-000000000020'', 10, N''Directory'',  @sys, @t),
    (''019f1000-0000-7003-8000-000000000021'', 12, N''مدیریت'',       @sys, @t),
    (''019f1000-0000-7003-8000-000000000021'', 10, N''Admin'',       @sys, @t),
    (''019f1000-0000-7003-8000-000000000030'', 12, N''ارزیابی'',      @sys, @t),
    (''019f1000-0000-7003-8000-000000000030'', 10, N''Assessment'',  @sys, @t),
    (''019f1000-0000-7003-8000-000000000031'', 12, N''مدیریت'',       @sys, @t),
    (''019f1000-0000-7003-8000-000000000031'', 10, N''Admin'',       @sys, @t);
';
EXEC sp_executesql @sql;

-- ── Step 4: Insert section Permissions (22 total — 5+2+2+2 sections × 2 actions)
-- Use staged Guids that we can reference for RolePermission seeding.
DECLARE @permTable TABLE (Id UNIQUEIDENTIFIER, ResourceId UNIQUEIDENTIFIER, Name VARCHAR(100), Description NVARCHAR(200), [Group] NVARCHAR(50), ModuleScope VARCHAR(20));

INSERT INTO @permTable VALUES
    (NEWID(), @rPersonInfo,    'Person.Information.Read',   N'View Person information section',  'Person', 'person'),
    (NEWID(), @rPersonInfo,    'Person.Information.Manage', N'Manage Person information section','Person', 'person'),
    (NEWID(), @rPersonAssets,  'Person.Assets.Read',        N'View Person assets',               'Person', 'person'),
    (NEWID(), @rPersonAssets,  'Person.Assets.Manage',      N'Manage Person assets',             'Person', 'person'),
    (NEWID(), @rPersonAccess,  'Person.Access.Read',        N'View Person access grants',        'Person', 'person'),
    (NEWID(), @rPersonAccess,  'Person.Access.Manage',      N'Manage Person access grants',      'Person', 'person'),
    (NEWID(), @rPersonSecurity,'Person.Security.Read',      N'View Person security/account',     'Person', 'person'),
    (NEWID(), @rPersonSecurity,'Person.Security.Manage',    N'Manage Person security/account',   'Person', 'person'),
    (NEWID(), @rPersonAdmin,   'Person.Admin.Read',         N'View Person admin pages',          'Person', 'person'),
    (NEWID(), @rPersonAdmin,   'Person.Admin.Manage',       N'Manage Person admin pages',        'Person', 'person'),

    (NEWID(), @rCompanyInfo,   'Company.Information.Read',  N'View Company information section',  'Company','company'),
    (NEWID(), @rCompanyInfo,   'Company.Information.Manage',N'Manage Company information section','Company','company'),
    (NEWID(), @rCompanyAdmin,  'Company.Admin.Read',        N'View Company admin pages',         'Company','company'),
    (NEWID(), @rCompanyAdmin,  'Company.Admin.Manage',      N'Manage Company admin pages',       'Company','company'),

    (NEWID(), @rMembersDir,    'Members.Directory.Read',    N'View Members directory',     'Members','members'),
    (NEWID(), @rMembersDir,    'Members.Directory.Manage',  N'Manage Members directory',   'Members','members'),
    (NEWID(), @rMembersAdmin,  'Members.Admin.Read',        N'View Members admin pages',   'Members','members'),
    (NEWID(), @rMembersAdmin,  'Members.Admin.Manage',      N'Manage Members admin pages', 'Members','members'),

    (NEWID(), @rCRAssessment,  'CreditRating.Assessment.Read',   N'View CreditRating assessments',     'CreditRating','creditrating'),
    (NEWID(), @rCRAssessment,  'CreditRating.Assessment.Manage', N'Manage CreditRating assessments',   'CreditRating','creditrating'),
    (NEWID(), @rCRAdmin,       'CreditRating.Admin.Read',        N'View CreditRating admin pages',     'CreditRating','creditrating'),
    (NEWID(), @rCRAdmin,       'CreditRating.Admin.Manage',      N'Manage CreditRating admin pages',   'CreditRating','creditrating');

INSERT INTO dbo.Permission (Id, Name, Description, [Group], ResourceId, CreatedBy, CreatedAt, IsActive)
SELECT Id, Name, Description, [Group], ResourceId, @system, @now, 1
FROM @permTable;

-- ── Step 5: Roles — rename Admin → Super Admin and add module admins
UPDATE dbo.[Role]
SET Name = 'Super Admin',
    Description = N'Full system access across all modules',
    ModuleId = NULL
WHERE Name = 'Admin';

DECLARE @superAdminId UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM dbo.[Role] WHERE Name = 'Super Admin');

-- Module admin roles (only insert if not already there — idempotent)
DECLARE @personAdminId      UNIQUEIDENTIFIER = NEWID();
DECLARE @companyAdminId     UNIQUEIDENTIFIER = NEWID();
DECLARE @membersAdminId     UNIQUEIDENTIFIER = NEWID();
DECLARE @creditRatingAdminId UNIQUEIDENTIFIER = NEWID();

IF NOT EXISTS (SELECT 1 FROM dbo.[Role] WHERE Name = 'Person Admin')
    INSERT INTO dbo.[Role] (Id, Name, Description, ModuleId, CreatedBy, CreatedAt, IsActive)
    VALUES (@personAdminId, 'Person Admin', N'Manages everything in the Person module', @modulePerson, @system, @now, 1);
ELSE
    SELECT @personAdminId = Id FROM dbo.[Role] WHERE Name = 'Person Admin';

IF NOT EXISTS (SELECT 1 FROM dbo.[Role] WHERE Name = 'Company Admin')
    INSERT INTO dbo.[Role] (Id, Name, Description, ModuleId, CreatedBy, CreatedAt, IsActive)
    VALUES (@companyAdminId, 'Company Admin', N'Manages everything in the Company module', @moduleCompany, @system, @now, 1);
ELSE
    SELECT @companyAdminId = Id FROM dbo.[Role] WHERE Name = 'Company Admin';

IF NOT EXISTS (SELECT 1 FROM dbo.[Role] WHERE Name = 'Members Admin')
    INSERT INTO dbo.[Role] (Id, Name, Description, ModuleId, CreatedBy, CreatedAt, IsActive)
    VALUES (@membersAdminId, 'Members Admin', N'Manages everything in the Members module', @moduleMembers, @system, @now, 1);
ELSE
    SELECT @membersAdminId = Id FROM dbo.[Role] WHERE Name = 'Members Admin';

IF NOT EXISTS (SELECT 1 FROM dbo.[Role] WHERE Name = 'CreditRating Admin')
    INSERT INTO dbo.[Role] (Id, Name, Description, ModuleId, CreatedBy, CreatedAt, IsActive)
    VALUES (@creditRatingAdminId, 'CreditRating Admin', N'Manages everything in the CreditRating module', @moduleCreditRate, @system, @now, 1);
ELSE
    SELECT @creditRatingAdminId = Id FROM dbo.[Role] WHERE Name = 'CreditRating Admin';

-- ── Step 6: RolePermission — link Super Admin to every permission
INSERT INTO dbo.RolePermission (RoleId, PermissionId, CreatedBy, CreatedAt)
SELECT @superAdminId, p.Id, @system, @now
FROM dbo.Permission p;

-- ── Step 7: RolePermission — link each module admin to its module's permissions
INSERT INTO dbo.RolePermission (RoleId, PermissionId, CreatedBy, CreatedAt)
SELECT @personAdminId, Id, @system, @now FROM @permTable WHERE ModuleScope = 'person';

INSERT INTO dbo.RolePermission (RoleId, PermissionId, CreatedBy, CreatedAt)
SELECT @companyAdminId, Id, @system, @now FROM @permTable WHERE ModuleScope = 'company';

INSERT INTO dbo.RolePermission (RoleId, PermissionId, CreatedBy, CreatedAt)
SELECT @membersAdminId, Id, @system, @now FROM @permTable WHERE ModuleScope = 'members';

INSERT INTO dbo.RolePermission (RoleId, PermissionId, CreatedBy, CreatedAt)
SELECT @creditRatingAdminId, Id, @system, @now FROM @permTable WHERE ModuleScope = 'creditrating';

COMMIT TRANSACTION;

PRINT '004_section_permissions_and_module_admin_roles.sql applied successfully.';
