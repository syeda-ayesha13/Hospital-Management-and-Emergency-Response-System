import pandas as pd, os

os.makedirs("data/raw", exist_ok=True)
xl = pd.ExcelFile("Hospital_Management_System.xlsx")
for s in xl.sheet_names:
    xl.parse(s).to_csv(f"data/raw/{s}.csv", index=False)
    print("saved:", s)
