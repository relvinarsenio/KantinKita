using Microsoft.Maui.Controls.Shapes;
using MauiPath = Microsoft.Maui.Controls.Shapes.Path;

namespace KantinKita.Controls;

/// <summary>
/// Themeable Microsoft Fluent SVG geometry. Regular SVGs already contain the
/// outline in their filled path; IsFilled selects the official filled variant.
/// </summary>
public sealed class Icon : ContentView
{
    private static readonly PathGeometryConverter GeometryConverter = new();

    public static readonly BindableProperty GlyphProperty = BindableProperty.Create(
        nameof(Glyph), typeof(string), typeof(Icon), default(string), propertyChanged: OnVisualPropertyChanged);

    public static readonly BindableProperty ColorProperty = BindableProperty.Create(
        nameof(Color), typeof(Color), typeof(Icon), null, propertyChanged: OnVisualPropertyChanged);

    public static readonly BindableProperty SizeProperty = BindableProperty.Create(
        nameof(Size), typeof(double), typeof(Icon), 22d, propertyChanged: OnVisualPropertyChanged);

    public static readonly BindableProperty IsFilledProperty = BindableProperty.Create(
        nameof(IsFilled), typeof(bool), typeof(Icon), false, propertyChanged: OnVisualPropertyChanged);

    private readonly MauiPath _path;
    private string? _renderedGlyph;

    public Icon()
    {
        _path = new MauiPath
        {
            Aspect = Stretch.Uniform,
            HorizontalOptions = LayoutOptions.Fill,
            VerticalOptions = LayoutOptions.Fill,
            InputTransparent = true,
        };

        Content = _path;
        HorizontalOptions = LayoutOptions.Center;
        VerticalOptions = LayoutOptions.Center;
        Apply();
    }

    public string? Glyph
    {
        get => (string?)GetValue(GlyphProperty);
        set => SetValue(GlyphProperty, value);
    }

    public Color? Color
    {
        get => (Color?)GetValue(ColorProperty);
        set => SetValue(ColorProperty, value);
    }

    public double Size
    {
        get => (double)GetValue(SizeProperty);
        set => SetValue(SizeProperty, value);
    }

    public bool IsFilled
    {
        get => (bool)GetValue(IsFilledProperty);
        set => SetValue(IsFilledProperty, value);
    }

    private static void OnVisualPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        => ((Icon)bindable).Apply();

    private void Apply()
    {
        var size = Size <= 0 ? 22 : Size;
        WidthRequest = size;
        HeightRequest = size;
        _path.WidthRequest = size;
        _path.HeightRequest = size;
        var color = Color ?? Colors.Black;
        _path.Fill = new SolidColorBrush(color);
        _path.Stroke = null!;

        // Geometry belongs to this icon and only changes with its glyph. Theme,
        // size and selection updates can reuse the already parsed path.
        var glyph = IsFilled ? Icons.FilledVariant(Glyph) : Glyph;
        if (!string.Equals(_renderedGlyph, glyph, StringComparison.Ordinal))
        {
            _renderedGlyph = glyph;
            _path.Data = ParseGeometry(glyph);
        }
    }

    private static Geometry? ParseGeometry(string? glyph)
    {
        if (string.IsNullOrWhiteSpace(glyph))
        {
            return null;
        }

        try
        {
            return GeometryConverter.ConvertFromInvariantString(glyph) as Geometry;
        }
        catch (Exception)
        {
            return null;
        }
    }
}


