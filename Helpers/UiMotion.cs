using System.Runtime.CompilerServices;
using Android.Views.Animations;
using NativeView = Android.Views.View;

namespace KantinKita.Helpers;

/// <summary>Short, interruptible compositor motion; never changes layout or consumes input.</summary>
public static class UiMotion
{
    private static readonly ConditionalWeakTable<VisualElement, object> Wired = new();
    private static readonly ConditionalWeakTable<VisualElement, RevealState> Reveals = new();
    private static readonly ConditionalWeakTable<InputView, VisualElement> FocusTargets = new();
    private sealed class RevealState { public bool Active; public int Version; }
    private static readonly PathInterpolator Ease = new(0.16f, 1f, 0.3f, 1f);
    private static readonly OvershootInterpolator Spring = new(0.65f);

    public static readonly BindableProperty InteractionProperty = BindableProperty.CreateAttached(
        "Interaction", typeof(bool), typeof(UiMotion), true);
    public static bool GetInteraction(BindableObject target) => (bool)target.GetValue(InteractionProperty);
    public static void SetInteraction(BindableObject target, bool value) => target.SetValue(InteractionProperty, value);

    public static readonly BindableProperty RevealProperty = BindableProperty.CreateAttached(
        "Reveal", typeof(int), typeof(UiMotion), -1, propertyChanged: OnRevealChanged);
    public static int GetReveal(BindableObject target) => (int)target.GetValue(RevealProperty);
    public static void SetReveal(BindableObject target, int value) => target.SetValue(RevealProperty, value);

    public static readonly BindableProperty PulseProperty = BindableProperty.CreateAttached(
        "Pulse", typeof(object), typeof(UiMotion), null, propertyChanged: (target, oldValue, newValue) =>
        {
            if (oldValue is not null && !Equals(oldValue, newValue) && target is VisualElement element && element.IsLoaded)
                Pulse(element, 1.075f);
        });
    public static object? GetPulse(BindableObject target) => target.GetValue(PulseProperty);
    public static void SetPulse(BindableObject target, object? value) => target.SetValue(PulseProperty, value);

    public static void Wire(VisualElement element)
    {
        if (!GetInteraction(element)) return;
        if (Wired.TryGetValue(element, out _)) return;
        if (element is Button button)
        {
            Wired.Add(element, new object());
            button.Pressed += (_, _) => Press(button);
            button.Released += (_, _) => Release(button);
        }
        else if (element is InputView input)
        {
            Wired.Add(element, new object());
            input.Focused += (_, _) => Focus(input, true);
            input.Unfocused += (_, _) => Focus(input, false);
        }
        else if (element is View view && element is not Controls.NavTabItem && view.GestureRecognizers.Count > 0)
        {
            Wired.Add(element, new object());
            foreach (var tap in view.GestureRecognizers.OfType<TapGestureRecognizer>())
                tap.Tapped += (_, _) => Pulse(view, 0.97f);
        }
        else return;
        element.Unloaded += (_, _) =>
        {
            Reset(element);
            if (element is InputView input && FocusTargets.TryGetValue(input, out var target)) Reset(target);
        };
    }

    private static void OnRevealChanged(BindableObject target, object oldValue, object newValue)
    {
        if (target is not VisualElement element || (int)oldValue >= 0 || (int)newValue < 0) return;
        element.Loaded += (_, _) => Reveal(element, Math.Clamp(GetReveal(element), 0, 160));
        element.Unloaded += (_, _) => Reset(element);
    }

    private static bool Enabled => !OperatingSystem.IsAndroidVersionAtLeast(26)
        || Android.Animation.ValueAnimator.AreAnimatorsEnabled();

    private static NativeView? Native(VisualElement element) =>
        element.Handler?.PlatformView is NativeView view && view.Handle != IntPtr.Zero ? view : null;

    private static void Reveal(VisualElement element, int delay)
    {
        var view = Native(element);
        if (view is null || !element.IsVisible || !Enabled) return;
        var state = Reveals.GetValue(element, static _ => new RevealState());
        state.Active = true;
        var version = ++state.Version;
        view.Animate()?.Cancel();
        view.Alpha = 0;
        view.TranslationY = 16 * (view.Resources?.DisplayMetrics?.Density ?? 1f);
        view.ScaleX = view.ScaleY = 0.985f;
        view.Animate()!.Alpha(1)!.TranslationY(0)!.ScaleX(1)!.ScaleY(1)!
            .SetStartDelay(delay)!.SetDuration(390)!.SetInterpolator(Ease)!
            .WithEndAction(new Java.Lang.Runnable(() => { if (version == state.Version) state.Active = false; }))!.Start();
    }

    private static void Press(VisualElement element)
    {
        if (!Enabled || Native(element) is not { } view) return;
        FinishReveal(element);
        view.Animate()?.Cancel();
        view.Animate()!.ScaleX(0.965f)!.ScaleY(0.965f)!
            .SetStartDelay(0)!.SetDuration(100)!.SetInterpolator(Ease)!.Start();
    }

    private static void Release(VisualElement element)
    {
        if (Native(element) is not { } view) return;
        view.Animate()?.Cancel();
        if (!Enabled) { view.ScaleX = view.ScaleY = 1; return; }
        view.Animate()!.ScaleX(1)!.ScaleY(1)!.SetStartDelay(0)!
            .SetDuration(330)!.SetInterpolator(Spring)!.Start();
    }

    private static void Pulse(VisualElement element, float scale)
    {
        if (!Enabled || Native(element) is not { } view) return;
        FinishReveal(element);
        view.Animate()?.Cancel();
        view.ScaleX = view.ScaleY = scale;
        Release(element);
    }

    private static void Focus(InputView element, bool focused)
    {
        Element? parent = element.Parent;
        while (parent is not null && parent is not Border) parent = parent.Parent;
        var target = parent as VisualElement ?? element;
        if (Native(target) is not { } view) return;
        FocusTargets.Remove(element);
        FocusTargets.Add(element, target);
        FinishReveal(target);
        view.Animate()?.Cancel();
        var scale = focused && Enabled ? 1.012f : 1f;
        view.Animate()!.ScaleX(scale)!.ScaleY(scale)!.SetStartDelay(0)!
            .SetDuration(220)!.SetInterpolator(Ease)!.Start();
    }

    private static void Reset(VisualElement element)
    {
        if (Reveals.TryGetValue(element, out var state)) { state.Active = false; ++state.Version; }
        if (Native(element) is not { } view) return;
        view.Animate()?.Cancel();
        view.Alpha = view.ScaleX = view.ScaleY = 1;
        view.TranslationY = 0;
    }

    private static void FinishReveal(VisualElement element)
    {
        if (Reveals.TryGetValue(element, out var state) && state.Active) Reset(element);
    }
}
