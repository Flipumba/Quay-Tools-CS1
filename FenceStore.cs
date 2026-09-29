using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

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
        /// <summary>Metres per slider unit (slider range is +-MaxUnits).</summary>
        public const float Unit = 0.1f;

        /// <summary>Slider/field limit in units: 1000 units = 100 m.</summary>
        public const int MaxUnits = 1000;

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
        /// Moves a fence corner (result of NetSegment.CalculateCorner) to where this segment's fence should be:
        /// at the quay model edge plus the horizontal offset, at the quay deck height plus the vertical offset.
        /// It works on the finished corner position, so other mods that reshape node corners (Node Controller)
        /// are respected: the model edge scales with the actual corner width, the along-road position is kept.
        /// </summary>
        public static bool TryAdjustCorner(ushort segment, bool start, bool left, ref Vector3 pos)
        {
            FenceSettings s;
            if (!TryGet(segment, out s)) return false;

            NetManager nm = NetManager.instance;
            NetSegment[] segs = nm.m_segments.m_buffer;
            NetInfo info = segs[segment].Info;
            if (info == null) return false;

            ushort node = start ? segs[segment].m_startNode : segs[segment].m_endNode;
            if (node == 0) return false;

            // direction start -> end at this node (both stored directions point from the node into the segment)
            Vector3 fwd = start ? segs[segment].m_startDirection : -segs[segment].m_endDirection;
            fwd.y = 0f;
            if (fwd.sqrMagnitude < 1e-6f) return false;
            fwd.Normalize();
            Vector3 right = Vector3.Cross(Vector3.up, fwd);

            bool invert = (segs[segment].m_flags & NetSegment.Flags.Invert) != NetSegment.Flags.None;
            QuayFrame f = QuayGeometry.GetFrame(info, invert);

            // At the end node the game's left/right are seen from the node, so they swap.
            bool geometricRight = start ? !left : left;
            bool isLand = geometricRight == !f.WaterIsRight;

            float edge = isLand ? f.LandEdge : f.WaterEdge;
            int h = isLand ? s.LandH : s.WaterH;
            int v = isLand ? s.LandV : s.WaterV;

            Vector3 center = nm.m_nodes.m_buffer[node].m_position;
            float cur = Vector3.Dot(pos - center, right);
            float k = Mathf.Clamp(Mathf.Abs(cur) / Mathf.Max(f.H, 0.1f), 0.05f, 4f); // width of the corner vs. normal

            float target = edge * k + h * Unit * (f.WaterIsRight ? 1f : -1f); // + moves toward the water
            pos += right * (target - cur);
            pos.y += v * Unit;
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
