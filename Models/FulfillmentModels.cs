using System.Text.Json.Serialization;

namespace KantinKita.Models;

public enum FulfillmentMode { Pickup, DineIn, Delivery }

/// <summary>A receipt-safe snapshot of how and where this order is received.</summary>
public sealed record OrderDestination
{
    public required string CanteenId { get; init; }
    public required string CanteenName { get; init; }
    public FulfillmentMode Mode { get; init; }
    public string? TableId { get; init; }
    public string? TableLabel { get; init; }
    public string? Building { get; init; }
    public string? Room { get; init; }

    [JsonIgnore] public string ModeText => Mode switch
    {
        FulfillmentMode.DineIn => "Makan di kantin",
        FulfillmentMode.Delivery => "Antar ke ruangan",
        _ => "Ambil sendiri",
    };
    [JsonIgnore] public string LocationText => Mode switch
    {
        FulfillmentMode.DineIn => $"{CanteenName} · {TableLabel}",
        FulfillmentMode.Delivery => $"{Building} · {Room}",
        _ => CanteenName,
    };

    public string? ValidationError()
    {
        static bool Valid(string? value, int max = 100) => !string.IsNullOrWhiteSpace(value)
            && value.Trim().Length <= max && !value.Any(char.IsControl);
        if (!Enum.IsDefined(Mode) || !Valid(CanteenId, 64) || !Valid(CanteenName))
            return "Kantin atau cara menerima pesanan belum valid.";
        if (Mode == FulfillmentMode.DineIn && (!Valid(TableId, 64) || !Valid(TableLabel)))
            return "Pilih meja terlebih dahulu.";
        if (Mode == FulfillmentMode.Delivery && (!Valid(Building, 80) || !Valid(Room, 80)))
            return "Pilih gedung dan isi ruangan tujuan terlebih dahulu.";
        return null;
    }
}

public static class FulfillmentDisplay
{
    public static string Mode(Order order) => order.Destination?.ModeText ?? "Ambil sendiri";
    public static string Canteen(Order order) => order.Destination?.CanteenName
        ?? order.Items.FirstOrDefault()?.CanteenName ?? "Kantin";
    public static string Location(Order order) => order.Destination?.LocationText ?? Canteen(order);
    public static string ReadyTimeLabel(Order order) => order.Destination?.Mode == FulfillmentMode.Delivery
        ? "Jadwal siap diantar" : "Jadwal siap";
    public static string ReadyStatus(Order order) => order.Destination?.Mode switch
    {
        FulfillmentMode.Delivery => "Siap diantar",
        FulfillmentMode.DineIn => "Siap ke meja",
        _ => "Siap diambil",
    };
}
