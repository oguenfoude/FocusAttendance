import io
import re

p = r"D:\FocusAttendance\src\FocusAttendance.Application\ViewModels\DevicesViewModel.cs"
t = io.open(p, encoding="utf-8").read()

# 1. field
if "_infoDump" not in t:
    t = t.replace("    private string _networkLog = string.Empty;",
                  "    private string _networkLog = string.Empty;\n    private string _infoDump = string.Empty;", 1)

# 2. command registration
if "DumpInfoCommand = new AsyncRelayCommand" not in t:
    t = t.replace(
        "        ReadUsersCommand = new AsyncRelayCommand(ReadUsersAsync, () => !IsBusy && SelectedDevice is not null);",
        "        ReadUsersCommand = new AsyncRelayCommand(ReadUsersAsync, () => !IsBusy && SelectedDevice is not null);\n"
        "        DumpInfoCommand = new AsyncRelayCommand(DumpInfoAsync, () => !IsBusy && SelectedDevice is not null);", 1)

# 3. property declaration
if "public AsyncRelayCommand DumpInfoCommand" not in t:
    t = t.replace("    public AsyncRelayCommand ReadUsersCommand { get; }",
                  "    public AsyncRelayCommand ReadUsersCommand { get; }\n    public AsyncRelayCommand DumpInfoCommand { get; }", 1)

# 4. public properties
if "public string InfoDump" not in t:
    t = t.replace(
        "    public string NetworkLog { get => _networkLog; private set => SetProperty(ref _networkLog, value); }",
        "    public string NetworkLog { get => _networkLog; private set => SetProperty(ref _networkLog, value); }\n\n"
        "    /// <summary>Raw SDK diagnostic dump of the selected terminal.</summary>\n"
        "    public string InfoDump\n"
        "    {\n"
        "        get => _infoDump;\n"
        "        private set\n"
        "        {\n"
        "            if (SetProperty(ref _infoDump, value))\n"
        "            {\n"
        "                OnPropertyChanged(nameof(HasInfoDump));\n"
        "            }\n"
        "        }\n"
        "    }\n\n"
        "    public bool HasInfoDump => !string.IsNullOrWhiteSpace(_infoDump);", 1)

# 5. reset when another device is opened
if "InfoDump = string.Empty;" not in t:
    t = t.replace("        NetworkLog = string.Empty;\n        IsDrawerOpen = true;",
                  "        NetworkLog = string.Empty;\n        InfoDump = string.Empty;\n        IsDrawerOpen = true;", 1)

# 6. method
if "private async Task DumpInfoAsync()" not in t:
    method = '''
    private async Task DumpInfoAsync()
    {
        if (SelectedDevice is null)
        {
            return;
        }

        IsBusy = true;
        try
        {
            var model = SelectedDevice.Model;
            var endpoint = new ZkEndpoint(model.IpAddress, model.Port, model.CommKey);
            var lines = await _host.Hardware.DumpSdkInfoAsync(endpoint).ConfigureAwait(true);
            InfoDump = string.Join(Environment.NewLine, lines);
            StatusMessage = IsArabic
                ? "تمت قراءة كل المعلومات المتاحة من الجهاز عبر SDK."
                : "Every value the terminal exposes has been read through the SDK.";
            _ui.Notify(
                IsArabic ? "تم الفحص الكامل للجهاز." : "Full terminal dump completed.",
                UiSeverity.Info);
        }
        catch (Exception ex)
        {
            _host.Logger.Error("SDK dump failed.", ex);
            _ui.Notify(Labels.Error(null, IsArabic), UiSeverity.Error);
        }
        finally
        {
            IsBusy = false;
        }
    }

'''
    t = t.replace("    private async Task ReadUsersAsync()", method + "    private async Task ReadUsersAsync()", 1)

io.open(p, "w", encoding="utf-8", newline="").write(t)
print("viewmodel patched")
