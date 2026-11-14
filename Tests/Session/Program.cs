using KantinKita.Services;
using KantinKita.ViewModels;
using Microsoft.Maui.Storage;

var tests = new (string Name, Func<Task> Run)[]
{
    ("A new install requires a local profile", () =>
    {
        True(!NewSession().HasSession);
        return Task.CompletedTask;
    }),
    ("Local profile survives service recreation without credentials", async () =>
    {
        var preferences = new MemoryPreferences();
        var session = NewSession(preferences);
        await session.StartLocalAsync("  Nara  ", "  nara@kampus.ac.id  ");
        var restored = NewSession(preferences);
        True(restored.HasSession && !restored.IsGuest);
        Equal("Nara", restored.DisplayName);
        Equal("nara@kampus.ac.id", restored.Email);
        Equal(3, preferences.Values.Count);
    }),
    ("Invalid names and email addresses never create a session", async () =>
    {
        foreach (var (name, email) in new[] { (" ", "a@b.id"), ("Nara", "bad email"), ("Nara", "Name <a@b.id>"), ("Nara", "a@localhost"), ("N\nara", "a@b.id") })
        {
            var session = NewSession();
            await Throws<ArgumentException>(() => session.StartLocalAsync(name, email));
            True(!session.HasSession);
        }
    }),
    ("Failed backend provider cannot replace an existing profile", async () =>
    {
        var preferences = new MemoryPreferences();
        await NewSession(preferences).StartLocalAsync("Nara", "nara@kampus.ac.id");
        var failing = new SessionService(preferences, new FailingProvider());
        await Throws<IOException>(() => failing.StartLocalAsync("Rani", "rani@kampus.ac.id"));
        True(failing.HasSession);
        Equal("Nara", failing.DisplayName);
    }),
    ("Guest mode clears prior identity and logout preserves transaction data", async () =>
    {
        var preferences = new MemoryPreferences();
        preferences.Set("profile.balance", 50000d);
        preferences.Set("orders.existing", "KT-EXISTING");
        var session = NewSession(preferences);
        await session.StartLocalAsync("Nara", "nara@kampus.ac.id");
        await session.ContinueAsGuestAsync();
        True(session.HasSession && session.IsGuest);
        Equal("Tamu", session.DisplayName);
        Equal(string.Empty, session.Email);
        True(!preferences.ContainsKey("session.local.email"));
        await session.SignOutAsync();
        True(!session.HasSession);
        Equal(50000d, preferences.Get("profile.balance", 0d));
        Equal("KT-EXISTING", preferences.Get("orders.existing", ""));
    }),
    ("Login validation is inline and completion requires persisted profile", async () =>
    {
        var session = NewSession();
        var vm = new LoginViewModel(session);
        var completions = 0;
        vm.Completed += (_, _) => { True(session.HasSession); completions++; };
        await vm.ContinueCommand.ExecuteAsync(null);
        True(vm.HasError && !vm.IsBusy);
        Equal(0, completions);
        vm.DisplayName = "Nara";
        vm.Email = "nara@kampus.ac.id";
        True(!vm.HasError);
        await vm.ContinueCommand.ExecuteAsync(null);
        Equal(1, completions);
        True(!vm.HasError && vm.IsReady);
    }),
    ("An asynchronous provider publishes no session before success", async () =>
    {
        var provider = new DeferredProvider();
        var session = new SessionService(new MemoryPreferences(), provider);
        var vm = new LoginViewModel(session) { DisplayName = "Nara", Email = "nara@kampus.ac.id" };
        var submit = vm.ContinueCommand.ExecuteAsync(null);
        True(vm.IsBusy && !session.HasSession);
        await vm.ContinueAsGuestCommand.ExecuteAsync(null);
        True(!session.HasSession);
        provider.Completion.SetResult(new SignInIdentity("Server name", "verified@campus.id"));
        await submit;
        True(session.HasSession && !vm.IsBusy);
        Equal("Server name", session.DisplayName);
    }),
};

foreach (var (name, run) in tests)
{
    await run();
    Console.WriteLine($"PASS {name}");
}
Console.WriteLine($"{tests.Length} local profile regression tests passed.");

static SessionService NewSession(MemoryPreferences? preferences = null) => new(preferences ?? new(), new LocalSignInProvider());
static void True(bool value) { if (!value) throw new InvalidOperationException("Expected true."); }
static void Equal<T>(T expected, T actual)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual)) throw new InvalidOperationException($"Expected {expected}, got {actual}.");
}
static async Task Throws<T>(Func<Task> action) where T : Exception
{
    try { await action(); } catch (T) { return; }
    throw new InvalidOperationException($"Expected {typeof(T).Name}.");
}

sealed class FailingProvider : ISignInProvider
{
    public Task<SignInIdentity> SignInAsync(SignInRequest request, CancellationToken cancellationToken = default) =>
        throw new IOException("Provider unavailable.");
}
sealed class DeferredProvider : ISignInProvider
{
    public TaskCompletionSource<SignInIdentity> Completion { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public Task<SignInIdentity> SignInAsync(SignInRequest request, CancellationToken cancellationToken = default) => Completion.Task;
}
sealed class MemoryPreferences : IPreferences
{
    public Dictionary<string, object?> Values { get; } = [];
    private static string Key(string key, string? sharedName) => $"{sharedName}|{key}";
    public bool ContainsKey(string key, string? sharedName = null) => Values.ContainsKey(Key(key, sharedName));
    public void Remove(string key, string? sharedName = null) => Values.Remove(Key(key, sharedName));
    public void Clear(string? sharedName = null)
    {
        foreach (var key in Values.Keys.Where(key => key.StartsWith($"{sharedName}|", StringComparison.Ordinal)).ToArray()) Values.Remove(key);
    }
    public void Set<T>(string key, T value, string? sharedName = null) => Values[Key(key, sharedName)] = value;
    public T Get<T>(string key, T defaultValue, string? sharedName = null) =>
        Values.TryGetValue(Key(key, sharedName), out var value) && value is T typed ? typed : defaultValue;
}
