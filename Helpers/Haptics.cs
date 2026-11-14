using Microsoft.Maui.Devices;

namespace KantinKita.Helpers;

internal static class Haptics
{
    public static void Tap()
    {
        try
        {
            HapticFeedback.Default.Perform(HapticFeedbackType.Click);
        }
        catch (Exception)
        {
            // Haptics are a nice-to-have; some devices refuse them.
        }
    }
}
