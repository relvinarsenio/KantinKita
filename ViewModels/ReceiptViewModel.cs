using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KantinKita.Helpers;
using KantinKita.Models;
using KantinKita.Services;
using Microsoft.Maui.ApplicationModel.DataTransfer;

namespace KantinKita.ViewModels;

public partial class ReceiptViewModel(IOrderService orders, IReceiptPdfService pdf, IAppNavigator navigator, IDialogService dialogs) : ObservableObject
{
    private Order? _order;
    private static readonly CultureInfo Indonesian = CultureInfo.GetCultureInfo("id-ID");
    public ObservableCollection<ReceiptLineViewModel> Lines { get; } = [];
    [ObservableProperty] private string code = string.Empty;
    [ObservableProperty] private string placedText = string.Empty;
    [ObservableProperty] private string pickupText = string.Empty;
    [ObservableProperty] private string fulfillmentText = string.Empty;
    [ObservableProperty] private string destinationText = string.Empty;
    [ObservableProperty] private string canteenText = string.Empty;
    [ObservableProperty] private string statusText = string.Empty;
    [ObservableProperty] private string paymentText = string.Empty;
    [ObservableProperty] private string itemCountText = string.Empty;
    [ObservableProperty] private string subtotalText = string.Empty;
    [ObservableProperty] private string serviceFeeText = string.Empty;
    [ObservableProperty] private string discountText = string.Empty;
    [ObservableProperty] private string totalText = string.Empty;
    [ObservableProperty] private string voucherText = string.Empty;
    [ObservableProperty] private string noteText = string.Empty;
    [ObservableProperty] private bool hasDiscount;
    [ObservableProperty] private bool hasVoucher;
    [ObservableProperty] private bool hasNote;
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SharePdfCommand))]
    private bool isSharing;

    public void Prepare(Order order)
    {
        ArgumentNullException.ThrowIfNull(order);
        _order = order;
        Code = order.Code;
        PlacedText = order.PlacedAt.ToLocalTime().ToString("dd MMMM yyyy · HH.mm zzz", Indonesian);
        PickupText = order.PickupAt.ToLocalTime().ToString("dd MMM yyyy · HH.mm", Indonesian);
        FulfillmentText = FulfillmentDisplay.Mode(order);
        DestinationText = FulfillmentDisplay.Location(order);
        CanteenText = FulfillmentDisplay.Canteen(order);
        var status = orders.GetStatus(order);
        StatusText = status == OrderStatus.Ready ? FulfillmentDisplay.ReadyStatus(order) : OrderStatusDisplay.Text(status);
        PaymentText = order.PaymentMethod;
        ItemCountText = $"{order.ItemCount} item · {order.Items.Count} pilihan";
        SubtotalText = Money.Rupiah(order.Subtotal);
        ServiceFeeText = Money.Rupiah(order.ServiceFee);
        DiscountText = "−" + Money.Rupiah(order.Discount);
        TotalText = Money.Rupiah(order.Total);
        VoucherText = order.VoucherCode ?? string.Empty;
        NoteText = order.Note ?? string.Empty;
        HasDiscount = order.Discount > 0;
        HasVoucher = !string.IsNullOrWhiteSpace(order.VoucherCode);
        HasNote = !string.IsNullOrWhiteSpace(order.Note);
        Lines.Clear();
        foreach (var line in order.Items) Lines.Add(new ReceiptLineViewModel(line));
        SharePdfCommand.NotifyCanExecuteChanged();
    }

    private bool CanSharePdf() => _order is not null && !IsSharing;

    [RelayCommand(CanExecute = nameof(CanSharePdf))]
    private async Task SharePdfAsync()
    {
        if (_order is not { } order) return;
        IsSharing = true;
        try
        {
            var path = await pdf.CreateAsync(order, orders.GetStatus(order));
            await Share.Default.RequestAsync(new ShareFileRequest
            {
                Title = $"Struk {order.Code}",
                File = new ShareFile(path, "application/pdf"),
            });
        }
        catch (OperationCanceledException) { }
        catch (Exception)
        {
            await dialogs.AlertAsync("Struk belum bisa dibagikan", "Coba lagi dan pastikan ruang penyimpanan perangkat tersedia.");
        }
        finally { IsSharing = false; }
    }

    [RelayCommand]
    private Task BackAsync() => navigator.CloseAsync();
}

public sealed class ReceiptLineViewModel
{
    public ReceiptLineViewModel(CartItem line)
    {
        Name = line.Name;
        Stall = line.Stall;
        QuantityPriceText = $"{line.Quantity} × {Money.Rupiah(line.UnitPrice)}";
        LineTotalText = Money.Rupiah(line.LineTotal);
        OptionsText = string.Join(", ", line.Options.Select(option => option.ExtraPrice > 0
            ? $"{option.Name} (+{Money.Rupiah(option.ExtraPrice)} / item)" : option.Name));
        NoteText = line.Note ?? string.Empty;
    }
    public string Name { get; }
    public string Stall { get; }
    public string QuantityPriceText { get; }
    public string LineTotalText { get; }
    public string OptionsText { get; }
    public string NoteText { get; }
    public bool HasOptions => !string.IsNullOrWhiteSpace(OptionsText);
    public bool HasNote => !string.IsNullOrWhiteSpace(NoteText);
}
