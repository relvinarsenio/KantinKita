using KantinKita.Models;
using KantinKita.Views;
using Microsoft.Extensions.DependencyInjection;

namespace KantinKita.Services;

/// <summary>
/// App level navigation intents. View models never touch pages directly, which keeps
/// them testable and keeps page construction in the DI container.
/// </summary>
public interface IAppNavigator
{
    Task OpenItemAsync(CanteenMenuItem item);
    Task OpenCartAsync();
    Task OpenCheckoutAsync();
    Task OpenOrderSuccessAsync(Order order);
    Task OpenReceiptAsync(Order order);
    Task CloseAsync();
    Task ShowOrdersAsync();
    Task ShowHomeAsync();
    Task ShowMenuAsync();
}

public sealed class AppNavigator : IAppNavigator
{
    private readonly IServiceProvider _services;
    private readonly SemaphoreSlim _navigationGate = new(1, 1);
    private readonly HashSet<string> _pendingIntents = [];

    public AppNavigator(IServiceProvider services) => _services = services;

    private static INavigation? Navigation =>
        Microsoft.Maui.Controls.Application.Current?.Windows.FirstOrDefault()?.Page?.Navigation;

    public Task OpenItemAsync(CanteenMenuItem item) => RunAsync("item", async navigation =>
    {
        var page = _services.GetRequiredService<ItemDetailPage>();
        page.Prepare(item);
        await navigation.PushAsync(page);
    });

    public Task OpenCartAsync() => RunAsync("cart", async navigation =>
        await navigation.PushAsync(_services.GetRequiredService<CartPage>()));

    public Task OpenCheckoutAsync() => RunAsync("checkout", async navigation =>
        await navigation.PushAsync(_services.GetRequiredService<CheckoutPage>()));

    public Task OpenOrderSuccessAsync(Order order) => RunAsync($"receipt:{order.Code}", async navigation =>
    {
        await navigation.PopToRootAsync(false);
        var page = _services.GetRequiredService<OrderSuccessPage>();
        page.Prepare(order);
        await navigation.PushAsync(page, false);
    }, requireOrigin: false);

    public Task OpenReceiptAsync(Order order) => RunAsync($"order-receipt:{order.Code}", async navigation =>
    {
        var page = _services.GetRequiredService<ReceiptPage>();
        page.Prepare(order);
        await navigation.PushAsync(page);
    });

    public Task CloseAsync() => RunAsync("back", async navigation =>
    {
        if (navigation.NavigationStack.Count > 1)
        {
            await navigation.PopAsync();
        }
    });

    public Task ShowOrdersAsync() => ShowTabAsync(2);

    public Task ShowHomeAsync() => ShowTabAsync(0);

    public Task ShowMenuAsync() => ShowTabAsync(1);

    private Task ShowTabAsync(int index) => RunAsync($"tab:{index}", async navigation =>
    {
        await navigation.PopToRootAsync(false);
        if (navigation.NavigationStack.FirstOrDefault() is MainTabsPage tabs)
        {
            tabs.SelectTab(index);
        }
    });

    private async Task RunAsync(string intent, Func<INavigation, Task> action, bool requireOrigin = true)
    {
        Page? origin = null;
        var accepted = false;
        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            origin = Navigation?.NavigationStack.LastOrDefault();
            // Coalesce repeated taps, including taps still delivered to an outgoing
            // view after PushAsync has already put the destination on the stack.
            accepted = origin is not null && _pendingIntents.Add(intent);
        });
        if (!accepted)
        {
            return;
        }

        try
        {
            // A valid action on the incoming page (for example Add then Close) must
            // wait for its entrance, rather than disappear while that push is busy.
            await _navigationGate.WaitAsync();
            try
            {
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    if (Navigation is not { } navigation)
                    {
                        return;
                    }

                    if (requireOrigin && !ReferenceEquals(origin, navigation.NavigationStack.LastOrDefault()))
                    {
                        return;
                    }

                    // A committed order always needs its receipt, even if the user
                    // left checkout while another native transition was finishing.
                    await action(navigation);
                });
            }
            finally
            {
                _navigationGate.Release();
            }
        }
        finally
        {
            await MainThread.InvokeOnMainThreadAsync(() => { _pendingIntents.Remove(intent); });
        }
    }
}

