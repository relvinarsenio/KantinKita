using KantinKita.Models;

namespace KantinKita.Services;

public sealed record CanteenTable(string Id, string Label, int Seats, bool IsAvailable);

/// <summary>Replace with the campus table feed; local data never claims live availability.</summary>
public interface ITableAvailabilityProvider
{
    string AvailabilityNote { get; }
    Task<IReadOnlyList<CanteenTable>> GetTablesAsync(string canteenId);
}

public sealed class LocalTableAvailabilityProvider(IOrderService orders) : ITableAvailabilityProvider
{
    public string AvailabilityNote => "Simulasi ketersediaan meja. Status belum tersinkron dengan pengelola kantin.";
    public async Task<IReadOnlyList<CanteenTable>> GetTablesAsync(string canteenId)
    {
        if (CanteenCatalog.Find(canteenId) is null) return [];
        await orders.InitializeAsync();
        if (!orders.IsInitialized) throw new IOException("Riwayat lokal belum dapat dimuat untuk memeriksa meja.");
        var occupied = orders.Orders.Where(order => orders.IsActive(order)
            && order.Destination?.Mode == FulfillmentMode.DineIn
            && order.Destination.CanteenId == canteenId)
            .Select(order => order.Destination!.TableId).ToHashSet(StringComparer.Ordinal);
        var prefix = canteenId.Split('-').Last().ToUpperInvariant();
        IReadOnlyList<CanteenTable> tables = Enumerable.Range(1, 8).Select(number => new CanteenTable(
            $"{canteenId}:table-{number:D2}", $"Meja {prefix}-{number:D2}", number % 3 == 0 ? 6 : 4,
            number is not (2 or 5) && !occupied.Contains($"{canteenId}:table-{number:D2}"))).ToArray();
        return tables;
    }
}

public interface IFulfillmentService
{
    string AvailabilityNote { get; }
    IReadOnlyList<string> Buildings { get; }
    Task<IReadOnlyList<CanteenTable>> GetTablesAsync(string canteenId);
    Task<string?> ValidateAsync(OrderDestination destination);
}

public sealed class FulfillmentService(ITableAvailabilityProvider tables) : IFulfillmentService
{
    public string AvailabilityNote => tables.AvailabilityNote;
    public IReadOnlyList<string> Buildings { get; } = ["Gedung A", "Gedung B", "Gedung C", "Perpustakaan"];
    public Task<IReadOnlyList<CanteenTable>> GetTablesAsync(string canteenId) => tables.GetTablesAsync(canteenId);
    public async Task<string?> ValidateAsync(OrderDestination destination)
    {
        if (destination.ValidationError() is { } error) return error;
        if (CanteenCatalog.Find(destination.CanteenId) is null) return "Kantin tidak ditemukan. Pilih ulang menu kantin.";
        if (destination.Mode == FulfillmentMode.DineIn)
        {
            var current = await tables.GetTablesAsync(destination.CanteenId);
            var selected = current.FirstOrDefault(table => table.Id == destination.TableId);
            if (selected is null || !selected.IsAvailable) return "Meja ini belum tersedia. Pilih meja lain.";
            if (selected.Label != destination.TableLabel) return "Informasi meja berubah. Pilih ulang meja.";
        }
        if (destination.Mode == FulfillmentMode.Delivery && !Buildings.Contains(destination.Building))
            return "Pilih gedung tujuan yang tersedia.";
        return null;
    }
}
