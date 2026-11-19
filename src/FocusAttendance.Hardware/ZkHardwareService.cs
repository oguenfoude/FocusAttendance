using FocusAttendance.Domain.Entities;

namespace FocusAttendance.Hardware;

/// <summary>
/// Dual-engine hardware facade: native port-4370 protocol first, official zkemkeeper SDK as a
/// transparent fallback when it is installed. All operations return a result instead of throwing.
/// </summary>
public sealed class ZkHardwareService
{
    private readonly ZkNativeEngine _native;
    private readonly ZkComSdkEngine _comSdk;
    private readonly LanScanner _scanner = new();

    public ZkHardwareService(int timeoutMs = 8000)
    {
        _native = new ZkNativeEngine(timeoutMs);
        _comSdk = new ZkComSdkEngine();
    }

    public bool ComSdkAvailable => _comSdk.IsAvailable;

    public string EngineSummary => ComSdkAvailable
        ? $"{_native.Name} + {_comSdk.Name}"
        : $"{_native.Name} (SDK not installed — native engine only)";

    public Task<IReadOnlyList<DiscoveredDevice>> ScanLanAsync(
        int timeoutMs = 250,
        int port = LanScanner.DefaultPort,
        IProgress<ScanProgress>? progress = null,
        CancellationToken cancellationToken = default) =>
        _scanner.ScanAsync(timeoutMs, port, progress, cancellationToken);

    public Task<IReadOnlyList<DiscoveredDevice>> ScanHostsAsync(
        IReadOnlyList<System.Net.IPAddress> hosts,
        int timeoutMs,
        IProgress<ScanProgress>? progress = null,
        CancellationToken cancellationToken = default) =>
        _scanner.ScanAsync(hosts, timeoutMs, LanScanner.DefaultPort, progress, cancellationToken);

    public Task<DiscoveredDevice> ProbeAsync(string ipAddress, int port, int commKey, CancellationToken cancellationToken = default) =>
        _scanner.ProbeAsync(ipAddress, port, commKey, cancellationToken);

    public Task<HardwareResult<DeviceSnapshot>> ReadSnapshotAsync(ZkEndpoint endpoint, CancellationToken cancellationToken = default) =>
        WithFallback(engine => engine.ReadSnapshotAsync(endpoint, cancellationToken));

    public Task<HardwareResult<IReadOnlyList<DevicePunch>>> PullPunchesAsync(ZkEndpoint endpoint, CancellationToken cancellationToken = default) =>
        WithFallback(engine => engine.PullPunchesAsync(endpoint, cancellationToken));

    public Task<HardwareResult<IReadOnlyList<DeviceUser>>> ReadUsersAsync(ZkEndpoint endpoint, CancellationToken cancellationToken = default) =>
        WithFallback(engine => engine.ReadUsersAsync(endpoint, cancellationToken));

    public Task<HardwareResult> PushUserAsync(ZkEndpoint endpoint, DeviceUser user, CancellationToken cancellationToken = default) =>
        WithFallback(engine => engine.PushUserAsync(endpoint, user, cancellationToken));

    public Task<HardwareResult> DeleteUserAsync(ZkEndpoint endpoint, string userId, CancellationToken cancellationToken = default) =>
        WithFallback(engine => engine.DeleteUserAsync(endpoint, userId, cancellationToken));

    public Task<HardwareResult> SetNetworkAsync(ZkEndpoint endpoint, string ip, string mask, string gateway, int? port, CancellationToken cancellationToken = default) =>
        WithFallback(engine => engine.SetNetworkAsync(endpoint, ip, mask, gateway, port, cancellationToken));

    public Task<HardwareResult> SyncClockAsync(ZkEndpoint endpoint, CancellationToken cancellationToken = default) =>
        WithFallback(engine => engine.SyncClockAsync(endpoint, cancellationToken));

    public Task<HardwareResult> RestartAsync(ZkEndpoint endpoint, CancellationToken cancellationToken = default) =>
        WithFallback(engine => engine.RestartAsync(endpoint, cancellationToken));

    public Task<HardwareResult> PowerOffAsync(ZkEndpoint endpoint, CancellationToken cancellationToken = default) =>
        WithFallback(engine => engine.PowerOffAsync(endpoint, cancellationToken));

    public Task<HardwareResult> ClearAdminsAsync(ZkEndpoint endpoint, CancellationToken cancellationToken = default) =>
        WithFallback(engine => engine.ClearAdminsAsync(endpoint, cancellationToken));

    public Task<HardwareResult> TestVoiceAsync(ZkEndpoint endpoint, int index = 0, CancellationToken cancellationToken = default) =>
        WithFallback(engine => engine.TestVoiceAsync(endpoint, index, cancellationToken));

    /// <summary>True when the endpoint answers the protocol handshake.</summary>
    public async Task<bool> IsReachableAsync(ZkEndpoint endpoint, CancellationToken cancellationToken = default)
    {
        var result = await _native.ReadSnapshotAsync(endpoint, cancellationToken).ConfigureAwait(false);
        return result.Success;
    }

    /// <summary>Full SDK diagnostic dump for one endpoint (one "key = value" line per item).</summary>
    public Task<IReadOnlyList<string>> DumpSdkInfoAsync(ZkEndpoint endpoint, CancellationToken cancellationToken = default) =>
        _comSdk.DumpAsync(endpoint, cancellationToken);

    private async Task<HardwareResult<T>> WithFallback<T>(Func<IZkEngine, Task<HardwareResult<T>>> operation)
    {
        var primary = await operation(_native).ConfigureAwait(false);
        if (primary.Success || !_comSdk.IsAvailable)
        {
            return primary;
        }

        var secondary = await operation(_comSdk).ConfigureAwait(false);
        return secondary.Success
            ? secondary with { Message = $"{secondary.Message} [SDK fallback]" }
            : primary with { Message = $"{primary.Message} | {secondary.Message}" };
    }

    private async Task<HardwareResult> WithFallback(Func<IZkEngine, Task<HardwareResult>> operation)
    {
        var primary = await operation(_native).ConfigureAwait(false);
        if (primary.Success || !_comSdk.IsAvailable)
        {
            return primary;
        }

        var secondary = await operation(_comSdk).ConfigureAwait(false);
        return secondary.Success
            ? secondary with { Message = $"{secondary.Message} [SDK fallback]" }
            : primary with { Message = $"{primary.Message} | {secondary.Message}" };
    }
}
