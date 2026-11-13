namespace ExpandedPanels;

[Service]
public sealed class Providers
{
    public List<AbstractShortcutProvider> All { get; } = new();
    public Providers()
    {
        // Use Umbra's public provider contract and service resolver. No private member access.
        foreach (var type in typeof(AbstractShortcutProvider).Assembly.GetTypes()
                     .Where(t => !t.IsAbstract && typeof(AbstractShortcutProvider).IsAssignableFrom(t)))
        {
            try {
                var provider = Framework.Service<AbstractShortcutProvider>(type);
                All.Add(provider.ShortcutType == "EM" ? new NativeEmoteProvider(provider) : provider);
            }
            catch (Exception ex) { Logger.Warning($"Expanded Panels: provider {type.Name} unavailable: {ex.Message}"); }
        }
        All.Sort((a,b) => a.ContextMenuEntryOrder.CompareTo(b.ContextMenuEntryOrder));
    }
    public AbstractShortcutProvider? Get(string type) => All.FirstOrDefault(p => p.ShortcutType == type);
}
