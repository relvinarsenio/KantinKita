using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KantinKita.Controls;
using KantinKita.Helpers;
using KantinKita.Models;
using KantinKita.Services;

namespace KantinKita.ViewModels;

public partial class OrdersViewModel : ObservableObject
{
    private readonly IOrderService _orders;
    private readonly ICartService _cart;
    private readonly IAppNavigator _navigator;
    private readonly IDialogService? _dialogs;
    private (Order Order, OrderStatus Status)[] _snapshot = [];
    private DateTime _snapshotDate;

    public OrdersViewModel(IOrderService orders, ICartService cart, IAppNavigator navigator, IDialogService? dialogs = null)
    {
        _orders = orders;
        _cart = cart;
        _navigator = navigator;
        _dialogs = dialogs;

        _orders.Changed += (_, _) => Refresh();
        Refresh();
    }

    public ObservableCollection<OrderCardViewModel> Active { get; } = [];
    public ObservableCollection<OrderCardViewModel> History { get; } = [];

    [ObservableProperty]
    private bool hasActive;

    [ObservableProperty]
    private bool hasHistory;

    [ObservableProperty]
    private bool isEmpty;

    [ObservableProperty]
    private string activeSummary = "Tidak ada pesanan berjalan";

    [ObservableProperty]
    private string historyError = string.Empty;

    [ObservableProperty]
    private bool hasHistoryError;

    public void Refresh()
    {
        HistoryError = _orders.LoadError ?? string.Empty;
        HasHistoryError = HistoryError.Length > 0;
        var today = DateTime.Today;
        var snapshot = _orders.Orders.OrderByDescending(order => order.PlacedAt)
            .Select(order => (Order: order, Status: _orders.GetStatus(order)))
            .ToArray();

        // Tab selection should not invalidate every card when only visibility changed.
        // Include the date because the relative "Hari ini/Kemarin" labels can change.
        if (_snapshotDate == today && snapshot.SequenceEqual(_snapshot))
        {
            return;
        }

        _snapshot = snapshot;
        _snapshotDate = today;
        Active.Clear();
        History.Clear();

        foreach (var (order, status) in snapshot)
        {
            var card = new OrderCardViewModel(order, status, this);
            if (card.IsActive)
            {
                Active.Add(card);
            }
            else
            {
                History.Add(card);
            }
        }

        HasActive = Active.Count > 0;
        HasHistory = History.Count > 0;
        IsEmpty = !HasActive && !HasHistory;
        ActiveSummary = HasActive
            ? $"{Active.Count} pesanan sedang diproses"
            : "Belum ada pesanan berjalan";
    }

    public async Task ReorderAsync(OrderCardViewModel card)
    {
        if (!await _cart.AddRangeWithConfirmationAsync(_dialogs, card.Order.Items)) return;
        Haptics.Tap();
        await _navigator.OpenCartAsync();
    }

    [RelayCommand]
    private Task BrowseAsync() => _navigator.ShowMenuAsync();

    [RelayCommand]
    private Task RetryHistoryAsync() => _orders.InitializeAsync();

    public Task OpenReceiptAsync(Order order) => _navigator.OpenReceiptAsync(order);
}

public sealed class OrderCardViewModel
{
    public OrderCardViewModel(Order order, OrderStatus status, OrdersViewModel owner)
    {
        Order = order;
        Status = status;
        IsActive = Status is OrderStatus.Confirmed or OrderStatus.Preparing or OrderStatus.Ready;

        IconPreview = order.Items.FirstOrDefault()?.IconGlyph ?? Icons.Food;

        var summary = order.Items.Take(3).Select(item => $"{item.Quantity}× {item.Name}").ToArray();
        ItemsSummary = string.Join(" · ", summary) + (order.Items.Count > 3 ? $" +{order.Items.Count - 3} lagi" : string.Empty);

        PlacedText = FormatPlaced(order.PlacedAt);
        PickupText = $"{FulfillmentDisplay.Location(order)} · siap {order.PickupAt:HH.mm}";
        TotalText = Money.Rupiah(order.Total);
        PaymentText = order.PaymentMethod;
        ItemCountText = $"{order.ItemCount} item";
        Steps = BuildSteps(order, Status);
        ReorderCommand = new AsyncRelayCommand(() => owner.ReorderAsync(this));
        ReceiptCommand = new AsyncRelayCommand(() => owner.OpenReceiptAsync(Order));
    }

    public Order Order { get; }
    public OrderStatus Status { get; }
    public bool IsActive { get; }
    public IReadOnlyList<OrderStepViewModel> Steps { get; }
    public IRelayCommand ReorderCommand { get; }
    public IAsyncRelayCommand ReceiptCommand { get; }

    public string Code => Order.Code;
    public string IconPreview { get; }
    public string ItemsSummary { get; }
    public string PlacedText { get; }
    public string PickupText { get; }
    public string TotalText { get; }
    public string PaymentText { get; }
    public string ItemCountText { get; }

    public bool ShowSteps => IsActive;
    public bool CanReorder => !IsActive;

    public string StatusText => Status switch
    {
        OrderStatus.Confirmed => "Dikonfirmasi",
        OrderStatus.Preparing => "Sedang disiapkan",
        OrderStatus.Ready => FulfillmentDisplay.ReadyStatus(Order),
        OrderStatus.Completed => "Selesai",
        _ => "Dibatalkan",
    };

    public string StatusHint => Status switch
    {
        OrderStatus.Confirmed => "Warung sedang mengecek pesananmu",
        OrderStatus.Preparing => "Pesananmu sedang dimasak sekarang",
        OrderStatus.Ready => $"Sudah siap · {FulfillmentDisplay.Location(Order)}",
        OrderStatus.Completed => "Terima kasih sudah jajan di kantin",
        _ => string.Empty,
    };

    public Color StatusColor => Status switch
    {
        OrderStatus.Ready => Color.FromArgb("#B87410"),
        OrderStatus.Completed => Color.FromArgb("#6C7F74"),
        OrderStatus.Cancelled => Color.FromArgb("#C9442C"),
        _ => Color.FromArgb("#0F7A54"),
    };

    public Color StatusBackground => Status switch
    {
        OrderStatus.Ready => Color.FromArgb("#29F2A93B"),
        OrderStatus.Completed => Color.FromArgb("#1F6C7F74"),
        OrderStatus.Cancelled => Color.FromArgb("#1FE5573F"),
        _ => Color.FromArgb("#1F0F6B49"),
    };

    private static string FormatPlaced(DateTimeOffset placedAt)
    {
        var date = placedAt.LocalDateTime.Date;
        var today = DateTime.Today;
        var prefix = date == today
            ? "Hari ini"
            : date == today.AddDays(-1)
                ? "Kemarin"
                : placedAt.ToString("d MMM", new System.Globalization.CultureInfo("id-ID"));

        return $"{prefix} · {placedAt:HH.mm}";
    }

    private static IReadOnlyList<OrderStepViewModel> BuildSteps(Order order, OrderStatus status)
    {
        var current = status switch
        {
            OrderStatus.Confirmed => 0,
            OrderStatus.Preparing => 1,
            OrderStatus.Ready => 2,
            _ => 3,
        };

        return
        [
            new("Dikonfirmasi", $"Diterima {order.PlacedAt:HH.mm}", current >= 0, current == 0),
            new("Disiapkan", "Sedang dimasak", current >= 1, current == 1),
            new(FulfillmentDisplay.ReadyStatus(order), $"Pukul {order.PickupAt:HH.mm}", current >= 2, current == 2),
            new("Selesai", "Terima kasih", current >= 3, current == 3),
        ];
    }
}

public sealed class OrderStepViewModel
{
    public OrderStepViewModel(string label, string hint, bool isDone, bool isCurrent)
    {
        Label = label;
        Hint = hint;
        IsDone = isDone;
        IsCurrent = isCurrent;
    }

    public string Label { get; }
    public string Hint { get; }
    public bool IsDone { get; }
    public bool IsCurrent { get; }

    public Color DotColor => IsCurrent
        ? Color.FromArgb("#F2A93B")
        : IsDone
            ? Color.FromArgb("#2AA57B")
            : Color.FromArgb("#C3CFC7");
}
