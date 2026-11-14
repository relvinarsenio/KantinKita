using KantinKita.Models;
using KantinKita.Services;
using KantinKita.ViewModels;

var tests = new (string Name, Action Run)[]
{
    ("Empty order history produces no notifications or unread dot", () =>
    {
        var vm = new HomeNotificationsViewModel(new TestOrders(), new MemoryPreferences());
        Equal(0, vm.Entries.Count);
        True(!vm.HasEntries && !vm.HasUnread);
    }),
    ("Only four latest real orders appear and refreshing never marks them read", () =>
    {
        var orders = BuildOrders();
        var preferences = new MemoryPreferences();
        var vm = new HomeNotificationsViewModel(orders, preferences);
        Equal(4, vm.Entries.Count);
        Equal("KT-5", vm.Entries[0].Order.Code);
        Equal("KT-2", vm.Entries[^1].Order.Code);
        True(vm.HasEntries && vm.HasUnread);
        Equal(0, preferences.Count);
        True(vm.Entries[0].Message.Contains("KT-5") && vm.Entries[0].Message.Contains("Rp9.000"));
        vm.Refresh();
        Equal(4, vm.Entries.Count);
        Equal(0, preferences.Count);
    }),
    ("Read state survives a new feed while a new order status becomes unread", () =>
    {
        var orders = BuildOrders();
        var preferences = new MemoryPreferences();
        var vm = new HomeNotificationsViewModel(orders, preferences);
        vm.Refresh(markRead: true);
        True(!vm.HasUnread);
        var reopened = new HomeNotificationsViewModel(orders, preferences);
        True(!reopened.HasUnread);
        orders.Statuses["KT-5"] = OrderStatus.Ready;
        reopened.Refresh();
        True(reopened.HasUnread);
        Equal("Siap diambil", reopened.Entries[0].Title);
        reopened.Refresh(markRead: true);
        True(!reopened.HasUnread);
    }),
    ("Removed history clears stale notification entries", () =>
    {
        var orders = BuildOrders();
        var vm = new HomeNotificationsViewModel(orders, new MemoryPreferences());
        orders.Items.Clear();
        vm.Refresh();
        Equal(0, vm.Entries.Count);
        True(!vm.HasEntries && !vm.HasUnread);
    }),
};

foreach (var (name, run) in tests)
{
    run();
    Console.WriteLine($"PASS {name}");
}
Console.WriteLine($"{tests.Length} notification regression tests passed.");

static TestOrders BuildOrders()
{
    var orders = new TestOrders();
    for (var i = 0; i < 6; i++)
    {
        orders.Items.Add(new Order
        {
            Code = $"KT-{i}",
            PlacedAt = new DateTimeOffset(2026, 10, 26, 12, 0, 0, TimeSpan.FromHours(7)).AddMinutes(i),
            Items = [new CartItem { ItemId = "es-teh", Name = "Es teh", UnitPrice = 4000, Quantity = 2 }],
            Subtotal = 8000,
            ServiceFee = 1000,
            PaymentMethod = "Tunai",
            Status = OrderStatus.Confirmed,
        });
    }
    return orders;
}

static void True(bool condition)
{
    if (!condition) throw new InvalidOperationException("Expected true.");
}

static void Equal<T>(T expected, T actual)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual)) throw new InvalidOperationException($"Expected {expected}, got {actual}.");
}
