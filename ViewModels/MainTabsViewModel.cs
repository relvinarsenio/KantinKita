using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KantinKita.Helpers;
using KantinKita.Services;

namespace KantinKita.ViewModels;

public partial class MainTabsViewModel : ObservableObject
{
    private readonly ICartService _cart;
    private readonly IAppNavigator _navigator;

    public MainTabsViewModel(ICartService cart, IAppNavigator navigator)
    {
        _cart = cart;
        _navigator = navigator;

        _cart.Changed += (_, _) => Refresh();
        Refresh();
    }

    [ObservableProperty]
    private bool isCartVisible;

    [ObservableProperty]
    private string cartCountText = string.Empty;

    [ObservableProperty]
    private string cartTotalText = string.Empty;

    [ObservableProperty]
    private string cartCanteenName = string.Empty;

    public void Refresh()
    {
        IsCartVisible = !_cart.IsEmpty;
        CartCountText = $"{_cart.TotalQuantity} item";
        CartTotalText = Money.Rupiah(_cart.Subtotal);
        CartCanteenName = _cart.Items.FirstOrDefault()?.CanteenName ?? "Keranjang";
    }

    [RelayCommand]
    private Task OpenCartAsync() => _navigator.OpenCartAsync();
}
