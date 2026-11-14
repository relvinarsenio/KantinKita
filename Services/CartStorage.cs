using System.Text.Json;
using System.Text.Json.Serialization;

namespace KantinKita.Services;

/// <summary>Shape persisted to disk for the cart. Kept separate from the domain model.</summary>
public sealed class CartSnapshot
{
    public List<CartLineSnapshot> Lines { get; set; } = [];
}

public sealed class CartLineSnapshot
{
    public string CanteenId { get; set; } = Models.CanteenCatalog.DefaultId;
    public string ItemId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Stall { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public string? Note { get; set; }
    public List<CartOptionSnapshot> Options { get; set; } = [];
}

public sealed class CartOptionSnapshot
{
    public string Name { get; set; } = string.Empty;
    public decimal ExtraPrice { get; set; }
}

/// <summary>Source generated JSON context: trim safe and allocation friendly.</summary>
// Earlier snapshots contain an Emoji field. Ignore it and derive the current icon
// from ItemId, so existing carts survive without persisting glyph SVG paths.
[JsonSourceGenerationOptions(WriteIndented = false, UnmappedMemberHandling = JsonUnmappedMemberHandling.Skip)]
[JsonSerializable(typeof(CartSnapshot))]
internal sealed partial class AppJsonContext : JsonSerializerContext;

public interface ICartStorage
{
    Task<CartSnapshot?> LoadAsync();
    Task SaveAsync(CartSnapshot snapshot);
}

public sealed class CartStorage : ICartStorage
{
    private static string FilePath => Path.Combine(FileSystem.AppDataDirectory, "kantin-cart.json");

    // Serialises writes so rapid +/− taps cannot interleave two file writes.
    private static readonly SemaphoreSlim WriteGate = new(1, 1);

    public async Task<CartSnapshot?> LoadAsync()
    {
        try
        {
            if (!File.Exists(FilePath))
            {
                return null;
            }

            await using var stream = File.OpenRead(FilePath);
            return await JsonSerializer.DeserializeAsync(stream, AppJsonContext.Default.CartSnapshot);
        }
        catch (Exception)
        {
            // A corrupt cache must never block the app; start with an empty cart.
            return null;
        }
    }

    public async Task SaveAsync(CartSnapshot snapshot)
    {
        await WriteGate.WaitAsync();
        try
        {
            var path = FilePath;
            var tempPath = path + ".tmp";

            await using (var stream = File.Create(tempPath))
            {
                await JsonSerializer.SerializeAsync(stream, snapshot, AppJsonContext.Default.CartSnapshot);
            }

            // Atomic swap keeps the cart readable if the process dies mid-write.
            File.Move(tempPath, path, overwrite: true);
        }
        catch (Exception)
        {
            // Persistence is best effort.
        }
        finally
        {
            WriteGate.Release();
        }
    }
}
