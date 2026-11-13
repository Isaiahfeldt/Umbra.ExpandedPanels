using System.Text.Json;

namespace ExpandedPanels;

public sealed record Slot(string Type, uint Id);

public sealed class PanelData
{
    public static int[] EnabledPages(IEnumerable<string> names) => names.Select((name,index) => (name,index))
        .Where(pair => !string.IsNullOrWhiteSpace(pair.name)).Select(pair => pair.index).ToArray();
    public Dictionary<int, Dictionary<int, Slot>> Pages { get; set; } = new();
    public Slot? Get(int page, int slot) => Pages.GetValueOrDefault(page)?.GetValueOrDefault(slot);
    public void Set(int page, int slot, Slot? value)
    {
        if (page is < 0 or > 3 || slot is < 0 or > 255) throw new ArgumentOutOfRangeException();
        if (!Pages.TryGetValue(page, out var slots)) Pages[page] = slots = new();
        if (value == null) slots.Remove(slot); else slots[slot] = value;
    }
    public string Serialize() => JsonSerializer.Serialize(this);
    public static PanelData Parse(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return new();
        var result = JsonSerializer.Deserialize<PanelData>(value) ?? throw new FormatException("Empty panel data");
        if (result.Pages == null || result.Pages.Any(p => p.Key is < 0 or > 3 || p.Value == null ||
            p.Value.Any(s => s.Key is < 0 or > 255 || s.Value == null || string.IsNullOrWhiteSpace(s.Value.Type))))
            throw new FormatException("Invalid panel data");
        return result;
    }
    public static Slot? ParseClipboard(string? text)
    {
        if (text == null || !text.StartsWith("SC:")) return null;
        var parts = text[3..].Split('/');
        return parts.Length == 2 && !string.IsNullOrWhiteSpace(parts[0]) && uint.TryParse(parts[1], out var id)
            ? new(parts[0], id) : null;
    }
}
