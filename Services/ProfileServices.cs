using KantinKita.Models;

namespace KantinKita.Services;

public interface IFavoriteService
{
    int Count { get; }
    event EventHandler? Changed;

    bool IsFavorite(string itemId);
    void Toggle(string itemId);
}

public sealed class FavoriteService : IFavoriteService
{
    private const string Key = "favorites.items";
    private readonly IPreferences _preferences;
    private readonly HashSet<string> _ids;

    public FavoriteService(IPreferences preferences)
    {
        _preferences = preferences;
        _ids = (preferences.Get(Key, string.Empty) ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .ToHashSet(StringComparer.Ordinal);
    }

    public int Count => _ids.Count;

    public event EventHandler? Changed;

    public bool IsFavorite(string itemId) => _ids.Contains(itemId);

    public void Toggle(string itemId)
    {
        if (!_ids.Remove(itemId))
        {
            _ids.Add(itemId);
        }

        _preferences.Set(Key, string.Join(",", _ids));
        Changed?.Invoke(this, EventArgs.Empty);
    }
}

public interface IProfileService
{
    StudentProfile Profile { get; }
    decimal Balance { get; }
    event EventHandler? Changed;

    bool TryDebit(decimal amount);
    void TopUp(decimal amount);
    void RecordOrder(decimal total);
}

public sealed class ProfileService : IProfileService
{
    private const string BalanceKey = "profile.balance";
    private const string OrdersKey = "profile.orders";
    private const string PointsKey = "profile.points";

    private readonly IPreferences _preferences;
    private readonly StudentProfile _profile;

    public ProfileService(IPreferences preferences, ISessionService session)
    {
        _preferences = preferences;
        _profile = new StudentProfile
        {
            Name = "Alya Rahmawati",
            Nim = "2210512078",
            Program = "Sistem Informasi · 2022",
            Campus = "Kampus Utama · Gedung B",
            Initials = "AR",
            Balance = (decimal)preferences.Get(BalanceKey, 87500d),
            OrderCount = preferences.Get(OrdersKey, 24),
            Points = preferences.Get(PointsKey, 320),
        };
        UpdateIdentity(session);
        session.Changed += (_, _) => { UpdateIdentity(session); Changed?.Invoke(this, EventArgs.Empty); };
    }

    private void UpdateIdentity(ISessionService session)
    {
        _profile.Name = session.HasSession ? session.DisplayName : "Tamu";
        _profile.Initials = string.Concat(_profile.Name.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Take(2).Select(word => word[0])).ToUpperInvariant();
    }

    public StudentProfile Profile => _profile;

    public decimal Balance => _profile.Balance;

    public event EventHandler? Changed;

    public bool TryDebit(decimal amount)
    {
        if (amount <= 0)
        {
            return true;
        }

        if (_profile.Balance < amount)
        {
            return false;
        }

        _profile.Balance -= amount;
        Persist();
        return true;
    }

    public void TopUp(decimal amount)
    {
        if (amount <= 0)
        {
            return;
        }

        _profile.Balance += amount;
        Persist();
    }

    public void RecordOrder(decimal total)
    {
        _profile.OrderCount++;
        _profile.Points += (int)Math.Floor(total / 1000m);
        Persist();
    }

    private void Persist()
    {
        _preferences.Set(BalanceKey, (double)_profile.Balance);
        _preferences.Set(OrdersKey, _profile.OrderCount);
        _preferences.Set(PointsKey, _profile.Points);
        Changed?.Invoke(this, EventArgs.Empty);
    }
}

public sealed record VoucherSuggestion(Voucher Voucher, bool IsEligible, string Status);

public interface IVoucherService
{
    IReadOnlyList<Voucher> All { get; }
    VoucherEvaluation Evaluate(Voucher? voucher, decimal subtotal);
    IReadOnlyList<VoucherSuggestion> Suggest(decimal subtotal);
}

public sealed class VoucherService : IVoucherService
{
    private readonly IReadOnlyList<Voucher> _vouchers = Data.SeedData.Vouchers;

    public IReadOnlyList<Voucher> All => _vouchers;

    public VoucherEvaluation Evaluate(Voucher? voucher, decimal subtotal)
    {
        if (voucher is null)
        {
            return new VoucherEvaluation(0m, null);
        }

        if (subtotal < voucher.MinSubtotal)
        {
            return new VoucherEvaluation(0m, $"Minimal belanja {Helpers.Money.Rupiah(voucher.MinSubtotal)}");
        }

        var discount = voucher.FlatDiscount > 0
            ? voucher.FlatDiscount
            : Math.Round(subtotal * voucher.Percent / 100m, 0, MidpointRounding.AwayFromZero);

        if (voucher.MaxDiscount != decimal.MaxValue)
        {
            discount = Math.Min(discount, voucher.MaxDiscount);
        }

        return new VoucherEvaluation(Math.Min(discount, subtotal), null);
    }

    public IReadOnlyList<VoucherSuggestion> Suggest(decimal subtotal) =>
        _vouchers.Select(voucher =>
        {
            var evaluation = Evaluate(voucher, subtotal);
            return new VoucherSuggestion(
                voucher,
                evaluation.IsValid,
                evaluation.IsValid ? $"Hemat {Helpers.Money.Rupiah(evaluation.Discount)}" : evaluation.Error!);
        }).ToArray();
}
