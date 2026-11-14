namespace KantinKita.Helpers;

internal static class SearchInteraction
{
    public static async Task DismissKeyboardAsync(Entry entry, CancellationToken cancellationToken = default)
    {
        if (entry.Handler is null) return;
        await entry.HideSoftInputAsync(cancellationToken);
        entry.Unfocus();
#if ANDROID
        // Hiding the IME is asynchronous. Wait for its insets to disappear before
        // calculating a scroll target against the expanded viewport.
        for (var frame = 0; frame < 38; frame++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (entry.Handler?.PlatformView is not Android.Views.View native || native.Handle == IntPtr.Zero) return;
            if (!OperatingSystem.IsAndroidVersionAtLeast(30) ||
                native.RootWindowInsets?.IsVisible(Android.Views.WindowInsets.Type.Ime()) != true) break;
            await Task.Delay(16, cancellationToken);
        }
#endif
        // Allow the final inset/layout pass to finish; no fixed keyboard-duration delay.
        await Task.Delay(32, cancellationToken);
    }

    public static bool AnimationsEnabled => !OperatingSystem.IsAndroidVersionAtLeast(26)
        || Android.Animation.ValueAnimator.AreAnimatorsEnabled();
}
