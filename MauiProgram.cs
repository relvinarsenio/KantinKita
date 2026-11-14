using KantinKita.Services;
using KantinKita.ViewModels;
using KantinKita.Views;
using KantinKita.Views.Tabs;
using Microsoft.Extensions.Logging;

namespace KantinKita;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
#if DEBUG && ANDROID
        Platforms.ExceptionDiagnostics.Initialize();
#endif
        var builder = MauiApp.CreateBuilder();

        // Fluent 2 Android typography uses the native Roboto family.
        builder.UseMauiApp<App>();

        builder.ConfigureCustomHandlers();
        builder.RegisterServices();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }

    private static void RegisterServices(this MauiAppBuilder builder)
    {
        var services = builder.Services;

        services.AddSingleton(Microsoft.Maui.Storage.Preferences.Default);
        services.AddSingleton<IAppSettings, AppSettings>();
        // Replace only this provider registration to connect a real sign-in backend.
        services.AddSingleton<ISignInProvider, LocalSignInProvider>();
        services.AddSingleton<ISessionService, SessionService>();

        services.AddSingleton<IMenuService, MenuService>();
        services.AddSingleton<ICanteenSelectionService, CanteenSelectionService>();
        services.AddSingleton<ITableAvailabilityProvider, LocalTableAvailabilityProvider>();
        services.AddSingleton<IFulfillmentService, FulfillmentService>();
        services.AddSingleton<ICartStorage, CartStorage>();
        services.AddSingleton<ICartService, CartService>();
        services.AddSingleton<IFavoriteService, FavoriteService>();
        services.AddSingleton<IProfileService, ProfileService>();
        services.AddSingleton<IVoucherService, VoucherService>();
        services.AddSingleton<IOrderService, OrderService>();
        services.AddSingleton<IOrderStorage, OrderStorage>();
        services.AddSingleton<IAppNavigator, AppNavigator>();
        services.AddSingleton<IDialogService, DialogService>();

#if ANDROID
        services.AddSingleton<ISystemChrome, Platforms.AndroidSystemChrome>();
        services.AddSingleton<IReceiptPdfService, Platforms.ReceiptPdfService>();
#endif

        // View models
        services.AddSingleton<MainTabsViewModel>();
        services.AddSingleton<HomeViewModel>();
        services.AddSingleton<HomeNotificationsViewModel>();
        services.AddSingleton<MenuViewModel>();
        services.AddSingleton<OrdersViewModel>();
        services.AddSingleton<ProfileViewModel>();
        services.AddTransient<ItemDetailViewModel>();
        services.AddTransient<CartViewModel>();
        services.AddTransient<CheckoutViewModel>();
        services.AddTransient<OrderSuccessViewModel>();
        services.AddTransient<ReceiptViewModel>();
        services.AddTransient<LoginViewModel>();

        // Views
        services.AddSingleton<MainTabsPage>();
        services.AddSingleton<HomeView>();
        services.AddSingleton<MenuView>();
        services.AddSingleton<OrdersView>();
        services.AddSingleton<ProfileView>();
        services.AddTransient<ItemDetailPage>();
        services.AddTransient<CartPage>();
        services.AddTransient<CheckoutPage>();
        services.AddTransient<OrderSuccessPage>();
        services.AddTransient<ReceiptPage>();
        services.AddTransient<LoginPage>();
    }

    private static void ConfigureCustomHandlers(this MauiAppBuilder builder)
    {
#if ANDROID
        Microsoft.Maui.Handlers.ViewHandler.ViewMapper.AppendToMapping("KantinKita.Motion", (_, view) =>
        {
            if (view is VisualElement element) Helpers.UiMotion.Wire(element);
        });

        // Android EditText draws a platform underline; our inputs live inside rounded
        // borders, so render them borderless instead.
        Microsoft.Maui.Handlers.EntryHandler.Mapper.AppendToMapping("KantinKita.BorderlessEntry", (handler, _) =>
        {
            handler.PlatformView.BackgroundTintList =
                Android.Content.Res.ColorStateList.ValueOf(Android.Graphics.Color.Transparent);
            handler.PlatformView.SetPadding(0, 0, 0, 0);
        });

        Microsoft.Maui.Handlers.EditorHandler.Mapper.AppendToMapping("KantinKita.BorderlessEditor", (handler, _) =>
        {
            handler.PlatformView.BackgroundTintList =
                Android.Content.Res.ColorStateList.ValueOf(Android.Graphics.Color.Transparent);
            handler.PlatformView.SetPadding(0, 0, 0, 0);
        });
#endif
    }
}
