using System.Windows;
using System.Windows.Input;
using FocusAttendance.App.Infrastructure;
using FocusAttendance.Application.ViewModels;
using Wpf.Ui.Controls;

namespace FocusAttendance.App.Views;

public partial class LoginWindow : FluentWindow
{
    private readonly LoginViewModel _viewModel;

    public LoginWindow(LoginViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;

        _viewModel.SignedIn += OnSignedIn;
        _viewModel.LanguageToggled += ApplyDirection;
        _viewModel.ThemeToggled += ApplyTheme;
        LanguageManager.Changed += ApplyDirection;
        Closed += OnClosed;

        ApplyDirection();
        ApplyTheme();
        Loaded += (_, _) => UsernameBox.Focus();
    }

    private void ApplyTheme()
    {
        ThemeService.Apply(_viewModel.IsDark);
        LoginThemeIcon.Symbol = ThemeService.IsDark ? SymbolRegular.DarkTheme24 : SymbolRegular.WeatherSunny24;
    }

    private void OnSignedIn()
    {
        DialogResult = true;
        Close();
    }

    private void ApplyDirection() =>
        Root.FlowDirection = LanguageManager.IsArabic ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;

    private void OnPasswordKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter || !_viewModel.LoginCommand.CanExecute(null))
        {
            return;
        }

        _viewModel.LoginCommand.Execute(null);
        e.Handled = true;
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        LanguageManager.Changed -= ApplyDirection;
        _viewModel.SignedIn -= OnSignedIn;
        _viewModel.LanguageToggled -= ApplyDirection;
        _viewModel.ThemeToggled -= ApplyTheme;
    }
}
