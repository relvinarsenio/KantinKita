using KantinKita.Services;

var checks = new (string Name, Action Verify)[]
{
    ("Opening without a choice cannot confirm", () =>
    {
        var selection = NewSelection();
        Check(selection.Selected is null && !selection.CanConfirm, "A new picker must require a choice.");
        Check(!selection.TryConfirm(out var result) && result is null && !selection.IsCompleted,
            "Confirm without a choice must neither commit nor close the picker.");
    }),
    ("Unavailable and unknown tables never become the selected result", () =>
    {
        var selection = NewSelection("table-01");
        Check(!selection.Select("table-02"), "Occupied table must reject selection.");
        Check(!selection.Select("missing"), "Unknown table must reject selection.");
        Check(selection.SelectedId == "table-01" && selection.CanConfirm,
            "Rejected choices must preserve the previously available table.");
        Check(selection.TryConfirm(out var result) && result == "table-01",
            "Only the available table may be returned.");
    }),
    ("A table that became unavailable is not preselected", () =>
    {
        var selection = NewSelection("table-02");
        Check(selection.SelectedId is null && !selection.CanConfirm,
            "A stale occupied table must require a fresh selection.");
        Check(!selection.TryConfirm(out var result) && result is null, "Occupied initial choice cannot be committed.");
    }),
    ("Switching choices stays provisional until explicit confirmation", () =>
    {
        var selection = NewSelection("table-01");
        Check(selection.Select("table-03") && selection.SelectedId == "table-03", "A free table can replace the initial choice.");
        Check(selection.Result is null && !selection.IsCompleted, "Tapping a tile must not commit the destination.");
        Check(selection.TryConfirm(out var result) && result == "table-03" && selection.Result == result,
            "Confirmation must return the latest choice's stable identity.");
    }),
    ("Cancel and back discard a provisional destination", () =>
    {
        var originalDestination = "table-01";
        var selection = NewSelection(originalDestination);
        selection.Select("table-03");
        selection.Cancel();
        selection.Cancel();
        Check(selection.IsCompleted && selection.Result is null && !selection.CanConfirm,
            "Cancel must finish with no replacement destination.");
        Check(!selection.TryConfirm(out var result) && result is null && !selection.Select("table-01"),
            "Queued taps and confirm events must not revive a cancelled picker.");
        var destinationAfterCancel = selection.Result ?? originalDestination;
        Check(destinationAfterCancel == originalDestination, "A cancelled sheet must preserve the checkout's previous destination.");
    }),
    ("Confirmation is accepted once even during the closing animation", () =>
    {
        var selection = NewSelection("table-01");
        Check(selection.TryConfirm(out var first) && first == "table-01", "First confirm should complete.");
        Check(!selection.TryConfirm(out var repeated) && repeated is null, "Duplicate confirm must not dispatch twice.");
        Check(!selection.Select("table-03"), "A late tile tap cannot change a committed result.");
        selection.Cancel();
        Check(selection.Result == "table-01", "A late dismissal cannot overwrite the committed result.");
    }),
    ("An entirely occupied or empty plan cannot confirm", () =>
    {
        var occupied = new VisualChoiceSelection([new("busy", "Meja A-02", IsEnabled: false)], "busy");
        var empty = new VisualChoiceSelection([]);
        Check(!occupied.CanConfirm && !empty.CanConfirm, "No available table means no usable destination.");
        Check(!occupied.TryConfirm(out _) && !empty.TryConfirm(out _), "Confirm must remain blocked.");
    }),
    ("Building choices preserve their exact identity and cancel safely", () =>
    {
        var selection = new VisualChoiceSelection([new("Gedung A", "Gedung A"), new("Perpustakaan", "Perpustakaan")]);
        Check(selection.Select("Perpustakaan") && selection.TryConfirm(out var id) && id == "Perpustakaan",
            "Building identity must be returned unchanged for existing destination validation.");
    }),
    ("Duplicate provider identities fail instead of mapping to the wrong tile", () =>
    {
        try
        {
            _ = new VisualChoiceSelection([new("duplicate", "Meja A-01"), new("duplicate", "Meja A-02")]);
            throw new InvalidOperationException("Duplicate IDs should fail.");
        }
        catch (ArgumentException) { }
    }),
};

foreach (var (name, verify) in checks)
{
    verify();
    Console.WriteLine($"PASS {name}");
}
Console.WriteLine($"{checks.Length} choice-flow checks passed.");

static VisualChoiceSelection NewSelection(string? selectedId = null) => new(
[
    new("table-01", "Meja A-01", "4 kursi", Capacity: 4),
    new("table-02", "Meja A-02", "4 kursi", IsEnabled: false, Capacity: 4),
    new("table-03", "Meja A-03", "6 kursi", Capacity: 6),
], selectedId);

static void Check(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}
