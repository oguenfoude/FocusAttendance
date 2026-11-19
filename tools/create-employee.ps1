param(
    [string]$Out = 'employee.png'
)

Add-Type -AssemblyName UIAutomationClient, UIAutomationTypes
Add-Type -AssemblyName System.Drawing
Add-Type @'
using System;using System.Runtime.InteropServices;
public class UIAWin {
 [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out RECT r);
 [DllImport("user32.dll")] public static extern bool SetWindowPos(IntPtr h, IntPtr a, int x,int y,int cx,int cy,uint f);
 [StructLayout(LayoutKind.Sequential)] public struct RECT{public int Left,Top,Right,Bottom;}
 public static void Top(IntPtr h,bool on){ SetWindowPos(h, on?new IntPtr(-1):new IntPtr(-2),0,0,0,0,0x13); }
}
'@

$proc = Get-Process FocusAttendance -ErrorAction Stop | Select-Object -First 1
$root = [System.Windows.Automation.AutomationElement]::RootElement
$pc = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ProcessIdProperty, $proc.Id)
$win = $null
for ($i = 0; $i -lt 15 -and $null -eq $win; $i++) {
    try { $win = $root.FindFirst([System.Windows.Automation.TreeScope]::Children, $pc) } catch { Start-Sleep -Milliseconds 700 }
}

function Find-ById([string]$id) {
    $c = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::AutomationIdProperty, $id)
    return $win.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $c)
}

function Set-Value([string]$id, [string]$value) {
    $el = Find-ById $id
    if ($null -eq $el) { Write-Host "missing $id"; return $false }
    $pattern = $el.GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern)
    $pattern.SetValue($value)
    Start-Sleep -Milliseconds 200
    return $true
}

function Invoke-El([string]$id) {
    $el = Find-ById $id
    if ($null -eq $el) { Write-Host "missing $id"; return $false }
    $pattern = $el.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern)
    $pattern.Invoke()
    Start-Sleep -Seconds 2
    return $true
}

# New employee
Invoke-El 'NewEmployeeButton' | Out-Null
Start-Sleep -Seconds 2
Set-Value 'EmployeePin' '9001' | Out-Null
Set-Value 'EmployeeNameAr' 'Employee Profile AR' | Out-Null
Set-Value 'EmployeeFirstName' 'Test' | Out-Null
Set-Value 'EmployeeLastName' 'Profile' | Out-Null
Invoke-El 'SaveEmployeeButton' | Out-Null
Start-Sleep -Seconds 3

# Capture list
$rect = New-Object UIAWin+RECT
[UIAWin]::GetWindowRect($proc.MainWindowHandle, [ref]$rect) | Out-Null
$w = $rect.Right - $rect.Left; $h = $rect.Bottom - $rect.Top
[UIAWin]::Top($proc.MainWindowHandle, $true)
Start-Sleep -Milliseconds 700
$bmp = New-Object System.Drawing.Bitmap($w, $h)
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.CopyFromScreen($rect.Left, $rect.Top, 0, 0, (New-Object System.Drawing.Size($w, $h)))
$g.Dispose(); $bmp.Save($Out, [System.Drawing.Imaging.ImageFormat]::Png); $bmp.Dispose()
[UIAWin]::Top($proc.MainWindowHandle, $false)
"$Out ($w x $h)"

# Open the first profile
Invoke-El 'EditEmployeeButton' | Out-Null
Start-Sleep -Seconds 2
[UIAWin]::Top($proc.MainWindowHandle, $true)
Start-Sleep -Milliseconds 600
$bmp = New-Object System.Drawing.Bitmap($w, $h)
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.CopyFromScreen($rect.Left, $rect.Top, 0, 0, (New-Object System.Drawing.Size($w, $h)))
$g.Dispose(); $bmp.Save(($Out -replace '\.png$', '-profile.png'), [System.Drawing.Imaging.ImageFormat]::Png); $bmp.Dispose()
[UIAWin]::Top($proc.MainWindowHandle, $false)
"profile captured"
