namespace KantinKita.Controls;

/// <summary>Centers the Android glyph line inside the login input's leading-icon row.</summary>
public static class LoginInput
{
    public static readonly BindableProperty CenterContentProperty = BindableProperty.CreateAttached(
        "CenterContent", typeof(bool), typeof(LoginInput), false,
        propertyChanged: (target, _, value) =>
        {
            if (target is not Entry entry) return;
            entry.HandlerChanged -= OnHandlerChanged;
            if ((bool)value) entry.HandlerChanged += OnHandlerChanged;
            Apply(entry);
        });

    public static bool GetCenterContent(BindableObject target) => (bool)target.GetValue(CenterContentProperty);
    public static void SetCenterContent(BindableObject target, bool value) => target.SetValue(CenterContentProperty, value);

    private static void OnHandlerChanged(object? sender, EventArgs args)
    {
        if (sender is Entry entry) Apply(entry);
    }

    private static void Apply(Entry entry)
    {
#if ANDROID
        if (!GetCenterContent(entry) || entry.Handler?.PlatformView is not Android.Widget.EditText native) return;
        // Font padding is Android's extra ascent/descent space. Removing it makes
        // the visible text line and the adjacent 20dp icon share an optical center.
        native.SetIncludeFontPadding(false);
        native.Gravity = Android.Views.GravityFlags.Start | Android.Views.GravityFlags.CenterVertical;
        native.SetPadding(0, 0, 0, 0);
#endif
    }
}
