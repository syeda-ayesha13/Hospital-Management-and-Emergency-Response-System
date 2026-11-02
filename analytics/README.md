# HMERS - Data Science & Analytics Layer

Python analytics for the Hospital Management and Emergency Response System.

## Folder structure
```
analytics/
  Hospital_Management_System.xlsx   <- original dataset (do not edit)
  scripts/
    clean.py       Step 1: clean data + create features
    analyze.py     Step 2: compute analytics + KPIs
    visualize.py   Step 3: create charts
  data/
    raw/           CSVs split from the Excel file
    cleaned/       cleaned CSVs (output of clean.py)
    processed/     result tables + KPIs (output of analyze.py)
  reports/charts/  PNG charts (output of visualize.py)
```

## How to run
```
pip install -r requirements.txt
python split_excel.py          # Excel -> data/raw/*.csv  (only once)
python scripts/clean.py
python scripts/analyze.py
python scripts/visualize.py
```
Always run from the project root folder.

## Analytics covered (from proposal)
Patient volume, department workload, emergency frequency & severity,
response-time distribution, bed occupancy, ambulance utilization,
peak hours/days, historical trends, operational KPIs.
