using System.Windows.Controls;
using System.Windows.Threading;
using FocusAttendance.Application.ViewModels;

namespace FocusAttendance.App.Views;

public partial class DashboardPage : UserControl
{
    private readonly DashboardViewModel _viewModel;
    private readonly DispatcherTimer _timer;

    public DashboardPage(DashboardViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;

        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(12) };
        _timer.Tick += async (_, _) => await _viewModel.RefreshAsync();

        Loaded += async (_, _) =>
        {
            await _viewModel.RefreshAsync();
            _timer.Start();
        };
        Unloaded += (_, _) => _timer.Stop();
    }

    public Task ActivateAsync() => _viewModel.RefreshAsync();
}
