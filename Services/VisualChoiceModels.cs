namespace KantinKita.Services;

public sealed record DialogChoice(string Id, string Title, string? Description = null, string? Glyph = null,
    bool IsEnabled = true, string? Badge = null, int Capacity = 0);

public enum VisualChoiceLayout { Tables, Buildings }

public sealed record VisualChoiceRequest(string Title, IReadOnlyList<DialogChoice> Choices,
    VisualChoiceLayout Layout, string? SelectedId = null, string? Message = null,
    string? Footnote = null, string ConfirmText = "Gunakan pilihan");

/// <summary>Selection stays provisional until the sheet's explicit confirm action.</summary>
public sealed class VisualChoiceSelection
{
    private readonly IReadOnlyDictionary<string, DialogChoice> _choices;
    public VisualChoiceSelection(IReadOnlyList<DialogChoice> choices, string? selectedId = null)
    {
        ArgumentNullException.ThrowIfNull(choices);
        _choices = choices.ToDictionary(choice => choice.Id, StringComparer.Ordinal);
        if (selectedId is not null) Select(selectedId);
    }
    public string? SelectedId { get; private set; }
    public DialogChoice? Selected => SelectedId is not null ? _choices[SelectedId] : null;
    public bool IsCompleted { get; private set; }
    public string? Result { get; private set; }
    public bool CanConfirm => !IsCompleted && Selected is { IsEnabled: true };
    public bool Select(string id)
    {
        if (IsCompleted || !_choices.TryGetValue(id, out var choice) || !choice.IsEnabled) return false;
        SelectedId = id;
        return true;
    }

    public bool TryConfirm(out string? result)
    {
        result = null;
        if (!CanConfirm) return false;
        IsCompleted = true;
        Result = result = SelectedId;
        return true;
    }

    public void Cancel()
    {
        if (IsCompleted) return;
        IsCompleted = true;
        Result = null;
    }
}
