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
        public bool CapStart, CapEnd; // straight closing fence at the start / end of the segment (dead ends)
    }

    /// <summary>Thread-safe store of FenceSettings keyed by segment id, saved in the savegame.</summary>
    internal static class FenceStore
    {
        /// <summary>Metres per slider unit (slider range is +-MaxUnits).</summary>
        public const float Unit = 0.1f;

        /// <summary>Slider/field limit in units: 1000 units = 100 m.</summary>
        public const int MaxUnits = 1000;

        private const int FormatVersion = 2;
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

        /// <summary>
        /// Where a fence of this segment lies across the quay: t = 0 is the geometric left edge of the network,
        /// t = 1 the right edge (the quay model is stretched between them); lateral = extra sideways offset in metres
        /// (right positive), dy = vertical offset in metres.
        /// </summary>
        public static bool TryGetLine(ushort segment, bool geometricRight, out float t, out float lateral, out float dy)
        {
            t = 0.5f;
            lateral = 0f;
            dy = 0f;

            FenceSettings s;
            if (!TryGet(segment, out s)) return false;

            NetSegment[] segs = NetManager.instance.m_segments.m_buffer;
            NetInfo info = segs[segment].Info;
            if (info == null) return false;

            bool invert = (segs[segment].m_flags & NetSegment.Flags.Invert) != NetSegment.Flags.None;
            QuayFrame f = QuayGeometry.GetFrame(info, invert);

            bool isLand = geometricRight == !f.WaterIsRight;
            float edge = isLand ? f.LandEdge : f.WaterEdge; // signed metres from the centre line, right positive
            int h = isLand ? s.LandH : s.WaterH;
            int v = isLand ? s.LandV : s.WaterV;

            t = 0.5f + edge / (2f * Mathf.Max(f.H, 0.1f));
            lateral = h * Unit * (f.WaterIsRight ? 1f : -1f); // + moves toward the water
            dy = v * Unit;
            return true;
        }

        /// <summary>Corner of a segment end as computed by the game and other mods (no fence offset).</summary>
        public static void GetRawCorner(ushort segment, bool start, bool left, out Vector3 pos, out Vector3 dir, out bool smooth)
        {
            NetSegment[] segs = NetManager.instance.m_segments.m_buffer;
            int saved = Patches.FenceContext.Depth;
            Patches.FenceContext.Depth = 0; // do not adjust our own helper calls
            try
            {
                segs[segment].CalculateCorner(segment, true, start, left, out pos, out dir, out smooth, 0f);
            }
            finally
            {
                Patches.FenceContext.Depth = saved;
            }
        }

        /// <summary>
        /// Gives the position a fence corner (at a node) of this segment must have: on the quay model edge plus the
        /// horizontal offset, at the height of the quay plus the vertical offset. The model edge is found by
        /// interpolating between the two real corners of the segment end, so node rotation, shift, stretching and
        /// height changes (Node Controller) carry over.
        /// </summary>
        public static bool TryAdjustCorner(ushort segment, bool start, bool left, ref Vector3 pos)
        {
            // At the end node the game's left/right are seen from the node, so they swap.
            bool geometricRight = start ? !left : left;

            float t, lateralMeters, dy;
            if (!TryGetLine(segment, geometricRight, out t, out lateralMeters, out dy)) return false;

            Vector3 cA, cB, d;
            bool sm;
            GetRawCorner(segment, start, true, out cA, out d, out sm);
            GetRawCorner(segment, start, false, out cB, out d, out sm);
            Vector3 geoLeft = start ? cA : cB;
            Vector3 geoRight = start ? cB : cA;

            Vector3 p = Vector3.Lerp(geoLeft, geoRight, t);

            Vector3 lat = geoRight - geoLeft;
            lat.y = 0f;
            if (lat.sqrMagnitude < 1e-6f) return false;
            lat.Normalize();

            p += lat * lateralMeters;
            p.y += dy;
            pos = p;
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
                        w.Write(s.CapStart);
                        w.Write(s.CapEnd);
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
                    if (version != 1 && version != FormatVersion) return;

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
                            if (version == 1) r.ReadBoolean(); // old "do not join" flag, replaced by the two closing options
                            else
                            {
                                s.CapStart = r.ReadBoolean();
                                s.CapEnd = r.ReadBoolean();
                            }
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
