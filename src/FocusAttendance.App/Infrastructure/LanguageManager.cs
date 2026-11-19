using System.Windows;

namespace FocusAttendance.App.Infrastructure;

/// <summary>Hot-swaps the Arabic (RTL) / French (LTR) resource dictionary.</summary>
public static class LanguageManager
{
    public static bool IsArabic { get; private set; } = true;

    public static event Action? Changed;

    public static void Apply(bool arabic)
    {
        var application = System.Windows.Application.Current;
        if (application is null)
        {
            IsArabic = arabic;
            return;
        }

        foreach (var dictionary in application.Resources.MergedDictionaries
                     .Where(d => d.Source?.OriginalString.Contains("lang.", StringComparison.OrdinalIgnoreCase) == true)
                     .ToList())
        {
            application.Resources.MergedDictionaries.Remove(dictionary);
        }

        application.Resources.MergedDictionaries.Add(new ResourceDictionary
        {
            Source = new Uri(
                $"pack://application:,,,/FocusAttendance;component/Resources/Lang.{(arabic ? "ar" : "fr")}.xaml",
                UriKind.Absolute)
        });

        IsArabic = arabic;
        Localized.Instance.Refresh();
        Changed?.Invoke();
    }
}
