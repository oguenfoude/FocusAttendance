import io
import re

# ---------- scanner code-behind: empty instead of dashes ----------
p = r"D:\FocusAttendance\src\FocusAttendance.App\Views\LanScannerWindow.xaml.cs"
code = io.open(p, encoding="utf-8").read()

code = re.sub(r'public string Serial => Device\.SerialNumber is \{ Length: > 0 \} sn \? sn : "-";',
              'public string Serial => Device.SerialNumber ?? string.Empty;', code)
code = re.sub(r'public string Firmware => Device\.Snapshot\?\.FirmwareVersion is \{ Length: > 0 \} fw \? fw : "-";',
              'public string Firmware => Device.Snapshot?.FirmwareVersion ?? string.Empty;', code)
code = re.sub(r'public string Mac => Device\.Snapshot\?\.MacAddress is \{ Length: > 0 \} mac \? mac : "-";',
              'public string Mac => Device.Snapshot?.MacAddress ?? string.Empty;', code)
code = re.sub(r'public string Platform => Device\.Snapshot\?\.Platform is \{ Length: > 0 \} platform \? platform : "-";',
              'public string Platform => Device.Snapshot?.Platform ?? string.Empty;', code)
code = re.sub(r'public string Biometrics => Device\.Snapshot is \{ \} s \? \$"\{s\.FingerCount\} FP / \{s\.FaceCount\} face" : "-";',
              'public string Biometrics => Device.Snapshot is { } s && (s.UserCount + s.FingerCount + s.FaceCount) > 0\n'
              '        ? $"{s.UserCount} users / {s.FingerCount} FP / {s.FaceCount} face"\n'
              '        : string.Empty;', code)
code = re.sub(r'public string Clock => Device\.Snapshot\?\.DeviceClock is \{ \} clock \? clock\.ToString\("dd/MM/yyyy HH:mm"\) : "-";',
              'public string Clock => Device.Snapshot?.DeviceClock?.ToString("dd/MM/yyyy HH:mm") ?? string.Empty;', code)
code = re.sub(r'public string Users => Device\.Snapshot\?\.UserCount\.ToString\(\) \?\? "-";',
              'public string Users => Device.Snapshot is { } us ? us.UserCount.ToString() : string.Empty;', code)
code = re.sub(r'public string Punches => Device\.Snapshot\?\.PunchCount\.ToString\(\) \?\? "-";',
              'public string Punches => Device.Snapshot is { } ps ? ps.PunchCount.ToString() : string.Empty;', code)
code = re.sub(r'public string Model => Device\.DisplayModel;',
              'public string Model => Device.DisplayModel;', code)

io.open(p, "w", encoding="utf-8", newline="").write(code)
print("scanner code patched")

# ---------- scanner XAML: hide empty detail fields ----------
p = r"D:\FocusAttendance\src\FocusAttendance.App\Views\LanScannerWindow.xaml"
xaml = io.open(p, encoding="utf-8").read()

start = xaml.find("<!-- Everything the terminal answered")
end = xaml.find('TextBlock Grid.Row="5"', start)
if start < 0 or end < 0:
    raise SystemExit("scanner detail markers not found")

region = xaml[start:end]


def hide_scan(match):
    tag, body = match.group(0), match.group(0)
    prop = re.search(r'\{Binding Selected(\w+)\}', body)
    if not prop:
        return tag
    name = prop.group(1)
    return tag.replace('<StackPanel Margin="0,0,22,8"',
                       f'<StackPanel Visibility="{{Binding Selected{name}, Converter={{StaticResource StringEmptyToVisibility}}}}" Margin="0,0,22,8"', 1)


new_region = re.sub(r'<StackPanel Margin="0,0,22,8".*?</StackPanel>', hide_scan, region, flags=re.S)
xaml = xaml[:start] + new_region + xaml[end:]

io.open(p, "w", encoding="utf-8", newline="").write(xaml)
print("scanner xaml patched:", new_region.count('StringEmptyToVisibility'))

# ---------- devices page: hide empty detail fields ----------
p = r"D:\FocusAttendance\src\FocusAttendance.App\Views\DevicesPage.xaml"
xaml = io.open(p, encoding="utf-8").read()

start = xaml.find("<!-- Full identity of the selected terminal -->")
end = xaml.find('TextBlock Grid.Row="3"', start)
if start < 0 or end < 0:
    raise SystemExit("devices detail markers not found")

region = xaml[start:end]


def hide_device(match):
    tag = match.group(0)
    prop = re.search(r'\{Binding SelectedDevice\.(\w+)\}', tag)
    if not prop:
        return tag
    name = prop.group(1)
    return tag.replace('<StackPanel Grid.Column="',
                       f'<StackPanel Visibility="{{Binding SelectedDevice.{name}, Converter={{StaticResource StringEmptyToVisibility}}}}" Grid.Column="', 1)


new_region = re.sub(r'<StackPanel Grid\.Column="\d">.*?</StackPanel>', hide_device, region, flags=re.S)
xaml = xaml[:start] + new_region + xaml[end:]

io.open(p, "w", encoding="utf-8", newline="").write(xaml)
print("devices xaml patched:", new_region.count('StringEmptyToVisibility'))
