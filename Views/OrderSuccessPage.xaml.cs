using KantinKita.Models;
using KantinKita.ViewModels;

namespace KantinKita.Views;

public partial class OrderSuccessPage : ContentPage
{
    private bool _hasAnimated;

    public OrderSuccessPage(OrderSuccessViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = ViewModel = viewModel;
    }

    public OrderSuccessViewModel ViewModel { get; }

    public void Prepare(Order order) => ViewModel.Prepare(order);

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_hasAnimated)
        {
            return;
        }

        _hasAnimated = true;
        ReceiptContent.Opacity = 0;
        ReceiptContent.TranslationY = 14;
        SuccessBadge.Scale = 0.88;

        // This page is pushed without a native transition after checkout.
        // Animate the receipt in place without delaying navigation or input.
        await Task.WhenAll(
            ReceiptContent.FadeToAsync(1, 180, Easing.CubicOut),
            ReceiptContent.TranslateToAsync(0, 0, 220, Easing.CubicOut),
            SuccessBadge.ScaleToAsync(1, 220, Easing.CubicOut));
    }

    protected override void OnDisappearing()
    {
        ReceiptContent.CancelAnimations();
        SuccessBadge.CancelAnimations();
        ReceiptContent.Opacity = 1;
        ReceiptContent.TranslationY = 0;
        SuccessBadge.Scale = 1;
        base.OnDisappearing();
    }
}
