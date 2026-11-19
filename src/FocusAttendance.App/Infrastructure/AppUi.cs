using System.Windows;
using System.Windows.Controls;
using FocusAttendance.Application;
using Wpf.Ui;
using Wpf.Ui.Controls;

namespace FocusAttendance.App.Infrastructure;

/// <summary>Snackbars + single in-window Fluent ContentDialogs (never a cascading popup window).</summary>
public sealed class AppUi : IUiMessenger, IUserPrompt{
    private readonly SnackbarService _snackbars = new();
    private readonly ContentDialogService _dialogs = new();
    private bool _attached;

    public void Attach(SnackbarPresenter snackbarPresenter, ContentDialogHost dialogHost)
    {
        _snackbars.DefaultTimeOut = TimeSpan.FromSeconds(4);
        _snackbars.SetSnackbarPresenter(snackbarPresenter);
        _dialogs.SetDialogHost(dialogHost);
        _attached = true;
    }

    public void Notify(string message, UiSeverity severity = UiSeverity.Info, string? title = null)
    {
        if (!_attached || string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        var appearance = severity switch
        {
            UiSeverity.Success => ControlAppearance.Success,
            UiSeverity.Warning => ControlAppearance.Caution,
            UiSeverity.Error => ControlAppearance.Danger,
            _ => ControlAppearance.Info
        };

        var icon = new SymbolIcon
        {
            Symbol = severity switch
            {
                UiSeverity.Success => SymbolRegular.CheckmarkCircle24,
                UiSeverity.Warning => SymbolRegular.Warning24,
                UiSeverity.Error => SymbolRegular.ErrorCircle24,
                _ => SymbolRegular.Info24
            }
        };

        var seconds = severity == UiSeverity.Error ? 8 : 4;
        _snackbars.Show(title ?? "Focus Attendance", message, appearance, icon, TimeSpan.FromSeconds(seconds));
    }

    public async Task<bool> ConfirmAsync(string title, string message, string confirmText, string cancelText)
    {
        if (!_attached)
        {
            return false;
        }

        var content = new StackPanel
        {
            MinWidth = 400,
            MaxWidth = 470
        };

        content.Children.Add(new SymbolIcon
        {
            Symbol = SymbolRegular.Warning24,
            FontSize = 26,
            Foreground = (System.Windows.Media.Brush)System.Windows.Application.Current.Resources["CautionBrush"],
            HorizontalAlignment = HorizontalAlignment.Left,
            Margin = new Thickness(0, 0, 0, 10)
        });
        content.Children.Add(new System.Windows.Controls.TextBlock
        {
            Text = message,
            TextWrapping = TextWrapping.Wrap,
            FontSize = 13,
            LineHeight = 20
        });

        var dialog = new ContentDialog
        {
            Title = title,
            Content = content,
            PrimaryButtonText = confirmText,
            CloseButtonText = cancelText,
            PrimaryButtonAppearance = ControlAppearance.Danger,
            IsFooterVisible = true,
            DialogWidth = 500,
            DialogMaxWidth = 520
        };

        var result = await _dialogs.ShowAsync(dialog, CancellationToken.None).ConfigureAwait(true);
        return result == ContentDialogResult.Primary;
    }

    public async Task<string?> PromptAsync(string title, string label, string? initialValue = null)
    {
        if (!_attached)
        {
            return null;
        }

        var input = new Wpf.Ui.Controls.TextBox
        {
            Text = initialValue ?? string.Empty,
            PlaceholderText = label,
            MinWidth = 360
        };

        var dialog = new ContentDialog
        {
            Title = title,
            Content = input,
            PrimaryButtonText = "OK",
            CloseButtonText = LanguageManager.IsArabic ? "إلغاء" : "Annuler",
            IsFooterVisible = true
        };

        var result = await _dialogs.ShowAsync(dialog, CancellationToken.None).ConfigureAwait(true);
        return result == ContentDialogResult.Primary ? input.Text : null;
    }
}
