using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using FocusAttendance.Application;
using FocusAttendance.Application.ViewModels;
using FocusAttendance.App.Infrastructure;

namespace FocusAttendance.App.Views;

public partial class SettingsPage : UserControl
{
    private readonly SettingsViewModel _viewModel;
    private readonly AppUi _ui;

    public SettingsPage(SettingsViewModel viewModel, AppUi ui)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _ui = ui;
        DataContext = viewModel;
    }

    public Task ActivateAsync() => _viewModel.RefreshAsync();

    private void OnRuleDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is DataGrid { SelectedItem: WorkRuleRowVm row })
        {
            _viewModel.EditRuleCommand.Execute(row);
        }
    }

    private void OnEditRuleClick(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is WorkRuleRowVm row)
        {
            _viewModel.EditRuleCommand.Execute(row);
        }
    }

    private async void OnArchiveRuleClick(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is not WorkRuleRowVm row)
        {
            return;
        }

        if (await _ui.ConfirmAsync(Texts.Get("Settings.Shifts"), Texts.Get("App.Archive") + " ?",
                Texts.Get("App.Confirm"), Texts.Get("App.Cancel")))
        {
            _viewModel.ArchiveRuleCommand.Execute(row);
        }
    }

    private void OnRestoreRuleClick(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is WorkRuleRowVm row)
        {
            _viewModel.RestoreRuleCommand.Execute(row);
        }
    }

    private void OnMakeDefaultRuleClick(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is WorkRuleRowVm row)
        {
            _viewModel.MakeDefaultRuleCommand.Execute(row);
        }
    }

    private void OnDepartmentDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is DataGrid { SelectedItem: DepartmentRowVm row })
        {
            _viewModel.EditDepartmentCommand.Execute(row);
        }
    }

    private void OnEditDepartmentClick(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is DepartmentRowVm row)
        {
            _viewModel.EditDepartmentCommand.Execute(row);
        }
    }

    private async void OnArchiveDepartmentClick(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is not DepartmentRowVm row)
        {
            return;
        }

        if (await _ui.ConfirmAsync(Texts.Get("Settings.Services"), Texts.Get("App.Archive") + " ?",
                Texts.Get("App.Confirm"), Texts.Get("App.Cancel")))
        {
            _viewModel.ArchiveDepartmentCommand.Execute(row);
        }
    }

    private void OnRestoreDepartmentClick(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is DepartmentRowVm row)
        {
            _viewModel.RestoreDepartmentCommand.Execute(row);
        }
    }

    private async void OnRestoreBackupClick(object sender, RoutedEventArgs e)
    {
        if (_viewModel.SelectedBackup is null)
        {
            _ui.Notify(Texts.Get("App.None"), UiSeverity.Warning);
            return;
        }

        if (await _ui.ConfirmAsync(Texts.Get("Settings.Database"), Texts.Get("Settings.RestoreSelected") + " ?",
                Texts.Get("App.Confirm"), Texts.Get("App.Cancel")))
        {
            _viewModel.RestoreBackupCommand.Execute(null);
        }
    }
}
