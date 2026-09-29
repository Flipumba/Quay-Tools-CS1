using System;
using ColossalFramework.UI;
using UnifiedUI.Helpers;
using UnityEngine;

namespace QuayTools
{
    /// <summary>
    /// Installs the tool and its launcher button: UnifiedUI if it is enabled,
    /// otherwise a small floating button.
    /// </summary>
    internal static class Bootstrap
    {
        private static QuayTool _tool;
        private static UIComponent _uuiButton;
        private static UIButton _fallbackButton;

        public static void Init()
        {
            Debug.Log("[QuayTools] Installing tool");
            _tool = ToolInstaller.Install();
            if (_tool == null) return;

            Debug.Log("[QuayTools] Tool installed, loading icon");
            Texture2D icon = ModPaths.LoadIcon("QuayTools.png");

            Debug.Log("[QuayTools] Registering UnifiedUI button");
            if (icon != null && TryRegisterUUI(icon)) return;

            Debug.Log("[QuayTools] Creating fallback button");
            CreateFallbackButton(icon);
        }

        public static void Shutdown()
        {
            try
            {
                if (_uuiButton != null) UUIHelpers.Destroy(_uuiButton);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[QuayTools] UUI cleanup failed: " + ex.Message);
            }
            _uuiButton = null;

            if (_fallbackButton != null)
            {
                UnityEngine.Object.Destroy(_fallbackButton.gameObject);
                _fallbackButton = null;
            }

            QuayToolPanel.DestroyPanel();
            ToolInstaller.Uninstall(_tool);
            _tool = null;
        }

        private static bool TryRegisterUUI(Texture2D icon)
        {
            try
            {
                UUIHotKeys hotkeys = new UUIHotKeys();
                hotkeys.ActivationKey = Settings.ActivationKey;

                _uuiButton = UUIHelpers.RegisterToolButton(
                    "Quay Tools", "Quay Tools", "Quay Tools", _tool, icon, hotkeys);

                Debug.Log("[QuayTools] UnifiedUI registration result: " + (_uuiButton != null ? "ok" : "null"));
                return _uuiButton != null;
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[QuayTools] UnifiedUI registration failed, using fallback button: " + ex);
                return false;
            }
        }

        private static void CreateFallbackButton(Texture2D icon)
        {
            UIView view = UIView.GetAView();
            if (view == null) return;

            UIButton button = view.AddUIComponent(typeof(UIButton)) as UIButton;
            if (button == null) return;

            button.size = new Vector2(40f, 40f);
            button.absolutePosition = new Vector3(10f, 110f);
            button.normalBgSprite = "ButtonMenu";
            button.hoveredBgSprite = "ButtonMenuHovered";
            button.pressedBgSprite = "ButtonMenuPressed";
            button.focusedBgSprite = "ButtonMenuFocused";
            button.tooltip = "Quay Tools";
            if (icon == null) button.text = "Q";

            if (icon != null)
            {
                UITextureSprite sprite = button.AddUIComponent<UITextureSprite>();
                sprite.texture = icon;
                sprite.size = new Vector2(30f, 30f);
                sprite.relativePosition = new Vector3(5f, 5f);
                sprite.isInteractive = false;
            }

            button.eventClicked += delegate (UIComponent c, UIMouseEventParameter p) { ToggleTool(); };
            _fallbackButton = button;
        }

        private static void ToggleTool()
        {
            if (_tool == null) return;

            ToolController controller = ToolsModifierControl.toolController;
            if (controller.CurrentTool == _tool)
            {
                ToolsModifierControl.SetTool<DefaultTool>();
            }
            else
            {
                controller.CurrentTool = _tool;
            }
        }
    }
}
