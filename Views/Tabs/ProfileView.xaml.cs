using KantinKita.ViewModels;

namespace KantinKita.Views.Tabs;

public partial class ProfileView : ContentView, IFloatingDockContent
{
    public ProfileView(ProfileViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    public void SetDockInset(double bottomInset) => ScrollBody.Padding = new Thickness(22, 22, 22, 28 + bottomInset);
}
