using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KantinKita.Services;

namespace KantinKita.ViewModels;

public partial class LoginViewModel(ISessionService session) : ObservableObject
{
    [ObservableProperty] private string displayName = string.Empty;
    [ObservableProperty] private string email = string.Empty;
    [ObservableProperty] private string errorMessage = string.Empty;
    [ObservableProperty] private bool isBusy;

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);
    public bool IsReady => !IsBusy;
    public event EventHandler? Completed;

    partial void OnDisplayNameChanged(string value) => ErrorMessage = string.Empty;
    partial void OnEmailChanged(string value) => ErrorMessage = string.Empty;
    partial void OnErrorMessageChanged(string value) => OnPropertyChanged(nameof(HasError));
    partial void OnIsBusyChanged(bool value) => OnPropertyChanged(nameof(IsReady));

    [RelayCommand]
    private Task ContinueAsync() => RunAsync(async () =>
    {
        if (SessionService.ValidateProfile(DisplayName, Email) is { } error)
        {
            ErrorMessage = error;
            return false;
        }
        await session.StartLocalAsync(DisplayName, Email);
        return true;
    });

    [RelayCommand]
    private Task ContinueAsGuestAsync() => RunAsync(async () =>
    {
        await session.ContinueAsGuestAsync();
        return true;
    });

    private async Task RunAsync(Func<Task<bool>> action)
    {
        if (IsBusy) return;
        IsBusy = true;
        ErrorMessage = string.Empty;
        try
        {
            if (await action()) Completed?.Invoke(this, EventArgs.Empty);
        }
        catch (OperationCanceledException)
        {
            // A future browser sign-in flow may be dismissed by the user.
        }
        catch (HttpRequestException)
        {
            ErrorMessage = "Belum bisa terhubung. Periksa koneksi lalu coba lagi.";
        }
        catch (Exception error) when (error is ArgumentException or IOException or UnauthorizedAccessException)
        {
            ErrorMessage = error is ArgumentException ? error.Message : "Profil belum tersimpan. Coba sekali lagi, ya.";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
