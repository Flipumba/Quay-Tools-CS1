using System;
using System.Collections.Generic;
using ColossalFramework.Math;
using UnityEngine;

namespace QuayTools
{
    /// <summary>
    /// Some fence models (shader "Custom/Net/Fence", NetInfo.m_requireHeightMap) ignore the height of the segment
    /// and take their height from the terrain height map under them. For our segments we hand them a small
    /// height map of our own: every texel holds the height of the quay deck (+ the vertical offset), so the
    /// fence stays at the level of the quay instead of following the ground.
    /// Encoding (from TerrainPatch.Refresh): r,g = high/low byte of the 16 bit height (1024 m = 65535).
    /// </summary>
    internal static class FenceHeight
    {
        private const int Size = 128;
        private const float MaxHeight = 1024f;

        private static readonly Dictionary<int, Texture2D> Textures = new Dictionary<int, Texture2D>();
        private static readonly List<int> PendingRelease = new List<int>();
        private static bool _logged;

        /// <summary>Called from any thread; the textures are destroyed later on the main thread.</summary>
        public static void Release(ushort segment)
        {
            lock (PendingRelease)
            {
                PendingRelease.Add(segment);
            }
        }

        /// <summary>Main thread only.</summary>
        public static void Drain()
        {
            List<int> list = null;
            lock (PendingRelease)
            {
                if (PendingRelease.Count == 0) return;
                list = new List<int>(PendingRelease);
                PendingRelease.Clear();
            }

            for (int i = 0; i < list.Count; i++)
            {
                Destroy(list[i] * 2);
                Destroy(list[i] * 2 + 1);
            }
        }

        public static void Clear()
        {
            foreach (KeyValuePair<int, Texture2D> kv in Textures)
            {
                if (kv.Value != null) UnityEngine.Object.Destroy(kv.Value);
            }
            Textures.Clear();
            lock (PendingRelease)
            {
                PendingRelease.Clear();
            }
        }

        private static void Destroy(int key)
        {
            Texture2D t;
            if (Textures.TryGetValue(key, out t))
            {
                if (t != null) UnityEngine.Object.Destroy(t);
                Textures.Remove(key);
            }
        }

        /// <summary>
        /// Main thread (render). Rebuilds the fence ribbon of one of our segments. The fence line is taken from the
        /// quay surface itself: the two edge curves of the segment (built from the real, possibly mod-changed corners)
        /// are blended at the fence's lateral position, so the fence follows the model exactly.
        /// </summary>
        public static void Rebuild(ref NetSegment segment, ushort segmentId, NetInfo info, NetInfo fenceInfo,
                                   ref RenderManager.Instance data, bool leftFence)
        {
            Drain();

            // The game's "left fence" slot is drawn on the geometric right side.
            bool geometricRight = leftFence;

            float t, lateralMeters, dy;
            if (!FenceStore.TryGetLine(segmentId, geometricRight, out t, out lateralMeters, out dy)) return;

            Vector3 sL, sR, eL, eR, dSL, dSR, dEL, dER;
            bool smSL, smSR, smEL, smER;
            FenceStore.GetRawCorner(segmentId, true, true, out sL, out dSL, out smSL);
            FenceStore.GetRawCorner(segmentId, true, false, out sR, out dSR, out smSR);
            FenceStore.GetRawCorner(segmentId, false, false, out eL, out dEL, out smEL); // geometric left at the end node
            FenceStore.GetRawCorner(segmentId, false, true, out eR, out dER, out smER);  // geometric right at the end node

            Vector3 mL1, mL2, mR1, mR2;
            NetSegment.CalculateMiddlePoints(sL, dSL, eL, dEL, smSL, smEL, out mL1, out mL2);
            NetSegment.CalculateMiddlePoints(sR, dSR, eR, dER, smSR, smER, out mR1, out mR2);

            Vector3[] left = { sL, mL1, mL2, eL };
            Vector3[] right = { sR, mR1, mR2, eR };
            Vector3[] P = new Vector3[4];
            for (int i = 0; i < 4; i++)
            {
                P[i] = Vector3.Lerp(left[i], right[i], t);
                Vector3 lat = right[i] - left[i];
                lat.y = 0f;
                if (lat.sqrMagnitude > 1e-6f) P[i] += lat.normalized * lateralMeters;
                P[i].y += dy;
            }

            float startShift, endShift, scaleX;
            if (!FenceStore.TryGetExtras(segmentId, geometricRight, out startShift, out endShift, out scaleX))
            {
                startShift = 0f;
                endShift = 0f;
                scaleX = 1f;
            }
            ShiftEnds(P, startShift, endShift);

            float hw = fenceInfo.m_halfWidth * scaleX;
            Vector3[] tangent = { P[1] - P[0], P[2] - P[0], P[3] - P[1], P[3] - P[2] };
            Vector3[] A = new Vector3[4]; // left edge of the ribbon
            Vector3[] B = new Vector3[4]; // right edge of the ribbon
            for (int i = 0; i < 4; i++)
            {
                Vector3 n = Vector3.Cross(tangent[i], Vector3.up);
                n.y = 0f;
                n = n.sqrMagnitude > 1e-8f ? n.normalized : Vector3.zero;
                A[i] = P[i] + n * hw;
                B[i] = P[i] - n * hw;
            }

            if (FenceStore.IsFlipped(segmentId, geometricRight))
            {
                // the model faces the other side: mirror the ribbon across its own axis
                Vector3[] swap = A;
                A = B;
                B = swap;
            }

            float vScale = fenceInfo.m_netAI.GetVScale();
            data.m_dataMatrix0 = NetSegment.CalculateControlMatrix(A[0], A[1], A[2], A[3], B[0], B[1], B[2], B[3], data.m_position, vScale);
            data.m_dataMatrix1 = NetSegment.CalculateControlMatrix(B[0], B[1], B[2], B[3], A[0], A[1], A[2], A[3], data.m_position, vScale);

            if (fenceInfo.m_requireHeightMap)
            {
                Bezier3 line = new Bezier3();
                line.a = P[0];
                line.b = P[1];
                line.c = P[2];
                line.d = P[3];
                ApplyHeightMap(segmentId * 2 + (leftFence ? 1 : 0), line, hw + 6f, ref data);
            }
        }

        // ---------- shifting the ends of a fence along the quay ----------

        /// <summary>Parameter of the point at arc length d on a curve sampled into dist (cumulative lengths, equal parameter steps).</summary>
        private static float ParamAt(float[] dist, float d)
        {
            int n = dist.Length - 1;
            if (d <= 0f) return 0f;
            if (d >= dist[n]) return 1f;
            int i = 0;
            while (i < n - 1 && dist[i + 1] < d) i++;
            float span = dist[i + 1] - dist[i];
            float f = span > 1e-6f ? (d - dist[i]) / span : 0f;
            return (i + f) / n;
        }

        private static void SplitCubic(Vector3[] P, float t, out Vector3[] left, out Vector3[] right)
        {
            Vector3 p01 = Vector3.Lerp(P[0], P[1], t), p12 = Vector3.Lerp(P[1], P[2], t), p23 = Vector3.Lerp(P[2], P[3], t);
            Vector3 p012 = Vector3.Lerp(p01, p12, t), p123 = Vector3.Lerp(p12, p23, t);
            Vector3 p = Vector3.Lerp(p012, p123, t);
            left = new Vector3[] { P[0], p01, p012, p };
            right = new Vector3[] { p, p123, p23, P[3] };
        }

        /// <summary>The part of the cubic between the parameters t0 and t1.</summary>
        internal static Vector3[] SubCubic(Vector3[] P, float t0, float t1)
        {
            Vector3[] a, b, c, d;
            SplitCubic(P, t1, out a, out b); // a = [0, t1]
            if (t0 <= 0.0001f) return a;
            SplitCubic(a, t0 / t1, out c, out d); // d = [t0, t1]
            return d;
        }

        private static Vector3 StartDirection(Vector3[] Q)
        {
            Vector3 d = Q[1] - Q[0];
            if (d.sqrMagnitude < 1e-6f) d = Q[2] - Q[0];
            if (d.sqrMagnitude < 1e-6f) d = Q[3] - Q[0];
            return d.sqrMagnitude < 1e-8f ? Vector3.forward : d.normalized;
        }

        private static Vector3 EndDirection(Vector3[] Q)
        {
            Vector3 d = Q[3] - Q[2];
            if (d.sqrMagnitude < 1e-6f) d = Q[3] - Q[1];
            if (d.sqrMagnitude < 1e-6f) d = Q[3] - Q[0];
            return d.sqrMagnitude < 1e-8f ? Vector3.forward : d.normalized;
        }

        /// <summary>
        /// Moves the two ends of the fence curve P along the quay (metres, positive = beyond the end of the segment,
        /// negative = trims the curve). Trimming cuts the real curve; extending continues it straight along the end
        /// direction and fits a new curve through the new end points with the game's own control point formula.
        /// </summary>
        private static void ShiftEnds(Vector3[] P, float startShift, float endShift)
        {
            if (Mathf.Abs(startShift) < 0.01f && Mathf.Abs(endShift) < 0.01f) return;

            const int N = 48;
            Bezier3 bz = new Bezier3();
            bz.a = P[0];
            bz.b = P[1];
            bz.c = P[2];
            bz.d = P[3];
            float[] dist = new float[N + 1];
            Vector3 prev = P[0];
            for (int i = 1; i <= N; i++)
            {
                Vector3 cur = bz.Position(i / (float)N);
                dist[i] = dist[i - 1] + (cur - prev).magnitude;
                prev = cur;
            }
            float len = dist[N];
            if (len < 0.2f) return;

            float d0 = Mathf.Max(0f, -startShift);
            float d1 = len - Mathf.Max(0f, -endShift);
            if (d1 - d0 < 0.2f)
            {
                float mid = Mathf.Clamp((d0 + d1) * 0.5f, 0.1f, len - 0.1f);
                d0 = mid - 0.1f;
                d1 = mid + 0.1f;
            }

            Vector3[] Q = { P[0], P[1], P[2], P[3] };
            if (d0 > 0.001f || d1 < len - 0.001f)
            {
                float t0 = ParamAt(dist, d0), t1 = ParamAt(dist, d1);
                if (t1 > t0 + 1e-4f) Q = SubCubic(Q, t0, t1);
            }

            float extS = Mathf.Max(0f, startShift), extE = Mathf.Max(0f, endShift);
            if (extS > 0.01f || extE > 0.01f)
            {
                Vector3 dirA = StartDirection(Q), dirB = EndDirection(Q);
                Vector3 pa = Q[0] - dirA * extS, pd = Q[3] + dirB * extE;
                Vector3 m1, m2;
                NetSegment.CalculateMiddlePoints(pa, dirA, pd, -dirB, false, false, out m1, out m2);
                Q = new Vector3[] { pa, m1, m2, pd };
            }

            for (int i = 0; i < 4; i++) P[i] = Q[i];
        }

        /// <summary>
        /// Makes a fence piece invisible: all control points collapse into a tiny spot far below the map.
        /// </summary>
        private static void Collapse(ref RenderManager.Instance data, Vector3 position)
        {
            Vector3 p = position + new Vector3(0f, -3000f, 0f);
            Vector3 q = p + new Vector3(0.01f, 0f, 0f);
            Vector3 r = p + new Vector3(0f, 0f, 0.01f);
            data.m_dataMatrix0 = NetSegment.CalculateControlMatrix(p, p, q, q, r, r, q, q, position, 1f);
            data.m_extraData.m_dataMatrix2 = NetSegment.CalculateControlMatrix(r, r, q, q, p, p, q, q, position, 1f);
        }

        private static void Corner(ushort segmentId, bool start, bool left, float offset, out Vector3 pos, out Vector3 dir, out bool smooth)
        {
            NetSegment[] segs = NetManager.instance.m_segments.m_buffer;
            int savedDepth = Patches.FenceContext.Depth;
            Patches.FenceContext.Depth = 0; // we adjust the corner ourselves
            try
            {
                segs[segmentId].CalculateCorner(segmentId, true, start, left, out pos, out dir, out smooth, offset);
            }
            finally
            {
                Patches.FenceContext.Depth = savedDepth;
            }
            FenceStore.TryAdjustCorner(segmentId, start, left, ref pos);
        }

        /// <summary>
        /// Same as NetNode.RefreshBendFenceData (the fence that joins two segments at a bend node), built from our corners.
        /// </summary>
        public static void RebuildBend(ref NetNode node, ushort nodeId, NetInfo fenceInfo, ref RenderManager.Instance data,
                                       bool left1, bool left2, ushort seg1, ushort seg2, int index)
        {
            if (!FenceStore.Has(seg1) && !FenceStore.Has(seg2)) return;

            NetSegment[] segs = NetManager.instance.m_segments.m_buffer;
            NetInfo i1 = segs[seg1].Info, i2 = segs[seg2].Info;
            if (i1 == null || i2 == null) return;

            bool start1 = segs[seg1].m_startNode == nodeId;
            bool start2 = segs[seg2].m_startNode == nodeId;

            // the fences of the two segments are not joined here: either one was detached or had its end shifted
            bool right1 = start1 ? !left1 : left1, right2 = start2 ? !left2 : left2;
            if (FenceStore.IsDetached(seg1, start1, right1) || FenceStore.IsDetached(seg2, start2, right2))
            {
                Collapse(ref data, node.m_position);
                return;
            }

            Vector3 p4, p6, p5, p7;
            bool s9, s11;
            Corner(seg1, start1, left1, i1.m_netAI.GetFencePosition(left1), out p4, out p6, out s9);
            Corner(seg2, start2, left2, i2.m_netAI.GetFencePosition(left2), out p5, out p7, out s11);

            float sa, sb, scale1;
            if (!FenceStore.TryGetExtras(seg1, right1, out sa, out sb, out scale1)) scale1 = 1f;
            float hw = fenceInfo.m_halfWidth * scale1;
            Vector3 n12 = Vector3.Cross(p6, Vector3.up).normalized;
            Vector3 n14 = Vector3.Cross(p7, Vector3.up).normalized;
            Vector3 p16 = p4 - n12 * hw;
            Vector3 p17 = p4 + n12 * hw;
            Vector3 p18 = p5 + n14 * hw;
            Vector3 p19 = p5 - n14 * hw;

            if (FenceStore.IsFlipped(seg1, right1))
            {
                Vector3 t16 = p16, t18 = p18;
                p16 = p17;
                p17 = t16;
                p18 = p19;
                p19 = t18;
            }

            Vector3 m20 = Vector3.zero, m21 = Vector3.zero, m22 = Vector3.zero, m23 = Vector3.zero;
            NetSegment.CalculateMiddlePoints(p16, -p6, p18, -p7, true, true, out m20, out m21);
            NetSegment.CalculateMiddlePoints(p17, -p6, p19, -p7, true, true, out m22, out m23);

            float vScale = fenceInfo.m_netAI.GetVScale();
            data.m_dataMatrix0 = NetSegment.CalculateControlMatrix(p16, m20, m21, p18, p17, m22, m23, p19, node.m_position, vScale);
            data.m_extraData.m_dataMatrix2 = NetSegment.CalculateControlMatrix(p17, m22, m23, p19, p16, m20, m21, p18, node.m_position, vScale);

            if (fenceInfo.m_requireHeightMap)
            {
                Bezier3 line = new Bezier3();
                line.a = p4;
                line.d = p5;
                line.b = (m20 + m22) * 0.5f;
                line.c = (m21 + m23) * 0.5f;
                ApplyHeightMap(-(1 + nodeId * 16 + (index & 7)), line, hw + 6f, ref data);
            }
        }

        /// <summary>
        /// Replacement shape of NetNode.RefreshEndFenceData (the fence closing a dead end): a straight fence across
        /// the quay, at right angles to it, between the left and right fence of the segment.
        /// </summary>
        public static void RebuildEnd(ref NetNode node, ushort nodeId, int index, NetInfo info, NetInfo fenceInfo,
                                      ref RenderManager.Instance data)
        {
            ushort seg = node.GetSegment(index);
            if (seg == 0 || !FenceStore.Has(seg)) return;

            NetSegment[] segs = NetManager.instance.m_segments.m_buffer;
            bool isStart = segs[seg].m_startNode == nodeId;
            float offset = info.m_netAI.GetFencePosition(true);

            Vector3 pL, dL, pR, dR;
            bool sL, sR;
            Corner(seg, isStart, true, offset, out pL, out dL, out sL);
            Corner(seg, isStart, false, offset, out pR, out dR, out sR);

            dL.y = 0f;
            if (dL.sqrMagnitude < 1e-6f) return;
            Vector3 axis = dL.normalized; // points from the end into the segment

            float hw = fenceInfo.m_halfWidth;
            Vector3 a0 = pL + axis * hw, a3 = pR + axis * hw;
            Vector3 b0 = pL - axis * hw, b3 = pR - axis * hw;
            Vector3 a1 = Vector3.Lerp(a0, a3, 1f / 3f), a2 = Vector3.Lerp(a0, a3, 2f / 3f);
            Vector3 b1 = Vector3.Lerp(b0, b3, 1f / 3f), b2 = Vector3.Lerp(b0, b3, 2f / 3f);

            float vScale = fenceInfo.m_netAI.GetVScale() / 1.5f;
            data.m_dataMatrix0 = NetSegment.CalculateControlMatrix(a0, a1, a2, a3, b0, b1, b2, b3, node.m_position, vScale);
            data.m_extraData.m_dataMatrix2 = NetSegment.CalculateControlMatrix(b0, b1, b2, b3, a0, a1, a2, a3, node.m_position, vScale);

            if (fenceInfo.m_requireHeightMap)
            {
                Bezier3 line = new Bezier3();
                line.a = pL;
                line.d = pR;
                line.b = Vector3.Lerp(pL, pR, 1f / 3f);
                line.c = Vector3.Lerp(pL, pR, 2f / 3f);
                ApplyHeightMap(-(1 + nodeId * 16 + 8 + (index & 7)), line, hw + 6f, ref data);
            }
        }

        /// <summary>Replaces the terrain height map of a fence instance by one holding the height of the fence line.</summary>
        private static void ApplyHeightMap(int key, Bezier3 line, float margin, ref RenderManager.Instance data)
        {
            const int Samples = 64;
            const int Window = 12;

            float minX = Mathf.Min(Mathf.Min(line.a.x, line.b.x), Mathf.Min(line.c.x, line.d.x)) - margin;
            float maxX = Mathf.Max(Mathf.Max(line.a.x, line.b.x), Mathf.Max(line.c.x, line.d.x)) + margin;
            float minZ = Mathf.Min(Mathf.Min(line.a.z, line.b.z), Mathf.Min(line.c.z, line.d.z)) - margin;
            float maxZ = Mathf.Max(Mathf.Max(line.a.z, line.b.z), Mathf.Max(line.c.z, line.d.z)) + margin;
            float side = Mathf.Max(Mathf.Max(maxX - minX, maxZ - minZ), 8f);

            Vector3[] pts = new Vector3[Samples];
            for (int i = 0; i < Samples; i++) pts[i] = line.Position(i / (Samples - 1f));

            float axX = line.d.x - line.a.x, axZ = line.d.z - line.a.z;
            float axLen2 = Mathf.Max(axX * axX + axZ * axZ, 0.01f);

            Color32[] pixels = new Color32[Size * Size];
            float cell = side / Size;
            for (int j = 0; j < Size; j++)
            {
                float z = minZ + (j + 0.5f) * cell;
                for (int i = 0; i < Size; i++)
                {
                    float x = minX + (i + 0.5f) * cell;
                    float t = Mathf.Clamp01(((x - line.a.x) * axX + (z - line.a.z) * axZ) / axLen2);
                    int i0 = Mathf.RoundToInt(t * (Samples - 1));
                    int lo = Mathf.Max(0, i0 - Window), hi = Mathf.Min(Samples - 1, i0 + Window);

                    float best = float.MaxValue;
                    float y = pts[i0].y;
                    for (int k = lo; k <= hi; k++)
                    {
                        float dx = pts[k].x - x, dz = pts[k].z - z;
                        float d2 = dx * dx + dz * dz;
                        if (d2 < best)
                        {
                            best = d2;
                            y = pts[k].y;
                        }
                    }

                    int raw = Mathf.Clamp(Mathf.RoundToInt(y * 65535f / MaxHeight), 0, 65535);
                    byte hb = (byte)(raw >> 8), lb = (byte)(raw & 255);
                    pixels[j * Size + i] = new Color32(hb, lb, hb, lb);
                }
            }

            Destroy(key);

            Texture2D tex = new Texture2D(Size, Size, TextureFormat.ARGB32, false, true);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Point;
            tex.SetPixels32(pixels);
            tex.Apply(false);
            Textures[key] = tex;

            if (!_logged)
            {
                _logged = true;
                Debug.Log("[QuayTools] Fence height map: original mapping v1=" + data.m_dataVector1 + " v2=" + data.m_dataVector2 +
                          " pos=" + data.m_position + " line y=" + line.a.y.ToString("0.0") + ".." + line.d.y.ToString("0.0"));
            }

            // mapping: uv = pos.xz * scale + offset (same layout as TerrainPatch.m_surfaceMappingRaw: x,y = offsets, z = scale)
            Vector4 v2 = data.m_dataVector2;
            v2.x = -minX / side;
            v2.y = -minZ / side;
            v2.z = 1f / side;
            data.m_dataVector2 = v2;

            Vector4 v1 = data.m_dataVector1;
            v1.x = 1f / Size;
            v1.z = 1f / Size;
            data.m_dataVector1 = v1;

            data.m_dataTexture0 = tex;
        }
    }
}
