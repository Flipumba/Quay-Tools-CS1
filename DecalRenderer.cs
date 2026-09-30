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
    /// size and cropped by the strip edges, and drawn with the chosen tint (composite unlit texture by default, or the prop's own material as an option).
    /// Without a decal the strip is a plain coloured surface. One mesh per segment, drawn with Graphics.DrawMesh.
    /// Main thread only. Lives on the QuayToolsController game object.
    /// </summary>
    public class DecalRenderer : MonoBehaviour
    {
        private const float SolidLift = 0.05f;     // metres above the surface, against z-fighting (plain strip)
        private const float DecalLift = 0.05f;
        private const float SampleStep = 1.0f;     // metres between cross sections
        private const int MaxVertices = 60000;     // a mesh holds at most 65535
        private const int MaxBuildsPerFrame = 12;
        private const int ChecksPerFrame = 6;
        private const float RetrySeconds = 3f;
        private const int MaxTiles = 1500;         // placed decals per segment

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
            public bool BridgeStart, BridgeEnd; // path continues across the node to the neighbour segment
            public List<Matrix4x4> Tiles;   // placed game decals (one matrix per tile)
            public bool Placed;
            public bool WaterRight;      // water side of the segment when built (the sideways shift is signed toward water)
        }

        private readonly Dictionary<ushort, Item> _items = new Dictionary<ushort, Item>();
        private readonly List<ushort> _ids = new List<ushort>();
        private readonly List<ushort> _dead = new List<ushort>();
        private readonly MaterialPropertyBlock _block = new MaterialPropertyBlock();
        private Material _plain;
        private readonly Dictionary<PropInfo, Material> _simple = new Dictionary<PropInfo, Material>();
        private readonly Dictionary<PropInfo, Texture2D> _composite = new Dictionary<PropInfo, Texture2D>();
        private readonly HashSet<PropInfo> _compositeFailed = new HashSet<PropInfo>();
        private Shader _shader;
        private bool _shaderSearched;
        private bool _shaderLit;
        private int _version = -1;
        private int _cursor;
        private int _diagnostics;
        private int _gapLogs;

        private void OnDestroy()
        {
            foreach (KeyValuePair<ushort, Item> kv in _items)
            {
                if (kv.Value.Mesh != null) Destroy(kv.Value.Mesh);
            }
            _items.Clear();

            if (_plain != null) Destroy(_plain);

            foreach (KeyValuePair<PropInfo, Material> kv in _simple)
            {
                if (kv.Value != null) Destroy(kv.Value);
            }
            _simple.Clear();

            foreach (KeyValuePair<PropInfo, Texture2D> kv in _composite)
            {
                if (kv.Value != null) Destroy(kv.Value);
            }
            _composite.Clear();
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

                // a neighbour got or lost its path: the bridge across the node changes
                if (BridgeAt(id, segs[id].m_startNode) != item.BridgeStart || BridgeAt(id, segs[id].m_endNode) != item.BridgeEnd) item.Dirty = true;

                // the segment was inverted: the water is on the other side now, the sideways shift follows it
                if (QuayGeometry.GetFrame(id).WaterIsRight != item.WaterRight) item.Dirty = true;

                // the rendering mode was changed in the options
                if (item.Prop != null && item.Placed != Settings.DecalPlaced) item.Dirty = true;
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
        private static bool CornerControlPoints(Corners c, DecalSettings s, float lift, bool waterRight, Vector3[] P)
        {
            Vector3 mL1, mL2, mR1, mR2;
            NetSegment.CalculateMiddlePoints(c.sL, c.dSL, c.eL, c.dEL, c.smSL, c.smEL, out mL1, out mL2);
            NetSegment.CalculateMiddlePoints(c.sR, c.dSR, c.eR, c.dER, c.smSR, c.smER, out mR1, out mR2);

            Vector3[] left = { c.sL, mL1, mL2, c.eL };
            Vector3[] right = { c.sR, mR1, mR2, c.eR };
            float lateral = s.Lateral * FenceStore.Unit * (waterRight ? 1f : -1f); // + toward the water

            for (int i = 0; i < 4; i++)
            {
                // the shift is a change of the blend parameter between the two edge curves, so the path stays on the
                // surface between them (heights, cross slope and twist included)
                Vector3 span = right[i] - left[i];
                Vector3 flat = span;
                flat.y = 0f;
                float w = flat.magnitude;
                float t = 0.5f;
                if (w > 0.05f) t = Mathf.Clamp(0.5f + lateral / w, -1f, 2f);
                P[i] = left[i] + span * t;
                P[i].y += lift;
                if (!Finite(P[i])) return false;
            }
            return true;
        }

        /// <summary>Fallback when the corners are unusable: the centre line between the two nodes.</summary>
        private static void NodeControlPoints(ushort id, DecalSettings s, float lift, bool waterRight, Vector3[] P)
        {
            NetManager nm = NetManager.instance;
            NetSegment seg = nm.m_segments.m_buffer[id];
            Vector3 a = nm.m_nodes.m_buffer[seg.m_startNode].m_position;
            Vector3 d = nm.m_nodes.m_buffer[seg.m_endNode].m_position;
            Vector3 b, c;
            NetSegment.CalculateMiddlePoints(a, seg.m_startDirection, d, seg.m_endDirection, false, false, out b, out c);

            Vector3[] pts = { a, b, c, d };
            float lateral = s.Lateral * FenceStore.Unit * (waterRight ? 1f : -1f);
            for (int i = 0; i < 4; i++)
            {
                Vector3 dir = i < 3 ? pts[i + 1] - pts[i] : pts[3] - pts[2];
                dir.y = 0f;
                Vector3 right = dir.sqrMagnitude > 1e-6f ? Vector3.Cross(Vector3.up, dir).normalized : Vector3.zero;
                P[i] = pts[i] + right * lateral;
                P[i].y += lift;
            }
        }

        /// <summary>
        /// True when the node joins exactly two segments. Segment ends can
        /// be moved away from the node (Node Controller Renewal); the node area between the two ends is then part of no
        /// segment, so the path is continued up to the node (the neighbour does the same from its side).
        /// </summary>
        private static bool BridgeAt(ushort id, ushort nodeId)
        {
            if (nodeId == 0) return false;
            NetNode node = NetManager.instance.m_nodes.m_buffer[nodeId];
            int count = 0;
            ushort other = 0;
            for (int i = 0; i < 8; i++)
            {
                ushort seg = node.GetSegment(i);
                if (seg == 0) continue;
                count++;
                if (seg != id) other = seg;
            }
            return count == 2 && other != 0;
        }

        /// <summary>
        /// End point of the neighbour's path at the node (the neighbour is the only other segment of the node). The
        /// neighbour's corners already contain the Node Controller Renewal edits, so this is where the real surface continues.
        /// </summary>
        private static bool NeighbourEnd(ushort id, ushort nodeId, DecalSettings fallback, float baseLift, out Vector3 point)
        {
            point = Vector3.zero;
            if (nodeId == 0) return false;

            NetManager nm = NetManager.instance;
            NetNode node = nm.m_nodes.m_buffer[nodeId];
            ushort other = 0;
            for (int i = 0; i < 8; i++)
            {
                ushort seg = node.GetSegment(i);
                if (seg != 0 && seg != id) other = seg;
            }
            if (other == 0) return false;

            Corners oc;
            if (!ReadCorners(other, out oc)) return false;

            DecalSettings os;
            if (!DecalStore.TryGet(other, out os)) os = fallback;

            Vector3[] T = new Vector3[4];
            bool wr = QuayGeometry.GetFrame(other).WaterIsRight;
            if (!CornerControlPoints(oc, os, baseLift + os.Lift * FenceStore.Unit, wr, T)) return false;

            point = nm.m_segments.m_buffer[other].m_startNode == nodeId ? T[0] : T[3];
            return true;
        }

        /// <summary>Adds a straight piece at the start and/or the end of the path (up to the node).</summary>
        private static void Extend(Path path, Vector3? atStart, Vector3? atEnd)
        {
            if (!atStart.HasValue && !atEnd.HasValue) return;

            int n = path.Pos.Length;
            int add = (atStart.HasValue ? 1 : 0) + (atEnd.HasValue ? 1 : 0);
            Vector3[] pos = new Vector3[n + add];
            Vector3[] left = new Vector3[n + add];
            int o = 0;
            if (atStart.HasValue)
            {
                pos[0] = atStart.Value;
                left[0] = path.Left[0];
                o = 1;
            }
            for (int i = 0; i < n; i++)
            {
                pos[o + i] = path.Pos[i];
                left[o + i] = path.Left[i];
            }
            if (atEnd.HasValue)
            {
                pos[n + o] = atEnd.Value;
                left[n + o] = path.Left[n - 1];
            }

            float[] dist = new float[n + add];
            for (int i = 1; i < dist.Length; i++) dist[i] = dist[i - 1] + (pos[i] - pos[i - 1]).magnitude;

            path.Pos = pos;
            path.Left = left;
            path.Dist = dist;
            path.Length = Mathf.Max(dist[dist.Length - 1], 0.01f);
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
                bool waterRight = QuayGeometry.GetFrame(id).WaterIsRight;
                item.WaterRight = waterRight;

                Corners c;
                bool ok = ReadCorners(id, out c);
                if (ok) item.Last = c;
                ok = ok && CornerControlPoints(c, item.Settings, lift, waterRight, P);
                if (!ok)
                {
                    // corners unusable (edited node in an unusual state): use the node centre line so something is shown
                    NodeControlPoints(id, item.Settings, lift, waterRight, P);
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

                NetSegment seg = NetManager.instance.m_segments.m_buffer[id];
                item.BridgeStart = BridgeAt(id, seg.m_startNode);
                item.BridgeEnd = BridgeAt(id, seg.m_endNode);
                if (ok && _gapLogs < 24)
                {
                    NetNode[] nn = NetManager.instance.m_nodes.m_buffer;
                    Vector3 ms = (c.sL + c.sR) * 0.5f, me = (c.eL + c.eR) * 0.5f;
                    Vector3 gs = nn[seg.m_startNode].m_position - ms, ge = nn[seg.m_endNode].m_position - me;
                    float hs = new Vector2(gs.x, gs.z).magnitude, he = new Vector2(ge.x, ge.z).magnitude;
                    if (hs > 0.3f || he > 0.3f || Mathf.Abs(gs.y) > 0.3f || Mathf.Abs(ge.y) > 0.3f)
                    {
                        _gapLogs++;
                        Debug.Log("[QuayTools] Segment " + id + " ends are away from the nodes: start " + hs.ToString("0.0") + " m (dy " + gs.y.ToString("0.0") + ", bridge " + item.BridgeStart +
                            "), end " + he.ToString("0.0") + " m (dy " + ge.y.ToString("0.0") + ", bridge " + item.BridgeEnd + ")");
                    }
                }
                if (ok && (item.BridgeStart || item.BridgeEnd))
                {
                    NetNode[] nodes = NetManager.instance.m_nodes.m_buffer;
                    Vector3? qs = null, qe = null;
                    float baseLift = prop != null ? DecalLift : SolidLift;
                    if (item.BridgeStart)
                    {
                        Vector3 po;
                        Vector3 q;
                        if (NeighbourEnd(id, seg.m_startNode, item.Settings, baseLift, out po))
                        {
                            q = (P[0] + po) * 0.5f; // both segments continue to the middle of the gap between their ends
                        }
                        else
                        {
                            q = nodes[seg.m_startNode].m_position + (P[0] - (c.sL + c.sR) * 0.5f);
                            q.y = P[0].y;
                        }
                        Vector3 d = q - P[0];
                        d.y = 0f;
                        if (d.sqrMagnitude > 0.0025f) qs = q;
                    }
                    if (item.BridgeEnd)
                    {
                        Vector3 po;
                        Vector3 q;
                        if (NeighbourEnd(id, seg.m_endNode, item.Settings, baseLift, out po))
                        {
                            q = (P[3] + po) * 0.5f;
                        }
                        else
                        {
                            q = nodes[seg.m_endNode].m_position + (P[3] - (c.eL + c.eR) * 0.5f);
                            q.y = P[3].y;
                        }
                        Vector3 d = q - P[3];
                        d.y = 0f;
                        if (d.sqrMagnitude > 0.0025f) qe = q;
                    }
                    Extend(path, qs, qe);
                }

                float width = Mathf.Clamp(item.Settings.Width, DecalStore.MinWidth, DecalStore.MaxWidth) * FenceStore.Unit;

                item.Placed = prop != null && Settings.DecalPlaced;
                if (item.Placed)
                {
                    item.Tiles = BuildTiles(path, width, item.Settings.Scale * FenceStore.Unit, item.Settings.Step * FenceStore.Unit, item.Settings.Box * FenceStore.Unit, prop);
                    if (item.Mesh != null) item.Mesh.Clear();
                    if (_diagnostics < 8 || !item.Logged)
                    {
                        if (_diagnostics < 8) Debug.Log("[QuayTools] Decal path placed: segment " + id + ", length " + path.Length.ToString("0.0") + " m, " + item.Tiles.Count + " tiles, decal " + prop.name +
                            ", mesh size " + prop.m_mesh.bounds.size + ", centre " + prop.m_mesh.bounds.center);
                        _diagnostics++;
                        item.Logged = true;
                    }
                    return;
                }
                item.Tiles = null;

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
                item.Mesh.uv2 = data.Uv.ToArray();
                Color32[] white = new Color32[data.Vertices.Count];
                for (int i = 0; i < white.Length; i++) white[i] = new Color32(255, 255, 255, 255);
                item.Mesh.colors32 = white;
                item.Mesh.triangles = data.Triangles.ToArray();
                item.Mesh.RecalculateBounds();

                if (_diagnostics < 6 || !item.Logged)
                {
                    if (_diagnostics < 6) Debug.Log("[QuayTools] Decal path built: segment " + id + ", length " + path.Length.ToString("0.0") + (item.BridgeStart || item.BridgeEnd ? " (bridged)" : "") +
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

        /// <summary>
        /// Game decals placed step by step: tiles of the decal prop along the path, each drawn with its own matrix like a
        /// prop instance (the game's decal shader projects the texture onto whatever is inside the tile's box, so heights,
        /// slopes and node edits are followed by the surface itself). No mask cropping: the footprint is the tile grid.
        /// </summary>
        private static List<Matrix4x4> BuildTiles(Path path, float width, float tile, float step, float boxHeight, PropInfo prop)
        {
            Bounds b = prop.m_mesh.bounds;
            Vector3 size = b.size;
            float aspect = size.x > 0.01f && size.z > 0.01f ? size.z / size.x : 1f;
            float len = path.Length;

            int cols = 1, rows = 1;
            float tileW = width, tileL = len, spacing = len;
            for (int attempt = 0; attempt < 16; attempt++)
            {
                cols = Mathf.Max(1, Mathf.RoundToInt(width / tile));
                tileW = width / cols;
                float tl = tile * aspect;
                rows = Mathf.Max(1, Mathf.RoundToInt(len / (step > 0.01f ? step : tl)));
                spacing = len / rows;
                tileL = step > 0.01f ? tl : spacing; // with an own step the tile keeps its size (gaps or overlaps), otherwise tiles touch
                if ((long)cols * rows <= MaxTiles) break;
                tile *= 1.5f;
                if (step > 0.01f) step *= 1.5f;
            }

            float sx = size.x > 0.01f ? tileW / size.x : 1f;
            float sz = size.z > 0.01f ? tileL / size.z : 1f;
            float sy = size.y > 0.1f ? Mathf.Clamp(boxHeight / size.y, 0.02f, 100f) : 1f; // height of the projection box around the surface
            Vector3 scale = new Vector3(sx, sy, sz);
            Matrix4x4 centre = Matrix4x4.TRS(-b.center, Quaternion.identity, Vector3.one);

            List<Matrix4x4> tiles = new List<Matrix4x4>(cols * rows);
            path.ResetCursor();
            for (int r = 0; r < rows; r++)
            {
                Vector3 pos, left;
                path.Eval((r + 0.5f) * spacing, out pos, out left);
                Vector3 fwd = Vector3.Cross(Vector3.up, left);
                if (fwd.sqrMagnitude < 1e-6f) fwd = Vector3.forward;
                Quaternion rot = Quaternion.LookRotation(fwd, Vector3.up);

                for (int c = 0; c < cols; c++)
                {
                    float x = (c - (cols - 1) * 0.5f) * tileW; // right positive
                    Vector3 p = pos - left * x;
                    tiles.Add(Matrix4x4.TRS(p, rot, scale) * centre);
                }
            }
            return tiles;
        }

        // ---------- drawing ----------

        private void Draw()
        {
            PropManager pm = null;
            EnsureShader(); // also switches the materials when the shadow option changed
            bool receive = _shaderLit;
            for (int i = 0; i < _ids.Count; i++)
            {
                Item item = _items[_ids[i]];
                if (item.Failed) continue;

                Color tint = item.Settings.TintColor;

                if (item.Prop != null && item.Placed)
                {
                    PropInfo info = item.Prop;
                    if (item.Tiles == null || info.m_material == null || info.m_mesh == null) continue;
                    if (pm == null) pm = Singleton<PropManager>.instance;

                    // the same call and material parameters the game uses for a decal prop instance
                    _block.Clear();
                    _block.SetColor(pm.ID_Color, tint);
                    _block.SetVector(pm.ID_ObjectIndex, RenderManager.DefaultColorLocation);
                    if (info.m_rollLocation != null)
                    {
                        info.m_material.SetVectorArray(pm.ID_RollLocation, info.m_rollLocation);
                        info.m_material.SetVectorArray(pm.ID_RollParams, info.m_rollParams);
                    }
                    for (int k = 0; k < item.Tiles.Count; k++)
                    {
                        Graphics.DrawMesh(info.m_mesh, item.Tiles[k], info.m_material, info.m_prefabDataLayer, null, 0, _block);
                    }
                    continue;
                }

                if (item.Mesh == null) continue;

                if (item.Prop != null)
                {
                    PropInfo info = item.Prop;
                    if (info.m_material == null) continue;

                    Material simple = GetCompositeMaterial(info);
                    if (simple == null) continue;
                    _block.Clear();
                    _block.SetColor("_Color", tint);
                    Graphics.DrawMesh(item.Mesh, Matrix4x4.identity, simple, 0, null, 0, _block, false, receive); // never casts shadows
                }
                else
                {
                    Material m = GetPlainMaterial();
                    if (m == null) continue; // no usable shader, already logged
                    _block.Clear();
                    _block.SetColor("_Color", tint);
                    Graphics.DrawMesh(item.Mesh, Matrix4x4.identity, m, 0, null, 0, _block, false, receive);
                }
            }
        }

        private Shader EnsureShader()
        {
            bool lit = Settings.DecalReceiveShadows;
            if (_shaderSearched && lit != _shaderLit)
            {
                // the option was changed: start again with the other kind of shader
                if (_plain != null) Destroy(_plain);
                _plain = null;
                foreach (KeyValuePair<PropInfo, Material> kv in _simple)
                {
                    if (kv.Value != null) Destroy(kv.Value);
                }
                _simple.Clear();
                _shader = null;
                _shaderSearched = false;
            }

            if (!_shaderSearched)
            {
                _shaderSearched = true;
                _shaderLit = lit;
                string[] names = lit
                    ? new[] { "Legacy Shaders/Transparent/Diffuse", "Sprites/Default", "Hidden/Internal-Colored", "UI/Default", "Unlit/Color" }
                    : new[] { "Sprites/Default", "Hidden/Internal-Colored", "UI/Default", "Unlit/Color", "Legacy Shaders/Transparent/Diffuse" };
                for (int i = 0; i < names.Length && _shader == null; i++)
                {
                    _shader = Shader.Find(names[i]);
                }

                if (_shader == null) Debug.LogError("[QuayTools] Decal paths: no usable shader found (tried Sprites/Default, Hidden/Internal-Colored, UI/Default, Unlit/Color, Legacy Shaders/Transparent/Diffuse)");
                else Debug.Log("[QuayTools] Decal strips use shader " + _shader.name + (lit ? " (receive shadows)" : " (unlit)"));
            }
            return _shader;
        }

        /// <summary>
        /// Default rendering: an unlit material with a texture composed from the decal prop (rgb from the diffuse map,
        /// opacity from the R channel of the ACI map), tint through _Color.
        /// </summary>
        private Material GetCompositeMaterial(PropInfo info)
        {
            Material m;
            if (_simple.TryGetValue(info, out m) && m != null) return m;
            if (EnsureShader() == null) return null;

            Texture tex = GetCompositeTexture(info);
            if (tex == null) tex = info.m_material.mainTexture;

            m = new Material(_shader);
            m.mainTexture = tex;
            m.renderQueue = 3000;
            if (m.HasProperty("_Cull")) m.SetInt("_Cull", 0);
            if (m.HasProperty("_ZWrite")) m.SetInt("_ZWrite", 0);
            _simple[info] = m;
            return m;
        }

        private static Texture2D ReadBack(Texture source, int w, int h)
        {
            RenderTexture rt = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default);
            RenderTexture prev = RenderTexture.active;
            try
            {
                Graphics.Blit(source, rt);
                RenderTexture.active = rt;
                Texture2D t = new Texture2D(w, h, TextureFormat.ARGB32, false);
                t.ReadPixels(new Rect(0, 0, w, h), 0, 0, false);
                t.Apply(false);
                return t;
            }
            finally
            {
                RenderTexture.active = prev;
                RenderTexture.ReleaseTemporary(rt);
            }
        }

        private Texture2D GetCompositeTexture(PropInfo info)
        {
            Texture2D t;
            if (_composite.TryGetValue(info, out t) && t != null) return t;
            if (_compositeFailed.Contains(info)) return null;

            try
            {
                Material src = info.m_material;
                Texture main = src.mainTexture;
                if (main == null)
                {
                    _compositeFailed.Add(info);
                    return null;
                }

                Texture aci = src.HasProperty("_ACIMap") ? src.GetTexture("_ACIMap") : null;

                int w = Mathf.Clamp(main.width, 4, 1024);
                int h = Mathf.Clamp(main.height, 4, 1024);
                if (main.width > 1024 || main.height > 1024)
                {
                    float k = 1024f / Mathf.Max(main.width, main.height);
                    w = Mathf.Max(4, Mathf.RoundToInt(main.width * k));
                    h = Mathf.Max(4, Mathf.RoundToInt(main.height * k));
                }

                Texture2D tm = ReadBack(main, w, h);
                Color32[] px = tm.GetPixels32();
                Destroy(tm);

                bool usedAci = false;
                int mainMax = 0, aciMax = 0;
                for (int i = 0; i < px.Length; i++) mainMax = Mathf.Max(mainMax, px[i].a);

                if (aci != null)
                {
                    Texture2D ta = ReadBack(aci, w, h);
                    Color32[] ap = ta.GetPixels32();
                    Destroy(ta);
                    for (int i = 0; i < ap.Length; i++) aciMax = Mathf.Max(aciMax, ap[i].r);
                    if (aciMax > 8)
                    {
                        for (int i = 0; i < px.Length; i++) px[i].a = ap[i].r;
                        usedAci = true;
                    }
                }

                if (!usedAci && mainMax <= 8)
                {
                    for (int i = 0; i < px.Length; i++) px[i].a = 255; // no opacity information at all: opaque
                }

                t = new Texture2D(w, h, TextureFormat.RGBA32, true);
                t.name = "QuayTools decal " + info.name;
                t.SetPixels32(px);
                t.Apply(true);
                t.wrapMode = TextureWrapMode.Clamp;
                t.filterMode = FilterMode.Trilinear;
                t.anisoLevel = 4;
                _composite[info] = t;

                Debug.Log("[QuayTools] Decal texture composed for '" + info.name + "': " + w + "x" + h +
                    (aci != null ? ", ACI map " + aci.width + "x" + aci.height + ", max R " + aciMax : ", no ACI map") +
                    ", main max alpha " + mainMax + (usedAci ? " (opacity from ACI R)" : " (opacity from main alpha)"));
                return t;
            }
            catch (System.Exception ex)
            {
                _compositeFailed.Add(info);
                Debug.LogWarning("[QuayTools] Could not compose the texture of decal '" + info.name + "', using the plain main texture: " + ex.Message);
                return null;
            }
        }

        /// <summary>One material for all plain strips; the colour (with opacity) comes per draw through _Color.</summary>
        private Material GetPlainMaterial()
        {
            if (_plain != null) return _plain;
            if (EnsureShader() == null) return null;

            Material mat = new Material(_shader);
            mat.renderQueue = 3000;
            if (mat.HasProperty("_Cull")) mat.SetInt("_Cull", 0);
            if (mat.HasProperty("_ZWrite")) mat.SetInt("_ZWrite", 0);
            if (mat.HasProperty("_ZTest")) mat.SetInt("_ZTest", 4); // LessEqual
            if (mat.HasProperty("_SrcBlend")) mat.SetInt("_SrcBlend", 5); // SrcAlpha
            if (mat.HasProperty("_DstBlend")) mat.SetInt("_DstBlend", 10); // OneMinusSrcAlpha
            _plain = mat;
            return mat;
        }
    }
}
