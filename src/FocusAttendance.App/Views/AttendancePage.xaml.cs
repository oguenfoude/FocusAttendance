using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using FocusAttendance.Application;
using FocusAttendance.Application.ViewModels;
using FocusAttendance.App.Infrastructure;
using Microsoft.Win32;

namespace FocusAttendance.App.Views;

public partial class AttendancePage : UserControl
{
    private readonly AttendanceViewModel _viewModel;
    private readonly AppUi _ui;

    public AttendancePage(AttendanceViewModel viewModel, AppUi ui)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _ui = ui;
        DataContext = viewModel;
    }

    public Task ActivateAsync() => _viewModel.RefreshAsync();

    private void OnRowDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (SheetGrid.SelectedItem is AttendanceRowVm row)
        {
            _viewModel.OpenDetailCommand.Execute(row);
        }
    }

    private async void OnArchiveAdjustmentClick(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is not AdjustmentRowVm row)
        {
            return;
        }

        if (await _ui.ConfirmAsync(Texts.Get("Attendance.Adjustments"), Texts.Get("Attendance.ArchiveConfirm"),
                Texts.Get("App.Confirm"), Texts.Get("App.Cancel")))
        {
            _viewModel.ArchiveAdjustmentCommand.Execute(row);
        }
    }

    private void OnExportCsvClick(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Filter = "CSV (*.csv)|*.csv",
            FileName = $"presence-{_viewModel.FromDate:yyyyMMdd}-{_viewModel.ToDate:yyyyMMdd}.csv"
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        _viewModel.ExportCsv(dialog.FileName);
        _ui.Notify(Texts.Get("App.Saved"), UiSeverity.Success);
    }

    private void OnPrintClick(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Filter = "HTML (*.html)|*.html",
            FileName = $"rapport-opgi-{_viewModel.FromDate:yyyyMMdd}-{_viewModel.ToDate:yyyyMMdd}.html"
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        var path = _viewModel.ExportHtml(dialog.FileName);
        try
        {
            Process.Start(new ProcessStartInfo($"file:///{path.Replace('\\', '/')}#print") { UseShellExecute = true });
        }
        catch (System.ComponentModel.Win32Exception ex)
        {
            _ui.Notify(ex.Message, UiSeverity.Error);
        }
    }
}
