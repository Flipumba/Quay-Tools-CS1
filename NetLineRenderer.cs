using System;
using System.Collections.Generic;
using System.Reflection;
using ColossalFramework;
using ColossalFramework.Math;
using UnityEngine;

namespace QuayTools
{
    /// <summary>
    /// Draws the network-model lines of quay segments. Every line is one network model (a fence, a wall...) that follows
    /// the same curve as a prop line (from the middle of the start node to the middle of the end node). The model is
    /// drawn the way the game draws fence pieces (the same shader inputs as NetSegment.RenderSegments, submitted
    /// directly with Graphics.DrawMesh, so other mods' patches of that routine cannot interfere), with render data
    /// that this class prepares like NetSegment.RefreshRoadFence. A model is turned around by the game's own
    /// "negated mesh scale" (a half turn, never a mirror image).
    /// Main thread only. Lives on the QuayToolsController game object.
    /// </summary>
    public class NetLineRenderer : MonoBehaviour
    {
        private const int MaxBuildsPerFrame = 6;
        private const int ChecksPerFrame = 6;
        private const float MaxDistance = 3500f;
        private const int CapStartPart = 60;
        private const int CapEndPart = 61;

        private const int TurnAroundRight = 512; // NetSegment.m_flags2 bit read by RenderSegments for a positive wOffset

        private class Part
        {
            public NetInfo.Segment[] Segs;   // the model's mesh pieces that are visible for the flags of this piece
            public RenderManager.Instance Data;
            public bool Turn;                // half turn of the model (the game's own turn-around)
            public Vector3 Centre;
            public float Radius;
        }

        private class Item
        {
            public NetLineSet Set;
            public DecalRenderer.Corners Last;
            public bool Dirty = true;
            public bool WaterRight;
            public int Joins;
            public readonly List<Part> Parts = new List<Part>();
        }

        private readonly Dictionary<ushort, Item> _items = new Dictionary<ushort, Item>();
        private readonly List<ushort> _ids = new List<ushort>();
        private readonly List<ushort> _dead = new List<ushort>();
        private readonly HashSet<string> _reported = new HashSet<string>();
        private int _version = -1;
        private int _cursor;
        private int _logged;

        private static MethodInfo _checkFlags;
        private static FieldInfo _flags2;
        private static FieldInfo _materialBlock, _idLeft, _idRight, _idScale, _idObject, _idColor, _idObjectColor, _idSurfA, _idSurfB, _idSurfMap, _idHeight, _idHeightMap;
        private static MethodInfo _heightMapping, _surfaceMapping, _windSpeed;
        private static bool _reflectionDone;
        private static bool _failed;

        internal static NetLineRenderer Instance;

        private void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private static void FindMethods()
        {
            if (_reflectionDone) return;
            _reflectionDone = true;

            BindingFlags all = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            _checkFlags = typeof(NetInfo.Segment).GetMethod("CheckFlags", all);
            _flags2 = typeof(NetSegment).GetField("m_flags2", all);
            _heightMapping = typeof(TerrainManager).GetMethod("GetHeightMapping", all);
            _surfaceMapping = typeof(TerrainManager).GetMethod("GetSurfaceMapping", all);
            _windSpeed = typeof(WeatherManager).GetMethod("GetWindSpeed", all, null, new Type[] { typeof(Vector3) }, null);

            _materialBlock = typeof(NetManager).GetField("m_materialBlock", all);
            _idLeft = typeof(NetManager).GetField("ID_LeftMatrix", all);
            _idRight = typeof(NetManager).GetField("ID_RightMatrix", all);
            _idScale = typeof(NetManager).GetField("ID_MeshScale", all);
            _idObject = typeof(NetManager).GetField("ID_ObjectIndex", all);
            _idColor = typeof(NetManager).GetField("ID_Color", all);
            _idObjectColor = typeof(NetManager).GetField("ID_ObjectColor", all);
            _idSurfA = typeof(NetManager).GetField("ID_SurfaceTexA", all);
            _idSurfB = typeof(NetManager).GetField("ID_SurfaceTexB", all);
            _idSurfMap = typeof(NetManager).GetField("ID_SurfaceMapping", all);
            _idHeight = typeof(NetManager).GetField("ID_HeightMap", all);
            _idHeightMap = typeof(NetManager).GetField("ID_HeightMapping", all);

            if (_checkFlags == null || _checkFlags.GetParameters().Length != 3 || _flags2 == null || _materialBlock == null || _idLeft == null ||
                _idRight == null || _idScale == null || _idObject == null || _idColor == null || _idObjectColor == null || _idSurfA == null ||
                _idSurfB == null || _idSurfMap == null || _idHeight == null || _idHeightMap == null)
            {
                _failed = true;
                Debug.LogWarning("[QuayTools] NetInfo.Segment.CheckFlags / NetManager shader ids not found or changed: network-model lines are unavailable");
            }
        }

        private void Update()
        {
            if (_version != NetLineStore.Version) Sync();
            if (_ids.Count == 0) return;

            NetSegment[] segs = NetManager.instance.m_segments.m_buffer;

            int builds = 0;
            for (int i = 0; i < _ids.Count && builds < MaxBuildsPerFrame; i++)
            {
                Item item = _items[_ids[i]];
                if (!item.Dirty) continue;
                Build(_ids[i], item);
                builds++;
            }

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
                if (item.Dirty) continue;

                DecalRenderer.Corners c;
                if (DecalRenderer.ReadCorners(id, out c) && !c.Same(item.Last)) item.Dirty = true;
                if (QuayGeometry.GetFrame(id).WaterIsRight != item.WaterRight) item.Dirty = true;
                if (JoinSignature(id) != item.Joins) item.Dirty = true;
            }
            for (int i = 0; i < _dead.Count; i++) NetLineStore.Remove(_dead[i]);
        }

        private void Sync()
        {
            _version = NetLineStore.Version;
            List<KeyValuePair<ushort, NetLineSet>> entries = NetLineStore.Snapshot();

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

                if (item.Set == null || !item.Set.SameAs(entries[i].Value))
                {
                    item.Set = entries[i].Value;
                    item.Dirty = true;
                }
            }

            List<ushort> remove = new List<ushort>();
            foreach (KeyValuePair<ushort, Item> kv in _items)
            {
                if (!present.Contains(kv.Key)) remove.Add(kv.Key);
            }
            for (int i = 0; i < remove.Count; i++) _items.Remove(remove[i]);

            _ids.Clear();
            _ids.AddRange(_items.Keys);
        }

        private static int JoinSignature(ushort id)
        {
            NetSegment seg = NetManager.instance.m_segments.m_buffer[id];
            return (PropLineRenderer.JoinsTwo(seg.m_startNode) ? 1 : 0) | (PropLineRenderer.JoinsTwo(seg.m_endNode) ? 2 : 0);
        }

        // ---------- building ----------

        private void Build(ushort id, Item item)
        {
            item.Dirty = false;
            item.Parts.Clear();

            if (_failed) return;
            FindMethods();
            if (_failed) return;

            try
            {
                NetSegment[] segs = NetManager.instance.m_segments.m_buffer;
                if ((segs[id].m_flags & NetSegment.Flags.Created) == NetSegment.Flags.None) return;

                DecalRenderer.Corners c;
                bool ok = DecalRenderer.ReadCorners(id, out c);
                if (ok) item.Last = c;

                bool waterRight = QuayGeometry.GetFrame(id).WaterIsRight;
                item.WaterRight = waterRight;
                item.Joins = JoinSignature(id);

                for (int k = 0; k < item.Set.Lines.Count; k++)
                {
                    NetLine line = item.Set.Lines[k];
                    if (string.IsNullOrEmpty(line.Model)) continue;

                    NetInfo fence = FenceCatalog.Find(line.Model);
                    if (fence == null || fence.m_segments == null || fence.m_segments.Length == 0)
                    {
                        if (_reported.Add(line.Model)) Debug.LogWarning("[QuayTools] Network line: '" + line.Model + "' is not available (asset missing or not usable)");
                        continue;
                    }

                    BuildLine(id, k, line, fence, c, ok, waterRight, item);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[QuayTools] Network lines on segment " + id + " failed: " + ex);
            }
        }

        private void BuildLine(ushort id, int index, NetLine line, NetInfo fence, DecalRenderer.Corners c, bool cornersOk, bool waterRight, Item item)
        {
            DecalSettings tmp = new DecalSettings();
            tmp.Lateral = line.Lateral;
            float lift = line.Lift * FenceStore.Unit;
            float hw = fence.m_halfWidth * Mathf.Clamp(line.Scale, NetLine.ScaleMin, NetLine.ScaleMax) / 100f;

            Vector3[] P = new Vector3[4];
            bool good = cornersOk;
            if (!good || !DecalRenderer.CornerControlPoints(c, tmp, lift, waterRight, P))
            {
                DecalRenderer.NodeControlPoints(id, tmp, lift, waterRight, P);
                good = false;
            }
            for (int i = 0; i < 4; i++)
            {
                if (!DecalRenderer.Finite(P[i])) return;
            }

            DecalRenderer.Path path = DecalRenderer.SamplePath(P);
            if (good) PropLineRenderer.ExtendToNodeCentres(id, c, tmp, lift, waterRight, path);

            float len = path.Length;
            float d0 = Mathf.Max(0f, -DecalRenderer.TrimStartValue(line.StartShift, line.EndShift, waterRight)) * FenceStore.Unit;
            float d1 = len - Mathf.Max(0f, -DecalRenderer.TrimEndValue(line.StartShift, line.EndShift, waterRight)) * FenceStore.Unit;
            if (d1 - d0 < 0.3f)
            {
                float mid = Mathf.Clamp((d0 + d1) * 0.5f, 0.15f, Mathf.Max(len - 0.15f, 0.15f));
                d0 = mid - 0.15f;
                d1 = mid + 0.15f;
            }

            bool rotated = line.Flip ^ waterRight; // false: the model faces the water side

            // The line is built the way the quay's own fences were built before: ONE curve from the start of the segment to its end
            // (the blend of the segment's real edge curves, taken from the game and the mods that change the corners). Only the gaps
            // at the nodes (a node moved away from the segment end, Node Controller Renewal) are separate pieces, one per gap.
            List<Vector3[]> pieces = CutPieces(path, d0, d1);
            float phase = 0f; // texture coordinate (v) at the start of the next piece: the texture runs on without a break
            for (int i = 0; i < pieces.Count; i++)
            {
                Part part = MakeRibbon(id, index, i, pieces[i], fence, hw, rotated, 1f, ref phase);
                if (part != null) item.Parts.Add(part);
            }

            if (line.CapStart && cornersOk)
            {
                Part cap = MakeCap(id, index, CapStartPart, c, true, fence, hw, lift);
                if (cap != null) item.Parts.Add(cap);
            }
            if (line.CapEnd && cornersOk)
            {
                Part cap = MakeCap(id, index, CapEndPart, c, false, fence, hw, lift);
                if (cap != null) item.Parts.Add(cap);
            }

            if (_logged < 6)
            {
                _logged++;
                Debug.Log("[QuayTools] Network line: segment " + id + ", line " + (index + 1) + ", " + fence.name + ", length " + (d1 - d0).ToString("0.0") + " m" +
                          (fence.m_requireHeightMap ? ", height map" : string.Empty) + (rotated ? ", turned" : string.Empty));
            }
        }

        /// <summary>The pieces [d0, d1] of the path: every cubic the path is made of (bridge at the start, edge curve, bridge at the end) is ONE piece.</summary>
        private static List<Vector3[]> CutPieces(DecalRenderer.Path path, float d0, float d1)
        {
            const int Table = 24;
            List<Vector3[]> result = new List<Vector3[]>();

            for (int ci = 0; ci < path.Cubics.Count; ci++)
            {
                float from = path.CubicFrom[ci], to = path.CubicTo[ci];
                float a = Mathf.Max(d0, from), b = Mathf.Min(d1, to);
                if (b - a < 0.05f || to - from < 0.01f) continue;

                Vector3[] C = path.Cubics[ci];
                if (a <= from + 0.001f && b >= to - 0.001f)
                {
                    result.Add(new Vector3[] { C[0], C[1], C[2], C[3] }); // untrimmed: the curve itself
                    continue;
                }

                // trimmed: the exact part of the cubic (arc length table of the cubic: parameter <-> fraction of its length)
                float[] len = new float[Table + 1];
                Vector3 prev = C[0];
                for (int i = 1; i <= Table; i++)
                {
                    float u = i / (float)Table, v = 1f - u;
                    Vector3 pt = v * v * v * C[0] + 3f * v * v * u * C[1] + 3f * v * u * u * C[2] + u * u * u * C[3];
                    len[i] = len[i - 1] + (pt - prev).magnitude;
                    prev = pt;
                }
                if (len[Table] < 0.01f) continue;

                float ta = ParamAt(len, (a - from) / (to - from)), tb = ParamAt(len, (b - from) / (to - from));
                if (tb - ta < 0.001f) continue;
                result.Add(FenceHeight.SubCubic(C, ta, tb));
            }
            return result;
        }

        /// <summary>Curve parameter at the given fraction of the arc length (table with equal parameter steps).</summary>
        private static float ParamAt(float[] len, float fraction)
        {
            int n = len.Length - 1;
            float target = Mathf.Clamp01(fraction) * len[n];
            int i = 1;
            while (i < n && len[i] < target) i++;
            float span = len[i] - len[i - 1];
            float f = span > 1e-6f ? (target - len[i - 1]) / span : 0f;
            return Mathf.Clamp01((i - 1 + f) / n);
        }

        /// <summary>
        /// A straight closing piece across the quay (the width of the visible quay model) at one end of the segment,
        /// facing away from the segment.
        /// </summary>
        private static Part MakeCap(ushort id, int index, int partIndex, DecalRenderer.Corners c, bool atStart, NetInfo fence, float hw, float lift)
        {
            Vector3 left = atStart ? c.sL : c.eL, right = atStart ? c.sR : c.eR;
            Vector3 dir = atStart ? c.dSL : c.dEL;

            QuayFrame f = QuayGeometry.GetFrame(id);
            Vector3 span = right - left;
            Vector3 flat = span;
            flat.y = 0f;
            float w = flat.magnitude;
            if (w < 0.5f) return null;

            float t0 = Mathf.Clamp(0.5f + f.YLo / w, 0f, 1f), t1 = Mathf.Clamp(0.5f + f.YHi / w, 0f, 1f);
            Vector3 pL = left + span * t0, pR = left + span * t1;
            pL.y += lift;
            pR.y += lift;

            dir.y = 0f;
            if (dir.sqrMagnitude < 1e-6f) return null;
            Vector3 axis = dir.normalized; // from the end into the segment

            Vector3 a0 = pL + axis * hw, a3 = pR + axis * hw;
            Vector3 b0 = pL - axis * hw, b3 = pR - axis * hw;
            Vector3[] A = { a0, Vector3.Lerp(a0, a3, 1f / 3f), Vector3.Lerp(a0, a3, 2f / 3f), a3 };
            Vector3[] B = { b0, Vector3.Lerp(b0, b3, 1f / 3f), Vector3.Lerp(b0, b3, 2f / 3f), b3 };
            Vector3[] Q = { pL, Vector3.Lerp(pL, pR, 1f / 3f), Vector3.Lerp(pL, pR, 2f / 3f), pR };

            float phase = 0f;
            return Assemble(id, index, partIndex, Q, A, B, fence, hw, true, 1.5f, ref phase);
        }

        private static Part MakeRibbon(ushort id, int index, int partIndex, Vector3[] Q, NetInfo fence, float hw, bool rotated, float vDivide, ref float phase)
        {
            // the ribbon edges are the curve moved sideways by half the model width at its four control points (the way the fence
            // ribbons of the quay were always built); the direction at a control point comes from the control polygon
            Vector3[] tangent = { Q[1] - Q[0], Q[2] - Q[0], Q[3] - Q[1], Q[3] - Q[2] };
            Vector3 chord = Q[3] - Q[0];
            Vector3[] A = new Vector3[4]; // left edge of the ribbon
            Vector3[] B = new Vector3[4]; // right edge of the ribbon
            for (int i = 0; i < 4; i++)
            {
                Vector3 t = tangent[i];
                t.y = 0f;
                if (t.sqrMagnitude < 1e-8f)
                {
                    t = chord;
                    t.y = 0f;
                }
                Vector3 n = Vector3.Cross(t, Vector3.up);
                n.y = 0f;
                n = n.sqrMagnitude > 1e-8f ? n.normalized : Vector3.zero;
                A[i] = Q[i] + n * hw;
                B[i] = Q[i] - n * hw;
            }
            return Assemble(id, index, partIndex, Q, A, B, fence, hw, rotated, vDivide, ref phase);
        }

        /// <summary>The render data of one piece, like NetSegment.RefreshRoadFence fills it, and the call of RenderSegments.</summary>
        private static Part Assemble(ushort id, int index, int partIndex, Vector3[] Q, Vector3[] A, Vector3[] B, NetInfo fence, float hw, bool rotated, float vDivide, ref float phase)
        {
            RenderManager.Instance data = new RenderManager.Instance();
            Vector3 position = (Q[0] + Q[3]) * 0.5f;
            data.m_position = position;
            data.m_rotation = Quaternion.identity;

            Color col = fence.m_color;
            col.a = 0f;
            data.m_dataColor0 = col;
            data.m_dataFloat0 = WindAt(position);
            data.m_dataVector0 = new Vector4(0.5f / fence.m_halfWidth, 1f / fence.m_segmentLength, 1f, 1f);
            data.m_dataVector3 = RenderManager.DefaultColorLocation;

            float vScale = fence.m_netAI.GetVScale() / vDivide;
            data.m_dataMatrix0 = NetSegment.CalculateControlMatrix(A[0], A[1], A[2], A[3], B[0], B[1], B[2], B[3], position, vScale);
            data.m_dataMatrix1 = NetSegment.CalculateControlMatrix(B[0], B[1], B[2], B[3], A[0], A[1], A[2], A[3], position, vScale);

            // The game rounds the texture length of a piece to quarter tiles (fine for a whole segment, but our pieces are
            // short: the texture would be stretched and restart at every joint). The texture coordinates of the four
            // control points (row 3 of the matrices) are set here: no rounding, and the coordinate carries on from the previous piece.
            float k0, k3;
            float[] kA = Knots(A, B, vScale), kB = Knots(B, A, vScale);
            k0 = kA[0];
            k3 = kA[3];
            float shift = phase - k0;
            data.m_dataMatrix0 = WithKnots(data.m_dataMatrix0, kA, shift);
            data.m_dataMatrix1 = WithKnots(data.m_dataMatrix1, kB, shift);
            phase = phase + (k3 - k0);

            if (fence.m_requireSurfaceMaps)
            {
                TerrainSurface(position, ref data);
            }
            else if (fence.m_requireHeightMap)
            {
                TerrainHeight(position, ref data);
                Bezier3 line = new Bezier3();
                line.a = Q[0];
                line.b = Q[1];
                line.c = Q[2];
                line.d = Q[3];
                FenceHeight.ApplyHeightMap(FenceHeight.Key(id, index, partIndex), line, hw + 6f, ref data);
            }

            NetSegment seg = NetManager.instance.m_segments.m_buffer[id];
            object box = seg;
            int bits = Convert.ToInt32(_flags2.GetValue(box));
            bits = rotated ? (bits | TurnAroundRight) : (bits & ~TurnAroundRight);
            Type ft = _flags2.FieldType;
            object value = ft.IsEnum ? Enum.ToObject(ft, bits) : Convert.ChangeType(bits, ft);
            _flags2.SetValue(box, value);
            seg = (NetSegment)box;

            List<NetInfo.Segment> visible = new List<NetInfo.Segment>();
            if (fence.m_segments != null)
            {
                for (int i = 0; i < fence.m_segments.Length; i++)
                {
                    NetInfo.Segment piece = fence.m_segments[i];
                    if (piece == null || piece.m_segmentMesh == null || piece.m_segmentMaterial == null) continue;
                    object[] args = { seg.m_flags, value, false };
                    bool show;
                    try
                    {
                        show = (bool)_checkFlags.Invoke(piece, args);
                    }
                    catch (Exception)
                    {
                        show = true;
                    }
                    if (show) visible.Add(piece);
                }
            }

            Part part = new Part();
            part.Segs = visible.ToArray();
            part.Data = data;
            part.Turn = rotated;
            part.Centre = position;
            part.Radius = (Q[3] - Q[0]).magnitude * 0.5f + hw + 30f;
            return part;
        }

        /// <summary>The texture coordinates of the four control points as NetSegment.CalculateControlMatrix computes them, without its rounding.</summary>
        private static float[] Knots(Vector3[] a, Vector3[] b, float vScale)
        {
            Vector3 d1 = a[1] - a[0];
            float k0 = Vector3.Dot(a[0] - b[0], d1) / Mathf.Max(0.001f, d1.magnitude) * vScale * 0.5f;
            float l1 = Vector3.Distance(a[0] + b[0], a[1] + b[1]) * vScale * 0.5f;
            float l2 = Vector3.Distance(a[1] + b[1], a[2] + b[2]) * vScale * 0.5f;
            float l3 = Vector3.Distance(a[2] + b[2], a[3] + b[3]) * vScale * 0.5f;
            return new float[] { k0, k0 + l1, k0 + l1 + l2, k0 + l1 + l2 + l3 };
        }

        private static Matrix4x4 WithKnots(Matrix4x4 m, float[] k, float shift)
        {
            m.m30 = k[0] + shift;
            m.m31 = k[1] + shift;
            m.m32 = k[2] + shift;
            m.m33 = k[3] + shift;
            return m;
        }

        private static float WindAt(Vector3 position)
        {
            try
            {
                if (_windSpeed == null) return 0f;
                object r = _windSpeed.Invoke(Singleton<WeatherManager>.instance, new object[] { position });
                return r is float ? (float)r : 0f;
            }
            catch (Exception)
            {
                return 0f;
            }
        }

        private static void TerrainHeight(Vector3 position, ref RenderManager.Instance data)
        {
            try
            {
                if (_heightMapping == null) return;
                object[] a = { position, null, new Vector4(), new Vector4() };
                _heightMapping.Invoke(Singleton<TerrainManager>.instance, a);
                data.m_dataTexture0 = a[1] as Texture;
                data.m_dataVector1 = (Vector4)a[2];
                data.m_dataVector2 = (Vector4)a[3];
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[QuayTools] Terrain height mapping failed: " + ex.Message);
            }
        }

        private static void TerrainSurface(Vector3 position, ref RenderManager.Instance data)
        {
            try
            {
                if (_surfaceMapping == null) return;
                object[] a = { position, null, null, new Vector4() };
                _surfaceMapping.Invoke(Singleton<TerrainManager>.instance, a);
                data.m_dataTexture0 = a[1] as Texture;
                data.m_dataTexture1 = a[2] as Texture;
                data.m_dataVector1 = (Vector4)a[3];
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[QuayTools] Terrain surface mapping failed: " + ex.Message);
            }
        }

        // ---------- drawing ----------

        /// <summary>Called by the game's network rendering (patch on NetManager.EndRenderingImpl), before it flushes its LOD batches.</summary>
        internal void Render(RenderManager.CameraInfo cameraInfo)
        {
            if (cameraInfo == null || _ids.Count == 0) return;
            FindMethods(); // the first frame can come before the first build
            if (_failed) return;

            NetManager nm = NetManager.instance;
            MaterialPropertyBlock block = _materialBlock.GetValue(nm) as MaterialPropertyBlock;
            if (block == null) return;
            int idLeft = (int)_idLeft.GetValue(nm), idRight = (int)_idRight.GetValue(nm), idScale = (int)_idScale.GetValue(nm);
            int idObject = (int)_idObject.GetValue(nm), idColor = (int)_idColor.GetValue(nm), idObjectColor = (int)_idObjectColor.GetValue(nm);
            int idSurfA = (int)_idSurfA.GetValue(nm), idSurfB = (int)_idSurfB.GetValue(nm), idSurfMap = (int)_idSurfMap.GetValue(nm);
            int idHeight = (int)_idHeight.GetValue(nm), idHeightMap = (int)_idHeightMap.GetValue(nm);

            Vector3 cam = cameraInfo.m_position;
            for (int i = 0; i < _ids.Count; i++)
            {
                Item item;
                if (!_items.TryGetValue(_ids[i], out item)) continue;
                for (int p = 0; p < item.Parts.Count; p++)
                {
                    Part part = item.Parts[p];
                    if (part == null || part.Segs == null) continue;
                    float reach = MaxDistance + part.Radius;
                    if ((part.Centre - cam).sqrMagnitude > reach * reach) continue;

                    RenderManager.Instance data = part.Data;
                    for (int k = 0; k < part.Segs.Length; k++)
                    {
                        NetInfo.Segment s = part.Segs[k];
                        if (s == null || s.m_segmentMesh == null || s.m_segmentMaterial == null) continue;

                        Vector4 objectIndex = data.m_dataVector3;
                        if (s.m_requireWindSpeed) objectIndex.w = data.m_dataFloat0;
                        Vector4 meshScale = data.m_dataVector0;
                        if (part.Turn)
                        {
                            meshScale.x = -meshScale.x;
                            meshScale.y = -meshScale.y;
                        }

                        block.Clear();
                        block.SetMatrix(idLeft, data.m_dataMatrix0);
                        block.SetMatrix(idRight, data.m_dataMatrix1);
                        block.SetVector(idScale, meshScale);
                        block.SetVector(idObject, objectIndex);
                        block.SetColor(idColor, data.m_dataColor0);
                        block.SetColor(idObjectColor, data.m_dataColor1);
                        if (s.m_requireSurfaceMaps)
                        {
                            if (data.m_dataTexture0 != null && data.m_dataTexture1 != null)
                            {
                                block.SetTexture(idSurfA, data.m_dataTexture0);
                                block.SetTexture(idSurfB, data.m_dataTexture1);
                                block.SetVector(idSurfMap, data.m_dataVector1);
                            }
                        }
                        else if (s.m_requireHeightMap && data.m_dataTexture0 != null)
                        {
                            block.SetTexture(idHeight, data.m_dataTexture0);
                            block.SetVector(idHeightMap, data.m_dataVector1);
                            block.SetVector(idSurfMap, data.m_dataVector2);
                        }

                        Graphics.DrawMesh(s.m_segmentMesh, data.m_position, data.m_rotation, s.m_segmentMaterial, s.m_layer, null, 0, block);
                    }
                }
            }
        }
    }
}

namespace QuayTools.Patches
{
    /// <summary>
    /// The network-model lines are drawn through the game's own routine for network segments, which batches distant
    /// pieces; they are submitted just before the game flushes those batches (start of NetManager.EndRenderingImpl).
    /// </summary>
    [HarmonyLib.HarmonyPatch]
    internal static class NetLineRenderPatch
    {
        private static int _errors;

        public static System.Reflection.MethodBase TargetMethod()
        {
            System.Reflection.MethodBase m = HarmonyLib.AccessTools.Method(typeof(NetManager), "EndRenderingImpl");
            if (m == null) UnityEngine.Debug.LogWarning("[QuayTools] NetManager.EndRenderingImpl not found: network-model lines are unavailable");
            return m;
        }

        public static void Prefix(RenderManager.CameraInfo __0)
        {
            if (_errors >= 5) return;
            try
            {
                NetLineRenderer r = NetLineRenderer.Instance;
                if (r != null) r.Render(__0);
            }
            catch (System.Exception ex)
            {
                _errors++;
                UnityEngine.Debug.LogError("[QuayTools] Network line rendering error " + _errors + (_errors >= 5 ? " (disabled)" : string.Empty) + ": " + ex);
            }
        }
    }
}
