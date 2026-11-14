using Android.Graphics.Drawables;
using AColor = Android.Graphics.Color;

namespace KantinKita.Platforms;

/// <summary>
/// Android 15+ enforces edge-to-edge, so the system bars are transparent and the
/// window background shows through the safe-area insets. Matching that background to
/// the app canvas (and flipping status bar icon contrast) keeps the chrome seamless.
/// </summary>
internal static class SystemUi
{
    private const string LightCanvas = "#F3F5F9";
    private const string DarkCanvas = "#000000";

    // WindowInsetsController.APPEARANCE_LIGHT_* values (API 30+).
    private const int LightStatusBars = 8;
    private const int LightNavigationBars = 16;

    public static void Apply()
    {
        var activity = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity;
        if (activity?.Window is not { } window)
        {
            return;
        }

        var isLight = Microsoft.Maui.Controls.Application.Current?.RequestedTheme != AppTheme.Dark;
        window.SetBackgroundDrawable(new ColorDrawable(AColor.ParseColor(isLight ? LightCanvas : DarkCanvas)));
        if (OperatingSystem.IsAndroidVersionAtLeast(29))
        {
            // Keep Android's three-button contrast scrim from darkening a light
            // application canvas while the app requests dark navigation icons.
            window.NavigationBarContrastEnforced = false;
        }

        if (!OperatingSystem.IsAndroidVersionAtLeast(30))
        {
            // Light system-bar icons are only available from API 30, so older devices get
            // a solid brand status bar that keeps the default white icons readable.
#pragma warning disable CA1422 // Only used on Android 10 and below.
            window.SetStatusBarColor(AColor.ParseColor(isLight ? "#173F8F" : DarkCanvas));
            window.SetNavigationBarColor(AColor.ParseColor(isLight ? LightCanvas : DarkCanvas));
#pragma warning restore CA1422
            return;
        }

        if (!OperatingSystem.IsAndroidVersionAtLeast(35))
        {
#pragma warning disable CA1422 // Edge-to-edge is enforced from Android 15.
            window.SetStatusBarColor(AColor.Transparent);
            window.SetNavigationBarColor(AColor.Transparent);
#pragma warning restore CA1422
        }

        try
        {
            if (window.DecorView is null)
            {
                return;
            }
            if (window.InsetsController is not { } controller)
            {
                return;
            }

            var mask = LightStatusBars | LightNavigationBars;
            var appearance = controller.SystemBarsAppearance;
            appearance = isLight ? appearance | mask : appearance & ~mask;
            controller.SetSystemBarsAppearance(appearance, mask);
        }
        catch (Exception)
        {
            // Best effort: system chrome is cosmetic.
        }
    }
}


