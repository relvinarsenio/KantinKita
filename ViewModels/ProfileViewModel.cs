using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KantinKita.Helpers;
using KantinKita.Services;

namespace KantinKita.ViewModels;

public partial class ProfileViewModel : ObservableObject
{
    private readonly IProfileService _profile;
    private readonly IFavoriteService _favorites;
    private readonly IOrderService _orders;
    private readonly IAppNavigator _navigator;
    private readonly IDialogService _dialogs;
    private readonly IAppSettings _settings;
    private readonly ISessionService _session;

    private bool _isInitializing = true;

    public ProfileViewModel(
        IProfileService profile,
        IFavoriteService favorites,
        IOrderService orders,
        IAppNavigator navigator,
        IDialogService dialogs,
        IAppSettings settings,
        ISessionService session)
    {
        _profile = profile;
        _favorites = favorites;
        _orders = orders;
        _navigator = navigator;
        _dialogs = dialogs;
        _settings = settings;
        _session = session;
        Campus = profile.Profile.Campus;
        VersionText = $"Versi {AppInfo.Current.VersionString} · contoh aplikasi";

        IsDarkMode = settings.HasThemePreference
            ? settings.IsDarkMode
            : Microsoft.Maui.Controls.Application.Current?.RequestedTheme == AppTheme.Dark;

        _isInitializing = false;
        RefreshStats();

        _profile.Changed += (_, _) => RefreshStats();
        _favorites.Changed += (_, _) => RefreshStats();
        _orders.Changed += (_, _) => RefreshStats();
    }

    public string Name => _profile.Profile.Name;
    public string Initials => _profile.Profile.Initials;
    public string Nim => _session.IsGuest ? "Masuk untuk menambahkan identitas" : _session.Email;
    public string Program => _session.IsGuest ? "Mode tamu · perangkat ini" : "Profil lokal · perangkat ini";
    public string Campus { get; }
    public string VersionText { get; }

    [ObservableProperty]
    private string balanceText = string.Empty;

    [ObservableProperty]
    private string pointsText = string.Empty;

    [ObservableProperty]
    private string orderCountText = string.Empty;

    [ObservableProperty]
    private string favoriteCountText = string.Empty;

    [ObservableProperty]
    private bool isDarkMode;

    partial void OnIsDarkModeChanged(bool value)
    {
        if (_isInitializing)
        {
            return;
        }

        _settings.IsDarkMode = value;
        if (Microsoft.Maui.Controls.Application.Current is { } application)
        {
            application.UserAppTheme = value ? AppTheme.Dark : AppTheme.Light;
        }

        Haptics.Tap();
    }

    private void RefreshStats()
    {
        OnPropertyChanged(nameof(Name));
        OnPropertyChanged(nameof(Initials));
        OnPropertyChanged(nameof(Nim));
        OnPropertyChanged(nameof(Program));
        BalanceText = Money.Rupiah(_profile.Balance);
        PointsText = _profile.Profile.Points.ToString("N0", System.Globalization.CultureInfo.GetCultureInfo("id-ID"));
        OrderCountText = _orders.Orders.Count(order => !order.IsLive).ToString("N0", System.Globalization.CultureInfo.GetCultureInfo("id-ID"));
        FavoriteCountText = _favorites.Count.ToString("N0", System.Globalization.CultureInfo.GetCultureInfo("id-ID"));
    }

    [RelayCommand]
    private async Task TopUpAsync()
    {
        var confirmed = await _dialogs.ConfirmAsync(
            "Top up saldo?",
            "Tambahkan Rp50.000 ke saldo kantinmu. Saldo demo akan langsung bertambah.",
            "Top up",
            "Nanti");

        if (!confirmed)
        {
            return;
        }

        _profile.TopUp(50000m);
        Haptics.Tap();
        await _dialogs.AlertAsync("Top up berhasil", $"Saldo kantinmu sekarang {Money.Rupiah(_profile.Balance)}.");
    }

    [RelayCommand]
    private Task OpenOrdersAsync() => _navigator.ShowOrdersAsync();

    [RelayCommand]
    private Task EditProfileAsync() => _dialogs.AlertAsync(
        "Profil mahasiswa",
        $"{Name}\n{Nim}\n\nProfil ini tersimpan di perangkat. Keluar lalu masuk kembali untuk mengganti identitas. Riwayat pesanan tetap tersimpan di perangkat ini.");

    [RelayCommand]
    private Task ShowVouchersAsync() => _dialogs.AlertAsync(
        "Voucher aktif",
        "HEMAT5 — potongan Rp5.000 (min. Rp25.000)\n" +
        "MAHASISWA10 — diskon 10%, maks. Rp15.000 (min. Rp30.000)\n" +
        "NGOPI3 — potongan Rp3.000 (min. Rp15.000)\n\n" +
        "Pakai kode voucher di halaman pembayaran.");

    [RelayCommand]
    private Task ShowHelpAsync() => _dialogs.AlertAsync(
        "Bantuan",
        "Ada kendala pesanan? Hubungi pengelola kantin di Gedung B lantai 1 atau WhatsApp 0812-0000-1234.");

    [RelayCommand]
    private Task ShowAboutAsync() => _dialogs.AlertAsync(
        "Tentang Kantin Kita",
        "Contoh aplikasi kantin kampus yang dibangun dengan .NET MAUI 10, MVVM, dan Shell-less navigation.\n\n" +
        "Semua data bersifat demo dan disimpan lokal di perangkat.");

    [RelayCommand]
    private async Task SignOutAsync()
    {
        var confirmed = await _dialogs.ConfirmAsync(
            "Keluar dari akun?",
            "Sesi profil akan ditutup. Riwayat pesanan dan keranjang tetap tersimpan di perangkat ini.",
            "Keluar",
            "Batal");

        if (confirmed)
        {
            await _session.SignOutAsync();
        }
    }
}
