using KantinKita.Data;
using KantinKita.Models;

namespace KantinKita.Services;

public interface IMenuService
{
    IReadOnlyList<CanteenMenuItem> All { get; }
    CanteenMenuItem? Find(string id);
    IReadOnlyList<CanteenMenuItem> GetHighlights(int count, string? canteenId = null);
    IReadOnlyList<CanteenMenuItem> Query(MenuCategory category, string? search, MenuSort sort, string? canteenId = null);
}

public sealed class MenuService : IMenuService
{
    private readonly IReadOnlyList<CanteenMenuItem> _items = SeedData.Menu;

    public IReadOnlyList<CanteenMenuItem> All => _items;

    public CanteenMenuItem? Find(string id) => _items.FirstOrDefault(item => item.Id == id);

    public IReadOnlyList<CanteenMenuItem> GetHighlights(int count, string? canteenId = null) => _items
        .Where(item => canteenId is null || item.CanteenId == canteenId)
        .Where(item => item.IsBestSeller)
        .OrderByDescending(item => item.SoldCount)
        .Take(count)
        .ToArray();

    public IReadOnlyList<CanteenMenuItem> Query(MenuCategory category, string? search, MenuSort sort, string? canteenId = null)
    {
        IEnumerable<CanteenMenuItem> query = _items;

        if (canteenId is not null) query = query.Where(item => item.CanteenId == canteenId);

        if (category != MenuCategory.Semua)
        {
            query = query.Where(item => item.Category == category);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(item =>
                item.Name.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                item.Stall.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                item.Description.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        query = sort switch
        {
            MenuSort.Termurah => query.OrderBy(item => item.Price),
            MenuSort.Rating => query.OrderByDescending(item => item.Rating),
            _ => query.OrderByDescending(item => item.SoldCount),
        };

        return query.ToArray();
    }
}

