using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KantinKita.Controls;
using KantinKita.Helpers;
using KantinKita.Models;
using KantinKita.Services;

namespace KantinKita.ViewModels;

public partial class CheckoutViewModel : ObservableObject
{
    private readonly ICartService _cart;
    private readonly IProfileService _profile;
    private readonly IVoucherService _vouchers;
    private readonly IOrderService _orders;
    private readonly IAppNavigator _navigator;
    private readonly IDialogService _dialogs;
    private readonly IFulfillmentService _fulfillment;
    private Canteen? _canteen;
    private CanteenTable? _selectedTable;
    private FulfillmentMode _mode;
    private bool _isDestinationPickerOpen;

    private decimal _subtotal;
    private decimal _discount;
    private Voucher? _selectedVoucher;
    private VoucherOptionViewModel? _selectedVoucherOption;
    private PickupSlotViewModel _pickup;
    private PaymentMethodViewModel _payment;

    public CheckoutViewModel(
        ICartService cart,
        IProfileService profile,
        IVoucherService vouchers,
        IOrderService orders,
        IAppNavigator navigator,
        IDialogService dialogs,
        IFulfillmentService fulfillment)
    {
        _cart = cart;
        _profile = profile;
        _vouchers = vouchers;
        _orders = orders;
        _navigator = navigator;
        _dialogs = dialogs;
        _fulfillment = fulfillment;

        FulfillmentModes =
        [
            new(FulfillmentMode.Pickup, "Ambil sendiri", SelectMode),
            new(FulfillmentMode.DineIn, "Di meja", SelectMode),
            new(FulfillmentMode.Delivery, "Antar", SelectMode),
        ];
        FulfillmentModes[0].IsSelected = true;

        var now = DateTime.Now;
        PickupSlots =
        [
            new("Secepatnya", "±10 menit", TimeSpan.FromMinutes(10), SelectPickup),
            new(now.AddMinutes(30).ToString("HH.mm"), "±30 menit", TimeSpan.FromMinutes(30), SelectPickup),
            new(now.AddMinutes(60).ToString("HH.mm"), "±1 jam", TimeSpan.FromMinutes(60), SelectPickup),
        ];
        _pickup = PickupSlots[0];
        _pickup.IsSelected = true;

        Payments =
        [
            new("saldo", "Saldo Kantin", $"Tersedia {Money.Rupiah(profile.Balance)}", Icons.Wallet, SelectPayment),
            new("qris", "QRIS", "Scan QR di kasir", Icons.Qr, SelectPayment),
            new("cash", "Tunai", "Bayar ketika pesanan diterima", Icons.Cash, SelectPayment),
        ];
        _payment = Payments[0];
        _payment.IsSelected = true;

        Load();
    }

    public ObservableCollection<CartLineViewModel> Lines { get; } = [];
    public IReadOnlyList<PickupSlotViewModel> PickupSlots { get; }
    public IReadOnlyList<PaymentMethodViewModel> Payments { get; }
    public ObservableCollection<VoucherOptionViewModel> Vouchers { get; } = [];
    public IReadOnlyList<FulfillmentModeViewModel> FulfillmentModes { get; }
    public bool IsDineIn => _mode == FulfillmentMode.DineIn;
    public bool IsDelivery => _mode == FulfillmentMode.Delivery;
    public string CanteenName => _canteen?.Name ?? "Pilih kantin";
    public string CanteenLocation => _canteen?.Location ?? "Kantin belum tersedia";
    public string AvailabilityNote => _fulfillment.AvailabilityNote;
    public bool HasDestinationError => !string.IsNullOrEmpty(DestinationError);

    [ObservableProperty] private string tableText = "Pilih meja kosong";
    [ObservableProperty] private string building = string.Empty;
    [ObservableProperty] private string room = string.Empty;
    [ObservableProperty] private string destinationError = string.Empty;
    public string BuildingText => string.IsNullOrWhiteSpace(Building) ? "Pilih gedung tujuan" : Building;
    partial void OnDestinationErrorChanged(string value) => OnPropertyChanged(nameof(HasDestinationError));
    partial void OnBuildingChanged(string value) { DestinationError = string.Empty; OnPropertyChanged(nameof(BuildingText)); }
    partial void OnRoomChanged(string value) => DestinationError = string.Empty;

    [ObservableProperty]
    private bool isVoucherPickerOpen;

    [ObservableProperty]
    private string note = string.Empty;

    [ObservableProperty]
    private string subtotalText = "Rp0";

    [ObservableProperty]
    private string serviceFeeText = "Rp0";

    [ObservableProperty]
    private string discountText = "Rp0";

    [ObservableProperty]
    private string totalText = "Rp0";

    [ObservableProperty]
    private string buttonText = "Buat pesanan";

    [ObservableProperty]
    private string pickupSummary = "Diambil secepatnya (±10 menit)";

    [ObservableProperty]
    private string voucherSummary = "Pilih voucher terbaikmu";

    [ObservableProperty]
    private bool hasVoucher;

    [ObservableProperty]
    private bool hasDiscount;

    [ObservableProperty]
    private string itemCountText = "0 item";

    [ObservableProperty]
    private bool isBusy;

    public void Load()
    {
        _canteen = CanteenCatalog.Find(_cart.Items.FirstOrDefault()?.CanteenId);
        OnPropertyChanged(nameof(CanteenName));
        OnPropertyChanged(nameof(CanteenLocation));
        Lines.Clear();
        foreach (var line in _cart.Items)
        {
            Lines.Add(new CartLineViewModel(line, null));
        }

        _subtotal = _cart.Subtotal;
        _discount = 0m;
        _selectedVoucher = null;
        _selectedVoucherOption = null;
        HasVoucher = false;
        HasDiscount = false;
        VoucherSummary = "Pilih voucher terbaikmu";
        ItemCountText = $"{_cart.TotalQuantity} item";

        Vouchers.Clear();
        foreach (var suggestion in _vouchers.Suggest(_subtotal))
        {
            Vouchers.Add(new VoucherOptionViewModel(suggestion, _vouchers, SelectVoucher));
        }

        UpdateTotals();
    }

    partial void OnIsVoucherPickerOpenChanged(bool value)
    {
        if (value)
        {
            // Refresh eligibility against the current subtotal every time it opens.
            foreach (var voucher in Vouchers)
            {
                voucher.Refresh(_subtotal);
            }
        }
    }

    [RelayCommand]
    private void ToggleVoucherPicker() => IsVoucherPickerOpen = !IsVoucherPickerOpen;

    [RelayCommand]
    private void ClearVoucher()
    {
        if (_selectedVoucherOption is null)
        {
            return;
        }

        _selectedVoucherOption.IsSelected = false;
        _selectedVoucherOption = null;
        _selectedVoucher = null;
        _discount = 0m;
        HasVoucher = false;
        HasDiscount = false;
        VoucherSummary = "Pilih voucher terbaikmu";
        UpdateTotals();
    }

    [RelayCommand]
    private async Task PlaceOrderAsync()
    {
        if (IsBusy)
        {
            return;
        }

        if (_cart.IsEmpty)
        {
            await _dialogs.AlertAsync("Keranjang kosong", "Tambahkan menu favoritmu dulu ya.");
            return;
        }

        IsBusy = true;
        try
        {
            var destination = new OrderDestination
            {
                CanteenId = _canteen?.Id ?? string.Empty, CanteenName = CanteenName, Mode = _mode,
                TableId = IsDineIn ? _selectedTable?.Id : null,
                TableLabel = IsDineIn ? _selectedTable?.Label : null,
                Building = IsDelivery ? Building.Trim() : null,
                Room = IsDelivery ? Room.Trim() : null,
            };
            if (_cart.Items.Any(line => line.CanteenId != destination.CanteenId))
            {
                DestinationError = "Kantin di keranjang berubah. Buka ulang pembayaran.";
                return;
            }
            try { DestinationError = await _fulfillment.ValidateAsync(destination) ?? string.Empty; }
            catch (Exception error) when (error is IOException or HttpRequestException)
            {
                DestinationError = "Status tempat belum bisa dimuat. Coba lagi sebentar.";
                return;
            }
            if (HasDestinationError) return;

            var total = _subtotal + Money.ServiceFee - _discount;
            if (_payment.Id == "saldo" && _profile.Balance < total)
            {
                await _dialogs.AlertAsync("Saldo tidak cukup",
                    $"Saldo kantinmu {Money.Rupiah(_profile.Balance)}, total pesanan {Money.Rupiah(total)}. Pilih metode lain atau top up dulu.");
                return;
            }
            await _orders.InitializeAsync();
            if (!_orders.IsInitialized)
            {
                await _dialogs.AlertAsync("Riwayat belum siap", _orders.LoadError ?? "Coba lagi sebentar.");
                return;
            }

            var debited = _payment.Id == "saldo";
            if (debited && !_profile.TryDebit(total))
            {
                await _dialogs.AlertAsync("Saldo tidak cukup", "Saldo berubah. Periksa saldo atau pilih metode pembayaran lain.");
                return;
            }

            Order order;
            try
            {
                order = await _orders.PlaceAsync(_cart.Items, _subtotal, _discount, _payment.Name,
                    _selectedVoucher?.Code, string.IsNullOrWhiteSpace(Note) ? null : Note.Trim(), _pickup.Offset, destination);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException)
            {
                if (debited) _profile.TopUp(total);
                await _dialogs.AlertAsync("Pesanan belum tersimpan", "Pesanan belum dibuat dan keranjang tetap tersedia. Periksa ruang penyimpanan lalu coba lagi.");
                return;
            }

            _profile.RecordOrder(total);
            _cart.Clear();
            Haptics.Tap();
            await _navigator.OpenOrderSuccessAsync(order);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void SelectMode(FulfillmentModeViewModel option)
    {
        _mode = option.Mode;
        foreach (var mode in FulfillmentModes) mode.IsSelected = ReferenceEquals(mode, option);
        DestinationError = string.Empty;
        OnPropertyChanged(nameof(IsDineIn));
        OnPropertyChanged(nameof(IsDelivery));
        UpdatePickupSummary();
        Haptics.Tap();
    }

    [RelayCommand]
    private async Task ChooseTableAsync()
    {
        if (IsBusy || _isDestinationPickerOpen || _canteen is null) return;
        _isDestinationPickerOpen = true;
        try
        {
            var tables = await _fulfillment.GetTablesAsync(_canteen.Id);
            var available = tables.Where(table => table.IsAvailable).ToArray();
            if (tables.Count == 0)
            {
                await _dialogs.AlertAsync("Meja belum tersedia", "Daftar meja belum dimuat. Pilih ambil sendiri atau antar ke ruangan.");
                return;
            }
            var selected = await _dialogs.ChooseVisualAsync(new VisualChoiceRequest(
                "Duduk di mana?",
                tables.Select(table => new DialogChoice(table.Id, table.Label, $"{table.Seats} kursi",
                    IsEnabled: table.IsAvailable, Badge: table.Label.Split('-').Last(), Capacity: table.Seats)).ToArray(),
                VisualChoiceLayout.Tables, _selectedTable?.Id, _canteen.Name, AvailabilityNote, "Gunakan meja"));
            if (available.FirstOrDefault(table => table.Id == selected) is { } table)
            {
                _selectedTable = table;
                TableText = $"{table.Label} · {table.Seats} kursi";
                DestinationError = string.Empty;
            }
        }
        catch (Exception error) when (error is IOException or HttpRequestException)
        {
            DestinationError = "Daftar meja belum bisa dimuat. Coba lagi sebentar.";
        }
        finally { _isDestinationPickerOpen = false; }
    }

    [RelayCommand]
    private async Task ChooseBuildingAsync()
    {
        if (IsBusy || _isDestinationPickerOpen) return;
        _isDestinationPickerOpen = true;
        try
        {
            var selected = await _dialogs.ChooseVisualAsync(new VisualChoiceRequest(
                "Antar ke mana?",
                _fulfillment.Buildings.Select(name => new DialogChoice(name, name,
                    name == "Perpustakaan" ? "Area perpustakaan" : "Area gedung kampus", Icons.Pin)).ToArray(),
                VisualChoiceLayout.Buildings, Building,
                "Pilih gedung dulu. Nomor atau nama ruangan diisi setelah ini.", ConfirmText: "Gunakan gedung"));
            if (selected is not null) Building = selected;
        }
        finally { _isDestinationPickerOpen = false; }
    }

    private void UpdatePickupSummary() => PickupSummary = _pickup.Offset == TimeSpan.FromMinutes(10)
        ? "Estimasi siap ±10 menit" : $"Estimasi siap pukul {_pickup.Title}";

    private void SelectPickup(PickupSlotViewModel slot)
    {
        foreach (var option in PickupSlots)
        {
            option.IsSelected = ReferenceEquals(option, slot);
        }

        _pickup = slot;
        UpdatePickupSummary();
    }

    private void SelectPayment(PaymentMethodViewModel payment)
    {
        foreach (var option in Payments)
        {
            option.IsSelected = ReferenceEquals(option, payment);
        }

        _payment = payment;
    }

    private void SelectVoucher(VoucherOptionViewModel option)
    {
        if (!option.IsEligible)
        {
            return;
        }

        if (ReferenceEquals(_selectedVoucherOption, option))
        {
            option.IsSelected = false;
            _selectedVoucherOption = null;
            _selectedVoucher = null;
            _discount = 0m;
            HasVoucher = false;
            HasDiscount = false;
            VoucherSummary = "Pilih voucher terbaikmu";
            UpdateTotals();
            return;
        }

        foreach (var voucher in Vouchers)
        {
            voucher.IsSelected = ReferenceEquals(voucher, option);
        }

        _selectedVoucherOption = option;
        _selectedVoucher = option.Voucher;
        _discount = _vouchers.Evaluate(option.Voucher, _subtotal).Discount;
        HasVoucher = true;
        HasDiscount = _discount > 0;
        VoucherSummary = $"{option.Code} · hemat {Money.Rupiah(_discount)}";
        IsVoucherPickerOpen = false;
        UpdateTotals();
    }

    private void UpdateTotals()
    {
        var fee = _subtotal > 0 ? Money.ServiceFee : 0m;
        var total = _subtotal + fee - _discount;

        SubtotalText = Money.Rupiah(_subtotal);
        ServiceFeeText = Money.Rupiah(fee);
        DiscountText = "-" + Money.Rupiah(_discount);
        TotalText = Money.Rupiah(total);
        ButtonText = $"Buat pesanan · {Money.Rupiah(total)}";
    }
}

public sealed partial class FulfillmentModeViewModel : ObservableObject
{
    public FulfillmentModeViewModel(FulfillmentMode mode, string title, Action<FulfillmentModeViewModel> select)
    {
        Mode = mode;
        Title = title;
        SelectCommand = new RelayCommand(() => select(this));
    }
    public FulfillmentMode Mode { get; }
    public string Title { get; }
    public IRelayCommand SelectCommand { get; }
    [ObservableProperty] private bool isSelected;
}

public sealed partial class PickupSlotViewModel : ObservableObject
{
    private readonly Action<PickupSlotViewModel> _onSelect;

    public PickupSlotViewModel(string title, string subtitle, TimeSpan offset, Action<PickupSlotViewModel> onSelect)
    {
        Title = title;
        Subtitle = subtitle;
        Offset = offset;
        _onSelect = onSelect;
        SelectCommand = new RelayCommand(() => _onSelect(this));
    }

    public string Title { get; }
    public string Subtitle { get; }
    public TimeSpan Offset { get; }
    public IRelayCommand SelectCommand { get; }

    [ObservableProperty]
    private bool isSelected;
}

public sealed partial class PaymentMethodViewModel : ObservableObject
{
    private readonly Action<PaymentMethodViewModel> _onSelect;

    public PaymentMethodViewModel(string id, string name, string subtitle, string glyph, Action<PaymentMethodViewModel> onSelect)
    {
        Id = id;
        Name = name;
        Subtitle = subtitle;
        Glyph = glyph;
        _onSelect = onSelect;
        SelectCommand = new RelayCommand(() => _onSelect(this));
    }

    public string Id { get; }
    public string Name { get; }
    public string Subtitle { get; }
    public string Glyph { get; }
    public IRelayCommand SelectCommand { get; }

    [ObservableProperty]
    private bool isSelected;
}

public sealed partial class VoucherOptionViewModel : ObservableObject
{
    private readonly Action<VoucherOptionViewModel> _onSelect;
    private readonly IVoucherService _vouchers;

    public VoucherOptionViewModel(VoucherSuggestion suggestion, IVoucherService vouchers, Action<VoucherOptionViewModel> onSelect)
    {
        Voucher = suggestion.Voucher;
        Code = suggestion.Voucher.Code;
        Title = suggestion.Voucher.Title;
        Description = suggestion.Voucher.Description;
        IsEligible = suggestion.IsEligible;
        StatusText = suggestion.Status;
        _onSelect = onSelect;
        _vouchers = vouchers;
        SelectCommand = new RelayCommand(() => _onSelect(this));
    }

    public Voucher Voucher { get; }
    public string Code { get; }
    public string Title { get; }
    public string Description { get; }
    public bool IsEligible { get; }
    public IRelayCommand SelectCommand { get; }

    [ObservableProperty]
    private string statusText = string.Empty;

    [ObservableProperty]
    private bool isSelected;

    public void Refresh(decimal subtotal)
    {
        var evaluation = _vouchers.Evaluate(Voucher, subtotal);
        StatusText = evaluation.IsValid ? $"Hemat {Money.Rupiah(evaluation.Discount)}" : evaluation.Error!;
    }
}

