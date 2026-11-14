using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KantinKita.Helpers;
using KantinKita.Models;
using KantinKita.Services;

namespace KantinKita.ViewModels;

public sealed class PromoSlideViewModel
{
    public required string Badge { get; init; }
    public required string Title { get; init; }
    public required string Subtitle { get; init; }
    public required string PriceText { get; init; }
    public string IconGlyph => Item.IconGlyph;
    public required Brush Background { get; init; }
    public required CanteenMenuItem Item { get; init; }
}

public partial class HomeViewModel : ObservableObject
{
    private readonly IMenuService _menu;
    private readonly ICartService _cart;
    private readonly IProfileService _profile;
    private readonly IAppNavigator _navigator;
    private readonly ICanteenSelectionService? _canteens;
    private readonly IDialogService? _dialogs;
    private Canteen _fallbackCanteen = CanteenCatalog.All[0];

    private MenuCategory _category = MenuCategory.Semua;

    public HomeViewModel(
        IMenuService menu,
        ICartService cart,
        IProfileService profile,
        IAppNavigator navigator,
        ICanteenSelectionService? canteens = null,
        IDialogService? dialogs = null)
    {
        _menu = menu;
        _cart = cart;
        _profile = profile;
        _navigator = navigator;
        _canteens = canteens;
        _dialogs = dialogs;

        Categories = Enum.GetValues<MenuCategory>()
            .Select(category => new CategoryChipViewModel(category,
                category == MenuCategory.Semua ? "Semua menu" : category.ToString(),
                MenuIconMap.ForCategory(category), OnCategorySelected)).ToArray();
        Categories[0].IsSelected = true;

        Highlights = [];
        RebuildCanteenContent();

        RefreshList();

        _cart.Changed += (_, _) => SyncCartQuantities();
        if (_canteens is not null) _canteens.Changed += (_, _) => OnCanteenChanged();
        _profile.Changed += (_, _) =>
        {
            OnPropertyChanged(nameof(FirstName));
            OnPropertyChanged(nameof(AvatarInitials));
        };
    }

    public string Greeting => DateTime.Now.Hour switch
    {
        < 11 => "Selamat pagi,",
        < 15 => "Selamat siang,",
        < 19 => "Selamat sore,",
        _ => "Selamat malam,",
    };

    public string FirstName => _profile.Profile.Name.Split(' ')[0];
    public string AvatarInitials => _profile.Profile.Initials;
    public Canteen SelectedCanteen => _canteens?.Selected ?? _fallbackCanteen;
    public IReadOnlyList<Canteen> Canteens => CanteenCatalog.All;
    public string CanteenName => SelectedCanteen.Name;
    public string CanteenShortName => SelectedCanteen.Name.Split('·', 2).Last().Trim();
    public string LocationText => SelectedCanteen.Location;
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
        OnPropertyChanged(nameof(LocationText));
        RebuildCanteenContent();
        RefreshList();
    }

    private void RebuildCanteenContent()
    {
        var offers = _menu.All.Where(item => item.CanteenId == SelectedCanteen.Id).ToArray();
        if (offers.Length == 0) throw new InvalidOperationException("Kantin belum memiliki menu.");
        Highlights.Clear();
        foreach (var item in _menu.GetHighlights(6, SelectedCanteen.Id)) Highlights.Add(CreateCard(item));
        var preferred = new[] { "paket-hemat", "geprek", "mie-ayam", "gado-gado" };
        var promoted = preferred.Select(id => offers.FirstOrDefault(item => item.Id == id || item.Id.EndsWith(":" + id, StringComparison.Ordinal)))
            .Where(item => item is not null).Select(item => item!).ToList();
        foreach (var offer in offers.Where(item => item.IsBestSeller).Concat(offers))
            if (promoted.Count < 4 && promoted.All(item => item.Id != offer.Id)) promoted.Add(offer);
        var colors = new[] { ("#173F9A", "#245BCA"), ("#4338A0", "#5146B1"), ("#165766", "#146477"), ("#26364D", "#3D5472") };
        Promos = promoted.Take(4).Select((item, index) => new PromoSlideViewModel
        {
            Badge = index == 0 ? "PILIHAN HARI INI" : item.IsBestSeller ? "FAVORIT KANTIN" : "COBA HARI INI",
            Title = item.Name, Subtitle = item.Description, PriceText = Money.Rupiah(item.Price),
            Background = BrushFactory.Gradient(colors[index].Item1, colors[index].Item2), Item = item,
        }).ToArray();
        OnPropertyChanged(nameof(Promos));
        CurrentPromoIndex = 0;
        CurrentPromo = Promos[0];
        PreviewPromo = Promos[Promos.Count > 1 ? 1 : 0];
        Promo = CreateCard(CurrentPromo.Item);
        OnPropertyChanged(nameof(Promo));
        UpdatePromoIndicator();
    }
    public string PromoTitle => CurrentPromo.Title;
    public string PromoSubtitle => CurrentPromo.Subtitle;
    public string PromoPriceText => CurrentPromo.PriceText;

    public IReadOnlyList<CategoryChipViewModel> Categories { get; }
    public string SelectedCategoryLabel => Categories.First(chip => chip.Category == _category).Label;
    public string SelectedCategoryGlyph => MenuIconMap.ForCategory(_category);
    public string CategorySummaryText => $"{_menu.All.Where(item => item.CanteenId == SelectedCanteen.Id).Select(item => item.Category).Distinct().Count()} kategori tersedia";
    public bool IsBrowsing => !IsSearching && _category == MenuCategory.Semua;
    public bool HasCategoryFilter => _category != MenuCategory.Semua;
    public ObservableCollection<MenuItemCardViewModel> Highlights { get; }
    public ObservableCollection<MenuItemCardViewModel> Recommendations { get; } = [];
    public MenuItemCardViewModel Promo { get; private set; } = null!;
    public IReadOnlyList<PromoSlideViewModel> Promos { get; private set; } = [];

    [ObservableProperty]
    private int currentPromoIndex;

    [ObservableProperty]
    private PromoSlideViewModel currentPromo = null!;

    [ObservableProperty]
    private PromoSlideViewModel previewPromo = null!;

    [ObservableProperty]
    private string promoIndicatorText = string.Empty;

    partial void OnCurrentPromoChanged(PromoSlideViewModel value)
    {
        OnPropertyChanged(nameof(PromoTitle));
        OnPropertyChanged(nameof(PromoSubtitle));
        OnPropertyChanged(nameof(PromoPriceText));
    }

    [ObservableProperty]
    private string searchText = string.Empty;

    [ObservableProperty]
    private bool isEmpty;

    [ObservableProperty]
    private bool isSearching;

    [ObservableProperty]
    private string recommendationsTitle = "Rekomendasi buat kamu";

    [ObservableProperty]
    private string resultCountText = string.Empty;

    [ObservableProperty]
    private bool hasMoreItems;

    [ObservableProperty]
    private string resultsDescription = string.Empty;

    partial void OnSearchTextChanged(string value)
    {
        IsSearching = !string.IsNullOrWhiteSpace(value);
        RefreshList();
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

        RefreshList();
    }

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

    [RelayCommand]
    private void ClearCategory() => Categories.First(chip => chip.Category == MenuCategory.Semua).SelectCommand.Execute(null);

    [RelayCommand]
    private void ResetFilters()
    {
        _category = MenuCategory.Semua;
        foreach (var chip in Categories) chip.IsSelected = chip.Category == _category;
        SearchText = string.Empty;
        RefreshList();
    }

    [RelayCommand]
    public void GoToNextPromo() => AdvancePromo(previous: false);

    [RelayCommand]
    public void GoToPreviousPromo() => AdvancePromo(previous: true);

    internal void AdvancePromo(bool previous, bool preservePreview = false)
    {
        CurrentPromoIndex = (CurrentPromoIndex + (previous ? -1 : 1) + Promos.Count) % Promos.Count;
        CurrentPromo = Promos[CurrentPromoIndex];
        // The native deck keeps the promoted card visible until the replacement
        // front has rendered. Changing its preview here would flash the next promo.
        if (!preservePreview) ShowPromoPreview(previous: false);
        UpdatePromoIndicator();
    }

    public void ShowPromoPreview(bool previous) =>
        PreviewPromo = Promos[(CurrentPromoIndex + (previous ? -1 : 1) + Promos.Count) % Promos.Count];

    private void UpdatePromoIndicator()
    {
        PromoIndicatorText = string.Join("   ", Promos.Select((_, i) => i == CurrentPromoIndex ? "●" : "○"));
    }

    [RelayCommand]
    private Task SeeAllAsync() => _navigator.ShowMenuAsync();

    [RelayCommand]
    private void ClearSearch() => SearchText = string.Empty;

    [RelayCommand]
    private Task OpenPromoAsync() => _navigator.OpenItemAsync(CurrentPromo.Item);

    private void RefreshList()
    {
        var items = _menu.Query(_category, SearchText, MenuSort.Populer, SelectedCanteen.Id);

        Recommendations.Clear();
        // Discovery is a short preview; an explicit search/filter must show every
        // match rather than silently truncating the results to six dishes.
        foreach (var item in IsBrowsing ? items.Take(6) : items)
        {
            Recommendations.Add(CreateCard(item));
        }

        IsEmpty = items.Count == 0;
        HasMoreItems = IsBrowsing && items.Count > Recommendations.Count;
        RecommendationsTitle = IsSearching
            ? "Hasil pencarian"
            : _category == MenuCategory.Semua
                ? "Pilihan kantinmu"
                : $"Menu {_category.ToString().ToLowerInvariant()}";
        ResultCountText = HasMoreItems ? $"{Recommendations.Count} dari {items.Count} menu" : $"{items.Count} menu";
        ResultsDescription = IsSearching
            ? $"Untuk \"{SearchText.Trim()}\" · {SelectedCategoryLabel.ToLowerInvariant()}"
            : IsBrowsing ? "Disiapkan segar dari kantin pilihanmu." : $"Pilihan {SelectedCategoryLabel.ToLowerInvariant()} di {CanteenName}.";
        OnPropertyChanged(nameof(IsBrowsing));
        OnPropertyChanged(nameof(HasCategoryFilter));
        OnPropertyChanged(nameof(SelectedCategoryLabel));
        OnPropertyChanged(nameof(SelectedCategoryGlyph));
        OnPropertyChanged(nameof(CategorySummaryText));
    }

    private void SyncCartQuantities()
    {
        Promo.SyncCart();
        foreach (var card in Highlights.Concat(Recommendations))
        {
            card.SyncCart();
        }
    }

    private MenuItemCardViewModel CreateCard(CanteenMenuItem item) => new(item, _cart, _navigator, _dialogs);
}
