using System;
using System.Collections.Generic;
using System.IO;

namespace QuayTools
{
    /// <summary>Our per-segment fence settings (the fence models themselves are stored by the game).</summary>
    internal class FenceSettings
    {
        public int LandH, LandV, WaterH, WaterV; // slider units, see FenceStore.Unit
        public bool NoConnect = true;
    }

    /// <summary>Thread-safe store of FenceSettings keyed by segment id, saved in the savegame.</summary>
    internal static class FenceStore
    {
        /// <summary>Metres per slider unit (slider range is -100..100).</summary>
        public const float Unit = 0.1f;

        private const int FormatVersion = 1;
        private static readonly Dictionary<ushort, FenceSettings> Map = new Dictionary<ushort, FenceSettings>();

        public static bool TryGet(ushort segment, out FenceSettings settings)
        {
            lock (Map)
            {
                return Map.TryGetValue(segment, out settings);
            }
        }

        public static bool Has(ushort segment)
        {
            lock (Map)
            {
                return Map.ContainsKey(segment);
            }
        }

        public static FenceSettings GetOrCreate(ushort segment)
        {
            lock (Map)
            {
                FenceSettings s;
                if (!Map.TryGetValue(segment, out s))
                {
                    s = new FenceSettings();
                    Map[segment] = s;
                }
                return s;
            }
        }

        public static void Remove(ushort segment)
        {
            lock (Map)
            {
                Map.Remove(segment);
            }
        }

        public static void Clear()
        {
            lock (Map)
            {
                Map.Clear();
            }
        }

        public static bool NoConnect(ushort segment)
        {
            FenceSettings s;
            return TryGet(segment, out s) && s.NoConnect;
        }

        /// <summary>
        /// How a fence corner of this segment must be changed: an extra lateral offset for CalculateCorner
        /// (outward positive) that moves the fence from the network edge to the wanted place, and a height shift.
        /// </summary>
        public static bool TryGetCornerAdjust(ushort segment, bool start, bool left, out float offsetDelta, out float dy)
        {
            offsetDelta = 0f;
            dy = 0f;

            FenceSettings s;
            if (!TryGet(segment, out s)) return false;

            NetSegment[] segs = NetManager.instance.m_segments.m_buffer;
            NetInfo info = segs[segment].Info;
            if (info == null) return false;

            bool invert = (segs[segment].m_flags & NetSegment.Flags.Invert) != NetSegment.Flags.None;
            QuayFrame f = QuayGeometry.GetFrame(info, invert);

            // At the end node the game's left/right are seen from the node, so they swap.
            bool geometricRight = start ? !left : left;
            bool isLand = geometricRight == !f.WaterIsRight;

            float edge = isLand ? f.LandEdge : f.WaterEdge;
            int h = isLand ? s.LandH : s.WaterH;
            int v = isLand ? s.LandV : s.WaterV;

            float target = edge + h * Unit * (f.WaterIsRight ? 1f : -1f); // + moves toward the water
            offsetDelta = (geometricRight ? target : -target) - f.H;
            dy = v * Unit;
            return true;
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
                    foreach (KeyValuePair<ushort, FenceSettings> kv in Map)
                    {
                        if ((segs[kv.Key].m_flags & NetSegment.Flags.Created) != NetSegment.Flags.None) keys.Add(kv.Key);
                    }

                    w.Write(FormatVersion);
                    w.Write(keys.Count);
                    for (int i = 0; i < keys.Count; i++)
                    {
                        FenceSettings s = Map[keys[i]];
                        w.Write((int)keys[i]);
                        w.Write(s.LandH);
                        w.Write(s.LandV);
                        w.Write(s.WaterH);
                        w.Write(s.WaterV);
                        w.Write(s.NoConnect);
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
                            FenceSettings s = new FenceSettings();
                            s.LandH = r.ReadInt32();
                            s.LandV = r.ReadInt32();
                            s.WaterH = r.ReadInt32();
                            s.WaterV = r.ReadInt32();
                            s.NoConnect = r.ReadBoolean();
                            Map[id] = s;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning("[QuayTools] Could not read saved fence settings: " + ex.Message);
                Clear();
            }
        }
    }
}
