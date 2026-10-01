# Quay Tools — Cities: Skylines 1

**Version: v0.5.2**

Quay Tools is a Cities: Skylines 1 mod with additional tools for working with quay segments.

## Features

### UnifiedUI

- Adds a **UnifiedUI button** that activates Quay Tools and opens a small tool window.
- If UnifiedUI is not installed or enabled, a small floating button is created instead.

### Invert Segment

Flip the direction of a quay segment without rebuilding it.

- Hover over a quay to highlight it. The highlight follows the visible quay model.
- **Left click** flips the highlighted segment.
- Hold **Shift** to highlight and flip the entire connected quay (blue = single segment, green = connected quay).
- **Right click** exits the tool.
- Network lines, prop lines and texture paths follow the water side when a segment is flipped.

#### Quick Flip

Flip a quay without activating the tool:

- **Ctrl + R** over a quay flips the segment.
- **Ctrl + R + Shift** flips the entire connected quay.
- The hotkey can be changed or disabled in the mod options.

### Lock Segment

Stops the game (or another mod) from flipping a quay segment on its own, for example while nodes are moved.

1. Select the **Lock segment orientation** tool (padlock icon).
2. Select quay segments (**Shift** selects the whole connected quay) and press **Lock selected segments**. Locked segments are drawn red while the tool is active.
3. If a locked segment is flipped by something else, it is flipped back right after that simulation step . **Unlock selected segments** removes the lock.
4. The Invert tool and Ctrl+R of Quay Tools still work on a locked segment; the lock keeps the new orientation.

The lock is saved in the savegame (`QuayTools.Locks`). If another mod keeps flipping a segment back and forth, the lock is released and a line is written to the log.

### Remove Pedestrian Path

Makes citizens stop using a quay as a footpath.

1. Select the **Remove pedestrian path** tool (crossed-out pedestrian icon), select quay segments (**Shift**: whole connected quay).
2. Press **Remove pedestrian path on selected**. The segments are marked red; the pathfinder skips them, so pedestrians neither route over them nor see them as a path. **Restore pedestrian path on selected** undoes it.
3. Citizens already walking along such a segment finish their current walk; new walking paths avoid it. Only walking paths are blocked (pedestrian lanes without vehicle lanes), vehicles still drive on the segment.

Implemented with Harmony patches on the `PathFind.ProcessItem*` methods that receive a segment id; they are found by reflection and listed in the log (`Pedestrian block: patched N PathFind method(s)`). If a game update changes them the feature is simply unavailable. Saved in the savegame (`QuayTools.NoPeds`).

### Network-line

Draws network models (fences, walls, ...) along quay segments. Any number of lines per segment, like Props-line.

1. Select the **Network-line** tool. Select quay segments (orange; **Shift**: whole connected quay; **right click** clears the selection).
2. Press **+ Add network-model line**. A line is added to every selected segment, **in the middle of the quay**; use **< >** to switch between the lines of the selection (the line being edited is drawn as a blue stripe).
3. Choose the **network model** in the drop-down list (preview and name, search field, scrolling, **star** = favourite: favourites are listed first and are kept in the mod settings for all savegames) and tune the line:
   - **Forward / back across the quay** (+ toward the water, ±50 m) and **height** (±50 m): slider, or type a value in the field next to it. Double-click a slider to reset it.
   - **Line start / end trim** (0 to −50 m): the line runs from the **middle of the start node to the middle of the end node** (across the node it follows its bend and height curve). Trimming moves the start or the end of the line back toward the segment, up to the border of the node and further, without joining it to the neighbouring segment.
   - **Width (thickness) scale** (10–500 %). The height of a model cannot be scaled (the game's net shader takes it from the model).
   - **Turn the model around**: the model faces the water by default; with this switch it faces the land. This is a real half turn (the front becomes the back), not a mirror image.
   - **Close line at segment start / end**: a straight piece of the same model across the quay, facing away from the segment (start = cyan ring, end = magenta ring).
4. **Remove this line** / **Remove all lines** delete lines from the selected segments.

Changes apply to the same line number on all selected segments. Where only one of two neighbouring segments has a line, only its half of the node is covered. Lines are placed relative to the water side, so they move with the quay when it is flipped (the model keeps facing the same way relative to the water).

Saves of v0.4.x are converted when loaded: the old **land** model becomes line 1 and the **water** model line 2 (position, height, trims, scale, closing pieces and orientation are kept; the old "mirror" flip is now a real turn-around; "detach" is no longer needed because lines are not joined to their neighbours).

### Props-line

Places props along quay segments at a fixed step (lamps, trees, benches, bollards, ...). Any non-decal prop you have can be used. The props are decoration only.

1. Select the **Props-line** tool. Select quay segments (**Shift**: whole connected quay).
2. Press **+ Add prop line**. A line is added to every selected segment; use **< >** to switch between the lines of the selection.
3. Choose the prop or **tree** (trees are marked `[tree]`) in the drop-down list (search field and favourites inside the list) and tune the line:
   - **Step between props** (0.5–50 m).
   - **Line start / end trim** (0 to −50 m): the line runs from the **middle of the start node to the middle of the end node** (across the node it follows its bend and height curve), so props also stand on the nodes. The props are counted from the start of the line; trimming one end only removes props and never moves the others.
   - **Shift forward / back across the quay** (+ toward the water) and **height** (±50 m).
   - **Rotation of the props**, **Random rotation** (for trees), **Prop scale** (5–1000 %) and **Random size variation**.
4. **Remove this line** / **Remove all lines** delete lines from the selected segments.

Changes apply to the same line number on all selected segments. The lines follow the curves and heights of the quay like decal paths do. Where only one of two neighbouring segments has a line, only its half of the node is covered. Trees are drawn through the game's tree renderer (no rotation). Props-line data is saved in the savegame (`QuayTools.PropLines`). If a prop asset is missing when a save is loaded, that line is not drawn (a line in the log says so).

### Texture-path

Lays a ground decal (any decal prop you have: Workshop decals, cobblestones, markings, ...) or a plain coloured strip along the top surface of a quay. Any number of paths per segment.

1. Select the **Texture-path** tool and select quay segments (**Shift**: whole connected quay).
2. Press **+ Add texture path**: a path appears in the middle of the quay; use **< >** to switch between the paths of the selection.
3. Choose a **decal** in the drop-down list (the first entry, *Plain colour*, draws a simple coloured strip).
4. The path is a **mask**: set its **width** (0.1–50 m), a **sideways shift** (+ toward the water) and a **height offset** (±50 m).
5. **Decal scale** is the width of one tile of the decal texture (choosing a decal sets its natural size); **Decal step** and **box height** control the placed tiles.
6. Set the **tint** with the R, G, B and A sliders or type a hex value (`#RRGGBB` or `#RRGGBBAA`).
7. **Alternative rendering** (per path): instead of placing game decals step by step, the path is drawn as one textured strip cropped by the mask, drawn unlit from a texture composed from the decal prop (colours from its diffuse map, opacity from its ACI map). Switch it in the settings of each path.
8. **Remove this path** / **Remove all paths** delete paths from the selected segments.

Changes apply to the same path number on all selected segments. The path is built from the two edge curves of the segment, so it follows the curves and heights of the quay, also after node edits made with Node Controller Renewal. Saves of v0.4.x keep their path (the old global rendering option becomes the setting of each path).

Notes:

- By default the decal is **placed like the game places decal props**: tiles of the decal prop are laid step by step along the path. This mode has no mask cropping. Decals that need the terrain height map are not offered.
- A decal is stored by the name of its prop. If the asset is missing when a save is loaded, that path is not drawn (a line in the log says so).

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

## Changelog

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
