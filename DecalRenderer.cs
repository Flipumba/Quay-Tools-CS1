using System.Collections.Generic;
using ColossalFramework;
using ColossalFramework.Math;
using UnityEngine;

namespace QuayTools
{
    /// <summary>
    /// Draws the decal paths along the middle of the top surface of quay segments.
    /// The path is a mask: a strip of the chosen width, built from the two edge curves of the segment (the real node
    /// corners, so node edits made with other mods are followed) blended at the middle, so it follows curves and heights.
    /// A decal prop is laid over the strip: its texture is repeated along and across the strip in tiles of the chosen
    /// size and cropped by the strip edges, and drawn with the prop's own material and the chosen tint.
    /// Without a decal the strip is a plain coloured surface. One mesh per segment, drawn with Graphics.DrawMesh.
    /// Main thread only. Lives on the QuayToolsController game object.
    /// </summary>
    public class DecalRenderer : MonoBehaviour
    {
        private const float SolidLift = 0.05f;     // metres above the surface, against z-fighting (plain strip)
        private const float DecalLift = 0.02f;     // decal shaders bring their own depth offset
        private const float SampleStep = 1.0f;     // metres between cross sections
        private const int MaxVertices = 60000;     // a mesh holds at most 65535
        private const int MaxBuildsPerFrame = 12;
        private const int ChecksPerFrame = 6;
        private const float RetrySeconds = 3f;

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
            public PropInfo Prop;        // null: plain strip
            public Corners Last;
            public bool Dirty = true;
            public bool Failed;
            public float RetryAt;
            public bool Logged;
            public bool FailLogged;
        }

        private readonly Dictionary<ushort, Item> _items = new Dictionary<ushort, Item>();
        private readonly List<ushort> _ids = new List<ushort>();
        private readonly List<ushort> _dead = new List<ushort>();
        private readonly MaterialPropertyBlock _block = new MaterialPropertyBlock();
        private Material[] _materials;
        private Shader _shader;
        private bool _shaderSearched;
        private int _version = -1;
        private int _cursor;
        private int _diagnostics;

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
            float now = Time.realtimeSinceStartup;

            // build what is new or changed
            int builds = 0;
            for (int i = 0; i < _ids.Count && builds < MaxBuildsPerFrame; i++)
            {
                Item item = _items[_ids[i]];
                if (item.Failed && now >= item.RetryAt)
                {
                    item.Failed = false;
                    item.Dirty = true;
                }
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
                if (ReadCorners(id, out c) && !c.Same(item.Last)) item.Dirty = true;
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

        private static bool Finite(Vector3 v)
        {
            return !(float.IsNaN(v.x) || float.IsNaN(v.y) || float.IsNaN(v.z) ||
                     float.IsInfinity(v.x) || float.IsInfinity(v.y) || float.IsInfinity(v.z));
        }

        /// <summary>Bezier control points of the path centre line from the four real corners of the segment.</summary>
        private static bool CornerControlPoints(Corners c, DecalSettings s, float lift, Vector3[] P)
        {
            Vector3 mL1, mL2, mR1, mR2;
            NetSegment.CalculateMiddlePoints(c.sL, c.dSL, c.eL, c.dEL, c.smSL, c.smEL, out mL1, out mL2);
            NetSegment.CalculateMiddlePoints(c.sR, c.dSR, c.eR, c.dER, c.smSR, c.smER, out mR1, out mR2);

            Vector3[] left = { c.sL, mL1, mL2, c.eL };
            Vector3[] right = { c.sR, mR1, mR2, c.eR };
            float lateral = s.Lateral * FenceStore.Unit;

            for (int i = 0; i < 4; i++)
            {
                P[i] = Vector3.Lerp(left[i], right[i], 0.5f);
                Vector3 lat = right[i] - left[i];
                lat.y = 0f;
                if (lat.sqrMagnitude > 1e-6f) P[i] += lat.normalized * lateral;
                P[i].y += lift;
                if (!Finite(P[i])) return false;
            }
            return true;
        }

        /// <summary>Fallback when the corners are unusable: the centre line between the two nodes.</summary>
        private static void NodeControlPoints(ushort id, DecalSettings s, float lift, Vector3[] P)
        {
            NetManager nm = NetManager.instance;
            NetSegment seg = nm.m_segments.m_buffer[id];
            Vector3 a = nm.m_nodes.m_buffer[seg.m_startNode].m_position;
            Vector3 d = nm.m_nodes.m_buffer[seg.m_endNode].m_position;
            Vector3 b, c;
            NetSegment.CalculateMiddlePoints(a, seg.m_startDirection, d, seg.m_endDirection, false, false, out b, out c);

            Vector3[] pts = { a, b, c, d };
            float lateral = s.Lateral * FenceStore.Unit;
            for (int i = 0; i < 4; i++)
            {
                Vector3 dir = i < 3 ? pts[i + 1] - pts[i] : pts[3] - pts[2];
                dir.y = 0f;
                Vector3 right = dir.sqrMagnitude > 1e-6f ? Vector3.Cross(Vector3.up, dir).normalized : Vector3.zero;
                P[i] = pts[i] + right * lateral;
                P[i].y += lift;
            }
        }

        private void Fail(ushort id, Item item, string why, float seconds)
        {
            item.Failed = true;
            item.RetryAt = Time.realtimeSinceStartup + seconds;
            if (!item.FailLogged)
            {
                item.FailLogged = true;
                Debug.LogWarning("[QuayTools] Decal path on segment " + id + ": " + why);
            }
        }

        private void Build(ushort id, Item item)
        {
            item.Dirty = false;
            try
            {
                PropInfo prop = null;
                if (!string.IsNullOrEmpty(item.Settings.Prop))
                {
                    prop = DecalCatalog.Find(item.Settings.Prop);
                    if (prop == null || !DecalCatalog.IsDecal(prop))
                    {
                        Fail(id, item, "decal '" + item.Settings.Prop + "' is not available (asset missing or not a plain decal)", 30f);
                        return;
                    }
                }
                item.Prop = prop;

                float lift = (prop != null ? DecalLift : SolidLift) + item.Settings.Lift * FenceStore.Unit;
                Vector3[] P = new Vector3[4];

                Corners c;
                bool ok = ReadCorners(id, out c);
                if (ok) item.Last = c;
                ok = ok && CornerControlPoints(c, item.Settings, lift, P);
                if (!ok)
                {
                    // corners unusable (edited node in an unusual state): use the node centre line so something is shown
                    NodeControlPoints(id, item.Settings, lift, P);
                    Debug.LogWarning("[QuayTools] Decal path on segment " + id + ": segment corners are not usable, using the node centre line");
                }

                for (int i = 0; i < 4; i++)
                {
                    if (!Finite(P[i]))
                    {
                        Fail(id, item, "invalid geometry", RetrySeconds);
                        return;
                    }
                }

                Path path = SamplePath(P);
                float width = Mathf.Clamp(item.Settings.Width, DecalStore.MinWidth, DecalStore.MaxWidth) * FenceStore.Unit;

                if (item.Mesh == null)
                {
                    item.Mesh = new Mesh();
                    item.Mesh.name = "QuayTools decal " + id;
                }
                else
                {
                    item.Mesh.Clear();
                }

                MeshData data = prop != null
                    ? BuildTextured(path, width, item.Settings.Scale * FenceStore.Unit, prop)
                    : BuildSolid(path, width);

                item.Mesh.vertices = data.Vertices.ToArray();
                item.Mesh.uv = data.Uv.ToArray();
                item.Mesh.normals = data.Normals.ToArray();
                item.Mesh.tangents = data.Tangents.ToArray();
                item.Mesh.triangles = data.Triangles.ToArray();
                item.Mesh.RecalculateBounds();

                if (_diagnostics < 6 || !item.Logged)
                {
                    if (_diagnostics < 6) Debug.Log("[QuayTools] Decal path built: segment " + id + ", length " + path.Length.ToString("0.0") +
                        " m, " + data.Vertices.Count + " vertices, " + (prop != null ? "decal " + prop.name : "plain strip"));
                    _diagnostics++;
                    item.Logged = true;
                }
            }
            catch (System.Exception ex)
            {
                Fail(id, item, "build failed, will retry: " + ex, RetrySeconds);
            }
        }

        // ---------- path sampling ----------

        /// <summary>Points along the path with the direction to the left and the distance from the start.</summary>
        private class Path
        {
            public Vector3[] Pos;
            public Vector3[] Left;
            public float[] Dist;
            public float Length;
            private int _cursor;

            public void ResetCursor()
            {
                _cursor = 0;
            }

            /// <summary>Position and left direction at distance d from the start; d must not decrease between calls after ResetCursor.</summary>
            public void Eval(float d, out Vector3 pos, out Vector3 left)
            {
                if (d <= 0f) { pos = Pos[0]; left = Left[0]; return; }
                if (d >= Length) { pos = Pos[Pos.Length - 1]; left = Left[Left.Length - 1]; return; }

                while (_cursor > 0 && Dist[_cursor] > d) _cursor--;
                while (_cursor < Pos.Length - 2 && Dist[_cursor + 1] < d) _cursor++;

                float span = Dist[_cursor + 1] - Dist[_cursor];
                float t = span > 1e-5f ? (d - Dist[_cursor]) / span : 0f;
                pos = Vector3.Lerp(Pos[_cursor], Pos[_cursor + 1], t);
                left = Vector3.Lerp(Left[_cursor], Left[_cursor + 1], t);
                if (left.sqrMagnitude > 1e-8f) left.Normalize();
                else left = Left[_cursor];
            }
        }

        /// <summary>Derivative of the cubic Bezier curve with control points P at parameter u.</summary>
        private static Vector3 Tangent(Vector3[] P, float u)
        {
            float v = 1f - u;
            return 3f * v * v * (P[1] - P[0]) + 6f * v * u * (P[2] - P[1]) + 3f * u * u * (P[3] - P[2]);
        }

        private static Path SamplePath(Vector3[] P)
        {
            Bezier3 line = new Bezier3();
            line.a = P[0];
            line.b = P[1];
            line.c = P[2];
            line.d = P[3];

            float rough = (P[1] - P[0]).magnitude + (P[2] - P[1]).magnitude + (P[3] - P[2]).magnitude;
            int n = Mathf.Clamp(Mathf.CeilToInt(rough / SampleStep), 8, 300);

            Path path = new Path();
            path.Pos = new Vector3[n + 1];
            path.Left = new Vector3[n + 1];
            path.Dist = new float[n + 1];

            Vector3 lastLeft = Vector3.left;
            for (int i = 0; i <= n; i++)
            {
                float u = i / (float)n;
                path.Pos[i] = line.Position(u);

                Vector3 left = Vector3.Cross(Tangent(P, u), Vector3.up); // to the left of the travel direction
                left.y = 0f;
                if (left.sqrMagnitude > 1e-8f) left.Normalize();
                else left = lastLeft;
                lastLeft = left;
                path.Left[i] = left;

                path.Dist[i] = i == 0 ? 0f : path.Dist[i - 1] + (path.Pos[i] - path.Pos[i - 1]).magnitude;
            }
            path.Length = Mathf.Max(path.Dist[n], 0.01f);
            return path;
        }

        // ---------- meshes ----------

        private class MeshData
        {
            public readonly List<Vector3> Vertices = new List<Vector3>();
            public readonly List<Vector2> Uv = new List<Vector2>();
            public readonly List<Vector3> Normals = new List<Vector3>();
            public readonly List<Vector4> Tangents = new List<Vector4>();
            public readonly List<int> Triangles = new List<int>();
        }

        private static void AddVertex(MeshData m, Vector3 pos, Vector3 right, float u, float v)
        {
            m.Vertices.Add(pos);
            m.Uv.Add(new Vector2(u, v));
            m.Normals.Add(Vector3.up);
            m.Tangents.Add(new Vector4(right.x, right.y, right.z, -1f));
        }

        /// <summary>Two vertices per cross section (left, right); winding faces up.</summary>
        private static void AddQuadStrip(MeshData m, int firstVertex, int sections)
        {
            for (int s = 0; s < sections - 1; s++)
            {
                int v0 = firstVertex + s * 2, v1 = v0 + 1, v2 = v0 + 2, v3 = v0 + 3;
                m.Triangles.Add(v0);
                m.Triangles.Add(v2);
                m.Triangles.Add(v1);
                m.Triangles.Add(v1);
                m.Triangles.Add(v2);
                m.Triangles.Add(v3);
            }
        }

        private static MeshData BuildSolid(Path path, float width)
        {
            MeshData m = new MeshData();
            int n = Mathf.Clamp(Mathf.CeilToInt(path.Length / 1.5f), 4, 120);
            float half = width * 0.5f;
            path.ResetCursor();

            for (int i = 0; i <= n; i++)
            {
                float d = path.Length * i / n;
                Vector3 pos, left;
                path.Eval(d, out pos, out left);
                AddVertex(m, pos + left * half, -left, 0f, d);
                AddVertex(m, pos - left * half, -left, 1f, d);
            }
            AddQuadStrip(m, 0, n + 1);
            return m;
        }

        /// <summary>
        /// The decal texture is laid over the strip in tiles of (tile x tile * aspect) metres; the tile grid is centred on
        /// the middle of the strip and starts at the segment start, and the strip edges crop the texture. Every tile
        /// piece has its own vertices with UV inside 0..1, so textures that do not repeat (clamped) work as well.
        /// </summary>
        private static MeshData BuildTextured(Path path, float width, float tile, PropInfo prop)
        {
            Vector3 size = prop.m_mesh.bounds.size;
            float aspect = size.x > 0.01f && size.z > 0.01f ? size.z / size.x : 1f;
            float half = width * 0.5f;
            float len = path.Length;

            int cols = 1, rows = 1, steps = 1;
            for (int attempt = 0; attempt < 16; attempt++)
            {
                float tileLen = tile * aspect;
                rows = Mathf.Max(1, Mathf.CeilToInt(len / tileLen));
                int bounds = Mathf.Max(0, Mathf.CeilToInt(half / tile - 0.5f));
                cols = 1 + 2 * bounds;
                steps = Mathf.Clamp(Mathf.CeilToInt(Mathf.Min(tileLen, len) / SampleStep), 1, 64);

                long vertices = (long)cols * rows * (steps + 1) * 2;
                if (vertices <= MaxVertices) break;
                tile *= 1.5f; // too many pieces: make the tiles bigger
            }

            float tl = tile * aspect;

            // lateral boundaries of the columns, measured from the middle (right positive)
            List<float> xs = new List<float>();
            xs.Add(-half);
            int nb = (cols - 1) / 2;
            for (int k = -nb; k < nb; k++) xs.Add((k + 0.5f) * tile);
            xs.Add(half);

            MeshData m = new MeshData();
            for (int c = 0; c < cols; c++)
            {
                float xa = xs[c], xb = xs[c + 1];
                float centre = Mathf.Round((xa + xb) * 0.5f / tile) * tile; // centre of the tile this column belongs to
                float ua = (xa - centre) / tile + 0.5f;
                float ub = (xb - centre) / tile + 0.5f;

                for (int r = 0; r < rows; r++)
                {
                    float d0 = r * tl;
                    float d1 = Mathf.Min(len, d0 + tl);
                    if (d1 - d0 < 1e-4f) continue;

                    int k = Mathf.Clamp(Mathf.CeilToInt((d1 - d0) / SampleStep), 1, steps);
                    path.ResetCursor();
                    int first = m.Vertices.Count;
                    for (int s = 0; s <= k; s++)
                    {
                        float d = d0 + (d1 - d0) * s / k;
                        float v = (d - d0) / tl;
                        Vector3 pos, left;
                        path.Eval(d, out pos, out left);
                        AddVertex(m, pos - left * xa, -left, ua, v); // x is right positive, left points to the left
                        AddVertex(m, pos - left * xb, -left, ub, v);
                    }
                    AddQuadStrip(m, first, k + 1);
                }
            }
            return m;
        }

        // ---------- drawing ----------

        private void Draw()
        {
            PropManager pm = null;
            for (int i = 0; i < _ids.Count; i++)
            {
                Item item = _items[_ids[i]];
                if (item.Mesh == null || item.Failed) continue;

                Color tint = DecalStore.Colors[Mathf.Clamp(item.Settings.ColorIndex, 0, DecalStore.Colors.Length - 1)];

                if (item.Prop != null)
                {
                    PropInfo info = item.Prop;
                    if (info.m_material == null) continue;
                    if (pm == null) pm = Singleton<PropManager>.instance;

                    // the same call and material parameters the game uses for a decal prop
                    tint.a = 1f;
                    _block.Clear();
                    _block.SetColor(pm.ID_Color, tint);
                    _block.SetVector(pm.ID_ObjectIndex, RenderManager.DefaultColorLocation);
                    if (info.m_rollLocation != null)
                    {
                        info.m_material.SetVectorArray(pm.ID_RollLocation, info.m_rollLocation);
                        info.m_material.SetVectorArray(pm.ID_RollParams, info.m_rollParams);
                    }
                    Graphics.DrawMesh(item.Mesh, Matrix4x4.identity, info.m_material, info.m_prefabDataLayer, null, 0, _block);
                }
                else
                {
                    Material m = GetMaterial(item.Settings.ColorIndex);
                    if (m == null) continue; // no usable shader, already logged
                    Graphics.DrawMesh(item.Mesh, Matrix4x4.identity, m, 0, null, 0, null, false, false);
                }
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

                if (_shader == null) Debug.LogError("[QuayTools] Plain decal strips: no usable shader found (tried Sprites/Default, Hidden/Internal-Colored, UI/Default, Unlit/Color, Legacy Shaders/Transparent/Diffuse)");
                else Debug.Log("[QuayTools] Plain decal strips use shader " + _shader.name);
            }
            if (_shader == null) return null;

            Material mat = new Material(_shader);
            mat.color = DecalStore.Colors[index];
            mat.renderQueue = 3000;
            if (mat.HasProperty("_Cull")) mat.SetInt("_Cull", 0);
            if (mat.HasProperty("_ZWrite")) mat.SetInt("_ZWrite", 0);
            if (mat.HasProperty("_ZTest")) mat.SetInt("_ZTest", 4); // LessEqual
            if (mat.HasProperty("_SrcBlend")) mat.SetInt("_SrcBlend", 5); // SrcAlpha
            if (mat.HasProperty("_DstBlend")) mat.SetInt("_DstBlend", 10); // OneMinusSrcAlpha
            _materials[index] = mat;
            return mat;
        }
    }
}
