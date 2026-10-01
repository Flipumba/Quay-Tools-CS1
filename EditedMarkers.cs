using System.Collections.Generic;
using UnityEngine;

namespace QuayTools
{
    /// <summary>
    /// While the Quay Tools tool is active: collects the segments that carry edits of the mod (for the highlight drawn by
    /// the tool) and draws a row of small tool icons above each of them. Main thread only.
    /// </summary>
    public class EditedMarkers : MonoBehaviour
    {
        private const float RefreshSeconds = 0.5f;
        private const float MaxDistance = 800f;
        private const int MaxHighlighted = 400;

        // bit -> icon file
        private static readonly int[] Bits = { 1, 2, 4, 8, 16 };
        private static readonly string[] Files = { "Network.png", "Decal.png", "Props.png", "Lock.png", "NoPedestrian.png" };
        private static readonly string[] TipKeys = { "mode_network", "mode_decal", "mode_props", "mode_lock", "mode_nopeds" };

        private static readonly List<ushort> EditedList = new List<ushort>();

        /// <summary>Segments with edits (empty while the tool is not active). Read by the tool's overlay.</summary>
        internal static List<ushort> Edited
        {
            get { return EditedList; }
        }

        private readonly Dictionary<ushort, int> _marks = new Dictionary<ushort, int>();
        private readonly Texture2D[] _icons = new Texture2D[5];
        private bool _iconsLoaded;
        private float _next;
        private GUIStyle _box;

        private static bool ToolActive()
        {
            QuayTool tool = QuayTool.Instance;
            return tool != null && tool.enabled && Settings.MarkEdited;
        }

        private void Add(ushort id, int bit)
        {
            int v;
            _marks.TryGetValue(id, out v);
            _marks[id] = v | bit;
        }

        private void Update()
        {
            if (!ToolActive())
            {
                if (EditedList.Count > 0) EditedList.Clear();
                _marks.Clear();
                _next = 0f;
                return;
            }

            if (Time.realtimeSinceStartup < _next) return;
            _next = Time.realtimeSinceStartup + RefreshSeconds;

            _marks.Clear();
            List<ushort> keys = NetLineStore.Keys();
            for (int i = 0; i < keys.Count; i++) Add(keys[i], 1);
            keys = DecalStore.Keys();
            for (int i = 0; i < keys.Count; i++) Add(keys[i], 2);
            keys = PropLineStore.Keys();
            for (int i = 0; i < keys.Count; i++) Add(keys[i], 4);
            keys = LockStore.Snapshot();
            for (int i = 0; i < keys.Count; i++) Add(keys[i], 8);
            keys = PedStore.Snapshot();
            for (int i = 0; i < keys.Count; i++) Add(keys[i], 16);

            NetSegment[] segs = NetManager.instance.m_segments.m_buffer;
            EditedList.Clear();
            List<ushort> dead = new List<ushort>();
            foreach (KeyValuePair<ushort, int> kv in _marks)
            {
                if ((segs[kv.Key].m_flags & NetSegment.Flags.Created) == NetSegment.Flags.None) dead.Add(kv.Key);
                else if (EditedList.Count < MaxHighlighted) EditedList.Add(kv.Key);
            }
            for (int i = 0; i < dead.Count; i++) _marks.Remove(dead[i]);
        }

        private void LoadIcons()
        {
            _iconsLoaded = true;
            for (int i = 0; i < _icons.Length; i++) _icons[i] = ModPaths.LoadIcon(Files[i]);
        }

        private void OnGUI()
        {
            if (Event.current.type != EventType.Repaint) return;
            if (_marks.Count == 0 || !ToolActive()) return;

            Camera cam = Camera.main;
            if (cam == null) return;
            if (!_iconsLoaded) LoadIcons();

            if (_box == null)
            {
                _box = new GUIStyle(GUI.skin.box);
            }

            NetSegment[] segs = NetManager.instance.m_segments.m_buffer;
            Vector3 camPos = cam.transform.position;
            Color old = GUI.color;

            foreach (KeyValuePair<ushort, int> kv in _marks)
            {
                Vector3 world = segs[kv.Key].m_middlePosition + new Vector3(0f, 8f, 0f);
                if ((world - camPos).sqrMagnitude > MaxDistance * MaxDistance) continue;

                Vector3 sp = cam.WorldToScreenPoint(world);
                if (sp.z <= 0f) continue;
                float sy = Screen.height - sp.y;
                if (sp.x < -60f || sp.x > Screen.width + 60f || sy < -60f || sy > Screen.height + 60f) continue;

                int count = 0;
                for (int b = 0; b < Bits.Length; b++)
                {
                    if ((kv.Value & Bits[b]) != 0 && _icons[b] != null) count++;
                }
                if (count == 0) continue;

                float size = Mathf.Clamp(1600f / sp.z, 16f, 30f);
                float gap = 2f;
                float total = count * size + (count - 1) * gap;
                float x = sp.x - total * 0.5f;

                GUI.color = new Color(0.08f, 0.1f, 0.14f, 0.8f);
                GUI.DrawTexture(new Rect(x - 3f, sy - size - 3f, total + 6f, size + 6f), Texture2D.whiteTexture);
                GUI.color = Color.white;

                for (int b = 0; b < Bits.Length; b++)
                {
                    if ((kv.Value & Bits[b]) == 0 || _icons[b] == null) continue;
                    GUI.DrawTexture(new Rect(x, sy - size, size, size), _icons[b]);
                    x += size + gap;
                }
            }
            GUI.color = old;
        }
    }
}
