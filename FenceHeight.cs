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

        /// <summary>Texture keys of a segment: segment * KeysPerSegment + line * 64 + part (0..59 = pieces of the line, 60 = closing piece at the start, 61 = at the end).</summary>
        public const int KeysPerSegment = 1024;

        public static int Key(ushort segment, int line, int part)
        {
            return segment * KeysPerSegment + (line & 15) * 64 + (part & 63);
        }

        private static readonly Dictionary<int, Texture2D> Textures = new Dictionary<int, Texture2D>();
        private static readonly List<int> PendingRelease = new List<int>();

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
                for (int k = 0; k < KeysPerSegment; k++) Destroy(list[i] * KeysPerSegment + k);
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

        /// <summary>Replaces the terrain height map of a fence instance by one holding the height of the fence line.</summary>
        internal static void ApplyHeightMap(int key, Bezier3 line, float margin, ref RenderManager.Instance data)
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
