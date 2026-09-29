using ICities;

namespace QuayTools
{
    /// <summary>Entry point: name, description and options page.</summary>
    public class ModInfo : IUserMod
    {
        public static ModInfo Instance { get; private set; }

        public ModInfo()
        {
            Instance = this;
        }

        public string Name
        {
            get { return "Quay Tools"; }
        }

        public string Description
        {
            get { return "Quay Tools: flip quay direction with a highlighted cursor tool. Works with UnifiedUI."; }
        }

        // Called by the game when the mod is enabled/disabled in Content Manager.
        public void OnEnabled()
        {
            HarmonySetup.Apply();
        }

        public void OnDisabled()
        {
            HarmonySetup.Revert();
        }

        public void OnSettingsUI(UIHelperBase helper)
        {
            Settings.BuildUI(helper);
        }
    }
}
