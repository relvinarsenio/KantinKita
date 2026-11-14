using Android.Views.Animations;
using NativeView = Android.Views.View;

namespace KantinKita.Controls;

/// <summary>A Fluent navigation destination; the shared active surface belongs to the dock.</summary>
public sealed class NavTabItem : ContentView
{
    public static readonly BindableProperty TitleProperty = BindableProperty.Create(
        nameof(Title), typeof(string), typeof(NavTabItem), string.Empty, propertyChanged: OnContentChanged);
    public static readonly BindableProperty GlyphProperty = BindableProperty.Create(
        nameof(Glyph), typeof(string), typeof(NavTabItem), string.Empty, propertyChanged: OnContentChanged);
    public static readonly BindableProperty IsSelectedProperty = BindableProperty.Create(
        nameof(IsSelected), typeof(bool), typeof(NavTabItem), false, propertyChanged: OnSelectionChanged);

    private readonly Icon _icon;
    private readonly Label _label;
    private readonly OvershootInterpolator _selectionEasing = new(1.2f);

    public NavTabItem()
    {
        _icon = new Icon { Size = 22, InputTransparent = true };
        _label = new Label
        {
            FontSize = 12,
            FontFamily = "sans-serif-medium",
            HorizontalTextAlignment = TextAlignment.Center,
            InputTransparent = true,
            MaxLines = 1,
        };
        Content = new VerticalStackLayout
        {
            Padding = new Thickness(2, 6, 2, 8),
            Spacing = 4,
            VerticalOptions = LayoutOptions.Center,
            InputTransparent = true,
            Children = { _icon, _label },
        };
        HeightRequest = 68;
        HorizontalOptions = LayoutOptions.Fill;
        VerticalOptions = LayoutOptions.Center;
        var tap = new TapGestureRecognizer();
        tap.Tapped += (_, _) => Tapped?.Invoke(this, EventArgs.Empty);
        GestureRecognizers.Add(tap);
        Unloaded += (_, _) => ResetIconMotion();
        ApplyContent();
        ApplySelection();
    }

    public event EventHandler? Tapped;
    public string Title { get => (string)GetValue(TitleProperty); set => SetValue(TitleProperty, value); }
    public string Glyph { get => (string)GetValue(GlyphProperty); set => SetValue(GlyphProperty, value); }
    public bool IsSelected { get => (bool)GetValue(IsSelectedProperty); set => SetValue(IsSelectedProperty, value); }

    private static void OnContentChanged(BindableObject bindable, object oldValue, object newValue) => ((NavTabItem)bindable).ApplyContent();
    private static void OnSelectionChanged(BindableObject bindable, object oldValue, object newValue) => ((NavTabItem)bindable).ApplySelection();

    private void ApplyContent()
    {
        _label.Text = Title;
        _icon.Glyph = Glyph;
        SemanticProperties.SetDescription(this, Title);
    }

    private void ApplySelection()
    {
        var selected = IsSelected;
        var light = Color.FromArgb(selected ? "#2563EB" : "#5F6B7A");
        var dark = Color.FromArgb(selected ? "#8FB5FF" : "#9DA3AD");
        _icon.SetAppThemeColor(Icon.ColorProperty, light, dark);
        _label.SetAppThemeColor(Label.TextColorProperty, light, dark);
        _icon.IsFilled = selected;
        SemanticProperties.SetHint(this, selected ? "Tab aktif" : $"Buka {Title}");
        if (_icon.Handler?.PlatformView is not NativeView view || view.Handle == IntPtr.Zero || !IsLoaded) return;
        view.Animate()?.Cancel();
        var density = view.Resources?.DisplayMetrics?.Density ?? 1f;
        if (selected)
        {
            view.ScaleX = view.ScaleY = 0.88f;
            view.TranslationY = 3 * density;
            view.Animate()!.ScaleX(1.06f)!.ScaleY(1.06f)!.TranslationY(-1.5f * density)!
                .SetDuration(320)!.SetInterpolator(_selectionEasing)!.Start();
        }
        else
        {
            view.Animate()!.ScaleX(1)!.ScaleY(1)!.TranslationY(0)!.SetDuration(180)!.Start();
        }
    }

    private void ResetIconMotion()
    {
        if (_icon.Handler?.PlatformView is not NativeView view || view.Handle == IntPtr.Zero) return;
        view.Animate()?.Cancel();
        view.TranslationY = 0;
        view.ScaleX = view.ScaleY = 1;
    }
}
