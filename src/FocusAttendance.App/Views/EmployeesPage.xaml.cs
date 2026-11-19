using System.Windows;
using System.Windows.Controls;
using FocusAttendance.Application;
using FocusAttendance.Application.ViewModels;
using FocusAttendance.App.Infrastructure;
using Microsoft.Win32;

namespace FocusAttendance.App.Views;

public partial class EmployeesPage : UserControl
{
    private readonly EmployeesViewModel _viewModel;
    private readonly AppUi _ui;

    public EmployeesPage(EmployeesViewModel viewModel, AppUi ui)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _ui = ui;
        DataContext = viewModel;
    }

    public Task ActivateAsync() => _viewModel.RefreshAsync();

    private void OnGridDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (EmployeeGrid.SelectedItem is EmployeeRowVm row)
        {
            _viewModel.EditCommand.Execute(row);
        }
    }

    private void OnEditRowClick(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is EmployeeRowVm row)
        {
            _viewModel.EditCommand.Execute(row);
        }
    }

    private async void OnPushClick(object sender, RoutedEventArgs e)
    {
        if (await _ui.ConfirmAsync(Texts.Get("Employees.Title"), Texts.Get("Employees.PushConfirm"),
                Texts.Get("App.Confirm"), Texts.Get("App.Cancel")))
        {
            _viewModel.PushCommand.Execute(null);
        }
    }

    private async void OnDeleteDeviceClick(object sender, RoutedEventArgs e)
    {
        if (await _ui.ConfirmAsync(Texts.Get("Employees.Title"), Texts.Get("Employees.DeleteDeviceConfirm"),
                Texts.Get("App.Confirm"), Texts.Get("App.Cancel")))
        {
            _viewModel.DeleteFromDeviceCommand.Execute(null);
        }
    }

    private async void OnArchiveClick(object sender, RoutedEventArgs e)
    {
        if (await _ui.ConfirmAsync(Texts.Get("Employees.Title"), Texts.Get("Employees.ArchiveConfirm"),
                Texts.Get("App.Confirm"), Texts.Get("App.Cancel")))
        {
            _viewModel.ArchiveCommand.Execute(null);
        }
    }

    private void OnExportClick(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Filter = "CSV (*.csv)|*.csv",
            FileName = $"employes-{DateTime.Now:yyyyMMdd}.csv"
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        _viewModel.ExportCsv(dialog.FileName);
        _ui.Notify(Texts.Get("App.Saved"), UiSeverity.Success);
    }
}
