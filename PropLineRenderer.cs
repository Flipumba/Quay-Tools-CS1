using System.Collections.Generic;
using ColossalFramework;
using UnityEngine;

namespace QuayTools
{
    /// <summary>
    /// Draws the prop lines of quay segments: every line is a prop placed at a fixed step along the surface of the quay.
    /// The line follows the same curve as a decal path (built from the real node corners, so curves, heights and node
    /// edits are followed). The props are drawn like the game draws a prop instance (Graphics.DrawMesh with the prop's
    /// own mesh and material); they are decoration only. Main thread only. Lives on the QuayToolsController game object.
    /// </summary>
    public class PropLineRenderer : MonoBehaviour
    {
        private const int MaxPerEntry = 3000;
        private const int MaxBuildsPerFrame = 8;
        private const int ChecksPerFrame = 6;
        private const float DefaultViewDistance = 1000f;

        private class Built
        {
            public PropInfo Info;
            public TreeInfo Tree;
            public Matrix4x4[] Matrices; // props
            public Vector3[] Positions;
            public float[] Scales;       // trees
            public Quaternion[] Rotations;
            public bool Lit;             // the prop has effects (lights) or illumination: drawn through the game's own prop rendering
            public float ViewDistance;
        }

        private class Item
        {
            public PropLine Settings;
            public DecalRenderer.Corners Last;
            public bool Dirty = true;
            public bool WaterRight;
            public readonly List<Built> Built = new List<Built>();
        }

        private readonly Dictionary<ushort, Item> _items = new Dictionary<ushort, Item>();
        private readonly List<ushort> _ids = new List<ushort>();
        private readonly List<ushort> _dead = new List<ushort>();
        private readonly HashSet<string> _reported = new HashSet<string>();
        private readonly MaterialPropertyBlock _block = new MaterialPropertyBlock();
        private int _version = -1;
        private int _cursor;
        private int _logged;

        /// <summary>The renderer of the current map (used by the tree rendering patch).</summary>
        internal static PropLineRenderer Instance;

        private void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            if (_version != PropLineStore.Version) Sync();
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
                if (item.Dirty) continue;

                DecalRenderer.Corners c;
                if (DecalRenderer.ReadCorners(id, out c) && !c.Same(item.Last)) item.Dirty = true;
                if (QuayGeometry.GetFrame(id).WaterIsRight != item.WaterRight) item.Dirty = true;
            }
            for (int i = 0; i < _dead.Count; i++) PropLineStore.Remove(_dead[i]);

            Draw();
        }

        private void Sync()
        {
            _version = PropLineStore.Version;
            List<KeyValuePair<ushort, PropLine>> entries = PropLineStore.Snapshot();

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
                    item.Settings = entries[i].Value;
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

        // ---------- building ----------

        private void Build(ushort id, Item item)
        {
            item.Dirty = false;
            item.Built.Clear();

            try
            {
                NetSegment[] segs = NetManager.instance.m_segments.m_buffer;
                if ((segs[id].m_flags & NetSegment.Flags.Created) == NetSegment.Flags.None) return;

                DecalRenderer.Corners c;
                bool ok = DecalRenderer.ReadCorners(id, out c);
                if (ok) item.Last = c;

                bool waterRight = QuayGeometry.GetFrame(id).WaterIsRight;
                item.WaterRight = waterRight;

                for (int k = 0; k < item.Settings.Entries.Count; k++)
                {
                    PropEntry e = item.Settings.Entries[k];
                    if (!e.Enabled || string.IsNullOrEmpty(e.Prop)) continue;

                    PropInfo info;
                    TreeInfo tree;
                    if (!PropCatalog.Resolve(e.Prop, out info, out tree))
                    {
                        if (_reported.Add(e.Prop)) Debug.LogWarning("[QuayTools] Prop line: '" + e.Prop + "' is not available (asset missing or not usable)");
                        continue;
                    }

                    DecalSettings tmp = new DecalSettings();
                    tmp.Lateral = e.Lateral;
                    float lift = e.Lift * FenceStore.Unit;

                    Vector3[] P = new Vector3[4];
                    bool good = ok;
                    if (!good || !DecalRenderer.CornerControlPoints(c, tmp, lift, waterRight, P))
                    {
                        DecalRenderer.NodeControlPoints(id, tmp, lift, waterRight, P);
                        good = false;
                    }

                    bool finite = true;
                    for (int i = 0; i < 4; i++) finite &= DecalRenderer.Finite(P[i]);
                    if (!finite) continue;

                    DecalRenderer.Path path = DecalRenderer.SamplePath(P);
                    if (good) ExtendToNodeCentres(id, c, tmp, lift, waterRight, path);

                    Built built = Place(id, k, e, info, tree, path, waterRight);
                    if (built != null)
                    {
                        item.Built.Add(built);
                        if (_logged < 6)
                        {
                            _logged++;
                            Debug.Log("[QuayTools] Prop line: segment " + id + ", " + e.Prop + ", " + built.Positions.Length + " items, length " + path.Length.ToString("0.0") + " m");
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning("[QuayTools] Prop line on segment " + id + " failed: " + ex);
            }
        }

        /// <summary>True when exactly two segments meet at the node (only then the gap between their ends can be split between them).</summary>
        internal static bool JoinsTwo(ushort nodeId)
        {
            if (nodeId == 0) return false;
            NetNode node = NetManager.instance.m_nodes.m_buffer[nodeId];
            int count = 0;
            for (int i = 0; i < 8; i++)
            {
                if (node.GetSegment(i) != 0) count++;
            }
            return count == 2;
        }

        /// <summary>
        /// The line of a segment runs from the middle of its start node to the middle of its end node: the path is
        /// continued over half of the gap between the segment end and its neighbour (the neighbour covers the other half),
        /// following the bend and the height curve of the node.
        /// </summary>
        internal static void ExtendToNodeCentres(ushort id, DecalRenderer.Corners c, DecalSettings tmp, float lift, bool waterRight, DecalRenderer.Path path)
        {
            NetSegment seg = NetManager.instance.m_segments.m_buffer[id];
            Vector3[] Qs = null, Qe = null;
            Vector3[] own = path.Cubics.Count > 0 ? path.Cubics[0] : null;
            if (own == null) return;

            if (JoinsTwo(seg.m_startNode))
            {
                Vector3[] q = new Vector3[4];
                if (DecalRenderer.BridgeControlPoints(id, seg.m_startNode, true, c, tmp, lift, waterRight, own, q) &&
                    new Vector2(q[0].x - q[3].x, q[0].z - q[3].z).magnitude > 0.05f)
                {
                    Qs = FenceHeight.SubCubic(q, 0.5f, 1f); // second half: from the node middle to the segment start
                }
            }
            if (JoinsTwo(seg.m_endNode))
            {
                Vector3[] q = new Vector3[4];
                if (DecalRenderer.BridgeControlPoints(id, seg.m_endNode, false, c, tmp, lift, waterRight, own, q) &&
                    new Vector2(q[0].x - q[3].x, q[0].z - q[3].z).magnitude > 0.05f)
                {
                    Qe = FenceHeight.SubCubic(q, 0f, 0.5f); // first half: from the segment end to the node middle
                }
            }
            DecalRenderer.ExtendCurve(path, Qs, Qe);
        }

        /// <summary>
        /// Positions are the grid d = k * step counted from the start of the line (the middle of the start node), so
        /// trimming one end never moves the props at the other end. The trim only removes props.
        /// </summary>
        private static Built Place(ushort id, int index, PropEntry e, PropInfo info, TreeInfo tree, DecalRenderer.Path path, bool waterRight)
        {
            float len = path.Length;

            // The line is walked in the canonical direction (the water on its right), whatever way the segment was built or
            // flipped: the grid starts at the canonical start, so the props of neighbouring segments continue each other, face
            // the same way and the trim / slide act on the same ends everywhere.
            bool flip = !waterRight;
            float trimS = Mathf.Max(0f, -e.StartShift) * FenceStore.Unit;
            float trimE = Mathf.Max(0f, -e.EndShift) * FenceStore.Unit;
            float lo = trimS - 0.001f, hi = len - trimE + 0.001f;
            if (hi < lo) return null;

            float step = Mathf.Max(e.Step * FenceStore.Unit, 0.5f);
            int last = Mathf.FloorToInt(len / step + 0.0001f);
            if (last + 1 > MaxPerEntry)
            {
                last = MaxPerEntry - 1;
                step = len / last;
            }

            float shiftMod = Mathf.Repeat(e.ShiftX * FenceStore.Unit, step); // the grid slides along the line, the ends stay

            // a prop exactly at the end of the line is the first prop of the next segment (the node middle): not placed twice
            NetSegment seg = NetManager.instance.m_segments.m_buffer[id];
            bool dropEnd = JoinsTwo(flip ? seg.m_startNode : seg.m_endNode);

            List<Vector3> positions = new List<Vector3>();
            List<Matrix4x4> matrices = new List<Matrix4x4>();
            List<float> scales = new List<float>();
            List<Quaternion> rotations = new List<Quaternion>();

            System.Random rnd = new System.Random(unchecked(id * 7919 + index * 104729 + 17));
            path.ResetCursor();
            for (int i = 0; i <= last; i++)
            {
                double r1 = rnd.NextDouble(), r2 = rnd.NextDouble(); // drawn for every grid position, kept or not
                float dc = i * step + shiftMod;   // distance along the canonical direction
                if (dc < lo || dc > hi) continue;
                if (dropEnd && dc > len - 0.02f) continue;
                float d = flip ? len - dc : dc;   // the same place measured along the path of the segment

                Vector3 pos, left;
                path.Eval(Mathf.Clamp(d, 0f, len), out pos, out left);

                Vector3 fwd = Vector3.Cross(Vector3.up, left);
                if (fwd.sqrMagnitude < 1e-6f) fwd = Vector3.forward;
                if (flip) fwd = -fwd;             // canonical forward

                float yaw = e.Angle + (e.RandomRotation ? (float)(r1 * 360.0) : 0f);
                float scale = e.Scale / 100f * (1f + (float)(r2 * 2.0 - 1.0) * e.ScaleRandom / 100f);
                scale = Mathf.Max(scale, 0.05f);

                positions.Add(pos);
                scales.Add(scale);
                if (info != null)
                {
                    Quaternion basis = Quaternion.LookRotation(fwd, Vector3.up);
                    if (e.Tilt)
                    {
                        // follow the height curve: the prop looks along the 3D tangent of the line
                        Vector3 pa, pb, la, lb;
                        path.Eval(Mathf.Clamp(d - 0.5f, 0f, len), out pa, out la);
                        path.Eval(Mathf.Clamp(d + 0.5f, 0f, len), out pb, out lb);
                        Vector3 tan = pb - pa;
                        if (flip) tan = -tan;
                        if (tan.sqrMagnitude > 1e-6f && (fwd.x * tan.x + fwd.z * tan.z) > 0f) basis = Quaternion.LookRotation(tan.normalized, Vector3.up);
                    }
                    Quaternion rot = basis * Quaternion.Euler(0f, yaw, 0f);
                    rotations.Add(rot);
                    matrices.Add(Matrix4x4.TRS(pos, rot, new Vector3(scale, scale, scale)));
                }
            }
            if (positions.Count == 0) return null;

            Built built = new Built();
            built.Info = info;
            built.Tree = tree;
            built.Positions = positions.ToArray();
            built.Scales = scales.ToArray();
            built.Matrices = matrices.ToArray();
            built.Rotations = rotations.ToArray();
            built.Lit = info != null && (info.m_hasEffects || info.m_alwaysActive || info.m_illuminationBlinkType != 0 || info.m_illuminationOffRange.x < 1000f);
            built.ViewDistance = info != null && info.m_maxRenderDistance > 1f ? info.m_maxRenderDistance : DefaultViewDistance;
            return built;
        }

        // ---------- props with lights: the game's own prop rendering ----------

        // PropInstance.RenderInstance(cameraInfo, info, id, position, rotation, scale, angle, color, objectIndex, active, billboard):
        // draws the prop (with its LOD and day/night illumination) and renders its effects, i.e. the lights of lamps.
        private static System.Reflection.MethodInfo _gameRender;
        private static bool _gameRenderSearched;
        private static bool _gameRenderFailed;
        private static readonly object[] GameArgs = new object[11];
        private static object _gameId;

        private static bool GameRenderAvailable
        {
            get
            {
                if (_gameRenderFailed) return false;
                if (!_gameRenderSearched)
                {
                    _gameRenderSearched = true;
                    try
                    {
                        System.Reflection.MethodInfo[] all = typeof(PropInstance).GetMethods(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                        for (int i = 0; i < all.Length && _gameRender == null; i++)
                        {
                            if (all[i].Name != "RenderInstance") continue;
                            System.Reflection.ParameterInfo[] ps = all[i].GetParameters();
                            if (ps.Length == 11 && ps[0].ParameterType == typeof(RenderManager.CameraInfo) && ps[1].ParameterType == typeof(PropInfo) &&
                                ps[3].ParameterType == typeof(Vector3) && ps[4].ParameterType == typeof(Quaternion) && ps[5].ParameterType == typeof(float) &&
                                ps[7].ParameterType == typeof(Color) && ps[8].ParameterType == typeof(Vector4) && ps[9].ParameterType == typeof(bool))
                                _gameRender = all[i];
                        }
                        _gameId = new InstanceID();
                        Debug.Log("[QuayTools] Prop lines: lights of props " + (_gameRender != null ? "use PropInstance.RenderInstance" : "are unavailable (PropInstance.RenderInstance not found)"));
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogWarning("[QuayTools] Prop lines: lights unavailable: " + ex.Message);
                    }
                }
                return _gameRender != null;
            }
        }

        private void RenderLit(RenderManager.CameraInfo cameraInfo)
        {
            if (!GameRenderAvailable) return;
            Vector3 camPos = cameraInfo.m_position;
            for (int i = 0; i < _ids.Count; i++)
            {
                Item item;
                if (!_items.TryGetValue(_ids[i], out item)) continue;
                for (int b = 0; b < item.Built.Count; b++)
                {
                    Built built = item.Built[b];
                    if (!built.Lit || built.Tree != null || built.Info == null) continue;
                    PropInfo info = built.Info;
                    float maxSqr = built.ViewDistance * built.ViewDistance;
                    for (int k = 0; k < built.Matrices.Length; k++)
                    {
                        if ((built.Positions[k] - camPos).sqrMagnitude > maxSqr) continue;
                        try
                        {
                            GameArgs[0] = cameraInfo;
                            GameArgs[1] = info;
                            GameArgs[2] = _gameId;
                            GameArgs[3] = built.Positions[k];
                            GameArgs[4] = built.Rotations[k];
                            GameArgs[5] = built.Scales[k];
                            // the angle (radians, the game's sign: a turn about the down axis) is what the LOD batch uses; the quaternion only turns the full mesh
                            GameArgs[6] = -built.Rotations[k].eulerAngles.y * Mathf.Deg2Rad; // the game turns a prop by AngleAxis(angle, Vector3.down)
                            GameArgs[7] = info.m_color0;
                            GameArgs[8] = RenderManager.DefaultColorLocation;
                            GameArgs[9] = true;
                            GameArgs[10] = Vector4.zero;
                            _gameRender.Invoke(null, GameArgs);
                        }
                        catch (System.Exception ex)
                        {
                            _gameRenderFailed = true; // the plain drawing takes over
                            Debug.LogWarning("[QuayTools] Prop lines: lights failed, drawing without them: " + ex.Message);
                            return;
                        }
                    }
                }
            }
        }

        // ---------- trees ----------

        /// <summary>Called by the game's tree rendering (patch on TreeManager.EndRenderingImpl).</summary>
        internal void RenderTrees(RenderManager.CameraInfo cameraInfo)
        {
            if (cameraInfo == null) return;
            RenderLit(cameraInfo);
            float maxSqr = DefaultViewDistance * 2f;
            maxSqr *= maxSqr;

            for (int i = 0; i < _ids.Count; i++)
            {
                Item item;
                if (!_items.TryGetValue(_ids[i], out item)) continue;
                for (int b = 0; b < item.Built.Count; b++)
                {
                    Built built = item.Built[b];
                    if (built.Tree == null) continue;
                    for (int k = 0; k < built.Positions.Length; k++)
                    {
                        if ((built.Positions[k] - cameraInfo.m_position).sqrMagnitude > maxSqr) continue;
                        TreeInstance.RenderInstance(cameraInfo, built.Tree, built.Positions[k], built.Scales[k], 1f, RenderManager.DefaultColorLocation, false);
                    }
                }
            }
        }

        // ---------- drawing ----------

        private void Draw()
        {
            PropManager pm = Singleton<PropManager>.instance;
            Camera cam = Camera.main;
            bool cull = cam != null;
            Vector3 camPos = cull ? cam.transform.position : Vector3.zero;

            for (int i = 0; i < _ids.Count; i++)
            {
                Item item = _items[_ids[i]];
                for (int b = 0; b < item.Built.Count; b++)
                {
                    Built built = item.Built[b];
                    PropInfo info = built.Info;
                    if (built.Tree != null) continue;
                    if (info == null || info.m_mesh == null || info.m_material == null) continue;
                    if (built.Lit && GameRenderAvailable) continue; // drawn by the game's own rendering with its lights

                    _block.Clear();
                    _block.SetColor(pm.ID_Color, info.m_color0);
                    _block.SetVector(pm.ID_ObjectIndex, RenderManager.DefaultColorLocation);
                    if (info.m_rollLocation != null)
                    {
                        info.m_material.SetVectorArray(pm.ID_RollLocation, info.m_rollLocation);
                        info.m_material.SetVectorArray(pm.ID_RollParams, info.m_rollParams);
                    }

                    float maxSqr = built.ViewDistance * built.ViewDistance;
                    for (int k = 0; k < built.Matrices.Length; k++)
                    {
                        if (cull && (built.Positions[k] - camPos).sqrMagnitude > maxSqr) continue;
                        Graphics.DrawMesh(info.m_mesh, built.Matrices[k], info.m_material, info.m_prefabDataLayer, null, 0, _block);
                    }
                }
            }
        }
    }
}

namespace QuayTools.Patches
{
    /// <summary>
    /// Trees of the prop lines are drawn through the game's own tree renderer (it batches them with its LOD meshes), so
    /// they are submitted just before the game finishes the tree rendering.
    /// </summary>
    [HarmonyLib.HarmonyPatch]
    internal static class TreeRenderPatch
    {
        public static System.Reflection.MethodBase TargetMethod()
        {
            System.Reflection.MethodBase m = HarmonyLib.AccessTools.Method(typeof(TreeManager), "EndRenderingImpl");
            if (m == null) UnityEngine.Debug.LogWarning("[QuayTools] TreeManager.EndRenderingImpl not found: trees in prop lines are unavailable");
            return m;
        }

        public static void Prefix(RenderManager.CameraInfo __0)
        {
            try
            {
                PropLineRenderer r = PropLineRenderer.Instance;
                if (r != null) r.RenderTrees(__0);
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogWarning("[QuayTools] Tree rendering failed: " + ex.Message);
            }
        }
    }
}
