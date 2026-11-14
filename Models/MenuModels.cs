using System.Text.Json.Serialization;

namespace KantinKita.Models;

public enum MenuCategory
{
    Semua,
    Makanan,
    Minuman,
    Camilan,
    Sehat,
}

public enum MenuSort
{
    Populer,
    Termurah,
    Rating,
}

/// <summary>A single customisable choice inside an option group.</summary>
public sealed record MenuOption(string Name, decimal ExtraPrice = 0);

/// <summary>A group of choices shown on the item detail screen, e.g. spice level.</summary>
public sealed record MenuOptionGroup(string Title, IReadOnlyList<MenuOption> Options, bool IsRequired = true);

/// <summary>A canteen dish or drink.</summary>
public sealed class CanteenMenuItem
{
    public required string Id { get; init; }
    public string CanteenId { get; init; } = CanteenCatalog.DefaultId;
    [JsonIgnore]
    public string CanteenName => CanteenCatalog.Find(CanteenId)?.Name ?? CanteenId;
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required string Stall { get; init; }
    public required MenuCategory Category { get; init; }
    public required decimal Price { get; init; }

    [JsonIgnore]
    public string IconGlyph => MenuIconMap.ForItem(Id, Category);
    public string TileStart { get; init; } = "#E9F5EE";
    public string TileEnd { get; init; } = "#D5EBDD";

    public double Rating { get; init; } = 4.8;
    public int ReviewCount { get; init; } = 120;
    public int PrepMinutes { get; init; } = 10;
    public int SoldCount { get; init; } = 180;
    public int Stock { get; init; } = 20;

    public bool IsSpicy { get; init; }
    public bool IsBestSeller { get; init; }
    public IReadOnlyList<MenuOptionGroup> OptionGroups { get; init; } = [];

    /// <summary>Cheapest possible configuration, used by quick add.</summary>
    public IReadOnlyList<MenuOption> DefaultOptions() =>
        OptionGroups.Select(group => group.Options.Count > 0 ? group.Options[0] : null)
            .Where(option => option is not null)
            .Select(option => option!)
            .ToArray();
}

public sealed class CartItem
{
    public required string ItemId { get; init; }
    public string CanteenId { get; init; } = CanteenCatalog.DefaultId;
    [JsonIgnore]
    public string CanteenName => CanteenCatalog.Find(CanteenId)?.Name ?? CanteenId;
    public required string Name { get; init; }
    // The menu identity restores its icon even for carts saved by older app versions.
    [JsonIgnore]
    public string IconGlyph => MenuIconMap.ForItem(ItemId);
    public string Stall { get; init; } = string.Empty;
    public decimal UnitPrice { get; init; }
    public int Quantity { get; set; }
    public string? Note { get; set; }
    public IReadOnlyList<MenuOption> Options { get; init; } = [];

    public decimal LineTotal => UnitPrice * Quantity;

    public string OptionsSummary => string.Join(", ", Options.Select(option => option.Name));

    public string Signature => $"{ItemId}|{OptionsSummary}";

    public CartItem Clone() => new()
    {
        ItemId = ItemId,
        CanteenId = CanteenId,
        Name = Name,
        Stall = Stall,
        UnitPrice = UnitPrice,
        Quantity = Quantity,
        Note = Note,
        Options = Options,
    };
}

public enum OrderStatus
{
    Confirmed,
    Preparing,
    Ready,
    Completed,
    Cancelled,
}

public sealed class Order
{
    public required string Code { get; init; }
    public required DateTimeOffset PlacedAt { get; init; }
    public required IReadOnlyList<CartItem> Items { get; init; }
    public required string PaymentMethod { get; init; }

    public decimal Subtotal { get; init; }
    public decimal ServiceFee { get; init; }
    public decimal Discount { get; init; }
    public decimal Total => Subtotal + ServiceFee - Discount;

    public string? VoucherCode { get; init; }
    public string? Note { get; init; }
    public TimeSpan PickupIn { get; init; }
    public OrderStatus Status { get; init; }
    public OrderDestination? Destination { get; init; }

    /// <summary>Live orders walk through the kitchen states based on their age.</summary>
    public bool IsLive { get; init; }

    public int ItemCount => Items.Sum(item => item.Quantity);

    public DateTimeOffset PickupAt => PlacedAt + PickupIn;
}

public sealed record Voucher(
    string Code,
    string Title,
    string Description,
    decimal MinSubtotal,
    decimal FlatDiscount = 0,
    decimal Percent = 0,
    decimal MaxDiscount = decimal.MaxValue);

public readonly record struct VoucherEvaluation(decimal Discount, string? Error)
{
    public bool IsValid => Error is null;
}

public sealed class StudentProfile
{
    public required string Name { get; set; }
    public required string Nim { get; init; }
    public required string Program { get; init; }
    public required string Campus { get; init; }
    public required string Initials { get; set; }

    public decimal Balance { get; set; }
    public int Points { get; set; }
    public int OrderCount { get; set; }
}

