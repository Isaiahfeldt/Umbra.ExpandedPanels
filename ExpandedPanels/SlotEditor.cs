namespace ExpandedPanels;

[Service]
public sealed class SlotEditor(Providers providers)
{
    private GridWidget? widget;
    private int page, slot, providerIndex;
    private bool open;
    private string search = "", error = "";
    private IList<Shortcut> results = new List<Shortcut>();
    private List<AbstractShortcutProvider> choices = new();
    public void Open(GridWidget owner, int pageId, int slotId)
    {
        widget = owner; page = pageId; slot = slotId; open = true; error = ""; search = "";
        choices = providers.All.Where(p => !owner.EmotesOnly || p.ShortcutType == "EM").ToList();
        providerIndex = Math.Max(0, choices.FindIndex(p => p.ShortcutType == owner.GetSlot(page,slot)?.Type));
        Refresh();
    }
    public void CloseFor(GridWidget owner) { if (widget == owner) { widget = null; open = false; } }
    private void Refresh()
    {
        results = new List<Shortcut>(); error = "";
        if (choices.Count == 0) { error = "No compatible Umbra shortcut providers found."; return; }
        var provider = choices[providerIndex];
        if (search.Trim().Length < provider.MinSearchLength) { error = $"Enter at least {provider.MinSearchLength} search characters."; return; }
        try { results = provider.GetShortcuts(search.Trim()); }
        catch (Exception ex) { error = ex.Message; }
    }
    private void Safely(Action action) { try { action(); error = ""; } catch (Exception ex) { error = ex.Message; } }
    [OnDraw]
    private void Draw()
    {
        if (!open || widget == null || widget.IsDisposed) return;
        ImGui.SetNextWindowSize(new Vector2(540, 500), ImGuiCond.FirstUseEver);
        if (ImGui.Begin("Expanded Panels - Edit slot", ref open))
        {
            ImGui.TextUnformatted($"{widget.GetInstanceName()} / Page {page+1} / Slot {slot+1}");
            var existing = widget.GetSlot(page, slot);
            if (existing != null && ImGui.Button("Copy")) ImGui.SetClipboardText($"SC:{existing.Type}/{existing.Id}");
            ImGui.SameLine();
            if (ImGui.Button("Paste")) Safely(() => {
                var value = PanelData.ParseClipboard(ImGui.GetClipboardText()) ?? throw new FormatException("Copy a shortcut slot first.");
                widget.Assign(page,slot,value);
            });
            ImGui.SameLine();
            if (ImGui.Button("Clear")) Safely(() => widget.Assign(page,slot,null));
            if (existing != null && ImGui.Button("Configure shortcut"))
                Safely(() => providers.Get(existing.Type)?.OnConfigureShortcut((byte)page,slot,existing.Id,widget.Id));
            ImGui.Separator();
            if (choices.Count > 0)
            {
                if (ImGui.BeginCombo("Type", choices[providerIndex].ContextMenuEntryName))
                {
                    for (int i = 0; i < choices.Count; i++)
                        if (ImGui.Selectable(choices[i].ContextMenuEntryName, providerIndex == i)) { providerIndex = i; Refresh(); }
                    ImGui.EndCombo();
                }
                if (ImGui.InputText("Search", ref search, 200)) Refresh();
                if (ImGui.Button("Refresh available shortcuts")) Refresh();
                ImGui.TextUnformatted($"{results.Count} results");
                if (ImGui.BeginChild("Results", new Vector2(0, -45)))
                {
                    foreach (var item in results.Take(300))
                    {
                        if (ImGui.Selectable($"{item.Name}##{item.Id}"))
                        {
                            var entry = new Slot(choices[providerIndex].ShortcutType,item.Id);
                            Safely(() => widget.Assign(page,slot,entry));
                            if (error.Length == 0) open = false;
                        }
                        if (ImGui.IsItemHovered() && !string.IsNullOrEmpty(item.Description)) ImGui.SetTooltip(item.Description);
                    }
                    if (results.Count > 300) ImGui.TextUnformatted("Refine the search to see more results.");
                }
                ImGui.EndChild();
            }
            if (!string.IsNullOrEmpty(error)) ImGui.TextWrapped(error);
        }
        ImGui.End();
    }
}
