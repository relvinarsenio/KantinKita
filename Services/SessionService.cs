using System.Net.Mail;
using Microsoft.Maui.Storage;

namespace KantinKita.Services;

/// <summary>A local device profile, deliberately separate from server authentication.</summary>
public interface ISessionService
{
    bool HasSession { get; }
    bool IsGuest { get; }
    string DisplayName { get; }
    string Email { get; }
    event EventHandler? Changed;
    Task StartLocalAsync(string displayName, string email);
    Task ContinueAsGuestAsync();
    Task SignOutAsync();
}

public sealed class SessionService(IPreferences preferences, ISignInProvider signInProvider) : ISessionService
{
    private const string ModeKey = "session.local.mode";
    private const string NameKey = "session.local.name";
    private const string EmailKey = "session.local.email";

    public bool HasSession => IsGuest || preferences.Get(ModeKey, string.Empty) == "profile"
        && ValidateProfile(DisplayName, Email) is null;
    public bool IsGuest => preferences.Get(ModeKey, string.Empty) == "guest";
    public string DisplayName => IsGuest ? "Tamu" : preferences.Get(NameKey, string.Empty);
    public string Email => IsGuest ? string.Empty : preferences.Get(EmailKey, string.Empty);
    public event EventHandler? Changed;

    public async Task StartLocalAsync(string displayName, string email)
    {
        displayName = displayName.Trim();
        email = email.Trim();
        if (ValidateProfile(displayName, email) is { } error) throw new ArgumentException(error);

        // A backend provider can complete real authentication before anything is published.
        var identity = await signInProvider.SignInAsync(new SignInRequest(displayName, email));
        if (ValidateProfile(identity.DisplayName, identity.Email) is { } invalidIdentity)
            throw new InvalidDataException(invalidIdentity);

        // Publish the marker last. A partial preference write cannot create a valid session.
        preferences.Remove(ModeKey);
        preferences.Set(NameKey, identity.DisplayName.Trim());
        preferences.Set(EmailKey, identity.Email.Trim());
        preferences.Set(ModeKey, "profile");
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public Task ContinueAsGuestAsync()
    {
        preferences.Remove(ModeKey);
        preferences.Remove(NameKey);
        preferences.Remove(EmailKey);
        preferences.Set(ModeKey, "guest");
        Changed?.Invoke(this, EventArgs.Empty);
        return Task.CompletedTask;
    }

    public Task SignOutAsync()
    {
        preferences.Remove(ModeKey);
        preferences.Remove(NameKey);
        preferences.Remove(EmailKey);
        Changed?.Invoke(this, EventArgs.Empty);
        return Task.CompletedTask;
    }

    public static string? ValidateProfile(string? displayName, string? email)
    {
        var name = displayName?.Trim() ?? string.Empty;
        if (name.Length is < 2 or > 80 || name.Any(char.IsControl))
            return "Isi nama panggilan 2–80 karakter dulu, ya.";

        var address = email?.Trim() ?? string.Empty;
        if (address.Length > 254 || !MailAddress.TryCreate(address, out var parsed)
            || parsed.Address != address || !parsed.Host.Contains('.') || address.Any(char.IsWhiteSpace))
            return "Masukkan alamat email yang valid, misalnya nama@kampus.ac.id.";

        return null;
    }
}
