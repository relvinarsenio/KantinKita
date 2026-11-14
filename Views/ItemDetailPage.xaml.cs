using KantinKita.Models;
using KantinKita.Services;
using KantinKita.ViewModels;

namespace KantinKita.Views;

public partial class ItemDetailPage : ContentPage
{
    private readonly IAppNavigator _navigator;

    public ItemDetailPage(ItemDetailViewModel viewModel, IAppNavigator navigator)
    {
        InitializeComponent();
        _navigator = navigator;
        BindingContext = ViewModel = viewModel;
    }

    public ItemDetailViewModel ViewModel { get; }

    public void Prepare(CanteenMenuItem item) => ViewModel.Prepare(item);

    private async void OnBackTapped(object? sender, TappedEventArgs e)
    {
        await _navigator.CloseAsync();
    }
}

