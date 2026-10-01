using System;
using System.Collections.Generic;
using UnityEngine;

namespace QuayTools
{
    /// <summary>A prop or a tree that can be placed along a quay. Key is what is stored: the prefab name, trees get a prefix.</summary>
    internal class PropCatalogEntry
    {
        public PropInfo Info;   // set for props
        public TreeInfo Tree;   // set for trees
        public string Title;
        public string Key;
    }

    /// <summary>The ordinary (non-decal) props and the trees that can be placed along a quay.</summary>
    internal static class PropCatalog
    {
        public const string TreePrefix = "tree:";

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

        public static bool IsUsable(TreeInfo info)
        {
            return info != null && info.m_mesh != null && info.m_material != null;
        }

        public static bool IsTreeKey(string key)
        {
            return key != null && key.StartsWith(TreePrefix, StringComparison.Ordinal);
        }

        public static string KeyOf(PropInfo info)
        {
            return info.name;
        }

        public static string KeyOf(TreeInfo info)
        {
            return TreePrefix + info.name;
        }

        public static PropInfo Find(string name)
        {
            return DecalCatalog.Find(name);
        }

        public static TreeInfo FindTree(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            try
            {
                return PrefabCollection<TreeInfo>.FindLoaded(name);
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>Finds what a stored key stands for (a prop or a tree). False when the asset is missing or unusable.</summary>
        public static bool Resolve(string key, out PropInfo prop, out TreeInfo tree)
        {
            prop = null;
            tree = null;
            if (string.IsNullOrEmpty(key)) return false;

            if (IsTreeKey(key))
            {
                tree = FindTree(key.Substring(TreePrefix.Length));
                if (!IsUsable(tree))
                {
                    tree = null;
                    return false;
                }
                return true;
            }

            prop = Find(key);
            if (!IsUsable(prop))
            {
                prop = null;
                return false;
            }
            return true;
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
                    e.Key = KeyOf(info);
                    e.Title = TitleOf(info);
                    _entries.Add(e);
                }

                int trees = PrefabCollection<TreeInfo>.LoadedCount();
                for (uint i = 0; i < trees; i++)
                {
                    TreeInfo info = PrefabCollection<TreeInfo>.GetLoaded(i);
                    if (!IsUsable(info)) continue;

                    PropCatalogEntry e = new PropCatalogEntry();
                    e.Tree = info;
                    e.Key = KeyOf(info);
                    e.Title = Loc.T("tree_tag") + TitleOf(info);
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

            Debug.Log("[QuayTools] Prop catalog: " + _entries.Count + " props and trees");
        }

        public static string TitleOf(PrefabInfo info)
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

        /// <summary>The title shown for a stored key (props: title, trees: tag + title); the key itself when the asset is missing.</summary>
        public static string TitleOfKey(string key)
        {
            PropInfo p;
            TreeInfo t;
            if (!Resolve(key, out p, out t)) return key;
            return t != null ? Loc.T("tree_tag") + TitleOf(t) : TitleOf(p);
        }

        /// <summary>All entries whose title or prefab name contains the text (empty text: all).</summary>
        public static List<PropCatalogEntry> Search(string text)
        {
            List<PropCatalogEntry> result = new List<PropCatalogEntry>();
            string t = (text ?? string.Empty).Trim();
            for (int i = 0; i < _entries.Count; i++)
            {
                PropCatalogEntry e = _entries[i];
                if (t.Length == 0 ||
                    e.Title.IndexOf(t, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    e.Key.IndexOf(t, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    result.Add(e);
                }
            }
            return result;
        }
    }
}
