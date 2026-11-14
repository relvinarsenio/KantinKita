using Android.Views;
using Android.Views.Animations;
using NativeView = Android.Views.View;

namespace KantinKita.Views.Tabs;

public partial class HomeView
{
    private readonly PathInterpolator _notificationEasing = new(0.2f, 0f, 0f, 1f);

    private static NativeView? NotificationNative(VisualElement element) =>
        element.Handler?.PlatformView is NativeView view && view.Handle != IntPtr.Zero ? view : null;

    private void ShowNotifications()
    {
        var overlay = NotificationNative(NotificationOverlay);
        var panel = NotificationNative(NotificationPanel);
        var scrim = NotificationNative(NotificationScrim);
        if (overlay is null || panel is null || scrim is null) { _notificationsOpen = false; return; }
        ++_notificationAnimationVersion;
        panel.Animate()?.Cancel();
        scrim.Animate()?.Cancel();
        overlay.Visibility = ViewStates.Visible;
        overlay.TranslationZ = 10;
        var density = panel.Resources?.DisplayMetrics?.Density ?? 1f;
        panel.PivotX = panel.Width;
        panel.PivotY = 0;
        panel.Alpha = 0;
        panel.ScaleX = panel.ScaleY = 0.96f;
        panel.TranslationY = -8 * density;
        scrim.Alpha = 0;
        if (OperatingSystem.IsAndroidVersionAtLeast(26) && !Android.Animation.ValueAnimator.AreAnimatorsEnabled())
        {
            panel.Alpha = scrim.Alpha = panel.ScaleX = panel.ScaleY = 1;
            panel.TranslationY = 0;
            return;
        }
        panel.Animate()!.Alpha(1)!.ScaleX(1)!.ScaleY(1)!.TranslationY(0)!
            .WithLayer()!.SetDuration(220)!.SetInterpolator(_notificationEasing)!.Start();
        scrim.Animate()!.Alpha(1)!.SetDuration(160)!.Start();
    }

    private void CloseNotifications()
    {
        if (!_notificationsOpen) return;
        _notificationsOpen = false;
        var panel = NotificationNative(NotificationPanel);
        var scrim = NotificationNative(NotificationScrim);
        if (panel is null || scrim is null) { HideNotificationsImmediately(); return; }
        var version = ++_notificationAnimationVersion;
        panel.Animate()?.Cancel();
        scrim.Animate()?.Cancel();
        scrim.Animate()!.Alpha(0)!.SetDuration(140)!.Start();
        var density = panel.Resources?.DisplayMetrics?.Density ?? 1f;
        panel.Animate()!.Alpha(0)!.ScaleX(0.97f)!.ScaleY(0.97f)!.TranslationY(-6 * density)!
            .SetDuration(140)!.SetInterpolator(_notificationEasing)!
            .WithEndAction(new Java.Lang.Runnable(() =>
            {
                if (version == _notificationAnimationVersion) HideNotificationsImmediately();
            }))!.Start();
    }

    private void HideNotificationsImmediately()
    {
        _notificationsOpen = false;
        ++_notificationAnimationVersion;
        if (NotificationNative(NotificationOverlay) is { } overlay) overlay.Visibility = ViewStates.Invisible;
        if (NotificationNative(NotificationPanel) is { } panel)
        {
            panel.Animate()?.Cancel();
            panel.Alpha = panel.ScaleX = panel.ScaleY = 1;
            panel.TranslationY = 0;
        }
        if (NotificationNative(NotificationScrim) is { } scrim)
        {
            scrim.Animate()?.Cancel();
            scrim.Alpha = 0;
        }
    }
}
