using KantinKita.Models;

// Only app-service dependencies are replaced. The linked view models, models,
// helpers, Toolkit generators and MAUI controls are the production implementations.
namespace KantinKita.Services;

public interface ICartService
{
    IReadOnlyList<CartItem> Items { get; }
    decimal Subtotal { get; }
    int TotalQuantity { get; }
    bool IsEmpty { get; }
    event EventHandler? Changed;
    void Add(CanteenMenuItem item, IReadOnlyList<MenuOption> options, int quantity, string? note = null);
    void AddRange(IEnumerable<CartItem> lines);
    void ReplaceWith(IEnumerable<CartItem> lines);
    int QuantityOf(string itemId);
    void Increase(CartItem item);
    void Decrease(CartItem item);
    void Remove(CartItem item);
    void Clear();
}

public interface IFavoriteService
{
    bool IsFavorite(string itemId);
    void Toggle(string itemId);
}

public interface IAppNavigator
{
    Task OpenItemAsync(CanteenMenuItem item);
    Task CloseAsync();
    Task OpenCheckoutAsync();
    Task ShowMenuAsync();
}

public interface IDialogService
{
    Task<bool> ConfirmAsync(string title, string message, string accept = "Ya", string cancel = "Batal");
    Task<string?> ChooseAsync(string title, IReadOnlyList<DialogChoice> choices, string? selectedId = null, string? message = null) =>
        Task.FromResult<string?>(null);
    Task<string?> ChooseCategoryAsync(string title, IReadOnlyList<DialogChoice> choices, string? selectedId = null, string? message = null) =>
        ChooseAsync(title, choices, selectedId, message);
}

public interface IProfileService
{
    StudentProfile Profile { get; }
    event EventHandler? Changed;
}

internal sealed class TestProfileService : IProfileService
{
    public event EventHandler? Changed { add { } remove { } }
    public StudentProfile Profile { get; } = new()
    {
        Name = "Test Student", Initials = "TS", Nim = "123", Program = "Program", Campus = "Campus",
    };
}

internal sealed class TestDependencies : ICartService, IFavoriteService, IAppNavigator, IDialogService
{
    public List<CartItem> CartItems { get; } = [];
    public IReadOnlyList<CartItem> Items => CartItems;
    public decimal Subtotal => CartItems.Sum(item => item.LineTotal);
    public int TotalQuantity => CartItems.Sum(item => item.Quantity);
    public bool IsEmpty => CartItems.Count == 0;
    public event EventHandler? Changed;
    public int AddCalls { get; private set; }
    public int FavoriteCalls { get; private set; }
    public int CloseCalls { get; private set; }
    public int QuantityOf(string itemId) => 0;
    public bool IsFavorite(string itemId) => false;
    public void Toggle(string itemId) => FavoriteCalls++;
    public void Add(CanteenMenuItem item, IReadOnlyList<MenuOption> options, int quantity, string? note = null) => AddCalls++;
    public void AddRange(IEnumerable<CartItem> lines) { CartItems.AddRange(lines); Changed?.Invoke(this, EventArgs.Empty); }
    public void ReplaceWith(IEnumerable<CartItem> lines) { CartItems.Clear(); CartItems.AddRange(lines); Changed?.Invoke(this, EventArgs.Empty); }
    public Task OpenItemAsync(CanteenMenuItem item) => Task.CompletedTask;
    public Task CloseAsync()
    {
        CloseCalls++;
        return Task.CompletedTask;
    }
    public Task OpenCheckoutAsync() => Task.CompletedTask;
    public Task ShowMenuAsync() => Task.CompletedTask;
    public Task<bool> ConfirmAsync(string title, string message, string accept = "Ya", string cancel = "Batal") => Task.FromResult(false);
    public void Increase(CartItem item) { item.Quantity++; Changed?.Invoke(this, EventArgs.Empty); }
    public void Decrease(CartItem item)
    {
        item.Quantity--;
        if (item.Quantity <= 0) CartItems.Remove(item);
        Changed?.Invoke(this, EventArgs.Empty);
    }
    public void Remove(CartItem item) { CartItems.Remove(item); Changed?.Invoke(this, EventArgs.Empty); }
    public void Clear() { CartItems.Clear(); Changed?.Invoke(this, EventArgs.Empty); }
}
