using System;
using System.Collections.Generic;
using ColossalFramework;
using UnityEngine;

namespace QuayTools
{
    internal class DecalEntry
    {
        public PropInfo Info;
        public string Title;
        public float NaturalSize; // metres: width of the decal mesh (one repeat of the texture)
    }

    /// <summary>
    /// The decal props (ground decals) that can be laid along a quay. A decal prop is a flat mesh with a decal shader;
    /// we reuse its material and draw our own mesh with it, so every decal asset the player has works.
    /// Decals that need the terrain height map are left out (they only work at their own spot).
    /// </summary>
    internal static class DecalCatalog
    {
        private static readonly List<DecalEntry> _entries = new List<DecalEntry>();

        public static List<DecalEntry> Entries
        {
            get { return _entries; }
        }

        public static bool IsDecal(PropInfo info)
        {
            if (info == null || info.m_mesh == null || info.m_material == null) return false;
            if (info.m_requireHeightMap || info.m_requireWaterMap) return false;

            if (info.m_isDecal) return true;
            Shader sh = info.m_material.shader;
            return sh != null && sh.name.IndexOf("Decal", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public static PropInfo Find(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            try
            {
                return PrefabCollection<PropInfo>.FindLoaded(name);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static void Refresh()
        {
            _entries.Clear();
            int shaderLogged = 0;

            try
            {
                int count = PrefabCollection<PropInfo>.LoadedCount();
                for (uint i = 0; i < count; i++)
                {
                    PropInfo info = PrefabCollection<PropInfo>.GetLoaded(i);
                    if (!IsDecal(info)) continue;

                    DecalEntry e = new DecalEntry();
                    e.Info = info;
                    e.Title = GetTitle(info);

                    Vector3 size = info.m_mesh.bounds.size;
                    e.NaturalSize = Mathf.Max(size.x, 0.5f);
                    _entries.Add(e);

                    if (shaderLogged++ < 3) Debug.Log("[QuayTools] Decal example: " + info.name + " shader=" + info.m_material.shader.name + " mesh size=" + size);
                }

                _entries.Sort(delegate (DecalEntry a, DecalEntry b)
                {
                    return string.Compare(a.Title, b.Title, StringComparison.OrdinalIgnoreCase);
                });
            }
            catch (Exception ex)
            {
                Debug.LogError("[QuayTools] Decal catalog refresh failed: " + ex);
            }

            Debug.Log("[QuayTools] Decal catalog: " + _entries.Count + " decal props");
        }

        private static string GetTitle(PropInfo info)
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
