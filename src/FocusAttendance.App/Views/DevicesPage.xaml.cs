using System.Windows;
using System.Windows.Controls;
using FocusAttendance.Application;
using FocusAttendance.Application.ViewModels;
using FocusAttendance.App.Infrastructure;

namespace FocusAttendance.App.Views;

public partial class DevicesPage : UserControl
{
    private readonly DevicesViewModel _viewModel;
    private readonly AppUi _ui;

    public DevicesPage(DevicesViewModel viewModel, AppUi ui)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _ui = ui;
        DataContext = viewModel;
    }

    public Task ActivateAsync() => _viewModel.RefreshAsync();

    private void OnOpenScannerClick(object sender, RoutedEventArgs e)
    {
        var window = new LanScannerWindow(_viewModel.Host, _ui)
        {
            Owner = Window.GetWindow(this)
        };
        window.ShowDialog();
        _ = _viewModel.RefreshAsync();
    }

    private void OnOpenConsole(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is DeviceCardVm card)
        {
            _viewModel.OpenDeviceCommand.Execute(card);
        }
    }

    private void OnPullRowClick(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is DeviceCardVm card)
        {
            _viewModel.SelectedDevice = card;
            _viewModel.PullSelectedCommand.Execute(null);
        }
    }

    private async void OnApplyNetworkClick(object sender, RoutedEventArgs e)
    {
        if (await _ui.ConfirmAsync(Texts.Get("Devices.Title"), Texts.Get("Devices.ApplyConfirm"),
                Texts.Get("App.Confirm"), Texts.Get("App.Cancel")))
        {
            _viewModel.ApplyNetworkCommand.Execute(null);
        }
    }

    private async void OnRebootClick(object sender, RoutedEventArgs e)
    {
        if (await _ui.ConfirmAsync(Texts.Get("Devices.Title"), Texts.Get("Devices.RebootConfirm"),
                Texts.Get("App.Confirm"), Texts.Get("App.Cancel")))
        {
            _viewModel.RebootCommand.Execute(null);
        }
    }

    private async void OnPowerOffClick(object sender, RoutedEventArgs e)
    {
        if (await _ui.ConfirmAsync(Texts.Get("Devices.Title"), Texts.Get("Devices.PowerConfirm"),
                Texts.Get("App.Confirm"), Texts.Get("App.Cancel")))
        {
            _viewModel.PowerOffCommand.Execute(null);
        }
    }

    private async void OnClearAdminsClick(object sender, RoutedEventArgs e)
    {
        if (await _ui.ConfirmAsync(Texts.Get("Devices.Title"), Texts.Get("Devices.ClearConfirm"),
                Texts.Get("App.Confirm"), Texts.Get("App.Cancel")))
        {
            _viewModel.ClearAdminsCommand.Execute(null);
        }
    }

    private async void OnArchiveDeviceClick(object sender, RoutedEventArgs e)
    {
        if (await _ui.ConfirmAsync(Texts.Get("Devices.Title"), Texts.Get("Devices.ArchiveConfirm"),
                Texts.Get("App.Confirm"), Texts.Get("App.Cancel")))
        {
            _viewModel.ArchiveCommand.Execute(null);
        }
    }

    private async void OnImportAllUsersClick(object sender, RoutedEventArgs e)
    {
        if (await _ui.ConfirmAsync(Texts.Get("Devices.Title"), Texts.Get("Devices.ImportConfirm"),
                Texts.Get("App.Confirm"), Texts.Get("App.Cancel")))
        {
            _viewModel.ImportAllUsersCommand.Execute(null);
        }
    }

    private void OnToggleEnabledClick(object sender, RoutedEventArgs e) =>
        _viewModel.ToggleEnabledCommand.Execute(null);
}
