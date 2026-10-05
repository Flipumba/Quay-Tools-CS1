# Quay Tools — Cities: Skylines 1

**Version: v0.6.0**

Quay Tools is a Cities: Skylines 1 mod with additional tools for working with quay segments.

## Features

### UnifiedUI

- Adds a **UnifiedUI button** that activates Quay Tools and opens a small tool window.
- If UnifiedUI is not installed or enabled, a small floating button is created instead.

### Segment Settings

One tool (crossed wrench and screwdriver) for the settings of whole quay segments. Select segments (a click selects one segment, **Shift** + click the whole connected quay, **Ctrl** + click adds or removes segments; right click clears the selection, then exits), then use the rows of the window. Each row has the icon of its function on the left:

- **Invert segment** (green check button): flips the direction of the selected segments; press again to flip back. Network lines, prop lines and texture paths follow the water side when a segment is flipped.
- **Lock orientation** (switch): the game or another mod can no longer flip the selected segments; if it does, the segment is flipped back right after that simulation step. The Invert button and Ctrl+R of Quay Tools still work on a locked segment, and the lock keeps the new orientation.
- **Remove pedestrian path** (switch): citizens do not walk on the selected segments and do not see them as a path (the pathfinder skips them, also with TM:PE). Citizens whose route already crosses one get a new route. Vehicle-only paths are not affected; use it only on pedestrian quays.
- **Hide default props** (switch): hides the props that come with the quay model (lights, trees, benches) on the selected segments. Your own Props-line props stay. Other segments of the same quay type are not affected.

- **Clear segments** (red button above Undo / Redo): removes everything of Quay Tools from the selected segments (network-lines, props-lines, texture-paths, lock, removed pedestrian path, hidden default props). The inversion stays. Undo brings everything back.

Segments with a lock, without a pedestrian path or with hidden props are drawn red while the tool is active. A switch shows "on" when all selected segments have the setting.

#### Quick Flip

Flip a quay without activating the tool:

- **Ctrl + R** over a quay flips the segment.
- **Ctrl + R + Shift** flips the entire connected quay.
- The hotkey can be changed or disabled in the mod options.

### Templates

Saves everything that is drawn along a quay segment and puts it on other segments. Select segments (click, **Shift**, **Ctrl** as above), type a name and press **Save as template**: the network-lines, props-lines (trees included) and texture-paths (models and all values, in their order) of the **first selected segment** are saved, together with a **picture**: a screenshot of the scene without the interface. Choose a template in the drop-down list (with the pictures) and press **Apply to selected**: the lines of all selected segments are **replaced** (no confirmation; **Undo** brings them back, one step for the whole apply).

The **pencil** next to the chosen template opens the template settings (third column): rename, a new screenshot (put the camera first), **Duplicate**, **Delete**, **Open folder**, and the list of the lines of the template, where any line can be removed.

- Start / end trims are saved as a share of the segment length, so a template fits segments of any length; all other values are absolute.
- A model that is not loaded in the game (a missing asset) is reported in the status line and its slot stays empty; the other lines are applied.
- Templates are text files (and a `.png` picture) in the game's local data folder, `QuayTools/Templates` (file name = name + short code, `.qtpl`). They are shared by all cities. A damaged file is skipped and logged. A name that already exists gets " (2)"; the characters `/ \ : * ? " < > |` cannot be typed.

### Settings

The last tool (gear). The options that are used while playing are here: **Quick flip** (the Ctrl + key hotkey over a quay), **Path shadows**, **Mark edited segments**, and the size of the icons above them. **Controls help** opens a third column that lists all the keys and mouse buttons (the tool hints no longer repeat them). The hotkey itself, the language, the swap of land and water, and the Ctrl+Z / Ctrl+Y option stay in the mod options.

**Clear all quays** removes every change of Quay Tools from all segments of the savegame (network-lines, props-lines, texture-paths, locks, removed pedestrian paths, hidden default props) after a confirmation. **Undo** brings everything back; flipped segments stay flipped.

### Network-line

Draws network models (fences, walls, ...) along quay segments. Any number of lines per segment, like Props-line.

1. Select the **Network-line** tool. Select quay segments (orange; click: one segment, **Shift**: whole connected quay, **Ctrl**: add or remove; **right click** clears the selection).
2. Press **+ Add network model**. A line is added to every selected segment, **in the middle of the quay**; use **< >** to switch between the lines of the selection (the line being edited is drawn as a blue stripe).
3. The window has two columns. Left column: the selection and line switch, **Add network model**, the **Network model** drop-down list (preview and name, search field, scrolling, **star** = favourite: favourites are listed first and are kept in the mod settings for all savegames), **Turn the model around**, **Close line at segment start / end**, **Remove this line** / **Remove all lines**, then **Undo / Redo / Reset**. Right column (the values; slider, or type a value in the field next to it, double-click a slider to reset it):
   - **Line start** / **Line end** (0 to −50 m): the line runs from the **middle of the start node to the middle of the end node** (across the node it follows its bend and height curve). The offset moves the start or the end of the line back toward the segment, up to the border of the node and further, without joining it to the neighbouring segment.
   - **Offset Y** (+ toward the water, ±50 m) and **Offset Z** (±50 m).
   - **Model width** (10–500 %). The height of a model cannot be scaled (the game's net shader takes it from the model).
   - **Turn the model around**: the model faces the water by default; with this switch it faces the land. This is a real half turn (the front becomes the back), not a mirror image.
   - **Close line at segment start / end**: a straight piece of the same model across the quay, facing away from the segment (start = cyan ring, end = magenta ring).
4. **Remove this line** / **Remove all lines** delete lines from the selected segments.

Changes apply to the same line number on all selected segments. Where only one of two neighbouring segments has a line, only its half of the node is covered. Lines are placed relative to the water side, so they move with the quay when it is flipped (the model keeps facing the same way relative to the water).

Saves of v0.4.x are converted when loaded: the old **land** model becomes line 1 and the **water** model line 2 (position, height, trims, scale, closing pieces and orientation are kept; the old "mirror" flip is now a real turn-around; "detach" is no longer needed because lines are not joined to their neighbours).

### Props-line

Places props along quay segments at a fixed step (lamps, trees, benches, bollards, ...). Any non-decal prop you have can be used. The props are decoration only.

1. Select the **Props-line** tool. Select quay segments (**Shift**: whole connected quay).
2. Press **+ Add prop line**. A line is added to every selected segment; use **< >** to switch between the lines of the selection.
3. Left column: **Prop** drop-down list (trees are marked `[tree]`; search field and favourites inside the list), **Remove this line** / **Remove all lines**, **Undo / Redo / Reset**. Right column (values), in this order:
   - **Prop step** (0.5–50 m), **Prop scale** (5–1000 %), **Random scale**.
   - **Prop rotation**, **Random rotation** and **Follow the slope** (all hidden for trees: a tree has no direction). *Follow the slope* tilts the prop up and down with the height curve of the quay instead of standing level.
   - **Offset X** (along the line), **Offset Y** (+ toward the water) and **Offset Z** (±50 m).
   - **Line start** / **Line end** (0 to −50 m): the line runs from the **middle of the start node to the middle of the end node** (across the node it follows its bend and height curve), so props also stand on the nodes. The props are counted from the start of the line; trimming one end only removes props and never moves the others.
4. **Remove this line** / **Remove all lines** delete lines from the selected segments.

Changes apply to the same line number on all selected segments. The lines follow the curves and heights of the quay like decal paths do. Where only one of two neighbouring segments has a line, only its half of the node is covered. Trees are drawn through the game's tree renderer (no rotation). Props that have lights (lamps) or day/night illumination are drawn through the game's own prop rendering, so their lights work. Props-line data is saved in the savegame (`QuayTools.PropLines`). If a prop asset is missing when a save is loaded, that line is not drawn (a line in the log says so).

### Texture-path

Lays a ground decal (any decal prop you have: Workshop decals, cobblestones, markings, ...) or a plain coloured strip along the top surface of a quay. Any number of paths per segment.

1. Select the **Texture-path** tool and select quay segments (**Shift**: whole connected quay).
2. Press **Add decal** or **Add plane** (side by side): a path appears in the middle of the quay; use **< >** to switch between the paths of the selection. The way of drawing is chosen when the path is added and stays with it (the button of the current path stays pressed):
   - **Decal**: game decals placed step by step along the path (like the game places decal props), with a projection box.
   - **Plane**: one textured strip cropped by the path edges, instead of placed decals. It lies lower than the quay surface by default (**Offset Z** 1 m), receives shadows when its tint is fully opaque, and is drawn unlit when the tint is translucent. The projection size is not shown for a plane.
3. Left column: **Texture** drop-down list (decal paths always use a decal texture; a plane can also use the first entry, *Colour (no texture)*, which is the game's **theme pavement** texture tinted by the path colour), **Remove this path** / **Remove all paths**, **Undo / Redo / Reset**.
4. Right column (values), in this order: **Path width** (0.1–50 m), **Texture scale** (width of one tile; choosing a decal sets its natural size, *Colour* sets the size of the theme pavement texture), **Texture step**, **Projection size** (decal only), **Offset X** (along the line; props, textures and tiles slide, the ends stay), **Offset Y** (+ toward the water), **Offset Z** (±50 m), **Line start** / **Line end** (0 to −50 m: the path is shortened at that end, like the lines of the other tools), the **tint** (R, G, B, A sliders or a hex value `#RRGGBB` / `#RRGGBBAA`) and **Colour multiply** (0–1: 0 = tint only where the decal allows it, 1 = the colour covers the whole texture, so white or any colour can be painted; works for decals and planes).
5. A plane draws the composed texture of the decal prop (colours from its diffuse map, opacity from its ACI map).

Changes apply to the same path number on all selected segments. The path is built from the two edge curves of the segment, so it follows the curves and heights of the quay, also after node edits made with Node Controller Renewal. Saves of v0.4.x keep their path (the old global rendering option becomes the way of drawing of each path).

Notes:

- A decal path **places decals like the game places decal props**: tiles of the decal prop are laid step by step along the path. This mode has no mask cropping. Decals that need the terrain height map are not offered.
- A decal is stored by the name of its prop. If the asset is missing when a save is loaded, that path is not drawn (a line in the log says so).

### Window

The window opens with the tool column only. Choosing a tool opens the columns it needs: the controls of the tool with the undo / redo / reset bar and the status line, and, for Network-line, Props-line and Texture-path, a third column with the sliders and the **Hide highlight while dragging** switch. Invert shows a second column only while it has a message. The description of a tool is shown when the mouse is over its button. With the switch on, the highlight of the quay borders and lines is hidden only while a slider is dragged.

### Edited segment markers

While the Quay Tools tool is active, every quay segment edited by the mod is highlighted faintly and carries a row of small icons above it, one per applied tool: network-model lines, texture paths, props-line, orientation lock, removed pedestrian path. Icons are drawn for segments within about 800 m of the camera. Can be switched off in the mod options (**While the tool is active, highlight edited segments...**).

### Undo, Redo and Reset

At the bottom of the window in the network-line, texture-path, props-line, lock and pedestrian modes:

- **Undo / Redo** (also **Ctrl+Z**, **Ctrl+Y** or **Ctrl+Shift+Z** while the tool is active): network lines, texture paths, prop lines, locks and resets, up to 100 steps. Dragging a slider counts as one step. The history is kept until the map is left; it is not saved in the savegame.
- **Reset**: sets the values of all lines (network-line mode) or paths (texture-path mode) of the selected segments back to their defaults. Models and decals stay.
- The Ctrl+Z / Ctrl+Y keys can be turned off in the mod options. The history is Quay Tools' own; it is not connected to other undo mods.

#### How it works

- Network-model lines are drawn by Quay Tools itself with the game's own routine for fence pieces (`NetSegment.RenderSegments`, called from a Harmony prefix on `NetManager.EndRenderingImpl`), so shaders, LOD meshes, colours and the terrain height map are the game's. The game's own fence slots of a segment are no longer used.
- The line follows the same curve as a prop line: the two edge curves of the segment (the real node corners, also after node edits) are blended at the line's position, and the line is extended over half of the gap to the neighbouring segment up to the middle of the node.
- Data is saved in the savegame: network lines under `QuayTools.NetLines`, texture paths under `QuayTools.Decals`, prop lines under `QuayTools.PropLines`, locks under `QuayTools.Locks`, removed pedestrian paths under `QuayTools.NoPeds`. Loading a save without the mod simply ignores them.

## Installation

### Compiled mod (from a release)

1. Install and enable the required mod **Harmony (Mod Dependency)** (Steam Workshop ID `2040656402`). See [Dependencies](#dependencies).
2. Copy the folder `QuayTools` (with `QuayTools.dll`, `UnifiedUILib.dll`, `CitiesHarmony.API.dll` and the `Icons` folder, all together) to:

   ```text
   %LOCALAPPDATA%\Colossal Order\Cities_Skylines\Addons\Mods\
   ```

   so that the result is `...\Addons\Mods\QuayTools\QuayTools.dll`.
3. Start the game, open **Content Manager → Mods** and enable **Quay Tools**.
4. Load a map. The tool is opened with the **Quay Tools** button in UnifiedUI (or a small floating button if UnifiedUI is not installed). The default activation hotkey is **Ctrl+Shift+Q**; it can be changed in UnifiedUI.

To update, replace the files in the same folder while the game is closed. To uninstall, disable the mod and delete the folder; savegames still load, the mod's data in them is ignored.

### From the Steam Workshop

Subscribe to Quay Tools; the required Harmony mod is installed automatically as its dependency. Enable both in **Content Manager → Mods**.

## Building

### Requirements

- Visual Studio with .NET tooling, or the `dotnet` CLI.
- A local installation of Cities: Skylines 1.
- The dependencies listed below.

### Build

Open `QuayTools.csproj` in Visual Studio and build in **Release**, or:

```bash
dotnet build -c Release
```

If the game is not in the default Steam directory, set `CS1ManagedPath` in `QuayTools.csproj`.

After a successful build on Windows the files are copied to:

```text
%LOCALAPPDATA%\Colossal Order\Cities_Skylines\Addons\Mods\QuayTools
```

- `QuayTools.dll`
- `UnifiedUILib.dll`
- `CitiesHarmony.API.dll`
- `Icons\`

All of them must stay together in the mod folder. Enable **Quay Tools** in **Content Manager → Mods**.

## Dependencies

### Required

- **Harmony (Mod Dependency)** — Steam Workshop ID `2040656402`. Quay Tools uses `CitiesHarmony.API`; the Harmony library itself comes from this mod. It must be subscribed to and enabled, and added as a required item when publishing Quay Tools to the Steam Workshop.
- **Cities: Skylines** (current version) and quay networks (`QuayAI`) to work on.

### Optional

- **UnifiedUI** — adds the tool button and hotkey to the UnifiedUI toolbar. Without it a small floating button is used. `Lib/UnifiedUILib.dll` is the UnifiedUI helper library (MIT license, © 2022 UnifiedUI), shipped next to the mod DLL so Quay Tools can talk to the UnifiedUI mod. UnifiedUI icons are 64×64 px, light glyphs on a transparent background.
- **Node Controller Renewal (NCR)** — not needed, but supported: network lines, texture paths and prop lines follow nodes edited with it.
- **Decal props** (Workshop or your own) — needed only to have something to choose in the *Texture-path* list; network models for *Network-line* and props for *Props-line* likewise come from the game or from installed assets.

### Build dependencies

Only needed to compile the mod yourself (restored by NuGet, except the game DLLs and `UnifiedUILib.dll`):

- `CitiesHarmony.API` and `CitiesHarmony.Harmony` (NuGet, version 2.x)
- `Microsoft.NETFramework.ReferenceAssemblies` (NuGet)
- `Lib/UnifiedUILib.dll` (included in the repository)
- Game assemblies from `Cities_Data\Managed`: `Assembly-CSharp.dll`, `ICities.dll`, `ColossalManaged.dll`, `UnityEngine.dll` (path set by `CS1ManagedPath` in the project)

## Compatibility and status

- The current version has been tested in-game. Please report problems together with `output_log.txt` (lines starting with `[QuayTools]`).
- Game APIs used: `QuayAI`, `ToolBase` (`OnEnable`, `OnDisable`, `OnToolUpdate`, `RenderOverlay`), `OverlayEffect.DrawBezier`, `NetSegment.CalculateCorner`, `NetSegment.RenderSegments`, `NetManager.EndRenderingImpl`, `PrefabCollection<NetInfo>`, `SerializableDataExtensionBase`, cs UI components (`UISlider`, `UIScrollbar`, ...).
- `ToolInstaller` adds the tool component to the private `m_tools` array via reflection.

## Translating

Translations live in `Locales/<code>.json` (flat key → text). `en.json` is the source file synced with [Crowdin](https://ru.crowdin.com/project/quay-tools-for-cities-skylines). Keep placeholders such as `{0}`, `{act}`, `{flip}` untouched. Missing keys fall back to English; the language list in the options shows only languages whose file exists. `Tools/gen_locales.py` regenerates `en.json`/`ru.json` from the built-in table in `Loc.cs`.

## Changelog

### v0.6.0
- New tool **Segment Settings** (crossed wrench and screwdriver) replaces four tools: **Invert segment**, **Lock segment**, **Remove pedestrian path** and **Hide default props** are now rows of one window with the icon of the function on the left: Invert is a button with a green check, the others are switches.
- All check-box buttons of all tools ("[x] text") are now switches: the text on the left, a switch on the right (bright green = on, red = the selected segments differ).
- New tool **Templates** (button at the bottom of the tool column): save the network-lines, props-lines and texture-paths of a segment as a template with a picture (screenshot without the interface), choose it in a drop-down list and apply it to other segments (replaces their lines, one Undo step). Template settings (pencil): rename, new screenshot, duplicate, delete, open folder, remove single lines.
- The window keeps the same height for all tools; at the first start it appears at the left edge of the screen, in the middle of its height.
- Selection in all tools: a click selects one segment (and drops the previous selection), **Shift** + click the whole connected quay, **Ctrl** + click adds or removes segments.
- New tool **Settings** (gear, last in the tool column): the game-time options moved here from the mod options; **Clear all quays** (with a confirmation, one Undo step).
- The window has a new flat look (dark green-black with a yellow accent, like the preview picture): slightly rounded corners, a thin yellow frame on hover, a yellow flash on click, and the chosen tool shows only its icon with the frame.
- The option "Centre line at bends" was removed: it acted only on texture-paths across sharp bends of nodes of other mods, and nothing was redrawn when it was switched, so it looked broken. The behaviour is always on now.
- Delete questions (template, template line, all modifications) are solid boxes with Delete / Cancel buttons.
- The mod page in the game options is redrawn in the same flat style: header with the mod icon, **What's new** (with a switch for the update window), **Language**, **Hotkeys** (click a field and press the new keys: tool activation, quick flip), **Settings** (all options with short names, full descriptions as tooltips, the new switch **Hide tooltips**) and **Support** (links to Crowdin, GitHub, Boosty).
- The mod page has three tabs: **Main**, **Advanced** (changelog, a compatibility check of the mod, reset of all settings with a confirmation, copy the game log to the desktop) and **Links** (Crowdin, GitHub, Boosty, Steam Workshop page).
- Translations are now loaded from `Locales/*.json` (Crowdin-ready) with English fallback. Added German, French, Spanish, Polish, Italian, Portuguese (Brazil), Ukrainian, Chinese (Simplified), Japanese and Korean (machine-translated first drafts, corrections welcome on Crowdin).
- **What's new** shows only the newest version; **Changelog** (Advanced tab) lists all versions from 0.3.5 as folding cards with Added / Updated / Fixed tags.
- The texts of **What's new** and **Changelog** are translated too (keys `cl_<version>_<n>` in `Locales/*.json`; a missing line falls back to English). For a new version add the lines to `WhatsNew.cs` and run `python3 Tools/gen_locales.py` (it exports them to `en.json` / `ru.json`).
- The quick-flip key can now be any key (with Ctrl / Alt if you like) instead of one of five.

### v0.5.7
- The **What's new** window now lists the changes from v0.5.6 (English and Russian); the options button shows all of them.
- New **What's new** window: after a new version is run for the first time, a short list of changes is shown once when a map is loaded (not on a first installation). The last seen version is kept in the mod settings.
- Colour multiply (Texture-path) rebuilt: the colour mask of the game's decal shader works the other way round from what v0.5.6 assumed (the slider removed the colour instead of adding it). Now 0 is the old behaviour and 1 lets the colour cover the whole texture, so a texture can be painted white. It also works for planes (the slider was disabled for them).
- Fixed: after flipping a segment with **Invert segment**, Line start / Line end (and Offset X) of its lines worked on the opposite end compared with a neighbouring segment. They are now given along a direction that depends on the water side (with the water on the right of start → end it is start → end, otherwise end → start), so the same value moves every segment of a quay the same way. Saved values of flipped segments now act on the other end than before.
- Window: the description of a tool is now the tooltip of its button instead of a block in the second column. Empty columns are closed: the window opens with the tool column only, choosing a tool opens one or two more columns (Invert opens one only while it shows a message). The tool is chosen by a click: nothing is highlighted or flipped until then.
- Props-line: props with lights (street lamps) now have their light sources and night illumination: they are drawn through the game's own prop rendering. If that is not available, they are drawn without lights as before.
- Props-line: new **Follow the slope** switch: the prop tilts with the height curve of the quay.
- Props-line / Texture-path: the tile grid and the props start at the canonical start of the segment (water on the right), the way "Line start / end" and "Offset X" already worked. Before, they were counted from the physical start, so a flipped segment did not continue its neighbour: props faced the other way, doubled or left gaps at the node, and texture tiles did not line up. A prop that falls exactly on the end of a segment is no longer placed twice at a node.
- The floating tool icons above edited segments are no longer drawn over the tool window.
- Window: the block with the delete buttons and Undo / Redo / Reset is always at the very bottom of the window.
- Window: the lower edge of the Undo / Redo / Reset block is level with the lower edge of the "Hide highlight" switch; the message about undone actions is above the block.
- Props-line: a prop with a LOD (distant model) now turns with the main model; before only the near model was turned (the angle is passed the way the game does it).
- Window: the title is on two lines when only the tool column is shown; the "Hide highlight while dragging" switch is created on top of the panels again.

### v0.5.6
- Interface language: the window was only half translated when the game language was English, because its texts were created once. The window is now built again when the language changes, and all texts have both languages.
- New option **Language** in the mod options: *Auto* (the language of the game), *English* or *Русский*. The options page itself is translated into Russian too (the options page changes its language when it is opened again).
- Texture-path: the **Colour (no texture)** choice is gone for decal paths (the game did not project anything without a texture); a new decal path starts with the first decal of the list. A plane can still be drawn without a texture (theme pavement and colour).
- Texture-path: new value **Colour multiply** under the palette (0 to 1). 0 is the old behaviour (the game applies the tint only where the decal allows a colour), 1 multiplies the whole texture by the colour. It works for placed decals; a plane always multiplies.
- Network-line: **Line start** and **Line end** (were *Line start offset* / *Line end offset*) now come after **Offset Z**.

### v0.5.5
- Line across a node (Network-line, Props-line, Texture-path), rebuilt after reading the source of Node Controller Renewal and the game's node rendering: NCR changes the corner position and direction of every segment end (corner offset, shift, stretch, embankment, slope, twist, sharp corners, per-corner position / direction) inside `NetSegment.CalculateCorner`, and the game draws the surface of a bend node from those corners: its two edge curves join each corner of one segment with the corner of the other segment that is on the same side (matched by the side going away from the node, not by distance) with smooth ends. The line over a node is now exactly that surface at the line's position (so it follows the real shape and height of the node), it starts and ends at the end points of the two segment lines, and it leaves each of them in the direction of that segment's line (a small correction only, so there is no kink). The centre-line shortcut is used only for turns above 110 degrees.
- New value **Offset X** for Props-line and Texture-path: moves the props / texture / tiles along the line without moving the ends of the line.
- The offsets are renamed and ordered the same in all tools: **Offset X**, **Offset Y** (was Horizontal offset), **Offset Z** (was Vertical offset).
- Window: one fixed size for all tools (the description block is sized for the longest text), content starts lower below the title, more space between the buttons, delete / undo / redo / reset on a lighter box of their own, delete buttons are dark red, the model / texture / prop chooser is twice as high with a framed picture, and **Hide highlight interface** is shown only in tools with sliders, at the bottom of the third column.
- Fixed kinks and breaks at the joints between a segment and a node (see the next item for how the curve over the node is built).
- Network-model lines are built again the way they were before v0.5.0: one curve from the start to the end of the segment (the blend of the real edge curves, taken from the game and from mods that change the corners). Only the gaps at nodes are separate pieces, one per gap. The texture coordinates are set by the mod without rounding and continue from piece to piece. The earlier cut into short pieces is gone (it made steps).
- Texture-path: new **Line start** / **Line end** values (shorten the path at either end, like the other tools).
- Texture-path: the **Alternative rendering** switch is replaced by two buttons, **Add decal** and **Add plane**; the way of drawing is chosen when a path is added.
- Texture-path: *Colour (no texture)* uses the game's theme pavement texture as its base. A decal path without a texture places a stand-in decal (a game decal prop with the pavement texture), a plane draws a strip.
- New window layout in three columns: the tools (vertical, on a panel of their own), the controls of the tool with the undo / redo / reset bar, the highlight switch and the description (on a panel of its own, on top for a tool without values and at the bottom otherwise), and the sliders. Texture-path, Network-line and Props-line use a common order of values. Explanations in brackets were removed from the labels.
- New switch **Hide highlight while dragging** in every tool: the highlight of the quay borders and lines is hidden while a slider is dragged and shown at all other times.
- New tool **Hide default props** (between "Remove pedestrian path" and "Network-line"): hides the props of the quay's network model on selected segments; your Props-line props stay.

### v0.5.4
- Fixed: network-model lines twisted at sharp corners (nodes narrowed with Node Controller Renewal): pieces now end exactly at sharp turns and use one-sided directions there, and no piece turns more than about 30 degrees.
- Fixed: "Remove pedestrian path" did not stop "any means" paths (walking combined with vehicles or transport, lane types 43 in the log): every path that may use pedestrian lanes now skips blocked segments.

### v0.5.3
- Fixed: network-model lines did not follow the quay (bends, S-curves, heights): a line is now built from short pieces fitted to the real path of the quay (including the bridged gaps at nodes), instead of one curve per segment.
- "Remove pedestrian path": citizens that already walk over a blocked segment (their saved path crosses it) are now sent on their way again with the game's own path invalidation, also when a save is loaded; the pathfinder patch covers more TM:PE methods and logs the first checks (`Pedestrian block check`).

### v0.5.2
- Fixed: network-model lines disappeared after the first frame (the renderer disabled itself); it now recovers from errors.
- Fixed: "Remove pedestrian path" had no effect when TM:PE is installed (TM:PE runs its own path-finder class); the mod now patches those classes too and logs what it patched.
- Texture-path strip mode: the default height is now 1 m (set when the strip switch is turned on).
- Texture-path strips whose texture has no transparency (and with full opacity) now use the game's lit shader: shadows fall on them and the brightness matches the surroundings. Semi-transparent strips stay unlit.

### v0.5.1
- Fixed: network-model lines were invisible when another mod patches the game's segment rendering (for example Adaptive Roads): the models are now submitted directly (same shader inputs as the game), not through the patched routine.
- Fixed: "Remove pedestrian path" did not stop pedestrians: the pathfinder check used wrong lane-type values.
- Texture-path in strip mode now looks for a lit shader among all loaded shaders (shadows fall on it, lighting matches the surroundings); the candidates are listed in the log.
- New option: size of the floating tool icons (1, 2, 3; default 2 = twice the former size).
- Note: if you unpack over an older folder, delete the old `Patches` folder (the file `FencePatches.cs` no longer exists in v0.5.x).

### v0.5.0
- **Network-line** (was *Add network model*) reworked like Props-line: any number of lines per segment, no more fixed "land" / "water" models. A new line appears in the middle of the quay, then it is moved (across, height), trimmed, scaled and chosen freely. Lines run from the middle of the start node to the middle of the end node; the start and the end can be trimmed back to the border of the node and beyond. Old saves are converted (land model = line 1, water model = line 2).
- **Turn the model around** is now a real half turn (the front faces the other way; no mirroring, no inverted normals).
- **Texture-path** (was *Add decal path*) reworked the same way: any number of paths per segment. The **rendering method** (placed game decals / one textured strip) is now a setting of each path instead of a mod option.
- Props-line: the rotation controls are hidden when a tree is chosen.
- All sliders with value fields share one layout: the slider ends right before the value box (no more overlapping in Props-line trims etc.).
- **Remove pedestrian path**: the pathfinder patch now also covers `ProcessItemCosts` (which returns a value) - the method that actually builds the walking routes.
- Tool order: Invert segment, Lock segment, Remove pedestrian path, Network-line, Props-line, Texture-path.

### v0.4.1
- The drop-down lists (models, decals, props) show **all** items (the prop list was cut off at 60 entries), have a **search field**, and a **star** per item for **favourites** (listed first, kept in the mod settings). The lists open above the Undo / Redo / Reset bar.
- **Width scale** of fence models (and rotation, prop scale, random size) can be typed in a field next to the slider.
- Fence and prop-line **ends can only be trimmed**: sliders run from −50 m to 0 (0 = full length, the default). Saved positive shifts become 0.
- Props-line: the line now runs from the middle of the start node to the middle of the end node and follows the node's bend and height; trimming the start never moves the other props (and vice versa). The **Line enabled** switch is gone. **Trees** can be placed too. Step is limited to 50 m.
- Horizontal / vertical offsets of fences, decal paths and prop lines are limited to ±50 m.
- New: **Flip model** for fence models (mirrors the model so it faces the other side).
- **Remove pedestrian path** reworked: the pathfinder patch now targets the `PathFind.ProcessItem*` methods that exist in the game and only blocks walking paths, so vehicles keep driving on the segment. The methods found are listed in the log.

### v0.4.0
- Edited segments are highlighted and marked with tool icons while the tool is active (mod option to switch it off).
- New tool **Lock segment orientation**: locked segments are flipped back at once if the game or another mod flips them.
- Fences: **detach** the fences of a segment from its neighbours at either end, **shift the start and end** of each fence along the quay (positive extends, negative trims) and **scale the width** of each fence model. Shifted ends detach automatically. The window gets a second column (**Extra** button) for these controls.
- New tool **Props-line**: any number of prop lines per segment (prop, step, start/end shift, shift across the quay, height, rotation, random rotation, scale, random size, on/off), searchable prop list.
- Decal paths at **sharp bends of nodes** (Node Controller Renewal): the path across the gap now follows one curve between the centres of the two segment ends instead of being blended from two crossing edge curves (angle above 30°). Can be switched off in the mod options (**Decal paths: at sharp bends ...**).
- The tool buttons are now a compact row of icons (the window is shorter).
- New tool **Remove pedestrian path**: selected segments are skipped by the pathfinder, so citizens do not walk on them or see them as a path.

### v0.3.6
- Node Controller Renewal nodes, rewritten bridge: when a node joins two segments, ONE of the two paths (the neighbour's if it has a path and a smaller id, otherwise this one) continues across the gap. The gap is treated as a short segment: its left and right edge curves join the corners of the two ends (the corners come from the game's corner calculation, which NCR replaces, so shifted borders, border angles - the different lengths of the two edge curves - and heights are included), and the path is blended from them exactly like the path of a real segment. Tiles no longer jump sideways or leave wedges at the node, wide gaps follow the curve of the quay, and the height in the gap follows the slopes of the two ends.
- Placed decals over such a gap use a taller projection box (at least 20 m) as a safety margin.

### v0.3.5
- New sliders for placed decals: **Decal step** (distance between tiles along the path, 0 = one tile length so tiles touch; a larger value leaves gaps, a smaller one overlaps them) and **Decal box height** (thickness of the projection box, default 8 m: it decides how far above and below the path the decal is projected onto the surface).
- Node Controller Renewal nodes: the gap between two segment ends is now bridged using the neighbour's real path end point (both paths meet in the middle of the gap, at its height and position) instead of a flat extension to the node centre. The corners used already contain the NCR edits, because NCR replaces the game's corner calculation that the mod reads.

### v0.3.4
- Decals are now placed step by step along the path as real game decal props (tile matrices, the way the game draws decal props). The previous approach drew one flat mesh with the game material, which the game's decal shader does not display. No mask cropping in this mode; the old textured strip is still available in the mod option **Decal path rendering**.
- Decal strips (plain colour and the textured-strip mode) now receive shadows and never cast them (lit shader; mod option **Decal strips receive shadows**, on by default). Placed game decals use the game's own shader.
- Node Controller Renewal: a path now always continues up to the node when the node joins exactly two segments (before, the neighbour also had to have a path). The log lists segments whose ends are away from their nodes (`ends are away from the nodes`).

### v0.3.3
- Sideways shift is now measured toward the water (+ = to the water), so a path no longer jumps to the other side on segments with a different orientation.
- The shift is applied between the two edge curves of the segment, so the path follows heights and slope also when shifted (fixes the path sinking through the quay near nodes edited with Node Controller Renewal).
- Decals are drawn by default from a composed texture (colours from the diffuse map, opacity from the ACI map): no more dark or translucent textures, and no need for the old option.
- The six preset tint colours are replaced by a free RGBA colour: R, G, B, A sliders and a hex field with a preview. Older saves keep their colours.
- The option is now "Draw decals with the decal prop's own game material" (experimental, off by default); the old option value is ignored.

### v0.3.2
- Decal paths continue across nodes whose segment ends were moved away with Node Controller Renewal (between two segments that both have a path), so no gap is left around such nodes.
- Option "Draw decal paths with a simple textured material" in the mod options: an unlit fallback for decals that stay invisible with the decal prop's own material. Extra diagnostics in the log.
- Window: shorter labels, taller hint area so the texts fit.

### v0.3.1
- Fixed a compile error (decal scale limits).

### v0.3.0
- Decal paths now use real decal props: drop-down list of decals, tiling along and across the path, cropping by the mask (width, sideways shift, vertical offset), tint colour and decal scale. Plain colour strip is still available.
- Decal paths on nodes edited with Node Controller Renewal: unusable corners no longer hide the path (fallback to the node centre line), failed builds are retried and logged.
- Reset keeps the chosen decal, like it keeps fence models.

### v0.2.1
- Fixed a compile error in the window code (missing field for the Undo/Redo/Reset bar).

### v0.2.0
- Fences follow automatic orientation changes of a segment (land side / water side stay correct).
- New: Undo / Redo (buttons and Ctrl+Z / Ctrl+Y) and Reset to defaults in the editor.
- New tool: **Add decal path** (width, shifts, colour; follows curves and heights of the quay).
- Removing models now refreshes the window at once.

### v0.1.6
- Segment fences are built from the quay's own two edge curves (blended at the fence position), so they follow the model exactly even when node corners are rotated.

### v0.1.5
- Node Controller compatibility: the fence position is now interpolated between the two real corners of the segment end, so rotation, shift, stretch and height changes of a node are followed without artifacts.

### v0.1.4
- Fences at node fragments (bends) are rebuilt too, so height and offsets also work on nodes edited with Node Controller.
- The closing fence at dead ends is now straight and perpendicular; separate options for the start and the end of each segment (replaces "do not join").
- Closing and bend fences follow the set heights.

### v0.1.3
- Fences are rebuilt from the final node corners, so they follow node edits made with Node Controller (width, curve) and stay editable.
- Fence height follows the real (smooth) height profile of the quay instead of a linear one.

### v0.1.2
- Fence models that follow the terrain (height-map shader) now get the height of the quay deck instead; the vertical offset works on them.

### v0.1.1
- Land/water side is now taken from the game's own quay terrain logic (Invert flag) instead of mesh bounds.
- Highlight and fence base position use the quay lane area instead of the whole network width.
- Offsets up to ±100 m, manual value entry, mouse-wheel scrolling in model lists.
- Option to swap land/water sides (mod options).

### v0.1.0
- Add Network Model reworked: select segments first, then choose models and offsets in the window.
- Model 1 (land) / Model 2 (water) drop-down lists with previews.
- Horizontal and vertical offset sliders (-100..100).
- Option to not join fences from both sides.
- Highlight and fence position follow the visible quay model instead of the network width.
- Fixed land/water side mismatch.
- Fences keep their side when a segment is flipped.
- Fence settings are saved with the savegame.
- Harmony patches added (fence corners and end caps).
- Localization: English and Russian.

### v0.0.1
- UnifiedUI integration and fallback floating button.
- Quay segment inversion, connected-quay selection, quick-flip hotkey.
- Add/remove fence networks on quay segments.
