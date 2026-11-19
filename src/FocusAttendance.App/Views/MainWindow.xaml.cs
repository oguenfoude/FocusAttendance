using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using FocusAttendance.App.Infrastructure;
using FocusAttendance.Application;
using FocusAttendance.Application.ViewModels;
using FocusAttendance.Domain;
using Wpf.Ui.Controls;

namespace FocusAttendance.App.Views;

public partial class MainWindow : FluentWindow
{
    private readonly AppHost _host;
    private readonly AppUi _ui;
    private readonly DashboardViewModel _dashboard;
    private readonly DevicesViewModel _devices;
    private readonly EmployeesViewModel _employees;
    private readonly AttendanceViewModel _attendance;
    private readonly SettingsViewModel _settings;

    private readonly Dictionary<string, UserControl> _pages = new(StringComparer.Ordinal);
    private readonly Dictionary<string, RadioButton> _navButtons;
    private readonly DispatcherTimer _clock;
    private string _currentKey = string.Empty;

    public MainWindow(
        AppHost host,
        AppUi ui,
        DashboardViewModel dashboard,
        DevicesViewModel devices,
        EmployeesViewModel employees,
        AttendanceViewModel attendance,
        SettingsViewModel settings)
    {
        InitializeComponent();
        _host = host;
        _ui = ui;
        _dashboard = dashboard;
        _devices = devices;
        _employees = employees;
        _attendance = attendance;
        _settings = settings;

        _navButtons = new Dictionary<string, RadioButton>(StringComparer.Ordinal)
        {
            ["dashboard"] = NavDashboard,
            ["devices"] = NavDevices,
            ["employees"] = NavEmployees,
            ["attendance"] = NavAttendance,
            ["settings"] = NavSettings
        };

        _ui.Attach(SnackbarHost, DialogHost);

        _pages["dashboard"] = new DashboardPage(dashboard);
        _pages["devices"] = new DevicesPage(devices, ui);
        _pages["employees"] = new EmployeesPage(employees, ui);
        _pages["attendance"] = new AttendancePage(attendance, ui);
        _pages["settings"] = new SettingsPage(settings, ui);

        WireEvents();
        UpdateChrome();

        _clock = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _clock.Tick += (_, _) => UpdateClock();
        _clock.Start();
        UpdateClock();

        Loaded += async (_, _) =>
        {
            WindowState = WindowState.Maximized;
            ShowPage("dashboard");
            await RefreshAllAsync();
        };

        PreviewKeyDown += OnPreviewKeyDown;
        Closed += OnClosedHandler;
    }

    public event Action? LogoutRequested;

    private void WireEvents()
    {
        LanguageManager.Changed += UpdateChrome;

        _dashboard.ScanCompleted += async found =>
        {
            _devices.ApplyScanResults(found);
            NavigateTo("devices");
            await _devices.RefreshAsync();
        };

        _employees.NavigateRequested += NavigateTo;

        _settings.AppearanceChanged += async () =>
        {
            ThemeService.Apply(string.Equals(_host.Settings.GetTheme(), "Dark", StringComparison.OrdinalIgnoreCase));
            LanguageManager.Apply(_host.Session.IsArabic);
            UpdateChrome();
            await RefreshAllAsync();
        };

        _settings.OpenFolderRequested += OpenFolder;
        _settings.DataChanged += async () => await _dashboard.RefreshAsync();
        _devices.DataChanged += async () => await _dashboard.RefreshAsync();

        _host.Monitor.StatusChanged += _ => Dispatcher.Invoke(OnMonitorStatusChanged);
        _host.Monitor.PunchesPulled += (_, inserted) => Dispatcher.Invoke(() => OnPunchesPulled(inserted));
    }

    private void OnMonitorStatusChanged()
    {
        UpdateMonitorText();
        _devices.RefreshStatuses();
        _ = _dashboard.RefreshAsync();
    }

    private void OnPunchesPulled(int inserted)
    {
        _ui.Notify(
            LanguageManager.IsArabic
                ? $"تم سحب {inserted} بصمة جديدة تلقائياً."
                : $"{inserted} nouveau(x) pointage(s) récupéré(s) automatiquement.",
            UiSeverity.Success);
        UpdateMonitorText();
        _ = _dashboard.RefreshAsync();
    }

    private async Task RefreshAllAsync()
    {
        await _dashboard.RefreshAsync();
        await _devices.RefreshAsync();
        await _employees.RefreshAsync();
        await _attendance.RefreshAsync();
        await _settings.RefreshAsync();
        UpdateChrome();
        _host.Logger.Info("Workspaces refreshed.");
    }

    private void OnNavChecked(object sender, RoutedEventArgs e)
    {
        if (sender is RadioButton { Tag: string key } && _pages.Count > 0)
        {
            ShowPage(key);
        }
    }

    private void ShowPage(string key)
    {
        if (!_pages.TryGetValue(key, out var page))
        {
            return;
        }

        if (ReferenceEquals(PageHost.Content, page))
        {
            _ = ActivateAsync(key);
            return;
        }

        _currentKey = key;
        PageHost.Content = page;

        if (page is FrameworkElement element)
        {
            var slide = new TranslateTransform(0, 12);
            element.RenderTransform = slide;
            element.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(180))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            });
            slide.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(12, 0, TimeSpan.FromMilliseconds(180))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            });
        }

        _ = ActivateAsync(key);
    }

    private void NavigateTo(string key)
    {
        if (!_pages.ContainsKey(key))
        {
            key = "dashboard";
        }

        if (_navButtons.TryGetValue(key, out var button))
        {
            if (button.IsChecked == true)
            {
                ShowPage(key);
            }
            else
            {
                button.IsChecked = true;
            }

            return;
        }

        ShowPage(key);
    }

    private Task ActivateAsync(string key) => key switch
    {
        "devices" => _devices.RefreshAsync(),
        "employees" => _employees.RefreshAsync(),
        "attendance" => _attendance.RefreshAsync(),
        "settings" => _settings.RefreshAsync(),
        _ => _dashboard.RefreshAsync()
    };

    private void OnPreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if ((System.Windows.Input.Keyboard.Modifiers & System.Windows.Input.ModifierKeys.Control) == 0)
        {
            return;
        }

        var key = e.Key switch
        {
            System.Windows.Input.Key.D1 or System.Windows.Input.Key.NumPad1 => "dashboard",
            System.Windows.Input.Key.D2 or System.Windows.Input.Key.NumPad2 => "devices",
            System.Windows.Input.Key.D3 or System.Windows.Input.Key.NumPad3 => "employees",
            System.Windows.Input.Key.D4 or System.Windows.Input.Key.NumPad4 => "attendance",
            System.Windows.Input.Key.D5 or System.Windows.Input.Key.NumPad5 => "settings",
            _ => null
        };

        if (key is null)
        {
            return;
        }

        NavigateTo(key);
        e.Handled = true;
    }

    private void OnLanguageClick(object sender, RoutedEventArgs e)
    {
        var arabic = !_host.Session.IsArabic;
        _host.Session.IsArabic = arabic;
        _host.Settings.SetLanguage(arabic ? "ar" : "fr");
        LanguageManager.Apply(arabic);
        UpdateChrome();
        _ = RefreshAllAsync();
    }

    private void OnThemeClick(object sender, RoutedEventArgs e)
    {
        var dark = !ThemeService.IsDark;
        _host.Settings.SetTheme(dark ? "Dark" : "Light");
        ThemeService.Apply(dark);
        UpdateChrome();
    }

    private void OnLogoutClick(object sender, RoutedEventArgs e) => LogoutRequested?.Invoke();

    private void UpdateChrome()
    {
        var arabic = _host.Session.IsArabic;
        Root.FlowDirection = arabic ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
        SidebarLanguage.Text = arabic ? "Français" : "العربية";
        SidebarThemeIcon.Symbol = ThemeService.IsDark ? SymbolRegular.DarkTheme24 : SymbolRegular.WeatherSunny24;

        var user = _host.Session.User;
        var name = user?.DisplayName ?? "—";
        UserName.Text = name;
        UserRoleText.Text = user?.Role == UserRole.SuperAdmin
            ? (arabic ? "مشرف عام" : "SuperAdmin")
            : (arabic ? "مشغل" : "Opérateur");
        UserInitials.Text = string.IsNullOrWhiteSpace(name)
            ? "OP"
            : new string(name.Where(char.IsLetter).Take(2).ToArray()).ToUpperInvariant();

        StatusDbText.Text = $"SQLite • WAL • {_host.Db.DatabasePath}";
        UpdateMonitorText();

        var sites = _host.Organization.GetSites();
        var site = sites.Count > 0 ? sites[0] : null;
        BrandBranch.Text = site is null
            ? (arabic ? "ديوان الترقية والتسيير العقاري — المسيلة" : "OPGI — Direction Générale de M'Sila")
            : (arabic ? site.NameAr : site.NameFr);
    }

    private void UpdateMonitorText()
    {
        var arabic = LanguageManager.IsArabic;
        MonitorText.Text = _host.Monitor.IsRunning
            ? (arabic ? $"المراقبة التلقائية نشطة ({_host.Monitor.IntervalSeconds} ثا)" : $"Surveillance auto active ({_host.Monitor.IntervalSeconds} s)")
            : (arabic ? "المراقبة التلقائية متوقفة" : "Surveillance auto arrêtée");
    }

    private void UpdateClock() =>
        ClockText.Text = DateTime.Now.ToString("ddd dd/MM/yyyy  HH:mm:ss", CultureInfo.GetCultureInfo("fr-FR"));

    private void OpenFolder(string path)
    {
        try
        {
            Directory.CreateDirectory(path);
            Process.Start(new ProcessStartInfo("explorer.exe", $"\"{path}\"") { UseShellExecute = true });
        }
        catch (Exception ex) when (ex is IOException or System.ComponentModel.Win32Exception or UnauthorizedAccessException)
        {
            _ui.Notify(ex.Message, UiSeverity.Error);
        }
    }

    private void OnClosedHandler(object? sender, EventArgs e)
    {
        LanguageManager.Changed -= UpdateChrome;
        _clock.Stop();
    }
}
