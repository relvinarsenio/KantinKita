using Android.Views;
using Android.Views.Animations;
using NativeView = Android.Views.View;

namespace KantinKita.Views;

public partial class MainTabsPage
{
    private readonly PathInterpolator _motionEasing = new(0.2f, 0f, 0f, 1f);

    private static NativeView? Native(VisualElement element) =>
        element.Handler?.PlatformView is NativeView view && view.Handle != IntPtr.Zero ? view : null;

    private bool AnimateScene(ContentView outgoing, ContentView incoming, int direction)
    {
        var previous = Native(outgoing);
        var next = Native(incoming);
        if (previous is null || next is null) return false;
        if (OperatingSystem.IsAndroidVersionAtLeast(26) && !Android.Animation.ValueAnimator.AreAnimatorsEnabled()) return false;
        var version = _transitionVersion;
        // Fluent 2 top-level navigation uses a quick fade: destinations are peers,
        // so sliding/scaling whole pages would imply a spatial hierarchy.
        next.TranslationX = next.TranslationY = 0;
        next.ScaleX = next.ScaleY = 1;
        next.Alpha = 0;
        previous.TranslationZ = 1;
        next.TranslationZ = 2;
        next.Animate()!.Alpha(1)!
            .SetDuration(180)!.SetInterpolator(_motionEasing)!
            .WithEndAction(new Java.Lang.Runnable(() =>
            {
                if (version == _transitionVersion) FinishTabTransition();
            }))!.Start();
        return true;
    }

    private static void ShowScene(ContentView owner, bool visible)
    {
        var view = Native(owner);
        if (view is null) return;
        view.Visibility = visible ? ViewStates.Visible : ViewStates.Invisible;
        view.TranslationZ = visible ? 2 : 0;
    }

    private void SetSceneInputShield(bool visible)
    {
        var view = Native(SceneInputShield);
        if (view is null) return;
        view.Clickable = true;
        view.TranslationZ = 3;
        view.Visibility = visible ? ViewStates.Visible : ViewStates.Invisible;
    }

    private void SetDockLayer()
    {
        if (Native(FloatingDock) is { } dock) dock.TranslationZ = 4;
    }

    private void ResetSceneMotion(ContentView owner)
    {
        var view = Native(owner);
        if (view is not null)
        {
            view.Animate()?.Cancel();
            view.TranslationX = view.TranslationY = view.Rotation = 0;
            view.ScaleX = view.ScaleY = view.Alpha = 1;
        }
    }

    private void MoveSelectionIndicator(bool animate)
    {
        if (_selectedIndex < 0 || NavRail.Width <= 0) return;
        var cell = NavRail.Width / 4;
        NavSelection.WidthRequest = Math.Max(1, cell - 8);
        var target = (float)(_selectedIndex * cell + 4);
        var view = Native(NavSelection);
        if (view is null) return;
        var density = view.Resources?.DisplayMetrics?.Density ?? 1f;
        var previous = view.TranslationX;
        view.Animate()?.Cancel();
        var shine = Native(NavShine);
        shine?.Animate()?.Cancel();
        // The native animator owns this transform. Updating the MAUI transform as
        // well lets a later layout pass apply the displacement a second time.
#if DEBUG
        Android.Util.Log.Debug("KantinKita.Motion", $"Tab {_selectedIndex}; rail={NavRail.Width:F1}; left={view.Left}; target={target:F1}dp");
#endif
        if (!animate || !IsLoaded ||
            OperatingSystem.IsAndroidVersionAtLeast(26) && !Android.Animation.ValueAnimator.AreAnimatorsEnabled())
        {
            view.TranslationX = target * density;
            view.ScaleX = view.ScaleY = 1;
            view.Rotation = 0;
            if (shine is not null) shine.Alpha = 0;
            return;
        }
        view.TranslationX = previous;
        view.ScaleX = 1.16f;
        view.ScaleY = 0.90f;
        view.Rotation = target * density > previous ? -1.5f : 1.5f;
        view.Animate()!.TranslationX(target * density)!.ScaleX(1)!.ScaleY(1)!
            .Rotation(0)!.SetDuration(380)!.SetInterpolator(_motionEasing)!.Start();
        if (shine is not null)
        {
            shine.TranslationX = -24 * density;
            shine.Alpha = 0.65f;
            shine.Animate()!.TranslationX((float)cell * density)!.Alpha(0)!
                .SetDuration(460)!.SetInterpolator(_motionEasing)!.Start();
        }
    }

    private void AnimateCartDock()
    {
        var view = Native(CartDock);
        if (view is null) return;
        view.Animate()?.Cancel();
        if (!CartDock.IsVisible || !IsLoaded)
        {
            view.TranslationY = 0;
            view.ScaleX = view.ScaleY = view.Alpha = 1;
            return;
        }
        var density = view.Resources?.DisplayMetrics?.Density ?? 1f;
        view.TranslationY = 8 * density;
        view.Alpha = 0.4f;
        view.ScaleX = view.ScaleY = 0.98f;
        view.Animate()!.TranslationY(0)!.Alpha(1)!.ScaleX(1)!.ScaleY(1)!
            .WithLayer()!.SetDuration(240)!.SetInterpolator(_motionEasing)!.Start();
    }
}
