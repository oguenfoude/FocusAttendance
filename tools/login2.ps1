param(
    [string]$User = 'admin',
    [string]$Password = 'Admin@2026'
)

Add-Type -AssemblyName UIAutomationClient, UIAutomationTypes
$proc = Get-Process FocusAttendance -ErrorAction Stop | Select-Object -First 1
$root = [System.Windows.Automation.AutomationElement]::RootElement
$pc = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ProcessIdProperty, $proc.Id)
$win = $root.FindFirst([System.Windows.Automation.TreeScope]::Children, $pc)
if ($null -eq $win) { throw 'window not found' }

function Find-Id([string]$id) {
    $c = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::AutomationIdProperty, $id)
    return $win.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $c)
}

$userBox = Find-Id 'UsernameBox'
$passBox = Find-Id 'PasswordBox'
if ($null -eq $passBox) { 'already logged in'; return }

$ws = New-Object -ComObject WScript.Shell
$ws.AppActivate($proc.Id) | Out-Null
Start-Sleep -Milliseconds 500

$userBox.SetFocus()
Start-Sleep -Milliseconds 300
$ws.SendKeys('^a')
Start-Sleep -Milliseconds 150
$ws.SendKeys($User)
Start-Sleep -Milliseconds 300

$passBox.SetFocus()
Start-Sleep -Milliseconds 300
$ws.SendKeys('^a')
Start-Sleep -Milliseconds 150
$ws.SendKeys($Password)
Start-Sleep -Milliseconds 400
$ws.SendKeys('{ENTER}')
Start-Sleep -Seconds 10

$passAfter = Find-Id 'PasswordBox'
if ($null -eq $passAfter) { 'logged in' } else { 'login still on screen' }
