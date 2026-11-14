using KantinKita.Controls;
using KantinKita.Models;
using KantinKita.Services;
using KantinKita.ViewModels;

var menu = new MenuService();
var tests = new (string Name, Func<Task> Run)[]
{
    ("Each canteen has a scoped catalog with globally unique offer IDs", () =>
    {
        Equal(menu.All.Count, menu.All.Select(item => item.Id).Distinct(StringComparer.Ordinal).Count());
        foreach (var canteen in CanteenCatalog.All)
        {
            var items = menu.Query(MenuCategory.Semua, null, MenuSort.Populer, canteen.Id);
            True(items.Count > 0 && items.All(item => item.CanteenId == canteen.Id));
        }
        Equal(0, menu.Query(MenuCategory.Semua, null, MenuSort.Populer, "missing-canteen").Count);
        return Task.CompletedTask;
    }),
    ("The same real food in different canteens keeps separate identity and price", () =>
    {
        var a = menu.Find("geprek")!;
        var b = menu.Find("kantin-b:geprek")!;
        Equal(a.Name, b.Name);
        True(a.Id != b.Id && a.CanteenId != b.CanteenId && a.Price != b.Price);
        Equal(Icons.FoodChickenLeg, b.IconGlyph);
        True(b.OptionGroups.Any(group => group.Title == "Level pedas"));
        True(b.OptionGroups.SelectMany(group => group.Options).Any(option => option.ExtraPrice > 0));
        return Task.CompletedTask;
    }),
    ("Search category sort and highlights stay inside the selected canteen", () =>
    {
        var results = menu.Query(MenuCategory.Makanan, "ayam", MenuSort.Termurah, "kantin-c");
        True(results.Count > 0 && results.All(item => item.CanteenId == "kantin-c" && item.Category == MenuCategory.Makanan));
        True(results.Select(item => item.Price).SequenceEqual(results.Select(item => item.Price).Order()));
        True(menu.GetHighlights(6, "kantin-b").All(item => item.CanteenId == "kantin-b"));
        Equal(0, menu.Query(MenuCategory.Semua, "gado", MenuSort.Populer, "kantin-b").Count);
        return Task.CompletedTask;
    }),
    ("Selected canteen persists while repeated selections publish no duplicate change", () =>
    {
        var preferences = new MemoryPreferences();
        var selection = new CanteenSelectionService(preferences);
        Equal(CanteenCatalog.DefaultId, selection.Selected.Id);
        var changes = 0;
        selection.Changed += (_, _) => changes++;
        selection.Select("kantin-b");
        selection.Select("kantin-b");
        Equal(1, changes);
        Equal("kantin-b", new CanteenSelectionService(preferences).Selected.Id);
        Throws<ArgumentException>(() => selection.Select("missing"));
        Equal("kantin-b", selection.Selected.Id);
        return Task.CompletedTask;
    }),
    ("Home menu highlights and promo all follow the same canteen without clearing the cart", () =>
    {
        var selection = new CanteenSelectionService(new MemoryPreferences());
        var cart = new CartService(new MemoryCartStorage());
        var ui = new UiDependencies();
        var a = menu.Find("geprek")!;
        cart.Add(a, a.DefaultOptions(), 1);
        var home = new HomeViewModel(menu, cart, ui, ui, selection);
        var catalog = new MenuViewModel(menu, cart, ui, selection);
        selection.Select("kantin-b");
        True(home.Promos.All(promo => promo.Item.CanteenId == "kantin-b"));
        True(home.Highlights.All(card => card.Item.CanteenId == "kantin-b"));
        True(home.Recommendations.All(card => card.Item.CanteenId == "kantin-b"));
        True(catalog.Items.All(card => card.Item.CanteenId == "kantin-b"));
        Equal("kantin-b", home.SelectedCanteen.Id);
        Equal("kantin-b", catalog.SelectedCanteen.Id);
        Equal(0, home.CurrentPromoIndex);
        Equal(a.Id, cart.Items.Single().ItemId);
        return Task.CompletedTask;
    }),
    ("Scoped search is recalculated when the canteen changes", () =>
    {
        var selection = new CanteenSelectionService(new MemoryPreferences());
        var cart = new CartService(new MemoryCartStorage());
        var ui = new UiDependencies();
        var home = new HomeViewModel(menu, cart, ui, ui, selection) { SearchText = "gado" };
        var catalog = new MenuViewModel(menu, cart, ui, selection) { SearchText = "gado" };
        selection.Select("kantin-b");
        True(home.IsEmpty && catalog.IsEmpty);
        selection.Select("kantin-c");
        True(!home.IsEmpty && !catalog.IsEmpty);
        True(catalog.Items.All(card => card.Item.CanteenId == "kantin-c" && card.Name.Contains("Gado")));
        return Task.CompletedTask;
    }),
    ("Home broad search includes every matching result rather than the discovery preview limit", () =>
    {
        var selection = new CanteenSelectionService(new MemoryPreferences());
        var cart = new CartService(new MemoryCartStorage());
        var ui = new UiDependencies();
        var home = new HomeViewModel(menu, cart, ui, ui, selection);
        var catalog = new MenuViewModel(menu, cart, ui, selection);
        True(home.IsBrowsing && home.HasMoreItems);
        Equal(6, home.Recommendations.Count);
        home.SearchText = catalog.SearchText = "a";
        True(catalog.Items.Count > 6);
        True(!home.IsBrowsing && !home.HasMoreItems);
        True(home.Recommendations.Select(card => card.Item.Id).SequenceEqual(catalog.Items.Select(card => card.Item.Id)));
        Equal("Hasil pencarian", home.RecommendationsTitle);
        selection.Select("kantin-b");
        True(home.Recommendations.Select(card => card.Item.Id).SequenceEqual(catalog.Items.Select(card => card.Item.Id)));
        True(home.Recommendations.All(card => card.Item.CanteenId == "kantin-b"));
        return Task.CompletedTask;
    }),
    ("Home no-match search clears stale cards and clearing restores the discovery state", () =>
    {
        var cart = new CartService(new MemoryCartStorage());
        var ui = new UiDependencies();
        var home = new HomeViewModel(menu, cart, ui, ui);
        home.SearchText = "menu-yang-tidak-ada-4751";
        True(home.IsSearching && !home.IsBrowsing && home.IsEmpty);
        Equal(0, home.Recommendations.Count);
        True(!home.HasMoreItems);
        home.ClearSearchCommand.Execute(null);
        True(!home.IsSearching && home.IsBrowsing && !home.IsEmpty);
        Equal(6, home.Recommendations.Count);
        True(home.HasMoreItems);
        home.SearchText = "  ";
        True(!home.IsSearching && home.IsBrowsing);
        return Task.CompletedTask;
    }),
    ("Home category and search intersect visibly and reset restores all categories", () =>
    {
        var cart = new CartService(new MemoryCartStorage());
        var ui = new UiDependencies();
        var home = new HomeViewModel(menu, cart, ui, ui);
        home.Categories.Single(category => category.Category == MenuCategory.Minuman).SelectCommand.Execute(null);
        Equal("Minuman", home.SelectedCategoryLabel);
        True(!home.IsBrowsing);
        var drinks = menu.Query(MenuCategory.Minuman, null, MenuSort.Populer, home.SelectedCanteen.Id);
        True(home.Recommendations.Select(card => card.Item.Id).SequenceEqual(drinks.Select(item => item.Id)));
        home.SearchText = "ayam";
        True(home.IsEmpty && home.Recommendations.Count == 0);
        home.ClearSearchCommand.Execute(null);
        Equal("Minuman", home.SelectedCategoryLabel);
        True(!home.IsBrowsing && !home.IsEmpty);
        home.ResetFiltersCommand.Execute(null);
        Equal("Semua menu", home.SelectedCategoryLabel);
        True(home.IsBrowsing && !home.IsSearching);
        True(home.Categories.Single(category => category.IsSelected).Category == MenuCategory.Semua);
        return Task.CompletedTask;
    }),
    ("Clearing only the Home category preserves the search query and updates the active filter state", () =>
    {
        var cart = new CartService(new MemoryCartStorage());
        var ui = new UiDependencies();
        var home = new HomeViewModel(menu, cart, ui, ui) { SearchText = "teh" };
        home.Categories.Single(category => category.Category == MenuCategory.Minuman).SelectCommand.Execute(null);
        True(home.HasCategoryFilter);
        var notifications = new List<string?>();
        home.PropertyChanged += (_, args) => notifications.Add(args.PropertyName);
        home.ClearCategoryCommand.Execute(null);
        Equal("teh", home.SearchText);
        True(home.IsSearching && !home.IsBrowsing && !home.HasCategoryFilter);
        Equal("Semua menu", home.SelectedCategoryLabel);
        var matches = menu.Query(MenuCategory.Semua, "teh", MenuSort.Populer, home.SelectedCanteen.Id);
        True(home.Recommendations.Select(card => card.Item.Id).SequenceEqual(matches.Select(item => item.Id)));
        True(notifications.Contains(nameof(HomeViewModel.HasCategoryFilter)));
        return Task.CompletedTask;
    }),
    ("Category chooser retains current selection and cancellation preserves active filters", async () =>
    {
        var selection = new CanteenSelectionService(new MemoryPreferences());
        var cart = new CartService(new MemoryCartStorage());
        var ui = new UiDependencies();
        var dialog = new CategoryDialog { Result = choices => choices.Single(choice => choice.Title == "Minuman").Id };
        var home = new HomeViewModel(menu, cart, ui, ui, selection, dialog);
        await home.ChooseCategoryCommand.ExecuteAsync(null);
        var drinkChoice = dialog.Choices.Single(choice => choice.Title == "Minuman");
        Equal("Minuman", home.SelectedCategoryLabel);
        Equal(Enum.GetValues<MenuCategory>().Length, dialog.Choices.Count);
        var drinkCount = menu.Query(MenuCategory.Minuman, null, MenuSort.Populer, selection.Selected.Id).Count;
        Equal($"{drinkCount} menu di {selection.Selected.Name}", drinkChoice.Description);
        home.SearchText = "teh";
        var oldIds = home.Recommendations.Select(card => card.Item.Id).ToArray();
        dialog.Result = _ => null;
        await home.ChooseCategoryCommand.ExecuteAsync(null);
        Equal(drinkChoice.Id, dialog.SelectedId);
        Equal("Minuman", home.SelectedCategoryLabel);
        Equal("teh", home.SearchText);
        Equal(drinkChoice.Description, dialog.Choices.Single(choice => choice.Title == "Minuman").Description);
        True(oldIds.SequenceEqual(home.Recommendations.Select(card => card.Item.Id)));
        var catalog = new MenuViewModel(menu, cart, ui, selection, dialog);
        dialog.Result = choices => choices.Single(choice => choice.Title == "Minuman").Id;
        await catalog.ChooseCategoryCommand.ExecuteAsync(null);
        Equal("Minuman", catalog.SelectedCategoryLabel);
        True(catalog.Items.All(card => card.Item.Category == MenuCategory.Minuman));
        True(catalog.Categories.Single(category => category.IsSelected).Category == MenuCategory.Minuman);
    }),
    ("Cross-canteen add is rejected before any cart mutation or persistence", () =>
    {
        var storage = new MemoryCartStorage();
        var cart = new CartService(storage);
        var a = menu.Find("geprek")!;
        var b = menu.Find("kantin-b:geprek")!;
        cart.Add(a, a.DefaultOptions(), 1);
        var snapshot = storage.Json;
        var writes = storage.Writes;
        Throws<CartCanteenConflictException>(() => cart.Add(b, b.DefaultOptions(), 1));
        Equal(snapshot, storage.Json);
        Equal(writes, storage.Writes);
        Equal(a.Id, cart.Items.Single().ItemId);
        Equal(1, cart.TotalQuantity);
        return Task.CompletedTask;
    }),
    ("Mixed-canteen reorder and invalid replacement preserve the existing cart atomically", () =>
    {
        var cart = new CartService(new MemoryCartStorage());
        var a = Line(menu.Find("geprek")!);
        var b = Line(menu.Find("kantin-b:geprek")!);
        cart.AddRange([a]);
        Throws<ArgumentException>(() => cart.AddRange([a, b]));
        Throws<ArgumentException>(() => cart.ReplaceWith([a, b]));
        Equal(1, cart.Items.Count);
        Equal(a.ItemId, cart.Items.Single().ItemId);
        return Task.CompletedTask;
    }),
    ("Declining a canteen change preserves the original cart", async () =>
    {
        var cart = new CartService(new MemoryCartStorage());
        var a = menu.Find("geprek")!;
        var b = menu.Find("kantin-b:geprek")!;
        cart.Add(a, a.DefaultOptions(), 2);
        var dialog = new CartDialog(false);
        True(!await cart.AddWithConfirmationAsync(dialog, b, b.DefaultOptions(), 1));
        Equal(1, dialog.Calls);
        Equal(a.Id, cart.Items.Single().ItemId);
        Equal(2, cart.TotalQuantity);
    }),
    ("Accepting a canteen change replaces once and keeps incoming options and amount", async () =>
    {
        var cart = new CartService(new MemoryCartStorage());
        var a = menu.Find("geprek")!;
        var b = menu.Find("kantin-b:geprek")!;
        cart.Add(a, a.DefaultOptions(), 1);
        var changes = 0;
        cart.Changed += (_, _) => changes++;
        var options = b.OptionGroups.Select(group => group.Options.Last()).ToArray();
        True(await cart.AddWithConfirmationAsync(new CartDialog(true), b, options, 2, "Pisah sambal"));
        Equal(1, changes);
        var line = cart.Items.Single();
        Equal("kantin-b", line.CanteenId);
        Equal(2, line.Quantity);
        Equal("Pisah sambal", line.Note);
        Equal(b.Price + options.Sum(option => option.ExtraPrice), line.UnitPrice);
        Equal(0, cart.QuantityOf(a.Id));
    }),
    ("Cart persistence preserves canteen offer options note and quantity on restart", async () =>
    {
        var storage = new MemoryCartStorage();
        var cart = new CartService(storage);
        var offer = menu.Find("kantin-c:mie-ayam")!;
        var options = offer.OptionGroups.Select(group => group.Options.Last()).ToArray();
        cart.Add(offer, options, 3, "Kuah dipisah");
        var restored = new CartService(storage);
        await restored.InitializeAsync();
        var line = restored.Items.Single();
        Equal(offer.CanteenId, line.CanteenId);
        Equal(offer.Id, line.ItemId);
        Equal(3, line.Quantity);
        Equal("Kuah dipisah", line.Note);
        True(options.SequenceEqual(line.Options));
        Equal((offer.Price + options.Sum(option => option.ExtraPrice)) * 3, restored.Subtotal);
    }),
    ("Legacy cart JSON without a canteen remains assigned to Kantin A", async () =>
    {
        var storage = new MemoryCartStorage
        {
            Json = """{"Lines":[{"ItemId":"geprek","Name":"Nasi Ayam Geprek","Stall":"Warung Bu Sri","UnitPrice":15000,"Quantity":1,"Options":[]}]}""",
        };
        var cart = new CartService(storage);
        await cart.InitializeAsync();
        Equal(CanteenCatalog.DefaultId, cart.Items.Single().CanteenId);
        Equal(15000m, cart.Subtotal);
    }),
};

foreach (var (name, run) in tests)
{
    await run();
    Console.WriteLine($"PASS {name}");
}
Console.WriteLine($"{tests.Length} catalog/cart regression tests passed.");

static CartItem Line(CanteenMenuItem item) => new()
{
    ItemId = item.Id, CanteenId = item.CanteenId, Name = item.Name, Stall = item.Stall,
    UnitPrice = item.Price, Quantity = 1, Options = [],
};
static void True(bool value) { if (!value) throw new InvalidOperationException("Expected true."); }
static void Equal<T>(T expected, T actual)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual)) throw new InvalidOperationException($"Expected {expected}, got {actual}.");
}
static void Throws<T>(Action action) where T : Exception
{
    try { action(); } catch (T) { return; }
    throw new InvalidOperationException($"Expected {typeof(T).Name}.");
}
