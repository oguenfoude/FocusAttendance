param([string]$Password = 'Admin@2026')

Add-Type -AssemblyName UIAutomationClient, UIAutomationTypes
$proc = Get-Process FocusAttendance -ErrorAction Stop | Select-Object -First 1
$root = [System.Windows.Automation.AutomationElement]::RootElement
$pc = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ProcessIdProperty, $proc.Id)
$win = $root.FindFirst([System.Windows.Automation.TreeScope]::Children, $pc)
$cond = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::AutomationIdProperty, 'PasswordBox')
$pw = $win.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $cond)
if ($null -eq $pw) { 'already logged in'; return }

$ws = New-Object -ComObject WScript.Shell
$pw.SetFocus()
Start-Sleep -Milliseconds 500
$ws.SendKeys($Password)
Start-Sleep -Milliseconds 300
$ws.SendKeys('{ENTER}')
Start-Sleep -Seconds 9
'logged in'
