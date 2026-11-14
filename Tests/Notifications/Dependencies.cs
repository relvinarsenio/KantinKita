using KantinKita.Models;
using Microsoft.Maui.Storage;

namespace KantinKita.Services;

// Only application-service contracts are replaced. The production feed, models,
// MAUI preferences contract, and MVVM generator remain unchanged.
public interface IOrderService
{
    IReadOnlyList<Order> Orders { get; }
    event EventHandler? Changed;
    OrderStatus GetStatus(Order order);
}

internal sealed class TestOrders : IOrderService
{
    public List<Order> Items { get; } = [];
    public Dictionary<string, OrderStatus> Statuses { get; } = [];
    public IReadOnlyList<Order> Orders => Items;
    public event EventHandler? Changed { add { } remove { } }
    public OrderStatus GetStatus(Order order) => Statuses.TryGetValue(order.Code, out var status) ? status : order.Status;
}

internal sealed class MemoryPreferences : IPreferences
{
    private readonly Dictionary<string, object?> _values = [];
    private static string Key(string key, string? sharedName) => $"{sharedName}|{key}";
    public int Count => _values.Count;
    public bool ContainsKey(string key, string? sharedName = null) => _values.ContainsKey(Key(key, sharedName));
    public void Remove(string key, string? sharedName = null) => _values.Remove(Key(key, sharedName));
    public void Clear(string? sharedName = null)
    {
        foreach (var key in _values.Keys.Where(key => key.StartsWith($"{sharedName}|", StringComparison.Ordinal)).ToArray()) _values.Remove(key);
    }
    public void Set<T>(string key, T value, string? sharedName = null) => _values[Key(key, sharedName)] = value;
    public T Get<T>(string key, T defaultValue, string? sharedName = null) =>
        _values.TryGetValue(Key(key, sharedName), out var value) && value is T typed ? typed : defaultValue;
}
