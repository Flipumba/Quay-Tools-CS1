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

            HarmonySetup.ApplyLate();

            // walkers whose saved path crosses a blocked segment are sent on their way again
            System.Collections.Generic.List<ushort> blocked = PedStore.Snapshot();
            if (blocked.Count > 0)
            {
                for (int i = 0; i < blocked.Count; i++) PedestrianPathPatch.QueueRepath(blocked[i]);
                ColossalFramework.Singleton<SimulationManager>.instance.AddAction(PedestrianPathPatch.RepathPending);
            }

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
            _hotkeyHost.AddComponent<DecalRenderer>();
            _hotkeyHost.AddComponent<PropLineRenderer>();
            _hotkeyHost.AddComponent<NetLineRenderer>();
            _hotkeyHost.AddComponent<EditedMarkers>();
            Debug.Log("[QuayTools] Load finished");
        }

        public override void OnLevelUnloading()
        {
            Debug.Log("[QuayTools] OnLevelUnloading");
            FenceHeight.Clear();
            History.Clear();
            DecalStore.Clear();
            PropLineStore.Clear();
            LockStore.Clear();
            PedStore.Clear();
            HideStore.Clear();
            NetLineStore.Clear();
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
