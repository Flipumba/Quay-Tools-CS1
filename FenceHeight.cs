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
        /// Main thread (render). Recomputes the fence ribbon exactly like NetSegment.RefreshRoadFence does, but from
        /// corners that went through our adjustment, so it also works when other mods reshape the node corners.
        /// </summary>
        public static void Rebuild(ref NetSegment segment, ushort segmentId, NetInfo info, NetInfo fenceInfo,
                                   ref RenderManager.Instance data, bool leftFence)
        {
            Drain();

            int savedDepth = Patches.FenceContext.Depth;
            Patches.FenceContext.Depth = 0; // we adjust the corners ourselves below
            Vector3 p8 = Vector3.zero, p9 = Vector3.zero, p10 = Vector3.zero, p11 = Vector3.zero;
            Vector3 d8 = Vector3.zero, d9 = Vector3.zero, d10 = Vector3.zero, d11 = Vector3.zero;
            bool sStart = false, sEnd = false, tmp = false;
            try
            {
                float offset = info.m_netAI.GetFencePosition(leftFence);
                segment.CalculateCorner(segmentId, true, true, true, out p8, out d8, out sStart, offset);
                segment.CalculateCorner(segmentId, true, false, false, out p10, out d10, out sEnd, offset);
                segment.CalculateCorner(segmentId, true, true, false, out p9, out d9, out tmp, offset);
                segment.CalculateCorner(segmentId, true, false, true, out p11, out d11, out tmp, offset);
            }
            finally
            {
                Patches.FenceContext.Depth = savedDepth;
            }

            FenceStore.TryAdjustCorner(segmentId, true, true, ref p8);
            FenceStore.TryAdjustCorner(segmentId, false, false, ref p10);
            FenceStore.TryAdjustCorner(segmentId, true, false, ref p9);
            FenceStore.TryAdjustCorner(segmentId, false, true, ref p11);

            float hw = fenceInfo.m_halfWidth;
            Vector3 p18, p19, p20, p21;
            Vector3 lineStart, lineEnd;
            if (leftFence)
            {
                Vector3 n24 = Vector3.Cross(d9, Vector3.up).normalized;
                Vector3 n22 = Vector3.Cross(d11, Vector3.up).normalized;
                p18 = p9 + n24 * hw;
                p19 = p9 - n24 * hw;
                p20 = p11 - n22 * hw;
                p21 = p11 + n22 * hw;
                lineStart = p9;
                lineEnd = p11;
            }
            else
            {
                Vector3 n26 = Vector3.Cross(d8, Vector3.up).normalized;
                Vector3 n28 = Vector3.Cross(d10, Vector3.up).normalized;
                p18 = p8 + n26 * hw;
                p19 = p8 - n26 * hw;
                p20 = p10 - n28 * hw;
                p21 = p10 + n28 * hw;
                lineStart = p8;
                lineEnd = p10;
            }

            Vector3 m30 = Vector3.zero, m31 = Vector3.zero, m32 = Vector3.zero, m33 = Vector3.zero;
            NetSegment.CalculateMiddlePoints(p18, d8, p20, d10, sStart, sEnd, out m30, out m31);
            NetSegment.CalculateMiddlePoints(p19, d9, p21, d11, sStart, sEnd, out m32, out m33);

            float vScale = fenceInfo.m_netAI.GetVScale();
            data.m_dataMatrix0 = NetSegment.CalculateControlMatrix(p18, m30, m31, p20, p19, m32, m33, p21, data.m_position, vScale);
            data.m_dataMatrix1 = NetSegment.CalculateControlMatrix(p19, m32, m33, p21, p18, m30, m31, p20, data.m_position, vScale);

            if (fenceInfo.m_requireHeightMap)
            {
                Bezier3 line = new Bezier3();
                line.a = lineStart;
                line.d = lineEnd;
                line.b = (m30 + m32) * 0.5f;
                line.c = (m31 + m33) * 0.5f;
                ApplyHeightMap(segmentId, leftFence, line, hw + 6f, ref data);
            }
        }

        /// <summary>Replaces the terrain height map of a fence instance by one holding the height of the fence line.</summary>
        private static void ApplyHeightMap(ushort segmentId, bool leftFence, Bezier3 line, float margin, ref RenderManager.Instance data)
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

            int key = segmentId * 2 + (leftFence ? 1 : 0);
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
