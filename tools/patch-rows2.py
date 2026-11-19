import io
import re

path = r"D:\FocusAttendance\src\FocusAttendance.Application\ViewModels\Rows.cs"
text = io.open(path, encoding="utf-8").read()

replacements = [
    (r'public string Serial => string\.IsNullOrWhiteSpace\(Model\.SerialNumber\) \? "[^"]*" : Model\.SerialNumber!;',
     'public string Serial => Model.SerialNumber ?? string.Empty;'),
    (r'public string Firmware => string\.IsNullOrWhiteSpace\(Model\.FirmwareVersion\) \? "[^"]*" : Model\.FirmwareVersion!;',
     'public string Firmware => Model.FirmwareVersion ?? string.Empty;'),
    (r'public string Platform => Model\.Platform \?\? "[^"]*";',
     'public string Platform => Model.Platform ?? string.Empty;'),
    (r'public string Mac => Model\.MacAddress \?\? "[^"]*";',
     'public string Mac => Model.MacAddress ?? string.Empty;'),
    (r'public string ClockText => \(Status\.Snapshot\?\.DeviceClock \?\? Model\.DeviceClock\)\?\.ToString\("dd/MM/yyyy HH:mm"\) \?\? "[^"]*";',
     'public string ClockText => (Status.Snapshot?.DeviceClock ?? Model.DeviceClock)?.ToString("dd/MM/yyyy HH:mm") ?? string.Empty;'),
]

for pattern, repl in replacements:
    text, count = re.subn(pattern, repl, text)
    print(count, repl[:48])

io.open(path, "w", encoding="utf-8", newline="").write(text)
print("done")
