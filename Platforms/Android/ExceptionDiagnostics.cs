#if DEBUG
namespace KantinKita.Platforms;

internal static class ExceptionDiagnostics
{
    private static int _initialized;

    public static void Initialize()
    {
        if (Interlocked.Exchange(ref _initialized, 1) != 0) return;

        // Binding failures can be caught internally by MAUI. Preserve their managed
        // stack in logcat even when the debugger cannot display an exception source.
        AppDomain.CurrentDomain.FirstChanceException += (_, args) =>
        {
            if (args.Exception is NullReferenceException exception)
            {
                Android.Util.Log.Error("KantinKita.Exception", exception.ToString());
            }
        };
    }
}
#endif
