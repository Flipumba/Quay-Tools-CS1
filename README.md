# Quay Tools — Cities: Skylines 1

**Version: v0.1.0**

Quay Tools is a Cities: Skylines 1 mod with additional tools for working with quay segments.

## Features

### UnifiedUI

* Adds a **UnifiedUI button** that activates Quay Tools and opens a small tool window.
* If UnifiedUI is not installed or enabled, a small floating button is created instead.

### Invert Segment

Flip the direction of a quay segment without rebuilding it.

* Hover over a quay to highlight it. The highlight follows the visible quay model.
* **Left click** flips the highlighted segment.
* Hold **Shift** to highlight and flip the entire connected quay (blue = single segment, green = connected quay).
* **Right click** exits the tool.
* Fences placed by Quay Tools keep their land/water side when a segment is flipped.

#### Quick Flip

Flip a quay without activating the tool:

* **Ctrl + R** over a quay flips the segment.
* **Ctrl + R + Shift** flips the entire connected quay.
* The hotkey can be changed or disabled in the mod options.

### Add Network Model

Adds fence/wall networks along the full length of quay segments.

1. Activate the tool and select the **Add network model** mode.
2. Click one or several quay segments to select them (orange). **Shift** selects the whole connected quay, **right click** clears the selection.
3. In the window, choose models with the drop-down lists (preview and name for each model):

   * **Model 1** — land side.
   * **Model 2** — water side.
   * The default entry is **Empty**.
4. Tune the offsets for each model:

   * **Horizontal offset** — slider from -100 to 100 (0.1 m per step). 0 places the fence right at the edge of the quay model; positive values move it towards the water, negative towards the land.
   * **Vertical offset** — slider from -100 to 100 (0.1 m per step).
   * Double-click a slider to reset it.
5. Optional checkbox: **do not join fences from both sides** (removes the end cap that connects left and right fences).
6. Close the tool when finished.

To remove models, select the segments and press **Remove models** under the settings.

Selection lines: green = land edge, blue = water edge.

#### How it works

* The fence is written into the segment's left/right fence slot, as the vanilla fence tool does (vanilla refuses quays because they are not `RoadBaseAI`). The game saves the fence with the segment and removes it when the segment is deleted.
* Horizontal/vertical offsets and the "do not join" option are applied with Harmony patches while the game builds fence geometry.
* The width used for the highlight and the fence position is measured from the quay's visible model, not from the (much wider) network.
* Per-segment offsets are saved in the savegame (key `QuayTools.Fences`). Loading a save without the mod simply ignores them.

## Planned / Incomplete Features

* **Remove pedestrian path** — the button is visible but disabled. Planned approach: swapping to existing quay variants without a pedestrian lane, plus a crossed-out pedestrian icon above the segment.

## Building

### Requirements

* Visual Studio with .NET tooling, or the `dotnet` CLI.
* A local installation of Cities: Skylines 1.
* The dependencies listed below.

### Build

Open `QuayTools.csproj` in Visual Studio and build in **Release**, or:

```bash
dotnet build -c Release
```

If the game is not in the default Steam directory, set `CS1ManagedPath` in `QuayTools.csproj`.

After a successful build on Windows the files are copied to:

```text
%LOCALAPPDATA%\\Colossal Order\\Cities\_Skylines\\Addons\\Mods\\QuayTools
```

* `QuayTools.dll`
* `UnifiedUILib.dll`
* `CitiesHarmony.API.dll`
* `Icons\\`

All of them must stay together in the mod folder. Enable **Quay Tools** in **Content Manager → Mods**.

## Dependencies

### Harmony (required)

Quay Tools uses `CitiesHarmony.API`; the Harmony library itself comes from the **Harmony (Mod Dependency)** mod (Workshop ID `2040656402`). It must be subscribed to and enabled, and added as a required item when publishing Quay Tools to the Steam Workshop.

### UnifiedUI (optional)

`Lib/UnifiedUILib.dll` is the UnifiedUI helper library (MIT license, © 2022 UnifiedUI), shipped next to the mod DLL so Quay Tools can talk to the UnifiedUI mod. UnifiedUI icons are 64×64 px, light glyphs on a transparent background.

## Compatibility and status

* v0.0.1 was compiled and tested in-game.
* **v0.1.0** was compiled and tested in-game. The new fence placement, offsets, side mapping and the new window were written from analysis of the game's `Assembly-CSharp.dll`. Please report problems together with `output\_log.txt` (lines starting with `\[QuayTools]`).
* Game APIs used: `QuayAI`, `ToolBase` (`OnEnable`, `OnDisable`, `OnToolUpdate`, `RenderOverlay`), `OverlayEffect.DrawBezier`, `NetSegment.CalculateCorner`, `NetNode.GetEndFences`, `PrefabCollection<NetInfo>`, `SerializableDataExtensionBase`, cs UI components (`UISlider`, `UIScrollablePanel`, ...).
* `ToolInstaller` adds the tool component to the private `m\_tools` array via reflection.

## Changelog

### v0.1.0

* Add Network Model reworked: select segments first, then choose models and offsets in the window.
* Model 1 (land) / Model 2 (water) drop-down lists with previews.
* Horizontal and vertical offset sliders (-100..100).
* Option to not join fences from both sides.
* Highlight and fence position follow the visible quay model instead of the network width.
* Fixed land/water side mismatch.
* Fences keep their side when a segment is flipped.
* Fence settings are saved with the savegame.
* Harmony patches added (fence corners and end caps).
* Localization: English and Russian.

### v0.0.1

* UnifiedUI integration and fallback floating button.
* Quay segment inversion, connected-quay selection, quick-flip hotkey.
* Add/remove fence networks on quay segments.

