using KantinKita.Models;

namespace KantinKita.Services;

public interface ICartService
{
    IReadOnlyList<CartItem> Items { get; }
    int TotalQuantity { get; }
    decimal Subtotal { get; }
    bool IsEmpty { get; }

    event EventHandler? Changed;

    Task InitializeAsync();
    void Add(CanteenMenuItem item, IReadOnlyList<MenuOption> options, int quantity, string? note = null);
    void AddRange(IEnumerable<CartItem> lines);
    void ReplaceWith(IEnumerable<CartItem> lines);
    void Increase(CartItem line);
    void Decrease(CartItem line);
    void Remove(CartItem line);
    void Clear();
    int QuantityOf(string itemId);
}

public sealed class CartService : ICartService
{
    private readonly List<CartItem> _items = [];
    private readonly ICartStorage _storage;

    public CartService(ICartStorage storage) => _storage = storage;

    public IReadOnlyList<CartItem> Items => _items;

    public int TotalQuantity => _items.Sum(item => item.Quantity);

    public decimal Subtotal => _items.Sum(item => item.LineTotal);

    public bool IsEmpty => _items.Count == 0;

    public event EventHandler? Changed;

    public async Task InitializeAsync()
    {
        var snapshot = await _storage.LoadAsync();
        if (snapshot is null || snapshot.Lines.Count == 0)
        {
            return;
        }

        foreach (var line in snapshot.Lines.Where(line => line.Quantity > 0))
        {
            _items.Add(ToCartItem(line));
        }

        RaiseChanged();
    }

    public void Add(CanteenMenuItem item, IReadOnlyList<MenuOption> options, int quantity, string? note = null)
    {
        if (quantity <= 0)
        {
            return;
        }

        EnsureCanteen(item.CanteenId);

        var unitPrice = item.Price + options.Sum(option => option.ExtraPrice);
        var signature = $"{item.Id}|{string.Join(", ", options.Select(option => option.Name))}";
        var existing = _items.FirstOrDefault(line =>
            line.Signature == signature && string.Equals(line.Note, note, StringComparison.Ordinal));

        if (existing is not null)
        {
            existing.Quantity += quantity;
        }
        else
        {
            _items.Add(new CartItem
            {
                ItemId = item.Id,
                CanteenId = item.CanteenId,
                Name = item.Name,
                Stall = item.Stall,
                UnitPrice = unitPrice,
                Quantity = quantity,
                Note = note,
                Options = options,
            });
        }

        RaiseChanged();
    }

    public void AddRange(IEnumerable<CartItem> lines)
    {
        var incoming = ValidateLines(lines);
        if (incoming.Length == 0) return;
        EnsureCanteen(incoming[0].CanteenId);
        _items.AddRange(incoming);
        RaiseChanged();
    }

    public void ReplaceWith(IEnumerable<CartItem> lines)
    {
        // Validate and snapshot first: replacing a cart is one observable mutation.
        var incoming = ValidateLines(lines);
        _items.Clear();
        _items.AddRange(incoming);
        RaiseChanged();
    }

    private void EnsureCanteen(string incomingId)
    {
        if (_items.FirstOrDefault(line => line.CanteenId != incomingId) is { } existing)
            throw new CartCanteenConflictException(existing.CanteenName,
                CanteenCatalog.Find(incomingId)?.Name ?? incomingId);
    }

    private static CartItem[] ValidateLines(IEnumerable<CartItem> lines)
    {
        var incoming = lines.Select(line => line.Clone()).ToArray();
        if (incoming.Any(line => line.Quantity <= 0 || line.UnitPrice < 0 || CanteenCatalog.Find(line.CanteenId) is null))
            throw new ArgumentException("Rincian keranjang tidak valid.", nameof(lines));
        if (incoming.Select(line => line.CanteenId).Distinct(StringComparer.Ordinal).Count() > 1)
            throw new ArgumentException("Satu pesanan hanya dapat berasal dari satu kantin.", nameof(lines));
        return incoming;
    }

    public void Increase(CartItem line)
    {
        line.Quantity++;
        RaiseChanged();
    }

    public void Decrease(CartItem line)
    {
        line.Quantity--;
        if (line.Quantity <= 0)
        {
            _items.Remove(line);
        }

        RaiseChanged();
    }

    public void Remove(CartItem line)
    {
        if (_items.Remove(line))
        {
            RaiseChanged();
        }
    }

    public void Clear()
    {
        if (_items.Count == 0)
        {
            return;
        }

        _items.Clear();
        RaiseChanged();
    }

    public int QuantityOf(string itemId) =>
        _items.Where(line => line.ItemId == itemId).Sum(line => line.Quantity);

    private void RaiseChanged()
    {
        Changed?.Invoke(this, EventArgs.Empty);
        _ = SaveAsync();
    }

    private async Task SaveAsync() =>
        await _storage.SaveAsync(new CartSnapshot
        {
            Lines = _items.Select(line => new CartLineSnapshot
            {
                ItemId = line.ItemId,
                CanteenId = line.CanteenId,
                Name = line.Name,
                Stall = line.Stall,
                UnitPrice = line.UnitPrice,
                Quantity = line.Quantity,
                Note = line.Note,
                Options = line.Options
                    .Select(option => new CartOptionSnapshot { Name = option.Name, ExtraPrice = option.ExtraPrice })
                    .ToList(),
            }).ToList(),
        });

    private static CartItem ToCartItem(CartLineSnapshot line) => new()
    {
        ItemId = line.ItemId,
        CanteenId = line.CanteenId,
        Name = line.Name,
        Stall = line.Stall,
        UnitPrice = line.UnitPrice,
        Quantity = line.Quantity,
        Note = line.Note,
        Options = line.Options
            .Select(option => new MenuOption(option.Name, option.ExtraPrice))
            .ToArray(),
    };
}

