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
        private static readonly SavedString LangCodeValue;
        private static readonly SavedString LastSeenValue;
        private static readonly SavedString[] FavValues = new SavedString[3];

        /// <summary>Hotkey that activates the Quay Tools (shown/rebindable through UnifiedUI).</summary>
        public static readonly SavedInputKey ActivationKey;

        /// <summary>Hotkey of the quick flip (Shift is reserved: with it held the whole chain is flipped).</summary>
        public static readonly SavedInputKey FlipKey;

        private static readonly SavedBool ShowWhatsNewValue;
        private static readonly SavedBool HideTipsValue;

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
            LangCodeValue = new SavedString("UiLanguageCode", FileName, string.Empty, true);
            LastSeenValue = new SavedString("LastSeenVersion", FileName, string.Empty, true);
            for (int i = 0; i < FavValues.Length; i++) FavValues[i] = new SavedString("Favourites" + i, FileName, string.Empty, true);
            ShowWhatsNewValue = new SavedBool("ShowWhatsNew", FileName, true, true);
            HideTipsValue = new SavedBool("HideTips", FileName, false, true);
            int old = KeyIndex.value;
            if (old < 0 || old >= Keys.Length) old = 0;
            FlipKey = new SavedInputKey("FlipKey", FileName, SavedInputKey.Encode(Keys[old], true, false, false), true);
            ActivationKey = new SavedInputKey(
                "ActivationKey", FileName,
                SavedInputKey.Encode(KeyCode.Q, true, true, false), true);
        }

        /// <summary>All options back to their defaults (favourites and the seen version stay).</summary>
        public static void ResetAll()
        {
            KeyIndex.value = 0;
            AnyNetwork.value = false;
            QuickFlip.value = true;
            Swap.value = false;
            UndoKeys.value = true;
            DecalShadows.value = true;
            MarkEditedValue.value = true;
            IconSizeValue.value = 2;
            HideHighlightValue.value = false;
            LanguageValue.value = 0;
            LangCodeValue.value = "auto";
            ShowWhatsNewValue.value = true;
            HideTipsValue.value = false;
            FlipKey.value = SavedInputKey.Encode(KeyCode.R, true, false, false);
            ActivationKey.value = SavedInputKey.Encode(KeyCode.Q, true, true, false);
        }

        /// <summary>A key binding as a text ("Ctrl + Shift + Q").</summary>
        public static string KeyLabel(SavedInputKey key)
        {
            string s = string.Empty;
            if (key.Control) s += "Ctrl + ";
            if (key.Shift) s += "Shift + ";
            if (key.Alt) s += "Alt + ";
            string name = key.Key.ToString();
            if (name.StartsWith("Alpha")) name = name.Substring(5);
            return s + name;
        }

        /// <summary>The controls help with the current hotkeys put in.</summary>
        public static string HelpText()
        {
            return Loc.T("help_text")
                .Replace("{act}", KeyLabel(ActivationKey))
                .Replace("{flip}", KeyLabel(FlipKey));
        }

        public static bool ShowWhatsNewWindow
        {
            get { return ShowWhatsNewValue.value; }
            set { ShowWhatsNewValue.value = value; }
        }

        public static bool HideTips
        {
            get { return HideTipsValue.value; }
            set { HideTipsValue.value = value; }
        }

        public static bool AllowAnyNetworkSetting
        {
            get { return AnyNetwork.value; }
            set { AnyNetwork.value = value; }
        }

        public static bool SwapLandWaterSetting
        {
            get { return Swap.value; }
            set { Swap.value = value; }
        }

        public static bool UndoHotkeysSetting
        {
            get { return UndoKeys.value; }
            set { UndoKeys.value = value; }
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
            get { return false; } // the option was removed
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
            set { DecalShadows.value = value; }
        }

        /// <summary>Only for old saves: the rendering method used to be one global option (true = textured strip). Now every path has its own switch.</summary>
        public static bool LegacyDecalStrip
        {
            get { return DecalModeValue.value == 1; }
        }

        /// <summary>Decal paths across a sharp bend at a node follow the centre line between the two segment ends.</summary>
        public static bool BridgeCentreLine
        {
            get { return true; } // always on: the switch was removed (it only acts at sharp bends of nodes of other mods)
            set { BridgeCentre.value = true; }
        }

        /// <summary>Size of the floating tool icons above edited segments: 1 (small), 2 (default, twice the former size), 3 (large).</summary>
        public static int MarkIconSize
        {
            get { return Mathf.Clamp(IconSizeValue.value, 1, 3); }
            set { IconSizeValue.value = Mathf.Clamp(value, 1, 3); }
        }

        /// <summary>Interface language: "auto" (the game language) or a code of a language file ("en", "ru", "de"...).</summary>
        public static string UiLanguageCode
        {
            get
            {
                string v = LangCodeValue.value;
                if (string.IsNullOrEmpty(v))
                {
                    // older versions stored 0 = auto, 1 = English, 2 = Russian
                    int old = LanguageValue.value;
                    return old == 1 ? "en" : old == 2 ? "ru" : "auto";
                }
                return v;
            }
            set { LangCodeValue.value = string.IsNullOrEmpty(value) ? "auto" : value; }
        }

        /// <summary>The last version of the mod whose "What's new" window the player has seen (empty on a first installation).</summary>
        public static string LastSeenVersion
        {
            get { return LastSeenValue.value ?? string.Empty; }
            set { LastSeenValue.value = value ?? string.Empty; }
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
            set { MarkEditedValue.value = value; }
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
            set { QuickFlip.value = value; }
        }

        /// <summary>The hotkey of the quick flip as a text ("Ctrl + R").</summary>
        public static string HotkeyLabel
        {
            get
            {
                int i = KeyIndex.value;
                return KeyLabels[i < 0 || i >= KeyLabels.Length ? 0 : i];
            }
        }

        public static void BuildUI(UIHelperBase helper)
        {
            OptionsPage.Build(helper);
        }
    }
}
