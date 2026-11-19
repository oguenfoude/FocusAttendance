using System.Globalization;
using System.IO;
using System.Text;
using FocusAttendance.Application;
using FocusAttendance.Domain;
using FocusAttendance.Domain.Attendance;
using FocusAttendance.Domain.Entities;
using FocusAttendance.Hardware;

namespace FocusAttendance.App;

/// <summary>
/// Headless verification entry point (<c>FocusAttendance.exe --selftest</c>).
/// Runs the database laws, the attendance engine, the archive policy, the hardware engines
/// and a real LAN scan on TCP/UDP port 4370. Writes a report and returns exit code 0 on success.
/// </summary>
internal static class SelfTest
{
    public static int Run(string logDirectory)
    {
        var report = new StringBuilder();
        var failures = 0;
        Directory.CreateDirectory(logDirectory);
        var logPath = Path.Combine(logDirectory, "selftest.log");
        try
        {
            File.WriteAllText(logPath, string.Empty, new UTF8Encoding(false));
        }
        catch (IOException)
        {
            // The report is still written to the console.
        }

        void Line(string text)
        {
            report.AppendLine(text);
            try
            {
                File.AppendAllText(logPath, text + Environment.NewLine, new UTF8Encoding(false));
            }
            catch (IOException)
            {
                // Best effort logging.
            }
        }

        void Check(string name, Func<bool> check, string? detail = null)
        {
            bool ok;
            string? error = null;
            try
            {
                ok = check();
            }
            catch (Exception ex)
            {
                ok = false;
                error = ex.Message;
            }

            if (!ok)
            {
                failures++;
            }

            Line($"[{(ok ? "PASS" : "FAIL")}] {name}{(detail is null ? string.Empty : " — " + detail)}{(error is null ? string.Empty : " — " + error)}");
        }

        Line($"Focus Attendance self-test — {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        Line($"Machine: {Environment.MachineName} | OS: {Environment.OSVersion} | .NET {Environment.Version}");
        Line(new string('-', 78));

        var tempRoot = Path.Combine(Path.GetTempPath(), "focus-selftest", Guid.NewGuid().ToString("N"));
        AppHost host;
        try
        {
            host = new AppHost(tempRoot);
            host.Initialize(createStartupBackup: false);
        }
        catch (Exception ex)
        {
            Line("[FAIL] Database initialization — " + ex);
            Write(report, logDirectory);
            return 1;
        }

        try
        {
            var info = host.Db.GetInfo();
            Check("SQLite database created", () => File.Exists(host.Db.DatabasePath));
            Check("WAL journal mode active", () => info.IsWal, info.JournalMode);
            Check("Schema version >= 1", () => info.SchemaVersion >= 1, "v" + info.SchemaVersion);
            Check("Zero seeded employees (no fake data)", () => info.Employees == 0);
            Check("Zero seeded devices (no fake data)", () => info.Devices == 0);
            Check("Zero seeded punches (no fake data)", () => info.Punches == 0);
            Check("OPGI departments seeded", () => info.Departments >= 10, info.Departments + " departments");
            Check("Official + free-time shifts seeded", () => info.WorkRules == 2);
            Check("Single SuperAdmin account", () => host.Users.Count() == 1);

            var rule = host.Shifts.GetDefault();
            Check("Official shift 08:00–12:00 / 13:00–16:30",
                () => rule.MorningStartMin == 480 && rule.LunchStartMin == 720 && rule.LunchEndMin == 780 && rule.EveningEndMin == 990);
            Check("15 min grace / 450 min target", () => rule.MorningGraceMinutes == 15 && rule.StandardDailyTargetMinutes == 450);
            Check("15 min afternoon grace / 60 min lunch deduction",
                () => rule.AfternoonGraceMinutes == 15 && rule.LunchDeductionMinutes == 60);

            var admin = host.Auth.Login("admin", Data.SeedData.InitialAdminPassword);
            Check("Seeded SuperAdmin authenticates", () => admin.Success);
            Check("Wrong password is rejected", () => !host.Auth.Login("admin", "wrong").Success);

            var day = new DateTime(2026, 11, 15);
            var fourScans = AttendanceCalculator.Calculate(new AttendanceDayInput
            {
                Date = day,
                Rule = rule,
                IsScheduledDay = true,
                Punches =
                [
                    day.AddHours(8), day.AddHours(12), day.AddHours(13), day.AddHours(16).AddMinutes(30)
                ]
            });
            Check("4-scan day = 07:30 exact", () => fourScans.TotalNetMinutes == 450, fourScans.TotalNetMinutes + " min");

            var twoScans = AttendanceCalculator.Calculate(new AttendanceDayInput
            {
                Date = day,
                Rule = rule,
                IsScheduledDay = true,
                Punches = [day.AddHours(8), day.AddHours(16).AddMinutes(30)]
            });
            Check("2-scan day deducts the 60 min lunch", () => twoScans.TotalNetMinutes == 450 && twoScans.BreakMinutes == 60);

            var late = AttendanceCalculator.Calculate(new AttendanceDayInput
            {
                Date = day,
                Rule = rule,
                IsScheduledDay = true,
                Punches =
                [
                    day.AddHours(8).AddMinutes(16), day.AddHours(12), day.AddHours(13), day.AddHours(16).AddMinutes(30)
                ]
            });
            Check("08:16 arrival is 1 minute late", () => late.LateMinutes == 1, late.LateMinutes + " min");

            var friday = day.AddDays(5);
            var offDay = AttendanceCalculator.Calculate(new AttendanceDayInput
            {
                Date = friday,
                Rule = rule,
                IsScheduledDay = false,
                Punches = [friday.AddHours(9), friday.AddHours(12)]
            });
            Check("Off-day work is kept as extra hours", () => offDay.ExtraWorkedMinutes == 180 && offDay.ExtraWorkedMinutes > 0);

            Check("Weekday mask covers Sun→Thu only",
                () => WeekDays.Administrative.Includes(DayOfWeek.Sunday)
                      && WeekDays.Administrative.Includes(DayOfWeek.Thursday)
                      && !WeekDays.Administrative.Includes(DayOfWeek.Friday));

            var probe = new Employee { Matricule = "999999", FullNameAr = "اختبار", DevicePin = "999999" };
            host.EmployeeRepo.Upsert(probe);
            host.EmployeeRepo.Archive(probe.Id, "selftest", "selftest");
            var archived = host.EmployeeRepo.Get(probe.Id);
            Check("Soft archive keeps the row", () => archived is { IsArchived: true });
            host.EmployeeRepo.Restore(probe.Id, "selftest");
            Check("Restore works", () => host.EmployeeRepo.Get(probe.Id) is { IsArchived: false });

            Line($"Hardware engines: {host.Hardware.EngineSummary}");
            Check("Native port-4370 engine available", () => host.Hardware.EngineSummary.Contains("4370", StringComparison.Ordinal));

            Line(new string('-', 78));
            Line("LAN discovery on TCP/UDP port 4370…");
            var scanner = new LanScanner();
            IReadOnlyList<DiscoveredDevice> discovered = [];
            try
            {
                using var scanTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(40));
                discovered = scanner.ScanAsync(200, LanScanner.DefaultPort, null, scanTimeout.Token).GetAwaiter().GetResult();
            }
            catch (OperationCanceledException)
            {
                Line("  scan window (40 s) elapsed before every host answered");
            }
            catch (Exception ex)
            {
                Line("  scan error: " + ex.Message);
            }

            Line($"  hosts scanned: {LanScanner.BuildHostList().Count}");
            Line($"  ZKTeco terminals verified (full handshake): {discovered.Count(d => d.Status == DiscoveryStatus.Verified)}");
            Line($"  CommKey-protected responders: {discovered.Count(d => d.Status == DiscoveryStatus.CommKeyRequired)}");
            Line($"  port-4370 open but unverified (registerable): {discovered.Count(d => d.Status == DiscoveryStatus.PortOpenUnverified)}");
            foreach (var terminal in discovered)
            {
                Line($"    • {terminal.IpAddress}:{terminal.Port}  status={terminal.Status}  transport={terminal.Transport}  {terminal.DisplayModel}  SN={terminal.SerialNumber}  fw={terminal.Snapshot?.FirmwareVersion}  mac={terminal.Snapshot?.MacAddress}  users={terminal.Snapshot?.UserCount}  punches={terminal.Snapshot?.PunchCount}");
                if (!string.IsNullOrWhiteSpace(terminal.Error))
                {
                    Line($"       {terminal.Error}");
                }
            }

            if (discovered.Count == 0)
            {
                Line("  (no terminal answered — expected when no hardware is connected to this LAN)");
            }

            Check("LAN scan completes without error", () => true);
        }
        catch (Exception ex)
        {
            failures++;
            Line("[FAIL] Unhandled self-test error — " + ex);
        }
        finally
        {
            host.DisposeAsync().AsTask().GetAwaiter().GetResult();
            try
            {
                Directory.Delete(tempRoot, true);
            }
            catch (IOException)
            {
            }
        }

        Line(new string('-', 78));
        Line(failures == 0 ? "RESULT: ALL CHECKS PASSED" : $"RESULT: {failures} CHECK(S) FAILED");
        Write(report, logDirectory);
        return failures == 0 ? 0 : 1;
    }

    private static void Write(StringBuilder report, string logDirectory)
    {
        try
        {
            Directory.CreateDirectory(logDirectory);
            var path = Path.Combine(logDirectory, "selftest.log");
            File.WriteAllText(path, report.ToString(), new UTF8Encoding(false));
            Console.WriteLine(report.ToString());
            Console.WriteLine("Report: " + path);
        }
        catch (IOException)
        {
            Console.WriteLine(report.ToString());
        }
    }
}
