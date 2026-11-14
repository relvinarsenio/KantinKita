using KantinKita.ViewModels;

namespace KantinKita.Views.Tabs;

public partial class OrdersView : ContentView, IFloatingDockContent
{
    public OrdersView(OrdersViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    public void SetDockInset(double bottomInset) => ScrollBody.Padding = new Thickness(20, 20, 20, 28 + bottomInset);
}
