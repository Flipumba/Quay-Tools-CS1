using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace QuayTools
{
    /// <summary>Settings of one decal path (a coloured strip along the middle of a quay segment).</summary>
    internal class DecalSettings
    {
        public const int DefaultWidth = 20; // 2.0 m
        public const int DefaultScale = 80; // 8.0 m: width of one repeat of the decal texture
        public const int MinScale = 5;      // 0.5 m
        public const int MaxScale = 1000;   // 100 m

        public int Width = DefaultWidth; // units of FenceStore.Unit (0.1 m)
        public int Lateral;              // sideways shift from the middle of the quay, right of start->end positive
        public int Lift;                 // height above the quay surface
        public int ColorIndex;         // tint of a decal, fill colour of a solid path
        public string Prop;            // name of the decal prop (PropInfo); null = plain coloured strip
        public int Scale = DefaultScale; // world width of one repeat of the decal texture, units of 0.1 m

        public DecalSettings Clone()
        {
            return (DecalSettings)MemberwiseClone();
        }

        public void ResetToDefaults()
        {
            Width = DefaultWidth;
            Lateral = 0;
            Lift = 0;
            ColorIndex = 0;
            Scale = DefaultScale;
        }

        public bool SameAs(DecalSettings o)
        {
            return o != null && Width == o.Width && Lateral == o.Lateral && Lift == o.Lift && ColorIndex == o.ColorIndex &&
                   Scale == o.Scale && Prop == o.Prop;
        }
    }

    /// <summary>Thread-safe store of decal paths keyed by segment id, saved in the savegame.</summary>
    internal static class DecalStore
    {
        public const int MinScale = DecalSettings.MinScale;
        public const int MaxScale = DecalSettings.MaxScale;
        public const int MinWidth = 1;    // 0.1 m
        public const int MaxWidth = 500;  // 50 m

        public static readonly Color[] Colors =
        {
            new Color(1.00f, 1.00f, 1.00f, 1.00f), // white (a decal keeps its own colours)
            new Color(0.98f, 0.82f, 0.10f, 0.96f), // yellow
            new Color(0.85f, 0.15f, 0.12f, 0.96f), // red
            new Color(0.15f, 0.42f, 0.85f, 0.96f), // blue
            new Color(0.50f, 0.50f, 0.50f, 0.96f), // grey
            new Color(0.08f, 0.08f, 0.08f, 0.96f)  // black
        };

        private const int FormatVersion = 2;
        private static readonly Dictionary<ushort, DecalSettings> Map = new Dictionary<ushort, DecalSettings>();

        /// <summary>Raised (flag) whenever the content changes; the renderer rebuilds its meshes.</summary>
        public static volatile int Version;

        public static bool TryGet(ushort segment, out DecalSettings s)
        {
            lock (Map)
            {
                return Map.TryGetValue(segment, out s);
            }
        }

        public static bool Has(ushort segment)
        {
            lock (Map)
            {
                return Map.ContainsKey(segment);
            }
        }

        public static void Set(ushort segment, DecalSettings s)
        {
            lock (Map)
            {
                if (s == null) Map.Remove(segment);
                else Map[segment] = s;
            }
            Version++;
        }

        public static void Remove(ushort segment)
        {
            Set(segment, null);
        }

        public static void Clear()
        {
            lock (Map)
            {
                Map.Clear();
            }
            Version++;
        }

        /// <summary>Copy of the current entries (safe to iterate on any thread).</summary>
        public static List<KeyValuePair<ushort, DecalSettings>> Snapshot()
        {
            lock (Map)
            {
                return new List<KeyValuePair<ushort, DecalSettings>>(Map);
            }
        }

        // ---------- savegame ----------

        public static byte[] Save()
        {
            using (MemoryStream ms = new MemoryStream())
            using (BinaryWriter w = new BinaryWriter(ms))
            {
                lock (Map)
                {
                    NetSegment[] segs = NetManager.instance.m_segments.m_buffer;
                    List<ushort> keys = new List<ushort>();
                    foreach (KeyValuePair<ushort, DecalSettings> kv in Map)
                    {
                        if ((segs[kv.Key].m_flags & NetSegment.Flags.Created) != NetSegment.Flags.None) keys.Add(kv.Key);
                    }

                    w.Write(FormatVersion);
                    w.Write(keys.Count);
                    for (int i = 0; i < keys.Count; i++)
                    {
                        DecalSettings s = Map[keys[i]];
                        w.Write((int)keys[i]);
                        w.Write(s.Width);
                        w.Write(s.Lateral);
                        w.Write(s.Lift);
                        w.Write(s.ColorIndex);
                        w.Write(s.Prop ?? string.Empty);
                        w.Write(s.Scale);
                    }
                }
                w.Flush();
                return ms.ToArray();
            }
        }

        public static void Load(byte[] data)
        {
            Clear();
            if (data == null || data.Length < 8) return;

            try
            {
                using (MemoryStream ms = new MemoryStream(data))
                using (BinaryReader r = new BinaryReader(ms))
                {
                    int version = r.ReadInt32();
                    if (version < 1 || version > FormatVersion) return;

                    int count = r.ReadInt32();
                    lock (Map)
                    {
                        for (int i = 0; i < count; i++)
                        {
                            ushort id = (ushort)r.ReadInt32();
                            DecalSettings s = new DecalSettings();
                            s.Width = Mathf.Clamp(r.ReadInt32(), MinWidth, MaxWidth);
                            s.Lateral = Mathf.Clamp(r.ReadInt32(), -FenceStore.MaxUnits, FenceStore.MaxUnits);
                            s.Lift = Mathf.Clamp(r.ReadInt32(), -FenceStore.MaxUnits, FenceStore.MaxUnits);
                            s.ColorIndex = Mathf.Clamp(r.ReadInt32(), 0, Colors.Length - 1);
                            if (version >= 2)
                            {
                                string prop = r.ReadString();
                                s.Prop = string.IsNullOrEmpty(prop) ? null : prop;
                                s.Scale = Mathf.Clamp(r.ReadInt32(), MinScale, MaxScale);
                            }
                            Map[id] = s;
                        }
                    }
                    Version++;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[QuayTools] Could not read saved decal paths: " + ex.Message);
                Clear();
            }
        }
    }
}
