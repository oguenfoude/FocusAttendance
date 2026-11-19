using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace FocusAttendance.App.Controls;

/// <summary>Right-hand slide-over drawer (no extra window, no nested tabs).</summary>
public partial class SlideOver : UserControl
{
    public static readonly DependencyProperty IsOpenProperty = DependencyProperty.Register(
        nameof(IsOpen), typeof(bool), typeof(SlideOver), new PropertyMetadata(false, OnIsOpenChanged));

    public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(
        nameof(Title), typeof(string), typeof(SlideOver), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty SubtitleProperty = DependencyProperty.Register(
        nameof(Subtitle), typeof(string), typeof(SlideOver), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty DrawerContentProperty = DependencyProperty.Register(
        nameof(DrawerContent), typeof(object), typeof(SlideOver), new PropertyMetadata(null));

    public static readonly DependencyProperty CloseCommandProperty = DependencyProperty.Register(
        nameof(CloseCommand), typeof(ICommand), typeof(SlideOver), new PropertyMetadata(null));

    public SlideOver()
    {
        InitializeComponent();
        PreviewKeyDown += OnPreviewKeyDown;
    }

    public bool IsOpen
    {
        get => (bool)GetValue(IsOpenProperty);
        set => SetValue(IsOpenProperty, value);
    }

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string Subtitle
    {
        get => (string)GetValue(SubtitleProperty);
        set => SetValue(SubtitleProperty, value);
    }

    public object? DrawerContent
    {
        get => GetValue(DrawerContentProperty);
        set => SetValue(DrawerContentProperty, value);
    }

    public ICommand? CloseCommand
    {
        get => (ICommand?)GetValue(CloseCommandProperty);
        set => SetValue(CloseCommandProperty, value);
    }

    private static void OnIsOpenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((SlideOver)d).Animate((bool)e.NewValue);

    private void Animate(bool open)
    {
        if (open)
        {
            Visibility = Visibility.Visible;
            Scrim.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(200)));
        }

        var duration = TimeSpan.FromMilliseconds(open ? 260 : 180);
        var ease = new CubicEase { EasingMode = open ? EasingMode.EaseOut : EasingMode.EaseIn };

        var target = open
            ? 0d
            : (Panel.ActualWidth > 0 ? Panel.ActualWidth + 60 : 640);

        var slide = new DoubleAnimation(target, duration) { EasingFunction = ease };
        var fade = new DoubleAnimation(open ? 1 : 0, duration) { EasingFunction = ease };

        if (!open)
        {
            fade.Completed += (_, _) =>
            {
                if (!IsOpen)
                {
                    Visibility = Visibility.Collapsed;
                }
            };
        }

        Slide.BeginAnimation(TranslateTransform.XProperty, slide);
        Panel.BeginAnimation(OpacityProperty, fade);
    }

    private void OnScrimMouseDown(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
        CloseCommand?.Execute(null);
    }

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape && IsOpen)
        {
            CloseCommand?.Execute(null);
            e.Handled = true;
        }
    }
}
