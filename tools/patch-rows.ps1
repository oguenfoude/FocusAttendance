$p = 'D:\FocusAttendance\src\FocusAttendance.Application\ViewModels\Rows.cs'
$t = [System.IO.File]::ReadAllText($p)

$map = @{
    'public string Serial => string.IsNullOrWhiteSpace(Model.SerialNumber) ? "-" : Model.SerialNumber!;' = 'public string Serial => Model.SerialNumber ?? string.Empty;'
    'public string ModelName => Model.ModelName ?? "ZKTeco";'                                                 = 'public string ModelName => Model.ModelName ?? string.Empty;'
    'public string Firmware => string.IsNullOrWhiteSpace(Model.FirmwareVersion) ? "-" : Model.FirmwareVersion!;' = 'public string Firmware => Model.FirmwareVersion ?? string.Empty;'
    'public string Platform => Model.Platform ?? "-";'                                                       = 'public string Platform => Model.Platform ?? string.Empty;'
    'public string Mac => Model.MacAddress ?? "-";'                                                          = 'public string Mac => Model.MacAddress ?? string.Empty;'
    'public string ClockText => (Status.Snapshot?.DeviceClock ?? Model.DeviceClock)?.ToString("dd/MM/yyyy HH:mm") ?? "—";' = 'public string ClockText => (Status.Snapshot?.DeviceClock ?? Model.DeviceClock)?.ToString("dd/MM/yyyy HH:mm") ?? string.Empty;'
}

foreach ($key in $map.Keys) {
    if (-not $t.Contains($key)) { Write-Output ("MISS: " + $key) }
    $t = $t.Replace($key, $map[$key])
}

$old = @'
    public string BiometricsText =>
        $"{Model.UserCount} / {Model.FingerCount} FP / {Model.FaceCount} face";
'@
$new = @'
    public string BiometricsText => Model.UserCount + Model.FingerCount + Model.FaceCount > 0
        ? $"{Model.UserCount} users / {Model.FingerCount} FP / {Model.FaceCount} face"
        : string.Empty;
'@

if ($t.Contains($old)) { $t = $t.Replace($old, $new) } else { Write-Output 'MISS: biometrics' }

[System.IO.File]::WriteAllText($p, $t, [System.Text.UTF8Encoding]::new($false))
'rows patched'
