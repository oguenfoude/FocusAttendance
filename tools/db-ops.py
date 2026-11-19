import os
import sqlite3

db = os.path.join(os.environ["LOCALAPPDATA"], "FocusAttendance", "focus-attendance.db")
conn = sqlite3.connect(db)
conn.row_factory = sqlite3.Row

print("devices:", conn.execute("select count(*) from Devices").fetchone()[0])
print("employees:", conn.execute("select count(*) from Employees").fetchone()[0])
print("punches:", conn.execute("select count(*) from Punches").fetchone()[0])
print()
for r in conn.execute("select * from HardwareOperationLogs order by rowid desc limit 6"):
    row = {k: (v[:160] if isinstance(v, str) else v) for k, v in dict(r).items()}
    print(row)
