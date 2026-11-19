param([string]$Dir = "D:\FocusAttendance\src\FocusAttendance.App\sdk64", [string]$Ip = "192.168.1.201", [int]$Port = 4370)

Set-Location $Dir
$clsid = [Guid]'00853A19-BD51-419B-9269-2DABE57EB61F'
try {
    $t = [Type]::GetTypeFromCLSID($clsid)
    $o = [Activator]::CreateInstance($t)
    "COM (CLSID) OK: " + $o.GetType().FullName
} catch {
    "COM create failed: " + $_.Exception.Message
    return
}

$flags = [Reflection.BindingFlags]::InvokeMethod
try {
    $ok = $t.InvokeMember('Connect_Net', $flags, $null, $o, @($Ip, $Port))
    "Connect_Net($Ip,$Port)=$ok"
    if ($ok) {
        $argsSn = @('')
        $r = $t.InvokeMember('SSR_GetSerialNumber', $flags, $null, $o, $argsSn)
        "serial ok=$r sn=$($argsSn[0])"
        $m = @('')
        $rm = $t.InvokeMember('GetDeviceName', $flags, $null, $o, @(1, $m)) 
        "GetDeviceName returned=$rm"
        $t.InvokeMember('Disconnect', $flags, $null, $o, @()) | Out-Null
    }
} catch {
    "invoke error: " + $_.Exception.InnerException.Message
}
[System.Runtime.InteropServices.Marshal]::ReleaseComObject($o) | Out-Null
