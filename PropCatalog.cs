using System;
using System.Collections.Generic;
using UnityEngine;

namespace QuayTools
{
    internal class PropCatalogEntry
    {
        public PropInfo Info;
        public string Title;
    }

    /// <summary>The ordinary (non-decal) props that can be placed along a quay.</summary>
    internal static class PropCatalog
    {
        public const int MaxListed = 60; // rows shown in the drop-down list at once (a search narrows the rest)

        private static readonly List<PropCatalogEntry> _entries = new List<PropCatalogEntry>();

        public static List<PropCatalogEntry> Entries
        {
            get { return _entries; }
        }

        public static bool IsUsable(PropInfo info)
        {
            if (info == null || info.m_mesh == null || info.m_material == null) return false;
            if (info.m_requireHeightMap || info.m_requireWaterMap) return false;
            if (DecalCatalog.IsDecal(info)) return false;
            return true;
        }

        public static PropInfo Find(string name)
        {
            return DecalCatalog.Find(name);
        }

        public static void Refresh()
        {
            _entries.Clear();
            try
            {
                int count = PrefabCollection<PropInfo>.LoadedCount();
                for (uint i = 0; i < count; i++)
                {
                    PropInfo info = PrefabCollection<PropInfo>.GetLoaded(i);
                    if (!IsUsable(info)) continue;

                    PropCatalogEntry e = new PropCatalogEntry();
                    e.Info = info;
                    e.Title = TitleOf(info);
                    _entries.Add(e);
                }

                _entries.Sort(delegate (PropCatalogEntry a, PropCatalogEntry b)
                {
                    return string.Compare(a.Title, b.Title, StringComparison.OrdinalIgnoreCase);
                });
            }
            catch (Exception ex)
            {
                Debug.LogError("[QuayTools] Prop catalog refresh failed: " + ex);
            }

            Debug.Log("[QuayTools] Prop catalog: " + _entries.Count + " props");
        }

        public static string TitleOf(PropInfo info)
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

        /// <summary>Entries whose title or prefab name contains the text (empty text: all), at most MaxListed.</summary>
        public static List<PropCatalogEntry> Search(string text)
        {
            List<PropCatalogEntry> result = new List<PropCatalogEntry>();
            string t = (text ?? string.Empty).Trim();
            for (int i = 0; i < _entries.Count && result.Count < MaxListed; i++)
            {
                PropCatalogEntry e = _entries[i];
                if (t.Length == 0 ||
                    e.Title.IndexOf(t, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    e.Info.name.IndexOf(t, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    result.Add(e);
                }
            }
            return result;
        }
    }
}
