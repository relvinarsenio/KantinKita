using KantinKita.Models;

namespace KantinKita.Services;

public interface IOrderService
{
    IReadOnlyList<Order> Orders { get; }
    Order? ActiveOrder { get; }
    bool IsInitialized { get; }
    string? LoadError { get; }
    event EventHandler? Changed;
    Task InitializeAsync();
    Order Place(IReadOnlyList<CartItem> items, decimal subtotal, decimal discount, string paymentMethod,
        string? voucherCode, string? note, TimeSpan pickupIn, OrderDestination? destination = null);
    Task<Order> PlaceAsync(IReadOnlyList<CartItem> items, decimal subtotal, decimal discount, string paymentMethod,
        string? voucherCode, string? note, TimeSpan pickupIn, OrderDestination? destination = null);
    OrderStatus GetStatus(Order order);
    bool IsActive(Order order);
}

/// <summary>Durable orders only. Opening the app never creates example purchases.</summary>
public sealed class OrderService : IOrderService
{
    private readonly IOrderStorage _storage;
    private readonly SynchronizationContext? _uiContext = SynchronizationContext.Current;
    private readonly List<Order> _orders = [];
    private readonly object _sync = new();
    private readonly SemaphoreSlim _placeGate = new(1, 1);
    private Task? _initializing;
    private int _lastSequence;

    public OrderService(IOrderStorage storage) => _storage = storage;
    public bool IsInitialized { get; private set; }
    public string? LoadError { get; private set; }
    public IReadOnlyList<Order> Orders { get { lock (_sync) return _orders.ToArray(); } }
    public Order? ActiveOrder => Orders.Where(IsActive).OrderByDescending(order => order.PlacedAt).FirstOrDefault();
    public event EventHandler? Changed;

    public Task InitializeAsync()
    {
        lock (_sync)
        {
            if (IsInitialized) return Task.CompletedTask;
            return _initializing is { IsCompleted: false } ? _initializing : _initializing = LoadCoreAsync();
        }
    }

    private async Task LoadCoreAsync()
    {
        try
        {
            var snapshot = await _storage.LoadAsync().ConfigureAwait(false);
            if (snapshot.Orders is null || snapshot.Orders.Any(record => record is null))
                throw new InvalidDataException("Saved order list is missing or invalid.");
            var orders = snapshot.Orders.Select(record => record.ToOrder()).ToArray();
            if (orders.Select(order => order.Code).Distinct(StringComparer.Ordinal).Count() != orders.Length)
                throw new InvalidDataException("Duplicate order codes in saved history.");
            lock (_sync)
            {
                _orders.Clear();
                _orders.AddRange(orders.OrderByDescending(order => order.PlacedAt));
                _lastSequence = Math.Max(Math.Max(0, snapshot.LastSequence),
                    orders.Select(order => int.TryParse(order.Code.Split('-').Last(), out var sequence) ? sequence : 0).DefaultIfEmpty().Max());
                IsInitialized = true;
                LoadError = null;
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or System.Text.Json.JsonException or InvalidDataException)
        {
            // A failed read must not replace the original history with an empty file.
            LoadError = "Riwayat tersimpan belum bisa dibuka. Coba muat ulang sebelum membuat pesanan.";
        }
        PublishChanged();
    }

    // Kept for existing callers; checkout uses the asynchronous durable path below.
    public Order Place(IReadOnlyList<CartItem> items, decimal subtotal, decimal discount, string paymentMethod,
        string? voucherCode, string? note, TimeSpan pickupIn, OrderDestination? destination = null) =>
        PlaceAsync(items, subtotal, discount, paymentMethod, voucherCode, note, pickupIn, destination).GetAwaiter().GetResult();

    public async Task<Order> PlaceAsync(IReadOnlyList<CartItem> items, decimal subtotal, decimal discount, string paymentMethod,
        string? voucherCode, string? note, TimeSpan pickupIn, OrderDestination? destination = null)
    {
        await InitializeAsync().ConfigureAwait(false);
        if (!IsInitialized) throw new IOException(LoadError);
        var lines = items.Select(CloneLine).ToArray();
        if (lines.Length == 0 || lines.Any(line => line.Quantity <= 0 || line.UnitPrice < 0 || string.IsNullOrWhiteSpace(line.Name)))
            throw new ArgumentException("Pesanan harus berisi item dan jumlah yang valid.", nameof(items));
        if (subtotal != lines.Sum(line => line.LineTotal))
            throw new ArgumentException("Subtotal tidak sesuai dengan rincian item.", nameof(subtotal));
        if (discount < 0 || discount > subtotal || string.IsNullOrWhiteSpace(paymentMethod) || pickupIn < TimeSpan.Zero)
            throw new ArgumentException("Rincian pembayaran atau pengambilan tidak valid.");
        var canteenIds = lines.Select(line => line.CanteenId).Distinct(StringComparer.Ordinal).ToArray();
        if (canteenIds.Length != 1) throw new ArgumentException("Satu pesanan hanya dapat berasal dari satu kantin.");
        var canteen = CanteenCatalog.Find(canteenIds[0])
            ?? throw new ArgumentException("Kantin di keranjang tidak ditemukan.", nameof(items));
        destination ??= new OrderDestination
        {
            CanteenId = canteen.Id, CanteenName = canteen.Name, Mode = FulfillmentMode.Pickup,
        };
        var destinationError = destination.ValidationError();
        if (destinationError is not null || destination.CanteenId != canteen.Id)
            throw new ArgumentException(destinationError ?? "Tujuan pesanan tidak sesuai dengan kantin di keranjang.", nameof(destination));
        destination = destination with { CanteenName = canteen.Name };

        await _placeGate.WaitAsync().ConfigureAwait(false);
        try
        {
            // Serialize the local table claim with the durable order write. The
            // availability feed is checked in checkout, but it may change while
            // two callers wait for this gate.
            if (destination.Mode == FulfillmentMode.DineIn && Orders.Any(existing => IsActive(existing)
                && existing.Destination?.Mode == FulfillmentMode.DineIn
                && existing.Destination.CanteenId == destination.CanteenId
                && existing.Destination.TableId == destination.TableId))
                throw new ArgumentException("Meja sudah dipakai pesanan aktif. Pilih meja lain.", nameof(destination));
            var sequence = checked(_lastSequence + 1);
            var now = DateTimeOffset.Now;
            var order = new Order
            {
                Code = $"KT-{now:yyyyMMdd}-{sequence:D6}", PlacedAt = now, Items = lines,
                Subtotal = subtotal, ServiceFee = Helpers.Money.ServiceFee, Discount = discount,
                PaymentMethod = paymentMethod, VoucherCode = voucherCode, Note = note, PickupIn = pickupIn,
                Status = OrderStatus.Confirmed, IsLive = false,
                Destination = destination,
            };
            var snapshot = new OrderHistorySnapshot
            {
                LastSequence = sequence,
                Orders = new[] { order }.Concat(Orders).Select(OrderRecord.FromOrder).ToList(),
            };
            // Save before publishing or clearing the cart. IO failure leaves the purchase uncommitted.
            await _storage.SaveAsync(snapshot).ConfigureAwait(false);
            lock (_sync)
            {
                _orders.Insert(0, order);
                _lastSequence = sequence;
            }
            PublishChanged();
            return order;
        }
        finally { _placeGate.Release(); }
    }

    public OrderStatus GetStatus(Order order) => order.Status;
    public bool IsActive(Order order) => GetStatus(order) is OrderStatus.Confirmed or OrderStatus.Preparing or OrderStatus.Ready;

    private static CartItem CloneLine(CartItem line) => new()
    {
        ItemId = line.ItemId, Name = line.Name, Stall = line.Stall, UnitPrice = line.UnitPrice,
        CanteenId = line.CanteenId,
        Quantity = line.Quantity, Note = line.Note, Options = line.Options.Select(option => new MenuOption(option.Name, option.ExtraPrice)).ToArray(),
    };

    private void PublishChanged()
    {
        if (_uiContext is null || ReferenceEquals(SynchronizationContext.Current, _uiContext)) Changed?.Invoke(this, EventArgs.Empty);
        else _uiContext.Post(_ => Changed?.Invoke(this, EventArgs.Empty), null);
    }
}
