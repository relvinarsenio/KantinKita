using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using KantinKita.Controls;
using KantinKita.Helpers;
using KantinKita.Models;
using KantinKita.Services;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Storage;

namespace KantinKita.ViewModels;

public sealed record NotificationEntryViewModel(Order Order, OrderStatus Status)
{
    public string Title => Status == OrderStatus.Ready ? FulfillmentDisplay.ReadyStatus(Order) : OrderStatusDisplay.Text(Status);
    public string Message => $"{Order.Code} · {Order.ItemCount} item · {Money.Rupiah(Order.Total)}";
    public string TimeText => Order.PlacedAt.ToLocalTime().ToString("d MMM · HH.mm", CultureInfo.GetCultureInfo("id-ID"));
    public string IconGlyph => Status switch
    {
        OrderStatus.Confirmed or OrderStatus.Preparing => Icons.Clock,
        OrderStatus.Ready or OrderStatus.Completed => Icons.CheckCircle,
        _ => Icons.Receipt,
    };
}

/// <summary>Latest persisted order activity; read state is keyed by order and status.</summary>
public partial class HomeNotificationsViewModel : ObservableObject
{
    private readonly IOrderService _orders;
    private readonly IPreferences _preferences;

    public HomeNotificationsViewModel(IOrderService orders, IPreferences preferences)
    {
        _orders = orders;
        _preferences = preferences;
        // Both services and this feed share the application's singleton lifetime.
        _orders.Changed += (_, _) => MainThread.BeginInvokeOnMainThread(() => Refresh());
        Refresh();
    }

    public ObservableCollection<NotificationEntryViewModel> Entries { get; } = [];

    [ObservableProperty]
    private bool hasEntries;

    [ObservableProperty]
    private bool hasUnread;

    public void Refresh(bool markRead = false)
    {
        var latest = _orders.Orders.OrderByDescending(order => order.PlacedAt)
            .Take(4)
            .Select(order => new NotificationEntryViewModel(order, _orders.GetStatus(order)))
            .ToArray();

        if (markRead)
        {
            foreach (var entry in latest)
            {
                _preferences.Set(ReadKey(entry.Order), (int)entry.Status);
            }
        }

        Entries.Clear();
        foreach (var entry in latest) Entries.Add(entry);
        HasEntries = latest.Length > 0;
        HasUnread = latest.Any(entry => _preferences.Get(ReadKey(entry.Order), -1) != (int)entry.Status);
    }

    private static string ReadKey(Order order) => $"notifications.read.{order.Code}";
}
