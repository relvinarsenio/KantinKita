using KantinKita.Models;

namespace KantinKita.Helpers;

public static class OrderStatusDisplay
{
    public static string Text(OrderStatus status) => status switch
    {
        OrderStatus.Confirmed => "Dikonfirmasi",
        OrderStatus.Preparing => "Sedang disiapkan",
        OrderStatus.Ready => "Siap diambil",
        OrderStatus.Completed => "Selesai",
        _ => "Dibatalkan",
    };
}
