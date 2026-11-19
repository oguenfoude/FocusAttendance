param(
    [string]$Dir = "D:\FocusAttendance\src\FocusAttendance.App\sdk64",
    [string[]]$Targets = @("192.168.1.201", "192.168.1.202"),
    [int]$Port = 4370
)

Set-Location $Dir
$clsid = [Guid]'00853A19-BD51-419B-9269-2DABE57EB61F'
$t = [Type]::GetTypeFromCLSID($clsid)
$flags = [Reflection.BindingFlags]::InvokeMethod

foreach ($ip in $Targets) {
    $o = [Activator]::CreateInstance($t)
    try {
        $ok = $t.InvokeMember('Connect_Net', $flags, $null, $o, @($ip, $Port))
        $errArgs = @(0)
        $t.InvokeMember('GetLastError', $flags, $null, $o, $errArgs) | Out-Null
        "$ip`:$Port connect=$ok lastError=$($errArgs[0])"
        if ($ok) {
            $sn = @('')
            $t.InvokeMember('SSR_GetSerialNumber', $flags, $null, $o, $sn) | Out-Null
            "   serial=$($sn[0])"
            $name = @('')
            $t.InvokeMember('GetDeviceName', $flags, $null, $o, @(1, $name)) | Out-Null
            "   name=$($name[0])"
            $fw = @('')
            $t.InvokeMember('GetFirmwareVersion', $flags, $null, $o, @(1, $fw)) | Out-Null
            "   firmware=$($fw[0])"
            $t.InvokeMember('Disconnect', $flags, $null, $o, @()) | Out-Null
        }
    } catch {
        "$ip -> error: " + $_.Exception.InnerException.Message
    }
    [System.Runtime.InteropServices.Marshal]::ReleaseComObject($o) | Out-Null
}

# ProgID path (what the app uses)
try {
    $pt = [Type]::GetTypeFromProgID('zkemkeeper.ZKEM')
    $po = [Activator]::CreateInstance($pt)
    "ProgID create OK"
    [System.Runtime.InteropServices.Marshal]::ReleaseComObject($po) | Out-Null
} catch {
    "ProgID create failed: " + $_.Exception.Message
}
