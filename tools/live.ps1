param(
    [string]$Action = 'shot',
    [string]$Keys = '',
    [string]$Out = 'shot.png'
)

Add-Type -AssemblyName System.Drawing
Add-Type @'
using System;using System.Runtime.InteropServices;using System.Threading;
public class L {
 [DllImport("user32.dll")] public static extern void SwitchToThisWindow(IntPtr h,bool a);
 [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
 [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr h,int c);
 [DllImport("user32.dll")] public static extern IntPtr SetActiveWindow(IntPtr h);
 [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out RECT r);
 [DllImport("user32.dll")] public static extern bool SetWindowPos(IntPtr h,IntPtr a,int x,int y,int cx,int cy,uint f);
 [DllImport("user32.dll")] static extern bool SetCursorPos(int x,int y);
 [DllImport("user32.dll")] static extern void mouse_event(uint f,uint dx,uint dy,uint d,IntPtr e);
 [StructLayout(LayoutKind.Sequential)] public struct RECT{public int Left,Top,Right,Bottom;}
 public static void Go(IntPtr h){ ShowWindow(h,9); SwitchToThisWindow(h,true); SetForegroundWindow(h); SetActiveWindow(h); }
 public static void Top(IntPtr h,bool on){ SetWindowPos(h, on?new IntPtr(-1):new IntPtr(-2),0,0,0,0,0x13); }
 public static void Click(int x,int y){ SetCursorPos(x,y); Thread.Sleep(180); mouse_event(2,0,0,0,IntPtr.Zero); mouse_event(4,0,0,0,IntPtr.Zero); Thread.Sleep(500); }
}
'@

$proc = Get-Process FocusAttendance -ErrorAction Stop | Select-Object -First 1
[L]::Go($proc.MainWindowHandle)
Start-Sleep -Milliseconds 900

if ($Action -eq 'login') {
    $ws = New-Object -ComObject WScript.Shell
    $ws.SendKeys('{TAB}'); Start-Sleep -Milliseconds 300
    $ws.SendKeys('Admin@2026'); Start-Sleep -Milliseconds 300
    $ws.SendKeys('{ENTER}'); Start-Sleep -Seconds 8
    'logged in'
    exit 0
}

if ($Action -eq 'keys') {
    $ws = New-Object -ComObject WScript.Shell
    $ws.SendKeys($Keys)
    Start-Sleep -Seconds 3
}

if ($Action -eq 'click') {
    $rect = New-Object L+RECT
    [L]::GetWindowRect($proc.MainWindowHandle, [ref]$rect) | Out-Null
    $x = $rect.Left + [int]$Keys.Split(',')[0]
    $y = $rect.Top + [int]$Keys.Split(',')[1]
    [L]::Top($proc.MainWindowHandle, $true)
    Start-Sleep -Milliseconds 400
    [L]::Click($x, $y)
    [L]::Top($proc.MainWindowHandle, $false)
    Start-Sleep -Seconds 1
}

$rect = New-Object L+RECT
[L]::GetWindowRect($proc.MainWindowHandle, [ref]$rect) | Out-Null
$w = $rect.Right - $rect.Left
$h = $rect.Bottom - $rect.Top
[L]::Top($proc.MainWindowHandle, $true)
Start-Sleep -Milliseconds 700
$bmp = New-Object System.Drawing.Bitmap($w, $h)
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.CopyFromScreen($rect.Left, $rect.Top, 0, 0, (New-Object System.Drawing.Size($w, $h)))
$g.Dispose()
$bmp.Save($Out, [System.Drawing.Imaging.ImageFormat]::Png)
$bmp.Dispose()
[L]::Top($proc.MainWindowHandle, $false)
"$Out ($w x $h)"
