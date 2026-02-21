-- Seed Permissions and Admin Role for KSS Authorization System
-- Run this after the AddRolesAndPermissions migration

-- ============================================
-- 1. Insert Permissions
-- ============================================

-- Company permissions
INSERT INTO [dbo].[Permission] (Id, Name, [Description], [Group], CreatedAt, UpdatedAt)
VALUES
    (NEWID(), 'Company.Read',   'View companies',           'Company',     GETUTCDATE(), GETUTCDATE()),
    (NEWID(), 'Company.Create', 'Create companies',         'Company',     GETUTCDATE(), GETUTCDATE()),
    (NEWID(), 'Company.Update', 'Update companies',         'Company',     GETUTCDATE(), GETUTCDATE()),
    (NEWID(), 'Company.Delete', 'Delete companies',         'Company',     GETUTCDATE(), GETUTCDATE());

-- Stakeholder permissions
INSERT INTO [dbo].[Permission] (Id, Name, [Description], [Group], CreatedAt, UpdatedAt)
VALUES
    (NEWID(), 'Stakeholder.Read',   'View stakeholders',       'Stakeholder', GETUTCDATE(), GETUTCDATE()),
    (NEWID(), 'Stakeholder.Create', 'Create stakeholders',     'Stakeholder', GETUTCDATE(), GETUTCDATE()),
    (NEWID(), 'Stakeholder.Update', 'Update stakeholders',     'Stakeholder', GETUTCDATE(), GETUTCDATE()),
    (NEWID(), 'Stakeholder.Delete', 'Delete stakeholders',     'Stakeholder', GETUTCDATE(), GETUTCDATE());

-- Address permissions
INSERT INTO [dbo].[Permission] (Id, Name, [Description], [Group], CreatedAt, UpdatedAt)
VALUES
    (NEWID(), 'Address.Read',   'View addresses',           'Address',     GETUTCDATE(), GETUTCDATE()),
    (NEWID(), 'Address.Create', 'Create addresses',         'Address',     GETUTCDATE(), GETUTCDATE()),
    (NEWID(), 'Address.Update', 'Update addresses',         'Address',     GETUTCDATE(), GETUTCDATE()),
    (NEWID(), 'Address.Delete', 'Delete addresses',         'Address',     GETUTCDATE(), GETUTCDATE());

-- Email permissions
INSERT INTO [dbo].[Permission] (Id, Name, [Description], [Group], CreatedAt, UpdatedAt)
VALUES
    (NEWID(), 'Email.Read',   'View emails',               'Email',       GETUTCDATE(), GETUTCDATE()),
    (NEWID(), 'Email.Create', 'Create emails',             'Email',       GETUTCDATE(), GETUTCDATE()),
    (NEWID(), 'Email.Update', 'Update emails',             'Email',       GETUTCDATE(), GETUTCDATE()),
    (NEWID(), 'Email.Delete', 'Delete emails',             'Email',       GETUTCDATE(), GETUTCDATE());

-- Phone permissions
INSERT INTO [dbo].[Permission] (Id, Name, [Description], [Group], CreatedAt, UpdatedAt)
VALUES
    (NEWID(), 'Phone.Read',   'View phones',               'Phone',       GETUTCDATE(), GETUTCDATE()),
    (NEWID(), 'Phone.Create', 'Create phones',             'Phone',       GETUTCDATE(), GETUTCDATE()),
    (NEWID(), 'Phone.Update', 'Update phones',             'Phone',       GETUTCDATE(), GETUTCDATE()),
    (NEWID(), 'Phone.Delete', 'Delete phones',             'Phone',       GETUTCDATE(), GETUTCDATE());

-- Lookup table permissions (Label, Type, Industry)
INSERT INTO [dbo].[Permission] (Id, Name, [Description], [Group], CreatedAt, UpdatedAt)
VALUES
    (NEWID(), 'CompanyType.Read',       'View company types',       'Lookup', GETUTCDATE(), GETUTCDATE()),
    (NEWID(), 'Industry.Read',          'View industries',          'Lookup', GETUTCDATE(), GETUTCDATE()),
    (NEWID(), 'StakeholderType.Read',   'View stakeholder types',   'Lookup', GETUTCDATE(), GETUTCDATE()),
    (NEWID(), 'AddressLabel.Read',      'View address labels',      'Lookup', GETUTCDATE(), GETUTCDATE()),
    (NEWID(), 'EmailLabel.Read',        'View email labels',        'Lookup', GETUTCDATE(), GETUTCDATE()),
    (NEWID(), 'PhoneLabel.Read',        'View phone labels',        'Lookup', GETUTCDATE(), GETUTCDATE());

-- Role management permission
INSERT INTO [dbo].[Permission] (Id, Name, [Description], [Group], CreatedAt, UpdatedAt)
VALUES
    (NEWID(), 'Role.Manage', 'Manage roles and permissions', 'Admin', GETUTCDATE(), GETUTCDATE());

-- ============================================
-- 2. Create Admin Role
-- ============================================

DECLARE @AdminRoleId UNIQUEIDENTIFIER = NEWID();

INSERT INTO [dbo].[Role] (Id, Name, [Description], IsActive, CreatedAt, UpdatedAt)
VALUES (@AdminRoleId, 'Admin', 'Full system administrator with all permissions', 1, GETUTCDATE(), GETUTCDATE());

-- ============================================
-- 3. Assign ALL permissions to Admin role
-- ============================================

INSERT INTO [dbo].[RolePermission] (RoleId, PermissionId, AssignedAt)
SELECT @AdminRoleId, Id, GETUTCDATE()
FROM [dbo].[Permission];

-- ============================================
-- 4. Assign Admin role to first user (0082280665)
-- ============================================

DECLARE @AdminUserId UNIQUEIDENTIFIER;
SELECT @AdminUserId = Id FROM [dbo].[User] WHERE Username = '0082280665';

IF @AdminUserId IS NOT NULL
BEGIN
    INSERT INTO [dbo].[UserRole] (UserId, RoleId, AssignedAt)
    VALUES (@AdminUserId, @AdminRoleId, GETUTCDATE());

    PRINT 'Admin role assigned to user 0082280665';
END
ELSE
BEGIN
    PRINT 'WARNING: User 0082280665 not found. Assign Admin role manually.';
END

-- ============================================
-- Verification
-- ============================================

SELECT 'Permissions' AS [Table], COUNT(*) AS [Count] FROM [dbo].[Permission]
UNION ALL
SELECT 'Roles', COUNT(*) FROM [dbo].[Role]
UNION ALL
SELECT 'RolePermissions', COUNT(*) FROM [dbo].[RolePermission]
UNION ALL
SELECT 'UserRoles', COUNT(*) FROM [dbo].[UserRole];
