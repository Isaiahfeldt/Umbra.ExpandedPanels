using Dalamud.Game.ClientState.Conditions;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using Lumina.Excel.Sheets;

namespace ExpandedPanels;

// Keep Umbra's normal emotes unchanged; only commandless rows use native execution.
internal sealed class NativeEmoteProvider(AbstractShortcutProvider original) : AbstractShortcutProvider
{
    public override string ShortcutType => original.ShortcutType;
    public override string PickerWindowTitle => original.PickerWindowTitle;
    public override string ContextMenuEntryName => original.ContextMenuEntryName;
    public override uint? ContextMenuEntryIcon => original.ContextMenuEntryIcon;
    public override int ContextMenuEntryOrder => original.ContextMenuEntryOrder;
    public override uint MinSearchLength => original.MinSearchLength;
    private static Emote? Row(uint id) => id is > 0 and <= ushort.MaxValue
        ? Framework.Service<IDataManager>().GetExcelSheet<Emote>().FindRow(id) : null;
    private static bool Commandless(Emote row) => row.TextCommand.RowId == 0
        || string.IsNullOrWhiteSpace(row.TextCommand.ValueNullable?.Command.ExtractText());
    private static bool Ready()
    {
        var player = Framework.Service<IObjectTable>().LocalPlayer;
        var condition = Framework.Service<ICondition>();
        return player != null && player.CurrentHp > 0
            && !condition[ConditionFlag.BetweenAreas] && !condition[ConditionFlag.BetweenAreas51]
            && !condition[ConditionFlag.WatchingCutscene] && !condition[ConditionFlag.WatchingCutscene78];
    }
    private static uint Icon(Emote row)
    {
        if (row.Icon != 0) return row.Icon;
        // A visual fallback only. The selected emote ID is never remapped.
        return 246256; // Sit icon for hidden entries lacking artwork.
    }
    public override IList<Shortcut> GetShortcuts(string? searchFilter)
    {
        var result = original.GetShortcuts(searchFilter);
        // These already exist on the user's native hotbar; expose their exact IDs for assignment.
        foreach (uint id in new uint[] { 88, 147 })
        {
            var row = Row(id);
            if (row == null || result.Any(s => s.Id == id)) continue;
            var name = row.Value.Name.ExtractText();
            if (!string.IsNullOrWhiteSpace(searchFilter) && !name.Contains(searchFilter, StringComparison.OrdinalIgnoreCase)) continue;
            result.Add(new Shortcut { Id = id, Name = name, IconId = Icon(row.Value), Description = $"Native emote #{id} (no chat command)" });
        }
        // Also repair missing icons in entries the official picker already returned.
        for (int i = 0; i < result.Count; i++)
        {
            var item = result[i];
            if (item.IconId == 0 && Row(item.Id) is { } row) { item.IconId = Icon(row); result[i] = item; }
        }
        return result;
    }
    public override unsafe Shortcut? GetShortcut(uint id, string widgetInstanceId)
    {
        var row = Row(id);
        if (row == null || !Commandless(row.Value)) return original.GetShortcut(id, widgetInstanceId);
        var agent = Ready() ? AgentEmote.Instance() : null;
        return new Shortcut { Id = id, Name = row.Value.Name.ExtractText(), IconId = Icon(row.Value),
            IsDisabled = agent == null || !agent->CanUseEmote((ushort)id) };
    }
    public override unsafe void OnInvokeShortcut(byte categoryId, int slotId, uint id, string widgetInstanceId)
    {
        var row = Row(id);
        if (row == null) return;
        if (!Commandless(row.Value)) { original.OnInvokeShortcut(categoryId, slotId, id, widgetInstanceId); return; }
        if (!Ready()) return;
        var agent = AgentEmote.Instance();
        if (agent == null || !agent->CanUseEmote((ushort)id)) return;
        agent->ExecuteEmote((ushort)id);
    }
}
