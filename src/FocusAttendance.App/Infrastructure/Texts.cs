using System.Windows;

namespace FocusAttendance.App.Infrastructure;

/// <summary>Reads a localized string from the merged language dictionary.</summary>
public static class Texts
{
    public static string Get(string key) =>
        System.Windows.Application.Current?.TryFindResource(key) as string ?? key;
}
