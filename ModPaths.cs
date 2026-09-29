using System.IO;
using ColossalFramework.Plugins;
using ICities;
using UnityEngine;
using UnifiedUI.Helpers;

namespace QuayTools
{
    /// <summary>Finds the mod folder and loads icon textures from its Icons subfolder.</summary>
    internal static class ModPaths
    {
        private static string _dir;

        public static string ModDirectory
        {
            get
            {
                if (_dir != null) return _dir;

                try
                {
                    foreach (PluginManager.PluginInfo info in PluginManager.instance.GetPluginsInfo())
                    {
                        foreach (IUserMod mod in info.GetInstances<IUserMod>())
                        {
                            if (mod is ModInfo)
                            {
                                _dir = info.modPath;
                                return _dir;
                            }
                        }
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning("[QuayTools] Could not resolve mod path: " + ex.Message);
                }

                _dir = string.Empty;
                return _dir;
            }
        }

        /// <summary>Loads Icons/&lt;fileName&gt;. Returns null on any failure.</summary>
        public static Texture2D LoadIcon(string fileName)
        {
            try
            {
                string path = Path.Combine(Path.Combine(ModDirectory, "Icons"), fileName);
                if (!File.Exists(path))
                {
                    Debug.LogWarning("[QuayTools] Icon not found: " + path);
                    return null;
                }
                return UUIHelpers.LoadTexture(path);
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning("[QuayTools] Icon load failed (" + fileName + "): " + ex.Message);
                return null;
            }
        }
    }
}
