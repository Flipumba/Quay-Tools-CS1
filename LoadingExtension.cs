using ICities;
using UnityEngine;

namespace QuayTools
{
    /// <summary>Sets everything up when a map is loaded and tears it down on unload.</summary>
    public class LoadingExtension : LoadingExtensionBase
    {
        private GameObject _hotkeyHost;

        public override void OnLevelLoaded(LoadMode mode)
        {
            if (mode != LoadMode.NewGame &&
                mode != LoadMode.LoadGame &&
                mode != LoadMode.NewGameFromScenario &&
                mode != LoadMode.NewMap &&
                mode != LoadMode.LoadMap)
            {
                return;
            }

            Debug.Log("[QuayTools] OnLevelLoaded, mode=" + mode);

            try
            {
                Bootstrap.Init();
                Debug.Log("[QuayTools] Bootstrap.Init finished");
            }
            catch (System.Exception ex)
            {
                Debug.LogError("[QuayTools] Init failed: " + ex);
            }

            // Quick-flip hotkey works without the tool; it checks the option itself.
            _hotkeyHost = new GameObject("QuayToolsController");
            _hotkeyHost.AddComponent<QuayToolsController>();
            Debug.Log("[QuayTools] Load finished");
        }

        public override void OnLevelUnloading()
        {
            Debug.Log("[QuayTools] OnLevelUnloading");
            Bootstrap.Shutdown();
            Debug.Log("[QuayTools] Shutdown finished");

            if (_hotkeyHost != null)
            {
                Object.Destroy(_hotkeyHost);
                _hotkeyHost = null;
            }
        }
    }
}
