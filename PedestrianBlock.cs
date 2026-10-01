using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace QuayTools
{
    /// <summary>
    /// Segments on which pedestrians must not walk: the pathfinder skips them, so citizens neither route over them
    /// nor see them as a path. Saved in the savegame. Paths that were already found keep working until they end.
    /// </summary>
    internal static class PedStore
    {
        private const int FormatVersion = 1;
        private static readonly HashSet<ushort> Set = new HashSet<ushort>();

        /// <summary>Read by the pathfinder threads without a lock (a plain array: a stale read is harmless).</summary>
        public static readonly bool[] Blocked = new bool[65536];

        public static volatile int Version;

        public static bool Has(ushort segment)
        {
            return Blocked[segment];
        }

        public static void SetBlocked(ushort segment, bool value)
        {
            lock (Set)
            {
                if (value) Set.Add(segment); else Set.Remove(segment);
            }
            Blocked[segment] = value;
            Version++;
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
                foreach (ushort id in Set) Blocked[id] = false;
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
                    for (int i = 0; i < count; i++) SetBlocked((ushort)r.ReadInt32(), true);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[QuayTools] Could not read saved pedestrian blocks: " + ex.Message);
                Clear();
            }
        }
    }

    /// <summary>
    /// Every PathFind.ProcessItem* method that receives a segment id (a ushort parameter whose name contains "segment")
    /// is skipped for blocked segments when the path being searched is a walking path (pedestrian lanes, no vehicle
    /// lanes). The pathfinder then never expands onto them, so citizens neither route over them nor see them as a path;
    /// cars, buses and so on are not affected. Methods are found by reflection, so a game update that renames things
    /// only disables this feature (the log says what was found). Paths that were already found keep working until they end.
    /// </summary>
    internal static class PedestrianPathPatch
    {
        private const int PedestrianLane = 4;   // NetInfo.LaneType.Pedestrian
        private const int VehicleLane = 2;      // NetInfo.LaneType.Vehicle

        private static Func<object, int> _laneTypes;

        /// <summary>The lane types of the path the thread is searching right now (private field m_laneTypes), read through a compiled getter.</summary>
        private static Func<object, int> MakeGetter(Type type)
        {
            FieldInfo field = type.GetField("m_laneTypes", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (field == null) return null;

            System.Reflection.Emit.DynamicMethod dm = new System.Reflection.Emit.DynamicMethod("QuayTools_laneTypes", typeof(int), new Type[] { typeof(object) }, type, true);
            System.Reflection.Emit.ILGenerator il = dm.GetILGenerator();
            il.Emit(System.Reflection.Emit.OpCodes.Ldarg_0);
            il.Emit(System.Reflection.Emit.OpCodes.Castclass, type);
            il.Emit(System.Reflection.Emit.OpCodes.Ldfld, field);
            il.Emit(System.Reflection.Emit.OpCodes.Ret);
            return (Func<object, int>)dm.CreateDelegate(typeof(Func<object, int>));
        }

        private static bool Skip(object pathFind, ushort segment)
        {
            if (!PedStore.Blocked[segment]) return false;
            int lanes = _laneTypes(pathFind);
            return (lanes & PedestrianLane) != 0 && (lanes & VehicleLane) == 0;
        }

        // one prefix per parameter position of the segment id (the parameter is read by position)
        public static bool P0(object __instance, ushort __0) { return !Skip(__instance, __0); }
        public static bool P1(object __instance, ushort __1) { return !Skip(__instance, __1); }
        public static bool P2(object __instance, ushort __2) { return !Skip(__instance, __2); }
        public static bool P3(object __instance, ushort __3) { return !Skip(__instance, __3); }
        public static bool P4(object __instance, ushort __4) { return !Skip(__instance, __4); }
        public static bool P5(object __instance, ushort __5) { return !Skip(__instance, __5); }
        public static bool P6(object __instance, ushort __6) { return !Skip(__instance, __6); }
        public static bool P7(object __instance, ushort __7) { return !Skip(__instance, __7); }
        public static bool P8(object __instance, ushort __8) { return !Skip(__instance, __8); }
        public static bool P9(object __instance, ushort __9) { return !Skip(__instance, __9); }

        public static void Apply(Harmony harmony)
        {
            try
            {
                Type type = AccessTools.TypeByName("PathFind");
                if (type == null)
                {
                    Debug.LogWarning("[QuayTools] PathFind not found: 'Remove pedestrian path' is unavailable");
                    return;
                }

                _laneTypes = MakeGetter(type);
                if (_laneTypes == null)
                {
                    Debug.LogWarning("[QuayTools] PathFind.m_laneTypes not found: 'Remove pedestrian path' is unavailable");
                    return;
                }

                System.Text.StringBuilder all = new System.Text.StringBuilder();
                int patched = 0;
                MethodInfo[] methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                for (int i = 0; i < methods.Length; i++)
                {
                    MethodInfo m = methods[i];
                    if (!m.Name.StartsWith("ProcessItem", StringComparison.Ordinal)) continue;

                    ParameterInfo[] ps = m.GetParameters();
                    int index = -1;
                    all.Append("\n  ").Append(m.ReturnType.Name).Append(' ').Append(m.Name).Append('(');
                    for (int k = 0; k < ps.Length; k++)
                    {
                        all.Append(k > 0 ? ", " : string.Empty).Append(ps[k].ParameterType.Name).Append(' ').Append(ps[k].Name);
                        if (index < 0 && ps[k].ParameterType == typeof(ushort) && ps[k].Name.IndexOf("segment", StringComparison.OrdinalIgnoreCase) >= 0) index = k;
                    }
                    all.Append(')');

                    if (index < 0 || index > 9 || m.ReturnType != typeof(void)) continue;

                    try
                    {
                        MethodInfo prefix = typeof(PedestrianPathPatch).GetMethod("P" + index, BindingFlags.Public | BindingFlags.Static);
                        harmony.Patch(m, new HarmonyMethod(prefix));
                        patched++;
                        all.Append("  <- patched (segment id at position ").Append(index).Append(')');
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning("[QuayTools] Pedestrian block: could not patch " + m.Name + ": " + ex.Message);
                    }
                }

                Debug.Log("[QuayTools] Pedestrian block: patched " + patched + " PathFind method(s). ProcessItem* methods of this game version:" + all);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[QuayTools] Pedestrian block setup failed: " + ex);
            }
        }
    }
}
