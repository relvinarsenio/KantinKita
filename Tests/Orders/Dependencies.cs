using KantinKita.Models;

namespace KantinKita.Services;

// App navigation/dialog are the only replaced collaborators; orders, storage,
// JSON source generator and receipt view model are the production implementations.
public interface IAppNavigator { Task CloseAsync(); }
public interface IDialogService { Task AlertAsync(string title, string message); }
internal sealed class ReceiptDependencies : IAppNavigator, IDialogService, IReceiptPdfService
{
    public Task CloseAsync() => Task.CompletedTask;
    public Task AlertAsync(string title, string message) => Task.CompletedTask;
    public Task<string> CreateAsync(Order order, OrderStatus status, CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException("The unit test must not launch platform sharing.");
}
