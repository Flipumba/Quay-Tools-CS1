# \# Quay Tools — Cities: Skylines 1

# 

# \*\*Version: v0.0.1\*\*

# 

# Quay Tools is a Cities: Skylines 1 mod that provides additional tools for working with quay segments and networks.

# 

# \## Features

# 

# \### UnifiedUI

# 

# \- Adds a \*\*UnifiedUI button\*\* for activating Quay Tools.

# \- Clicking the button opens a small floating tool window.

# \- If UnifiedUI is not installed or enabled, Quay Tools creates a small floating button instead.

# 

# \### Invert Segment

# 

# Flip the direction of a quay segment without rebuilding it.

# 

# \- Hover over a quay to highlight it.

# \- \*\*Left click\*\* to flip the highlighted segment.

# \- Hold \*\*Shift\*\* to highlight and flip the entire connected quay.

# &#x20; - \*\*Blue\*\* — single segment.

# &#x20; - \*\*Green\*\* — connected quay.

# \- \*\*Right click\*\* to exit the tool.

# 

# \#### Quick Flip

# 

# You can also flip a quay without activating the tool:

# 

# \- \*\*Ctrl + R\*\* over a quay — flip the segment.

# \- \*\*Ctrl + R + Shift\*\* — flip the entire connected quay.

# \- The hotkey can be enabled or disabled in the mod options.

# 

# \### Add Network Model

# 

# Allows you to add a fence or wall network to a quay.

# 

# 1\. Select a fence/wall network from the list.

# 2\. Click the \*\*side of a quay\*\* where you want to place it.

# 3\. Hold \*\*Shift\*\* to apply it to the entire connected quay.

# 4\. Use \*\*Remove fence\*\* to remove the fence.

# 

# The selected side is indicated by a highlighted stripe.

# 

# The tool works by writing the fence into the segment's \*\*left/right fence slot\*\*, similarly to the vanilla fence tool.

# 

# The vanilla fence tool normally refuses to work with quays because quays are not `RoadBaseAI`. Quay Tools bypasses this limitation by writing directly to the segment data.

# 

# The game saves the fence together with the segment and removes it when the segment is deleted.

# 

# > \*\*Note:\*\* Fence offsets are not implemented yet.

# 

# \#### Fence Side Configuration

# 

# If the highlighted stripe appears on the opposite side from the cursor, change:

# 

# ```text

# QuayTool.LeftIsGeometricLeft

# ```

# 

# \## Planned / Incomplete Features

# 

# \- Fence/network offsets are not implemented yet.

# \- The \*\*Remove pedestrian path\*\* button is visible in the tool window but currently disabled.

# 

# \## Building

# 

# \### Requirements

# 

# \- Visual Studio with the required .NET tooling, or the `dotnet` CLI.

# \- A local installation of Cities: Skylines 1.

# \- Required mod dependencies listed below.

# 

# \### Build with Visual Studio

# 

# Open:

# 

# ```text

# QuayTools.csproj

# ```

# 

# in Visual Studio and build the project in \*\*Release\*\* configuration.

# 

# \### Build with dotnet

# 

# ```bash

# dotnet build -c Release

# ```

# 

# If Cities: Skylines is not installed in the default Steam directory, set `CS1ManagedPath` in `QuayTools.csproj`.

# 

# After a successful build on Windows, the following files are copied to:

# 

# ```text

# %LOCALAPPDATA%\\Colossal Order\\Cities\_Skylines\\Addons\\Mods\\QuayTools

# ```

# 

# \- `QuayTools.dll`

# \- `UnifiedUILib.dll`

# \- `Icons\\`

# 

# All three must remain together in the mod folder.

# 

# Enable \*\*Quay Tools\*\* in:

# 

# \*\*Content Manager → Mods\*\*

# 

# UnifiedUI is optional, but recommended.

# 

# \## Dependencies

# 

# \### UnifiedUI

# 

# `Lib/UnifiedUILib.dll` is the UnifiedUI helper library.

# 

# \- License: MIT

# \- Copyright: © 2022 UnifiedUI

# 

# The library is shipped alongside the mod DLL so Quay Tools can communicate with the UnifiedUI mod.

# 

# \### Harmony

# 

# Quay Tools references `CitiesHarmony.API`.

# 

# The Harmony library itself is provided by the \*\*Harmony (Mod Dependency)\*\* mod:

# 

# \*\*Workshop ID:\*\* `2040656402`

# 

# The Harmony mod must be:

# 

# \- subscribed to;

# \- enabled;

# \- added as a required item when publishing Quay Tools to the Steam Workshop.

# 

# `HarmonySetup` applies and removes Harmony patches in `OnEnabled` and `OnDisabled`.

# 

# There are currently \*\*no Harmony patches implemented\*\*.

# 

# \## Compatibility

# 

# The current version has been \*\*successfully compiled and tested in-game\*\*.

# 

# The UnifiedUI API calls were verified against the actual `UnifiedUILib.dll` signatures.

# 

# The following Cities: Skylines APIs are used by the project:

# 

# \- `QuayAI` — used for quay detection.

# \- `ToolBase` overrides:

# &#x20; - `OnEnable`

# &#x20; - `OnDisable`

# &#x20; - `OnToolUpdate`

# &#x20; - `RenderOverlay`

# \- `RenderManager.instance.OverlayEffect.DrawBezier(...)`

# \- `NetSegment.CalculateMiddlePoints(...)`

# \- `UIComponent.Awake()` / `Start()`

# \- `UIButton.eventClicked`

# \- `UITextureSprite`

# \- `UIButton.state`

# \- `ToolController.CurrentTool`

# \- `ToolsModifierControl.SetTool<DefaultTool>()`

# 

# \### Tool Registration

# 

# `ToolInstaller` adds the Quay Tools component to the private `m\_tools` array using reflection.

# 

# If the field does not exist, the registration attempt is harmless.

# 

# \### UnifiedUI Icon

# 

# UnifiedUI expects icons to be \*\*64×64 pixels\*\*.

# 

# The included icons use light glyphs on a transparent background.

# 

# \## Project Status

# 

# \*\*v0.0.1\*\*

# 

# The initial release includes:

# 

# \- UnifiedUI integration.

# \- Quay segment inversion.

# \- Quick-flip hotkey.

# \- Connected quay selection and flipping.

# \- Adding fence/wall networks to quay segments.

# \- Removing fences from quay segments.

# \- Fallback floating button when UnifiedUI is unavailable.

