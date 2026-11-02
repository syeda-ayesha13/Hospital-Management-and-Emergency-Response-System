/* Run in SSMS after load_data.py to confirm the database works */
USE HMERS;

-- 1. Row counts of the main tables
SELECT 'Patients' AS tbl, COUNT(*) AS n FROM Patients UNION ALL
SELECT 'Emergency', COUNT(*) FROM Emergency UNION ALL
SELECT 'Dispatch',  COUNT(*) FROM Dispatch  UNION ALL
SELECT 'BedRecords',COUNT(*) FROM BedRecords;

-- 2. Most urgent emergencies (what the C# priority queue will handle)
SELECT TOP 10 emergency_Id, emergency_Type, severity_Level, priority_Score, reported_At
FROM Emergency
ORDER BY severity_Level ASC, priority_Score DESC;

-- 3. Average ambulance response time (minutes) per severity
SELECT e.severity_Level,
       COUNT(*) AS dispatches,
       AVG(DATEDIFF(SECOND, e.reported_At, d.arrived_Scene_At) / 60.0) AS avg_response_min
FROM Dispatch d JOIN Emergency e ON e.emergency_Id = d.emergency_Id
GROUP BY e.severity_Level
ORDER BY e.severity_Level;

-- 4. Appointments per department
SELECT dp.dept_Name, COUNT(*) AS appointments
FROM Appointment a
JOIN Doctor doc ON doc.doct_Id = a.doct_Id
JOIN Department dp ON dp.dept_Id = doc.dept_Id
GROUP BY dp.dept_Name
ORDER BY appointments DESC;

-- 5. Road graph size (for Dijkstra)
SELECT (SELECT COUNT(*) FROM Location) AS nodes, (SELECT COUNT(*) FROM Road) AS edges;
