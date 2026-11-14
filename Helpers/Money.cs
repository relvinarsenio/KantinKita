using System.Globalization;

namespace KantinKita.Helpers;

/// <summary>Currency and number formatting for the Indonesian Rupiah.</summary>
public static class Money
{
    public const decimal ServiceFee = 1000m;

    private static readonly CultureInfo Indonesian = CultureInfo.GetCultureInfo("id-ID");

    /// <summary>12345 -> "Rp12.345"</summary>
    public static string Rupiah(decimal amount) => "Rp" + amount.ToString("N0", Indonesian);

    /// <summary>12345 -> "12.345" (no currency prefix).</summary>
    public static string Number(decimal amount) => amount.ToString("N0", Indonesian);

    public static string Rating(double rating) => rating.ToString("0.0", CultureInfo.InvariantCulture);
}
