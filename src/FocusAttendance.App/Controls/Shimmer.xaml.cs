using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace FocusAttendance.App.Controls;

/// <summary>Lightweight shimmer placeholder (pulsing skeleton) shown while data loads.</summary>
public partial class Shimmer : UserControl
{
    public Shimmer()
    {
        InitializeComponent();
        Loaded += (_, _) => Start();
    }

    private void Start()
    {
        var animation = new DoubleAnimation
        {
            From = 1.0,
            To = 0.45,
            Duration = TimeSpan.FromMilliseconds(850),
            AutoReverse = true,
            RepeatBehavior = RepeatBehavior.Forever,
            EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
        };

        Host.BeginAnimation(OpacityProperty, animation);
    }
}
