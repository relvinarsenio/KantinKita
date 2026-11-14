namespace KantinKita.Services;

public sealed record SignInRequest(string DisplayName, string Email);
public sealed record SignInIdentity(string DisplayName, string Email);

/// <summary>
/// Replace this provider when an authentication backend is available. Only return
/// after successful authentication; keep server tokens out of profile preferences.
/// A provider can launch an OAuth browser flow or an app-specific sign-in flow.
/// </summary>
public interface ISignInProvider
{
    Task<SignInIdentity> SignInAsync(SignInRequest request, CancellationToken cancellationToken = default);
}

/// <summary>Creates an unverified profile on this device. It does not authenticate an account.</summary>
public sealed class LocalSignInProvider : ISignInProvider
{
    public Task<SignInIdentity> SignInAsync(SignInRequest request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (SessionService.ValidateProfile(request.DisplayName, request.Email) is { } error)
            throw new ArgumentException(error);

        return Task.FromResult(new SignInIdentity(request.DisplayName.Trim(), request.Email.Trim()));
    }
}
