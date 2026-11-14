using KantinKita.Models;

namespace KantinKita.Services;

public sealed class CartCanteenConflictException(string existingName, string incomingName)
    : InvalidOperationException($"Keranjang {existingName} tidak dapat digabung dengan {incomingName}.");

/// <summary>Serializes add/replace confirmation across cards, detail, and reorder.</summary>
public static class CartActions
{
    private static readonly SemaphoreSlim Gate = new(1, 1);

    public static Task<bool> AddWithConfirmationAsync(this ICartService cart, IDialogService? dialogs,
        CanteenMenuItem item, IReadOnlyList<MenuOption> options, int quantity, string? note = null) =>
        RunAsync(cart, dialogs, item.CanteenId,
            () => cart.Add(item, options, quantity, note),
            () => cart.ReplaceWith([new CartItem
            {
                ItemId = item.Id, CanteenId = item.CanteenId, Name = item.Name, Stall = item.Stall,
                UnitPrice = item.Price + options.Sum(option => option.ExtraPrice), Quantity = quantity,
                Options = options.ToArray(), Note = note,
            }]));

    public static Task<bool> AddRangeWithConfirmationAsync(this ICartService cart, IDialogService? dialogs,
        IReadOnlyList<CartItem> lines)
    {
        var snapshot = lines.Select(line => line.Clone()).ToArray();
        if (snapshot.Length == 0) return Task.FromResult(false);
        return RunAsync(cart, dialogs, snapshot[0].CanteenId,
            () => cart.AddRange(snapshot), () => cart.ReplaceWith(snapshot));
    }

    private static async Task<bool> RunAsync(ICartService cart, IDialogService? dialogs,
        string canteenId, Action add, Action replace)
    {
        await Gate.WaitAsync();
        try
        {
            if (cart.Items.FirstOrDefault(line => line.CanteenId != canteenId) is { } existing)
            {
                var name = CanteenCatalog.Find(canteenId)?.Name ?? canteenId;
                if (dialogs is null || !await dialogs.ConfirmAsync("Ganti kantin pesanan?",
                    $"Keranjangmu berisi menu dari {existing.CanteenName}. Untuk memesan dari {name}, isi keranjang saat ini akan diganti.",
                    "Ganti keranjang", "Pertahankan")) return false;
                replace();
            }
            else add();
            return true;
        }
        finally { Gate.Release(); }
    }
}
