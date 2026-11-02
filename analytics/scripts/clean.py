"""
clean.py  -  Step 1: Load raw CSVs, fix types, handle missing values,
             create new features, save cleaned files.
Run from project root:  python scripts/clean.py
"""
import os
import numpy as np
import pandas as pd

RAW, CLEAN = "data/raw", "data/cleaned"
os.makedirs(CLEAN, exist_ok=True)

# Which columns in each table are dates (so pandas parses them properly)
DATE_COLS = {
    "Appointment":   ["appointment_Date"],
    "BedRecords":    ["admission_Date", "discharge_Date"],
    "RoomRecords":   ["admission_Date", "discharge_Date"],
    "MedicalRecord": ["visit_Date", "next_Visit"],
    "Patients":      ["Date_Of_Birth"],
    "StaffShift":    ["shift_Date"],
    "SurgeryRecord": ["surgery_Date"],
    "Emergency":     ["reported_At", "discharge_Date", "closed_At"],
    "Dispatch":      ["assigned_At", "departed_At", "arrived_Scene_At", "left_Scene_At",
                      "arrived_Hospital_At", "left_Hospital_At", "completed_At"],
    "StatusHistory": ["changed_At"],
    "Alert":         ["created_At", "resolved_At"],
}
TABLES = ["Appointment", "Bed", "BedRecords", "Department", "Doctor", "Helpers",
          "MedicalRecord", "Nurse", "Patients", "Room", "RoomRecords", "StaffShift",
          "SurgeryRecord", "Ward", "Location", "Road", "Ambulance", "Emergency",
          "Dispatch", "StatusHistory", "Alert"]

report = []
data = {}
for t in TABLES:
    df = pd.read_csv(f"{RAW}/{t}.csv", parse_dates=DATE_COLS.get(t, []))
    before = len(df)
    df = df.drop_duplicates()
    # strip extra spaces in all text columns
    for c in df.columns:
        if pd.api.types.is_string_dtype(df[c]):
            df[c] = df[c].str.strip()
    # phone numbers must be text, not numbers
    if "contact_No" in df.columns:
        df["contact_No"] = df["contact_No"].astype(str)
    report.append((t, before, len(df), int(df.isna().sum().sum())))
    data[t] = df

# ---- Meaningful fills for columns that are empty ON PURPOSE ----
em = data["Emergency"]
em["required_Unit_Type"] = em["required_Unit_Type"].fillna("None (Walk-in)")
em["disposition"] = em["disposition"].fillna("Cancelled")
data["Doctor"]["surgeon_Type"] = data["Doctor"]["surgeon_Type"].fillna("Not a surgeon")

# ---- Feature engineering ----
# Emergency: time features
em["hour"] = em["reported_At"].dt.hour
em["day_of_week"] = em["reported_At"].dt.day_name()
em["month"] = em["reported_At"].dt.to_period("M").astype(str)
em["total_case_min"] = (em["closed_At"] - em["reported_At"]).dt.total_seconds() / 60

# Dispatch: response-time features (in minutes)
dp = data["Dispatch"].merge(em[["emergency_Id", "reported_At", "severity_Level",
                                "emergency_Type"]], on="emergency_Id", how="left")
mins = lambda a, b: (dp[a] - dp[b]).dt.total_seconds() / 60
dp["dispatch_delay_min"] = mins("assigned_At", "reported_At")      # call -> ambulance assigned
dp["response_min"]       = mins("arrived_Scene_At", "reported_At") # call -> ambulance on scene
dp["travel_to_scene_min"] = mins("arrived_Scene_At", "departed_At")
dp["to_hospital_min"]    = mins("arrived_Hospital_At", "left_Scene_At")
dp["unit_busy_min"]      = mins("completed_At", "assigned_At")     # how long ambulance was busy
dp["hour"] = dp["reported_At"].dt.hour
dp["day_of_week"] = dp["reported_At"].dt.day_name()
data["Dispatch"] = dp

# Admissions: length of stay
for t in ["BedRecords", "RoomRecords"]:
    data[t]["stay_days"] = (data[t]["discharge_Date"] - data[t]["admission_Date"]).dt.days

# Patients: age (on 2025-06-30, the end of the dataset)
p = data["Patients"]
p["age"] = ((pd.Timestamp("2025-06-30") - p["Date_Of_Birth"]).dt.days / 365.25).astype(int)
p["age_group"] = pd.cut(p["age"], [-1, 12, 18, 40, 60, 120],
                        labels=["Child (0-12)", "Teen (13-18)", "Adult (19-40)",
                                "Middle (41-60)", "Senior (60+)"])
p["city"] = p["pt_Address"].str.split(",").str[-3].str.strip()

# Alerts: how long it took to resolve
a = data["Alert"]
a["resolve_min"] = (a["resolved_At"] - a["created_At"]).dt.total_seconds() / 60

# ---- Sanity checks (should all be 0) ----
checks = {
    "Emergency closed before reported": int((em.closed_At < em.reported_At).sum()),
    "Dispatch: arrived before assigned": int((dp.arrived_Scene_At < dp.assigned_At).sum()),
    "BedRecords: discharge before admission": int((data["BedRecords"].stay_days < 0).sum()),
    "Alerts: resolved before created": int((a.resolve_min < 0).sum()),
}

# ---- Save ----
for t, df in data.items():
    df.to_csv(f"{CLEAN}/{t}.csv", index=False)

print(f"{'Table':15}{'Rows in':>9}{'Rows out':>10}{'Null cells':>12}")
for t, b, a_, n in report:
    print(f"{t:15}{b:>9}{a_:>10}{n:>12}")
print("\nSanity checks (0 = good):")
for k, v in checks.items():
    print(f"  {k}: {v}")
print(f"\nDone. Cleaned files saved in {CLEAN}/")
