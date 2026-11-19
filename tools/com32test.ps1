try {
    $o = New-Object -ComObject zkemkeeper.ZKEM
    "COM32 OK: " + $o.GetType().FullName
    try {
        $ok = $o.Connect_Net('192.168.1.201', 4370)
        "Connect_Net(.201)=$ok"
        if ($ok) {
            $sn = ''
            $r = $o.SSR_GetSerialNumber([ref]$sn)
            "serial ok=$r sn=$sn"
            $o.Disconnect()
        }
    }
    catch { "connect err: " + $_.Exception.Message }
    [System.Runtime.InteropServices.Marshal]::ReleaseComObject($o) | Out-Null
}
catch { "COM32 failed: " + $_.Exception.Message }
