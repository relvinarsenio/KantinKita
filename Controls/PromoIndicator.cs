using Android.Views.Animations;
using NativeView = Android.Views.View;

namespace KantinKita.Controls;

/// <summary>A fixed-size promo track with a compositor-animated active pill.</summary>
public sealed class PromoIndicator : ContentView
{
    public static readonly BindableProperty CountProperty = BindableProperty.Create(
        nameof(Count), typeof(int), typeof(PromoIndicator), 4,
        propertyChanged: (owner, _, _) => ((PromoIndicator)owner).BuildTrack());
    public static readonly BindableProperty SelectedIndexProperty = BindableProperty.Create(
        nameof(SelectedIndex), typeof(int), typeof(PromoIndicator), 0,
        propertyChanged: (owner, _, _) => ((PromoIndicator)owner).Update(true));

    private readonly PathInterpolator _travel = new(.2f, 0f, 0f, 1f);
    private readonly List<BoxView> _dots = [];
    private BoxView? _active;

    public int Count { get => (int)GetValue(CountProperty); set => SetValue(CountProperty, value); }
    public int SelectedIndex { get => (int)GetValue(SelectedIndexProperty); set => SetValue(SelectedIndexProperty, value); }

    public PromoIndicator()
    {
        HeightRequest = 44;
        HorizontalOptions = LayoutOptions.Center;
        VerticalOptions = LayoutOptions.Center;
        InputTransparent = true;
        Loaded += (_, _) => Update(false);
        Unloaded += (_, _) => StopMotion();
        BuildTrack();
    }

    private void BuildTrack()
    {
        StopMotion();
        _dots.Clear();
        var count = Math.Clamp(Count, 1, 20);
        var track = new Grid
        {
            WidthRequest = count * 18,
            HeightRequest = 12,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
            InputTransparent = true,
        };
        for (var index = 0; index < count; index++)
        {
            var dot = CreateMark(5);
            dot.Margin = new Thickness(6.5 + index * 18, 0, 0, 0);
            dot.Opacity = .3;
            _dots.Add(dot);
            track.Add(dot);
        }
        _active = CreateMark(16);
        _active.Loaded += (_, _) => Update(false);
        track.Add(_active);
        Content = track;
        WidthRequest = count * 18;
        Update(false);
    }

    private static BoxView CreateMark(double width)
    {
        var mark = new BoxView
        {
            WidthRequest = width, HeightRequest = 5, CornerRadius = 2.5,
            HorizontalOptions = LayoutOptions.Start, VerticalOptions = LayoutOptions.Center,
            InputTransparent = true,
        };
        mark.SetAppThemeColor(BoxView.ColorProperty, Color.FromArgb("#2563EB"), Color.FromArgb("#8FB5FF"));
        return mark;
    }

    private static NativeView? Native(VisualElement? element) =>
        element?.Handler?.PlatformView is NativeView view && view.Handle != IntPtr.Zero ? view : null;

    private void Update(bool animate)
    {
        var count = Math.Clamp(Count, 1, 20);
        var index = Math.Clamp(SelectedIndex, 0, count - 1);
        SemanticProperties.SetDescription(this, $"Promo {index + 1} dari {count}");
        if (Native(_active) is not { } active) return;
        active.Animate()?.Cancel();
        var density = active.Resources?.DisplayMetrics?.Density ?? 1f;
        var target = (1 + index * 18) * density;
        var enabled = !OperatingSystem.IsAndroidVersionAtLeast(26)
            || Android.Animation.ValueAnimator.AreAnimatorsEnabled();
        if (!animate || !IsLoaded || !enabled)
        {
            active.TranslationX = target;
            active.ScaleX = active.ScaleY = active.Alpha = 1;
            return;
        }

        // Stretch only the moving marker, never the track or the promo card.
        active.ScaleX = Math.Min(1.5f, 1 + Math.Abs(target - active.TranslationX) / (100 * density));
        active.Animate()!.TranslationX(target)!.ScaleX(1)!.Alpha(1)!
            .SetStartDelay(0)!.SetDuration(340)!.SetInterpolator(_travel)!.Start();
    }

    private void StopMotion()
    {
        if (Native(_active) is not { } active) return;
        active.Animate()?.Cancel();
        active.ScaleX = active.ScaleY = active.Alpha = 1;
    }
}
