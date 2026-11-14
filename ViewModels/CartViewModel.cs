using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KantinKita.Helpers;
using KantinKita.Models;
using KantinKita.Services;

namespace KantinKita.ViewModels;

public partial class CartViewModel : ObservableObject
{
    private readonly ICartService _cart;
    private readonly IAppNavigator _navigator;
    private readonly IDialogService _dialogs;
    private bool _isActive;

    public CartViewModel(ICartService cart, IAppNavigator navigator, IDialogService dialogs)
    {
        _cart = cart;
        _navigator = navigator;
        _dialogs = dialogs;
    }

    public ObservableCollection<CartLineViewModel> Lines { get; } = [];

    [ObservableProperty]
    private bool isEmpty;

    [ObservableProperty]
    private bool hasItems;

    [ObservableProperty]
    private string subtotalText = "Rp0";

    [ObservableProperty]
    private string serviceFeeText = "Rp0";

    [ObservableProperty]
    private string totalText = "Rp0";

    [ObservableProperty]
    private string itemCountText = "0 item";

    [ObservableProperty]
    private string checkoutButtonText = "Lanjut ke pembayaran";

    [ObservableProperty]
    private string canteenName = string.Empty;

    public void Activate()
    {
        if (_isActive)
        {
            return;
        }

        _isActive = true;
        _cart.Changed += OnCartChanged;
        Refresh();
    }

    public void Deactivate()
    {
        if (!_isActive)
        {
            return;
        }

        _isActive = false;
        _cart.Changed -= OnCartChanged;
    }

    private void OnCartChanged(object? sender, EventArgs e) => Refresh();

    public void Refresh()
    {
        var items = _cart.Items;
        CanteenName = items.FirstOrDefault()?.CanteenName ?? "Kantin";
        var sameLines = Lines.Count == items.Count &&
            Lines.Select(line => line.Item).SequenceEqual(items);
        if (sameLines)
        {
            // Keep the controls under the user's finger alive during repeated +/- taps.
            foreach (var line in Lines) line.RefreshQuantity();
        }
        else
        {
            Lines.Clear();
            foreach (var line in items) Lines.Add(new CartLineViewModel(line, this));
        }

        var subtotal = _cart.Subtotal;
        var fee = subtotal > 0 ? Money.ServiceFee : 0m;
        SubtotalText = Money.Rupiah(subtotal);
        ServiceFeeText = Money.Rupiah(fee);
        TotalText = Money.Rupiah(subtotal + fee);
        ItemCountText = $"{_cart.TotalQuantity} item";
        IsEmpty = _cart.IsEmpty;
        HasItems = !_cart.IsEmpty;
        CheckoutButtonText = $"Lanjut · {TotalText}";
    }

    public void Increase(CartLineViewModel line) => _cart.Increase(line.Item);

    public void Decrease(CartLineViewModel line) => _cart.Decrease(line.Item);

    public void Remove(CartLineViewModel line) => _cart.Remove(line.Item);

    [RelayCommand]
    private async Task CheckoutAsync()
    {
        if (_cart.IsEmpty)
        {
            return;
        }

        await _navigator.OpenCheckoutAsync();
    }

    [RelayCommand]
    private async Task ClearAsync()
    {
        if (_cart.IsEmpty)
        {
            return;
        }

        if (await _dialogs.ConfirmAsync("Kosongkan keranjang?", "Semua pesanan yang belum dibayar akan dihapus.", "Kosongkan", "Batal"))
        {
            _cart.Clear();
            Haptics.Tap();
        }
    }

    [RelayCommand]
    private Task BrowseAsync() => _navigator.ShowMenuAsync();
}

public sealed class CartLineViewModel : ObservableObject
{
    private readonly CartViewModel? _owner;

    public CartLineViewModel(CartItem item, CartViewModel? owner)
    {
        Item = item;
        _owner = owner;
        IncreaseCommand = new RelayCommand(() => owner?.Increase(this));
        DecreaseCommand = new RelayCommand(() => owner?.Decrease(this));
        RemoveCommand = new RelayCommand(() => owner?.Remove(this));
    }

    public CartItem Item { get; }

    public IRelayCommand IncreaseCommand { get; }
    public IRelayCommand DecreaseCommand { get; }
    public IRelayCommand RemoveCommand { get; }

    public string Name => Item.Name;
    public string IconGlyph => Item.IconGlyph;
    public string OptionsText => string.IsNullOrWhiteSpace(Item.OptionsSummary) ? Item.Stall : Item.OptionsSummary;
    public string NoteText => Item.Note ?? string.Empty;
    public bool HasNote => !string.IsNullOrWhiteSpace(Item.Note);
    public string QuantityText => Item.Quantity.ToString(CultureInfo.InvariantCulture);
    public string UnitPriceText => Money.Rupiah(Item.UnitPrice);
    public string LineTotalText => Money.Rupiah(Item.LineTotal);

    public void RefreshQuantity()
    {
        OnPropertyChanged(nameof(QuantityText));
        OnPropertyChanged(nameof(LineTotalText));
    }
}



