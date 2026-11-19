using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using FocusAttendance.App.Converters;
using FocusAttendance.App.Infrastructure;
using FocusAttendance.App.Views;
using FocusAttendance.Application;
using FocusAttendance.Application.Services;
using FocusAttendance.Application.ViewModels;
using FocusAttendance.Domain;
using FocusAttendance.Domain.Entities;
using Wpf.Ui.Appearance;
using Wpf.Ui.Markup;

namespace FocusAttendance.UiRender;

/// <summary>
/// Deterministic UI smoke renderer. Builds each workspace on a throw-away database with
/// representative data, then renders it to PNG so layout and XAML runtime errors are caught
/// without driving the real application.
/// </summary>
internal static class Program
{
    private const int Width = 1440;
    private const int Height = 900;

    [STAThread]
    private static int Main(string[] args)
    {
        var output = args.Length > 0 ? args[0] : Path.Combine(AppContext.BaseDirectory, "shots");
        Directory.CreateDirectory(output);

        var application = new System.Windows.Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        application.Resources.MergedDictionaries.Add(new ThemesDictionary { Theme = ApplicationTheme.Light });
        application.Resources.MergedDictionaries.Add(new ControlsDictionary());
        application.Resources.MergedDictionaries.Add(Dictionary("Brand.Light.xaml"));
        application.Resources.MergedDictionaries.Add(Dictionary("Styles.xaml"));
        application.Resources.MergedDictionaries.Add(Dictionary("Lang.ar.xaml"));
        AddConverters(application);

        var dataDirectory = Path.Combine(Path.GetTempPath(), "focus-ui-render", Guid.NewGuid().ToString("N"));
        var host = new AppHost(dataDirectory);
        host.Initialize(createStartupBackup: false);
        Seed(host);

        var messenger = new AppUi();
        var dashboard = new DashboardViewModel(host, messenger);
        var devices = new DevicesViewModel(host, messenger);
        var employees = new EmployeesViewModel(host, messenger);
        var attendance = new AttendanceViewModel(host, messenger);
        var settings = new SettingsViewModel(host, messenger);

        RenderPage(output, "01-dashboard-ar", new DashboardPage(dashboard), dashboard.RefreshAsync);
        RenderPage(output, "02-devices-ar", new DevicesPage(devices, messenger), devices.RefreshAsync);
        RenderPage(output, "03-employees-ar", new EmployeesPage(employees, messenger), employees.RefreshAsync);
        RenderPage(output, "04-attendance-ar", new AttendancePage(attendance, messenger), attendance.RefreshAsync);
        RenderPage(output, "05-settings-ar", new SettingsPage(settings, messenger), settings.RefreshAsync);

        // Employee drawer with a selected record.
        employees.EditCommand.Execute(employees.Employees.FirstOrDefault());
        RenderPage(output, "06-employee-drawer-ar", new EmployeesPage(employees, messenger), () => Task.CompletedTask);

        // French / LTR dashboard.
        LanguageManager.Apply(false);
        host.Session.IsArabic = false;
        RenderPage(output, "07-dashboard-fr", new DashboardPage(dashboard), dashboard.RefreshAsync);

        // Dark theme dashboard.
        ThemeService.Apply(true);
        RenderPage(output, "08-dashboard-dark-ar", new DashboardPage(dashboard), dashboard.RefreshAsync);

        Console.WriteLine($"Rendered to {output}");
        host.DisposeAsync().AsTask().GetAwaiter().GetResult();
        try
        {
            Directory.Delete(dataDirectory, true);
        }
        catch (IOException)
        {
        }

        return 0;
    }

    private static ResourceDictionary Dictionary(string name) => new()
    {
        Source = new Uri($"pack://application:,,,/FocusAttendance;component/Resources/{name}", UriKind.Absolute)
    };

    private static void AddConverters(System.Windows.Application application)
    {
        application.Resources["BoolToVisibility"] = new BoolToVisibilityConverter();
        application.Resources["InverseBoolToVisibility"] = new BoolToVisibilityConverter { Invert = true };
        application.Resources["CountToVisibility"] = new CountToVisibilityConverter();
        application.Resources["ZeroCountToVisibility"] = new CountToVisibilityConverter { Invert = true };
        application.Resources["NullToVisibility"] = new NullToVisibilityConverter();
        application.Resources["NotNullToVisibility"] = new NullToVisibilityConverter { Invert = true };
        application.Resources["InverseBool"] = new InverseBoolConverter();
        application.Resources["StringEmptyToVisibility"] = new StringEmptyToVisibilityConverter();
        application.Resources["EqualityToBool"] = new EqualityToBoolConverter();
        application.Resources["FlowToTextAlignment"] = new FlowDirectionToTextAlignmentConverter();
    }

    private static void RenderPage(string output, string name, UserControl page, Func<Task> activate)
    {
        var task = activate();
        Pump(task);
        Console.WriteLine($"{name}: dataContext={page.DataContext?.GetType().Name ?? "null"}");
        if (page.DataContext is AttendanceViewModel attendance)
        {
            Console.WriteLine($"  rows={attendance.Rows.Count} summary={attendance.SummaryText}");
        }

        if (page.DataContext is SettingsViewModel settings)
        {
            Console.WriteLine($"  rules={settings.Rules.Count} departments={settings.Departments.Count}");
        }

        if (page.DataContext is EmployeesViewModel employees)
        {
            Console.WriteLine($"  employees={employees.Employees.Count}");
        }

        page.FlowDirection = LanguageManager.IsArabic ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
        var root = new Border
        {
            Background = (Brush)System.Windows.Application.Current.Resources["AppBgGradientBrush"],
            Padding = new Thickness(18),
            Width = Width,
            Height = Height,
            FlowDirection = LanguageManager.IsArabic ? FlowDirection.RightToLeft : FlowDirection.LeftToRight,
            Child = page
        };

        TextOptions.SetTextFormattingMode(root, TextFormattingMode.Display);

        root.Measure(new Size(Width, Height));
        root.Arrange(new Rect(0, 0, Width, Height));
        root.UpdateLayout();

        // A second pass after the first layout: combo-box selections settle during parsing.
        var second = activate();
        Pump(second);
        root.UpdateLayout();
        Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.Render);
        root.UpdateLayout();

        var bitmap = new RenderTargetBitmap(Width, Height, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(root);

        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        var path = Path.Combine(output, name + ".png");
        using var stream = File.Create(path);
        encoder.Save(stream);
        Console.WriteLine("rendered " + path);
    }

    private static void Pump(Task task)
    {
        var frame = new DispatcherFrame();
        var start = DateTime.UtcNow;
        var timer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromMilliseconds(50)
        };
        timer.Tick += (_, _) =>
        {
            var elapsed = DateTime.UtcNow - start;
            if ((task.IsCompleted && elapsed > TimeSpan.FromMilliseconds(600)) || elapsed > TimeSpan.FromSeconds(15))
            {
                timer.Stop();
                frame.Continue = false;
            }
        };
        timer.Start();
        Dispatcher.PushFrame(frame);
        Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
        if (task.IsFaulted)
        {
            Console.WriteLine("ACTIVATION ERROR: " + task.Exception);
        }
    }

    private static void Seed(AppHost host)
    {
        var rule = host.Shifts.GetDefault();
        var departments = host.Organization.GetDepartments();
        var site = host.Organization.GetSites()[0];

        var deviceA = new Device
        {
            Name = "Réception — UF100 Plus-S",
            IpAddress = "192.168.1.50",
            SerialNumber = "UF100-2311-0042",
            ModelName = "UF100 Plus-S",
            FirmwareVersion = "6.60.1.0",
            Platform = "ZEM560",
            MacAddress = "00:17:61:0A:0B:0C",
            UserCount = 34,
            FingerCount = 78,
            FaceCount = 34,
            PunchCount = 1284,
            LastSeenUtc = DateTime.UtcNow,
            LastSyncUtc = DateTime.UtcNow,
            LastPullUtc = DateTime.UtcNow,
            DeviceClock = DateTime.Now
        };
        var deviceB = new Device
        {
            Name = "Entrée DRH",
            IpAddress = "192.168.1.51",
            SerialNumber = "K14-2209-0117",
            ModelName = "K14",
            FirmwareVersion = "6.58.0.1",
            Platform = "ZEM510",
            UserCount = 21,
            FingerCount = 42,
            PunchCount = 640,
            LastSeenUtc = DateTime.UtcNow.AddMinutes(-12),
            DeviceClock = DateTime.Now.AddMinutes(-3)
        };
        host.DeviceRepo.Upsert(deviceA);
        host.DeviceRepo.Upsert(deviceB);

        var people = new (string Matricule, string Pin, string Ar, string Fr1, string Fr2, string JobAr, string JobFr, string Dept)[]
        {
            ("000101", "1001", "بن علي محمد", "Mohamed", "Benali", "رئيس مصلحة", "Chef de service", "DRH"),
            ("000102", "1002", "عمراني فاطمة", "Fatima", "Amrani", "محاسبة", "Comptable", "DFC"),
            ("000103", "1003", "زياتي يوسف", "Youcef", "Ziati", "مهندس", "Ingénieur", "DGI"),
            ("000104", "1004", "خليفي سعاد", "Souad", "Khelifi", "أمينة", "Secrétaire", "DG"),
            ("000105", "1005", "بوزيدي كريم", "Karim", "Bouzidi", "تقني سامي", "Technicien", "CI"),
            ("000106", "1006", "حمداني ليلى", "Leila", "Hamdani", "مكلفة بالوسائل", "Chargée des moyens", "DMO")
        };

        var index = 0;
        foreach (var person in people)
        {
            var department = departments.FirstOrDefault(d => d.Code == person.Dept);
            var employee = new Employee
            {
                Matricule = person.Matricule,
                DevicePin = person.Pin,
                FullNameAr = person.Ar,
                FirstNameFr = person.Fr1,
                LastNameFr = person.Fr2,
                JobTitleAr = person.JobAr,
                JobTitleFr = person.JobFr,
                SiteId = site.Id,
                DepartmentId = department?.Id,
                WorkRuleId = rule.Id,
                ScheduledWorkDays = WeekDays.Administrative,
                HireDate = new DateTime(2019, 3, 4).AddDays(index * 120),
                Phone = $"05{index + 5} 42 18 {20 + index}",
                BaseSalaryDzd = 42000 + (index * 3500),
                DailyRateDzd = 0,
                HourlyRateDzd = 0,
                OvertimeMultiplier = 1.5m,
                PrimaryDeviceId = index % 2 == 0 ? deviceA.Id : deviceB.Id,
                DevicePrivilege = index == 0 ? 6 : 0,
                CardNumber = 1000 + index,
                FpCount = 2,
                FaceCount = 1
            };

            host.EmployeeRepo.Upsert(employee);
            index++;
        }

        var today = DateTime.Today;
        var punches = new List<Punch>();
        var serialA = deviceA.SerialNumber!;

        void Punch(string pin, DateTime moment)
        {
            var local = moment;
            var utc = DateTime.SpecifyKind(local, DateTimeKind.Local).ToUniversalTime();
            punches.Add(new Punch
            {
                EmployeeId = host.EmployeeRepo.GetByPin(pin)?.Id,
                DevicePin = pin,
                DeviceSerial = serialA,
                DeviceId = deviceA.Id,
                PunchTimeLocal = local,
                PunchTimeUtc = utc,
                DedupeKey = $"{serialA}|{pin}|{utc:O}",
                Source = PunchSource.Device,
                VerifyMode = 15,
                PunchState = 0
            });
        }

        // Yesterday: complete 4-scan day.
        var yesterday = today.AddDays(-1);
        Punch("1001", yesterday.AddHours(7).AddMinutes(55));
        Punch("1001", yesterday.AddHours(12).AddMinutes(2));
        Punch("1001", yesterday.AddHours(13).AddMinutes(4));
        Punch("1001", yesterday.AddHours(16).AddMinutes(35));

        // Today: varied states.
        Punch("1001", today.AddHours(7).AddMinutes(52));
        Punch("1001", today.AddHours(12).AddMinutes(1));
        Punch("1001", today.AddHours(13).AddMinutes(3));
        Punch("1001", today.AddHours(16).AddMinutes(33));

        Punch("1002", today.AddHours(8).AddMinutes(24));
        Punch("1002", today.AddHours(12).AddMinutes(6));
        Punch("1002", today.AddHours(13).AddMinutes(21));
        Punch("1002", today.AddHours(16).AddMinutes(41));

        Punch("1003", today.AddHours(7).AddMinutes(58));
        Punch("1003", today.AddHours(12).AddMinutes(0));

        Punch("1004", today.AddHours(8).AddMinutes(41));
        Punch("1004", today.AddHours(12).AddMinutes(45));

        Punch("1005", today.AddHours(8).AddMinutes(5));
        Punch("1005", today.AddHours(16).AddMinutes(20));

        // Off-day work (yesterday was a working day, so use the last Friday).
        var friday = today;
        while (friday.DayOfWeek != DayOfWeek.Friday)
        {
            friday = friday.AddDays(-1);
        }

        Punch("1006", friday.AddHours(9));
        Punch("1006", friday.AddHours(12).AddMinutes(30));

        host.PunchRepo.InsertBatch(punches);

        var mission = new AttendanceAdjustment
        {
            EmployeeId = host.EmployeeRepo.GetByPin("1006")!.Id,
            WorkDate = today,
            Kind = AdjustmentKind.Mission,
            StartLocal = today.AddHours(8),
            EndLocal = today.AddHours(12),
            HoursCreditedMinutes = 240,
            Reference = "OM-2026-114",
            Note = "Mission à Bordj Bou Arreridj",
            CreatedBy = "admin"
        };
        host.AdjustmentRepo.Upsert(mission);

        host.Settings.SetTheme("Light");
        Console.WriteLine($"seeded: employees={host.EmployeeRepo.Count()} devices={host.DeviceRepo.Count()} punches={host.PunchRepo.CountAll()}");
    }


}
