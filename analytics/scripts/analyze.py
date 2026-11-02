"""
analyze.py  -  Step 2: Compute all analytics from the proposal and save
               result tables to data/processed/ plus a KPI summary.
Run after clean.py:  python scripts/analyze.py
"""
import os
import numpy as np
import pandas as pd

CLEAN, OUT = "data/cleaned", "data/processed"
os.makedirs(OUT, exist_ok=True)

def load(name, dates=()):
    return pd.read_csv(f"{CLEAN}/{name}.csv", parse_dates=list(dates))

em   = load("Emergency", ["reported_At", "closed_At", "discharge_Date"])
dp   = load("Dispatch", ["assigned_At", "arrived_Scene_At", "completed_At", "reported_At"])
ap   = load("Appointment", ["appointment_Date"])
doc  = load("Doctor")
dept = load("Department")
bed  = load("Bed")
br   = load("BedRecords", ["admission_Date", "discharge_Date"])
amb  = load("Ambulance")
alert = load("Alert", ["created_At", "resolved_At"])
pat  = load("Patients")

DAYS = ["Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday"]
save = lambda df, name: df.to_csv(f"{OUT}/{name}.csv")

# 1. Patient volume (monthly appointments + emergencies)
ap["month"] = ap["appointment_Date"].dt.to_period("M").astype(str)
vol = pd.DataFrame({
    "appointments": ap.groupby("month").size(),
    "emergencies":  em.groupby("month").size(),
}).fillna(0).astype(int)
save(vol, "1_monthly_patient_volume")

# 2. Department workload (appointments per department)
w = ap.merge(doc[["doct_Id", "dept_Id"]], on="doct_Id").merge(dept, on="dept_Id")
workload = w.groupby("dept_Name").agg(appointments=("appointment_Id", "count"),
                                      revenue=("payment_amount", "sum")
                                      ).sort_values("appointments", ascending=False)
save(workload, "2_department_workload")

# 3. Emergency frequency & severity
etype = em.groupby("emergency_Type").agg(cases=("emergency_Id", "count"),
                                         avg_severity=("severity_Level", "mean")
                                         ).sort_values("cases", ascending=False).round(2)
sev = em["severity_Level"].value_counts().sort_index()
save(etype, "3a_emergency_by_type")
save(sev.to_frame("cases"), "3b_severity_counts")

# 4. Response-time distribution (minutes)
resp = dp["response_min"]
resp_stats = resp.describe(percentiles=[.5, .9, .95]).round(2)
resp_by_sev = dp.groupby("severity_Level")["response_min"].agg(["count", "mean", "median"]).round(1)
save(resp_stats.to_frame("response_min"), "4a_response_time_stats")
save(resp_by_sev, "4b_response_by_severity")

# 5. Bed occupancy (daily)
days = pd.date_range(br["admission_Date"].min(), br["discharge_Date"].max())
occ = pd.Series({d: ((br.admission_Date <= d) & (br.discharge_Date >= d)).sum() for d in days})
occupancy = pd.DataFrame({"occupied_beds": occ, "occupancy_pct": (occ / len(bed) * 100).round(2)})
save(occupancy, "5_bed_occupancy_daily")

# 6. Ambulance utilization
period_min = (em["reported_At"].max() - em["reported_At"].min()).total_seconds() / 60
util = dp.groupby("unit_Id").agg(dispatches=("dispatch_Id", "count"),
                                 busy_min=("unit_busy_min", "sum") if "unit_busy_min" in dp else ("dispatch_Id", "count"),
                                 avg_response_min=("response_min", "mean")).round(1)
util = amb.set_index("unit_Id")[["unit_Code", "unit_Type"]].join(util)
util["utilization_pct"] = (util["busy_min"] / period_min * 100).round(2)
save(util, "6_ambulance_utilization")

# 7. Peak hours / days (emergencies)
heat = em.pivot_table(index="day_of_week", columns="hour", values="emergency_Id",
                      aggfunc="count", fill_value=0).reindex(DAYS)
by_hour = em.groupby("hour").size()
save(heat, "7a_peak_heatmap")
save(by_hour.to_frame("emergencies"), "7b_by_hour")

# 8. Historical trend: monthly revenue (appointments + bed stays)
rev = pd.DataFrame({
    "appointment_revenue": ap.groupby("month")["payment_amount"].sum(),
    "bed_revenue": br.groupby(br["admission_Date"].dt.to_period("M").astype(str))["amount"].sum(),
}).fillna(0).astype(int)
save(rev, "8_monthly_revenue")

# 9. KPIs
dispo = em["disposition"].value_counts(normalize=True) * 100
kpi = {
    "Total emergencies": len(em),
    "Ambulance cases (%)": round((em.arrival_Mode == "Ambulance").mean() * 100, 1),
    "Critical cases - severity 1-2 (%)": round((em.severity_Level <= 2).mean() * 100, 1),
    "Avg response time (min)": round(resp.mean(), 1),
    "Median response time (min)": round(resp.median(), 1),
    "90th percentile response (min)": round(resp.quantile(.9), 1),
    "Responses over 30 min (%)": round((resp > 30).mean() * 100, 1),
    "Avg bed occupancy (%)": round(occupancy.occupancy_pct.mean(), 1),
    "Peak bed occupancy (%)": round(occupancy.occupancy_pct.max(), 1),
    "Avg bed stay (days)": round(br["stay_days"].mean(), 1),
    "Busiest emergency hour": int(by_hour.idxmax()),
    "Busiest day": heat.sum(axis=1).idxmax(),
    "Most common emergency": etype.index[0],
    "Admitted from emergency (%)": round(dispo.filter(like="Admitted").sum(), 1),
    "Appointment no-show rate (%)": round((ap.appointment_status == "No-Show").mean() * 100, 1),
    "Alerts raised": len(alert),
    "Avg alert resolution (min)": round(alert["resolve_min"].mean(), 1),
    "Total patients": len(pat),
}
kpi = pd.Series(kpi, name="value")
kpi.to_csv(f"{OUT}/9_KPIs.csv", header=True)

print("========== KPI SUMMARY ==========")
print(kpi.to_string())
print("\nTop 5 departments by appointments:\n", workload.head(5)[["appointments"]].to_string())
print("\nAmbulance utilization:\n", util.to_string())
print(f"\nAll result tables saved in {OUT}/")
