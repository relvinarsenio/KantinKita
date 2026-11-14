using KantinKita.Services;
using KantinKita.Views;
using Microsoft.Extensions.DependencyInjection;

namespace KantinKita;

public partial class App : Application
{
    private readonly IServiceProvider _services;
    private readonly ISystemChrome _chrome;

    public App(IServiceProvider services, IAppSettings settings, ISystemChrome chrome)
    {
        InitializeComponent();

        _services = services;
        _chrome = chrome;

        UserAppTheme = settings.HasThemePreference
            ? settings.IsDarkMode ? AppTheme.Dark : AppTheme.Light
            : AppTheme.Unspecified;

        RequestedThemeChanged += (_, _) =>
            MainThread.BeginInvokeOnMainThread(_chrome.Apply);
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var cart = _services.GetRequiredService<ICartService>();
        _ = cart.InitializeAsync();
        _ = _services.GetRequiredService<IOrderService>().InitializeAsync();

        var session = _services.GetRequiredService<ISessionService>();
        NavigationPage? mainNavigation = null;
        LoginPage? login = null;
        var window = new Window { Title = "Kantin Kita" };

        void ShowMain()
        {
            if (login is not null) login.SessionStarted -= OnSessionStarted;
            login = null;
            mainNavigation ??= new NavigationPage(_services.GetRequiredService<MainTabsPage>())
            {
                BarBackgroundColor = Colors.Transparent,
            };
            window.Page = mainNavigation;
            _services.GetRequiredService<MainTabsPage>().SelectTab(0);
            _chrome.Apply();
        }

        void OnSessionStarted(object? sender, EventArgs args) => ShowMain();

        void ShowLogin()
        {
            if (login is not null) return;
            login = _services.GetRequiredService<LoginPage>();
            login.SessionStarted += OnSessionStarted;
            window.Page = new NavigationPage(login) { BarBackgroundColor = Colors.Transparent };
            _chrome.Apply();
        }

        void OnSessionChanged(object? sender, EventArgs args)
        {
            if (!session.HasSession) MainThread.BeginInvokeOnMainThread(ShowLogin);
        }

        session.Changed += OnSessionChanged;
        window.Destroying += (_, _) => session.Changed -= OnSessionChanged;
        if (session.HasSession) ShowMain();
        else ShowLogin();

        // MAUI paints the window after CreateWindow, so (re)apply the system chrome once
        // the window is actually on screen.
        window.Created += (_, _) => _chrome.Apply();
        window.Activated += (_, _) => _chrome.Apply();
        window.Resumed += (_, _) => _chrome.Apply();

        MainThread.BeginInvokeOnMainThread(_chrome.Apply);
        Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(600), _chrome.Apply);

        return window;
    }
}
