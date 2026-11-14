using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KantinKita.Controls;
using KantinKita.Helpers;
using KantinKita.Models;
using KantinKita.Services;
using System.Collections.ObjectModel;

namespace KantinKita.ViewModels;

public partial class ItemDetailViewModel : ObservableObject
{
    private readonly ICartService _cart;
    private readonly IFavoriteService _favorites;
    private readonly IAppNavigator _navigator;
    private readonly IDialogService? _dialogs;

    private CanteenMenuItem? _item;

    public ItemDetailViewModel(ICartService cart, IFavoriteService favorites, IAppNavigator navigator, IDialogService? dialogs = null)
    {
        _cart = cart;
        _favorites = favorites;
        _navigator = navigator;
        _dialogs = dialogs;
    }

    public ObservableCollection<OptionGroupViewModel> OptionGroups { get; } = [];

    [ObservableProperty]
    private string name = string.Empty;

    [ObservableProperty]
    private string description = string.Empty;

    [ObservableProperty]
    private string stall = string.Empty;

    [ObservableProperty]
    private string iconGlyph = Icons.Food;

    [ObservableProperty]
    private Brush heroBrush = BrushFactory.Gradient("#E9F5EE", "#D5EBDD");

    [ObservableProperty]
    private string ratingText = string.Empty;

    [ObservableProperty]
    private string reviewText = string.Empty;

    [ObservableProperty]
    private string prepText = string.Empty;

    [ObservableProperty]
    private string soldText = string.Empty;

    [ObservableProperty]
    private string stockText = string.Empty;

    [ObservableProperty]
    private string descriptionMeta = string.Empty;

    [ObservableProperty]
    private string unitPriceText = string.Empty;

    [ObservableProperty]
    private string totalText = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AddButtonText))]
    private int quantity = 1;

    [ObservableProperty]
    private string note = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotFavorite))]
    private bool isFavorite;

    [ObservableProperty]
    private bool isSpicy;

    [ObservableProperty]
    private bool isBestSeller;

    [ObservableProperty]
    private bool hasOptions;

    public bool IsNotFavorite => !IsFavorite;

    public string AddButtonText => $"Tambah · {Money.Rupiah(UnitPrice * Quantity)}";

    // BindingContext is assigned before navigation calls Prepare, so bindings may
    // read AddButtonText while no item has been selected yet.
    private decimal UnitPrice => (_item?.Price ?? 0m) + OptionGroups.Sum(group => group.SelectedOption.ExtraPrice);

    public void Prepare(CanteenMenuItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        _item = item;

        Name = item.Name;
        Description = item.Description;
        Stall = $"{item.CanteenName} · {item.Stall}";
        IconGlyph = item.IconGlyph;
        HeroBrush = BrushFactory.Gradient(item.TileStart, item.TileEnd);
        RatingText = Money.Rating(item.Rating);
        ReviewText = $"({item.ReviewCount})";
        PrepText = $"±{item.PrepMinutes} mnt";
        SoldText = $"{Money.Number(item.SoldCount)} terjual";
        StockText = $"Tersisa {item.Stock} porsi hari ini";
        IsSpicy = item.IsSpicy;
        IsBestSeller = item.IsBestSeller;
        IsFavorite = _favorites.IsFavorite(item.Id);
        Quantity = 1;
        Note = string.Empty;

        OptionGroups.Clear();
        foreach (var group in item.OptionGroups)
        {
            OptionGroups.Add(new OptionGroupViewModel(group, OnOptionChanged));
        }

        HasOptions = OptionGroups.Count > 0;
        RefreshTotals();
    }

    partial void OnQuantityChanged(int value)
    {
        if (value < 1)
        {
            Quantity = 1;
            return;
        }

        RefreshTotals();
    }

    [RelayCommand]
    private void Increase() => Quantity++;

    [RelayCommand]
    private void Reduce()
    {
        if (Quantity > 1)
        {
            Quantity--;
        }
    }

    [RelayCommand]
    private void ToggleFavorite()
    {
        if (_item is not { } item)
        {
            return;
        }

        _favorites.Toggle(item.Id);
        IsFavorite = _favorites.IsFavorite(item.Id);
        Haptics.Tap();
    }

    [RelayCommand]
    private async Task AddToCartAsync()
    {
        if (_item is not { } item)
        {
            return;
        }

        var options = OptionGroups.Select(group => group.SelectedOption).ToArray();
        if (!await _cart.AddWithConfirmationAsync(_dialogs, item, options, Quantity,
            string.IsNullOrWhiteSpace(Note) ? null : Note.Trim())) return;
        Haptics.Tap();

        // Let the tap gesture finish before the page pops; otherwise the finger-up can
        // land on the navigation bar underneath.
        await Task.Delay(120);
        await _navigator.CloseAsync();
    }

    private void OnOptionChanged() => RefreshTotals();

    private void RefreshTotals()
    {
        if (_item is null)
        {
            return;
        }

        var unit = UnitPrice;
        UnitPriceText = Money.Rupiah(unit);
        TotalText = Money.Rupiah(unit * Quantity);
        OnPropertyChanged(nameof(AddButtonText));
    }
}


