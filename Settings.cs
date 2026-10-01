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
        private static readonly SavedBool BridgeCentre;
        private static readonly SavedBool MarkEditedValue;
        private static readonly SavedInt IconSizeValue;
        private static readonly SavedBool HideHighlightValue;
        private static readonly SavedInt LanguageValue;
        private static readonly SavedString[] FavValues = new SavedString[3];

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
            BridgeCentre = new SavedBool("BridgeCentreLine", FileName, true, true);
            MarkEditedValue = new SavedBool("MarkEdited", FileName, true, true);
            IconSizeValue = new SavedInt("MarkIconSize", FileName, 2, true);
            HideHighlightValue = new SavedBool("HideHighlightUi", FileName, false, true);
            LanguageValue = new SavedInt("UiLanguage", FileName, 0, true);
            for (int i = 0; i < FavValues.Length; i++) FavValues[i] = new SavedString("Favourites" + i, FileName, string.Empty, true);
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

        /// <summary>Only for old saves: the rendering method used to be one global option (true = textured strip). Now every path has its own switch.</summary>
        public static bool LegacyDecalStrip
        {
            get { return DecalModeValue.value == 1; }
        }

        /// <summary>Decal paths across a sharp bend at a node follow the centre line between the two segment ends.</summary>
        public static bool BridgeCentreLine
        {
            get { return BridgeCentre.value; }
        }

        /// <summary>Size of the floating tool icons above edited segments: 1 (small), 2 (default, twice the former size), 3 (large).</summary>
        public static int MarkIconSize
        {
            get { return Mathf.Clamp(IconSizeValue.value, 1, 3); }
        }

        /// <summary>Interface language: 0 = the game language, 1 = English, 2 = Russian.</summary>
        public static int UiLanguage
        {
            get { return Mathf.Clamp(LanguageValue.value, 0, 2); }
            set { LanguageValue.value = Mathf.Clamp(value, 0, 2); }
        }

        /// <summary>Hide the highlight of the quay borders and lines while a slider is being dragged.</summary>
        public static bool HideHighlightUi
        {
            get { return HideHighlightValue.value; }
            set { HideHighlightValue.value = value; }
        }

        /// <summary>While the tool is active, segments edited by the mod are highlighted and carry tool icons.</summary>
        public static bool MarkEdited
        {
            get { return MarkEditedValue.value; }
        }

        /// <summary>Favourite list of a drop-down kind (see Favorites), items separated by '|'.</summary>
        public static string GetFavorites(int kind)
        {
            return kind >= 0 && kind < FavValues.Length ? FavValues[kind].value : string.Empty;
        }

        public static void SetFavorites(int kind, string value)
        {
            if (kind >= 0 && kind < FavValues.Length) FavValues[kind].value = value ?? string.Empty;
        }

        public static bool QuickFlipEnabled
        {
            get { return QuickFlip.value; }
        }

        public static void BuildUI(UIHelperBase helper)
        {
            UIHelperBase group = helper.AddGroup("Quay Tools");

            group.AddDropdown(Loc.T("opt_lang"), new string[] { Loc.T("opt_lang_auto"), "English", "Русский" }, UiLanguage, delegate (int sel)
            {
                UiLanguage = sel;
            });

            group.AddCheckbox(Loc.T("opt_quickflip"), QuickFlip.value, delegate (bool isChecked)
            {
                QuickFlip.value = isChecked;
            });

            int current = KeyIndex.value;
            if (current < 0 || current >= Keys.Length) current = 0;

            group.AddDropdown(Loc.T("opt_hotkey"), KeyLabels, current, delegate (int sel)
            {
                KeyIndex.value = sel;
            });

            group.AddCheckbox(Loc.T("opt_swap"), Swap.value, delegate (bool isChecked)
            {
                Swap.value = isChecked;
            });

            group.AddCheckbox(Loc.T("opt_undokeys"), UndoKeys.value, delegate (bool isChecked)
            {
                UndoKeys.value = isChecked;
            });

            group.AddCheckbox(Loc.T("opt_shadows"), DecalShadows.value, delegate (bool isChecked)
            {
                DecalShadows.value = isChecked;
            });

            group.AddCheckbox(Loc.T("opt_bridge"), BridgeCentre.value, delegate (bool isChecked)
            {
                BridgeCentre.value = isChecked;
            });

            group.AddCheckbox(Loc.T("opt_mark"), MarkEditedValue.value, delegate (bool isChecked)
            {
                MarkEditedValue.value = isChecked;
            });

            group.AddDropdown(Loc.T("opt_iconsize"), new string[] { Loc.T("opt_icon1"), Loc.T("opt_icon2"), Loc.T("opt_icon3") }, MarkIconSize - 1, delegate (int sel)
            {
                IconSizeValue.value = sel + 1;
            });

            group.AddCheckbox(Loc.T("opt_anynet"), AnyNetwork.value, delegate (bool isChecked)
            {
                AnyNetwork.value = isChecked;
            });
        }
    }
}
