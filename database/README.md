# HMERS Database (SQL Server)

| File | Purpose |
|---|---|
| `schema.sql` | Creates database `HMERS`, 21 tables with primary keys, foreign keys, CHECK constraints, indexes, plus an empty `AppUser` table for login/roles |
| `load_data.py` | Loads `Hospital_Management_System.xlsx` into the tables (all-or-nothing) |
| `check_queries.sql` | Sample queries to confirm everything works |

## Run order
1. SSMS: open `schema.sql` -> Execute (F5)
2. `pip install pyodbc pandas openpyxl`
3. Put the Excel file in the project root, edit `SERVER` in `load_data.py`
4. `python database/load_data.py --dry-run`   (check only)
5. `python database/load_data.py`             (real load)
6. SSMS: open `check_queries.sql` -> Execute

## Design notes
- 44 foreign keys; the dataset has zero orphan rows, so all constraints pass.
- `StatusHistory.entity_Id` points to Emergency OR Ambulance, so it has no FK.
- NULLs that are intentional: walk-in emergencies have no ambulance unit,
  cancelled emergencies have no disposition, non-admitted ones have no bed.
- `AppUser` is not in the dataset; the C# backend fills it (store password HASHES).
