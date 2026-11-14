using System.Text.Json;
using KantinKita.Helpers;
using KantinKita.Models;
using KantinKita.Services;
using KantinKita.ViewModels;

var tests = new (string Name, Func<Task> Run)[]
{
    ("New history contains no fabricated purchases", async () =>
    {
        using var directory = new TestDirectory();
        var orders = new OrderService(new OrderStorage(directory.Path));
        await orders.InitializeAsync();
        True(orders.IsInitialized);
        Equal(0, orders.Orders.Count);
        True(orders.ActiveOrder is null && orders.LoadError is null);
    }),
    ("Order is durable before publish and survives a restart", async () =>
    {
        using var directory = new TestDirectory();
        var storage = new OrderStorage(directory.Path);
        var orders = new OrderService(storage);
        var line = Item();
        var order = await orders.PlaceAsync([line], 36000m, 5000m, "Tunai di kasir", "HEMAT5", "Ambil bareng", TimeSpan.FromMinutes(15));
        Equal(32000m, order.Total);
        True(File.Exists(System.IO.Path.Combine(directory.Path, "kantin-orders.json")));
        var loaded = new OrderService(new OrderStorage(directory.Path));
        await loaded.InitializeAsync();
        Equal(1, loaded.Orders.Count);
        var saved = loaded.Orders[0];
        Equal(order.Code, saved.Code);
        Equal(order.PlacedAt, saved.PlacedAt);
        Equal(32000m, saved.Total);
        Equal("HEMAT5", saved.VoucherCode);
        Equal("Ambil bareng", saved.Note);
        Equal("Tanpa sambal", saved.Items[0].Note);
        Equal(3000m, saved.Items[0].Options[0].ExtraPrice);
        True(!saved.IsLive);
        Equal(OrderStatus.Confirmed, loaded.GetStatus(saved));
        var second = await loaded.PlaceAsync([Item()], 36000m, 0m, "QRIS", null, null, TimeSpan.Zero);
        True(second.Code != order.Code);
    }),
    ("Mutable cart data does not change a committed receipt", async () =>
    {
        var storage = new MemoryStorage();
        var orders = new OrderService(storage);
        var options = new List<MenuOption> { new("Telur", 3000m) };
        var line = new CartItem { ItemId = "geprek", Name = "Nasi ayam", UnitPrice = 18000m, Quantity = 2, Options = options, Note = "Asli" };
        var saved = await orders.PlaceAsync([line], 36000m, 0m, "Tunai", null, null, TimeSpan.Zero);
        line.Quantity = 99;
        line.Note = "Diubah";
        options.Clear();
        Equal(2, saved.Items[0].Quantity);
        Equal("Asli", saved.Items[0].Note);
        Equal(1, saved.Items[0].Options.Count);
        Equal(37000m, saved.Total);
    }),
    ("Concurrent submissions keep unique IDs and every order", async () =>
    {
        var storage = new MemoryStorage { DelayWrites = true };
        var orders = new OrderService(storage);
        var placed = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ =>
            orders.PlaceAsync([Item()], 36000m, 0m, "Tunai", null, null, TimeSpan.Zero)));
        Equal(8, placed.Select(order => order.Code).Distinct().Count());
        Equal(8, orders.Orders.Count);
        Equal(8, (await storage.LoadAsync()).Orders.Count);
    }),
    ("Failed write does not publish an order or consume a sequence", async () =>
    {
        var storage = new MemoryStorage { FailWrites = true };
        var orders = new OrderService(storage);
        await Throws<IOException>(() => orders.PlaceAsync([Item()], 36000m, 0m, "Tunai", null, null, TimeSpan.Zero));
        Equal(0, orders.Orders.Count);
        storage.FailWrites = false;
        var first = await orders.PlaceAsync([Item()], 36000m, 0m, "Tunai", null, null, TimeSpan.Zero);
        True(first.Code.EndsWith("-000001", StringComparison.Ordinal));
    }),
    ("Corrupt history is preserved and cannot be overwritten by checkout", async () =>
    {
        using var directory = new TestDirectory();
        var file = System.IO.Path.Combine(directory.Path, "kantin-orders.json");
        await File.WriteAllTextAsync(file, "not valid JSON");
        var orders = new OrderService(new OrderStorage(directory.Path));
        await orders.InitializeAsync();
        True(!orders.IsInitialized && orders.LoadError is not null);
        await Throws<IOException>(() => orders.PlaceAsync([Item()], 36000m, 0m, "Tunai", null, null, TimeSpan.Zero));
        Equal("not valid JSON", await File.ReadAllTextAsync(file));
    }),
    ("Null options in saved JSON are rejected without losing history", async () =>
    {
        using var directory = new TestDirectory();
        var file = System.IO.Path.Combine(directory.Path, "kantin-orders.json");
        const string invalid = """{"Orders":[{"Code":"KT-X","PaymentMethod":"Tunai","Subtotal":4000,"Items":[{"ItemId":"es-teh","Name":"Es teh","Quantity":1,"UnitPrice":4000,"Options":[null]}]}]}""";
        await File.WriteAllTextAsync(file, invalid);
        var orders = new OrderService(new OrderStorage(directory.Path));
        await orders.InitializeAsync();
        True(!orders.IsInitialized && orders.LoadError is not null);
        await Throws<IOException>(() => orders.PlaceAsync([Item()], 36000m, 0m, "Tunai", null, null, TimeSpan.Zero));
        Equal(invalid, await File.ReadAllTextAsync(file));
    }),
    ("Incorrect totals are rejected instead of producing a misleading receipt", async () =>
    {
        var orders = new OrderService(new MemoryStorage());
        await Throws<ArgumentException>(() => orders.PlaceAsync([Item()], 1m, 0m, "Tunai", null, null, TimeSpan.Zero));
        await Throws<ArgumentException>(() => orders.PlaceAsync([Item()], 36000m, 36001m, "Tunai", null, null, TimeSpan.Zero));
        Equal(0, orders.Orders.Count);
    }),
    ("Receipt uses actual totals, options, note and payment without claiming paid", async () =>
    {
        var orders = new OrderService(new MemoryStorage());
        var order = await orders.PlaceAsync([Item()], 36000m, 5000m, "Tunai di kasir", "HEMAT5", "Ambil sore", TimeSpan.FromMinutes(15));
        var dependencies = new ReceiptDependencies();
        var vm = new ReceiptViewModel(orders, dependencies, dependencies, dependencies);
        True(!vm.SharePdfCommand.CanExecute(null));
        vm.Prepare(order);
        Equal(order.Code, vm.Code);
        Equal("Rp36.000", vm.SubtotalText);
        Equal("Rp1.000", vm.ServiceFeeText);
        Equal("−Rp5.000", vm.DiscountText);
        Equal("Rp32.000", vm.TotalText);
        Equal("Tunai di kasir", vm.PaymentText);
        Equal("Ambil sore", vm.NoteText);
        Equal("2 × Rp18.000", vm.Lines[0].QuantityPriceText);
        Equal("Rp36.000", vm.Lines[0].LineTotalText);
        True(vm.Lines[0].OptionsText.Contains("Telur") && vm.Lines[0].OptionsText.Contains("Rp3.000"));
        True(vm.HasNote && vm.HasVoucher && vm.HasDiscount && vm.SharePdfCommand.CanExecute(null));
        Equal(OrderStatusDisplay.Text(order.Status), vm.StatusText);
        True(!vm.StatusText.Contains("Lunas", StringComparison.OrdinalIgnoreCase));
    }),
    ("Delivery destination and canteen survive storage and receipt mapping", async () =>
    {
        using var directory = new TestDirectory();
        var orders = new OrderService(new OrderStorage(directory.Path));
        var line = Item();
        line = new CartItem { ItemId = "kantin-b:geprek", CanteenId = "kantin-b", Name = line.Name,
            Stall = line.Stall, UnitPrice = line.UnitPrice, Quantity = line.Quantity, Options = line.Options };
        var destination = new OrderDestination { CanteenId = "kantin-b", CanteenName = "Untrusted title",
            Mode = FulfillmentMode.Delivery, Building = "Gedung C", Room = "Ruang 302 · lantai 3" };
        await orders.PlaceAsync([line], line.LineTotal, 0m, "Tunai", null, null, TimeSpan.FromMinutes(10), destination);
        var loaded = new OrderService(new OrderStorage(directory.Path));
        await loaded.InitializeAsync();
        var saved = loaded.Orders.Single();
        Equal("kantin-b", saved.Items.Single().CanteenId);
        Equal(CanteenCatalog.Find("kantin-b")!.Name, saved.Destination!.CanteenName);
        Equal(FulfillmentMode.Delivery, saved.Destination.Mode);
        Equal("Gedung C", saved.Destination.Building);
        Equal("Ruang 302 · lantai 3", saved.Destination.Room);
        var dependencies = new ReceiptDependencies();
        var vm = new ReceiptViewModel(loaded, dependencies, dependencies, dependencies);
        vm.Prepare(saved);
        Equal("Antar ke ruangan", vm.FulfillmentText);
        Equal("Gedung C · Ruang 302 · lantai 3", vm.DestinationText);
        Equal(CanteenCatalog.Find("kantin-b")!.Name, vm.CanteenText);
    }),
    ("Local active dine-in orders make their table unavailable within the same canteen", async () =>
    {
        var orders = new OrderService(new MemoryStorage());
        var provider = new LocalTableAvailabilityProvider(orders);
        var fulfillment = new FulfillmentService(provider);
        var table = (await provider.GetTablesAsync("kantin-a")).First(table => table.IsAvailable);
        var destination = new OrderDestination { CanteenId = "kantin-a", CanteenName = "Kantin A",
            Mode = FulfillmentMode.DineIn, TableId = table.Id, TableLabel = table.Label };
        True(await fulfillment.ValidateAsync(destination) is null);
        await orders.PlaceAsync([Item()], 36000m, 0m, "Tunai", null, null, TimeSpan.Zero, destination);
        await Throws<ArgumentException>(() => orders.PlaceAsync([Item()], 36000m, 0m, "Tunai", null, null, TimeSpan.Zero, destination));
        Equal(1, orders.Orders.Count);
        True(await fulfillment.ValidateAsync(destination) is not null);
        True(!(await provider.GetTablesAsync("kantin-a")).Single(row => row.Id == table.Id).IsAvailable);
        True((await provider.GetTablesAsync("kantin-b")).Single(row => row.Id == "kantin-b:table-01").IsAvailable);
        True(!((await provider.GetTablesAsync("kantin-a")).Single(row => row.Id == "kantin-a:table-02")).IsAvailable);
        True(provider.AvailabilityNote.Contains("Simulasi", StringComparison.Ordinal));
    }),
    ("Missing delivery room and mismatched canteen are rejected before saving", async () =>
    {
        var orders = new OrderService(new MemoryStorage());
        var incomplete = new OrderDestination { CanteenId = "kantin-a", CanteenName = "Kantin A",
            Mode = FulfillmentMode.Delivery, Building = "Gedung B" };
        await Throws<ArgumentException>(() => orders.PlaceAsync([Item()], 36000m, 0m, "Tunai", null, null, TimeSpan.Zero, incomplete));
        var mismatch = incomplete with { CanteenId = "kantin-b", CanteenName = "Kantin B", Room = "Ruang 201" };
        await Throws<ArgumentException>(() => orders.PlaceAsync([Item()], 36000m, 0m, "Tunai", null, null, TimeSpan.Zero, mismatch));
        Equal(0, orders.Orders.Count);
    }),
    ("Legacy history without destination restores as pickup at its original canteen", async () =>
    {
        using var directory = new TestDirectory();
        const string legacy = """{"Orders":[{"Code":"KT-LEGACY","PaymentMethod":"Tunai","Subtotal":4000,"ServiceFee":1000,"Items":[{"ItemId":"es-teh","Name":"Es teh","Quantity":1,"UnitPrice":4000}]}]}""";
        await File.WriteAllTextAsync(System.IO.Path.Combine(directory.Path, "kantin-orders.json"), legacy);
        var orders = new OrderService(new OrderStorage(directory.Path));
        await orders.InitializeAsync();
        True(orders.IsInitialized);
        var legacyOrder = orders.Orders.Single();
        True(legacyOrder.Destination is null);
        Equal("Ambil sendiri", FulfillmentDisplay.Mode(legacyOrder));
        Equal(CanteenCatalog.Find("kantin-a")!.Name, FulfillmentDisplay.Canteen(legacyOrder));
        Equal(5000m, legacyOrder.Total);
    }),
    ("Malformed saved destination preserves history instead of discarding it", async () =>
    {
        using var directory = new TestDirectory();
        const string malformed = """{"Orders":[{"Code":"KT-BAD-DEST","PaymentMethod":"Tunai","Subtotal":4000,"Items":[{"ItemId":"es-teh","Name":"Es teh","Quantity":1,"UnitPrice":4000}],"Destination":{"CanteenId":"kantin-a","CanteenName":"Kantin A","Mode":2,"Building":"Gedung B"}}]}""";
        var path = System.IO.Path.Combine(directory.Path, "kantin-orders.json");
        await File.WriteAllTextAsync(path, malformed);
        var orders = new OrderService(new OrderStorage(directory.Path));
        await orders.InitializeAsync();
        True(!orders.IsInitialized);
        Equal(malformed, await File.ReadAllTextAsync(path));
    }),
};

foreach (var (name, run) in tests)
{
    await run();
    Console.WriteLine($"PASS {name}");
}
Console.WriteLine($"{tests.Length} order/receipt regression tests passed.");

static CartItem Item() => new()
{
    ItemId = "geprek", Name = "Nasi ayam", Stall = "Warung Ayam", UnitPrice = 18000m, Quantity = 2,
    Options = [new("Telur", 3000m)], Note = "Tanpa sambal",
};
static void True(bool condition) { if (!condition) throw new InvalidOperationException("Expected condition to be true."); }
static void Equal<T>(T expected, T actual) { if (!EqualityComparer<T>.Default.Equals(expected, actual)) throw new InvalidOperationException($"Expected {expected}, got {actual}."); }
static async Task Throws<T>(Func<Task> action) where T : Exception
{
    try { await action(); } catch (T) { return; }
    throw new InvalidOperationException($"Expected {typeof(T).Name}.");
}

sealed class MemoryStorage : IOrderStorage
{
    private string _json = "{\"LastSequence\":0,\"Orders\":[]}";
    public bool FailWrites { get; set; }
    public bool DelayWrites { get; set; }
    public Task<OrderHistorySnapshot> LoadAsync() => Task.FromResult(JsonSerializer.Deserialize(_json, OrderJsonContext.Default.OrderHistorySnapshot)!);
    public async Task SaveAsync(OrderHistorySnapshot snapshot)
    {
        if (FailWrites) throw new IOException("Expected test failure.");
        if (DelayWrites) await Task.Delay(8).ConfigureAwait(false);
        _json = JsonSerializer.Serialize(snapshot, OrderJsonContext.Default.OrderHistorySnapshot);
    }
}

sealed class TestDirectory : IDisposable
{
    private readonly string _root = System.IO.Path.GetFullPath(AppContext.BaseDirectory).TrimEnd(System.IO.Path.DirectorySeparatorChar) + System.IO.Path.DirectorySeparatorChar;
    public string Path { get; }
    public TestDirectory()
    {
        Path = System.IO.Path.GetFullPath(System.IO.Path.Combine(_root, "order-tests-" + Guid.NewGuid().ToString("N")));
        Directory.CreateDirectory(Path);
    }
    public void Dispose()
    {
        if (!Path.StartsWith(_root, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Test cleanup escaped its output directory.");
        Directory.Delete(Path, recursive: true);
    }
}
