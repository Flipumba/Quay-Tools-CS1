# Quay Tools (Cities: Skylines 1) - Stage A

Quay Tools with a UnifiedUI button.

## What works in this stage
- UnifiedUI button (icon) -> activates the Quay tool and opens a small floating window.
  If UnifiedUI is not installed/enabled, a small floating button is created instead.
- Tool "Invert segment": hover a quay (highlighted), left click to flip it.
  Hold Shift to highlight/flip the whole connected quay (blue = single, green = chain).
  Right click exits the tool.
- Quick-flip hotkey without the tool: Ctrl + R over a quay (Shift = whole quay). Optional in mod options.
- Tool "Add network model" (v1.3): pick a fence/wall network in the list, then click the SIDE of a quay
  (the stripe shows where it goes). Shift = whole connected quay. "Remove fence" removes it.
  Works by writing the fence into the segment's left/right fence slot, like the vanilla fence tool
  (which itself refuses quays because they are not RoadBaseAI). The game saves and deletes it with the segment.
  No offsets yet (next stage). If the highlighted stripe is on the wrong side compared to the cursor,
  flip `QuayTool.LeftIsGeometricLeft`.
- Window button "Remove pedestrian path" is visible but disabled.

## Build
1. Open `QuayTools.csproj` in Visual Studio (or `dotnet build -c Release`).
2. If the game is not in the default Steam folder, set `CS1ManagedPath` (see csproj).
3. Build. On Windows the output is copied to
   `%LOCALAPPDATA%\Colossal Order\Cities_Skylines\Addons\Mods\QuayTools`:
   `QuayTools.dll`, `UnifiedUILib.dll` and the `Icons` folder. All three must stay together.
4. Enable "Quay Tools" in Content Manager > Mods. UnifiedUI is optional but recommended.

## Third-party
`Lib/UnifiedUILib.dll` is the UnifiedUI helper library (MIT license, (c) 2022 UnifiedUI).
It is shipped next to the mod DLL so the mod can talk to the UnifiedUI mod.

## Not verified (never compiled or run by the author of this code)
The UnifiedUI calls were checked against the real UnifiedUILib.dll signatures.
Game API calls were written from memory. If the build or the game complains, check first:
- `QuayAI` type (used for quay detection; the game has it, AdaptiveRoads patches it).
- `ToolBase` overrides: `OnEnable`, `OnDisable`, `OnToolUpdate`, `RenderOverlay` (signatures/access).
- `RenderManager.instance.OverlayEffect.DrawBezier(...)` argument list.
- `NetSegment.CalculateMiddlePoints(...)` argument list.
- `UIComponent.Awake/Start` overrides, `UIButton.eventClicked`, `UITextureSprite`, `UIButton.state`.
- `ToolController.CurrentTool` setter, `ToolsModifierControl.SetTool<DefaultTool>()`.
- Tool registration: `ToolInstaller` adds the tool to a private `m_tools` array by reflection
  (harmless if the field does not exist).
- Icon size expected by UnifiedUI (icons are 64x64, light glyphs on transparent background).

## Harmony
The mod references CitiesHarmony.API (shipped in the mod folder). The Harmony library itself comes from the
"Harmony (Mod Dependency)" mod (Workshop 2040656402), which must be subscribed/enabled and marked as a
required item when publishing. `HarmonySetup` applies/removes patches in `OnEnabled` / `OnDisabled`.
There are no patches yet.
