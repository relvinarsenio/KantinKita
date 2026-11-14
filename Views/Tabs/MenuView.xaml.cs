using KantinKita.ViewModels;
using KantinKita.Services;

namespace KantinKita.Views.Tabs;

public partial class MenuView : ContentView, IFloatingDockContent
{
    private bool _isSortPickerOpen;
    private bool _isCanteenPickerOpen;
    private bool _isCategoryPickerOpen;
    private readonly IDialogService _dialogs;
    public MenuView(MenuViewModel viewModel, IDialogService dialogs)
    {
        InitializeComponent();
        _dialogs = dialogs;
        BindingContext = viewModel;
    }

    public void SetDockInset(double bottomInset) => CatalogFooter.Padding = new Thickness(12, 16, 12, 32 + bottomInset);

    private async void OnSearchCompleted(object? sender, EventArgs e)
    {
        if (InputTransparent || !IsVisible || !IsLoaded) return;
        await Helpers.SearchInteraction.DismissKeyboardAsync(SearchEntry);
    }

    private async void OnCategoryTapped(object? sender, TappedEventArgs e)
    {
        if (_isCategoryPickerOpen || InputTransparent || !IsVisible || !IsLoaded ||
            BindingContext is not MenuViewModel viewModel) return;
        _isCategoryPickerOpen = true;
        try
        {
            await Helpers.SearchInteraction.DismissKeyboardAsync(SearchEntry);
            if (InputTransparent || !IsVisible || !IsLoaded) return;
            await viewModel.ChooseCategoryCommand.ExecuteAsync(null);
        }
        finally { _isCategoryPickerOpen = false; }
    }

    private async void OnCanteenTapped(object? sender, TappedEventArgs e)
    {
        if (_isCanteenPickerOpen || BindingContext is not MenuViewModel viewModel) return;
        _isCanteenPickerOpen = true;
        try
        {
            var selected = await _dialogs.ChooseAsync("Mau makan dari mana?",
                viewModel.Canteens.Select(canteen => new DialogChoice(canteen.Id, canteen.Name, canteen.Location)).ToArray(),
                viewModel.SelectedCanteen.Id, "Setiap kantin punya pilihan dan harga menunya sendiri.");
            if (selected is not null) viewModel.SelectCanteen(selected);
        }
        finally { _isCanteenPickerOpen = false; }
    }

    private async void OnSortTapped(object? sender, TappedEventArgs e)
    {
        if (_isSortPickerOpen || BindingContext is not MenuViewModel viewModel) return;
        _isSortPickerOpen = true;
        try
        {
            var descriptions = new[] { "Menu yang paling sering dipesan", "Harga terendah lebih dahulu", "Pilihan dengan ulasan terbaik" };
            var selected = await _dialogs.ChooseAsync("Urutkan menu",
                viewModel.Sorts.Select((sort, i) => new DialogChoice(sort.Label, sort.Label, descriptions[i])).ToArray(),
                viewModel.SortLabel, "Temukan pilihan yang paling pas buatmu.");
            viewModel.Sorts.FirstOrDefault(sort => sort.Label == selected)?.SelectCommand.Execute(null);
        }
        finally
        {
            _isSortPickerOpen = false;
        }
    }
}
