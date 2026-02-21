-- ============================================
-- Create RBAC Tables + Seed Data for KSS Authorization System
-- Database: KSS_Auth_Prod
--
-- This script creates the Role, Permission, UserRole, and RolePermission
-- tables, then seeds permissions and assigns Admin role.
--
-- Safe to re-run: uses IF NOT EXISTS checks for table creation.
-- ============================================

USE [KSS_Auth_Prod];
GO

-- ============================================
-- 1. Create [Role] table
-- ============================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Role' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE [dbo].[Role] (
        [Id]          UNIQUEIDENTIFIER NOT NULL,
        [Name]        NVARCHAR(50)     NOT NULL,
        [Description] NVARCHAR(200)    NULL,
        [IsActive]    BIT              NOT NULL DEFAULT 1,
        [CreatedAt]   DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME(),
        [UpdatedAt]   DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT [PK_Role] PRIMARY KEY ([Id])
    );

    CREATE UNIQUE INDEX [IX_Role_Name] ON [dbo].[Role] ([Name]);
    PRINT 'Created table [dbo].[Role]';
END
ELSE
    PRINT 'Table [dbo].[Role] already exists - skipping';
GO

-- ============================================
-- 2. Create [Permission] table
-- ============================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Permission' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE [dbo].[Permission] (
        [Id]          UNIQUEIDENTIFIER NOT NULL,
        [Name]        NVARCHAR(100)    NOT NULL,
        [Description] NVARCHAR(200)    NULL,
        [Group]       NVARCHAR(50)     NULL,
        [CreatedAt]   DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME(),
        [UpdatedAt]   DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT [PK_Permission] PRIMARY KEY ([Id])
    );

    CREATE UNIQUE INDEX [IX_Permission_Name] ON [dbo].[Permission] ([Name]);
    PRINT 'Created table [dbo].[Permission]';
END
ELSE
    PRINT 'Table [dbo].[Permission] already exists - skipping';
GO

-- ============================================
-- 3. Create [UserRole] table (join: User <-> Role)
-- ============================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'UserRole' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE [dbo].[UserRole] (
        [UserId]     UNIQUEIDENTIFIER NOT NULL,
        [RoleId]     UNIQUEIDENTIFIER NOT NULL,
        [AssignedAt] DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT [PK_UserRole] PRIMARY KEY ([UserId], [RoleId]),
        CONSTRAINT [FK_UserRole_User] FOREIGN KEY ([UserId]) REFERENCES [dbo].[User] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_UserRole_Role] FOREIGN KEY ([RoleId]) REFERENCES [dbo].[Role] ([Id]) ON DELETE CASCADE
    );

    PRINT 'Created table [dbo].[UserRole]';
END
ELSE
    PRINT 'Table [dbo].[UserRole] already exists - skipping';
GO

-- ============================================
-- 4. Create [RolePermission] table (join: Role <-> Permission)
-- ============================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'RolePermission' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE [dbo].[RolePermission] (
        [RoleId]       UNIQUEIDENTIFIER NOT NULL,
        [PermissionId] UNIQUEIDENTIFIER NOT NULL,
        [AssignedAt]   DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT [PK_RolePermission] PRIMARY KEY ([RoleId], [PermissionId]),
        CONSTRAINT [FK_RolePermission_Role]       FOREIGN KEY ([RoleId])       REFERENCES [dbo].[Role] ([Id])       ON DELETE CASCADE,
        CONSTRAINT [FK_RolePermission_Permission] FOREIGN KEY ([PermissionId]) REFERENCES [dbo].[Permission] ([Id]) ON DELETE CASCADE
    );

    PRINT 'Created table [dbo].[RolePermission]';
END
ELSE
    PRINT 'Table [dbo].[RolePermission] already exists - skipping';
GO

-- ============================================
-- 5. Create EF Migrations History table (so EF knows the DB is in sync)
-- ============================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = '__EFMigrationsHistory' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE [dbo].[__EFMigrationsHistory] (
        [MigrationId]    NVARCHAR(150) NOT NULL,
        [ProductVersion] NVARCHAR(32)  NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );

    PRINT 'Created table [dbo].[__EFMigrationsHistory]';
END
GO

-- ============================================
-- 6. Seed Permissions
-- ============================================
PRINT '';
PRINT '--- Seeding Permissions ---';

-- Company permissions
IF NOT EXISTS (SELECT 1 FROM [dbo].[Permission] WHERE [Name] = 'Company.Read')
BEGIN
    INSERT INTO [dbo].[Permission] (Id, Name, [Description], [Group], CreatedAt, UpdatedAt)
    VALUES
        (NEWID(), 'Company.Read',   'View companies',       'Company', GETUTCDATE(), GETUTCDATE()),
        (NEWID(), 'Company.Create', 'Create companies',     'Company', GETUTCDATE(), GETUTCDATE()),
        (NEWID(), 'Company.Update', 'Update companies',     'Company', GETUTCDATE(), GETUTCDATE()),
        (NEWID(), 'Company.Delete', 'Delete companies',     'Company', GETUTCDATE(), GETUTCDATE());
    PRINT 'Seeded Company permissions';
END

-- Stakeholder permissions
IF NOT EXISTS (SELECT 1 FROM [dbo].[Permission] WHERE [Name] = 'Stakeholder.Read')
BEGIN
    INSERT INTO [dbo].[Permission] (Id, Name, [Description], [Group], CreatedAt, UpdatedAt)
    VALUES
        (NEWID(), 'Stakeholder.Read',   'View stakeholders',   'Stakeholder', GETUTCDATE(), GETUTCDATE()),
        (NEWID(), 'Stakeholder.Create', 'Create stakeholders', 'Stakeholder', GETUTCDATE(), GETUTCDATE()),
        (NEWID(), 'Stakeholder.Update', 'Update stakeholders', 'Stakeholder', GETUTCDATE(), GETUTCDATE()),
        (NEWID(), 'Stakeholder.Delete', 'Delete stakeholders', 'Stakeholder', GETUTCDATE(), GETUTCDATE());
    PRINT 'Seeded Stakeholder permissions';
END

-- Address permissions
IF NOT EXISTS (SELECT 1 FROM [dbo].[Permission] WHERE [Name] = 'Address.Read')
BEGIN
    INSERT INTO [dbo].[Permission] (Id, Name, [Description], [Group], CreatedAt, UpdatedAt)
    VALUES
        (NEWID(), 'Address.Read',   'View addresses',   'Address', GETUTCDATE(), GETUTCDATE()),
        (NEWID(), 'Address.Create', 'Create addresses', 'Address', GETUTCDATE(), GETUTCDATE()),
        (NEWID(), 'Address.Update', 'Update addresses', 'Address', GETUTCDATE(), GETUTCDATE()),
        (NEWID(), 'Address.Delete', 'Delete addresses', 'Address', GETUTCDATE(), GETUTCDATE());
    PRINT 'Seeded Address permissions';
END

-- Email permissions
IF NOT EXISTS (SELECT 1 FROM [dbo].[Permission] WHERE [Name] = 'Email.Read')
BEGIN
    INSERT INTO [dbo].[Permission] (Id, Name, [Description], [Group], CreatedAt, UpdatedAt)
    VALUES
        (NEWID(), 'Email.Read',   'View emails',   'Email', GETUTCDATE(), GETUTCDATE()),
        (NEWID(), 'Email.Create', 'Create emails', 'Email', GETUTCDATE(), GETUTCDATE()),
        (NEWID(), 'Email.Update', 'Update emails', 'Email', GETUTCDATE(), GETUTCDATE()),
        (NEWID(), 'Email.Delete', 'Delete emails', 'Email', GETUTCDATE(), GETUTCDATE());
    PRINT 'Seeded Email permissions';
END

-- Phone permissions
IF NOT EXISTS (SELECT 1 FROM [dbo].[Permission] WHERE [Name] = 'Phone.Read')
BEGIN
    INSERT INTO [dbo].[Permission] (Id, Name, [Description], [Group], CreatedAt, UpdatedAt)
    VALUES
        (NEWID(), 'Phone.Read',   'View phones',   'Phone', GETUTCDATE(), GETUTCDATE()),
        (NEWID(), 'Phone.Create', 'Create phones', 'Phone', GETUTCDATE(), GETUTCDATE()),
        (NEWID(), 'Phone.Update', 'Update phones', 'Phone', GETUTCDATE(), GETUTCDATE()),
        (NEWID(), 'Phone.Delete', 'Delete phones', 'Phone', GETUTCDATE(), GETUTCDATE());
    PRINT 'Seeded Phone permissions';
END

-- Lookup table permissions
IF NOT EXISTS (SELECT 1 FROM [dbo].[Permission] WHERE [Name] = 'CompanyType.Read')
BEGIN
    INSERT INTO [dbo].[Permission] (Id, Name, [Description], [Group], CreatedAt, UpdatedAt)
    VALUES
        (NEWID(), 'CompanyType.Read',     'View company types',     'Lookup', GETUTCDATE(), GETUTCDATE()),
        (NEWID(), 'Industry.Read',        'View industries',        'Lookup', GETUTCDATE(), GETUTCDATE()),
        (NEWID(), 'StakeholderType.Read', 'View stakeholder types', 'Lookup', GETUTCDATE(), GETUTCDATE()),
        (NEWID(), 'AddressLabel.Read',    'View address labels',    'Lookup', GETUTCDATE(), GETUTCDATE()),
        (NEWID(), 'EmailLabel.Read',      'View email labels',      'Lookup', GETUTCDATE(), GETUTCDATE()),
        (NEWID(), 'PhoneLabel.Read',      'View phone labels',      'Lookup', GETUTCDATE(), GETUTCDATE());
    PRINT 'Seeded Lookup permissions';
END

-- Role management permission
IF NOT EXISTS (SELECT 1 FROM [dbo].[Permission] WHERE [Name] = 'Role.Manage')
BEGIN
    INSERT INTO [dbo].[Permission] (Id, Name, [Description], [Group], CreatedAt, UpdatedAt)
    VALUES
        (NEWID(), 'Role.Manage', 'Manage roles and permissions', 'Admin', GETUTCDATE(), GETUTCDATE());
    PRINT 'Seeded Admin permissions';
END
GO

-- ============================================
-- 7. Create Admin Role + Assign all permissions + Assign to user
-- ============================================
PRINT '';
PRINT '--- Creating Admin Role ---';

IF NOT EXISTS (SELECT 1 FROM [dbo].[Role] WHERE [Name] = 'Admin')
BEGIN
    DECLARE @AdminRoleId UNIQUEIDENTIFIER = NEWID();

    INSERT INTO [dbo].[Role] (Id, Name, [Description], IsActive, CreatedAt, UpdatedAt)
    VALUES (@AdminRoleId, 'Admin', 'Full system administrator with all permissions', 1, GETUTCDATE(), GETUTCDATE());

    PRINT 'Created Admin role';

    -- Assign ALL permissions to Admin role
    INSERT INTO [dbo].[RolePermission] (RoleId, PermissionId, AssignedAt)
    SELECT @AdminRoleId, Id, GETUTCDATE()
    FROM [dbo].[Permission];

    PRINT 'Assigned all permissions to Admin role';

    -- Assign Admin role to user 0082280665
    DECLARE @AdminUserId UNIQUEIDENTIFIER;
    SELECT @AdminUserId = Id FROM [dbo].[User] WHERE Username = '0082280665';

    IF @AdminUserId IS NOT NULL
    BEGIN
        INSERT INTO [dbo].[UserRole] (UserId, RoleId, AssignedAt)
        VALUES (@AdminUserId, @AdminRoleId, GETUTCDATE());
        PRINT 'Admin role assigned to user 0082280665';
    END
    ELSE
        PRINT 'WARNING: User 0082280665 not found. Assign Admin role manually.';
END
ELSE
    PRINT 'Admin role already exists - skipping';
GO

-- ============================================
-- 8. Verification
-- ============================================
PRINT '';
PRINT '--- Verification ---';

SELECT 'Permissions' AS [Table], COUNT(*) AS [Count] FROM [dbo].[Permission]
UNION ALL
SELECT 'Roles', COUNT(*) FROM [dbo].[Role]
UNION ALL
SELECT 'RolePermissions', COUNT(*) FROM [dbo].[RolePermission]
UNION ALL
SELECT 'UserRoles', COUNT(*) FROM [dbo].[UserRole];
GO

PRINT '';
PRINT '=== RBAC Setup Complete ===';
GO
