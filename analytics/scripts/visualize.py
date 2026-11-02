"""
visualize.py  -  Step 3: Create all charts and save as PNG in reports/charts/
Run after analyze.py:  python scripts/visualize.py
"""
import os
import matplotlib
matplotlib.use("Agg")            # draws to files, no window needed
import matplotlib.pyplot as plt
import numpy as np
import pandas as pd

P, OUT = "data/processed", "reports/charts"
os.makedirs(OUT, exist_ok=True)
plt.rcParams.update({"figure.dpi": 110, "axes.spines.top": False,
                     "axes.spines.right": False, "font.size": 10})
BLUE, RED, GREEN = "#2563eb", "#dc2626", "#16a34a"

rd = lambda f, **k: pd.read_csv(f"{P}/{f}.csv", index_col=0, **k)
def done(fig, name):
    fig.tight_layout(); fig.savefig(f"{OUT}/{name}.png"); plt.close(fig); print("saved", name)

# 1. Monthly patient volume
v = rd("1_monthly_patient_volume")
fig, ax = plt.subplots(figsize=(9, 4.5))
ax.plot(v.index, v.appointments, marker="o", color=BLUE, label="Appointments")
ax.plot(v.index, v.emergencies, marker="s", color=RED, label="Emergencies")
ax.set(title="Monthly Patient Volume", xlabel="Month", ylabel="Count")
ax.tick_params(axis="x", rotation=45); ax.legend()
done(fig, "01_monthly_patient_volume")

# 2. Department workload (top 10)
w = rd("2_department_workload").head(10)[::-1]
fig, ax = plt.subplots(figsize=(8, 5))
ax.barh(w.index, w.appointments, color=BLUE)
for i, val in enumerate(w.appointments): ax.text(val + 3, i, val, va="center")
ax.set(title="Department Workload - Top 10 (Appointments)", xlabel="Appointments")
done(fig, "02_department_workload")

# 3. Emergency types
t = rd("3a_emergency_by_type")[::-1]
fig, ax = plt.subplots(figsize=(8.5, 5.5))
ax.barh(t.index, t.cases, color=RED)
for i, val in enumerate(t.cases): ax.text(val + 2, i, val, va="center")
ax.set(title="Emergency Cases by Type", xlabel="Cases")
done(fig, "03_emergency_types")

# 4. Severity pie
s = rd("3b_severity_counts")
labels = [f"Level {i}" + (" (most critical)" if i == 1 else " (least critical)" if i == 5 else "") for i in s.index]
fig, ax = plt.subplots(figsize=(6.5, 6))
ax.pie(s.cases, labels=labels, autopct="%1.1f%%", startangle=90,
       colors=["#7f1d1d", "#dc2626", "#f59e0b", "#84cc16", "#16a34a"])
ax.set_title("Emergency Severity Distribution")
done(fig, "04_severity_pie")

# 5. Response-time histogram
dp = pd.read_csv("data/cleaned/Dispatch.csv")
r = dp["response_min"]
fig, ax = plt.subplots(figsize=(8.5, 4.5))
ax.hist(r, bins=30, color=BLUE, edgecolor="white")
ax.axvline(r.mean(), color=RED, ls="--", label=f"Mean = {r.mean():.1f} min")
ax.axvline(r.median(), color=GREEN, ls="--", label=f"Median = {r.median():.1f} min")
ax.set(title="Ambulance Response-Time Distribution", xlabel="Minutes (call to arrival at scene)", ylabel="Dispatches")
ax.legend()
done(fig, "05_response_time_hist")

# 6. Response time by severity
rs = rd("4b_response_by_severity")
fig, ax = plt.subplots(figsize=(7, 4.5))
ax.bar([f"Level {i}" for i in rs.index], rs["mean"], color=BLUE)
for i, val in enumerate(rs["mean"]): ax.text(i, val + 0.5, f"{val:.1f}", ha="center")
ax.set(title="Average Response Time by Severity", ylabel="Minutes")
done(fig, "06_response_by_severity")

# 7. Peak heatmap (day x hour)
h = rd("7a_peak_heatmap")
fig, ax = plt.subplots(figsize=(11, 4.5))
im = ax.imshow(h.values, aspect="auto", cmap="YlOrRd")
ax.set_xticks(range(len(h.columns))); ax.set_xticklabels(h.columns)
ax.set_yticks(range(len(h.index))); ax.set_yticklabels(h.index)
ax.set(title="Emergency Peak Times (Day vs Hour)", xlabel="Hour of day")
fig.colorbar(im, ax=ax, label="Emergencies")
done(fig, "07_peak_heatmap")

# 8. Bed occupancy over time
o = rd("5_bed_occupancy_daily"); o.index = pd.to_datetime(o.index)
fig, ax = plt.subplots(figsize=(9.5, 4.5))
ax.plot(o.index, o.occupancy_pct, color=BLUE, lw=0.8, alpha=0.5, label="Daily")
ax.plot(o.index, o.occupancy_pct.rolling(7, min_periods=1).mean(), color=RED, lw=2, label="7-day average")
ax.set(title="Bed Occupancy Over Time", ylabel="Occupancy (%)", xlabel="Date"); ax.legend()
done(fig, "08_bed_occupancy")

# 9. Ambulance utilization
u = rd("6_ambulance_utilization")
fig, ax = plt.subplots(1, 2, figsize=(11, 4.5))
lab = u.unit_Code + "\n" + u.unit_Type
ax[0].bar(lab, u.dispatches, color=BLUE); ax[0].set(title="Dispatches per Ambulance", ylabel="Dispatches")
ax[1].bar(lab, u.utilization_pct, color=GREEN); ax[1].set(title="Ambulance Utilization (% of time busy)", ylabel="%")
done(fig, "09_ambulance_utilization")

# 10. Hourly emergencies
bh = rd("7b_by_hour")
fig, ax = plt.subplots(figsize=(9, 4.5))
ax.bar(bh.index, bh.emergencies, color=np.where(bh.emergencies == bh.emergencies.max(), RED, BLUE))
ax.set(title="Emergencies by Hour of Day (red = peak)", xlabel="Hour", ylabel="Emergencies")
done(fig, "10_emergencies_by_hour")

# 11. Revenue trend
rv = rd("8_monthly_revenue")
fig, ax = plt.subplots(figsize=(9, 4.5))
ax.plot(rv.index, rv.appointment_revenue / 1e6, marker="o", color=BLUE, label="Appointments")
ax.plot(rv.index, rv.bed_revenue / 1e6, marker="s", color=GREEN, label="Bed admissions")
ax.set(title="Monthly Revenue Trend", ylabel="Revenue (millions)"); ax.tick_params(axis="x", rotation=45); ax.legend()
done(fig, "11_revenue_trend")

print(f"\nAll charts saved in {OUT}/")
