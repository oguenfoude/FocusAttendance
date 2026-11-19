param(
    [string]$Tlb = "D:\FocusAttendance\src\FocusAttendance.App\sdk64\zkemkeeper.tlb",
    [string]$Dll = "D:\FocusAttendance\src\FocusAttendance.App\sdk64\zkemkeeper.dll"
)

Add-Type -Namespace Zk -Name Tl -MemberDefinition @'
[DllImport("oleaut32.dll", PreserveSig=true)]
public static extern int LoadTypeLibEx([MarshalAs(UnmanagedType.LPWStr)] string path, int regKind, out IntPtr typeLib);
[DllImport("oleaut32.dll", PreserveSig=true)]
public static extern int RegisterTypeLibForUser(IntPtr typeLib, [MarshalAs(UnmanagedType.LPWStr)] string fullPath, [MarshalAs(UnmanagedType.LPWStr)] string helpDir);
'@

foreach ($kind in 0, 1, 2) {
    $ptr = [IntPtr]::Zero
    $hr = [Zk.Tl]::LoadTypeLibEx($Tlb, $kind, [ref]$ptr)
    "tlb kind=$kind hr=0x{0:X8} ptr=$ptr" -f $hr
}

$ptr = [IntPtr]::Zero
$hr = [Zk.Tl]::LoadTypeLibEx($Tlb, 0, [ref]$ptr)
if ($hr -eq 0 -and $ptr -ne [IntPtr]::Zero) {
    $rc = [Zk.Tl]::RegisterTypeLibForUser($ptr, $Tlb, (Split-Path $Tlb))
    "RegisterTypeLibForUser hr=0x{0:X8}" -f $rc
}

$hr2 = [Zk.Tl]::LoadTypeLibEx($Dll, 0, [ref]$ptr)
"dll loadtyp elib hr=0x{0:X8}" -f $hr2
