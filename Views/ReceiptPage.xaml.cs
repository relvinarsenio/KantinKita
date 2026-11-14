using KantinKita.Models;
using KantinKita.ViewModels;

namespace KantinKita.Views;

public partial class ReceiptPage : ContentPage
{
    private readonly ReceiptViewModel _viewModel;
    public ReceiptPage(ReceiptViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }
    public void Prepare(Order order) => _viewModel.Prepare(order);
}
