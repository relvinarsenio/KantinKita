using KantinKita.Controls;

namespace KantinKita.Models;

/// <summary>Presentation glyphs derived from stable menu IDs, never persisted SVG paths.</summary>
public static class MenuIconMap
{
    public static string ForItem(string? itemId, MenuCategory category = MenuCategory.Makanan) => BaseId(itemId) switch
    {
        "geprek" or "ayam-bakar" => Icons.FoodChickenLeg,
        "paket-hemat" or "rendang" => Icons.FoodRice,
        "nasi-goreng" => Icons.FoodEgg,
        "mie-ayam" or "soto" or "bubur-ayam" => Icons.FoodBowl,
        "gado-gado" or "salad-buah" => Icons.FoodSalad,
        "es-teh" or "es-jeruk" => Icons.Drink,
        "kopi-aren" => Icons.DrinkCoffee,
        "jus-almond" => Icons.DrinkSmoothie,
        "pisang-keju" => Icons.Cookie,
        "tahu-crispy" => Icons.FoodDumpling,
        "roti-bakar" => Icons.FoodToast,
        _ => ForCategory(category),
    };

    private static string? BaseId(string? itemId)
    {
        if (itemId is null) return null;
        var separator = itemId.LastIndexOf(':');
        return separator >= 0 ? itemId[(separator + 1)..] : itemId;
    }

    public static string ForCategory(MenuCategory category) => category switch
    {
        MenuCategory.Semua => Icons.Grid,
        MenuCategory.Minuman => Icons.Drink,
        MenuCategory.Camilan => Icons.Cookie,
        MenuCategory.Sehat => Icons.Leaf,
        _ => Icons.Food,
    };
}
