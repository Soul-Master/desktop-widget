using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Windows.UI.ViewManagement;

namespace DesktopWidget.UI;

/// <summary>Animates a bound target without replacing the binding with an animation.</summary>
public static class AnimatedProgress
{
    public static readonly DependencyProperty TargetProperty = DependencyProperty.RegisterAttached(
        "Target", typeof(double), typeof(AnimatedProgress), new PropertyMetadata(double.NaN, TargetChanged));
    private static readonly DependencyProperty StateProperty = DependencyProperty.RegisterAttached(
        "State", typeof(AnimationState), typeof(AnimatedProgress), new PropertyMetadata(null));

    public static double GetTarget(DependencyObject element) => (double)element.GetValue(TargetProperty);
    public static void SetTarget(DependencyObject element, double value) => element.SetValue(TargetProperty, value);

    private static void TargetChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        if (sender is not ProgressBar bar || args.NewValue is not double target || !double.IsFinite(target)) return;
        var state = (AnimationState?)bar.GetValue(StateProperty);
        if (state is null)
        {
            state = new AnimationState(bar);
            bar.SetValue(StateProperty, state);
        }
        state.Update(Math.Clamp(target, bar.Minimum, bar.Maximum));
    }

    private sealed class AnimationState
    {
        private readonly ProgressBar bar;
        private Storyboard? animation;
        private double target;

        public AnimationState(ProgressBar bar)
        {
            this.bar = bar;
            bar.Unloaded += (_, _) => Snap();
        }

        public void Update(double value)
        {
            target = value;
            // Start a new transition from the current visual value if interrupted.
            var current = bar.Value;
            animation?.Stop();
            animation = null;
            bar.Value = value;
            if (!bar.IsLoaded || !new UISettings().AnimationsEnabled || Math.Abs(current - value) < 0.01) return;

            var transition = new DoubleAnimation
            {
                From = current, To = value, Duration = TimeSpan.FromMilliseconds(450),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut },
                EnableDependentAnimation = true
            };
            Storyboard.SetTarget(transition, bar);
            Storyboard.SetTargetProperty(transition, "Value");
            var storyboard = new Storyboard();
            storyboard.Children.Add(transition);
            storyboard.Completed += (_, _) =>
            {
                if (animation == storyboard) Snap();
            };
            animation = storyboard;
            storyboard.Begin();
        }

        private void Snap()
        {
            animation?.Stop();
            animation = null;
            bar.Value = target;
        }
    }
}
