namespace KantinKita.Models;

public sealed record Canteen(string Id, string Name, string Location);

/// <summary>Local campus locations; replace this catalog with campus data when available.</summary>
public static class CanteenCatalog
{
    public const string DefaultId = "kantin-a";
    public static IReadOnlyList<Canteen> All { get; } =
    [
        new(DefaultId, "Kantin A · Taman Rasa", "Gedung A · area taman utama"),
        new("kantin-b", "Kantin B · Dapur Teknik", "Gedung B · lantai dasar"),
        new("kantin-c", "Kantin C · Sudut Pustaka", "Gedung C · dekat perpustakaan"),
    ];

    public static Canteen? Find(string? id) => All.FirstOrDefault(canteen => canteen.Id == id);
}
