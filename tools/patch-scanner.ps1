$p = 'D:\FocusAttendance\src\FocusAttendance.Hardware\LanScanner.cs'
$t = [System.IO.File]::ReadAllText($p)

$markerStart = '        var outcome = Handshake(ipAddress, port, commKey, cancellationToken);'
$markerEnd = '    /// <summary>Sends the bare 8-byte CONNECT without the 4-byte TCP frame header.</summary>'

$start = $t.IndexOf($markerStart)
$end = $t.IndexOf($markerEnd)
if ($start -lt 0 -or $end -lt 0 -or $end -le $start) { throw "markers not found ($start/$end)" }

$replacement = @'
        // A single session only: most terminals accept one connection at a time, so the handshake
        // and the identity read must run on the same TCP/UDP session.
        var engine = new ZkNativeEngine(_probeTimeoutMs);
        var result = await engine
            .ReadSnapshotAsync(new ZkEndpoint(ipAddress, port, commKey), cancellationToken)
            .ConfigureAwait(false);

        if (result.Success && result.Value is { } snapshot)
        {
            return new DiscoveredDevice
            {
                IpAddress = ipAddress,
                Port = port,
                Responded = true,
                PortOpen = true,
                Status = DiscoveryStatus.Verified,
                Snapshot = snapshot,
                Transport = udpAnswered ? "UDP" : "TCP"
            };
        }

        var (sdkDevice, sdkMessage) = await TrySdkAsync(ipAddress, port, commKey, cancellationToken).ConfigureAwait(false);
        if (sdkDevice is not null)
        {
            return sdkDevice;
        }

        if (NeedsCommKey(result.Message))
        {
            return new DiscoveredDevice
            {
                IpAddress = ipAddress,
                Port = port,
                Responded = false,
                PortOpen = true,
                Status = DiscoveryStatus.CommKeyRequired,
                Error = "The terminal asked for a CommKey: enter the device communication key and scan again.",
                Transport = portOpen ? "TCP" : "UDP"
            };
        }

        var raw = TryRawTcpProbe(ipAddress, port, out var rawDetail);
        return new DiscoveredDevice
        {
            IpAddress = ipAddress,
            Port = port,
            Responded = false,
            PortOpen = true,
            Status = DiscoveryStatus.PortOpenUnverified,
            Error = $"Port {port} reachable but no ZKTeco answer. Native: {result.Message} | {sdkMessage} | Raw TCP: {(raw ? "answered" : "no answer")} - {rawDetail}",
            Transport = portOpen ? "TCP" : "UDP"
        };
    }

    /// <summary>Official SDK fallback: zkemkeeper can reach terminals the native framing cannot.</summary>
    private static async Task<(DiscoveredDevice? Device, string Message)> TrySdkAsync(
        string ipAddress,
        int port,
        int commKey,
        CancellationToken cancellationToken)
    {
        var engine = new ZkComSdkEngine();
        if (!engine.IsAvailable)
        {
            return (null, "SDK unavailable");
        }

        try
        {
            var result = await engine
                .ReadSnapshotAsync(new ZkEndpoint(ipAddress, port, commKey), cancellationToken)
                .ConfigureAwait(false);
            if (result.Success && result.Value is { } snapshot)
            {
                return (new DiscoveredDevice
                {
                    IpAddress = ipAddress,
                    Port = port,
                    Responded = true,
                    PortOpen = true,
                    Status = DiscoveryStatus.Verified,
                    Snapshot = snapshot,
                    Transport = "SDK"
                }, "SDK verified");
            }

            return (null, "SDK: " + result.Message);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return (null, "SDK: " + ex.Message);
        }
    }

    private static bool NeedsCommKey(string message) =>
        message.Contains("COMM key", StringComparison.OrdinalIgnoreCase) ||
        message.Contains("0x2005", StringComparison.OrdinalIgnoreCase);

'@

$t = $t.Substring(0, $start) + $replacement + $t.Substring($end)
[System.IO.File]::WriteAllText($p, $t, [System.Text.UTF8Encoding]::new($false))
"rewritten; Handshake present: " + $t.Contains('private static DiscoveryStatus Handshake')
