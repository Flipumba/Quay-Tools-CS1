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
        private static readonly SavedBool Swap;
        private static readonly SavedBool UndoKeys;
        private static readonly SavedInt DecalModeValue;
        private static readonly SavedBool DecalShadows;

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
            Swap = new SavedBool("SwapLandWater", FileName, false, true);
            UndoKeys = new SavedBool("UndoHotkeys", FileName, true, true);
            DecalModeValue = new SavedInt("DecalMode", FileName, 0, true);
            DecalShadows = new SavedBool("DecalReceiveShadows", FileName, true, true);
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

        /// <summary>Fallback: treat the land side as the water side and vice versa.</summary>
        public static bool SwapLandWater
        {
            get { return Swap.value; }
        }

        /// <summary>Ctrl+Z / Ctrl+Y inside the Quay Tools tool.</summary>
        public static bool UndoHotkeysEnabled
        {
            get { return UndoKeys.value; }
        }

        /// <summary>Plain and textured decal strips use a lit shader so that shadows fall on them (they never cast shadows).</summary>
        public static bool DecalReceiveShadows
        {
            get { return DecalShadows.value; }
        }

        /// <summary>True: decals are placed as real game decal props along the path; false: one textured strip with a composed texture.</summary>
        public static bool DecalPlaced
        {
            get { return DecalModeValue.value != 1; }
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

            group.AddCheckbox("Swap land/water sides for fences (use only if fences go to the wrong side everywhere)", Swap.value, delegate (bool isChecked)
            {
                Swap.value = isChecked;
            });

            group.AddCheckbox("Ctrl+Z / Ctrl+Y undo and redo while the Quay Tools window is open (turn off if it clashes with another undo mod)", UndoKeys.value, delegate (bool isChecked)
            {
                UndoKeys.value = isChecked;
            });

            string[] decalModes =
            {
                "Game decals placed step by step along the path (no mask cropping)",
                "One textured strip with a composed texture (mask cropping, unlit)"
            };
            group.AddDropdown("Decal path rendering", decalModes, DecalModeValue.value == 1 ? 1 : 0, delegate (int sel)
            {
                DecalModeValue.value = sel;
            });

            group.AddCheckbox("Decal strips receive shadows (lit shader; turn off for the unlit look if the colours look wrong)", DecalShadows.value, delegate (bool isChecked)
            {
                DecalShadows.value = isChecked;
            });

            group.AddCheckbox("Allow tools on any network segment (not only quays)", AnyNetwork.value, delegate (bool isChecked)
            {
                AnyNetwork.value = isChecked;
            });
        }
    }
}
