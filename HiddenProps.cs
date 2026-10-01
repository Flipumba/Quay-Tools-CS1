using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace QuayTools
{
    /// <summary>
    /// Segments whose default props (the props that belong to the network model itself: lights, trees, benches along
    /// its lanes) are not drawn. Our own prop lines are separate and are never affected. Saved in the savegame.
    /// </summary>
    internal static class HideStore
    {
        private const int FormatVersion = 1;
        private static readonly HashSet<ushort> Set = new HashSet<ushort>();

        /// <summary>Read by the render code without a lock (a plain array: a stale read is harmless).</summary>
        public static readonly bool[] Hidden = new bool[65536];

        public static volatile int Version;

        private static MethodInfo _updateRenderer;
        private static bool _searched;

        public static bool Has(ushort segment)
        {
            return Hidden[segment];
        }

        /// <summary>Simulation thread (or loading). The segment's render groups are rebuilt so that the change shows at once.</summary>
        public static void SetHidden(ushort segment, bool value)
        {
            bool changed;
            lock (Set)
            {
                changed = value ? Set.Add(segment) : Set.Remove(segment);
            }
            Hidden[segment] = value;
            Version++;
            if (changed) Refresh(segment);
        }

        private static void Refresh(ushort segment)
        {
            try
            {
                if (!_searched)
                {
                    _searched = true;
                    _updateRenderer = typeof(NetManager).GetMethod("UpdateSegmentRenderer", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                }
                if (_updateRenderer == null || NetManager.instance == null) return;
                if ((NetManager.instance.m_segments.m_buffer[segment].m_flags & NetSegment.Flags.Created) == NetSegment.Flags.None) return;
                _updateRenderer.Invoke(NetManager.instance, new object[] { segment, true });
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[QuayTools] Could not refresh the segment renderer: " + ex.Message);
            }
        }

        public static List<ushort> Snapshot()
        {
            lock (Set)
            {
                return new List<ushort>(Set);
            }
        }

        public static void Clear()
        {
            lock (Set)
            {
                foreach (ushort id in Set) Hidden[id] = false;
                Set.Clear();
            }
            Version++;
        }

        public static byte[] Save()
        {
            using (MemoryStream ms = new MemoryStream())
            using (BinaryWriter w = new BinaryWriter(ms))
            {
                lock (Set)
                {
                    NetSegment[] segs = NetManager.instance.m_segments.m_buffer;
                    List<ushort> keys = new List<ushort>();
                    foreach (ushort id in Set)
                    {
                        if ((segs[id].m_flags & NetSegment.Flags.Created) != NetSegment.Flags.None) keys.Add(id);
                    }
                    w.Write(FormatVersion);
                    w.Write(keys.Count);
                    for (int i = 0; i < keys.Count; i++) w.Write((int)keys[i]);
                }
                w.Flush();
                return ms.ToArray();
            }
        }

        public static void Load(byte[] data)
        {
            Clear();
            if (data == null || data.Length < 8) return;
            try
            {
                using (MemoryStream ms = new MemoryStream(data))
                using (BinaryReader r = new BinaryReader(ms))
                {
                    if (r.ReadInt32() != FormatVersion) return;
                    int count = r.ReadInt32();
                    lock (Set)
                    {
                        for (int i = 0; i < count; i++)
                        {
                            ushort id = (ushort)r.ReadInt32();
                            Set.Add(id);
                            Hidden[id] = true;
                        }
                    }
                    Version++;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[QuayTools] Could not read saved hidden-props segments: " + ex.Message);
                Clear();
            }
        }
    }

    /// <summary>
    /// The props of a network model are drawn lane by lane (NetLane.RenderInstance for the near view, PopulateGroupData
    /// for the combined distant batches). For hidden segments those routines are skipped. Methods are found by
    /// reflection (a ushort parameter whose name contains "segment"); the log says what was found.
    /// </summary>
    internal static class HidePropsPatch
    {
        public static bool H0(ushort __0) { return !HideStore.Hidden[__0]; }
        public static bool H1(ushort __1) { return !HideStore.Hidden[__1]; }
        public static bool H2(ushort __2) { return !HideStore.Hidden[__2]; }
        public static bool H3(ushort __3) { return !HideStore.Hidden[__3]; }

        public static void Apply(Harmony harmony)
        {
            try
            {
                int patched = 0;
                System.Text.StringBuilder all = new System.Text.StringBuilder();
                MethodInfo[] methods = typeof(NetLane).GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                for (int i = 0; i < methods.Length; i++)
                {
                    MethodInfo m = methods[i];
                    if (m.Name != "RenderInstance" && m.Name != "RenderDestroyedInstance" && m.Name != "PopulateGroupData") continue;

                    ParameterInfo[] ps = m.GetParameters();
                    int index = -1;
                    all.Append("\n  ").Append(m.ReturnType.Name).Append(' ').Append(m.Name).Append('(');
                    for (int k = 0; k < ps.Length; k++)
                    {
                        all.Append(k > 0 ? ", " : string.Empty).Append(ps[k].ParameterType.Name).Append(' ').Append(ps[k].Name);
                        if (index < 0 && ps[k].ParameterType == typeof(ushort) && ps[k].Name.IndexOf("segment", StringComparison.OrdinalIgnoreCase) >= 0) index = k;
                    }
                    all.Append(')');

                    if (index < 0 || index > 3 || m.ReturnType != typeof(void)) continue;
                    try
                    {
                        harmony.Patch(m, new HarmonyMethod(typeof(HidePropsPatch).GetMethod("H" + index, BindingFlags.Public | BindingFlags.Static)));
                        patched++;
                        all.Append("  <- patched (segment id at position ").Append(index).Append(')');
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning("[QuayTools] Hide props: could not patch " + m.Name + ": " + ex.Message);
                    }
                }
                Debug.Log("[QuayTools] Hide props: patched " + patched + " NetLane method(s):" + all);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[QuayTools] Hide props setup failed: " + ex);
            }
        }
    }
}
