using System.Windows;
using System.Windows.Media;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace FocusAttendance.App.Infrastructure;

/// <summary>Swaps the brand palette and applies the Fluent 2 theme + OPGI emerald accent.</summary>
public static class ThemeService
{
    public static bool IsDark { get; private set; }

    public static void Apply(bool dark)
    {
        var application = System.Windows.Application.Current;
        if (application is null)
        {
            return;
        }

        foreach (var dictionary in application.Resources.MergedDictionaries
                     .Where(d => d.Source?.OriginalString.Contains("brand.", StringComparison.OrdinalIgnoreCase) == true)
                     .ToList())
        {
            application.Resources.MergedDictionaries.Remove(dictionary);
        }

        var brand = new ResourceDictionary
        {
            Source = new Uri(
                $"pack://application:,,,/FocusAttendance;component/Resources/Brand.{(dark ? "Dark" : "Light")}.xaml",
                UriKind.Absolute)
        };
        application.Resources.MergedDictionaries.Add(brand);

        var theme = dark ? ApplicationTheme.Dark : ApplicationTheme.Light;
        ApplicationThemeManager.Apply(theme, WindowBackdropType.Mica, true);

        var accent = dark
            ? Color.FromRgb(0x2F, 0xA0, 0x89)
            : Color.FromRgb(0x0F, 0x6B, 0x5C);
        try
        {
            ApplicationAccentColorManager.Apply(accent, theme, true, false);
        }
        catch (InvalidOperationException)
        {
            // Accent is cosmetic: the brand brushes still carry the identity.
        }

        IsDark = dark;
    }
}
