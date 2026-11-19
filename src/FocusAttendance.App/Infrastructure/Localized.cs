using System.ComponentModel;

namespace FocusAttendance.App.Infrastructure;

/// <summary>
/// Bindable localization source for elements that do not support DynamicResource
/// (DataGrid columns). Raises a single change notification to re-localise every header.
/// </summary>
public sealed class Localized : INotifyPropertyChanged
{
    public static Localized Instance { get; } = new();

    public string this[string key] => Texts.Get(key);

    public event PropertyChangedEventHandler? PropertyChanged;

    public void Refresh() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
}
