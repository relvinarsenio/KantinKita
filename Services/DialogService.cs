using KantinKita.Views.Dialogs;

namespace KantinKita.Services;

public interface IDialogService
{
    Task AlertAsync(string title, string message);
    Task<bool> ConfirmAsync(string title, string message, string accept = "Ya", string cancel = "Batal");
    Task<string?> ChooseAsync(string title, IReadOnlyList<DialogChoice> choices, string? selectedId = null, string? message = null);
    Task<string?> ChooseCategoryAsync(string title, IReadOnlyList<DialogChoice> choices, string? selectedId = null, string? message = null) =>
        ChooseAsync(title, choices, selectedId, message);
    Task<string?> ChooseVisualAsync(VisualChoiceRequest request) =>
        ChooseAsync(request.Title, request.Choices.Where(choice => choice.IsEnabled).ToArray(), request.SelectedId, request.Message);
}

public sealed class DialogService : IDialogService
{
    private readonly SemaphoreSlim _gate = new(1, 1);

    public async Task AlertAsync(string title, string message) =>
        _ = await ShowAsync(new DialogRequest(title, message, "Mengerti", null, null, null));

    public async Task<bool> ConfirmAsync(string title, string message, string accept = "Ya", string cancel = "Batal") =>
        await ShowAsync(new DialogRequest(title, message, accept, cancel, null, null)) == AppDialogView.Accepted;

    public Task<string?> ChooseAsync(string title, IReadOnlyList<DialogChoice> choices, string? selectedId = null, string? message = null)
    {
        ArgumentNullException.ThrowIfNull(choices);
        return choices.Count == 0 ? Task.FromResult<string?>(null) :
            ShowAsync(new DialogRequest(title, message, null, "Tutup", choices.ToArray(), selectedId));
    }

    public Task<string?> ChooseVisualAsync(VisualChoiceRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return request.Choices.Count == 0 ? Task.FromResult<string?>(null) :
            ShowAsync(new DialogRequest(request.Title, request.Message, null, "Batal", request.Choices.ToArray(), request.SelectedId, request));
    }

    public Task<string?> ChooseCategoryAsync(string title, IReadOnlyList<DialogChoice> choices, string? selectedId = null, string? message = null) =>
        choices.Count == 0 ? Task.FromResult<string?>(null) :
            ShowAsync(new DialogRequest(title, message, null, "Tutup", choices.ToArray(), selectedId, CompactGrid: true));

    private async Task<string?> ShowAsync(DialogRequest request)
    {
        await _gate.WaitAsync();
        try { return await MainThread.InvokeOnMainThreadAsync(() => AppDialogHost.ShowAsync(request)); }
        finally { _gate.Release(); }
    }
}

public interface IAppSettings
{
    bool HasThemePreference { get; }
    bool IsDarkMode { get; set; }
}

public sealed class AppSettings(IPreferences preferences) : IAppSettings
{
    private const string ThemeKey = "settings.darkMode";

    public bool HasThemePreference => preferences.ContainsKey(ThemeKey);

    public bool IsDarkMode
    {
        get => preferences.Get(ThemeKey, false);
        set => preferences.Set(ThemeKey, value);
    }
}
