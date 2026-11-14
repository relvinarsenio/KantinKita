using KantinKita.Services;
using KantinKita.ViewModels;

namespace KantinKita.Views;

public partial class CheckoutPage : ContentPage
{
    private readonly IAppNavigator _navigator;

    public CheckoutPage(CheckoutViewModel viewModel, IAppNavigator navigator)
    {
        InitializeComponent();
        _navigator = navigator;
        BindingContext = viewModel;
    }

    private async void OnBackTapped(object? sender, TappedEventArgs e)
    {
        await _navigator.CloseAsync();
    }
}
