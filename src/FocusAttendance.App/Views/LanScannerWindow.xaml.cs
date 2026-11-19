using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net;
using FocusAttendance.Application;
using FocusAttendance.Application.Mvvm;
using FocusAttendance.Application.ViewModels;
using FocusAttendance.App.Infrastructure;
using FocusAttendance.Hardware;
using Wpf.Ui.Controls;

namespace FocusAttendance.App.Views;

public sealed class ScanRow : ObservableObject
{
    private bool _added;

    public ScanRow(DiscoveredDevice device, bool arabic)
    {
        Device = device;
        Arabic = arabic;
        IpAddress = $"{device.IpAddress}:{device.Port}";
        CommKey = 0;
        Update(device);
    }

    public DiscoveredDevice Device { get; private set; }
    public bool Arabic { get; }
    public string IpAddress { get; }
    public int CommKey { get; set; }
    public string Serial => Device.SerialNumber ?? string.Empty;
    public string Model => Device.DisplayModel;
    public string Users => Device.Snapshot is { } us ? us.UserCount.ToString() : string.Empty;
    public string Punches => Device.Snapshot is { } ps ? ps.PunchCount.ToString() : string.Empty;
    public string Firmware => Device.Snapshot?.FirmwareVersion ?? string.Empty;
    public string Mac => Device.Snapshot?.MacAddress ?? string.Empty;
    public string Platform => Device.Snapshot?.Platform ?? string.Empty;
    public string Biometrics => Device.Snapshot is { } s && (s.UserCount + s.FingerCount + s.FaceCount) > 0
        ? $"{s.UserCount} users / {s.FingerCount} FP / {s.FaceCount} face"
        : string.Empty;
    public string Clock => Device.Snapshot?.DeviceClock?.ToString("dd/MM/yyyy HH:mm") ?? string.Empty;

    public string StatusDetail => string.IsNullOrWhiteSpace(Device.Error)
        ? $"{StatusText} · {Device.Transport ?? "TCP"}"
        : $"{StatusText} · {Device.Error}";

    public string StatusText => Device.Status switch
    {
        DiscoveryStatus.Verified when string.IsNullOrWhiteSpace(Device.SerialNumber) =>
            Arabic ? "ZKTeco متصل — بدون معرّف" : "Verified — no identity",
        DiscoveryStatus.Verified => Arabic ? "ZKTeco موثّق" : "Verified ZKTeco",
        DiscoveryStatus.CommKeyRequired => Arabic ? "المنفذ مفتوح — مطلوب مفتاح" : "Port open — CommKey required",
        DiscoveryStatus.PortOpenUnverified => Arabic ? "المنفذ مفتوح — غير موثّق" : "Port open — unverified",
        _ => Arabic ? "غير متاح" : "Unreachable"
    };

    public bool Added
    {
        get => _added;
        set
        {
            if (SetProperty(ref _added, value))
            {
                OnPropertyChanged(nameof(AddText), nameof(CanAdd));
            }
        }
    }

    public bool CanAdd => !Added;

    public string AddText => Added
        ? (Arabic ? "مضاف" : "Added")
        : (Arabic ? "إضافة" : "Add");

    public void Update(DiscoveredDevice device)
    {
        Device = device;
        OnPropertyChanged(
            nameof(Device),
            nameof(StatusText),
            nameof(StatusDetail),
            nameof(Serial),
            nameof(Model),
            nameof(Users),
            nameof(Punches),
            nameof(Firmware),
            nameof(Mac),
            nameof(Platform),
            nameof(Biometrics),
            nameof(Clock));
    }
}

public sealed record NetworkOption(string Label, string? Cidr, bool Custom);

public partial class LanScannerWindow : FluentWindow, INotifyPropertyChanged, IDisposable
{
    private readonly AppHost _host;
    private readonly AppUi _ui;
    private CancellationTokenSource? _cancellation;
    private ScanRow? _selectedRow;

    public LanScannerWindow(AppHost host, AppUi ui)
    {
        InitializeComponent();
        _host = host;
        _ui = ui;
        DataContext = this;
        LoadNetworks();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<ScanRow> Rows { get; } = [];

    public ScanRow? SelectedRow
    {
        get => _selectedRow;
        set
        {
            if (ReferenceEquals(_selectedRow, value))
            {
                return;
            }

            _selectedRow = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedRow)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HasSelection)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedSerial)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedModel)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedFirmware)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedMac)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedPlatform)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedBiometrics)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedClock)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedUsers)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedPunches)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedStatusDetail)));
        }
    }

    public bool HasSelection => _selectedRow is not null;
    public string SelectedSerial => _selectedRow?.Serial ?? "-";
    public string SelectedModel => _selectedRow?.Model ?? "-";
    public string SelectedFirmware => _selectedRow?.Firmware ?? "-";
    public string SelectedMac => _selectedRow?.Mac ?? "-";
    public string SelectedPlatform => _selectedRow?.Platform ?? "-";
    public string SelectedBiometrics => _selectedRow?.Biometrics ?? "-";
    public string SelectedClock => _selectedRow?.Clock ?? "-";
    public string SelectedUsers => _selectedRow?.Users ?? "-";
    public string SelectedPunches => _selectedRow?.Punches ?? "-";
    public string SelectedStatusDetail => _selectedRow?.StatusDetail ?? string.Empty;

    private void NotifySelection()
    {
        var current = _selectedRow;
        _selectedRow = null;
        SelectedRow = current;
    }

    private void LoadNetworks()
    {
        var arabic = LanguageManager.IsArabic;
        NetworkBox.Items.Add(new NetworkOption(arabic ? "كل الشبكات المحلية" : "All local networks", null, false));
        foreach (var network in LanScanner.GetLocalNetworks())
        {
            NetworkBox.Items.Add(new NetworkOption($"{network.Cidr}  ·  {network.InterfaceName}", network.Cidr, false));
        }

        NetworkBox.Items.Add(new NetworkOption(arabic ? "نطاق مخصص" : "Custom range", null, true));
        NetworkBox.SelectedIndex = 0;
    }

    private async void OnStartClick(object sender, System.Windows.RoutedEventArgs e)
    {
        if (_cancellation is not null)
        {
            return;
        }

        var selected = NetworkBox.SelectedItem as NetworkOption;
        var hosts = selected is { Custom: true }
            ? LanScanner.BuildHostRange(RangeStartBox.Text.Trim(), RangeEndBox.Text.Trim())
            : LanScanner.BuildHostList(254, selected?.Cidr);

        if (hosts.Count == 0)
        {
            _ui.Notify(LanguageManager.IsArabic ? "لا يوجد مضيفون للفحص." : "No hosts to scan.", UiSeverity.Warning);
            return;
        }

        _cancellation = new CancellationTokenSource();
        StartButton.IsEnabled = false;
        StopButton.IsEnabled = true;
        Rows.Clear();
        ScanProgress.Value = 0;

        var progress = new Progress<ScanProgress>(p =>
        {
            ScanProgress.Value = p.Total == 0 ? 0 : (double)p.Scanned / p.Total * 100;
            ScanStatusText.Text = LanguageManager.IsArabic
                ? $"فحص {p.Scanned}/{p.Total} — تم العثور على {p.Found}"
                : $"Scanning {p.Scanned}/{p.Total} — {p.Found} found";
            FoundCounter.Text = p.Found.ToString();
        });

        try
        {
            var devices = await _host.Hardware
                .ScanHostsAsync(hosts, _host.Settings.GetScanTimeoutMs(), progress, _cancellation.Token)
                .ConfigureAwait(true);

            foreach (var device in devices.OrderByDescending(d => d.Status).ThenBy(d => d.IpAddress, StringComparer.Ordinal))
            {
                var row = new ScanRow(device, LanguageManager.IsArabic);
                if (_host.Devices.GetDevices(includeArchived: true)
                        .Any(d => string.Equals(d.IpAddress, device.IpAddress, StringComparison.OrdinalIgnoreCase)
                                  && d.Port == device.Port))
                {
                    row.Added = true;
                }

                Rows.Add(row);
            }

            if (SelectedRow is null && Rows.Count > 0)
            {
                SelectedRow = Rows[0];
                ResultsGrid.SelectedItem = Rows[0];
            }

            ScanStatusText.Text = LanguageManager.IsArabic
                ? $"اكتمل الفحص: {devices.Count} منفذ 4370 مستجيب."
                : $"Scan complete: {devices.Count} responder(s) on port 4370.";
        }
        catch (OperationCanceledException)
        {
            ScanStatusText.Text = LanguageManager.IsArabic ? "تم إيقاف الفحص." : "Scan stopped.";
        }
        catch (Exception ex)
        {
            _host.Logger.Error("LAN scanner window failed.", ex);
            _ui.Notify(ex.Message, UiSeverity.Error);
        }
        finally
        {
            _cancellation.Dispose();
            _cancellation = null;
            StartButton.IsEnabled = true;
            StopButton.IsEnabled = false;
        }
    }

    private void OnStopClick(object sender, System.Windows.RoutedEventArgs e) => _cancellation?.Cancel();

    private async void OnRetryClick(object sender, System.Windows.RoutedEventArgs e)
    {
        if ((sender as System.Windows.FrameworkElement)?.DataContext is not ScanRow row)
        {
            return;
        }

        await RetryAsync(row).ConfigureAwait(true);
    }

    private async void OnRetrySelectedClick(object sender, System.Windows.RoutedEventArgs e)
    {
        if (SelectedRow is { } row)
        {
            await RetryAsync(row).ConfigureAwait(true);
        }
    }

    private async Task RetryAsync(ScanRow row)
    {
        var key = row.CommKey;
        var device = await _host.Hardware
            .ProbeAsync(row.Device.IpAddress, row.Device.Port, key)
            .ConfigureAwait(true);
        row.Update(device);
        NotifySelection();

        if (device.Status == DiscoveryStatus.Verified)
        {
            _ui.Notify(LanguageManager.IsArabic ? "تم تأكيد الجهاز." : "Terminal verified.", UiSeverity.Success);
        }
        else
        {
            _ui.Notify(device.HandshakeText, UiSeverity.Warning);
        }
    }

    private async void OnAddClick(object sender, System.Windows.RoutedEventArgs e)
    {
        if ((sender as System.Windows.FrameworkElement)?.DataContext is not ScanRow row)
        {
            return;
        }

        await AddAsync(row).ConfigureAwait(true);
    }

    private async void OnAddSelectedClick(object sender, System.Windows.RoutedEventArgs e)
    {
        if (SelectedRow is { } row)
        {
            await AddAsync(row).ConfigureAwait(true);
        }
    }

    private async Task AddAsync(ScanRow row)
    {
        if (row.Added)
        {
            return;
        }

        var (result, device) = _host.Devices.AddFromScan(
            row.Device,
            $"ZKTeco {row.Device.IpAddress}",
            row.CommKey,
            _host.Session.Actor);

        if (!result.Success || device is null)
        {
            _ui.Notify(Labels.Error(result.ErrorCode, LanguageManager.IsArabic, result.Detail), UiSeverity.Error);
            return;
        }

        row.Added = true;
        _ui.Notify(
            LanguageManager.IsArabic ? "تمت إضافة الجهاز. جارٍ سحب البصمات…" : "Terminal added. Pulling punches…",
            UiSeverity.Success);

        var pull = await _host.Devices.PullPunchesAsync(device.Id, _host.Session.Actor).ConfigureAwait(true);
        if (pull.Success)
        {
            _ui.Notify(pull.Message, UiSeverity.Info);
        }

        if (Owner?.DataContext is DevicesViewModel vm)
        {
            _ = vm.RefreshAsync();
        }
    }

    private void OnCloseClick(object sender, System.Windows.RoutedEventArgs e) => Close();

    protected override void OnClosed(EventArgs e)
    {
        _cancellation?.Cancel();
        base.OnClosed(e);
    }

    public void Dispose()
    {
        _cancellation?.Dispose();
        GC.SuppressFinalize(this);
    }
}
