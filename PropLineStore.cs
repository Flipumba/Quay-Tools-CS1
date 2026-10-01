using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace QuayTools
{
    /// <summary>One line of props along a quay segment: a prop placed at a fixed step.</summary>
    internal class PropEntry
    {
        public const int StepDefault = 100;  // 10 m
        public const int StepMin = 5;        // 0.5 m
        public const int StepMax = 500;      // 50 m
        public const int MaxShift = 500;     // 50 m (start / end trim)
        public const int MaxOffset = 500;    // 50 m (sideways and vertical offset)
        public const int ScaleDefault = 100; // percent
        public const int ScaleMin = 5;
        public const int ScaleMax = 1000;
        public const int RandomMax = 100;    // percent

        public string Prop;                  // name of the prop (PropInfo); null = nothing chosen yet
        public bool Enabled = true;
        public int Step = StepDefault;       // distance between props along the quay, units of 0.1 m
        public int StartShift, EndShift;     // units of 0.1 m, -MaxShift..0: how much the line is shortened at that end
        public int Lateral;                  // units of 0.1 m, + toward the water, - toward the land
        public int Lift;                     // units of 0.1 m, + up
        public int Angle;                    // degrees, turn of every prop around the vertical axis (0 = prop faces along the quay)
        public bool RandomRotation;
        public int Scale = ScaleDefault;     // percent
        public int ScaleRandom;              // percent (0 = all props equal; 30 = 70%..130% of Scale)

        public PropEntry Clone()
        {
            return (PropEntry)MemberwiseClone();
        }

        public bool SameAs(PropEntry o)
        {
            return o != null && Prop == o.Prop && Enabled == o.Enabled && Step == o.Step && StartShift == o.StartShift &&
                   EndShift == o.EndShift && Lateral == o.Lateral && Lift == o.Lift && Angle == o.Angle &&
                   RandomRotation == o.RandomRotation && Scale == o.Scale && ScaleRandom == o.ScaleRandom;
        }
    }

    /// <summary>All prop lines of one segment.</summary>
    internal class PropLine
    {
        public readonly List<PropEntry> Entries = new List<PropEntry>();

        public PropLine Clone()
        {
            PropLine c = new PropLine();
            for (int i = 0; i < Entries.Count; i++) c.Entries.Add(Entries[i].Clone());
            return c;
        }

        public bool SameAs(PropLine o)
        {
            if (o == null || o.Entries.Count != Entries.Count) return false;
            for (int i = 0; i < Entries.Count; i++)
            {
                if (!Entries[i].SameAs(o.Entries[i])) return false;
            }
            return true;
        }
    }

    /// <summary>Thread-safe store of prop lines keyed by segment id, saved in the savegame.</summary>
    internal static class PropLineStore
    {
        private const int FormatVersion = 1;
        private static readonly Dictionary<ushort, PropLine> Map = new Dictionary<ushort, PropLine>();

        /// <summary>Changes whenever the content changes; the renderer rebuilds what it shows.</summary>
        public static volatile int Version;

        public static bool TryGet(ushort segment, out PropLine line)
        {
            lock (Map)
            {
                return Map.TryGetValue(segment, out line);
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

        /// <summary>Replaces (or, with null or an empty line, removes) the prop lines of a segment.</summary>
        public static void Set(ushort segment, PropLine line)
        {
            lock (Map)
            {
                if (line == null || line.Entries.Count == 0) Map.Remove(segment);
                else Map[segment] = line;
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

        public static List<KeyValuePair<ushort, PropLine>> Snapshot()
        {
            List<KeyValuePair<ushort, PropLine>> list = new List<KeyValuePair<ushort, PropLine>>();
            lock (Map)
            {
                foreach (KeyValuePair<ushort, PropLine> kv in Map)
                {
                    list.Add(new KeyValuePair<ushort, PropLine>(kv.Key, kv.Value.Clone()));
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
                    foreach (KeyValuePair<ushort, PropLine> kv in Map)
                    {
                        if ((segs[kv.Key].m_flags & NetSegment.Flags.Created) != NetSegment.Flags.None) keys.Add(kv.Key);
                    }

                    w.Write(FormatVersion);
                    w.Write(keys.Count);
                    for (int i = 0; i < keys.Count; i++)
                    {
                        PropLine line = Map[keys[i]];
                        w.Write((int)keys[i]);
                        w.Write(line.Entries.Count);
                        for (int k = 0; k < line.Entries.Count; k++)
                        {
                            PropEntry e = line.Entries[k];
                            w.Write(e.Prop ?? string.Empty);
                            w.Write(e.Enabled);
                            w.Write(e.Step);
                            w.Write(e.StartShift);
                            w.Write(e.EndShift);
                            w.Write(e.Lateral);
                            w.Write(e.Lift);
                            w.Write(e.Angle);
                            w.Write(e.RandomRotation);
                            w.Write(e.Scale);
                            w.Write(e.ScaleRandom);
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
                    int version = r.ReadInt32();
                    if (version != FormatVersion) return;

                    int count = r.ReadInt32();
                    lock (Map)
                    {
                        for (int i = 0; i < count; i++)
                        {
                            ushort id = (ushort)r.ReadInt32();
                            int n = r.ReadInt32();
                            PropLine line = new PropLine();
                            for (int k = 0; k < n; k++)
                            {
                                PropEntry e = new PropEntry();
                                string prop = r.ReadString();
                                e.Prop = prop.Length == 0 ? null : prop;
                                r.ReadBoolean(); // the on/off switch was removed in v0.4.1
                                e.Enabled = true;
                                e.Step = r.ReadInt32();
                                e.StartShift = r.ReadInt32();
                                e.EndShift = r.ReadInt32();
                                e.Lateral = r.ReadInt32();
                                e.Lift = r.ReadInt32();
                                e.Angle = r.ReadInt32();
                                e.RandomRotation = r.ReadBoolean();
                                e.Scale = r.ReadInt32();
                                e.ScaleRandom = r.ReadInt32();
                                e.Step = Mathf.Clamp(e.Step, PropEntry.StepMin, PropEntry.StepMax);
                                e.StartShift = Mathf.Clamp(e.StartShift, -PropEntry.MaxShift, 0);
                                e.EndShift = Mathf.Clamp(e.EndShift, -PropEntry.MaxShift, 0);
                                e.Lateral = Mathf.Clamp(e.Lateral, -PropEntry.MaxOffset, PropEntry.MaxOffset);
                                e.Lift = Mathf.Clamp(e.Lift, -PropEntry.MaxOffset, PropEntry.MaxOffset);
                                line.Entries.Add(e);
                            }
                            if (line.Entries.Count > 0) Map[id] = line;
                        }
                    }
                }
                Version++;
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[QuayTools] Could not read saved prop lines: " + ex.Message);
                Clear();
            }
        }
    }
}
