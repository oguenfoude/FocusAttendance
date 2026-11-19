using System.Reflection;
using System.Runtime.InteropServices;
using FocusAttendance.Domain.Entities;

#pragma warning disable CA1304 // Late-bound COM invocation is culture independent by design.

namespace FocusAttendance.Hardware;

/// <summary>
/// Optional second engine: the official ZKTeco Standalone SDK (zkemkeeper.dll / CZKEM), loaded
/// through late binding so the application does not depend on the COM library being registered.
/// Method arities follow the CZKEM type library (see the SDK manual, section 5).
/// </summary>
public sealed class ZkComSdkEngine : IZkEngine
{
    private const string ProgId = "zkemkeeper.ZKEM";
    private const string ClassId = "{00853A19-BD51-419B-9269-2DABE57EB61F}";
    private const int Machine = 1;

    private readonly Lazy<Type?> _comType = new(() =>
    {
        if (!OperatingSystem.IsWindows())
        {
            return null;
        }

        return Type.GetTypeFromProgID(ProgId) ?? Type.GetTypeFromCLSID(new Guid(ClassId));
    });

    [ThreadStatic]
    private static string? _connectError;

    private bool? _usable;

    public string Name => "ZKTeco Standalone SDK (zkemkeeper)";

    public bool IsAvailable => _usable ??= Probe();

    /// <summary>Human readable reason of the last failed connection on this thread.</summary>
    private static string ConnectError => _connectError ?? "SDK connection refused.";

    /// <summary>Verifies once that the COM class can actually be instantiated (DLL installed & registered).</summary>
    private bool Probe()
    {
        if (!OperatingSystem.IsWindows() || _comType.Value is null)
        {
            return false;
        }

        object? com = null;
        try
        {
            com = Activator.CreateInstance(_comType.Value);
            return com is not null;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return false;
        }
        finally
        {
            if (com is not null && Marshal.IsComObject(com))
            {
                Marshal.FinalReleaseComObject(com);
            }
        }
    }

    public Task<HardwareResult<DeviceSnapshot>> ReadSnapshotAsync(ZkEndpoint endpoint, CancellationToken cancellationToken = default) =>
        Task.Run(() => Execute(endpoint, com =>
        {
            if (!Connect(com, endpoint))
            {
                return HardwareResult<DeviceSnapshot>.Fail(ConnectError);
            }

            var product = ReadString(com, "GetProductCode");
            var name = ReadOption(com, "~DeviceName");
            var vendor = ReadString(com, "GetVendor");
            var model = Default(product, Default(name, string.Empty));

            var snapshot = new DeviceSnapshot
            {
                IpAddress = endpoint.IpAddress,
                Port = endpoint.Port,
                SerialNumber = ReadString(com, "GetSerialNumber"),
                ModelName = model,
                FirmwareVersion = Default(ReadString(com, "GetFirmwareVersion"), ReadOption(com, "~ZKFPVersion")),
                Platform = Default(ReadString(com, "GetPlatform"), vendor),
                MacAddress = ReadString(com, "GetDeviceMAC"),
                DeviceClock = ReadClock(com),
                UserCount = ReadStatus(com, 2),
                FingerCount = ReadStatus(com, 3),
                FaceCount = ReadStatus(com, 21),
                PunchCount = ReadStatus(com, 6),
                Engine = Name
            };

            Disconnect(com);
            return snapshot.IsIdentified
                ? HardwareResult<DeviceSnapshot>.Ok(snapshot)
                : HardwareResult<DeviceSnapshot>.Fail($"SDK: {endpoint} accepted the connection but returned no device identity.");
        }, "Read device info"), cancellationToken);

    public Task<HardwareResult<IReadOnlyList<DevicePunch>>> PullPunchesAsync(ZkEndpoint endpoint, CancellationToken cancellationToken = default) =>
        Task.Run(() => Execute(endpoint, com =>
        {
            if (!Connect(com, endpoint))
            {
                return HardwareResult<IReadOnlyList<DevicePunch>>.Fail(ConnectError);
            }

            var punches = new List<DevicePunch>();
            if (Invoke(com, "ReadGeneralLogData", Machine) is not bool read || !read)
            {
                Disconnect(com);
                return HardwareResult<IReadOnlyList<DevicePunch>>.Ok(punches, "No new attendance log.");
            }

            for (var guard = 0; guard < 200_000; guard++)
            {
                // SSR_GetGeneralLogData: machine, pin, verify, in/out, y, m, d, h, min, s, workcode.
                var args = new object[] { Machine, string.Empty, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
                if (Invoke(com, "SSR_GetGeneralLogData", args) is not bool found || !found)
                {
                    break;
                }

                var pin = Convert.ToString(args[1]) ?? string.Empty;
                if (pin.Length == 0)
                {
                    continue;
                }

                try
                {
                    punches.Add(new DevicePunch
                    {
                        DevicePin = pin,
                        VerifyMode = Convert.ToInt32(args[2]),
                        PunchState = Convert.ToInt32(args[3]),
                        Timestamp = new DateTime(
                            Convert.ToInt32(args[4]),
                            Convert.ToInt32(args[5]),
                            Convert.ToInt32(args[6]),
                            Convert.ToInt32(args[7]),
                            Convert.ToInt32(args[8]),
                            Convert.ToInt32(args[9])),
                        WorkCode = Convert.ToInt32(args[10])
                    });
                }
                catch (Exception ex) when (ex is ArgumentOutOfRangeException or FormatException or InvalidCastException)
                {
                    // Skip corrupted log rows.
                }
            }

            Disconnect(com);
            return HardwareResult<IReadOnlyList<DevicePunch>>.Ok(punches, $"SDK pulled {punches.Count} punch(es).");
        }, "Pull attendance"), cancellationToken);

    public Task<HardwareResult<IReadOnlyList<DeviceUser>>> ReadUsersAsync(ZkEndpoint endpoint, CancellationToken cancellationToken = default) =>
        Task.Run(() => Execute(endpoint, com =>
        {
            if (!Connect(com, endpoint))
            {
                return HardwareResult<IReadOnlyList<DeviceUser>>.Fail(ConnectError);
            }

            var users = new List<DeviceUser>();
            var uid = 0;
            for (var guard = 0; guard < 100_000; guard++)
            {
                // SSR_GetAllUserInfo: machine, pin, name, password, privilege, enabled.
                var args = new object[] { Machine, string.Empty, string.Empty, string.Empty, 0, false };
                if (Invoke(com, "SSR_GetAllUserInfo", args) is not bool found || !found)
                {
                    break;
                }

                var pin = Convert.ToString(args[1]) ?? string.Empty;
                if (pin.Length == 0)
                {
                    break;
                }

                users.Add(new DeviceUser
                {
                    Uid = ++uid,
                    UserId = pin,
                    Name = Convert.ToString(args[2]) ?? string.Empty,
                    Password = Convert.ToString(args[3]) ?? string.Empty,
                    Privilege = Convert.ToInt32(args[4]),
                    Card = 0
                });
            }

            Disconnect(com);
            return HardwareResult<IReadOnlyList<DeviceUser>>.Ok(users, $"SDK read {users.Count} user(s).");
        }, "Read device users"), cancellationToken);

    public Task<HardwareResult> PushUserAsync(ZkEndpoint endpoint, DeviceUser user, CancellationToken cancellationToken = default) =>
        Task.Run(() => Execute(endpoint, com =>
        {
            if (!Connect(com, endpoint))
            {
                return HardwareResult.Fail(ConnectError);
            }

            // SSR_SetUserInfo: machine, pin (string), name, password, privilege, enabled.
            var args = new object[]
            {
                Machine,
                string.IsNullOrWhiteSpace(user.UserId) ? user.Uid.ToString() : user.UserId,
                user.Name ?? string.Empty,
                user.Password ?? string.Empty,
                user.Privilege,
                true
            };

            var success = Invoke(com, "SSR_SetUserInfo", args);
            Disconnect(com);
            return HardwareResult.From(success is bool ok && ok, "SDK user write done.");
        }, "Push user"), cancellationToken);

    public Task<HardwareResult> DeleteUserAsync(ZkEndpoint endpoint, string userId, CancellationToken cancellationToken = default) =>
        Task.Run(() => Execute(endpoint, com =>
        {
            if (!Connect(com, endpoint))
            {
                return HardwareResult.Fail(ConnectError);
            }

            // SSR_DeleteEnrollData: machine, pin, backup number (12 = all templates).
            var success = Invoke(com, "SSR_DeleteEnrollData", Machine, userId, 12);
            Disconnect(com);
            return HardwareResult.From(success is bool ok && ok, "SDK delete done.");
        }, "Delete user"), cancellationToken);

    public Task<HardwareResult> SetNetworkAsync(ZkEndpoint endpoint, string ipAddress, string subnetMask, string gateway, int? port, CancellationToken cancellationToken = default) =>
        Task.Run(() => Execute(endpoint, com =>
        {
            if (!Connect(com, endpoint))
            {
                return HardwareResult.Fail(ConnectError);
            }

            // SetDeviceIP is the only network write exposed by this COM build.
            var success = Invoke(com, "SetDeviceIP", Machine, ipAddress);
            Disconnect(com);
            if (success is bool ok && ok)
            {
                return HardwareResult.Ok("Device IP written. The terminal restarts on the new address.");
            }

            return HardwareResult.Fail($"SDK refused the address {ipAddress}.");
        }, "Update network"), cancellationToken);

    public Task<HardwareResult> SyncClockAsync(ZkEndpoint endpoint, CancellationToken cancellationToken = default) =>
        Task.Run(() => Execute(endpoint, com =>
        {
            if (!Connect(com, endpoint))
            {
                return HardwareResult.Fail(ConnectError);
            }

            var now = DateTime.Now;
            var success = Invoke(com, "SetDeviceTime2", Machine, now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second);
            Disconnect(com);
            return HardwareResult.From(success is bool ok && ok, "SDK clock sync done.");
        }, "Sync clock"), cancellationToken);

    public Task<HardwareResult> RestartAsync(ZkEndpoint endpoint, CancellationToken cancellationToken = default) =>
        Task.Run(() => Execute(endpoint, com =>
        {
            if (!Connect(com, endpoint))
            {
                return HardwareResult.Fail(ConnectError);
            }

            var success = Invoke(com, "RestartDevice", Machine);
            Disconnect(com);
            return HardwareResult.From(success is bool ok && ok, "SDK reboot sent.");
        }, "Reboot"), cancellationToken);

    public Task<HardwareResult> PowerOffAsync(ZkEndpoint endpoint, CancellationToken cancellationToken = default) =>
        Task.Run(() => Execute(endpoint, com =>
        {
            if (!Connect(com, endpoint))
            {
                return HardwareResult.Fail(ConnectError);
            }

            var success = Invoke(com, "PowerOffDevice", Machine);
            Disconnect(com);
            return HardwareResult.From(success is bool ok && ok, "SDK power off sent.");
        }, "Power off"), cancellationToken);

    public Task<HardwareResult> ClearAdminsAsync(ZkEndpoint endpoint, CancellationToken cancellationToken = default) =>
        Task.Run(() => Execute(endpoint, com =>
        {
            if (!Connect(com, endpoint))
            {
                return HardwareResult.Fail(ConnectError);
            }

            var success = Invoke(com, "ClearAdministrators", Machine);
            Disconnect(com);
            return HardwareResult.From(success is bool ok && ok, "SDK administrators cleared.");
        }, "Clear administrators"), cancellationToken);

    public Task<HardwareResult> TestVoiceAsync(ZkEndpoint endpoint, int index = 0, CancellationToken cancellationToken = default) =>
        Task.Run(() => Execute(endpoint, com =>
        {
            if (!Connect(com, endpoint))
            {
                return HardwareResult.Fail(ConnectError);
            }

            // PlayVoice(position, length).
            var success = Invoke(com, "PlayVoice", index, 1);
            Disconnect(com);
            return HardwareResult.From(success is bool ok && ok, "SDK voice test sent.");
        }, "Voice test"), cancellationToken);

    /// <summary>
    /// Full diagnostic dump through the official SDK: every readable identity, storage counter and
    /// option of the terminal, one "key = value" line each. Never throws.
    /// </summary>
    public Task<IReadOnlyList<string>> DumpAsync(ZkEndpoint endpoint, CancellationToken cancellationToken = default) =>
        Task.Run<IReadOnlyList<string>>(() =>
        {
            var lines = new List<string>();
            if (!IsAvailable)
            {
                lines.Add("SDK = not available");
                return lines;
            }

            object? com = null;
            try
            {
                com = Activator.CreateInstance(_comType.Value!);
                if (com is null)
                {
                    lines.Add("SDK = cannot instantiate zkemkeeper.ZKEM");
                    return lines;
                }

                lines.Add($"Endpoint = {endpoint} (CommKey {endpoint.CommKey})");
                var connected = Connect(com, endpoint);
                lines.Add($"Connect_Net = {(connected ? "OK" : "FAILED")}");

                var code = new object[] { 0 };
                if (TryInvoke(com, "GetLastError", code))
                {
                    lines.Add($"GetLastError = {code[0]}");
                }

                if (!connected)
                {
                    return lines;
                }

                foreach (var method in new[]
                         {
                             "GetSerialNumber", "GetProductCode", "GetFirmwareVersion", "GetPlatform",
                             "GetDeviceMAC", "GetDeviceIP", "GetVendor"
                         })
                {
                    lines.Add($"{method} = {ReadStringValue(com, method)}");
                }

                foreach (var option in new[]
                         {
                             "~SerialNumber", "~DeviceName", "~OEMDeviceName", "~Platform", "~ZKFPVersion",
                             "~MAC", "MAC", "~Product", "~Vendor", "~DeviceType", "~Language", "~ZKLanguage"
                         })
                {
                    lines.Add($"GetSysOption({option}) = {ReadOptionValue(com, option)}");
                }

                var clock = ReadClockLine(com);
                lines.Add($"GetDeviceTime = {clock}");

                var tft = new object[] { Machine };
                lines.Add($"IsTFTMachine = {(TryInvoke(com, "IsTFTMachine", tft) ? Convert.ToString(tft[0]) : "(n/a)")}");

                var labels = new (int Code, string Name)[]
                {
                    (1, "admins"), (2, "users"), (3, "fingerprints"), (4, "passwords"), (5, "operation records"),
                    (6, "attendance records"), (7, "fingerprint capacity"), (8, "user capacity"),
                    (9, "attendance capacity"), (10, "free fingerprint capacity"), (11, "free user capacity"),
                    (12, "free attendance capacity"), (21, "faces"), (22, "face capacity")
                };

                foreach (var (status, name) in labels)
                {
                    var args = new object[] { Machine, status, 0 };
                    lines.Add($"GetDeviceStatus({status}) {name} = {(TryInvoke(com, "GetDeviceStatus", args) ? Convert.ToString(args[2]) : "(n/a)")}");
                }

                Disconnect(com);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                lines.Add("SDK error = " + ((ex as TargetInvocationException)?.InnerException?.Message ?? ex.Message));
            }
            finally
            {
                if (com is not null && OperatingSystem.IsWindows() && Marshal.IsComObject(com))
                {
                    Marshal.FinalReleaseComObject(com);
                }
            }

            return lines;
        }, cancellationToken);

    private static bool TryInvoke(object com, string method, object[] args)
    {
        try
        {
            return Invoke(com, method, args) is bool ok && ok;
        }
        catch (Exception ex) when (ex is TargetInvocationException or COMException or MissingMethodException or TargetParameterCountException or InvalidCastException)
        {
            return false;
        }
    }

    private static string ReadStringValue(object com, string method)
    {
        var args = new object[] { Machine, string.Empty };
        return TryInvoke(com, method, args) ? Convert.ToString(args[1]) ?? "(empty)" : "(failed)";
    }

    private static string ReadOptionValue(object com, string option)
    {
        var args = new object[] { Machine, option, string.Empty };
        return TryInvoke(com, "GetSysOption", args) ? Convert.ToString(args[2]) ?? "(empty)" : "(failed)";
    }

    private static string ReadClockLine(object com)
    {
        var args = new object[] { Machine, 0, 0, 0, 0, 0, 0 };
        if (!TryInvoke(com, "GetDeviceTime", args))
        {
            return "(failed)";
        }

        return $"{args[1]:00}/{args[2]:00}/{args[3]:0000} {args[4]:00}:{args[5]:00}:{args[6]:00}";
    }

    private HardwareResult Execute(ZkEndpoint endpoint, Func<object, HardwareResult> action, string operation)
    {
        if (!IsAvailable)
        {
            return HardwareResult.Fail($"{operation}: the ZKTeco SDK is not available on this machine.");
        }

        object? com = null;
        try
        {
            com = Activator.CreateInstance(_comType.Value!);
            if (com is null)
            {
                return HardwareResult.Fail($"{operation}: cannot instantiate zkemkeeper.ZKEM.");
            }

            return action(com);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            var inner = (ex as TargetInvocationException)?.InnerException?.Message ?? ex.Message;
            return HardwareResult.Fail($"{operation} failed: {inner}");
        }
        finally
        {
            if (com is not null && OperatingSystem.IsWindows() && Marshal.IsComObject(com))
            {
                Marshal.FinalReleaseComObject(com);
            }
        }
    }

    private HardwareResult<T> Execute<T>(ZkEndpoint endpoint, Func<object, HardwareResult<T>> action, string operation)
    {
        if (!IsAvailable)
        {
            return HardwareResult<T>.Fail($"{operation}: the ZKTeco SDK is not available on this machine.");
        }

        object? com = null;
        try
        {
            com = Activator.CreateInstance(_comType.Value!);
            if (com is null)
            {
                return HardwareResult<T>.Fail($"{operation}: cannot instantiate zkemkeeper.ZKEM.");
            }

            return action(com);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            var inner = (ex as TargetInvocationException)?.InnerException?.Message ?? ex.Message;
            return HardwareResult<T>.Fail($"{operation} failed: {inner}");
        }
        finally
        {
            if (com is not null && OperatingSystem.IsWindows() && Marshal.IsComObject(com))
            {
                Marshal.FinalReleaseComObject(com);
            }
        }
    }

    private static bool Connect(object com, ZkEndpoint endpoint)
    {
        _connectError = null;

        if (endpoint.CommKey > 0)
        {
            Invoke(com, "SetCommPassword", endpoint.CommKey);
        }

        if (Invoke(com, "Connect_Net", endpoint.IpAddress, endpoint.Port) is bool success && success)
        {
            return true;
        }

        var code = 0;
        try
        {
            var errorArgs = new object[] { 0 };
            Invoke(com, "GetLastError", errorArgs);
            code = Convert.ToInt32(errorArgs[0]);
        }
        catch (Exception ex) when (ex is TargetInvocationException or COMException or MissingMethodException or InvalidCastException or FormatException)
        {
            code = 0;
        }

        _connectError = code switch
        {
            0 => $"SDK: no answer from {endpoint.IpAddress}:{endpoint.Port} (port reachable, ZKTeco protocol silent).",
            -6 or -7 or -8 => $"SDK: CommKey rejected by {endpoint.IpAddress}:{endpoint.Port} (code {code}).",
            _ => $"SDK: connection to {endpoint.IpAddress}:{endpoint.Port} failed (code {code})."
        };

        return false;
    }

    private static void Disconnect(object com)
    {
        try
        {
            Invoke(com, "Disconnect");
        }
        catch (Exception ex) when (ex is TargetInvocationException or COMException or MissingMethodException)
        {
            // The terminal may drop the socket first.
        }
    }

    private static string ReadString(object com, string method)
    {
        try
        {
            var args = new object[] { Machine, string.Empty };
            return Invoke(com, method, args) is bool ok && ok ? Convert.ToString(args[1]) ?? string.Empty : string.Empty;
        }
        catch (Exception ex) when (ex is TargetInvocationException or COMException or MissingMethodException or TargetParameterCountException)
        {
            return string.Empty;
        }
    }

    /// <summary>Reads a device option such as ~DeviceName through GetSysOption.</summary>
    private static string ReadOption(object com, string option)
    {
        try
        {
            var args = new object[] { Machine, option, string.Empty };
            return Invoke(com, "GetSysOption", args) is bool ok && ok ? Convert.ToString(args[2]) ?? string.Empty : string.Empty;
        }
        catch (Exception ex) when (ex is TargetInvocationException or COMException or MissingMethodException or TargetParameterCountException)
        {
            return string.Empty;
        }
    }

    private static int ReadStatus(object com, int status)
    {
        try
        {
            var args = new object[] { Machine, status, 0 };
            if (Invoke(com, "GetDeviceStatus", args) is not bool ok || !ok)
            {
                return 0;
            }

            var value = Convert.ToInt32(args[2]);
            return value > 0 ? value : 0;
        }
        catch (Exception ex) when (ex is TargetInvocationException or COMException or MissingMethodException or TargetParameterCountException or FormatException)
        {
            return 0;
        }
    }

    private static DateTime? ReadClock(object com)
    {
        try
        {
            var args = new object[] { Machine, 0, 0, 0, 0, 0, 0 };
            if (Invoke(com, "GetDeviceTime", args) is not bool ok || !ok)
            {
                return null;
            }

            return new DateTime(
                Convert.ToInt32(args[1]),
                Convert.ToInt32(args[2]),
                Convert.ToInt32(args[3]),
                Convert.ToInt32(args[4]),
                Convert.ToInt32(args[5]),
                Convert.ToInt32(args[6]));
        }
        catch (Exception ex) when (ex is TargetInvocationException or COMException or MissingMethodException or TargetParameterCountException or ArgumentOutOfRangeException or FormatException)
        {
            return null;
        }
    }

    private static object? Invoke(object com, string method, params object[] args) =>
        com.GetType().InvokeMember(method, BindingFlags.InvokeMethod, null, com, args);

    private static string Default(string value, string fallback) =>
        string.IsNullOrWhiteSpace(value) ? fallback : value;
}
