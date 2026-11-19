using System.Runtime.InteropServices;
using FocusAttendance.Hardware;

// The SDK DLLs live next to the WPF app; make them resolvable from this console harness too.
SetDllDirectory(@"D:\FocusAttendance\src\FocusAttendance.App\bin\Release\net10.0-windows10.0.19041.0");

var ip = args.Length > 0 ? args[0] : "192.168.1.201";
var port = args.Length > 1 ? int.Parse(args[1]) : 4370;
var key = args.Length > 2 ? int.Parse(args[2]) : 0;
var endpoint = new ZkEndpoint(ip, port, key);

Console.WriteLine($"=== SDK availability on {endpoint} ===");
var sdk = new ZkComSdkEngine();
Console.WriteLine($"IsAvailable = {sdk.IsAvailable}");
Console.WriteLine();

Console.WriteLine("=== Full SDK dump ===");
var dump = await sdk.DumpAsync(endpoint);
foreach (var line in dump)
{
    Console.WriteLine(line);
}

Console.WriteLine();
Console.WriteLine("=== Native engine snapshot ===");
var native = new ZkNativeEngine(4000);
var snap = await native.ReadSnapshotAsync(endpoint);
Console.WriteLine($"success={snap.Success} message={snap.Message}");
if (snap.Value is { } s)
{
    Console.WriteLine($"  serial={s.SerialNumber} model={s.ModelName} fw={s.FirmwareVersion} platform={s.Platform} mac={s.MacAddress} clock={s.DeviceClock} users={s.UserCount} fp={s.FingerCount} faces={s.FaceCount} punches={s.PunchCount}");
}

Console.WriteLine();
Console.WriteLine("=== Users ===");
var users = await native.ReadUsersAsync(endpoint);
Console.WriteLine($"success={users.Success} message={users.Message} count={users.Value?.Count ?? 0}");

Console.WriteLine("=== Punches ===");
var punches = await native.PullPunchesAsync(endpoint);
Console.WriteLine($"success={punches.Success} message={punches.Message} count={punches.Value?.Count ?? 0}");

[DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
static extern bool SetDllDirectory(string lpPathName);
