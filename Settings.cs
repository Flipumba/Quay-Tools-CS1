using ColossalFramework;
using ICities;
using UnityEngine;

namespace QuayTools
{
    /// <summary>Persistent options stored in QuayTools.cgs (game settings file).</summary>
    internal static class Settings
    {
        private const string FileName = "QuayTools";

        // Quick-flip hotkey is always Ctrl + one of these keys.
        private static readonly KeyCode[] Keys = { KeyCode.R, KeyCode.F, KeyCode.G, KeyCode.B, KeyCode.K };
        private static readonly string[] KeyLabels = { "Ctrl + R", "Ctrl + F", "Ctrl + G", "Ctrl + B", "Ctrl + K" };

        private static readonly SavedInt KeyIndex;
        private static readonly SavedBool AnyNetwork;
        private static readonly SavedBool QuickFlip;

        /// <summary>Hotkey that activates the Quay Tools (shown/rebindable through UnifiedUI).</summary>
        public static readonly SavedInputKey ActivationKey;

        static Settings()
        {
            if (GameSettings.FindSettingsFileByName(FileName) == null)
            {
                GameSettings.AddSettingsFile(new SettingsFile { fileName = FileName });
            }

            KeyIndex = new SavedInt("HotkeyIndex", FileName, 0, true);
            AnyNetwork = new SavedBool("AllowAnyNetwork", FileName, false, true);
            QuickFlip = new SavedBool("QuickFlipEnabled", FileName, true, true);
            ActivationKey = new SavedInputKey(
                "ActivationKey", FileName,
                SavedInputKey.Encode(KeyCode.Q, true, true, false), true);
        }

        public static KeyCode Hotkey
        {
            get
            {
                int i = KeyIndex.value;
                if (i < 0 || i >= Keys.Length) i = 0;
                return Keys[i];
            }
        }

        public static bool AllowAnyNetwork
        {
            get { return AnyNetwork.value; }
        }

        public static bool QuickFlipEnabled
        {
            get { return QuickFlip.value; }
        }

        public static void BuildUI(UIHelperBase helper)
        {
            UIHelperBase group = helper.AddGroup("Quay Tools");

            group.AddCheckbox("Enable quick-flip hotkey (Ctrl + key over a quay, no tool needed)", QuickFlip.value, delegate (bool isChecked)
            {
                QuickFlip.value = isChecked;
            });

            int current = KeyIndex.value;
            if (current < 0 || current >= Keys.Length) current = 0;

            group.AddDropdown("Quick-flip hotkey", KeyLabels, current, delegate (int sel)
            {
                KeyIndex.value = sel;
            });

            group.AddCheckbox("Allow tools on any network segment (not only quays)", AnyNetwork.value, delegate (bool isChecked)
            {
                AnyNetwork.value = isChecked;
            });
        }
    }
}
