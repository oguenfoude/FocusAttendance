param(
    [string]$Action = 'shot',
    [string]$Id = '',
    [string]$Out = 'shot.png',
    [int]$Wait = 1500,
    [switch]$NoTop
)

Add-Type -AssemblyName UIAutomationClient, UIAutomationTypes
Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName System.Windows.Forms
Add-Type @'
using System;using System.Runtime.InteropServices;using System.Threading;
public class UIW {
 [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out RECT r);
 [DllImport("user32.dll")] public static extern bool SetWindowPos(IntPtr h, IntPtr a, int x,int y,int cx,int cy,uint f);
 [DllImport("user32.dll")] public static extern void SwitchToThisWindow(IntPtr h, bool a);
 [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
 [StructLayout(LayoutKind.Sequential)] public struct RECT{public int Left,Top,Right,Bottom;}
 public static void Go(IntPtr h){ SwitchToThisWindow(h,true); SetForegroundWindow(h); }
 public static void Top(IntPtr h,bool on){ SetWindowPos(h, on?new IntPtr(-1):new IntPtr(-2),0,0,0,0,0x13); }
}
'@

$proc = Get-Process FocusAttendance -ErrorAction Stop | Select-Object -First 1
[UIW]::Go($proc.MainWindowHandle)
Start-Sleep -Milliseconds 400

$root = [System.Windows.Automation.AutomationElement]::RootElement
$pc = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ProcessIdProperty, $proc.Id)
$windows = @($root.FindAll([System.Windows.Automation.TreeScope]::Children, $pc))
if ($windows.Count -eq 0) { throw 'no window found' }

function Find-Global([string]$id) {
    $cond = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::AutomationIdProperty, $id)
    foreach ($w in $windows) {
        $e = $w.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $cond)
        if ($null -ne $e) { return [pscustomobject]@{ Window = $w; Element = $e } }
    }
    return $null
}

function Save-Window($win, [string]$path) {
    $hwnd = [IntPtr]$win.Current.NativeWindowHandle
    $rect = New-Object UIW+RECT
    [UIW]::GetWindowRect($hwnd, [ref]$rect) | Out-Null
    $w = $rect.Right - $rect.Left; $h = $rect.Bottom - $rect.Top
    [UIW]::Top($hwnd, $true); Start-Sleep -Milliseconds 500
    $bmp = New-Object System.Drawing.Bitmap($w, $h)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.CopyFromScreen($rect.Left, $rect.Top, 0, 0, (New-Object System.Drawing.Size($w, $h)))
    $g.Dispose(); $bmp.Save($path, [System.Drawing.Imaging.ImageFormat]::Png); $bmp.Dispose()
    [UIW]::Top($hwnd, $false)
    "$path ($w x $h)"
}

switch ($Action) {
    'invoke' {
        $hit = Find-Global $Id
        if ($null -eq $hit) { "missing $Id"; exit 2 }
        $pat = $hit.Element.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern)
        $pat.Invoke()
        Start-Sleep -Milliseconds $Wait
        "invoked $Id"
    }
    'select' {
        $hit = Find-Global $Id
        if ($null -eq $hit) { "missing $Id"; exit 2 }
        $pat = $hit.Element.GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern)
        $pat.Select()
        Start-Sleep -Milliseconds $Wait
        "selected $Id"
    }
    'shot' {
        if ($Id) {
            $hit = Find-Global $Id
            if ($null -eq $hit) { "missing $Id"; exit 2 }
            $walker = [System.Windows.Automation.TreeWalker]::ControlViewWalker
            $node = $hit.Element
            while ($null -ne $node -and $node.Current.ControlType -ne [System.Windows.Automation.ControlType]::Window) {
                $parent = $walker.GetParent($node)
                if ($null -eq $parent -or $parent -eq $root) { break }
                $node = $parent
            }
            Save-Window $node $Out
        }
        else {
            Save-Window $windows[0] $Out
        }
    }
    'fullshot' {
        $bounds = [System.Windows.Forms.SystemInformation]::VirtualScreen
        $bmp = New-Object System.Drawing.Bitmap($bounds.Width, $bounds.Height)
        $g = [System.Drawing.Graphics]::FromImage($bmp)
        $g.CopyFromScreen($bounds.Left, $bounds.Top, 0, 0, (New-Object System.Drawing.Size($bounds.Width, $bounds.Height)))
        $g.Dispose(); $bmp.Save($Out, [System.Drawing.Imaging.ImageFormat]::Png); $bmp.Dispose()
        "$Out ($($bounds.Width) x $($bounds.Height))"
    }
    'dump' {
        foreach ($w in $windows) {
            "== window: $($w.Current.Name)"
            $all = $w.FindAll([System.Windows.Automation.TreeScope]::Descendants, [System.Windows.Automation.Condition]::TrueCondition)
            foreach ($e in $all) {
                $id = $e.Current.AutomationId
                if ($id) { "  {0} | {1} | {2}" -f $id, $e.Current.ControlType.ProgrammaticName, $e.Current.Name }
            }
        }
    }
}
