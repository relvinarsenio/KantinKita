using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KantinKita.Models;
using KantinKita.Services;

namespace KantinKita.ViewModels;

public partial class MenuViewModel : ObservableObject
{
    private readonly IMenuService _menu;
    private readonly ICartService _cart;
    private readonly IAppNavigator _navigator;
    private readonly ICanteenSelectionService? _canteens;
    private readonly IDialogService? _dialogs;
    private Canteen _fallbackCanteen = CanteenCatalog.All[0];

    private MenuCategory _category = MenuCategory.Semua;
    private MenuSort _sort = MenuSort.Populer;

    public MenuViewModel(
        IMenuService menu,
        ICartService cart,
        IAppNavigator navigator,
        ICanteenSelectionService? canteens = null,
        IDialogService? dialogs = null)
    {
        _menu = menu;
        _cart = cart;
        _navigator = navigator;
        _canteens = canteens;
        _dialogs = dialogs;

        Categories = Enum.GetValues<MenuCategory>()
            .Select(category => new CategoryChipViewModel(category,
                category == MenuCategory.Semua ? "Semua menu" : category.ToString(),
                MenuIconMap.ForCategory(category), OnCategorySelected)).ToArray();
        Categories[0].IsSelected = true;

        Sorts =
        [
            new(MenuSort.Populer, "Terlaris", OnSortSelected),
            new(MenuSort.Termurah, "Termurah", OnSortSelected),
            new(MenuSort.Rating, "Rating tertinggi", OnSortSelected),
        ];
        Sorts[0].IsSelected = true;

        Refresh();

        _cart.Changed += (_, _) => SyncCartQuantities();
        if (_canteens is not null) _canteens.Changed += (_, _) => OnCanteenChanged();
    }

    public Canteen SelectedCanteen => _canteens?.Selected ?? _fallbackCanteen;
    public IReadOnlyList<Canteen> Canteens => CanteenCatalog.All;
    public string CanteenName => SelectedCanteen.Name;
    public string CanteenShortName => SelectedCanteen.Name.Split('·', 2).Last().Trim();
    public string CanteenLocation => SelectedCanteen.Location;
    public void SelectCanteen(string id)
    {
        if (_canteens is not null) _canteens.Select(id);
        else
        {
            _fallbackCanteen = CanteenCatalog.Find(id) ?? throw new ArgumentException("Kantin tidak ditemukan.", nameof(id));
            OnCanteenChanged();
        }
    }

    private void OnCanteenChanged()
    {
        OnPropertyChanged(nameof(SelectedCanteen));
        OnPropertyChanged(nameof(CanteenName));
        OnPropertyChanged(nameof(CanteenShortName));
        OnPropertyChanged(nameof(CanteenLocation));
        Refresh();
    }

    public IReadOnlyList<CategoryChipViewModel> Categories { get; }
    public string SelectedCategoryLabel => Categories.First(chip => chip.Category == _category).Label;
    public string SelectedCategoryGlyph => MenuIconMap.ForCategory(_category);
    public string CategorySummaryText => $"{_menu.All.Where(item => item.CanteenId == SelectedCanteen.Id).Select(item => item.Category).Distinct().Count()} kategori tersedia";
    public IReadOnlyList<SortChipViewModel> Sorts { get; }
    public string SortLabel => _sort switch
    {
        MenuSort.Termurah => "Termurah",
        MenuSort.Rating => "Rating tertinggi",
        _ => "Terlaris",
    };
    public ObservableCollection<MenuItemCardViewModel> Items { get; } = [];

    [ObservableProperty]
    private string searchText = string.Empty;

    [ObservableProperty]
    private bool isEmpty;

    [ObservableProperty]
    private bool isSearching;

    [ObservableProperty]
    private string summaryText = string.Empty;

    partial void OnSearchTextChanged(string value)
    {
        IsSearching = !string.IsNullOrWhiteSpace(value);
        Refresh();
    }

    private void OnCategorySelected(CategoryChipViewModel chip)
    {
        if (_category == chip.Category)
        {
            return;
        }

        _category = chip.Category;
        foreach (var category in Categories)
        {
            category.IsSelected = category.Category == _category;
        }

        Refresh();
    }

    private void OnSortSelected(SortChipViewModel chip)
    {
        if (_sort == chip.Sort)
        {
            return;
        }

        _sort = chip.Sort;
        foreach (var sort in Sorts)
        {
            sort.IsSelected = sort.Sort == _sort;
        }

        OnPropertyChanged(nameof(SortLabel));
        Refresh();
    }

    [RelayCommand]
    private void ClearSearch() => SearchText = string.Empty;

    [RelayCommand]
    private async Task ChooseCategoryAsync()
    {
        if (_dialogs is null) return;
        var canteenId = SelectedCanteen.Id;
        var offers = _menu.All.Where(item => item.CanteenId == canteenId).ToArray();
        var choices = Categories.Select(chip =>
        {
            var count = offers.Count(item => chip.Category == MenuCategory.Semua || item.Category == chip.Category);
            return new DialogChoice(chip.Category.ToString(), chip.Label, $"{count} menu di {CanteenName}", chip.IconGlyph,
                IsEnabled: count > 0 || chip.Category == MenuCategory.Semua, Badge: $"{count} menu");
        }).ToArray();
        var selected = await _dialogs.ChooseCategoryAsync("Mau yang mana?", choices, _category.ToString(), CanteenName);
        if (canteenId != SelectedCanteen.Id) return;
        Categories.FirstOrDefault(chip => chip.Category.ToString() == selected && choices.Any(choice => choice.Id == selected && choice.IsEnabled))
            ?.SelectCommand.Execute(null);
    }

    public void Refresh()
    {
        var items = _menu.Query(_category, SearchText, _sort, SelectedCanteen.Id);

        Items.Clear();
        foreach (var item in items)
        {
            Items.Add(new MenuItemCardViewModel(item, _cart, _navigator, _dialogs));
        }

        IsEmpty = items.Count == 0;
        SummaryText = string.IsNullOrWhiteSpace(SearchText)
            ? $"{items.Count} menu siap dipesan"
            : $"{items.Count} hasil untuk \"{SearchText.Trim()}\"";
        OnPropertyChanged(nameof(SelectedCategoryLabel));
        OnPropertyChanged(nameof(SelectedCategoryGlyph));
        OnPropertyChanged(nameof(CategorySummaryText));
    }

    private void SyncCartQuantities()
    {
        foreach (var card in Items)
        {
            card.SyncCart();
        }
    }

}

