import io
import re

path = r"D:\FocusAttendance\src\FocusAttendance.Application\ViewModels\Rows.cs"
text = io.open(path, encoding="utf-8").read()

for line in text.splitlines():
    if any(k in line for k in ("public string Serial =>", "public string ModelName =>", "public string Firmware =>",
                                "public string Platform =>", "public string Mac =>", "public string ClockText =>")):
        print(repr(line))
