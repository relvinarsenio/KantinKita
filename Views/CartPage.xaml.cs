using KantinKita.Services;
using KantinKita.ViewModels;

namespace KantinKita.Views;

public partial class CartPage : ContentPage
{
    private readonly IAppNavigator _navigator;

    public CartPage(CartViewModel viewModel, IAppNavigator navigator)
    {
        InitializeComponent();
        _navigator = navigator;
        BindingContext = ViewModel = viewModel;
    }

    public CartViewModel ViewModel { get; }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        ViewModel.Activate();
    }

    protected override void OnDisappearing()
    {
        ViewModel.Deactivate();
        base.OnDisappearing();
    }

    private async void OnBackTapped(object? sender, TappedEventArgs e)
    {
        await _navigator.CloseAsync();
    }
}
