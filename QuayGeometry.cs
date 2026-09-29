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

            float[] ext;
            lock (Cache)
            {
                if (!Cache.TryGetValue(info, out ext))
                {
                    ext = MeasureMesh(info);
                    Cache[info] = ext;
                }
            }

            f.H = info.m_halfWidth;
            float xLo = ext[0], xHi = ext[1];

            // mesh +x is the right side for a normal segment; Invert mirrors the model (same rule as lane positions)
            f.YLo = invert ? -xHi : xLo;
            f.YHi = invert ? -xLo : xHi;

            float center = (f.YLo + f.YHi) * 0.5f;
            f.WaterIsRight = Mathf.Abs(center) < 0.05f * Mathf.Max(f.H, 1f) ? true : center > 0f;
            return f;
        }

        private static float[] MeasureMesh(NetInfo info)
        {
            float h = info.m_halfWidth;
            float lo = float.MaxValue, hi = float.MinValue;

            try
            {
                if (info.m_segments != null)
                {
                    for (int i = 0; i < info.m_segments.Length; i++)
                    {
                        Mesh mesh = info.m_segments[i].m_segmentMesh != null
                            ? info.m_segments[i].m_segmentMesh
                            : info.m_segments[i].m_mesh;
                        if (mesh == null) continue;

                        Bounds b = mesh.bounds;
                        lo = Mathf.Min(lo, b.min.x);
                        hi = Mathf.Max(hi, b.max.x);
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning("[QuayTools] Could not measure mesh of " + info.name + ": " + ex.Message);
            }

            bool ok = hi > lo + 0.01f;
            if (!ok)
            {
                lo = -h;
                hi = h;
            }
            else
            {
                lo = Mathf.Max(lo, -h);
                hi = Mathf.Min(hi, h);
            }

            Debug.Log("[QuayTools] Quay " + info.name + ": halfWidth=" + h.ToString("0.00") +
                      ", model x=[" + lo.ToString("0.00") + " .. " + hi.ToString("0.00") + "]" +
                      (ok ? "" : " (mesh not measurable, using full width)"));

            return new[] { lo, hi };
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
