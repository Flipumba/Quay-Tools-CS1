using System.Collections.Generic;
using ColossalFramework.Math;
using UnityEngine;

namespace QuayTools
{
    /// <summary>
    /// Where the visible quay model really is inside the network.
    /// A quay network is much wider (NetInfo.m_halfWidth) than its visible wall/walkway: the model sits on
    /// one edge, the rest is the terrain-shaping area. All lateral values are "right of start->end" positive.
    /// </summary>
    internal struct QuayFrame
    {
        public float H;          // half width of the whole network (where the game puts fences by default)
        public float YLo, YHi;   // lateral extent of the visible model
        public bool WaterIsRight;

        /// <summary>Model edge on the water side (the outer edge of the model).</summary>
        public float WaterEdge { get { return WaterIsRight ? YHi : YLo; } }

        /// <summary>Model edge on the land side (the inner edge of the model).</summary>
        public float LandEdge { get { return WaterIsRight ? YLo : YHi; } }
    }

    internal static class QuayGeometry
    {
        private static readonly Dictionary<NetInfo, float[]> Cache = new Dictionary<NetInfo, float[]>();

        public static QuayFrame GetFrame(ushort segmentId)
        {
            NetSegment seg = NetManager.instance.m_segments.m_buffer[segmentId];
            bool invert = (seg.m_flags & NetSegment.Flags.Invert) != NetSegment.Flags.None;
            return GetFrame(seg.Info, invert);
        }

        public static QuayFrame GetFrame(NetInfo info, bool invert)
        {
            QuayFrame f = new QuayFrame();
            if (info == null) return f;

            f.H = info.m_halfWidth;
            float e = GetLaneExtent(info);

            // The model spans the lane area on both sides of the centre line; the exact edge is tuned with the offsets.
            f.YLo = -e;
            f.YHi = e;

            // QuayAI.SegmentModifyMask raises the land slope on the +x (right) side of a normal segment
            // and on the -x side of an inverted one, so the water is on the right exactly when Invert is set.
            f.WaterIsRight = invert ^ Settings.SwapLandWater;
            return f;
        }

        private static float GetLaneExtent(NetInfo info)
        {
            float[] cached;
            lock (Cache)
            {
                if (Cache.TryGetValue(info, out cached)) return cached[0];
            }

            float h = info.m_halfWidth;
            float e = 0f;
            string dump = string.Empty;
            try
            {
                if (info.m_lanes != null)
                {
                    for (int i = 0; i < info.m_lanes.Length; i++)
                    {
                        NetInfo.Lane lane = info.m_lanes[i];
                        dump += " [" + lane.m_laneType + " " + lane.m_position.ToString("0.0") + " w" + lane.m_width.ToString("0.0") + "]";
                        if (lane.m_laneType == NetInfo.LaneType.None) continue;
                        e = Mathf.Max(e, Mathf.Abs(lane.m_position) + lane.m_width * 0.5f);
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning("[QuayTools] Could not read lanes of " + info.name + ": " + ex.Message);
            }

            if (e < 1f) e = Mathf.Min(6f, h);
            e = Mathf.Min(e, h);

            Debug.Log("[QuayTools] Quay " + info.name + ": halfWidth=" + h.ToString("0.00") + ", model half width=" + e.ToString("0.00") + ", lanes:" + dump);

            lock (Cache)
            {
                Cache[info] = new[] { e };
            }
            return e;
        }

        /// <summary>Which fence slot lies on the land or the water side. The game's "left" fence slot is drawn on the geometric right.</summary>
        public static bool SlotIsLeft(bool landSide, bool waterIsRight)
        {
            // land is on the geometric right when water is on the left, and the left slot is the geometric right
            return landSide ? !waterIsRight : waterIsRight;
        }

        // ---------- overlay helpers ----------

        public static bool TryGetCenterBezier(ushort segmentId, out Bezier3 bezier)
        {
            bezier = new Bezier3();
            NetManager nm = NetManager.instance;
            NetSegment seg = nm.m_segments.m_buffer[segmentId];
            if ((seg.m_flags & NetSegment.Flags.Created) == NetSegment.Flags.None) return false;

            Vector3 a = nm.m_nodes.m_buffer[seg.m_startNode].m_position;
            Vector3 d = nm.m_nodes.m_buffer[seg.m_endNode].m_position;
            Vector3 b, c;
            NetSegment.CalculateMiddlePoints(a, seg.m_startDirection, d, seg.m_endDirection, false, false, out b, out c);

            bezier.a = a;
            bezier.b = b;
            bezier.c = c;
            bezier.d = d;
            return true;
        }

        private static Vector3 Right(Vector3 dir)
        {
            dir.y = 0f;
            if (dir.sqrMagnitude < 1e-6f) return Vector3.zero;
            return Vector3.Cross(Vector3.up, dir).normalized;
        }

        /// <summary>The same curve shifted sideways by y (right positive).</summary>
        public static Bezier3 Offset(Bezier3 b, float y)
        {
            Bezier3 o = new Bezier3();
            o.a = b.a + Right(b.b - b.a) * y;
            o.b = b.b + Right(b.c - b.a) * y;
            o.c = b.c + Right(b.d - b.b) * y;
            o.d = b.d + Right(b.d - b.c) * y;
            return o;
        }

        /// <summary>Draws a stripe of the given width whose centre is y metres to the right of the segment centre line.</summary>
        public static void DrawStripe(RenderManager.CameraInfo cameraInfo, ushort segmentId, float y, float width, Color color)
        {
            Bezier3 center;
            if (!TryGetCenterBezier(segmentId, out center)) return;

            RenderManager.instance.OverlayEffect.DrawBezier(
                cameraInfo, color, Offset(center, y), Mathf.Max(width, 0.4f), -100000f, -100000f, -1f, 1280f, false, true);
        }

        /// <summary>Draws the visible model strip of a quay segment.</summary>
        public static void DrawModel(RenderManager.CameraInfo cameraInfo, ushort segmentId, Color color)
        {
            QuayFrame f = GetFrame(segmentId);
            DrawStripe(cameraInfo, segmentId, (f.YLo + f.YHi) * 0.5f, f.YHi - f.YLo, color);
        }
    }
}
