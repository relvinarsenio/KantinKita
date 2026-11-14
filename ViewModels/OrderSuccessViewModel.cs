using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KantinKita.Helpers;
using KantinKita.Models;
using KantinKita.Services;

namespace KantinKita.ViewModels;

public partial class OrderSuccessViewModel : ObservableObject
{
    private readonly IAppNavigator _navigator;
    private Order? _order;

    public OrderSuccessViewModel(IAppNavigator navigator) => _navigator = navigator;

    [ObservableProperty]
    private string code = string.Empty;

    [ObservableProperty]
    private string pickupText = string.Empty;

    [ObservableProperty]
    private string pickupTimeText = string.Empty;
    [ObservableProperty] private string destinationText = string.Empty;
    [ObservableProperty] private string fulfillmentText = string.Empty;
    [ObservableProperty] private string receiveStepTitle = string.Empty;
    [ObservableProperty] private string codeInstruction = string.Empty;

    [ObservableProperty]
    private string totalText = string.Empty;

    [ObservableProperty]
    private string paymentText = string.Empty;

    [ObservableProperty]
    private string itemsText = string.Empty;

    [ObservableProperty]
    private string savingText = string.Empty;

    [ObservableProperty]
    private bool hasSaving;

    [ObservableProperty]
    private string noteText = string.Empty;

    [ObservableProperty]
    private bool hasNote;

    public void Prepare(Order order)
    {
        _order = order;
        Code = order.Code;
        DestinationText = FulfillmentDisplay.Location(order);
        FulfillmentText = FulfillmentDisplay.Mode(order);
        ReceiveStepTitle = order.Destination?.Mode switch
        {
            FulfillmentMode.Delivery => "Terima di ruanganmu",
            FulfillmentMode.DineIn => "Nikmati di meja pilihanmu",
            _ => "Ambil dan nikmati",
        };
        CodeInstruction = order.Destination?.Mode == FulfillmentMode.Pickup || order.Destination is null
            ? "Tunjukkan kode ini ke kasir." : "Cocokkan kode ini saat menerima pesanan.";
        PickupText = $"Estimasi siap pukul {order.PickupAt:HH.mm} · {DestinationText}";
        PickupTimeText = order.PickupAt.ToString("HH.mm");
        TotalText = Money.Rupiah(order.Total);
        PaymentText = order.PaymentMethod;
        ItemsText = string.Join("\n", order.Items.Select(item => $"{item.Quantity}× {item.Name}"));
        HasSaving = order.Discount > 0;
        SavingText = $"Kamu hemat {Money.Rupiah(order.Discount)}";
        HasNote = !string.IsNullOrWhiteSpace(order.Note);
        NoteText = order.Note ?? string.Empty;
    }

    [RelayCommand]
    private Task ShowOrdersAsync() => _navigator.ShowOrdersAsync();

    [RelayCommand]
    private Task OpenReceiptAsync() => _order is null ? Task.CompletedTask : _navigator.OpenReceiptAsync(_order);

    [RelayCommand]
    private Task GoHomeAsync() => _navigator.ShowHomeAsync();
}
