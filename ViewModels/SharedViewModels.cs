using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KantinKita.Helpers;
using KantinKita.Models;
using KantinKita.Services;

namespace KantinKita.ViewModels;

/// <summary>Presentation wrapper for a menu item, used by every list in the app.</summary>
public partial class MenuItemCardViewModel : ObservableObject
{
    private readonly ICartService _cart;
    private readonly IAppNavigator _navigator;
    private readonly IDialogService? _dialogs;

    public MenuItemCardViewModel(
        CanteenMenuItem item,
        ICartService cart,
        IAppNavigator navigator,
        IDialogService? dialogs = null)
    {
        Item = item;
        _cart = cart;
        _navigator = navigator;
        _dialogs = dialogs;
        TileBrush = BrushFactory.Gradient(item.TileStart, item.TileEnd);
        SyncCart();
    }

    public CanteenMenuItem Item { get; }

    public string Name => Item.Name;
    public string IconGlyph => Item.IconGlyph;
    public string Stall => Item.Stall;
    public string PriceText => Money.Rupiah(Item.Price);
    public string RatingText => Money.Rating(Item.Rating);
    public bool IsBestSeller => Item.IsBestSeller;
    public Brush TileBrush { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsInCart))]
    [NotifyPropertyChangedFor(nameof(QuantityText))]
    private int quantityInCart;

    public bool IsInCart => QuantityInCart > 0;

    public string QuantityText => QuantityInCart.ToString(CultureInfo.InvariantCulture);

    public void SyncCart() => QuantityInCart = _cart.QuantityOf(Item.Id);

    [RelayCommand]
    private Task OpenAsync() => _navigator.OpenItemAsync(Item);

    [RelayCommand]
    private async Task AddAsync()
    {
        if (await _cart.AddWithConfirmationAsync(_dialogs, Item, Item.DefaultOptions(), 1)) Haptics.Tap();
    }
}

public partial class CategoryChipViewModel : ObservableObject
{
    private readonly Action<CategoryChipViewModel> _onSelect;

    public CategoryChipViewModel(MenuCategory category, string label, string iconGlyph, Action<CategoryChipViewModel> onSelect)
    {
        Category = category;
        Label = label;
        IconGlyph = iconGlyph;
        _onSelect = onSelect;
    }

    public MenuCategory Category { get; }
    public string Label { get; }
    public string IconGlyph { get; }

    [ObservableProperty]
    private bool isSelected;

    [RelayCommand]
    private void Select() => _onSelect(this);
}

public partial class SortChipViewModel : ObservableObject
{
    private readonly Action<SortChipViewModel> _onSelect;

    public SortChipViewModel(MenuSort sort, string label, Action<SortChipViewModel> onSelect)
    {
        Sort = sort;
        Label = label;
        _onSelect = onSelect;
    }

    public MenuSort Sort { get; }
    public string Label { get; }

    [ObservableProperty]
    private bool isSelected;

    [RelayCommand]
    private void Select() => _onSelect(this);
}

public partial class OptionGroupViewModel : ObservableObject
{
    private readonly Action _onChanged;
    private OptionChoiceViewModel _selected;

    public OptionGroupViewModel(MenuOptionGroup group, Action onChanged)
    {
        Title = group.Title;
        SelectionHint = group.IsRequired ? "Pilih 1" : "Opsional · pilih 1";
        Options = group.Options.Select(option => new OptionChoiceViewModel(option, this)).ToArray();
        _onChanged = onChanged;
        _selected = Options[0];
        _selected.IsSelected = true;
    }

    public string Title { get; }
    public string SelectionHint { get; }
    public IReadOnlyList<OptionChoiceViewModel> Options { get; }

    public MenuOption SelectedOption => _selected.Option;

    public void Select(OptionChoiceViewModel choice)
    {
        if (ReferenceEquals(_selected, choice))
        {
            return;
        }

        _selected.IsSelected = false;
        _selected = choice;
        choice.IsSelected = true;
        _onChanged();
    }

    public void Reset()
    {
        foreach (var option in Options)
        {
            option.IsSelected = false;
        }

        _selected = Options[0];
        _selected.IsSelected = true;
    }
}

public partial class OptionChoiceViewModel : ObservableObject
{
    private readonly OptionGroupViewModel _group;

    public OptionChoiceViewModel(MenuOption option, OptionGroupViewModel group)
    {
        Option = option;
        _group = group;
    }

    public MenuOption Option { get; }

    public string Name => Option.Name;
    public string PriceLabel => Option.ExtraPrice switch
    {
        > 0 => "+" + Money.Rupiah(Option.ExtraPrice),
        < 0 => "−" + Money.Rupiah(Math.Abs(Option.ExtraPrice)),
        _ => "Termasuk",
    };
    public string AccessibilityLabel => $"{_group.Title}, {Name}, {PriceLabel}, {(IsSelected ? "dipilih" : "belum dipilih")}";
    public string AccessibilityHint => $"Pilih satu opsi untuk {_group.Title}. Pilihan ini menggantikan pilihan sebelumnya.";

    public string Label => Option.ExtraPrice > 0
        ? $"{Option.Name} +{Money.Rupiah(Option.ExtraPrice)}"
        : Option.Name;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AccessibilityLabel))]
    private bool isSelected;

    [RelayCommand]
    private void Choose() => _group.Select(this);
}

