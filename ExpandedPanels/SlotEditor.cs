using Umbra.Windows;
using ExpandedPanels.NativeUi;
using Menu = ExpandedPanels.NativeUi.ContextMenu;
using Entry = ExpandedPanels.NativeUi.ContextMenuEntry;

namespace ExpandedPanels;

[Service]
public sealed class SlotEditor(Providers providers, NativeUi.ContextMenuManager menus) : IDisposable
{
    private Menu? menu;
    private GridWidget? owner;
    public void Open(GridWidget widget, int page, int slot)
    {
        menus.Close();
        menu?.Dispose();
        owner = widget;
        var value = widget.GetSlot(page, slot);
        var entries = providers.All.Where(p => !widget.EmotesOnly || p.ShortcutType == "EM")
            .Select(p => new Entry(p.ShortcutType) {
                Label = p.ContextMenuEntryName, IconId = p.ContextMenuEntryIcon,
                OnClick = () => Framework.Service<WindowManager>().Present(
                    $"ExpandedPanels.Picker.{widget.Id}", new ShortcutPickerWindow(p), window => {
                        if (widget.IsDisposed || window.PickedId == null) return;
                        var picked = PanelData.ParseClipboard("SC:" + window.PickedId);
                        if (picked != null) Safely(() => widget.Assign(page, slot, picked));
                    }),
            }).ToList();
        entries.Add(new Entry("-"));
        entries.Add(new Entry("Copy") { Label = I18N.Translate("Widget.ShortcutPanel.ContextMenu.CopySlot"), IsDisabled = value == null,
            OnClick = () => { if (value != null) ImGui.SetClipboardText($"SC:{value.Type}/{value.Id}"); } });
        var pasted = PanelData.ParseClipboard(ImGui.GetClipboardText());
        entries.Add(new Entry("Paste") { Label = I18N.Translate("Widget.ShortcutPanel.ContextMenu.PasteSlot"),
            IsDisabled = pasted == null || providers.Get(pasted.Type) == null || (widget.EmotesOnly && pasted.Type != "EM"),
            OnClick = () => { if (!widget.IsDisposed && pasted != null) Safely(() => widget.Assign(page, slot, pasted)); } });
        entries.Add(new Entry("Clear") { Label = I18N.Translate("Widget.ShortcutPanel.ContextMenu.ClearSlot"), IconId = 61502,
            IsDisabled = value == null, OnClick = () => { if (!widget.IsDisposed) Safely(() => widget.Assign(page, slot, null)); } });
        menu = new Menu(entries);
        menu.Present();
    }
    private static void Safely(Action action)
    {
        try { action(); } catch (Exception ex) { Logger.Error($"Expanded Panels: {ex.Message}"); }
    }
    public void CloseFor(GridWidget widget)
    {
        if (owner != widget) return;
        menus.Close();
        owner = null;
    }
    public void Dispose()
    {
        menus.Close();
        menu?.Dispose();
    }
}
