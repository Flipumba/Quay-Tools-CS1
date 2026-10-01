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
            public Matrix4x4[] Matrices;
            public Vector3[] Positions;
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

                    PropInfo info = PropCatalog.Find(e.Prop);
                    if (info == null || !PropCatalog.IsUsable(info))
                    {
                        if (_reported.Add(e.Prop)) Debug.LogWarning("[QuayTools] Prop line: prop '" + e.Prop + "' is not available (asset missing or not usable)");
                        continue;
                    }

                    DecalSettings tmp = new DecalSettings();
                    tmp.Lateral = e.Lateral;
                    float lift = e.Lift * FenceStore.Unit;

                    Vector3[] P = new Vector3[4];
                    if (!ok || !DecalRenderer.CornerControlPoints(c, tmp, lift, waterRight, P))
                    {
                        DecalRenderer.NodeControlPoints(id, tmp, lift, waterRight, P);
                    }

                    bool finite = true;
                    for (int i = 0; i < 4; i++) finite &= DecalRenderer.Finite(P[i]);
                    if (!finite) continue;

                    DecalRenderer.Path path = DecalRenderer.SamplePath(P);
                    Built built = Place(id, k, e, info, path);
                    if (built != null)
                    {
                        item.Built.Add(built);
                        if (_logged < 6)
                        {
                            _logged++;
                            Debug.Log("[QuayTools] Prop line: segment " + id + ", " + info.name + ", " + built.Matrices.Length + " props, length " + path.Length.ToString("0.0") + " m");
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning("[QuayTools] Prop line on segment " + id + " failed: " + ex);
            }
        }

        private static Built Place(ushort id, int index, PropEntry e, PropInfo info, DecalRenderer.Path path)
        {
            float len = path.Length;
            float from = -e.StartShift * FenceStore.Unit;   // positive start shift: the line starts before the segment start
            float to = len + e.EndShift * FenceStore.Unit;  // positive end shift: the line ends after the segment end
            if (to < from) return null;

            float step = Mathf.Max(e.Step * FenceStore.Unit, 0.5f);
            int count = Mathf.FloorToInt((to - from) / step + 0.0001f) + 1;
            if (count > MaxPerEntry)
            {
                count = MaxPerEntry;
                step = (to - from) / (count - 1);
            }
            if (count < 1) return null;

            Built built = new Built();
            built.Info = info;
            built.Matrices = new Matrix4x4[count];
            built.Positions = new Vector3[count];
            built.ViewDistance = info.m_maxRenderDistance > 1f ? info.m_maxRenderDistance : DefaultViewDistance;

            System.Random rnd = new System.Random(unchecked(id * 7919 + index * 104729 + 17));
            path.ResetCursor();
            for (int i = 0; i < count; i++)
            {
                float d = from + i * step;
                Vector3 pos, left;
                path.Eval(Mathf.Clamp(d, 0f, len), out pos, out left);

                Vector3 fwd = Vector3.Cross(Vector3.up, left);
                if (fwd.sqrMagnitude < 1e-6f) fwd = Vector3.forward;
                if (d < 0f) pos += fwd * d;                 // before the start: continue straight
                else if (d > len) pos += fwd * (d - len);   // after the end: continue straight

                double r1 = rnd.NextDouble(), r2 = rnd.NextDouble();
                float yaw = e.Angle + (e.RandomRotation ? (float)(r1 * 360.0) : 0f);
                float scale = e.Scale / 100f * (1f + (float)(r2 * 2.0 - 1.0) * e.ScaleRandom / 100f);
                scale = Mathf.Max(scale, 0.05f);

                Quaternion rot = Quaternion.LookRotation(fwd, Vector3.up) * Quaternion.Euler(0f, yaw, 0f);
                built.Matrices[i] = Matrix4x4.TRS(pos, rot, new Vector3(scale, scale, scale));
                built.Positions[i] = pos;
            }
            return built;
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
                    if (info == null || info.m_mesh == null || info.m_material == null) continue;

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
