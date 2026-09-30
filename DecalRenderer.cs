using System.Collections.Generic;
using ColossalFramework.Math;
using UnityEngine;

namespace QuayTools
{
    /// <summary>
    /// Draws the decal paths: a flat coloured strip along the middle of a quay segment, on its top surface.
    /// The strip is built from the two edge curves of the segment (the real node corners, so node edits made with
    /// other mods are followed), blended at the middle, so it follows the curve and the height profile of the quay.
    /// Rendering: one mesh per segment, drawn every frame with Graphics.DrawMesh (no game shader involved).
    /// Main thread only. Lives on the QuayToolsController game object.
    /// </summary>
    public class DecalRenderer : MonoBehaviour
    {
        private const float BaseLift = 0.05f;      // metres above the surface, against z-fighting
        private const float SampleStep = 1.5f;     // metres between cross sections
        private const int MaxBuildsPerFrame = 16;
        private const int ChecksPerFrame = 6;

        private struct Corners
        {
            public Vector3 sL, sR, eL, eR, dSL, dSR, dEL, dER;
            public bool smSL, smSR, smEL, smER;

            public bool Same(Corners o)
            {
                return Near(sL, o.sL) && Near(sR, o.sR) && Near(eL, o.eL) && Near(eR, o.eR) &&
                       Near(dSL, o.dSL) && Near(dSR, o.dSR) && Near(dEL, o.dEL) && Near(dER, o.dER) &&
                       smSL == o.smSL && smSR == o.smSR && smEL == o.smEL && smER == o.smER;
            }

            private static bool Near(Vector3 a, Vector3 b)
            {
                return (a - b).sqrMagnitude < 1e-5f;
            }
        }

        private class Item
        {
            public Mesh Mesh;
            public DecalSettings Settings;
            public Corners Last;
            public bool Dirty = true;
            public bool Failed;
        }

        private readonly Dictionary<ushort, Item> _items = new Dictionary<ushort, Item>();
        private readonly List<ushort> _ids = new List<ushort>();
        private readonly List<ushort> _dead = new List<ushort>();
        private Material[] _materials;
        private Shader _shader;
        private bool _shaderSearched;
        private int _version = -1;
        private int _cursor;

        private void OnDestroy()
        {
            foreach (KeyValuePair<ushort, Item> kv in _items)
            {
                if (kv.Value.Mesh != null) Destroy(kv.Value.Mesh);
            }
            _items.Clear();

            if (_materials != null)
            {
                for (int i = 0; i < _materials.Length; i++)
                {
                    if (_materials[i] != null) Destroy(_materials[i]);
                }
            }
        }

        private void Update()
        {
            if (_version != DecalStore.Version) Sync();
            if (_ids.Count == 0) return;

            NetSegment[] segs = NetManager.instance.m_segments.m_buffer;

            // build what is new or changed
            int builds = 0;
            for (int i = 0; i < _ids.Count && builds < MaxBuildsPerFrame; i++)
            {
                Item item = _items[_ids[i]];
                if (!item.Dirty || item.Failed) continue;
                Build(_ids[i], item);
                builds++;
            }

            // look for geometry changes (node edits, moved nodes) a few segments per frame
            int checks = Mathf.Min(ChecksPerFrame, _ids.Count);
            _dead.Clear();
            for (int k = 0; k < checks; k++)
            {
                if (_cursor >= _ids.Count) _cursor = 0;
                ushort id = _ids[_cursor++];

                if ((segs[id].m_flags & NetSegment.Flags.Created) == NetSegment.Flags.None)
                {
                    _dead.Add(id);
                    continue;
                }

                Item item = _items[id];
                if (item.Dirty || item.Failed) continue;

                Corners c;
                if (!ReadCorners(id, out c) || !c.Same(item.Last)) item.Dirty = true;
            }
            for (int i = 0; i < _dead.Count; i++) DecalStore.Remove(_dead[i]);

            Draw();
        }

        private void Sync()
        {
            _version = DecalStore.Version;
            List<KeyValuePair<ushort, DecalSettings>> entries = DecalStore.Snapshot();

            HashSet<ushort> present = new HashSet<ushort>();
            for (int i = 0; i < entries.Count; i++)
            {
                ushort id = entries[i].Key;
                present.Add(id);

                Item item;
                if (!_items.TryGetValue(id, out item))
                {
                    item = new Item();
                    _items[id] = item;
                }

                if (item.Settings == null || !item.Settings.SameAs(entries[i].Value))
                {
                    item.Settings = entries[i].Value.Clone();
                    item.Dirty = true;
                    item.Failed = false;
                }
            }

            List<ushort> remove = new List<ushort>();
            foreach (KeyValuePair<ushort, Item> kv in _items)
            {
                if (!present.Contains(kv.Key)) remove.Add(kv.Key);
            }
            for (int i = 0; i < remove.Count; i++)
            {
                Item item = _items[remove[i]];
                if (item.Mesh != null) Destroy(item.Mesh);
                _items.Remove(remove[i]);
            }

            _ids.Clear();
            _ids.AddRange(_items.Keys);
        }

        // ---------- geometry ----------

        private static bool ReadCorners(ushort id, out Corners c)
        {
            c = new Corners();
            NetSegment[] segs = NetManager.instance.m_segments.m_buffer;
            if (segs[id].Info == null) return false;

            FenceStore.GetRawCorner(id, true, true, out c.sL, out c.dSL, out c.smSL);
            FenceStore.GetRawCorner(id, true, false, out c.sR, out c.dSR, out c.smSR);
            FenceStore.GetRawCorner(id, false, false, out c.eL, out c.dEL, out c.smEL); // geometric left at the end node
            FenceStore.GetRawCorner(id, false, true, out c.eR, out c.dER, out c.smER);  // geometric right at the end node
            return true;
        }

        private void Build(ushort id, Item item)
        {
            item.Dirty = false;
            try
            {
                Corners c;
                if (!ReadCorners(id, out c)) return;
                item.Last = c;

                Vector3 mL1, mL2, mR1, mR2;
                NetSegment.CalculateMiddlePoints(c.sL, c.dSL, c.eL, c.dEL, c.smSL, c.smEL, out mL1, out mL2);
                NetSegment.CalculateMiddlePoints(c.sR, c.dSR, c.eR, c.dER, c.smSR, c.smER, out mR1, out mR2);

                Vector3[] left = { c.sL, mL1, mL2, c.eL };
                Vector3[] right = { c.sR, mR1, mR2, c.eR };

                float lateral = item.Settings.Lateral * FenceStore.Unit;
                float lift = item.Settings.Lift * FenceStore.Unit + BaseLift;
                float halfWidth = Mathf.Clamp(item.Settings.Width, DecalStore.MinWidth, DecalStore.MaxWidth) * FenceStore.Unit * 0.5f;

                Vector3[] P = new Vector3[4];
                for (int i = 0; i < 4; i++)
                {
                    P[i] = Vector3.Lerp(left[i], right[i], 0.5f);
                    Vector3 lat = right[i] - left[i];
                    lat.y = 0f;
                    if (lat.sqrMagnitude > 1e-6f) P[i] += lat.normalized * lateral;
                    P[i].y += lift;
                }

                Bezier3 line = new Bezier3();
                line.a = P[0];
                line.b = P[1];
                line.c = P[2];
                line.d = P[3];

                float len = (P[1] - P[0]).magnitude + (P[2] - P[1]).magnitude + (P[3] - P[2]).magnitude;
                int n = Mathf.Clamp(Mathf.CeilToInt(len / SampleStep), 4, 120);

                Vector3[] vertices = new Vector3[(n + 1) * 2];
                Vector2[] uv = new Vector2[vertices.Length];
                int[] triangles = new int[n * 6];

                Vector3 lastNormal = Vector3.zero;
                float dist = 0f;
                Vector3 prev = P[0];
                for (int i = 0; i <= n; i++)
                {
                    float u = i / (float)n;
                    Vector3 pos = line.Position(u);
                    Vector3 tangent = Tangent(P, u);

                    Vector3 normal = Vector3.Cross(tangent, Vector3.up); // points to the left of the travel direction
                    normal.y = 0f;
                    if (normal.sqrMagnitude > 1e-8f) normal.Normalize();
                    else normal = lastNormal;
                    lastNormal = normal;

                    dist += (pos - prev).magnitude;
                    prev = pos;

                    vertices[i * 2] = pos + normal * halfWidth;
                    vertices[i * 2 + 1] = pos - normal * halfWidth;
                    uv[i * 2] = new Vector2(0f, dist);
                    uv[i * 2 + 1] = new Vector2(1f, dist);
                }

                for (int i = 0; i < n; i++)
                {
                    int v0 = i * 2, v1 = v0 + 1, v2 = v0 + 2, v3 = v0 + 3;
                    int t = i * 6;
                    triangles[t] = v0;
                    triangles[t + 1] = v2;
                    triangles[t + 2] = v1;
                    triangles[t + 3] = v1;
                    triangles[t + 4] = v2;
                    triangles[t + 5] = v3;
                }

                if (item.Mesh == null)
                {
                    item.Mesh = new Mesh();
                    item.Mesh.name = "QuayTools decal " + id;
                }
                else
                {
                    item.Mesh.Clear();
                }
                item.Mesh.vertices = vertices;
                item.Mesh.uv = uv;
                item.Mesh.triangles = triangles;
                item.Mesh.RecalculateNormals();
                item.Mesh.RecalculateBounds();
            }
            catch (System.Exception ex)
            {
                item.Failed = true;
                Debug.LogError("[QuayTools] Decal path build failed for segment " + id + ": " + ex);
            }
        }

        /// <summary>Derivative of the cubic Bezier curve with control points P at parameter u.</summary>
        private static Vector3 Tangent(Vector3[] P, float u)
        {
            float v = 1f - u;
            return 3f * v * v * (P[1] - P[0]) + 6f * v * u * (P[2] - P[1]) + 3f * u * u * (P[3] - P[2]);
        }

        // ---------- drawing ----------

        private void Draw()
        {
            for (int i = 0; i < _ids.Count; i++)
            {
                Item item = _items[_ids[i]];
                if (item.Mesh == null || item.Failed) continue;

                Material m = GetMaterial(item.Settings.ColorIndex);
                if (m == null) return; // no usable shader, already logged
                Graphics.DrawMesh(item.Mesh, Matrix4x4.identity, m, 0, null, 0, null, false, false);
            }
        }

        private Material GetMaterial(int index)
        {
            index = Mathf.Clamp(index, 0, DecalStore.Colors.Length - 1);
            if (_materials == null) _materials = new Material[DecalStore.Colors.Length];
            if (_materials[index] != null) return _materials[index];

            if (!_shaderSearched)
            {
                _shaderSearched = true;
                string[] names = { "Sprites/Default", "Hidden/Internal-Colored", "UI/Default", "Unlit/Color", "Legacy Shaders/Transparent/Diffuse" };
                for (int i = 0; i < names.Length && _shader == null; i++)
                {
                    _shader = Shader.Find(names[i]);
                }

                if (_shader == null) Debug.LogError("[QuayTools] Decal paths: no usable shader found (tried Sprites/Default, Hidden/Internal-Colored, UI/Default, Unlit/Color, Legacy Shaders/Transparent/Diffuse)");
                else Debug.Log("[QuayTools] Decal paths use shader " + _shader.name);
            }
            if (_shader == null) return null;

            Material m = new Material(_shader);
            m.color = DecalStore.Colors[index];
            m.renderQueue = 3000;
            if (m.HasProperty("_Cull")) m.SetInt("_Cull", 0);
            if (m.HasProperty("_ZWrite")) m.SetInt("_ZWrite", 0);
            if (m.HasProperty("_ZTest")) m.SetInt("_ZTest", 4); // LessEqual
            if (m.HasProperty("_SrcBlend")) m.SetInt("_SrcBlend", 5); // SrcAlpha
            if (m.HasProperty("_DstBlend")) m.SetInt("_DstBlend", 10); // OneMinusSrcAlpha
            _materials[index] = m;
            return m;
        }
    }
}
