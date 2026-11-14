#if ANDROID
using Android.App;
using Android.Graphics.Drawables;
using Android.Views;
using Android.Views.Animations;
using Microsoft.Maui.Platform;
using NativeView = Android.Views.View;

namespace KantinKita.Views.Dialogs;

internal static class AppDialogHost
{
    internal static async Task<string?> ShowAsync(DialogRequest request)
    {
        var activity = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity;
        var page = Microsoft.Maui.Controls.Application.Current?.Windows.FirstOrDefault()?.Page;
        var context = page?.Handler?.MauiContext;
        if (activity is null || activity.IsFinishing || activity.IsDestroyed || context is null) return null;
        var previousFocus = activity.CurrentFocus;

        var result = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var content = new AppDialogView(request);
        using var dialog = new Dialog(activity);
        dialog.RequestWindowFeature((int)WindowFeatures.NoTitle);
        var native = content.ToPlatform(context);
        dialog.SetContentView(native);
        dialog.SetCancelable(true);
        dialog.SetCanceledOnTouchOutside(false);
        var closing = false;
        string? selected = null;
        NativeView? sheet = null;
        using var enterEasing = new OvershootInterpolator(.65f);
        using var exitEasing = new AccelerateInterpolator();

        void Finish(string? value)
        {
            if (closing) return;
            closing = true;
            selected = value;
            if (sheet is null || !AnimationsEnabled()) { dialog.Dismiss(); return; }
            sheet.Animate()?.Cancel();
            sheet.Animate()?.Alpha(0f).TranslationY(28 * Density(sheet)).ScaleX(.98f).ScaleY(.98f)
                .SetDuration(150).SetInterpolator(exitEasing)
                .WithEndAction(new Java.Lang.Runnable(dialog.Dismiss)).Start();
        }
        void OnCompleted(string? value) => Finish(value);
        EventHandler cancel = (_, _) => { closing = true; selected = null; };
        void CompleteWindow()
        {
            closing = true;
            if (selected is null) content.CancelPendingSelection();
            if (sheet is { Handle: var handle } && handle != IntPtr.Zero) sheet.Animate()?.Cancel();
            // A destroyed activity can detach the window without delivering Dialog.Dismiss.
            // Always release the service's queue; destruction must cancel a pending choice.
            result.TrySetResult(activity.IsFinishing || activity.IsDestroyed ? null : selected);
        }
        EventHandler dismiss = (_, _) => CompleteWindow();
        EventHandler<NativeView.ViewDetachedFromWindowEventArgs> detached = (_, _) => CompleteWindow();
        void FitViewport()
        {
            if (closing || native.Handle == IntPtr.Zero) return;
            var density = Density(native);
            content.FitViewport(native.Width / density, native.Height / density);
        }
        EventHandler<NativeView.LayoutChangeEventArgs> layout = (_, _) => FitViewport();
        EventHandler<Android.Content.DialogKeyEventArgs> key = (_, args) =>
        {
            if (args.KeyCode != Keycode.Back) return;
            args.Handled = true;
            if (args.Event?.Action == KeyEventActions.Up) Finish(null);
        };
        content.Completed += OnCompleted;
        dialog.CancelEvent += cancel;
        dialog.DismissEvent += dismiss;
        dialog.KeyPress += key;
        native.ViewDetachedFromWindow += detached;
        native.LayoutChange += layout;
        try
        {
            dialog.Show();
            if (dialog.Window is { } window)
            {
                window.SetBackgroundDrawable(new ColorDrawable(Android.Graphics.Color.Transparent));
                window.SetLayout(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent);
                window.AddFlags(WindowManagerFlags.DimBehind);
                window.SetDimAmount(.38f);
                window.SetSoftInputMode(SoftInput.AdjustResize);
                window.SetGravity(GravityFlags.Center);
            }
            native.Post(FitViewport);
            sheet = content.Sheet.Handler?.PlatformView as NativeView;
            if (sheet is not null && AnimationsEnabled())
            {
                sheet.Alpha = 0f;
                sheet.TranslationY = 44 * Density(sheet);
                sheet.ScaleX = .97f;
                sheet.ScaleY = .97f;
                sheet.Post(() =>
                {
                    if (closing) return;
                    sheet.Animate()?.Alpha(1f).TranslationY(0).ScaleX(1).ScaleY(1)
                        .SetDuration(300).SetInterpolator(enterEasing).Start();
                });
            }
            return await result.Task;
        }
        finally
        {
            if (sheet is { Handle: var handle } && handle != IntPtr.Zero) sheet.Animate()?.Cancel();
            content.Completed -= OnCompleted;
            dialog.CancelEvent -= cancel;
            dialog.DismissEvent -= dismiss;
            dialog.KeyPress -= key;
            native.ViewDetachedFromWindow -= detached;
            native.LayoutChange -= layout;
            if (dialog.IsShowing && !activity.IsDestroyed) dialog.Dismiss();
            content.DisconnectHandlers();
            if (!activity.IsFinishing && !activity.IsDestroyed && previousFocus is { Handle: var focusHandle }
                && focusHandle != IntPtr.Zero && previousFocus.IsAttachedToWindow)
                previousFocus.RequestFocus();
        }
    }
    private static float Density(NativeView view) => view.Resources?.DisplayMetrics?.Density ?? 1f;
    private static bool AnimationsEnabled() => !OperatingSystem.IsAndroidVersionAtLeast(26) || Android.Animation.ValueAnimator.AreAnimatorsEnabled();
}
#endif
