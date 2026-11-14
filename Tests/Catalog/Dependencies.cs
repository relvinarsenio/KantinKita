using System.Text.Json;
using Microsoft.Maui.Storage;
using KantinKita.Models;

namespace KantinKita.Services;

public interface IAppNavigator
{
    Task OpenItemAsync(CanteenMenuItem item);
    Task ShowMenuAsync();
}
public interface IProfileService
{
    StudentProfile Profile { get; }
    event EventHandler? Changed;
}
internal sealed class UiDependencies : IAppNavigator, IProfileService
{
    public Task OpenItemAsync(CanteenMenuItem item) => Task.CompletedTask;
    public Task ShowMenuAsync() => Task.CompletedTask;
    public StudentProfile Profile { get; } = new()
    {
        Name = "Test", Initials = "T", Nim = "123", Program = "Program", Campus = "Campus",
    };
    public event EventHandler? Changed { add { } remove { } }
}

public interface IDialogService
{
    Task<bool> ConfirmAsync(string title, string message, string accept = "Ya", string cancel = "Batal");
    Task<string?> ChooseAsync(string title, IReadOnlyList<DialogChoice> choices, string? selectedId = null, string? message = null) =>
        Task.FromResult<string?>(null);
    Task<string?> ChooseCategoryAsync(string title, IReadOnlyList<DialogChoice> choices, string? selectedId = null, string? message = null) =>
        ChooseAsync(title, choices, selectedId, message);
}

internal sealed class CategoryDialog : IDialogService
{
    public IReadOnlyList<DialogChoice> Choices { get; private set; } = [];
    public string? SelectedId { get; private set; }
    public Func<IReadOnlyList<DialogChoice>, string?> Result { get; set; } = _ => null;
    public Task<bool> ConfirmAsync(string title, string message, string accept = "Ya", string cancel = "Batal") =>
        Task.FromResult(false);
    public Task<string?> ChooseAsync(string title, IReadOnlyList<DialogChoice> choices, string? selectedId = null, string? message = null)
    {
        Choices = choices;
        SelectedId = selectedId;
        return Task.FromResult(Result(choices));
    }
}

internal sealed class CartDialog(bool accepted) : IDialogService
{
    public int Calls { get; private set; }
    public Task<bool> ConfirmAsync(string title, string message, string accept = "Ya", string cancel = "Batal")
    {
        Calls++;
        return Task.FromResult(accepted);
    }
}

/// <summary>Uses the production JSON context, without touching a device or real user data.</summary>
internal sealed class MemoryCartStorage : ICartStorage
{
    public string? Json { get; set; }
    public int Writes { get; private set; }
    public Task<CartSnapshot?> LoadAsync() => Task.FromResult(Json is null ? null :
        JsonSerializer.Deserialize(Json, AppJsonContext.Default.CartSnapshot));
    public Task SaveAsync(CartSnapshot snapshot)
    {
        Json = JsonSerializer.Serialize(snapshot, AppJsonContext.Default.CartSnapshot);
        Writes++;
        return Task.CompletedTask;
    }
}

internal sealed class MemoryPreferences : IPreferences
{
    private readonly Dictionary<string, object?> _values = [];
    private static string Key(string key, string? sharedName) => $"{sharedName}|{key}";
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
