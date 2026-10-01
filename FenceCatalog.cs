using System;
using System.Collections.Generic;
using ColossalFramework;
using UnityEngine;

namespace QuayTools
{
    internal class FenceEntry
    {
        public NetInfo Info;
        public string Title;
    }

    /// <summary>
    /// The list of network models the player can use as fences.
    /// The game marks fence/wall networks with DecorationWallAI (checked in the vanilla fence tool).
    /// </summary>
    internal static class FenceCatalog
    {
        private static readonly List<FenceEntry> _entries = new List<FenceEntry>();

        public static List<FenceEntry> Entries
        {
            get { return _entries; }
        }

        private static readonly Dictionary<string, NetInfo> _byName = new Dictionary<string, NetInfo>();

        /// <summary>A network model by its prefab name (null when it is not loaded).</summary>
        public static NetInfo Find(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            NetInfo info;
            if (_byName.TryGetValue(name, out info) && info != null) return info;
            try
            {
                info = PrefabCollection<NetInfo>.FindLoaded(name);
            }
            catch (Exception)
            {
                info = null;
            }
            if (info != null) _byName[name] = info;
            return info;
        }

        public static void Refresh()
        {
            _entries.Clear();

            try
            {
                int count = PrefabCollection<NetInfo>.LoadedCount();
                for (uint i = 0; i < count; i++)
                {
                    NetInfo info = PrefabCollection<NetInfo>.GetLoaded(i);
                    if (info == null) continue;
                    if (!(info.m_netAI is DecorationWallAI)) continue;

                    // Available in the current game mode and unlocked for the player
                    if ((info.m_availableIn & ToolsModifierControl.toolController.m_mode) == 0) continue;
                    if (info.m_UnlockMilestone != null && !ToolsModifierControl.IsUnlocked(info.m_UnlockMilestone)) continue;

                    FenceEntry entry = new FenceEntry();
                    entry.Info = info;
                    entry.Title = GetTitle(info);
                    _entries.Add(entry);
                }

                _entries.Sort(delegate (FenceEntry a, FenceEntry b)
                {
                    return string.Compare(a.Title, b.Title, StringComparison.OrdinalIgnoreCase);
                });
            }
            catch (Exception ex)
            {
                Debug.LogError("[QuayTools] Fence catalog refresh failed: " + ex);
            }

            Debug.Log("[QuayTools] Fence catalog: " + _entries.Count + " network models");
        }

        private static string GetTitle(NetInfo info)
        {
            string title = null;
            try
            {
                title = info.GetUncheckedLocalizedTitle();
            }
            catch (Exception)
            {
                // fall back to the prefab name below
            }

            if (string.IsNullOrEmpty(title)) title = info.name;
            return title;
        }
    }
}
