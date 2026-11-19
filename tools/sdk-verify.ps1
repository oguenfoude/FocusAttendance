param(
    [string]$Dir = "D:\FocusAttendance\src\FocusAttendance.App\bin\Release\net10.0-windows10.0.19041.0",
    [string]$Ip = "192.168.1.201",
    [int]$Port = 4370
)

Set-Location $Dir
$flags = [Reflection.BindingFlags]::InvokeMethod
$t = [Type]::GetTypeFromProgID('zkemkeeper.ZKEM')
if ($null -eq $t) { 'ProgID not registered'; return }
"type: $($t.FullName)"

function New-Zk {
    $o = [Activator]::CreateInstance($t)
    return $o
}

$o = New-Zk
"instance: $($o.GetType().FullName)"

try {
    $ok = $t.InvokeMember('Connect_Net', $flags, $null, $o, @($Ip, $Port))
    "Connect_Net => $ok"
    Start-Sleep -Milliseconds 500
    $e = @([int64]0); $t.InvokeMember('GetLastError', $flags, $null, $o, $e) | Out-Null
    "GetLastError => $($e[0])"

    if ($ok) {
        $r = @($false)
        $t.InvokeMember('ReadGeneralLogData', $flags, $null, $o, @(1, $false) ) | Out-Null
        "ReadGeneralLogData invoked"

        $sn = @(''); $rc = $t.InvokeMember('SSR_GetSerialNumber', $flags, $null, $o, $sn)
        "SSR_GetSerialNumber => $rc / '$($sn[0])'"

        $nm = @(''); $rc = $t.InvokeMember('GetDeviceName', $flags, $null, $o, @(1, $nm))
        "GetDeviceName => $rc / '$($nm[0])'"

        $fw = @(''); $rc = $t.InvokeMember('GetFirmwareVersion', $flags, $null, $o, @(1, $fw))
        "GetFirmwareVersion => $rc / '$($fw[0])'"

        $tm = @(1, 0, 0, 0, 0, 0, 0)
        $rc = $t.InvokeMember('GetDeviceTime', $flags, $null, $o, $tm)
        "GetDeviceTime => $rc / $($tm -join ',')"

        $st = @(1, 0, 0, 0, 0, 0, 0, 0, 0)
        $rc = $t.InvokeMember('GetDeviceStatus', $flags, $null, $o, $st)
        "GetDeviceStatus => $rc / $($st -join ',')"

        $t.InvokeMember('Disconnect', $flags, $null, $o, @()) | Out-Null
        "Disconnected"
    }
} catch {
    "invoke error: " + $_.Exception.InnerException.Message
} finally {
    [System.Runtime.InteropServices.Marshal]::ReleaseComObject($o) | Out-Null
}
