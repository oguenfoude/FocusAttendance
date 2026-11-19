$path = "C:\Users\Administrator\AppData\Local\Temp\opencode\interop\Interop.zkemkeeper.dll"
$a = [System.Reflection.Assembly]::LoadFrom($path)
$a.GetTypes() | ForEach-Object { $_.FullName } | Sort-Object
"---- CZKEM class methods ----"
$cls = $a.GetTypes() | Where-Object { $_.Name -eq 'CZKEMClass' -or $_.Name -eq 'CZKEM' } | Select-Object -First 1
$cls.FullName
foreach ($m in $cls.GetMethods() | Where-Object { $true } | Sort-Object Name) {
    $ps = ($m.GetParameters() | ForEach-Object { "$($_.ParameterType.Name) $($_.Name)" + $(if ($_.IsOut) { ' [out]' } else { '' }) }) -join ', '
    "$($m.ReturnType.Name) $($m.Name)($ps)"
}

"---- all CZKEM methods ----"
$cls.GetMethods() | Sort-Object Name | ForEach-Object {
    $ps = ($_.GetParameters() | ForEach-Object { "$($_.ParameterType.Name) $($_.Name)" + $(if ($_.IsOut) { ' [out]' } else { '' }) }) -join ', '
    "$($_.ReturnType.Name) $($_.Name)($ps)"
}