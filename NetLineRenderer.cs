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
            float d0 = Mathf.Max(0f, -line.StartShift) * FenceStore.Unit;
            float d1 = len - Mathf.Max(0f, -line.EndShift) * FenceStore.Unit;
            if (d1 - d0 < 0.3f)
            {
                float mid = Mathf.Clamp((d0 + d1) * 0.5f, 0.15f, Mathf.Max(len - 0.15f, 0.15f));
                d0 = mid - 0.15f;
                d1 = mid + 0.15f;
            }

            Vector3[] Q = MakeCurve(path, d0, d1);
            bool rotated = line.Flip ^ waterRight; // false: the model faces the water side
            Part part = MakeRibbon(id, index, 0, Q, fence, hw, rotated, 1f, true);
            if (part != null) item.Parts.Add(part);

            if (line.CapStart && cornersOk)
            {
                Part cap = MakeCap(id, index, 1, c, true, fence, hw, lift);
                if (cap != null) item.Parts.Add(cap);
            }
            if (line.CapEnd && cornersOk)
            {
                Part cap = MakeCap(id, index, 2, c, false, fence, hw, lift);
                if (cap != null) item.Parts.Add(cap);
            }

            if (_logged < 6)
            {
                _logged++;
                Debug.Log("[QuayTools] Network line: segment " + id + ", line " + (index + 1) + ", " + fence.name + ", length " + (d1 - d0).ToString("0.0") + " m" +
                          (fence.m_requireHeightMap ? ", height map" : string.Empty) + (rotated ? ", turned" : string.Empty));
            }
        }

        /// <summary>One cubic through the part [d0, d1] of the sampled path, with the end directions of the path.</summary>
        private static Vector3[] MakeCurve(DecalRenderer.Path path, float d0, float d1)
        {
            float e = Mathf.Min(0.5f, (d1 - d0) * 0.25f);
            Vector3 p0, p0b, p3a, p3, l;
            path.ResetCursor();
            path.Eval(d0, out p0, out l);
            path.Eval(d0 + e, out p0b, out l);
            path.Eval(d1 - e, out p3a, out l);
            path.Eval(d1, out p3, out l);

            Vector3 dirA = p0b - p0, dirB = p3 - p3a;
            dirA = dirA.sqrMagnitude > 1e-8f ? dirA.normalized : Vector3.forward;
            dirB = dirB.sqrMagnitude > 1e-8f ? dirB.normalized : dirA;

            Vector3 m1, m2;
            NetSegment.CalculateMiddlePoints(p0, dirA, p3, -dirB, false, false, out m1, out m2);
            return new Vector3[] { p0, m1, m2, p3 };
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

            return Assemble(id, index, partIndex, Q, A, B, fence, hw, true, 1.5f);
        }

        private static Part MakeRibbon(ushort id, int index, int partIndex, Vector3[] Q, NetInfo fence, float hw, bool rotated, float vDivide, bool unused)
        {
            Vector3[] tangent = { Q[1] - Q[0], Q[2] - Q[0], Q[3] - Q[1], Q[3] - Q[2] };
            Vector3[] A = new Vector3[4]; // left edge of the ribbon
            Vector3[] B = new Vector3[4]; // right edge of the ribbon
            for (int i = 0; i < 4; i++)
            {
                Vector3 n = Vector3.Cross(tangent[i], Vector3.up);
                n.y = 0f;
                n = n.sqrMagnitude > 1e-8f ? n.normalized : Vector3.zero;
                A[i] = Q[i] + n * hw;
                B[i] = Q[i] - n * hw;
            }
            return Assemble(id, index, partIndex, Q, A, B, fence, hw, rotated, vDivide);
        }

        /// <summary>The render data of one piece, like NetSegment.RefreshRoadFence fills it, and the call of RenderSegments.</summary>
        private static Part Assemble(ushort id, int index, int partIndex, Vector3[] Q, Vector3[] A, Vector3[] B, NetInfo fence, float hw, bool rotated, float vDivide)
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
