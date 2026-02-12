-- ============================================================
-- Database: KSS_Auth_Prod (microservice: auth)
-- User table for authentication only (register and login).
-- PersonId references external Person microservice (no FK cross-service).
-- CountryId ref KSS_Common_Prod (no FK cross-database).
-- ============================================================
IF DB_ID(N'KSS_Auth_Prod') IS NULL
    CREATE DATABASE [KSS_Auth_Prod];
GO

USE [KSS_Auth_Prod];
GO

-- User (GUID key)
-- Username, Email, Phone, PasswordHash for authentication. PersonId references external Person service.
CREATE TABLE dbo.[User] (
    Id                   UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_User_Id DEFAULT NEWSEQUENTIALID(),
    PersonId             UNIQUEIDENTIFIER NULL,
    Username             VARCHAR(50)     NOT NULL,
    Email                VARCHAR(128)     NOT NULL,
    Phone                VARCHAR(15)     NULL,
    CountryId            SMALLINT         NULL,
    PasswordHash         NVARCHAR(256)   NOT NULL,
    IsActive             BIT              NOT NULL CONSTRAINT DF_User_IsActive DEFAULT 1,
    IsEmailVerified      BIT              NOT NULL CONSTRAINT DF_User_IsEmailVerified DEFAULT 0,
    EmailVerifiedAt      DATETIME2(7)     NULL,
    IsPhoneVerified      BIT              NOT NULL CONSTRAINT DF_User_IsPhoneVerified DEFAULT 0,
    PhoneVerifiedAt      DATETIME2(7)     NULL,
    LastLoginAt          DATETIME2(7)     NULL,
    FailedLoginAttempts  INT              NOT NULL CONSTRAINT DF_User_FailedLoginAttempts DEFAULT 0,
    LockedUntil          DATETIME2(7)     NULL,
    PasswordResetToken   NVARCHAR(256)    NULL,
    PasswordResetExpires DATETIME2(7)     NULL,
    RefreshToken         NVARCHAR(512)    NULL,
    RefreshTokenExpires  DATETIME2(7)     NULL,
    CreatedAt            DATETIME2(7)     NOT NULL CONSTRAINT DF_User_CreatedAt DEFAULT SYSUTCDATETIME(),
    UpdatedAt            DATETIME2(7)     NOT NULL CONSTRAINT DF_User_UpdatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_User PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT UQ_User_Username UNIQUE (Username),
    CONSTRAINT UQ_User_Email UNIQUE (Email),
    CONSTRAINT CK_User_FailedLoginAttempts CHECK (FailedLoginAttempts >= 0)
);
CREATE NONCLUSTERED INDEX IX_User_Username ON dbo.[User] (Username);
CREATE NONCLUSTERED INDEX IX_User_Email ON dbo.[User] (Email);
CREATE NONCLUSTERED INDEX IX_User_PersonId ON dbo.[User] (PersonId) WHERE PersonId IS NOT NULL;
CREATE NONCLUSTERED INDEX IX_User_IsActive ON dbo.[User] (IsActive) WHERE IsActive = 1;
CREATE NONCLUSTERED INDEX IX_User_PasswordResetToken ON dbo.[User] (PasswordResetToken) WHERE PasswordResetToken IS NOT NULL;
CREATE NONCLUSTERED INDEX IX_User_RefreshToken ON dbo.[User] (RefreshToken) WHERE RefreshToken IS NOT NULL;
GO