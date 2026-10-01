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

        // Trim of the two fence ends along the quay (units of 0.1 m): 0 = the fence has the full length of the segment,
        // negative = shortened by that much (never positive: a fence cannot be extended).
        public int LandStart, LandEnd, WaterStart, WaterEnd;

        // Width (thickness) scale of each fence model, percent.
        public int LandScale = FenceStore.ScaleDefault, WaterScale = FenceStore.ScaleDefault;

        // The fence is not joined to the neighbouring segment at this end of the segment.
        public bool DetachStart, DetachEnd;

        // The model is turned to face the other side (the ribbon is mirrored across its own axis).
        public bool LandFlip, WaterFlip;

        /// <summary>The Invert flag of the segment the fence slots currently correspond to (see FenceStore.Reconcile).</summary>
        public bool Inverted;

        public FenceSettings Clone()
        {
            return (FenceSettings)MemberwiseClone();
        }

        /// <summary>Back to defaults: no offsets, no closing fences. The Inverted state is kept.</summary>
        public void ResetOffsets()
        {
            LandH = LandV = WaterH = WaterV = 0;
            CapStart = CapEnd = false;
            LandStart = LandEnd = WaterStart = WaterEnd = 0;
            LandScale = WaterScale = FenceStore.ScaleDefault;
            DetachStart = DetachEnd = false;
            LandFlip = WaterFlip = false;
        }

        /// <summary>True when nothing but the models themselves is set.</summary>
        public bool IsDefault()
        {
            return LandH == 0 && LandV == 0 && WaterH == 0 && WaterV == 0 && !CapStart && !CapEnd &&
                   LandStart == 0 && LandEnd == 0 && WaterStart == 0 && WaterEnd == 0 &&
                   LandScale == FenceStore.ScaleDefault && WaterScale == FenceStore.ScaleDefault && !DetachStart && !DetachEnd && !LandFlip && !WaterFlip;
        }

        public bool SameAs(FenceSettings o)
        {
            return o != null && LandH == o.LandH && LandV == o.LandV && WaterH == o.WaterH && WaterV == o.WaterV &&
                   CapStart == o.CapStart && CapEnd == o.CapEnd && Inverted == o.Inverted &&
                   LandStart == o.LandStart && LandEnd == o.LandEnd && WaterStart == o.WaterStart && WaterEnd == o.WaterEnd &&
                   LandScale == o.LandScale && WaterScale == o.WaterScale && DetachStart == o.DetachStart && DetachEnd == o.DetachEnd &&
                   LandFlip == o.LandFlip && WaterFlip == o.WaterFlip;
        }
    }

    /// <summary>Thread-safe store of FenceSettings keyed by segment id, saved in the savegame.</summary>
    internal static class FenceStore
    {
        /// <summary>Metres per slider unit (slider range is +-MaxUnits).</summary>
        public const float Unit = 0.1f;

        /// <summary>Slider/field limit in units: 500 units = 50 m.</summary>
        public const int MaxUnits = 500;

        /// <summary>Limit of the fence end shifts in units: 500 units = 50 m.</summary>
        public const int MaxShift = 500;

        public const int ScaleDefault = 100, ScaleMin = 10, ScaleMax = 500; // percent

        private const int FormatVersion = 5;
        private static readonly Dictionary<ushort, FenceSettings> Map = new Dictionary<ushort, FenceSettings>();

        public static bool TryGet(ushort segment, out FenceSettings settings)
        {
            lock (Map)
            {
                return Map.TryGetValue(segment, out settings);
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

        public static FenceSettings GetOrCreate(ushort segment)
        {
            lock (Map)
            {
                FenceSettings s;
                if (!Map.TryGetValue(segment, out s))
                {
                    s = new FenceSettings();
                    s.Inverted = IsInverted(segment);
                    Map[segment] = s;
                }
                return s;
            }
        }

        /// <summary>Replaces (or, with null, removes) the settings of a segment. Used by undo/redo.</summary>
        public static void Set(ushort segment, FenceSettings settings)
        {
            lock (Map)
            {
                if (settings == null) Map.Remove(segment);
                else Map[segment] = settings;
            }
        }

        private static bool IsInverted(ushort segment)
        {
            return (NetManager.instance.m_segments.m_buffer[segment].m_flags & NetSegment.Flags.Invert) != NetSegment.Flags.None;
        }

        /// <summary>
        /// The game (or another mod) may change the Invert flag of a segment by itself, for example while nodes are moved.
        /// That mirrors the quay, so land and water swap sides; the fences follow their land/water meaning, hence the two
        /// slots are swapped. Returns true when something changed (the caller refreshes the renderers).
        /// Any thread that owns the segment data (simulation thread).
        /// </summary>
        public static bool Reconcile(ushort segment)
        {
            FenceSettings s;
            if (!TryGet(segment, out s)) return false;

            NetSegment[] segs = NetManager.instance.m_segments.m_buffer;
            if ((segs[segment].m_flags & NetSegment.Flags.Created) == NetSegment.Flags.None) return false;

            bool now = IsInverted(segment);
            if (s.Inverted == now) return false;

            s.Inverted = now;
            NetInfo left = segs[segment].LeftFenceInfo;
            segs[segment].LeftFenceInfo = segs[segment].RightFenceInfo;
            segs[segment].RightFenceInfo = left;
            return true;
        }

        /// <summary>Segments whose Invert flag no longer matches the state their fences were placed for.</summary>
        public static void FindMismatches(List<ushort> result)
        {
            result.Clear();
            NetSegment[] segs = NetManager.instance.m_segments.m_buffer;
            lock (Map)
            {
                foreach (KeyValuePair<ushort, FenceSettings> kv in Map)
                {
                    bool now = (segs[kv.Key].m_flags & NetSegment.Flags.Invert) != NetSegment.Flags.None;
                    if (kv.Value.Inverted != now && (segs[kv.Key].m_flags & NetSegment.Flags.Created) != NetSegment.Flags.None)
                    {
                        result.Add(kv.Key);
                    }
                }
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

        /// <summary>Which of the two models (land or water) lies on the given geometric side of the segment.</summary>
        private static bool IsLandSide(ushort segment, bool geometricRight, FenceSettings s, out bool ok)
        {
            ok = false;
            NetInfo info = NetManager.instance.m_segments.m_buffer[segment].Info;
            if (info == null) return false;
            bool invert = (NetManager.instance.m_segments.m_buffer[segment].m_flags & NetSegment.Flags.Invert) != NetSegment.Flags.None;
            QuayFrame f = QuayGeometry.GetFrame(info, invert);
            ok = true;
            return geometricRight == !f.WaterIsRight;
        }

        /// <summary>
        /// Shifts of the two fence ends along the quay (metres, positive extends) and the width scale of the fence
        /// on the given geometric side of the segment.
        /// </summary>
        public static bool TryGetExtras(ushort segment, bool geometricRight, out float startShift, out float endShift, out float scaleX)
        {
            startShift = 0f;
            endShift = 0f;
            scaleX = 1f;

            FenceSettings s;
            if (!TryGet(segment, out s)) return false;

            bool ok;
            bool isLand = IsLandSide(segment, geometricRight, s, out ok);
            if (!ok) return false;

            startShift = Mathf.Min(0, isLand ? s.LandStart : s.WaterStart) * Unit; // trim only
            endShift = Mathf.Min(0, isLand ? s.LandEnd : s.WaterEnd) * Unit;
            scaleX = Mathf.Clamp(isLand ? s.LandScale : s.WaterScale, ScaleMin, ScaleMax) / 100f;
            return true;
        }

        /// <summary>
        /// True when the fence of this segment on the given geometric side must not be joined to the neighbouring
        /// segment at that end: it was detached explicitly, or the fence end was shifted (then it no longer meets the neighbour).
        /// </summary>
        public static bool IsDetached(ushort segment, bool atStart, bool geometricRight)
        {
            FenceSettings s;
            if (!TryGet(segment, out s)) return false;
            if (atStart ? s.DetachStart : s.DetachEnd) return true;

            bool ok;
            bool isLand = IsLandSide(segment, geometricRight, s, out ok);
            if (!ok) return false;

            int shift = atStart ? (isLand ? s.LandStart : s.WaterStart) : (isLand ? s.LandEnd : s.WaterEnd);
            return shift < 0;
        }

        /// <summary>True when the model on the given geometric side of the segment is flipped to face the other way.</summary>
        public static bool IsFlipped(ushort segment, bool geometricRight)
        {
            FenceSettings s;
            if (!TryGet(segment, out s)) return false;
            bool ok;
            bool isLand = IsLandSide(segment, geometricRight, s, out ok);
            return ok && (isLand ? s.LandFlip : s.WaterFlip);
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
                        w.Write(s.Inverted);
                        w.Write(s.LandStart);
                        w.Write(s.LandEnd);
                        w.Write(s.WaterStart);
                        w.Write(s.WaterEnd);
                        w.Write(s.LandScale);
                        w.Write(s.WaterScale);
                        w.Write(s.DetachStart);
                        w.Write(s.DetachEnd);
                        w.Write(s.LandFlip);
                        w.Write(s.WaterFlip);
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

                            // older saves did not record the orientation the fences belong to: assume the current one
                            if (version >= 3) s.Inverted = r.ReadBoolean();
                            else s.Inverted = IsInverted(id);

                            if (version >= 4)
                            {
                                s.LandStart = r.ReadInt32();
                                s.LandEnd = r.ReadInt32();
                                s.WaterStart = r.ReadInt32();
                                s.WaterEnd = r.ReadInt32();
                                s.LandScale = r.ReadInt32();
                                s.WaterScale = r.ReadInt32();
                                s.DetachStart = r.ReadBoolean();
                                s.DetachEnd = r.ReadBoolean();
                            }
                            if (version >= 5)
                            {
                                s.LandFlip = r.ReadBoolean();
                                s.WaterFlip = r.ReadBoolean();
                            }

                            // limits of v0.4.1: offsets +-50 m, ends can only be trimmed
                            s.LandH = Mathf.Clamp(s.LandH, -MaxUnits, MaxUnits);
                            s.LandV = Mathf.Clamp(s.LandV, -MaxUnits, MaxUnits);
                            s.WaterH = Mathf.Clamp(s.WaterH, -MaxUnits, MaxUnits);
                            s.WaterV = Mathf.Clamp(s.WaterV, -MaxUnits, MaxUnits);
                            s.LandStart = Mathf.Clamp(s.LandStart, -MaxShift, 0);
                            s.LandEnd = Mathf.Clamp(s.LandEnd, -MaxShift, 0);
                            s.WaterStart = Mathf.Clamp(s.WaterStart, -MaxShift, 0);
                            s.WaterEnd = Mathf.Clamp(s.WaterEnd, -MaxShift, 0);
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
