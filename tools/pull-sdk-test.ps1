param(
    [string]$Dir = "C:\Users\Administrator\AppData\Local\Temp\opencode\pullsdk\extract\lib",
    [string]$Ip = "192.168.1.201",
    [int]$Port = 4370
)

$dll = Join-Path $Dir 'plcommpro.dll'
if (-not (Test-Path $dll)) { "plcommpro.dll missing"; return }

Add-Type -TypeDefinition @"
using System;
using System.Runtime.InteropServices;
public static class Pull {
    [DllImport(@"$dll", EntryPoint="Connect", CharSet=CharSet.Ansi, CallingConvention=CallingConvention.Cdecl)]
    public static extern int Connect(string ip, string protocol, int port, string commPassword, string deviceModel, string options);

    [DllImport(@"$dll", EntryPoint="PullLastError", CallingConvention=CallingConvention.Cdecl)]
    public static extern int PullLastError();

    [DllImport(@"$dll", EntryPoint="GetDeviceParam", CharSet=CharSet.Ansi, CallingConvention=CallingConvention.Cdecl)]
    public static extern int GetDeviceParam(IntPtr buffer, int size, string item, string value);

    [DllImport(@"$dll", EntryPoint="GetPullSDKVersion", CharSet=CharSet.Ansi, CallingConvention=CallingConvention.Cdecl)]
    public static extern int GetPullSDKVersion(IntPtr buffer, int size);

    [DllImport(@"$dll", EntryPoint="Disconnect", CallingConvention=CallingConvention.Cdecl)]
    public static extern int Disconnect();
}
"@

$ver = [Runtime.InteropServices.Marshal]::AllocHGlobal(256)
if ([Pull]::GetPullSDKVersion($ver, 256) -eq 0) { "PullSDK version: " + [Runtime.InteropServices.Marshal]::PtrToStringAnsi($ver) }
[Runtime.InteropServices.Marshal]::FreeHGlobal($ver)

$h = [Pull]::Connect($Ip, 'TCP', $Port, '', '', '')
"Connect($Ip,$Port) => $h err=$([Pull]::PullLastError())"
if ($h -ne 0) {
    foreach ($item in '~SerialNumber', '~DeviceName', '~Platform', '~ZKFPVersion', '~OEMDeviceName', 'FirmwareVersion', '~MAC') {
        $buf = [Runtime.InteropServices.Marshal]::AllocHGlobal(512)
        $rc = [Pull]::GetDeviceParam($buf, 512, $item, '')
        $val = [Runtime.InteropServices.Marshal]::PtrToStringAnsi($buf)
        "{0,-18} rc={1} value='{2}'" -f $item, $rc, $val
        [Runtime.InteropServices.Marshal]::FreeHGlobal($buf)
    }
    [Pull]::Disconnect() | Out-Null
}
