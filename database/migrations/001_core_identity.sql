/* =====================================================================
   Migration 001_core_identity
   Upgrades a database that was created from the FIRST schema version
   (Patients all NOT NULL, no sequence, AppUser without staff links).

   Changes
     1. Patients: detail columns become NULLable, new column is_Temporary
     2. Patients: CHECK - permanent patients must be complete
     3. Patients: SEQUENCE for new IDs, started above the maximum imported ID
     4. AppUser : doct_Id / nurse_Id links + CHECK + one-login-per-person indexes
     5. SchemaMigration bookkeeping table

   Safe to run more than once: every step checks first. Existing patient
   data and IDs are not modified. database/schema.sql is NOT changed by this
   migration; schema.sql stays the baseline and this file applies the changes.

   IMPORTANT - after any data import, run once:  EXEC dbo.usp_ResyncPatientSequence;
   (otherwise the patient-ID sequence may hand out an ID that already exists).
   Typical order on a fresh machine:
       schema.sql  ->  load_data.py  ->  this migration
   ===================================================================== */
SET QUOTED_IDENTIFIER ON;
GO
USE HMERS;
GO

/* 0. bookkeeping + "already applied?" guard */
IF OBJECT_ID('dbo.SchemaMigration', 'U') IS NULL
    CREATE TABLE dbo.SchemaMigration (
        migration_Id VARCHAR(100) NOT NULL PRIMARY KEY,
        applied_At   DATETIME2(0) NOT NULL DEFAULT SYSDATETIME());
GO
IF EXISTS (SELECT 1 FROM dbo.SchemaMigration WHERE migration_Id = '001_core_identity')
BEGIN
    PRINT 'Migration 001_core_identity was already applied. Nothing to do.';
    SET NOEXEC ON;
END
GO

/* 1a. sequence for new patient IDs */
IF OBJECT_ID('dbo.seq_Patient_Id', 'SO') IS NULL
    CREATE SEQUENCE dbo.seq_Patient_Id AS INT START WITH 1 INCREMENT BY 1 NO CACHE;
GO

/* 1b. SQL Server blocks ALTER COLUMN while a CHECK depends on the column,
       so drop the old unnamed Gender check (re-added with a name below). */
DECLARE @ck sysname;
SELECT @ck = cc.name
FROM sys.check_constraints cc
JOIN sys.columns c ON c.object_id = cc.parent_object_id AND c.column_id = cc.parent_column_id
WHERE cc.parent_object_id = OBJECT_ID('dbo.Patients')
  AND c.name = 'Gender' AND cc.name <> 'CK_Patients_Gender';
IF @ck IS NOT NULL EXEC('ALTER TABLE dbo.Patients DROP CONSTRAINT ' + QUOTENAME(@ck));
GO

/* 1c. detail columns become nullable (temporary patients) */
ALTER TABLE dbo.Patients ALTER COLUMN FName         VARCHAR(50)  NULL;
ALTER TABLE dbo.Patients ALTER COLUMN LName         VARCHAR(50)  NULL;
ALTER TABLE dbo.Patients ALTER COLUMN Gender        CHAR(1)      NULL;
ALTER TABLE dbo.Patients ALTER COLUMN Date_Of_Birth DATE         NULL;
ALTER TABLE dbo.Patients ALTER COLUMN contact_No    VARCHAR(20)  NULL;
ALTER TABLE dbo.Patients ALTER COLUMN pt_Address    VARCHAR(200) NULL;
GO

/* 1d. is_Temporary flag (existing rows become permanent = 0) */
IF COL_LENGTH('dbo.Patients', 'is_Temporary') IS NULL
    ALTER TABLE dbo.Patients
        ADD is_Temporary BIT NOT NULL CONSTRAINT DF_Patients_IsTemporary DEFAULT (0);
GO

/* 2. constraints (separate batch: the new column must exist first) */
IF OBJECT_ID('dbo.CK_Patients_Gender', 'C') IS NULL
    ALTER TABLE dbo.Patients
        ADD CONSTRAINT CK_Patients_Gender CHECK (Gender IN ('M','F'));

IF OBJECT_ID('dbo.CK_Patients_PermanentComplete', 'C') IS NULL
    ALTER TABLE dbo.Patients
        ADD CONSTRAINT CK_Patients_PermanentComplete CHECK (
            is_Temporary = 1
            OR (FName IS NOT NULL AND LName IS NOT NULL AND Gender IS NOT NULL
                AND Date_Of_Birth IS NOT NULL AND contact_No IS NOT NULL AND pt_Address IS NOT NULL));

/* 3a. new IDs come from the sequence; explicit (imported) IDs still allowed */
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints
               WHERE parent_object_id = OBJECT_ID('dbo.Patients') AND name = 'DF_Patients_Id')
    ALTER TABLE dbo.Patients
        ADD CONSTRAINT DF_Patients_Id DEFAULT (NEXT VALUE FOR dbo.seq_Patient_Id) FOR patient_Id;
GO

/* 3b. procedure that moves the sequence above the maximum existing ID */
CREATE OR ALTER PROCEDURE dbo.usp_ResyncPatientSequence
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @next INT = ISNULL((SELECT MAX(patient_Id) FROM dbo.Patients), 0) + 1;
    DECLARE @sql NVARCHAR(200) =
        N'ALTER SEQUENCE dbo.seq_Patient_Id RESTART WITH ' + CAST(@next AS NVARCHAR(12));
    EXEC sys.sp_executesql @sql;
    SELECT @next AS next_patient_id;
END
GO
EXEC dbo.usp_ResyncPatientSequence;      -- dataset: max ID 1599 -> next ID 1600
GO

/* 4. AppUser <-> Doctor / Nurse */
IF COL_LENGTH('dbo.AppUser', 'doct_Id') IS NULL
    ALTER TABLE dbo.AppUser ADD doct_Id INT NULL
        CONSTRAINT FK_AppUser_Doctor REFERENCES dbo.Doctor(doct_Id);
IF COL_LENGTH('dbo.AppUser', 'nurse_Id') IS NULL
    ALTER TABLE dbo.AppUser ADD nurse_Id INT NULL
        CONSTRAINT FK_AppUser_Nurse REFERENCES dbo.Nurse(nurse_Id);
GO

IF OBJECT_ID('dbo.CK_AppUser_StaffLink', 'C') IS NULL
    ALTER TABLE dbo.AppUser ADD CONSTRAINT CK_AppUser_StaffLink CHECK (
           (role = 'Doctor' AND doct_Id IS NOT NULL AND nurse_Id IS NULL)
        OR (role = 'Nurse'  AND nurse_Id IS NOT NULL AND doct_Id IS NULL)
        OR (role NOT IN ('Doctor','Nurse') AND doct_Id IS NULL AND nurse_Id IS NULL));

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_AppUser_Doctor' AND object_id = OBJECT_ID('dbo.AppUser'))
    CREATE UNIQUE INDEX UX_AppUser_Doctor ON dbo.AppUser(doct_Id)  WHERE doct_Id  IS NOT NULL;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_AppUser_Nurse' AND object_id = OBJECT_ID('dbo.AppUser'))
    CREATE UNIQUE INDEX UX_AppUser_Nurse  ON dbo.AppUser(nurse_Id) WHERE nurse_Id IS NOT NULL;
GO

/* 5. record that the migration ran */
INSERT INTO dbo.SchemaMigration (migration_Id) VALUES ('001_core_identity');
PRINT 'Migration 001_core_identity applied successfully.';
GO
SET NOEXEC OFF;
GO
