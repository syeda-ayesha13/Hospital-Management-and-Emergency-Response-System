/* =====================================================================
   HMERS - Hospital Management and Emergency Response System
   SQL Server schema (T-SQL)
   How to run: open in SSMS -> press F5 (Execute). Safe to re-run:
   it drops and recreates all tables. Then run load_data.py to fill them.
   ===================================================================== */

IF DB_ID('HMERS') IS NULL CREATE DATABASE HMERS;
GO
USE HMERS;
GO

/* ---------- Drop old tables (children first) ---------- */
DROP TABLE IF EXISTS StatusHistory, Alert, Dispatch, Emergency, SurgeryRecord,
    StaffShift, RoomRecords, BedRecords, MedicalRecord, Appointment, Road,
    Ambulance, Room, Bed, Ward, Helpers, Nurse, Doctor, Patients, Location,
    Department, AppUser;
GO

/* ---------- Reference / master tables ---------- */
CREATE TABLE Department (
    dept_Id    INT         NOT NULL PRIMARY KEY,
    dept_Name  VARCHAR(50) NOT NULL UNIQUE
);

CREATE TABLE Location (
    location_Id    INT          NOT NULL PRIMARY KEY,
    location_Name  VARCHAR(100) NOT NULL,
    location_Type  VARCHAR(20)  NOT NULL,           -- Hospital / City
    latitude       DECIMAL(9,6) NOT NULL,
    longitude      DECIMAL(9,6) NOT NULL
);

CREATE TABLE Patients (
    patient_Id     INT          NOT NULL PRIMARY KEY,
    FName          VARCHAR(50)  NOT NULL,
    LName          VARCHAR(50)  NOT NULL,
    Gender         CHAR(1)      NOT NULL CHECK (Gender IN ('M','F')),
    Date_Of_Birth  DATE         NOT NULL,
    contact_No     VARCHAR(20)  NOT NULL,           -- text, not number
    pt_Address     VARCHAR(200) NOT NULL
);

/* ---------- Staff ---------- */
CREATE TABLE Doctor (
    doct_Id       INT         NOT NULL PRIMARY KEY,
    dept_Id       INT         NOT NULL REFERENCES Department(dept_Id),
    FName         VARCHAR(50) NOT NULL,
    LName         VARCHAR(50) NOT NULL,
    Gender        CHAR(1)     NOT NULL CHECK (Gender IN ('M','F')),
    contact_No    VARCHAR(20) NOT NULL,
    surgeon_Type  VARCHAR(50) NULL,                 -- NULL = not a surgeon
    office_No     INT         NULL
);

CREATE TABLE Nurse (
    nurse_Id    INT         NOT NULL PRIMARY KEY,
    dept_Id     INT         NOT NULL REFERENCES Department(dept_Id),
    FName       VARCHAR(50) NOT NULL,
    LName       VARCHAR(50) NOT NULL,
    Gender      CHAR(1)     NOT NULL CHECK (Gender IN ('M','F')),
    contact_No  VARCHAR(20) NOT NULL
);

CREATE TABLE Helpers (
    helper_Id   INT         NOT NULL PRIMARY KEY,
    dept_Id     INT         NOT NULL REFERENCES Department(dept_Id),
    FName       VARCHAR(50) NOT NULL,
    LName       VARCHAR(50) NOT NULL,
    Gender      CHAR(1)     NOT NULL CHECK (Gender IN ('M','F')),
    contact_No  VARCHAR(20) NOT NULL
);

/* ---------- Physical resources ---------- */
CREATE TABLE Ward (
    ward_No    INT         NOT NULL PRIMARY KEY,
    ward_Name  VARCHAR(50) NOT NULL,
    dept_Id    INT         NOT NULL REFERENCES Department(dept_Id)
);

CREATE TABLE Bed (
    bed_No   INT NOT NULL PRIMARY KEY,
    ward_No  INT NOT NULL REFERENCES Ward(ward_No)
);

CREATE TABLE Room (
    room_No    INT         NOT NULL PRIMARY KEY,
    dept_Id    INT         NOT NULL REFERENCES Department(dept_Id),
    room_Type  VARCHAR(50) NOT NULL
);

CREATE TABLE Ambulance (
    unit_Id           INT         NOT NULL PRIMARY KEY,
    unit_Code         VARCHAR(10) NOT NULL UNIQUE,
    unit_Type         VARCHAR(20) NOT NULL,         -- BLS / ALS / Mobile ICU
    dept_Id           INT         NOT NULL REFERENCES Department(dept_Id),
    base_Location_Id  INT         NOT NULL REFERENCES Location(location_Id),
    status            VARCHAR(20) NOT NULL
);

/* ---------- Road network (graph for Dijkstra) ---------- */
CREATE TABLE Road (
    road_Id           INT          NOT NULL PRIMARY KEY,
    from_Location_Id  INT          NOT NULL REFERENCES Location(location_Id),
    to_Location_Id    INT          NOT NULL REFERENCES Location(location_Id),
    distance_Km       DECIMAL(8,2) NOT NULL CHECK (distance_Km > 0),
    travel_Time_Min   DECIMAL(8,2) NOT NULL CHECK (travel_Time_Min > 0),
    road_Type         VARCHAR(30)  NOT NULL,
    CHECK (from_Location_Id <> to_Location_Id)
);

/* ---------- Outpatient activity ---------- */
CREATE TABLE Appointment (
    appointment_Id       INT          NOT NULL PRIMARY KEY,
    patient_Id           INT          NOT NULL REFERENCES Patients(patient_Id),
    doct_Id              INT          NOT NULL REFERENCES Doctor(doct_Id),
    reason               VARCHAR(100) NOT NULL,
    appointment_Date     DATE         NOT NULL,
    payment_amount       INT          NOT NULL CHECK (payment_amount >= 0),
    mode_of_payment      VARCHAR(30)  NOT NULL,
    mode_of_appointment  VARCHAR(30)  NOT NULL,
    appointment_status   VARCHAR(30)  NOT NULL
);

CREATE TABLE MedicalRecord (
    record_Id            INT           NOT NULL PRIMARY KEY,
    doct_Id              INT           NOT NULL REFERENCES Doctor(doct_Id),
    patient_Id           INT           NOT NULL REFERENCES Patients(patient_Id),
    visit_Date           DATE          NOT NULL,
    curr_Weight          DECIMAL(6,2)  NOT NULL,
    curr_height          DECIMAL(6,2)  NOT NULL,
    curr_Blood_Pressure  VARCHAR(10)   NOT NULL,
    curr_Temp_F          DECIMAL(5,2)  NOT NULL,
    diagnosis            VARCHAR(100)  NOT NULL,
    treatment            VARCHAR(150)  NOT NULL,
    next_Visit           DATE          NULL          -- NULL = no follow-up needed
);

/* ---------- Admissions ---------- */
CREATE TABLE BedRecords (
    admission_Id     INT          NOT NULL PRIMARY KEY,
    bed_No           INT          NOT NULL REFERENCES Bed(bed_No),
    patient_Id       INT          NOT NULL REFERENCES Patients(patient_Id),
    nurse_Id         INT          NOT NULL REFERENCES Nurse(nurse_Id),
    helper_Id        INT          NOT NULL REFERENCES Helpers(helper_Id),
    admission_Date   DATE         NOT NULL,
    discharge_Date   DATE         NOT NULL,
    amount           INT          NOT NULL CHECK (amount >= 0),
    mode_of_payment  VARCHAR(30)  NOT NULL,
    CHECK (discharge_Date >= admission_Date)
);

CREATE TABLE RoomRecords (
    admission_Id     INT          NOT NULL PRIMARY KEY,
    room_no          INT          NOT NULL REFERENCES Room(room_No),
    patient_Id       INT          NOT NULL REFERENCES Patients(patient_Id),
    nurse_Id         INT          NOT NULL REFERENCES Nurse(nurse_Id),
    helper_Id        INT          NOT NULL REFERENCES Helpers(helper_Id),
    admission_Date   DATE         NOT NULL,
    discharge_Date   DATE         NOT NULL,
    amount           INT          NOT NULL CHECK (amount >= 0),
    mode_of_payment  VARCHAR(30)  NOT NULL,
    CHECK (discharge_Date >= admission_Date)
);

CREATE TABLE StaffShift (
    shift_Id     INT  NOT NULL PRIMARY KEY,
    doct_Id      INT  NULL REFERENCES Doctor(doct_Id),
    nurse_Id     INT  NULL REFERENCES Nurse(nurse_Id),
    helper_Id    INT  NULL REFERENCES Helpers(helper_Id),
    shift_Date   DATE NOT NULL,
    shift_Start  TIME(0) NOT NULL,
    shift_End    TIME(0) NOT NULL,
    -- a shift belongs to exactly ONE staff member (doctor OR nurse OR helper)
    CHECK ( (CASE WHEN doct_Id   IS NULL THEN 0 ELSE 1 END
           + CASE WHEN nurse_Id  IS NULL THEN 0 ELSE 1 END
           + CASE WHEN helper_Id IS NULL THEN 0 ELSE 1 END) = 1 )
);

CREATE TABLE SurgeryRecord (
    surgery_Id    INT          NOT NULL PRIMARY KEY,
    patient_Id    INT          NOT NULL REFERENCES Patients(patient_Id),
    surgeon_Id    INT          NOT NULL REFERENCES Doctor(doct_Id),
    surgery_Type  VARCHAR(100) NOT NULL,
    surgery_Date  DATE         NOT NULL,
    start_Time    TIME(0)      NOT NULL,
    end_Time      TIME(0)      NOT NULL,
    room_no       INT          NOT NULL REFERENCES Room(room_No),
    notes         VARCHAR(100) NULL,
    nurse_Id      INT          NOT NULL REFERENCES Nurse(nurse_Id),
    helper_Id     INT          NOT NULL REFERENCES Helpers(helper_Id)
);

/* ---------- Emergency response ---------- */
CREATE TABLE Emergency (
    emergency_Id        INT          NOT NULL PRIMARY KEY,
    patient_Id          INT          NOT NULL REFERENCES Patients(patient_Id),
    location_Id         INT          NOT NULL REFERENCES Location(location_Id),
    doct_Id             INT          NULL     REFERENCES Doctor(doct_Id),
    emergency_Type      VARCHAR(60)  NOT NULL,
    severity_Level      TINYINT      NOT NULL CHECK (severity_Level BETWEEN 1 AND 5),  -- 1 = most critical
    priority_Score      INT          NOT NULL,
    required_Unit_Type  VARCHAR(20)  NULL,          -- NULL = walk-in, no ambulance
    arrival_Mode        VARCHAR(20)  NOT NULL,
    reported_At         DATETIME2(0) NOT NULL,
    status              VARCHAR(20)  NOT NULL,
    disposition         VARCHAR(60)  NULL,          -- NULL = cancelled
    ward_No             INT          NULL REFERENCES Ward(ward_No),   -- only if admitted
    bed_No              INT          NULL REFERENCES Bed(bed_No),
    discharge_Date      DATE         NULL,
    closed_At           DATETIME2(0) NOT NULL,
    CHECK (closed_At >= reported_At)
);

CREATE TABLE Dispatch (
    dispatch_Id              INT          NOT NULL PRIMARY KEY,
    emergency_Id             INT          NOT NULL REFERENCES Emergency(emergency_Id),
    unit_Id                  INT          NOT NULL REFERENCES Ambulance(unit_Id),
    nurse_Id                 INT          NOT NULL REFERENCES Nurse(nurse_Id),
    helper_Id                INT          NOT NULL REFERENCES Helpers(helper_Id),
    origin_Location_Id       INT          NOT NULL REFERENCES Location(location_Id),
    route_To_Scene           VARCHAR(30)  NOT NULL,   -- e.g. 601>631
    distance_To_Scene_Km     DECIMAL(8,2) NOT NULL,
    route_To_Hospital        VARCHAR(30)  NOT NULL,
    distance_To_Hospital_Km  DECIMAL(8,2) NOT NULL,
    assigned_At              DATETIME2(0) NOT NULL,
    departed_At              DATETIME2(0) NOT NULL,
    arrived_Scene_At         DATETIME2(0) NOT NULL,
    left_Scene_At            DATETIME2(0) NOT NULL,
    arrived_Hospital_At      DATETIME2(0) NOT NULL,
    left_Hospital_At         DATETIME2(0) NOT NULL,
    completed_At             DATETIME2(0) NOT NULL,
    CHECK (arrived_Scene_At >= assigned_At)
);

CREATE TABLE Alert (
    alert_Id      INT           NOT NULL PRIMARY KEY,
    emergency_Id  INT           NULL REFERENCES Emergency(emergency_Id),
    unit_Id       INT           NULL REFERENCES Ambulance(unit_Id),
    alert_Type    VARCHAR(50)   NOT NULL,
    alert_Level   VARCHAR(20)   NOT NULL CHECK (alert_Level IN ('Critical','Warning','Info')),
    message       VARCHAR(200)  NOT NULL,
    created_At    DATETIME2(0)  NOT NULL,
    resolved_At   DATETIME2(0)  NULL
);

/* entity_Id points to Emergency OR Ambulance depending on entity_Type,
   so it cannot be a normal foreign key. */
CREATE TABLE StatusHistory (
    history_Id   INT          NOT NULL PRIMARY KEY,
    entity_Type  VARCHAR(30)  NOT NULL CHECK (entity_Type IN ('Emergency','Ambulance')),
    entity_Id    INT          NOT NULL,
    old_Status   VARCHAR(30)  NULL,                 -- NULL = first status
    new_Status   VARCHAR(30)  NOT NULL,
    changed_At   DATETIME2(0) NOT NULL,
    changed_By   VARCHAR(50)  NOT NULL
);

/* ---------- Login / roles (proposal objective 3) ----------
   NOT in the dataset: filled later by the C# backend.
   Never store plain passwords - store a hash.                */
CREATE TABLE AppUser (
    user_Id        INT IDENTITY(1,1) PRIMARY KEY,
    username       VARCHAR(50)  NOT NULL UNIQUE,
    password_Hash  VARCHAR(255) NOT NULL,
    role           VARCHAR(30)  NOT NULL
                   CHECK (role IN ('Admin','Doctor','Nurse','Receptionist','EmergencyCoordinator')),
    created_At     DATETIME2(0) NOT NULL DEFAULT SYSDATETIME()
);
GO

/* ---------- Indexes for the most common lookups ---------- */
CREATE INDEX IX_Emergency_reported   ON Emergency(reported_At);
CREATE INDEX IX_Emergency_priority   ON Emergency(severity_Level, priority_Score DESC);
CREATE INDEX IX_Emergency_patient    ON Emergency(patient_Id);
CREATE INDEX IX_Dispatch_emergency   ON Dispatch(emergency_Id);
CREATE INDEX IX_Dispatch_unit        ON Dispatch(unit_Id);
CREATE INDEX IX_Appointment_patient  ON Appointment(patient_Id);
CREATE INDEX IX_Appointment_doctor   ON Appointment(doct_Id, appointment_Date);
CREATE INDEX IX_MedicalRecord_pat    ON MedicalRecord(patient_Id, visit_Date);
CREATE INDEX IX_BedRecords_patient   ON BedRecords(patient_Id);
CREATE INDEX IX_Road_from            ON Road(from_Location_Id);
CREATE INDEX IX_StatusHistory_entity ON StatusHistory(entity_Type, entity_Id);
GO

PRINT 'HMERS schema created successfully.';
GO
