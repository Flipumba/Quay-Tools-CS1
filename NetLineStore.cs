using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace QuayTools
{
    /// <summary>
    /// One line of a network model (fence, wall, ...) along a quay segment. Like a prop line it runs from the middle of
    /// the start node to the middle of the end node, at a sideways and a vertical offset from the middle of the quay.
    /// </summary>
    internal class NetLine
    {
        public const int ScaleDefault = 100, ScaleMin = 10, ScaleMax = 500; // percent of the model's own width
        public const int MaxShift = 500;     // 50 m (start / end trim)
        public const int MaxOffset = 500;    // 50 m (sideways and vertical offset)

        public string Model;                 // name of the network model (NetInfo); null = nothing chosen yet
        public int Lateral;                  // units of 0.1 m, + toward the water, - toward the land
        public int Lift;                     // units of 0.1 m, + up
        public int StartShift, EndShift;     // units of 0.1 m, -MaxShift..0: how much the line is shortened at that end
        public int Scale = ScaleDefault;     // percent
        public bool Flip;                    // false: the model faces the water side, true: it faces the land side
        public bool CapStart, CapEnd;        // a straight closing piece across the quay at the start / end of the segment

        public NetLine Clone()
        {
            return (NetLine)MemberwiseClone();
        }

        public bool SameAs(NetLine o)
        {
            return o != null && Model == o.Model && Lateral == o.Lateral && Lift == o.Lift && StartShift == o.StartShift &&
                   EndShift == o.EndShift && Scale == o.Scale && Flip == o.Flip && CapStart == o.CapStart && CapEnd == o.CapEnd;
        }

        public void ResetValues()
        {
            Lateral = 0;
            Lift = 0;
            StartShift = 0;
            EndShift = 0;
            Scale = ScaleDefault;
            Flip = false;
            CapStart = false;
            CapEnd = false;
        }
    }

    /// <summary>All network-model lines of one segment.</summary>
    internal class NetLineSet
    {
        public readonly List<NetLine> Lines = new List<NetLine>();

        public NetLineSet Clone()
        {
            NetLineSet c = new NetLineSet();
            for (int i = 0; i < Lines.Count; i++) c.Lines.Add(Lines[i].Clone());
            return c;
        }

        public bool SameAs(NetLineSet o)
        {
            if (o == null || o.Lines.Count != Lines.Count) return false;
            for (int i = 0; i < Lines.Count; i++)
            {
                if (!Lines[i].SameAs(o.Lines[i])) return false;
            }
            return true;
        }
    }

    /// <summary>Thread-safe store of network-model lines keyed by segment id, saved in the savegame.</summary>
    internal static class NetLineStore
    {
        private const int FormatVersion = 1;
        public const int MaxLinesPerSegment = 15;

        private static readonly Dictionary<ushort, NetLineSet> Map = new Dictionary<ushort, NetLineSet>();

        /// <summary>Changes whenever the content changes; the renderer rebuilds what it shows.</summary>
        public static volatile int Version;

        public static bool TryGet(ushort segment, out NetLineSet set)
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

        /// <summary>Replaces (or, with null or an empty set, removes) the lines of a segment.</summary>
        public static void Set(ushort segment, NetLineSet set)
        {
            lock (Map)
            {
                if (set == null || set.Lines.Count == 0) Map.Remove(segment);
                else Map[segment] = set;
            }
            if (set == null || set.Lines.Count == 0) FenceHeight.Release(segment);
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

        public static List<KeyValuePair<ushort, NetLineSet>> Snapshot()
        {
            List<KeyValuePair<ushort, NetLineSet>> list = new List<KeyValuePair<ushort, NetLineSet>>();
            lock (Map)
            {
                foreach (KeyValuePair<ushort, NetLineSet> kv in Map)
                {
                    list.Add(new KeyValuePair<ushort, NetLineSet>(kv.Key, kv.Value.Clone()));
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
                    foreach (KeyValuePair<ushort, NetLineSet> kv in Map)
                    {
                        if ((segs[kv.Key].m_flags & NetSegment.Flags.Created) != NetSegment.Flags.None) keys.Add(kv.Key);
                    }

                    w.Write(FormatVersion);
                    w.Write(keys.Count);
                    for (int i = 0; i < keys.Count; i++)
                    {
                        NetLineSet set = Map[keys[i]];
                        w.Write((int)keys[i]);
                        w.Write(set.Lines.Count);
                        for (int k = 0; k < set.Lines.Count; k++)
                        {
                            NetLine l = set.Lines[k];
                            w.Write(l.Model ?? string.Empty);
                            w.Write(l.Lateral);
                            w.Write(l.Lift);
                            w.Write(l.StartShift);
                            w.Write(l.EndShift);
                            w.Write(l.Scale);
                            w.Write(l.Flip);
                            w.Write(l.CapStart);
                            w.Write(l.CapEnd);
                        }
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
                    if (r.ReadInt32() != FormatVersion) return;

                    int count = r.ReadInt32();
                    lock (Map)
                    {
                        for (int i = 0; i < count; i++)
                        {
                            ushort id = (ushort)r.ReadInt32();
                            int n = r.ReadInt32();
                            NetLineSet set = new NetLineSet();
                            for (int k = 0; k < n; k++)
                            {
                                NetLine l = new NetLine();
                                string model = r.ReadString();
                                l.Model = model.Length == 0 ? null : model;
                                l.Lateral = Mathf.Clamp(r.ReadInt32(), -NetLine.MaxOffset, NetLine.MaxOffset);
                                l.Lift = Mathf.Clamp(r.ReadInt32(), -NetLine.MaxOffset, NetLine.MaxOffset);
                                l.StartShift = Mathf.Clamp(r.ReadInt32(), -NetLine.MaxShift, 0);
                                l.EndShift = Mathf.Clamp(r.ReadInt32(), -NetLine.MaxShift, 0);
                                l.Scale = Mathf.Clamp(r.ReadInt32(), NetLine.ScaleMin, NetLine.ScaleMax);
                                l.Flip = r.ReadBoolean();
                                l.CapStart = r.ReadBoolean();
                                l.CapEnd = r.ReadBoolean();
                                if (set.Lines.Count < MaxLinesPerSegment) set.Lines.Add(l);
                            }
                            if (set.Lines.Count > 0) Map[id] = set;
                        }
                    }
                }
                Version++;
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[QuayTools] Could not read saved network lines: " + ex.Message);
                Clear();
            }
        }

        // ---------- old saves (v0.4.x): the "land" and "water" fence slots ----------

        /// <summary>
        /// Converts the old per-segment fence settings (format versions 1..5, QuayTools.Fences) into lines: the "land"
        /// model becomes line 1, the "water" model line 2. The game's fence slots of those segments are emptied, because
        /// the lines are drawn by Quay Tools itself now. Runs once while a savegame is loaded.
        /// </summary>
        public static int MigrateLegacy(byte[] data)
        {
            if (data == null || data.Length < 8) return 0;

            int migrated = 0;
            try
            {
                using (MemoryStream ms = new MemoryStream(data))
                using (BinaryReader r = new BinaryReader(ms))
                {
                    int version = r.ReadInt32();
                    if (version < 1 || version > 5) return 0;

                    NetSegment[] segs = NetManager.instance.m_segments.m_buffer;
                    int count = r.ReadInt32();
                    for (int i = 0; i < count; i++)
                    {
                        ushort id = (ushort)r.ReadInt32();
                        int landH = r.ReadInt32(), landV = r.ReadInt32(), waterH = r.ReadInt32(), waterV = r.ReadInt32();
                        bool capStart = false, capEnd = false;
                        if (version == 1) r.ReadBoolean();
                        else
                        {
                            capStart = r.ReadBoolean();
                            capEnd = r.ReadBoolean();
                        }

                        bool storedInverted = false, hasInverted = false;
                        if (version >= 3)
                        {
                            storedInverted = r.ReadBoolean();
                            hasInverted = true;
                        }

                        int landStart = 0, landEnd = 0, waterStart = 0, waterEnd = 0, landScale = 100, waterScale = 100;
                        if (version >= 4)
                        {
                            landStart = r.ReadInt32();
                            landEnd = r.ReadInt32();
                            waterStart = r.ReadInt32();
                            waterEnd = r.ReadInt32();
                            landScale = r.ReadInt32();
                            waterScale = r.ReadInt32();
                            r.ReadBoolean(); // detach start (no longer needed: lines are not joined to the neighbours)
                            r.ReadBoolean(); // detach end
                        }
                        bool landFlip = false, waterFlip = false;
                        if (version >= 5)
                        {
                            landFlip = r.ReadBoolean();
                            waterFlip = r.ReadBoolean();
                        }

                        if (id == 0 || (segs[id].m_flags & NetSegment.Flags.Created) == NetSegment.Flags.None) continue;
                        NetInfo info = segs[id].Info;
                        if (info == null) continue;

                        // the slots belong to the orientation recorded in the save: bring them to the current one first
                        bool now = (segs[id].m_flags & NetSegment.Flags.Invert) != NetSegment.Flags.None;
                        NetInfo slotLeft = segs[id].LeftFenceInfo, slotRight = segs[id].RightFenceInfo;
                        if (hasInverted && storedInverted != now)
                        {
                            NetInfo t = slotLeft;
                            slotLeft = slotRight;
                            slotRight = t;
                        }

                        QuayFrame frame = QuayGeometry.GetFrame(info, now);
                        bool landSlotIsLeft = QuayGeometry.SlotIsLeft(true, frame.WaterIsRight);
                        NetInfo landModel = landSlotIsLeft ? slotLeft : slotRight;
                        NetInfo waterModel = landSlotIsLeft ? slotRight : slotLeft;

                        int edge = Mathf.RoundToInt(frame.YHi / FenceStore.Unit); // distance of the model edge from the middle, units

                        NetLineSet set = new NetLineSet();
                        if (landModel != null)
                        {
                            NetLine l = new NetLine();
                            l.Model = landModel.name;
                            l.Lateral = Mathf.Clamp(-edge + landH, -NetLine.MaxOffset, NetLine.MaxOffset);
                            l.Lift = Mathf.Clamp(landV, -NetLine.MaxOffset, NetLine.MaxOffset);
                            l.StartShift = Mathf.Clamp(landStart, -NetLine.MaxShift, 0);
                            l.EndShift = Mathf.Clamp(landEnd, -NetLine.MaxShift, 0);
                            l.Scale = Mathf.Clamp(landScale, NetLine.ScaleMin, NetLine.ScaleMax);
                            l.Flip = !landFlip; // the old land fence faced the land; the new default faces the water
                            set.Lines.Add(l);
                        }
                        if (waterModel != null)
                        {
                            NetLine l = new NetLine();
                            l.Model = waterModel.name;
                            l.Lateral = Mathf.Clamp(edge + waterH, -NetLine.MaxOffset, NetLine.MaxOffset);
                            l.Lift = Mathf.Clamp(waterV, -NetLine.MaxOffset, NetLine.MaxOffset);
                            l.StartShift = Mathf.Clamp(waterStart, -NetLine.MaxShift, 0);
                            l.EndShift = Mathf.Clamp(waterEnd, -NetLine.MaxShift, 0);
                            l.Scale = Mathf.Clamp(waterScale, NetLine.ScaleMin, NetLine.ScaleMax);
                            l.Flip = waterFlip;
                            set.Lines.Add(l);
                        }
                        if (set.Lines.Count == 0) continue;

                        set.Lines[0].CapStart = capStart;
                        set.Lines[0].CapEnd = capEnd;

                        lock (Map)
                        {
                            if (!Map.ContainsKey(id)) Map[id] = set;
                        }

                        // the game must not draw the old fences any more
                        segs[id].LeftFenceInfo = null;
                        segs[id].RightFenceInfo = null;
                        migrated++;
                    }
                }
                Version++;
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[QuayTools] Could not convert saved fence settings: " + ex.Message);
            }
            return migrated;
        }
    }
}
