-- 001_audit_columns_PROD.sql
--
-- Drop+recreate dbo.Permission, dbo.Role, dbo.[User] in KSS_Auth_Prod to:
--   * Add audit columns (CreatedBy, UpdatedBy, DeletedBy, DeletedAt)
--   * Move CreatedAt/UpdatedAt to end as canonical clump:
--     CreatedBy, CreatedAt, UpdatedBy, UpdatedAt, DeletedBy, DeletedAt
--   * Place IsActive at the very end (parents only)
--   * Make UpdatedAt NULL (per CLAUDE.md — only set on actual updates)
--
-- M2M tables (RolePermission, UserRole) are left untouched (Option A).
--
-- Source for restored data: KSS_Auth_Dev (just refreshed from Prod).
--
-- Apply ONLY to KSS_Auth_Prod.

SET QUOTED_IDENTIFIER ON;
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @system_user UNIQUEIDENTIFIER = '00000000-0000-0000-0000-000000000001';

BEGIN TRANSACTION;

-- ── Step 1: Drop 4 incoming FKs to Permission/Role/User
ALTER TABLE dbo.RolePermission DROP CONSTRAINT FK_RolePermission_Permission;
ALTER TABLE dbo.RolePermission DROP CONSTRAINT FK_RolePermission_Role;
ALTER TABLE dbo.UserRole       DROP CONSTRAINT FK_UserRole_Role;
ALTER TABLE dbo.UserRole       DROP CONSTRAINT FK_UserRole_User;

-- ── Step 2: Drop the 3 main tables
DROP TABLE dbo.[User];
DROP TABLE dbo.[Role];
DROP TABLE dbo.Permission;

-- ── Step 3: Recreate Permission (parent: business cols, audit clump, IsActive last)
CREATE TABLE dbo.Permission (
    Id           UNIQUEIDENTIFIER NOT NULL,
    Name         NVARCHAR(100)    NOT NULL,
    [Description] NVARCHAR(200)   NULL,
    [Group]      NVARCHAR(50)     NULL,
    CreatedBy    UNIQUEIDENTIFIER NOT NULL,
    CreatedAt    DATETIME2        NOT NULL,
    UpdatedBy    UNIQUEIDENTIFIER NULL,
    UpdatedAt    DATETIME2        NULL,
    DeletedBy    UNIQUEIDENTIFIER NULL,
    DeletedAt    DATETIME2        NULL,
    IsActive     BIT              NOT NULL CONSTRAINT DF_Permission_IsActive DEFAULT (1),
    CONSTRAINT PK_Permission PRIMARY KEY CLUSTERED (Id)
);
CREATE UNIQUE NONCLUSTERED INDEX IX_Permission_Name ON dbo.Permission([Name]);

-- ── Step 4: Recreate Role (parent: business cols, audit clump, IsActive last)
CREATE TABLE dbo.[Role] (
    Id            UNIQUEIDENTIFIER NOT NULL,
    Name          NVARCHAR(50)     NOT NULL,
    [Description] NVARCHAR(200)    NULL,
    CreatedBy     UNIQUEIDENTIFIER NOT NULL,
    CreatedAt     DATETIME2        NOT NULL,
    UpdatedBy     UNIQUEIDENTIFIER NULL,
    UpdatedAt     DATETIME2        NULL,
    DeletedBy     UNIQUEIDENTIFIER NULL,
    DeletedAt     DATETIME2        NULL,
    IsActive      BIT              NOT NULL CONSTRAINT DF_Role_IsActive DEFAULT (1),
    CONSTRAINT PK_Role PRIMARY KEY CLUSTERED (Id)
);
CREATE UNIQUE NONCLUSTERED INDEX IX_Role_Name ON dbo.[Role]([Name]);

-- ── Step 5: Recreate User (parent: business cols, audit clump, IsActive last)
CREATE TABLE dbo.[User] (
    Id                    UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_User_Id DEFAULT (NEWSEQUENTIALID()),
    PersonId              UNIQUEIDENTIFIER NULL,
    Username              VARCHAR(50)      NOT NULL,
    Email                 VARCHAR(128)     NOT NULL,
    Phone                 VARCHAR(15)      NULL,
    CountryId             SMALLINT         NULL,
    PasswordHash          NVARCHAR(256)    NOT NULL,
    IsEmailVerified       BIT              NOT NULL CONSTRAINT DF_User_IsEmailVerified DEFAULT (0),
    EmailVerifiedAt       DATETIME2        NULL,
    IsPhoneVerified       BIT              NOT NULL CONSTRAINT DF_User_IsPhoneVerified DEFAULT (0),
    PhoneVerifiedAt       DATETIME2        NULL,
    LastLoginAt           DATETIME2        NULL,
    FailedLoginAttempts   INT              NOT NULL CONSTRAINT DF_User_FailedLoginAttempts DEFAULT (0)
                                           CONSTRAINT CK_User_FailedLoginAttempts CHECK (FailedLoginAttempts >= 0),
    LockedUntil           DATETIME2        NULL,
    PasswordResetToken    NVARCHAR(256)    NULL,
    PasswordResetExpires  DATETIME2        NULL,
    RefreshToken          NVARCHAR(512)    NULL,
    RefreshTokenExpires   DATETIME2        NULL,
    CreatedBy             UNIQUEIDENTIFIER NOT NULL,
    CreatedAt             DATETIME2        NOT NULL,
    UpdatedBy             UNIQUEIDENTIFIER NULL,
    UpdatedAt             DATETIME2        NULL,
    DeletedBy             UNIQUEIDENTIFIER NULL,
    DeletedAt             DATETIME2        NULL,
    IsActive              BIT              NOT NULL CONSTRAINT DF_User_IsActive DEFAULT (1),
    CONSTRAINT PK_User PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT UQ_User_Username UNIQUE (Username),
    CONSTRAINT UQ_User_Email    UNIQUE (Email)
);
CREATE NONCLUSTERED INDEX IX_User_Username           ON dbo.[User](Username);
CREATE NONCLUSTERED INDEX IX_User_Email              ON dbo.[User](Email);
CREATE NONCLUSTERED INDEX IX_User_PersonId           ON dbo.[User](PersonId);
CREATE NONCLUSTERED INDEX IX_User_IsActive           ON dbo.[User](IsActive);
CREATE NONCLUSTERED INDEX IX_User_PasswordResetToken ON dbo.[User](PasswordResetToken);
CREATE NONCLUSTERED INDEX IX_User_RefreshToken       ON dbo.[User](RefreshToken);

-- ── Step 6: Restore Permission data from Auth_Dev (dynamic SQL — defers binding)
EXEC sp_executesql N'
INSERT INTO dbo.Permission (Id, Name, [Description], [Group], CreatedBy, CreatedAt, UpdatedBy, UpdatedAt, DeletedBy, DeletedAt, IsActive)
SELECT Id, Name, [Description], [Group], @sysu, CreatedAt, NULL, NULL, NULL, NULL, 1
FROM KSS_Auth_Dev.dbo.Permission;
', N'@sysu UNIQUEIDENTIFIER', @sysu = @system_user;

-- ── Step 7: Restore Role data from Auth_Dev
EXEC sp_executesql N'
INSERT INTO dbo.[Role] (Id, Name, [Description], CreatedBy, CreatedAt, UpdatedBy, UpdatedAt, DeletedBy, DeletedAt, IsActive)
SELECT Id, Name, [Description], @sysu, CreatedAt, NULL, NULL, NULL, NULL, IsActive
FROM KSS_Auth_Dev.dbo.[Role];
', N'@sysu UNIQUEIDENTIFIER', @sysu = @system_user;

-- ── Step 8: Restore User data from Auth_Dev
EXEC sp_executesql N'
INSERT INTO dbo.[User] (
    Id, PersonId, Username, Email, Phone, CountryId, PasswordHash,
    IsEmailVerified, EmailVerifiedAt, IsPhoneVerified, PhoneVerifiedAt,
    LastLoginAt, FailedLoginAttempts, LockedUntil,
    PasswordResetToken, PasswordResetExpires, RefreshToken, RefreshTokenExpires,
    CreatedBy, CreatedAt, UpdatedBy, UpdatedAt, DeletedBy, DeletedAt, IsActive
)
SELECT
    Id, PersonId, Username, Email, Phone, CountryId, PasswordHash,
    IsEmailVerified, EmailVerifiedAt, IsPhoneVerified, PhoneVerifiedAt,
    LastLoginAt, FailedLoginAttempts, LockedUntil,
    PasswordResetToken, PasswordResetExpires, RefreshToken, RefreshTokenExpires,
    @sysu, CreatedAt, NULL, NULL, NULL, NULL, IsActive
FROM KSS_Auth_Dev.dbo.[User];
', N'@sysu UNIQUEIDENTIFIER', @sysu = @system_user;

-- ── Step 9: Recreate the 4 incoming FKs
ALTER TABLE dbo.RolePermission ADD CONSTRAINT FK_RolePermission_Permission FOREIGN KEY (PermissionId) REFERENCES dbo.Permission(Id) ON DELETE CASCADE;
ALTER TABLE dbo.RolePermission ADD CONSTRAINT FK_RolePermission_Role       FOREIGN KEY (RoleId)       REFERENCES dbo.[Role](Id)    ON DELETE CASCADE;
ALTER TABLE dbo.UserRole       ADD CONSTRAINT FK_UserRole_Role             FOREIGN KEY (RoleId)       REFERENCES dbo.[Role](Id)    ON DELETE CASCADE;
ALTER TABLE dbo.UserRole       ADD CONSTRAINT FK_UserRole_User             FOREIGN KEY (UserId)       REFERENCES dbo.[User](Id)    ON DELETE CASCADE;

COMMIT TRANSACTION;

PRINT '001_audit_columns_PROD.sql applied successfully.';
