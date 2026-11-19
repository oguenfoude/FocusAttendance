import os
import sqlite3

db = os.path.join(os.environ["LOCALAPPDATA"], "FocusAttendance", "focus-attendance.db")
conn = sqlite3.connect(db)
conn.row_factory = sqlite3.Row

tables = [r[0] for r in conn.execute("select name from sqlite_master where type='table' order by name")]
print("tables:", ", ".join(tables))

for table in ("Devices", "HardwareOperationLogs"):
    if table in tables:
        print()
        print("==", table)
        for row in conn.execute(f'select * from "{table}"'):
            print(dict(row))
