using System.Text.Json;
using System.Text.Json.Serialization;
using KantinKita.Models;

namespace KantinKita.Services;

public sealed class OrderHistorySnapshot
{
    public int LastSequence { get; set; }
    public List<OrderRecord> Orders { get; set; } = [];
}

public sealed class OrderRecord
{
    public string Code { get; set; } = string.Empty;
    public DateTimeOffset PlacedAt { get; set; }
    public List<OrderLineRecord> Items { get; set; } = [];
    public string PaymentMethod { get; set; } = string.Empty;
    public decimal Subtotal { get; set; }
    public decimal ServiceFee { get; set; }
    public decimal Discount { get; set; }
    public string? VoucherCode { get; set; }
    public string? Note { get; set; }
    public TimeSpan PickupIn { get; set; }
    public OrderStatus Status { get; set; }
    public OrderDestination? Destination { get; set; }

    public static OrderRecord FromOrder(Order order) => new()
    {
        Code = order.Code, PlacedAt = order.PlacedAt, PaymentMethod = order.PaymentMethod,
        Subtotal = order.Subtotal, ServiceFee = order.ServiceFee, Discount = order.Discount,
        VoucherCode = order.VoucherCode, Note = order.Note, PickupIn = order.PickupIn, Status = order.Status,
        Destination = order.Destination,
        Items = order.Items.Select(line => new OrderLineRecord
        {
            ItemId = line.ItemId, Name = line.Name, Stall = line.Stall, Quantity = line.Quantity,
            CanteenId = line.CanteenId,
            UnitPrice = line.UnitPrice, Note = line.Note,
            Options = line.Options.Select(option => new MenuOption(option.Name, option.ExtraPrice)).ToList(),
        }).ToList(),
    };

    public Order ToOrder()
    {
        if (string.IsNullOrWhiteSpace(Code) || string.IsNullOrWhiteSpace(PaymentMethod) || Items is null || Items.Count == 0 ||
            Items.Any(item => item is null || item.Quantity <= 0 || item.UnitPrice < 0 || string.IsNullOrWhiteSpace(item.Name)
                || item.Options?.Any(option => option is null) == true) ||
            Items.Sum(item => item.Quantity * item.UnitPrice) != Subtotal || ServiceFee < 0 || Discount < 0 || Discount > Subtotal ||
            !Enum.IsDefined(Status)) throw new InvalidDataException("Rincian pesanan tersimpan tidak valid.");
        if (Destination?.ValidationError() is not null || Destination is not null && Items.Any(item => item.CanteenId != Destination.CanteenId))
            throw new InvalidDataException("Tujuan pesanan tersimpan tidak valid.");
        return new Order
        {
            Code = Code, PlacedAt = PlacedAt, PaymentMethod = PaymentMethod, Subtotal = Subtotal,
            ServiceFee = ServiceFee, Discount = Discount, VoucherCode = VoucherCode, Note = Note,
            PickupIn = PickupIn, Status = Status, IsLive = false,
            Destination = Destination,
            Items = Items.Select(line => new CartItem
            {
                ItemId = line.ItemId, Name = line.Name, Stall = line.Stall, Quantity = line.Quantity,
                CanteenId = line.CanteenId,
                UnitPrice = line.UnitPrice, Note = line.Note,
                Options = (line.Options ?? []).Select(option => new MenuOption(option.Name, option.ExtraPrice)).ToArray(),
            }).ToArray(),
        };
    }
}

public sealed class OrderLineRecord
{
    public string CanteenId { get; set; } = CanteenCatalog.DefaultId;
    public string ItemId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Stall { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public string? Note { get; set; }
    public List<MenuOption> Options { get; set; } = [];
}

[JsonSourceGenerationOptions(WriteIndented = false, UnmappedMemberHandling = JsonUnmappedMemberHandling.Skip)]
[JsonSerializable(typeof(OrderHistorySnapshot))]
internal sealed partial class OrderJsonContext : JsonSerializerContext;

public interface IOrderStorage
{
    Task<OrderHistorySnapshot> LoadAsync();
    Task SaveAsync(OrderHistorySnapshot snapshot);
}

public sealed class OrderStorage : IOrderStorage
{
    private readonly string _filePath;
    private readonly SemaphoreSlim _writeGate = new(1, 1);
    public OrderStorage() : this(FileSystem.AppDataDirectory) { }
    public OrderStorage(string storageDirectory) => _filePath = Path.Combine(storageDirectory, "kantin-orders.json");

    public async Task<OrderHistorySnapshot> LoadAsync()
    {
        if (!File.Exists(_filePath)) return new OrderHistorySnapshot();
        await using var stream = File.OpenRead(_filePath);
        return await JsonSerializer.DeserializeAsync(stream, OrderJsonContext.Default.OrderHistorySnapshot).ConfigureAwait(false)
            ?? throw new InvalidDataException("Riwayat pesanan kosong atau tidak valid.");
    }

    public async Task SaveAsync(OrderHistorySnapshot snapshot)
    {
        await _writeGate.WaitAsync().ConfigureAwait(false);
        try
        {
            var path = _filePath;
            var temporary = path + ".tmp";
            await using (var stream = File.Create(temporary))
            {
                await JsonSerializer.SerializeAsync(stream, snapshot, OrderJsonContext.Default.OrderHistorySnapshot).ConfigureAwait(false);
                await stream.FlushAsync().ConfigureAwait(false);
            }
            File.Move(temporary, path, overwrite: true);
        }
        finally { _writeGate.Release(); }
    }
}
