param([string]$Dir = "D:\FocusAttendance\src\FocusAttendance.App\sdk64")

Add-Type -Namespace Zk -Name Native -MemberDefinition @'
[DllImport("kernel32.dll", SetLastError=true, CharSet=CharSet.Unicode)]
public static extern IntPtr LoadLibraryEx(string path, IntPtr h, uint flags);
[DllImport("oleaut32.dll", PreserveSig=false)]
public static extern void LoadTypeLibEx(string path, int regKind, out IntPtr typeLib);
[DllImport("oleaut32.dll")]
public static extern int RegisterTypeLibForUser(IntPtr typeLib, [MarshalAs(UnmanagedType.LPWStr)] string fullPath, [MarshalAs(UnmanagedType.LPWStr)] string helpDir);
[DllImport("oleaut32.dll")]
public static extern int UnRegisterTypeLibForUser(ref Guid libId, short major, short minor, int sysKind);
'@

$deps = @('commpro.dll','comms.dll','rscagent.dll','rscomm.dll','tcpcomm.dll','usbcomm.dll','zkemsdk.dll','zkemkeeper.dll')
foreach ($d in $deps) {
    $p = Join-Path $Dir $d
    if (Test-Path $p) {
        $h = [Zk.Native]::LoadLibraryEx($p, [IntPtr]::Zero, 0x8)
        "{0,-16} load={1}" -f $d, $(if ($h -ne [IntPtr]::Zero) { 'ok' } else { [ComponentModel.Win32Exception]::new([Runtime.InteropServices.Marshal]::GetLastWin32Error()).Message })
    }
}

$zk = Join-Path $Dir 'zkemkeeper.dll'
$tl = [IntPtr]::Zero
try {
    [Zk.Native]::LoadTypeLibEx($zk, 2, [ref]$tl)
    "typelib loaded: $tl"
    $rc = [Zk.Native]::RegisterTypeLibForUser($tl, $zk, $Dir)
    "RegisterTypeLibForUser rc=$rc"
} catch {
    "typelib load failed: " + $_.Exception.Message
}

Set-Location $Dir
try {
    $o = New-Object -ComObject zkemkeeper.ZKEM
    "COM OK: " + $o.GetType().FullName
    try {
        $ok = $o.Connect_Net('192.168.1.201', 4370)
        "Connect_Net(.201)=$ok"
        if ($ok) {
            $sn = ''; $r = $o.SSR_GetSerialNumber([ref]$sn); "serial ok=$r sn=$sn"
            $o.Disconnect()
        }
    } catch { "connect err: " + $_.Exception.Message }
    [System.Runtime.InteropServices.Marshal]::ReleaseComObject($o) | Out-Null
} catch { "COM failed: " + $_.Exception.Message }
