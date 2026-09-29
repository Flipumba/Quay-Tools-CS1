using System;
using System.Reflection;
using UnityEngine;

namespace QuayTools
{
    /// <summary>Adds the QuayTool component to the game's ToolController.</summary>
    internal static class ToolInstaller
    {
        public static QuayTool Install()
        {
            ToolController controller = ToolsModifierControl.toolController;
            if (controller == null)
            {
                Debug.LogError("[QuayTools] ToolController not found.");
                return null;
            }

            QuayTool tool = controller.gameObject.GetComponent<QuayTool>();
            if (tool == null)
            {
                tool = controller.gameObject.AddComponent<QuayTool>();
            }

            tool.enabled = false;
            tool.MarkReady();
            TryRegisterInToolList(controller, tool);
            return tool;
        }

        public static void Uninstall(QuayTool tool)
        {
            if (tool == null) return;

            try
            {
                ToolController controller = ToolsModifierControl.toolController;
                if (controller != null && controller.CurrentTool == tool)
                {
                    ToolsModifierControl.SetTool<DefaultTool>();
                }
                if (controller != null) TryUnregisterFromToolList(controller, tool);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[QuayTools] Uninstall cleanup failed: " + ex.Message);
            }

            tool.enabled = false;
            UnityEngine.Object.Destroy(tool);
        }

        private static void TryUnregisterFromToolList(ToolController controller, ToolBase tool)
        {
            FieldInfo field = typeof(ToolController).GetField("m_tools", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (field == null || field.FieldType != typeof(ToolBase[])) return;

            ToolBase[] tools = (ToolBase[])field.GetValue(controller);
            if (tools == null) return;

            int index = Array.IndexOf(tools, tool);
            if (index < 0) return;

            ToolBase[] reduced = new ToolBase[tools.Length - 1];
            for (int i = 0, j = 0; i < tools.Length; i++)
            {
                if (i != index) reduced[j++] = tools[i];
            }
            field.SetValue(controller, reduced);
        }

        /// <summary>
        /// Some game code (ToolsModifierControl.SetTool&lt;T&gt;) looks tools up in a private array.
        /// We add ourselves there if the field exists; failure is harmless because UnifiedUI and
        /// our fallback button set ToolController.CurrentTool directly.
        /// </summary>
        private static void TryRegisterInToolList(ToolController controller, ToolBase tool)
        {
            try
            {
                FieldInfo field = typeof(ToolController).GetField("m_tools", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                if (field == null || field.FieldType != typeof(ToolBase[])) return;

                ToolBase[] tools = (ToolBase[])field.GetValue(controller);
                if (tools == null || Array.IndexOf(tools, tool) >= 0) return;

                ToolBase[] extended = new ToolBase[tools.Length + 1];
                Array.Copy(tools, extended, tools.Length);
                extended[tools.Length] = tool;
                field.SetValue(controller, extended);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[QuayTools] Could not add tool to ToolController list: " + ex.Message);
            }
        }
    }
}
