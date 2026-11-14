using KantinKita.Controls;
using KantinKita.Models;
using KantinKita.Services;
using KantinKita.ViewModels;

// Uses real neutral-target MAUI controls, without a handler or platform stubs.
// Run: dotnet run --project Tests/ViewModels/KantinKita.ViewModels.Tests.csproj
var tests = new (string Name, Action Run)[]
{
    ("Detail bindings can read the price before Prepare", () =>
    {
        var dependencies = new TestDependencies();
        var vm = new ItemDetailViewModel(dependencies, dependencies, dependencies);
        Equal("Tambah · Rp0", vm.AddButtonText);

        var label = new Label();
        label.SetBinding(Label.TextProperty, new Binding(nameof(ItemDetailViewModel.AddButtonText)));
        label.BindingContext = vm;
        Equal("Tambah · Rp0", label.Text);
        // The initial binding reproduces the getter read during page construction.
        // Later live-binding updates need a platform dispatcher, so test subsequent
        // generated properties directly without replacing MAUI's dispatcher API.
        label.RemoveBinding(Label.TextProperty);

        vm.Quantity = 2;
        Equal("Tambah · Rp0", vm.AddButtonText);
        vm.ToggleFavoriteCommand.Execute(null);
        vm.AddToCartCommand.ExecuteAsync(null).GetAwaiter().GetResult();
        Equal(0, dependencies.FavoriteCalls);
        Equal(0, dependencies.AddCalls);
        Equal(0, dependencies.CloseCalls);
    }),
    ("Prepared detail price follows option and quantity changes", () =>
    {
        var dependencies = new TestDependencies();
        var vm = new ItemDetailViewModel(dependencies, dependencies, dependencies);
        var notifications = new List<string?>();
        vm.PropertyChanged += (_, e) => notifications.Add(e.PropertyName);

        vm.Prepare(new CanteenMenuItem
        {
            Id = "regression-menu",
            Name = "Mie ayam",
            Description = "Menu test",
            Stall = "Warung test",
            Category = MenuCategory.Makanan,
            Price = 15000m,
            OptionGroups = [new MenuOptionGroup("Porsi", [new("Biasa"), new("Jumbo", 3000m)])],
        });
        Equal("Tambah · Rp15.000", vm.AddButtonText);
        vm.OptionGroups[0].Options[1].ChooseCommand.Execute(null);
        Equal("Tambah · Rp18.000", vm.AddButtonText);
        vm.Quantity = 2;
        Equal("Tambah · Rp36.000", vm.AddButtonText);
        Equal("Rp36.000", vm.TotalText);
        True(notifications.Contains(nameof(ItemDetailViewModel.AddButtonText)));
    }),
    ("Icon keeps parsed geometry through visual-property changes", () =>
    {
        var icon = new Icon { Glyph = "M2,2 L22,22" };
        var path = (Microsoft.Maui.Controls.Shapes.Path)icon.Content;
        var geometry = path.Data ?? throw new InvalidOperationException("Expected parsed geometry.");
        icon.Color = Colors.Red;
        icon.Size = 28;
        icon.IsFilled = true;
        True(ReferenceEquals(geometry, path.Data));

        icon.Glyph = "M2,22 L22,2";
        True(path.Data is not null && !ReferenceEquals(geometry, path.Data));
        icon.Glyph = null;
        True(path.Data is null);
    }),
    ("Every vendored Fluent icon parses and uses its official filled geometry", () =>
    {
        foreach (var field in typeof(Icons).GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static))
        {
            if (!field.IsLiteral || field.FieldType != typeof(string)) continue;
            var icon = new Icon { Glyph = (string)field.GetRawConstantValue()! };
            var path = (Microsoft.Maui.Controls.Shapes.Path)icon.Content;
            True(path.Data is not null);
            True(path.Fill is not null && path.Stroke is null);
        }
        var heart = new Icon { Glyph = Icons.Heart };
        var heartPath = (Microsoft.Maui.Controls.Shapes.Path)heart.Content;
        var regular = heartPath.Data;
        heart.IsFilled = true;
        True(heartPath.Data is not null && !ReferenceEquals(regular, heartPath.Data));
        Equal(Icons.HeartFilled, Icons.FilledVariant(Icons.Heart));
    }),
    ("Cart icons are restored from stable IDs and excluded from JSON", () =>
    {
        var item = System.Text.Json.JsonSerializer.Deserialize<CartItem>(
            """{"ItemId":"es-teh","Name":"Es teh","Emoji":"legacy icon","UnitPrice":4000,"Quantity":2}""")!;
        Equal(Icons.Drink, item.IconGlyph);
        Equal(2, item.Quantity);
        var json = System.Text.Json.JsonSerializer.Serialize(item);
        True(!json.Contains("IconGlyph") && !json.Contains("Emoji"));

        var snapshot = System.Text.Json.JsonSerializer.Deserialize(
            """{"Lines":[{"ItemId":"es-teh","Name":"Es teh","Emoji":"old icon","Quantity":2,"UnitPrice":4000}]}""",
            AppJsonContext.Default.CartSnapshot)!;
        Equal("es-teh", snapshot.Lines[0].ItemId);
        Equal(2, snapshot.Lines[0].Quantity);
        Equal(4000m, snapshot.Lines[0].UnitPrice);
        var saved = System.Text.Json.JsonSerializer.Serialize(snapshot, AppJsonContext.Default.CartSnapshot);
        True(!saved.Contains("Emoji") && !saved.Contains("IconGlyph"));
    }),
    ("Cart quantity changes retain the row and publish new prices", () =>
    {
        var dependencies = new TestDependencies();
        dependencies.CartItems.Add(new CartItem
        {
            ItemId = "test-tea", Name = "Es teh", UnitPrice = 4000m, Quantity = 2,
        });
        var vm = new CartViewModel(dependencies, dependencies, dependencies);
        vm.Activate();
        var row = vm.Lines[0];
        var notifications = new List<string?>();
        row.PropertyChanged += (_, e) => notifications.Add(e.PropertyName);
        row.DecreaseCommand.Execute(null);
        True(ReferenceEquals(row, vm.Lines[0]));
        Equal("1", row.QuantityText);
        Equal("Rp4.000", row.LineTotalText);
        Equal("Rp5.000", vm.TotalText);
        True(notifications.Contains(nameof(CartLineViewModel.QuantityText)));
        True(notifications.Contains(nameof(CartLineViewModel.LineTotalText)));
        row.DecreaseCommand.Execute(null);
        Equal(0, vm.Lines.Count);
        True(vm.IsEmpty);
        vm.Deactivate();
    }),
    ("Promo handoff retains the revealed surface until the new front is ready", () =>
    {
        var dependencies = new TestDependencies();
        var vm = new HomeViewModel(new MenuService(), dependencies, new TestProfileService(), dependencies);
        var first = vm.CurrentPromo;
        var revealed = vm.PreviewPromo;
        vm.AdvancePromo(previous: false, preservePreview: true);
        True(ReferenceEquals(revealed, vm.CurrentPromo));
        True(ReferenceEquals(revealed, vm.PreviewPromo));
        True(!ReferenceEquals(first, vm.CurrentPromo));
        Equal(1, vm.CurrentPromoIndex);
        // Native animation exchanges the identical surfaces before preparing the
        // next background. No additional carousel step is consumed by the settle.
        vm.ShowPromoPreview(previous: false);
        True(!ReferenceEquals(vm.CurrentPromo, vm.PreviewPromo));
        Equal(1, vm.CurrentPromoIndex);
    }),
    ("Previous promo handoff wraps and normal commands restore the next preview", () =>
    {
        var dependencies = new TestDependencies();
        var vm = new HomeViewModel(new MenuService(), dependencies, new TestProfileService(), dependencies);
        var first = vm.CurrentPromo;
        vm.ShowPromoPreview(previous: true);
        var revealed = vm.PreviewPromo;
        vm.AdvancePromo(previous: true, preservePreview: true);
        Equal(vm.Promos.Count - 1, vm.CurrentPromoIndex);
        True(ReferenceEquals(revealed, vm.CurrentPromo));
        True(ReferenceEquals(revealed, vm.PreviewPromo));
        vm.ShowPromoPreview(previous: false);
        True(ReferenceEquals(first, vm.PreviewPromo));
        vm.GoToNextPromo();
        Equal(0, vm.CurrentPromoIndex);
        True(ReferenceEquals(first, vm.CurrentPromo));
        True(ReferenceEquals(vm.Promos[1], vm.PreviewPromo));
    }),
};

foreach (var (name, run) in tests)
{
    run();
    Console.WriteLine($"PASS {name}");
}
Console.WriteLine($"{tests.Length} view model/control regression tests passed.");

static void True(bool condition)
{
    if (!condition)
    {
        throw new InvalidOperationException("Expected condition to be true.");
    }
}

static void Equal<T>(T expected, T actual)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
    {
        throw new InvalidOperationException($"Expected {expected}, got {actual}.");
    }
}
