using System.IO;
using System.Windows;
using System.Windows.Threading;
using FocusAttendance.App.Infrastructure;
using FocusAttendance.App.Views;
using FocusAttendance.Application;
using FocusAttendance.Application.ViewModels;

namespace FocusAttendance.App;

public partial class App : System.Windows.Application
{
    private bool _switchingUser;

    public static AppHost Host { get; private set; } = null!;

    public static AppUi Ui { get; } = new();

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        DispatcherUnhandledException += OnUnhandledException;

        try
        {
            if (e.Args.Any(a => string.Equals(a, "--selftest", StringComparison.OrdinalIgnoreCase)))
            {
                var code = SelfTest.Run(Path.Combine(AppHost.DefaultDataDirectory, "Logs"));
                Shutdown(code);
                return;
            }

            Host = new AppHost(AppHost.DefaultDataDirectory);
            Host.Initialize();

            var dark = string.Equals(Host.Settings.GetTheme(), "Dark", StringComparison.OrdinalIgnoreCase);
            ThemeService.Apply(dark);
            LanguageManager.Apply(Host.Settings.GetLanguage() != "fr");

            ShowLogin();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Focus Attendance could not start.\n\n{ex.Message}",
                "Focus Attendance",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown(-1);
        }
    }

    private void ShowLogin()
    {
        var viewModel = new LoginViewModel(Host);
        var window = new LoginWindow(viewModel);
        var authenticated = window.ShowDialog() == true;

        if (!authenticated)
        {
            Shutdown();
            return;
        }

        StartShell();
    }

    private void StartShell()
    {
        LanguageManager.Apply(Host.Session.IsArabic);
        ThemeService.Apply(string.Equals(Host.Settings.GetTheme(), "Dark", StringComparison.OrdinalIgnoreCase));

        var dashboard = new DashboardViewModel(Host, Ui);
        var devices = new DevicesViewModel(Host, Ui);
        var employees = new EmployeesViewModel(Host, Ui);
        var attendance = new AttendanceViewModel(Host, Ui);
        var settings = new SettingsViewModel(Host, Ui);

        var shell = new MainWindow(Host, Ui, dashboard, devices, employees, attendance, settings);
        shell.LogoutRequested += OnLogout;
        shell.Closed += OnShellClosed;

        MainWindow = shell;
        shell.Show();
        Host.StartMonitoring();

        if (Host.Session.MustChangePassword)
        {
            Ui.Notify(
                LanguageManager.IsArabic
                    ? "الحساب الأولي ما زال يستعمل كلمة المرور الافتراضية. غيّرها من الإعدادات."
                    : "Le compte initial utilise encore le mot de passe par défaut. Modifiez-le dans Paramètres.",
                UiSeverity.Warning);
        }
    }

    private async void OnLogout()
    {
        _switchingUser = true;
        try
        {
            await Host.Monitor.StopAsync().ConfigureAwait(true);
            Host.Session.SignOut();
            MainWindow?.Close();
            MainWindow = null;
        }
        finally
        {
            _switchingUser = false;
        }

        ShowLogin();
    }

    private void OnShellClosed(object? sender, EventArgs e)
    {
        if (!_switchingUser)
        {
            Shutdown();
        }
    }

    private void OnUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        Host?.Logger.Error("Unhandled UI exception.", e.Exception);
        Ui.Notify(e.Exception.Message, UiSeverity.Error);
        e.Handled = true;
    }

    protected override void OnExit(ExitEventArgs e)
    {
        try
        {
            Host?.DisposeAsync().AsTask().Wait(TimeSpan.FromSeconds(5));
        }
        catch (AggregateException)
        {
            // Shutdown must never throw.
        }

        base.OnExit(e);
    }
}
