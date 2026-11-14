using KantinKita.Models;

namespace KantinKita.Services;

public interface ICanteenSelectionService
{
    Canteen Selected { get; }
    event EventHandler? Changed;
    void Select(string canteenId);
}

/// <summary>Browsing a different canteen never modifies the cart.</summary>
public sealed class CanteenSelectionService : ICanteenSelectionService
{
    private const string Key = "catalog.selectedCanteen";
    private readonly IPreferences _preferences;
    private Canteen _selected;

    public CanteenSelectionService(IPreferences preferences)
    {
        _preferences = preferences;
        _selected = CanteenCatalog.Find(preferences.Get(Key, CanteenCatalog.DefaultId)) ?? CanteenCatalog.All[0];
    }

    public Canteen Selected => _selected;
    public event EventHandler? Changed;

    public void Select(string canteenId)
    {
        var canteen = CanteenCatalog.Find(canteenId) ?? throw new ArgumentException("Kantin tidak ditemukan.", nameof(canteenId));
        if (canteen.Id == _selected.Id) return;
        _preferences.Set(Key, canteen.Id);
        _selected = canteen;
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
