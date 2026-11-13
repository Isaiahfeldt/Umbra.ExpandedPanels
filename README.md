# Umbra Expanded Panels

An Umbra XIV extension with **Expanded Shortcuts** and **Expanded Emotes** widgets. No Umbra fork required. Initial experimental build: in-game validation is required.

## Install and use

In Umbra Settings → Plugins, install owner `Isaiahfeldt`, repository `Umbra.ExpandedPanels`, once its GitHub release is published. Restart Umbra if requested.

1. Create an auxiliary bar and set its orientation to **Vertical**. Umbra's vertical bars fit their contents; horizontal bars enforce toolbar height. This does not restrict the grid's shape.
2. Add **Expanded Shortcuts** or **Expanded Emotes** to that bar.
3. In the widget settings choose rows and columns (1–16 each), icon scale (50–250%), spacing, and four page names.
4. Right-click a slot to select a shortcut type, search, and assign it. The editor also supports Copy/Paste/Clear. Clipboard slots are compatible with official Shortcuts `SC:` entries.
5. Click slots to use them. Panels remain expanded and pages stay available. Position the bar using Umbra's auxiliary-bar controls.

The shortcut widget uses Umbra's existing providers for supported actions, emotes, mounts, minions, items, macros, crafting recipes, and other official shortcut types. It displays icons, item counts, tooltips and disabled state. The emote widget restricts selection to emotes and includes the game's emote chat-output toggle. Pages and content persist in Umbra's widget settings; separate instances have separate settings. Shrinking or changing grid shape preserves assignments in linear slot order, including temporarily hidden slots. Existing official widgets are not automatically imported or altered.

## Validation and limits

Built against Umbra 3.1.18 and Dalamud API 15. Automated checks cover slot serialization, four-page capacity, clearing and preservation, malformed data and clipboard parsing. These checks do not simulate the game or prove rendered layout correctness.

In-game acceptance checks:
- Add both widgets to separate vertical auxiliary bars. Verify layout, movement, clicking elsewhere, and visibility rules.
- Test rows/columns at small and large values, scaling, page names and switching. Reduce and restore capacity, restart Umbra and verify assignments and active page.
- Assign and invoke emotes, mounts, macros and items; check unavailable actions, counts, tooltips, copy/paste/clear and emote chat output.
- Remove/disable widgets while their editor is open, change profiles and reload Umbra; inspect logs for errors.

Large grids can exceed your screen. Choose dimensions and scale that fit. Use multiple bars for independent panels. Combat skill hotbars, cooldown overlays, keybindings and game-hotbar drag/drop are outside this extension's scope.

## Build

Requires .NET 10, Dalamud API 15 libraries in `DALAMUD_HOME`, and official Umbra release DLLs:

```sh
dotnet build ExpandedPanels/ExpandedPanels.csproj -c Release -p:EnableWindowsTargeting=true -p:UmbraLibPath=/path/to/Umbra/
dotnet run --project Checks
```

Output: `out/Release/Umbra.ExpandedPanels.dll`. Package only the extension DLL/deps JSON and license; Umbra provides its own dependencies.

AGPL-3.0-or-later. Uses the public Umbra widget/provider interfaces; design informed by the official Una-XIV Umbra grids. The original project is https://github.com/una-xiv/umbra.
