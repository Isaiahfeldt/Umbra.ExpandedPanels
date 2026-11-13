namespace ExpandedPanels;

[ToolbarWidget("ExpandedShortcuts", "Expanded Shortcuts", "Always-expanded paged shortcut grid. Use a vertical auxiliary bar.")]
public sealed class ShortcutsWidget(WidgetInfo info, string? guid = null, Dictionary<string, object>? configValues = null)
    : GridWidget(info, guid, configValues, false);

[ToolbarWidget("ExpandedEmotes", "Expanded Emotes", "Always-expanded paged emote grid. Use a vertical auxiliary bar.")]
public sealed class EmotesWidget(WidgetInfo info, string? guid = null, Dictionary<string, object>? configValues = null)
    : GridWidget(info, guid, configValues, true);

public abstract class GridWidget(WidgetInfo info, string? guid, Dictionary<string, object>? configValues, bool emotesOnly)
    : ToolbarWidget(info, guid, configValues)
{
    public bool EmotesOnly => emotesOnly;
    public override WidgetPopup? Popup => null;
    public override Node Node { get; } = new() { Style = new() { Flow = Flow.Vertical, Gap = 4, Padding = new(4) } };
    private PanelData data = new();
    private bool dirty = true;
    private string lastData = "";
    private string? error;
    private long nextRefresh;
    private bool lastVertical;
    private readonly List<(Node Button, Node Icon, Node Count, Node SubIcon, int Index)> slots = new();
    public int Page => GetConfigValue<int>("Page");
    private Providers Providers => Framework.Service<Providers>();
    protected override void Initialize() => dirty = true;
    protected override void OnConfigurationChanged() => dirty = true;
    protected override void OnDisposed() => Framework.Service<SlotEditor>().CloseFor(this);
    public override string GetInstanceName() => GetConfigValue<string>("Title");
    public Slot? GetSlot(int page, int index) => data.Get(page, index);

    public void Assign(int page, int index, Slot? value)
    {
        if (error != null) throw new InvalidOperationException(error);
        if (value != null && (Providers.Get(value.Type) == null || (emotesOnly && value.Type != "EM")))
            throw new InvalidOperationException("This shortcut type is not available in this panel.");
        var previous = data.Get(page, index);
        if (previous == value) return;
        if (previous != null) Providers.Get(previous.Type)?.OnShortcutRemoved((byte)page, index, previous.Id, Id);
        if (value != null) Providers.Get(value.Type)!.OnShortcutAdded((byte)page, index, value.Id, Id);
        data.Set(page, index, value);
        SetConfigValue("Data", data.Serialize());
        dirty = true;
    }

    protected override void OnUpdate()
    {
        if (lastVertical != IsMemberOfVerticalBar) { lastVertical = IsMemberOfVerticalBar; dirty = true; }
        var saved = GetConfigValue<string>("Data");
        if (saved != lastData)
        {
            try { data = PanelData.Parse(saved); error = null; }
            catch (Exception ex) { error = "Panel data could not be loaded; editing is disabled to protect saved slots."; Logger.Warning(ex.Message); }
            lastData = saved;
            dirty = true;
        }
        if (dirty) { Rebuild(); dirty = false; }
        if (Environment.TickCount64 < nextRefresh) return;
        nextRefresh = Environment.TickCount64 + 250;
        foreach (var cell in slots)
        {
            var entry = data.Get(Page, cell.Index);
            Shortcut? shortcut = null;
            try { if (entry != null) shortcut = Providers.Get(entry.Type)?.GetShortcut(entry.Id, Id); }
            catch (Exception ex) { Logger.Warning($"Expanded Panels: {ex.Message}"); }
            cell.Icon.Style.IconId = shortcut?.IconId ?? 0;
            cell.SubIcon.Style.IconId = shortcut?.SubIconId ?? 0;
            cell.Count.NodeValue = shortcut?.Count?.ToString() ?? "";
            cell.Button.Style.Opacity = shortcut?.IsDisabled == true ? 0.45f : entry == null && !GetConfigValue<bool>("Empty") ? 0.1f : 1;
            cell.Button.Tooltip = error ?? (shortcut?.Name ?? (entry == null ? "Right-click to assign a shortcut" : "Shortcut unavailable. Right-click to edit."));
        }
    }

    private void Rebuild()
    {
        foreach (var child in Node.ChildNodes.ToArray()) child.Dispose();
        Node.ChildNodes.Clear(); slots.Clear();
        int size = 48 * GetConfigValue<int>("Scale") / 100;
        int gap = GetConfigValue<int>("Gap");
        int cols = GetConfigValue<int>("Columns"), rows = GetConfigValue<int>("Rows");
        Node.Style.Gap = gap;
        int width = cols * size + (cols - 1) * gap;
        Node.Style.Size = new(width + 8, 0);
        var tabs = new Node { Style = new() { Flow = Flow.Horizontal, Gap = 2 } };
        for (int p = 0; p < 4; p++)
        {
            int page = p;
            var tab = new Node { NodeValue = GetConfigValue<string>($"Page{p}"), Tooltip = $"Switch to page {p+1}",
                Style = new() { Size = new(Math.Max(20, (width - 6) / 4), Math.Max(22, size / 2)),
                    FontSize = Math.Clamp(size / 3, 10, 26), Color = new("Window.Text"),
                    BackgroundColor = new(p == Page ? "Input.BackgroundHover" : "Input.Background") } };
            tab.OnMouseUp += _ => SetConfigValue("Page", page);
            tabs.AppendChild(tab);
        }
        Node.AppendChild(tabs);
        if (!IsMemberOfVerticalBar)
        {
            Node.AppendChild(new Node { NodeValue = "Set this auxiliary bar to Vertical", Style = new() { Color = new("Window.Text"), Size = new(width, 24) } });
            return;
        }
        for (int r = 0; r < rows; r++)
        {
            var row = new Node { Style = new() { Flow = Flow.Horizontal, Gap = gap } };
            for (int c = 0; c < cols; c++)
            {
                int index = r * cols + c;
                var button = new Node { Style = new() { Size = new(size, size), BackgroundColor = new("Input.Background"), BorderRadius = 4 } };
                var icon = new Node { Style = new() { Size = new(size-4, size-4), Anchor = Anchor.MiddleCenter } };
                var sub = new Node { Style = new() { Size = new(size/2, size/2), Anchor = Anchor.TopRight } };
                var count = new Node { Style = new() { Anchor = Anchor.BottomRight, FontSize = Math.Max(10,size/4), Color = new("Window.Text"), OutlineColor = new("Window.TextOutline"), OutlineSize = 1 } };
                button.AppendChild(icon); button.AppendChild(sub); button.AppendChild(count);
                button.OnMouseUp += _ => Invoke(index);
                button.OnRightClick += _ => Framework.Service<SlotEditor>().Open(this, Page, index);
                row.AppendChild(button); slots.Add((button,icon,count,sub,index));
            }
            Node.AppendChild(row);
        }
        if (emotesOnly)
        {
            var game = Framework.Service<IGameConfig>();
            var footer = new Node { NodeValue = game.UiConfig.GetBool("EmoteTextType") ? "Emote chat output: ON" : "Emote chat output: OFF",
                Tooltip = "Click to toggle the game's emote chat output", Style = new() { Size = new(width, 24), Color = new("Window.Text") } };
            footer.OnMouseUp += _ => { game.UiConfig.Set("EmoteTextType", !game.UiConfig.GetBool("EmoteTextType")); dirty = true; };
            Node.AppendChild(footer);
        }
        nextRefresh = 0;
    }

    private void Invoke(int index)
    {
        if (error != null) return;
        var entry = data.Get(Page,index);
        if (entry == null) return;
        try
        {
            var provider = Providers.Get(entry.Type);
            if (provider?.GetShortcut(entry.Id,Id) is { IsDisabled: false })
                provider.OnInvokeShortcut((byte)Page,index,entry.Id,Id);
        }
        catch (Exception ex) { Logger.Warning($"Expanded Panels: action failed: {ex.Message}"); }
    }

    protected override IEnumerable<IWidgetConfigVariable> GetConfigVariables() => new IWidgetConfigVariable[] {
        new StringWidgetConfigVariable("Title", "Panel name", null, emotesOnly ? "Expanded Emotes" : "Expanded Shortcuts"),
        new IntegerWidgetConfigVariable("Columns", "Columns", "Slots across each page", 8, 1, 16),
        new IntegerWidgetConfigVariable("Rows", "Rows", "Slots down each page. Shrinking preserves hidden assignments.", 4, 1, 16),
        new IntegerWidgetConfigVariable("Scale", "Icon scale (%)", "Scales the icons and their clickable slots", 100, 50, 250),
        new IntegerWidgetConfigVariable("Gap", "Slot spacing", null, 4, 0, 20),
        new BooleanWidgetConfigVariable("Empty", "Show empty slots", "Hidden empty slots retain their positions and can still be right-clicked.", true),
        new StringWidgetConfigVariable("Page0", "Page 1 name", null, "Page 1"),
        new StringWidgetConfigVariable("Page1", "Page 2 name", null, "Page 2"),
        new StringWidgetConfigVariable("Page2", "Page 3 name", null, "Page 3"),
        new StringWidgetConfigVariable("Page3", "Page 4 name", null, "Page 4"),
        new IntegerWidgetConfigVariable("Page", "Active page", null, 0, 0, 3) { IsHidden = true },
        new StringWidgetConfigVariable("Data", "Slot data", null, "", 0) { IsHidden = true },
    };
}
