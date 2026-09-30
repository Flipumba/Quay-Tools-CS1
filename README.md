# Quay Tools — Cities: Skylines 1

**Version: v0.3.0**

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
- Fences placed by Quay Tools keep their land/water side when a segment is flipped.

#### Quick Flip

Flip a quay without activating the tool:

- **Ctrl + R** over a quay flips the segment.
- **Ctrl + R + Shift** flips the entire connected quay.
- The hotkey can be changed or disabled in the mod options.

### Add Network Model

Adds fence/wall networks along the full length of quay segments.

1. Activate the tool and select the **Add network model** mode.
2. Click one or several quay segments to select them (orange). **Shift** selects the whole connected quay, **right click** clears the selection.
3. In the window, choose models with the drop-down lists (preview and name for each model):
   - **Model 1** — land side.
   - **Model 2** — water side.
   - The default entry is **Empty**.
4. Tune the offsets for each model:
   - **Horizontal offset** — slider up to ±100 m (0.1 m per step) or type a value in the field next to it. 0 places the fence at the edge of the quay lanes; positive values move it towards the water, negative towards the land.
   - **Vertical offset** — same range.
   - Double-click a slider to reset it. The drop-down lists scroll with the mouse wheel.
5. Optional: **Close fence at segment start / end** — a straight fence across the quay, at right angles, at a dead end of the segment (the start is marked with a cyan ring, the end with a magenta ring). Each end is set separately, per segment.
6. Close the tool when finished.

To remove models, select the segments and press **Remove models** under the settings.

Selection lines: green = land edge, blue = water edge.

#### Orientation changes made by the game

If the game (or another mod) flips the orientation of a segment by itself, for example while nodes are moved, the fences follow: Model 1 stays on the land side and Model 2 on the water side. The check runs about three times a second, so a fence may take a moment to move.

### Add Decal Path

Lays a ground decal (any decal prop you have: Workshop decals, cobblestones, markings, ...) along the middle of the top surface of a quay.

1. Select the **Add decal path** mode. The selection is shared with the network-model mode.
2. Select quay segments (**Shift** selects the whole connected quay).
3. Choose a **decal** in the drop-down list (the first entry, *Plain colour*, draws a simple coloured strip).
4. The strip is a **mask**: set its **width** (0.1–50 m), an optional **sideways shift** and a **vertical offset** (±100 m). The decal is cropped by the mask.
5. **Decal scale** is the width of one tile of the decal texture (choosing a decal sets its natural size). The texture is repeated along and across the mask in tiles of this size, centred on the middle of the path.
6. Pick a **tint**: it multiplies the colours of the decal (white = original colours) or fills the plain strip.
7. Press **Add / apply path**. **Remove path** removes it from the selected segments.

Changing any control while segments with paths are selected updates those paths at once. The path is built from the two edge curves of the segment, so it follows the curves and heights of the quay, also after node edits made with Node Controller Renewal.

Notes:

- The decal is drawn with the material of the decal prop, so it looks like that prop (lighting, blending). Decals that need the terrain height map are not offered.
- A decal is stored by the name of its prop. If the asset is missing when a save is loaded, that path is not drawn (a line in the log says so).
- The log lines `[QuayTools] Decal catalog: N decal props` and `[QuayTools] Decal path built: ...` help to find problems.

### Undo, Redo and Reset

At the bottom of the window in the network-model and decal modes:

- **Undo / Redo** (also **Ctrl+Z**, **Ctrl+Y** or **Ctrl+Shift+Z** while the tool is active): models, offsets, closing fences, decal paths and resets, up to 100 steps. Dragging a slider counts as one step. The history is kept until the map is left; it is not saved in the savegame.
- **Reset**: sets offsets and closing fences (network-model mode) or width, shifts and colour (decal mode) of the selected segments back to their defaults. Models stay.
- The Ctrl+Z / Ctrl+Y keys can be turned off in the mod options. The history is Quay Tools' own; it is not connected to other undo mods.

#### How it works

- The fence is written into the segment's left/right fence slot, as the vanilla fence tool does (vanilla refuses quays because they are not `RoadBaseAI`). The game saves the fence with the segment and removes it when the segment is deleted.
- Horizontal/vertical offsets and the "do not join" option are applied with Harmony patches while the game builds fence geometry.
- The width used for the highlight and the fence position is measured from the quay's visible model, not from the (much wider) network.
- Per-segment offsets are saved in the savegame (key `QuayTools.Fences`) and decal paths under `QuayTools.Decals`. Loading a save without the mod simply ignores them.

## Planned / Incomplete Features

- **Remove pedestrian path** — the button is visible but disabled. Planned approach: swapping to existing quay variants without a pedestrian lane, plus a crossed-out pedestrian icon above the segment.

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
- **Node Controller Renewal (NCR)** — not needed, but supported: fences and decal paths follow nodes edited with it.
- **Decal props** (Workshop or your own) — needed only to have something to choose in the *Add decal path* list; network fence models for *Add network model* likewise come from the game or from installed assets.

### Build dependencies

Only needed to compile the mod yourself (restored by NuGet, except the game DLLs and `UnifiedUILib.dll`):

- `CitiesHarmony.API` and `CitiesHarmony.Harmony` (NuGet, version 2.x)
- `Microsoft.NETFramework.ReferenceAssemblies` (NuGet)
- `Lib/UnifiedUILib.dll` (included in the repository)
- Game assemblies from `Cities_Data\Managed`: `Assembly-CSharp.dll`, `ICities.dll`, `ColossalManaged.dll`, `UnityEngine.dll` (path set by `CS1ManagedPath` in the project)

## Compatibility and status

- The current version has been tested in-game. Please report problems together with `output_log.txt` (lines starting with `[QuayTools]`).
- Game APIs used: `QuayAI`, `ToolBase` (`OnEnable`, `OnDisable`, `OnToolUpdate`, `RenderOverlay`), `OverlayEffect.DrawBezier`, `NetSegment.CalculateCorner`, `NetNode.GetEndFences`, `PrefabCollection<NetInfo>`, `SerializableDataExtensionBase`, cs UI components (`UISlider`, `UIScrollablePanel`, ...).
- `ToolInstaller` adds the tool component to the private `m_tools` array via reflection.

## Changelog

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
