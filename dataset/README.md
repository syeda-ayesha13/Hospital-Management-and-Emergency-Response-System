HMERS dataset has 21 related tables and 27,042 rows, available in both the Excel workbook and the SQL Server script.
It models one Pakistani hospital with its patients and staff, plus a year of emergency response.

# Hospital data (14 tables, 13,543 rows): 
departments, doctors, nurses, helpers, patients, appointments, beds, admissions, medical records, staff shifts and surgeries. Patients live across Punjab, and most are in the Lahore area.
# Emergency-response data (7 tables, 13,499 rows): 
This is sythetic data organized in a way from different datasets to get summed up with hospital data. It covers 1,169 emergencies from Jul 2024 to Jun 2025, the 700 ambulance dispatches among them, a road network, status history and alerts.
# Nature of the data: 
 Th data has been collected from kaggle and linkedIn, but it is fitted to match the standards of real-time data.

## Dataset Attributes

| Group | Table | Rows | What it holds |
|---|---|---:|---|
| People | Patients | 1,500 | 790 F / 710 M, address with city |
| People | Doctor | 400 | 31 departments, 38 surgeons |
| People | Nurse / Helpers | 500 / 1,100 | Staff by department |
| Structure | Department | 31 | Cardiology, Neurology, Emergency Medicine, Critical Care, etc. |
| Structure | Ward / Room / Bed | 63 / 391 / 500 | 6 room types, including 33 Emergency Rooms and 30 Operation Theatres |
| Activity | Appointment | 1,000 | Jul 2024–Jun 2025 |
| Activity | BedRecords / RoomRecords | 1,000 each | Admissions, Nov 2024–May 2025 |
| Activity | MedicalRecord | 3,000 | Vitals and diagnosis, 25 diagnoses |
| Activity | StaffShift | 2,058 | Only 19 May–2 Jun 2025 |
| Activity | SurgeryRecord | 1,000 | 38 surgery types, 2022–2025 |
| Emergency | Location / Road | 61 / 85 | Hospital plus about 60 cities; roads between them |
| Emergency | Ambulance | 6 | 3 BLS/ALS/Mobile ICU units, with bases |
| Emergency | Emergency | 1,169 | One row per case |
| Emergency | Dispatch | 700 | Full ambulance timeline and routes |
| Emergency | StatusHistory | 11,218 | Every status change |
| Emergency | Alert | 260 | Critical cases, delays, shortages |

## How the tables link
Patients connect to Emergency, Appointment, MedicalRecord and the admission tables. Emergency links to Dispatch, Location, Doctor, Ward and Bed. Dispatch links to Ambulance, Nurse and Helpers, and Road connects Locations as a graph for Dijkstra and BFS/DFS.
