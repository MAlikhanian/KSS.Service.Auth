-- ============================================================
-- Database: KSS_Auth_Prod (microservice: auth)
-- Truncate all tables. Run against KSS_Auth_Prod.
-- ============================================================
USE [KSS_Auth_Prod];
GO

-- Single table; no FKs to other tables in this database.
TRUNCATE TABLE dbo.[User];
GO
