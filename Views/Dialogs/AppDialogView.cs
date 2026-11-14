using KantinKita.Controls;
using KantinKita.Services;
using Microsoft.Maui.Controls.Shapes;

namespace KantinKita.Views.Dialogs;

internal sealed record DialogRequest(string Title, string? Message, string? Accept, string? Cancel,
    IReadOnlyList<DialogChoice>? Choices, string? SelectedId, VisualChoiceRequest? Visual = null, bool CompactGrid = false);

/// <summary>App-owned surface; Android supplies only its transparent window.</summary>
internal sealed class AppDialogView : ContentView
{
    internal const string Accepted = "__accept";
    internal event Action<string?>? Completed;
    internal Border Sheet { get; }
    private readonly VerticalStackLayout _body;
    private readonly ScrollView _bodyScroll;
    private double _viewportWidth;
    private double _viewportHeight;
    private bool _sizing;
    private View? _stickyHeader;
    private View? _stickyFooter;
    private Grid? _visualGrid;
    private int _visualColumns;
    private VisualChoiceSelection? _selection;
    private readonly List<(DialogChoice Choice, Border Surface, Icon Marker)> _visualCells = [];
    private Label? _selectionText;
    private Button? _confirmButton;

    internal AppDialogView(DialogRequest request)
    {
        var backdrop = new BoxView { Color = Colors.Transparent };
        Helpers.UiMotion.SetInteraction(backdrop, false);
        Tap(backdrop, Cancel);
        var headerIcon = new Icon { Glyph = request.Visual?.Layout == VisualChoiceLayout.Buildings
            ? Icons.Pin : request.Choices is not null ? Icons.Grid : Icons.Info, Size = 24 };
        Theme(headerIcon, Icon.ColorProperty, "BrandLight", "BrandDark");
        var iconPlate = new Border
        {
            StrokeThickness = 0, StrokeShape = new RoundRectangle { CornerRadius = 12 },
            WidthRequest = 48, HeightRequest = 48, Content = headerIcon, HorizontalOptions = LayoutOptions.Start,
        };
        Theme(iconPlate, BackgroundColorProperty, "BrandSoftLight", "BrandSoftDark");
        var closeIcon = new Icon { Glyph = Icons.Close, Size = 17 };
        Theme(closeIcon, Icon.ColorProperty, "InkSoftLight", "InkSoftDark");
        var close = new Border
        {
            StrokeThickness = 0, BackgroundColor = Colors.Transparent,
            WidthRequest = 44, HeightRequest = 44, Content = closeIcon,
        };
        SemanticProperties.SetDescription(close, "Tutup dialog");
        Tap(close, Cancel);
        var header = new Grid { ColumnDefinitions = [new() { Width = GridLength.Star }, new() { Width = GridLength.Auto }] };
        header.Add(iconPlate);
        header.Add(close, 1);
        var title = Text(request.Title, 24, "InkLight", "InkDark", true);
        SemanticProperties.SetHeadingLevel(title, SemanticHeadingLevel.Level1);
        var body = new VerticalStackLayout { Spacing = 16 };
        _body = body;
        if (request.Visual is { } visual)
        {
            _selection = new VisualChoiceSelection(visual.Choices, visual.SelectedId);
            // Keep context visible while the options scroll. The footer's Batal
            // action supplies dismissal, so this header does not repeat an X.
            header.Children.Remove(iconPlate);
            headerIcon.Size = 24;
            iconPlate.StrokeShape = new RoundRectangle { CornerRadius = 12 };
            var heading = new VerticalStackLayout { Spacing = 4, Children = { title } };
            if (!string.IsNullOrWhiteSpace(request.Message))
                heading.Add(Text(request.Message, 14, "InkSoftLight", "InkSoftDark"));
            var visualHeader = new Grid
            {
                ColumnDefinitions = [new() { Width = GridLength.Auto }, new() { Width = GridLength.Star }],
                ColumnSpacing = 12,
            };
            visualHeader.Add(iconPlate);
            visualHeader.Add(heading, 1);
            var persistentHeader = new VerticalStackLayout { Spacing = 12, Children = { visualHeader } };
            if (visual.Layout == VisualChoiceLayout.Tables) persistentHeader.Add(TableLegend(visual.Choices));
            _stickyHeader = persistentHeader;
            body.Add(VisualOptions(visual));
            if (!string.IsNullOrWhiteSpace(visual.Footnote))
                body.Add(Text(visual.Footnote, 12, "MutedLight", "MutedDark"));
            _stickyFooter = VisualFooter(visual);
            RefreshVisualSelection(visual);
        }
        else
        {
            if (request.Choices is { } choices)
            {
                // Long category catalogs scroll inside a stable title/dismissal
                // frame, rather than pushing those controls out of reach.
                header.Children.Remove(iconPlate);
                var heading = new VerticalStackLayout { Spacing = 4, Children = { title } };
                if (!string.IsNullOrWhiteSpace(request.Message))
                    heading.Add(Text(request.Message, 14, "InkSoftLight", "InkSoftDark"));
                var choiceHeader = new Grid
                {
                    ColumnDefinitions = [new() { Width = GridLength.Auto }, new() { Width = GridLength.Star }],
                    ColumnSpacing = 12,
                };
                choiceHeader.Add(iconPlate);
                choiceHeader.Add(heading, 1);
                _stickyHeader = choiceHeader;
                if (request.CompactGrid) body.Add(CategoryTiles(choices, request.SelectedId));
                else
                {
                    var options = new VerticalStackLayout { Spacing = 8 };
                    foreach (var choice in choices) options.Add(ChoiceRow(choice, choice.Id == request.SelectedId));
                    body.Add(options);
                }
            }
            else
            {
                body.Add(header);
                body.Add(title);
                if (!string.IsNullOrWhiteSpace(request.Message))
                    body.Add(Text(request.Message, 14, "InkSoftLight", "InkSoftDark"));
            }
        }
        var footer = new Grid { ColumnSpacing = 10 };
        if (request.Cancel is not null)
        {
            footer.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            var cancel = ActionButton(request.Cancel, false);
            cancel.Clicked += (_, _) => Cancel();
            footer.Add(cancel);
        }
        if (request.Accept is not null)
        {
            var column = footer.ColumnDefinitions.Count;
            footer.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            var accept = ActionButton(request.Accept, true);
            accept.Clicked += (_, _) => Completed?.Invoke(Accepted);
            footer.Add(accept, column);
        }
        if (request.Visual is null)
        {
            if (request.Choices is not null) _stickyFooter = footer;
            else body.Add(footer);
        }
        var bodyScroll = new ScrollView
        {
            Content = body, HeightRequest = 1,
            VerticalScrollBarVisibility = ScrollBarVisibility.Never,
        };
        _bodyScroll = bodyScroll;
        View sheetContent = bodyScroll;
        if (_stickyHeader is not null && _stickyFooter is not null)
        {
            var fixedFooter = new Grid
            {
                RowDefinitions = [new() { Height = GridLength.Auto }, new() { Height = GridLength.Auto }, new() { Height = GridLength.Auto }],
                RowSpacing = 16,
            };
            fixedFooter.Add(_stickyHeader);
            fixedFooter.Add(bodyScroll, 0, 1);
            fixedFooter.Add(_stickyFooter, 0, 2);
            sheetContent = fixedFooter;
        }
        Sheet = new Border
        {
            StrokeShape = new RoundRectangle { CornerRadius = request.Visual is not null ? 24 : 26 }, StrokeThickness = 1,
            Padding = request.Visual is not null ? 20 : 24,
            Margin = request.Visual is not null ? new Thickness(16) : new Thickness(16, 24, 16, 24),
            MaximumWidthRequest = 480, HorizontalOptions = LayoutOptions.Fill,
            VerticalOptions = LayoutOptions.End,
            Content = sheetContent,
        };
        Theme(Sheet, BackgroundColorProperty, "SurfaceLight", "SurfaceDark");
        Helpers.UiMotion.SetInteraction(Sheet, false);
        ThemeStroke(Sheet, "LineLight", "LineDark");
        Tap(Sheet, () => { });
        Content = new Grid { Children = { backdrop, Sheet } };

        // A ScrollView otherwise requests the complete available height, leaving a
        // large blank tail on short dialogs. Measure its content at the actual sheet
        // width, then cap the viewport only when long text needs to scroll.
        Loaded += (_, _) => FitSheet();
        body.SizeChanged += (_, _) => FitSheet();
        if (_stickyHeader is not null) _stickyHeader.SizeChanged += (_, _) => FitSheet();
        if (_stickyFooter is not null) _stickyFooter.SizeChanged += (_, _) => FitSheet();
    }

    // Native Dialog hosts do not assign the standalone ContentView's MAUI Bounds.
    // The host passes its measured native viewport after layout instead.
    internal void FitViewport(double width, double height)
    {
        if (width <= 0 || height <= 0) return;
        if (Math.Abs(_viewportWidth - width) <= .5 && Math.Abs(_viewportHeight - height) <= .5 && _bodyScroll.HeightRequest > 1) return;
        _viewportWidth = width;
        _viewportHeight = height;
        FitSheet();
    }

    private void FitSheet()
    {
        if (_sizing || _viewportWidth <= 0 || _viewportHeight <= 0 || _body.Handler is null) return;
        var sheetWidth = Math.Min(Sheet.MaximumWidthRequest, _viewportWidth - Sheet.Margin.HorizontalThickness);
        var contentWidth = sheetWidth - Sheet.Padding.HorizontalThickness - Sheet.StrokeThickness * 2;
        var headerHeight = _stickyHeader is null ? 0 : ((IView)_stickyHeader).Measure(contentWidth, double.PositiveInfinity).Height + 16;
        var footerHeight = _stickyFooter is null ? 0 : ((IView)_stickyFooter).Measure(contentWidth, double.PositiveInfinity).Height + 16;
        var availableHeight = _viewportHeight - Sheet.Margin.VerticalThickness - Sheet.Padding.VerticalThickness - Sheet.StrokeThickness * 2 - headerHeight - footerHeight;
        if (contentWidth <= 0 || availableHeight <= 0) return;
        _sizing = true;
        try
        {
            ConfigureVisualColumns(contentWidth);
            ((IView)_body).InvalidateMeasure();
            var measured = ((IView)_body).Measure(contentWidth, double.PositiveInfinity);
            if (!double.IsFinite(measured.Height) || measured.Height <= 1) return;
            var height = Math.Max(1, Math.Min(measured.Height, availableHeight));
            if (Math.Abs(_bodyScroll.HeightRequest - height) > .5) _bodyScroll.HeightRequest = height;
        }
        finally { _sizing = false; }
    }

    private View ChoiceRow(DialogChoice choice, bool selected)
    {
        var labels = new VerticalStackLayout { Spacing = 3, VerticalOptions = LayoutOptions.Center };
        labels.Add(Text(choice.Title, 14, selected ? "BrandLight" : "InkLight", selected ? "BrandDark" : "InkDark", true));
        if (!string.IsNullOrWhiteSpace(choice.Description))
            labels.Add(Text(choice.Description, 12, "MutedLight", "MutedDark"));
        var marker = new Icon { Glyph = Icons.CheckCircle, Size = 21, Opacity = selected ? 1 : .2 };
        Theme(marker, Icon.ColorProperty, selected ? "BrandLight" : "MutedLight", selected ? "BrandDark" : "MutedDark");
        var row = new Grid
        {
            ColumnSpacing = 12,
        };
        if (!string.IsNullOrWhiteSpace(choice.Glyph))
        {
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = 40 });
            var glyph = new Icon { Glyph = choice.Glyph, Size = 20 };
            Theme(glyph, Icon.ColorProperty, selected ? "BrandLight" : "InkSoftLight", selected ? "BrandDark" : "InkSoftDark");
            row.Add(glyph);
        }
        var labelColumn = row.ColumnDefinitions.Count;
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        row.Add(labels, labelColumn);
        row.Add(marker, labelColumn + 1);
        var option = new Border
        {
            Padding = new Thickness(16, 14), MinimumHeightRequest = 60, Content = row,
            StrokeThickness = selected ? 1 : 0,
            StrokeShape = new RoundRectangle { CornerRadius = 15 },
        };
        option.IsEnabled = choice.IsEnabled;
        option.Opacity = choice.IsEnabled ? 1 : .5;
        Theme(option, BackgroundColorProperty, selected ? "BrandSoftLight" : "CanvasLight", selected ? "BrandSoftDark" : "SurfaceMutedDark");
        option.SetAppTheme<Brush>(Border.StrokeProperty, new SolidColorBrush(Color.FromArgb("#AEC8FF")), new SolidColorBrush(Color.FromArgb("#4A6390")));
        SemanticProperties.SetDescription(option, choice.Title + (selected ? ", dipilih" : ""));
        if (choice.IsEnabled) Tap(option, () => Completed?.Invoke(choice.Id));
        return option;
    }

    private View CategoryTiles(IReadOnlyList<DialogChoice> choices, string? selectedId)
    {
        var grid = new Grid { ColumnDefinitions = [new(), new()], ColumnSpacing = 8, RowSpacing = 8 };
        _visualGrid = grid;
        _visualColumns = 2;
        for (var index = 0; index < choices.Count; index++)
        {
            var choice = choices[index];
            var selected = choice.Id == selectedId;
            if (index % 2 == 0) grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            var glyph = new Icon { Glyph = choice.Glyph ?? Icons.Grid, Size = 24, HorizontalOptions = LayoutOptions.Start };
            Theme(glyph, Icon.ColorProperty, selected ? "BrandLight" : "InkSoftLight", selected ? "BrandDark" : "InkSoftDark");
            var marker = new Icon { Glyph = Icons.CheckCircle, Size = 16, Opacity = selected ? 1 : 0 };
            Theme(marker, Icon.ColorProperty, "BrandLight", "BrandDark");
            var top = new Grid { ColumnDefinitions = [new() { Width = GridLength.Star }, new() { Width = GridLength.Auto }] };
            top.Add(glyph);
            top.Add(marker, 1);
            var content = new VerticalStackLayout { Spacing = 4, InputTransparent = true };
            content.Add(top);
            content.Add(Text(choice.Title, 14, selected ? "BrandLight" : "InkLight", selected ? "BrandDark" : "InkDark", true));
            content.Add(Text(choice.Badge ?? choice.Description ?? string.Empty, 12, "MutedLight", "MutedDark"));
            var tile = new Border
            {
                Content = content, Padding = 8, MinimumHeightRequest = 88, IsEnabled = choice.IsEnabled,
                Opacity = choice.IsEnabled ? 1 : .45, StrokeThickness = 1,
                StrokeShape = new RoundRectangle { CornerRadius = 12 },
            };
            Theme(tile, BackgroundColorProperty, selected ? "BrandSoftLight" : "SurfaceLight", selected ? "BrandSoftDark" : "SurfaceDark");
            ThemeStroke(tile, selected ? "BrandLight" : "LineLight", selected ? "BrandDark" : "LineDark");
            SemanticProperties.SetDescription(tile, $"{choice.Title}, {choice.Badge}{(selected ? ", dipilih" : string.Empty)}{(choice.IsEnabled ? string.Empty : ", tidak tersedia")}");
            if (choice.IsEnabled) Tap(tile, () => Completed?.Invoke(choice.Id));
            grid.Add(tile, index % 2, index / 2);
            _visualCells.Add((choice, tile, marker));
        }
        return grid;
    }

    private View TableLegend(IReadOnlyList<DialogChoice> choices)
    {
        return Text($"{choices.Count(choice => choice.IsEnabled)} dari {choices.Count} meja kosong · Meja terisi tidak dapat dipilih",
            12, "MutedLight", "MutedDark");
    }

    private View VisualOptions(VisualChoiceRequest request)
    {
        var grid = new Grid { ColumnDefinitions = [new(), new()], ColumnSpacing = 12, RowSpacing = 12 };
        _visualGrid = grid;
        _visualColumns = 2;
        for (var index = 0; index < request.Choices.Count; index++)
        {
            var choice = request.Choices[index];
            if (index % 2 == 0) grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            var marker = new Icon { Glyph = Icons.CheckCircle, Size = 20, InputTransparent = true };
            Theme(marker, Icon.ColorProperty, "BrandLight", "BrandDark");
            var status = Text(choice.IsEnabled ? request.Layout == VisualChoiceLayout.Tables ? "Kosong" : "Tujuan antar" : "Terisi", 12, "MutedLight", "MutedDark");
            var top = new Grid { ColumnDefinitions = [new() { Width = GridLength.Star }, new() { Width = GridLength.Auto }] };
            top.Add(status);
            top.Add(marker, 1);
            var content = new VerticalStackLayout { Spacing = 8, InputTransparent = true };
            content.Add(top);
            content.Add(request.Layout == VisualChoiceLayout.Tables ? TableGraphic(choice) : BuildingGraphic(choice));
            content.Add(Text(choice.Title, 14, "InkLight", "InkDark", true));
            if (!string.IsNullOrWhiteSpace(choice.Description)) content.Add(Text(choice.Description, 12, "MutedLight", "MutedDark"));
            var surface = new Border
            {
                Content = content, Padding = 12, StrokeThickness = 1,
                StrokeShape = new RoundRectangle { CornerRadius = 16 }, IsEnabled = choice.IsEnabled,
                Opacity = choice.IsEnabled ? 1 : .6,
            };
            Helpers.UiMotion.SetInteraction(surface, false);
            SemanticProperties.SetDescription(surface, $"{choice.Title}, {choice.Description}, {(choice.IsEnabled ? "tersedia" : "terisi, tidak dapat dipilih")}");
            if (choice.IsEnabled)
            {
                Tap(surface, () =>
                {
                    if (_selection?.Select(choice.Id) != true) return;
                    RefreshVisualSelection(request);
                    Helpers.Haptics.Tap();
                    // Native compositor pulse on the individual tile only.
                    if (surface.Handler?.PlatformView is Android.Views.View native &&
                        (!OperatingSystem.IsAndroidVersionAtLeast(26) || Android.Animation.ValueAnimator.AreAnimatorsEnabled()))
                    {
                        native.Animate()?.Cancel();
                        native.ScaleX = native.ScaleY = .97f;
                        native.Animate()?.ScaleX(1)?.ScaleY(1)?.SetDuration(190)?.Start();
                    }
                });
            }
            _visualCells.Add((choice, surface, marker));
            grid.Add(surface, index % 2, index / 2);
        }
        return grid;
    }

    private View VisualFooter(VisualChoiceRequest request)
    {
        _selectionText = Text(request.Layout == VisualChoiceLayout.Tables ? "Pilih satu meja kosong" : "Pilih gedung tujuan", 14, "InkLight", "InkDark", true);
        _confirmButton = ActionButton(request.ConfirmText, true);
        _confirmButton.FontSize = 14;
        _confirmButton.HeightRequest = -1;
        _confirmButton.MinimumHeightRequest = 48;
        _confirmButton.CornerRadius = 12;
        _confirmButton.Padding = new Thickness(12);
        _confirmButton.IsEnabled = false;
        _confirmButton.Clicked += (_, _) =>
        {
            if (_selection is { } selection && selection.TryConfirm(out var result)) Completed?.Invoke(result);
        };
        var cancel = ActionButton("Batal", false);
        cancel.FontSize = 14;
        cancel.HeightRequest = -1;
        cancel.MinimumHeightRequest = 48;
        cancel.CornerRadius = 12;
        cancel.Padding = new Thickness(12);
        cancel.Clicked += (_, _) => Cancel();
        var buttons = new Grid
        {
            ColumnDefinitions = [new() { Width = GridLength.Auto }, new() { Width = GridLength.Star }], ColumnSpacing = 12,
        };
        buttons.Add(cancel);
        buttons.Add(_confirmButton, 1);
        var line = new BoxView { HeightRequest = 1 };
        Theme(line, BoxView.ColorProperty, "LineLight", "LineDark");
        return new VerticalStackLayout { Spacing = 12, Children = { line, _selectionText, buttons } };
    }

    private void RefreshVisualSelection(VisualChoiceRequest request)
    {
        foreach (var cell in _visualCells)
        {
            var isSelected = cell.Choice.Id == _selection?.SelectedId;
            Theme(cell.Surface, BackgroundColorProperty, isSelected ? "BrandSoftLight" : "SurfaceMutedLight", isSelected ? "BrandSoftDark" : "SurfaceMutedDark");
            ThemeStroke(cell.Surface, isSelected ? "BrandLight" : "LineLight", isSelected ? "BrandDark" : "LineDark");
            cell.Marker.Opacity = isSelected ? 1 : 0;
            SemanticProperties.SetDescription(cell.Surface,
                $"{cell.Choice.Title}, {cell.Choice.Description}, {(cell.Choice.IsEnabled ? "tersedia" : "terisi, tidak dapat dipilih")}{(isSelected ? ", dipilih" : string.Empty)}");
            SemanticProperties.SetHint(cell.Surface, isSelected ? "Dipilih. Tekan tombol konfirmasi untuk menggunakan." : cell.Choice.IsEnabled ? "Ketuk untuk memilih" : "Tidak tersedia");
        }
        if (_selectionText is not null)
            _selectionText.Text = _selection?.Selected is { } selected ? $"{selected.Title} · {selected.Description}" :
                request.Layout == VisualChoiceLayout.Tables ? "Pilih satu meja kosong" : "Pilih gedung tujuan";
        if (_confirmButton is not null)
        {
            _confirmButton.IsEnabled = _selection?.CanConfirm == true;
            _confirmButton.Opacity = _confirmButton.IsEnabled ? 1 : .4;
        }
    }

    private static View TableGraphic(DialogChoice choice)
    {
        var diagram = new AbsoluteLayout { WidthRequest = 70, HeightRequest = 44, HorizontalOptions = LayoutOptions.Center, InputTransparent = true };
        var desk = new Border
        {
            StrokeShape = new RoundRectangle { CornerRadius = 6 }, StrokeThickness = 1,
            Content = Text(choice.Badge ?? choice.Title.Split('-').Last(), 12, "BrandLight", "BrandDark", true),
        };
        ((Label)desk.Content).HorizontalTextAlignment = TextAlignment.Center;
        ((Label)desk.Content).VerticalTextAlignment = TextAlignment.Center;
        Theme(desk, BackgroundColorProperty, "BrandSoftLight", "BrandSoftDark");
        ThemeStroke(desk, "BrandLight", "BrandDark");
        AbsoluteLayout.SetLayoutBounds(desk, new Rect(12, 7, 46, 30));
        diagram.Add(desk);
        foreach (var rect in new[] { new Rect(22, 0, 12, 4), new Rect(40, 0, 12, 4), new Rect(22, 40, 12, 4), new Rect(40, 40, 12, 4) }
            .Concat(choice.Capacity >= 6 ? new[] { new Rect(5, 16, 4, 12), new Rect(61, 16, 4, 12) } : Array.Empty<Rect>()))
        {
            var chair = new BoxView { CornerRadius = 2 };
            Theme(chair, BoxView.ColorProperty, "BrandLight", "BrandDark");
            AbsoluteLayout.SetLayoutBounds(chair, rect);
            diagram.Add(chair);
        }
        return diagram;
    }

    private static View BuildingGraphic(DialogChoice choice)
    {
        var icon = new Icon { Glyph = choice.Glyph ?? Icons.Pin, Size = 24 };
        Theme(icon, Icon.ColorProperty, "BrandLight", "BrandDark");
        var plate = new Border
        {
            WidthRequest = 48, HeightRequest = 48, StrokeThickness = 0,
            StrokeShape = new RoundRectangle { CornerRadius = 12 }, Content = icon,
            HorizontalOptions = LayoutOptions.Start,
        };
        Theme(plate, BackgroundColorProperty, "BrandSoftLight", "BrandSoftDark");
        return plate;
    }

    private void ConfigureVisualColumns(double contentWidth)
    {
        if (_visualGrid is null) return;
        var fontScale = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity?.Resources?.Configuration?.FontScale ?? 1;
        var columns = contentWidth >= 276 * fontScale ? 2 : 1;
        if (_visualColumns == columns) return;
        _visualColumns = columns;
        _visualGrid.ColumnDefinitions.Clear();
        _visualGrid.RowDefinitions.Clear();
        for (var column = 0; column < columns; column++) _visualGrid.ColumnDefinitions.Add(new ColumnDefinition());
        for (var index = 0; index < _visualCells.Count; index++)
        {
            if (index % columns == 0) _visualGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            Grid.SetColumn(_visualCells[index].Surface, index % columns);
            Grid.SetRow(_visualCells[index].Surface, index / columns);
        }
    }

    internal void CancelPendingSelection() => _selection?.Cancel();

    private void Cancel()
    {
        if (_selection?.IsCompleted == true) return;
        CancelPendingSelection();
        Completed?.Invoke(null);
    }

    private static void ThemeStroke(Border border, string light, string dark)
    {
        var resources = Application.Current!.Resources;
        border.SetAppTheme<Brush>(Border.StrokeProperty,
            new SolidColorBrush((Color)resources[light]), new SolidColorBrush((Color)resources[dark]));
    }

    private static Button ActionButton(string text, bool primary)
    {
        var button = new Button
        {
            Text = text, FontFamily = "sans-serif-medium", FontSize = 14,
            HeightRequest = 48, CornerRadius = 12, BorderWidth = 0, Padding = new Thickness(12, 0),
        };
        Theme(button, BackgroundColorProperty, primary ? "BrandLight" : "SurfaceMutedLight", primary ? "BrandDark" : "SurfaceMutedDark");
        Theme(button, Button.TextColorProperty, primary ? "OnBrandLight" : "InkSoftLight", primary ? "OnBrandDark" : "InkSoftDark");
        return button;
    }

    private static Label Text(string text, double size, string light, string dark, bool semibold = false)
    {
        var label = new Label
        {
            Text = text, FontSize = size, LineBreakMode = LineBreakMode.WordWrap,
            FontFamily = semibold ? "sans-serif-medium" : "sans-serif", LineHeight = 1.15,
        };
        Theme(label, Label.TextColorProperty, light, dark);
        return label;
    }

    private static void Theme(BindableObject target, BindableProperty property, string light, string dark)
    {
        var resources = Application.Current!.Resources;
        target.SetAppThemeColor(property, (Color)resources[light], (Color)resources[dark]);
    }

    private static void Tap(View view, Action action)
    {
        var tap = new TapGestureRecognizer();
        tap.Tapped += (_, _) => action();
        view.GestureRecognizers.Add(tap);
    }
}
