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
        public const int MaxStep = 1000;    // 100 m; 0 = automatic (one tile length)
        public const int DefaultBox = 80;   // 8.0 m: height of the projection box of placed decals
        public const int MinBox = 5;        // 0.5 m
        public const int MaxBox = 500;      // 50 m
        public const int MaxShift = 500;    // 50 m (start / end trim)
        public const int StripLift = 10;    // 1.0 m: default height of a plane (the plane lies lower than the quay surface)

        public int Width = DefaultWidth; // units of FenceStore.Unit (0.1 m)
        public int Lateral;              // sideways shift from the middle of the quay, right of start->end positive
        public int Lift;                 // height above the quay surface
        public int ShiftX;               // units of 0.1 m: the texture / tiles slide along the path (the ends of the path stay)
        public byte R = 255, G = 255, B = 255, A = 255; // tint of a decal (multiplies its colours and opacity), fill colour of a plain path
        public string Prop;            // name of the decal prop (PropInfo); null = plain coloured strip
        public int Scale = DefaultScale; // world width of one repeat of the decal texture, units of 0.1 m
        public int Step;                 // distance between placed decal tiles along the path, units of 0.1 m; 0 = one tile length
        public int Box = DefaultBox;     // height (thickness) of the projection box of placed decals, units of 0.1 m
        public bool Strip;               // plane mode: one textured strip with a composed texture instead of placed game decals (chosen when the path is added)
        public int StartShift, EndShift; // units of 0.1 m, -MaxShift..0: how much the path is shortened at that end
        public int ColorMul;             // 0..10 (tenths): 0 = the tint works as the game applies it, 10 = the texture is fully multiplied by the colour (placed decals)

        public Color TintColor
        {
            get { return new Color32(R, G, B, A); }
        }

        public DecalSettings Clone()
        {
            return (DecalSettings)MemberwiseClone();
        }

        public void ResetToDefaults()
        {
            Width = DefaultWidth;
            Lateral = 0;
            ShiftX = 0;
            Lift = Strip ? StripLift : 0;
            StartShift = 0;
            EndShift = 0;
            R = 255;
            G = 255;
            B = 255;
            A = 255;
            Scale = DefaultScale;
            Step = 0;
            Box = DefaultBox;
            ColorMul = 0;
        }

        public bool SameAs(DecalSettings o)
        {
            return o != null && Width == o.Width && Lateral == o.Lateral && ShiftX == o.ShiftX && Lift == o.Lift && R == o.R && G == o.G && B == o.B && A == o.A &&
                   Scale == o.Scale && Step == o.Step && Box == o.Box && Prop == o.Prop && Strip == o.Strip &&
                   StartShift == o.StartShift && EndShift == o.EndShift && ColorMul == o.ColorMul;
        }
    }

    /// <summary>All texture paths of one segment.</summary>
    internal class DecalSet
    {
        public readonly List<DecalSettings> Paths = new List<DecalSettings>();

        public DecalSet Clone()
        {
            DecalSet c = new DecalSet();
            for (int i = 0; i < Paths.Count; i++) c.Paths.Add(Paths[i].Clone());
            return c;
        }

        public bool SameAs(DecalSet o)
        {
            if (o == null || o.Paths.Count != Paths.Count) return false;
            for (int i = 0; i < Paths.Count; i++)
            {
                if (!Paths[i].SameAs(o.Paths[i])) return false;
            }
            return true;
        }
    }

    /// <summary>Thread-safe store of texture paths (any number per segment) keyed by segment id, saved in the savegame.</summary>
    internal static class DecalStore
    {
        public const int MinScale = DecalSettings.MinScale;
        public const int MaxScale = DecalSettings.MaxScale;
        public const int MaxStep = DecalSettings.MaxStep;
        public const int MinBox = DecalSettings.MinBox;
        public const int MaxBox = DecalSettings.MaxBox;
        public const int MinWidth = 1;    // 0.1 m
        public const int MaxWidth = 500;  // 50 m
        public const int MaxShift = DecalSettings.MaxShift;
        public const int MaxPathsPerSegment = 15;

        // palette of the old versions (format 2 saves stored an index)
        private static readonly Color32[] LegacyColors =
        {
            new Color32(255, 255, 255, 255), // white
            new Color32(250, 209, 26, 245),  // yellow
            new Color32(217, 38, 31, 245),   // red
            new Color32(38, 107, 217, 245),  // blue
            new Color32(128, 128, 128, 245), // grey
            new Color32(20, 20, 20, 245)     // black
        };

        private const int FormatVersion = 8;
        private static readonly Dictionary<ushort, DecalSet> Map = new Dictionary<ushort, DecalSet>();

        /// <summary>Raised (flag) whenever the content changes; the renderer rebuilds its meshes.</summary>
        public static volatile int Version;

        public static bool TryGet(ushort segment, out DecalSet set)
        {
            lock (Map)
            {
                return Map.TryGetValue(segment, out set);
            }
        }

        public static List<ushort> Keys()
        {
            lock (Map)
            {
                return new List<ushort>(Map.Keys);
            }
        }

        public static bool Has(ushort segment)
        {
            lock (Map)
            {
                return Map.ContainsKey(segment);
            }
        }

        /// <summary>Replaces (or, with null or an empty set, removes) the paths of a segment.</summary>
        public static void Set(ushort segment, DecalSet set)
        {
            lock (Map)
            {
                if (set == null || set.Paths.Count == 0) Map.Remove(segment);
                else Map[segment] = set;
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
        public static List<KeyValuePair<ushort, DecalSet>> Snapshot()
        {
            List<KeyValuePair<ushort, DecalSet>> list = new List<KeyValuePair<ushort, DecalSet>>();
            lock (Map)
            {
                foreach (KeyValuePair<ushort, DecalSet> kv in Map)
                {
                    list.Add(new KeyValuePair<ushort, DecalSet>(kv.Key, kv.Value.Clone()));
                }
            }
            return list;
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
                    foreach (KeyValuePair<ushort, DecalSet> kv in Map)
                    {
                        if ((segs[kv.Key].m_flags & NetSegment.Flags.Created) != NetSegment.Flags.None) keys.Add(kv.Key);
                    }

                    w.Write(FormatVersion);
                    w.Write(keys.Count);
                    for (int i = 0; i < keys.Count; i++)
                    {
                        DecalSet set = Map[keys[i]];
                        w.Write((int)keys[i]);
                        w.Write(set.Paths.Count);
                        for (int k = 0; k < set.Paths.Count; k++)
                        {
                            DecalSettings s = set.Paths[k];
                            w.Write(s.Width);
                            w.Write(s.Lateral);
                            w.Write(s.Lift);
                            w.Write(s.R);
                            w.Write(s.G);
                            w.Write(s.B);
                            w.Write(s.A);
                            w.Write(s.Prop ?? string.Empty);
                            w.Write(s.Scale);
                            w.Write(s.Step);
                            w.Write(s.Box);
                            w.Write(s.Strip);
                            w.Write(s.StartShift);
                            w.Write(s.EndShift);
                            w.Write(s.ShiftX);
                            w.Write(s.ColorMul);
                        }
                    }
                }
                w.Flush();
                return ms.ToArray();
            }
        }

        private static DecalSettings ReadOne(BinaryReader r, int version)
        {
            DecalSettings s = new DecalSettings();
            s.Width = Mathf.Clamp(r.ReadInt32(), MinWidth, MaxWidth);
            s.Lateral = Mathf.Clamp(r.ReadInt32(), -FenceStore.MaxUnits, FenceStore.MaxUnits);
            s.Lift = Mathf.Clamp(r.ReadInt32(), -FenceStore.MaxUnits, FenceStore.MaxUnits);
            if (version >= 3)
            {
                s.R = r.ReadByte();
                s.G = r.ReadByte();
                s.B = r.ReadByte();
                s.A = r.ReadByte();
            }
            else
            {
                Color32 old = LegacyColors[Mathf.Clamp(r.ReadInt32(), 0, LegacyColors.Length - 1)];
                s.R = old.r;
                s.G = old.g;
                s.B = old.b;
                s.A = old.a;
            }
            if (version >= 2)
            {
                string prop = r.ReadString();
                s.Prop = string.IsNullOrEmpty(prop) ? null : prop;
                s.Scale = Mathf.Clamp(r.ReadInt32(), MinScale, MaxScale);
            }
            if (version >= 4)
            {
                s.Step = Mathf.Clamp(r.ReadInt32(), 0, MaxStep);
                s.Box = Mathf.Clamp(r.ReadInt32(), MinBox, MaxBox);
            }
            if (version >= 5) s.Strip = r.ReadBoolean();
            else s.Strip = Settings.LegacyDecalStrip; // older saves: the method was a global option
            if (version >= 6)
            {
                s.StartShift = Mathf.Clamp(r.ReadInt32(), -MaxShift, 0);
                s.EndShift = Mathf.Clamp(r.ReadInt32(), -MaxShift, 0);
            }
            if (version >= 7) s.ShiftX = Mathf.Clamp(r.ReadInt32(), -FenceStore.MaxUnits, FenceStore.MaxUnits);
            if (version >= 8) s.ColorMul = Mathf.Clamp(r.ReadInt32(), 0, 10);
            return s;
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
                            DecalSet set = new DecalSet();
                            int n = version >= 5 ? r.ReadInt32() : 1; // before format 5 a segment had one path
                            for (int k = 0; k < n; k++)
                            {
                                DecalSettings s = ReadOne(r, version);
                                if (set.Paths.Count < MaxPathsPerSegment) set.Paths.Add(s);
                            }
                            if (set.Paths.Count > 0) Map[id] = set;
                        }
                    }
                    Version++;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[QuayTools] Could not read saved texture paths: " + ex.Message);
                Clear();
            }
        }
    }
}
