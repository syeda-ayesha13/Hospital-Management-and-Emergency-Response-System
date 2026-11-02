"""
load_data.py  -  Fill the HMERS SQL Server database from the Excel dataset.

Steps:
  1. Run schema.sql in SSMS first (creates the empty tables).
  2. pip install pyodbc pandas openpyxl
  3. Edit SERVER below if needed, then run:
         python database/load_data.py                 (loads into SQL Server)
         python database/load_data.py --dry-run       (only checks the data, no database)
         python database/load_data.py path/to/file.xlsx
"""
import sys
import datetime as dt
import numpy as np
import pandas as pd

# ------------------------- SETTINGS (edit these) -------------------------
SERVER   = r"localhost\SQLEXPRESS01"  # SSMS login screen shows your server name
DATABASE = "HMERS"
EXCEL    = "Hospital_Management_System.xlsx"
USE_WINDOWS_AUTH = True              # False -> fill USER and PASSWORD below
USER, PASSWORD = "", ""
FAST = True                          # if you get a weird type error, set to False
# --------------------------------------------------------------------------

# Parents must be loaded before children (foreign keys)
LOAD_ORDER = ["Department", "Location", "Patients", "Doctor", "Nurse", "Helpers",
              "Ward", "Bed", "Room", "Ambulance", "Road", "Appointment",
              "MedicalRecord", "BedRecords", "RoomRecords", "StaffShift",
              "SurgeryRecord", "Emergency", "Dispatch", "Alert", "StatusHistory"]

TEXT_COLS = {("Patients", "contact_No"), ("Doctor", "contact_No"),
             ("Nurse", "contact_No"), ("Helpers", "contact_No")}


def convert_column(table, col, s):
    """Turn one pandas column into plain Python values SQL Server accepts."""
    if (table, col) in TEXT_COLS:
        return [None if pd.isna(v) else str(v).strip() for v in s]

    if pd.api.types.is_datetime64_any_dtype(s):
        valid = s.dropna()
        date_only = (valid == valid.dt.normalize()).all()   # all midnight -> DATE
        out = []
        for v in s:
            if pd.isna(v):
                out.append(None)
            else:
                out.append(v.date() if date_only else v.to_pydatetime())
        return out

    out = []
    for v in s:
        if v is None or (not isinstance(v, (str, dt.time)) and pd.isna(v)):
            out.append(None)
        elif isinstance(v, (np.integer,)):
            out.append(int(v))
        elif isinstance(v, (np.floating, float)):
            out.append(int(v) if float(v).is_integer() else float(v))
        elif isinstance(v, str):
            out.append(v.strip())
        else:
            out.append(v)            # datetime.time etc.
    return out


def prepare(table, df):
    cols = {c: convert_column(table, c, df[c]) for c in df.columns}
    return list(df.columns), list(zip(*cols.values()))


DRIVER_PREFERENCE = ["ODBC Driver 18 for SQL Server", "ODBC Driver 17 for SQL Server",
                     "ODBC Driver 13 for SQL Server", "ODBC Driver 11 for SQL Server",
                     "SQL Server Native Client 11.0", "SQL Server"]   # "SQL Server" = oldest, last choice


def connect():
    import pyodbc
    installed = pyodbc.drivers()
    driver = next((d for d in DRIVER_PREFERENCE if d in installed), None)
    if driver is None:
        sys.exit("No SQL Server ODBC driver found. Install 'ODBC Driver 17 for SQL Server'.")
    auth = "Trusted_Connection=yes;" if USE_WINDOWS_AUTH else f"UID={USER};PWD={PASSWORD};"
    cs = f"DRIVER={{{driver}}};SERVER={SERVER};DATABASE={DATABASE};{auth}TrustServerCertificate=yes;"
    print("Using driver:", driver)
    if driver == "SQL Server":
        print("  (old driver: slower but works. For speed install 'ODBC Driver 17 for SQL Server')")
    return pyodbc.connect(cs), driver


def stringify(rows):
    """Send dates/times as ISO text. SQL Server converts them itself, and this
    avoids 'string data, right truncation' errors seen with older ODBC drivers."""
    def f(v):
        if isinstance(v, dt.datetime):
            return v.strftime("%Y-%m-%d %H:%M:%S")
        if isinstance(v, dt.date):
            return v.isoformat()
        if isinstance(v, dt.time):
            return v.strftime("%H:%M:%S")
        return v
    return [tuple(f(v) for v in r) for r in rows]


def main():
    args = [a for a in sys.argv[1:] if not a.startswith("--")]
    dry = "--dry-run" in sys.argv
    xl = pd.ExcelFile(args[0] if args else EXCEL)

    data = {}
    for t in LOAD_ORDER:
        data[t] = prepare(t, xl.parse(t))
    print(f"Read {len(data)} sheets from Excel.")

    if dry:
        for t in LOAD_ORDER:
            cols, rows = data[t]
            print(f"  {t:15} {len(rows):>6} rows  {len(cols):>2} columns")
        print("Dry run finished. Nothing was written.")
        return

    conn, driver = connect()
    cur = conn.cursor()
    cur.fast_executemany = FAST and driver != "SQL Server"   # old driver cannot do fast mode
    try:
        for t in LOAD_ORDER:
            cols, rows = data[t]
            sql = f"INSERT INTO {t} ({', '.join(cols)}) VALUES ({', '.join('?' * len(cols))})"
            cur.executemany(sql, stringify(rows))
            print(f"  loaded {t:15} {len(rows):>6} rows")
        conn.commit()
    except Exception:
        conn.rollback()          # all-or-nothing: no half-filled database
        print("\nERROR - nothing was saved. Check the message below.\n")
        raise

    print("\nVerifying row counts in the database:")
    for t in LOAD_ORDER:
        n = cur.execute(f"SELECT COUNT(*) FROM {t}").fetchone()[0]
        status = "OK" if n == len(data[t][1]) else "MISMATCH"
        print(f"  {t:15} {n:>6}  {status}")
    conn.close()
    print("\nDone. Database is ready.")


if __name__ == "__main__":
    main()