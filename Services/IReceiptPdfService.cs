using KantinKita.Models;

namespace KantinKita.Services;

/// <summary>Creates a receipt from an order snapshot; sharing remains a separate user action.</summary>
public interface IReceiptPdfService
{
    Task<string> CreateAsync(Order order, OrderStatus status, CancellationToken cancellationToken = default);
}
