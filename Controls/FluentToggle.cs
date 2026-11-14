using Android.Views.Animations;
using KantinKita.Helpers;
using Microsoft.Maui.Controls.Shapes;

namespace KantinKita.Controls;

/// <summary>App-owned theme toggle, with a sliding thumb and crossfading track.</summary>
public sealed class FluentToggle : ContentView
{
    public static readonly BindableProperty IsToggledProperty = BindableProperty.Create(
        nameof(IsToggled), typeof(bool), typeof(FluentToggle), false, BindingMode.TwoWay,
        propertyChanged: (target, _, _) => ((FluentToggle)target).Update(true));
    private readonly Border _active;
    private readonly Border _thumb;
    private readonly OvershootInterpolator _spring = new(.75f);
    public bool IsToggled { get => (bool)GetValue(IsToggledProperty); set => SetValue(IsToggledProperty, value); }

    public FluentToggle()
    {
        UiMotion.SetInteraction(this, false);
        HeightRequest = MinimumHeightRequest = 44;
        WidthRequest = 52;
        var track = new Border { HeightRequest = 28, WidthRequest = 48, StrokeThickness = 0,
            StrokeShape = new RoundRectangle { CornerRadius = 14 }, InputTransparent = true };
        track.SetAppThemeColor(BackgroundColorProperty, Color.FromArgb("#D5DCE8"), Color.FromArgb("#363636"));
        _active = new Border { HeightRequest = 28, WidthRequest = 48, StrokeThickness = 0,
            StrokeShape = new RoundRectangle { CornerRadius = 14 }, InputTransparent = true };
        _active.SetAppThemeColor(BackgroundColorProperty, Color.FromArgb("#2563EB"), Color.FromArgb("#8FB5FF"));
        _thumb = new Border { WidthRequest = 22, HeightRequest = 22, Margin = new Thickness(3, 0, 0, 0),
            HorizontalOptions = LayoutOptions.Start, VerticalOptions = LayoutOptions.Center,
            StrokeThickness = 0, StrokeShape = new RoundRectangle { CornerRadius = 11 },
            BackgroundColor = Colors.White, InputTransparent = true };
        Content = new Grid { WidthRequest = 48, HeightRequest = 28,
            HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center,
            Children = { track, _active, _thumb }, InputTransparent = true };
        var tap = new TapGestureRecognizer();
        tap.Tapped += (_, _) => { IsToggled = !IsToggled; Haptics.Tap(); };
        GestureRecognizers.Add(tap);
        Loaded += (_, _) => Update(false);
        SizeChanged += (_, _) => { if (IsLoaded) Update(false); };
        Unloaded += (_, _) => Update(false);
        Update(false);
    }

    private void Update(bool animate)
    {
        SemanticProperties.SetDescription(this, "Mode AMOLED, " + (IsToggled ? "aktif" : "nonaktif"));
        SemanticProperties.SetHint(this, "Ketuk untuk mengganti tema");
        if (_thumb.Handler?.PlatformView is not Android.Views.View thumb ||
            _active.Handler?.PlatformView is not Android.Views.View active) return;
        thumb.Animate()?.Cancel();
        active.Animate()?.Cancel();
        var target = IsToggled ? 20 * (thumb.Resources?.DisplayMetrics?.Density ?? 1f) : 0;
        if (!animate || !IsLoaded || OperatingSystem.IsAndroidVersionAtLeast(26) && !Android.Animation.ValueAnimator.AreAnimatorsEnabled())
        {
            thumb.TranslationX = target;
            active.Alpha = IsToggled ? 1 : 0;
            return;
        }
        thumb.Animate()!.TranslationX(target)!.SetDuration(330)!.SetInterpolator(_spring)!.Start();
        active.Animate()!.Alpha(IsToggled ? 1 : 0)!.SetDuration(180)!.Start();
    }
}
