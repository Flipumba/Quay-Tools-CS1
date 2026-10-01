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
        private const int PedestrianLane = 2;   // NetInfo.LaneType.Pedestrian (PathFind tests m_laneTypes & 2 for walking)
        private const int VehicleLane = 1;      // NetInfo.LaneType.Vehicle

        // lane-type getters per path-finder class (the game's PathFind and classes derived from it, for example
        // TM:PE's CustomPathFind, which runs its own copy of the search with its own fields); replaced as a whole when
        // changed, so the path-finding threads can read it without a lock
        private static Dictionary<Type, Func<object, int>> _getters = new Dictionary<Type, Func<object, int>>();
        private static readonly HashSet<MethodBase> Patched = new HashSet<MethodBase>();

        /// <summary>The lane types of the path the thread is searching right now: the field of the class that actually runs the search, read through a compiled getter.</summary>
        private static Func<object, int> MakeGetter(Type type)
        {
            FieldInfo field = null;
            for (Type t = type; t != null && field == null && t != typeof(object); t = t.BaseType)
            {
                FieldInfo[] fields = t.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly);
                for (int i = 0; i < fields.Length; i++)
                {
                    string n = fields[i].Name.Replace("_", string.Empty).ToLowerInvariant();
                    if (n != "lanetypes") continue;
                    Type ft = fields[i].FieldType;
                    if (ft.IsEnum || ft == typeof(int) || ft == typeof(byte) || ft == typeof(uint) || ft == typeof(short) || ft == typeof(ushort))
                    {
                        field = fields[i];
                        break;
                    }
                }
            }
            if (field == null) return null;

            System.Reflection.Emit.DynamicMethod dm = new System.Reflection.Emit.DynamicMethod("QuayTools_laneTypes", typeof(int), new Type[] { typeof(object) }, field.DeclaringType, true);
            System.Reflection.Emit.ILGenerator il = dm.GetILGenerator();
            il.Emit(System.Reflection.Emit.OpCodes.Ldarg_0);
            il.Emit(System.Reflection.Emit.OpCodes.Castclass, field.DeclaringType);
            il.Emit(System.Reflection.Emit.OpCodes.Ldfld, field);
            il.Emit(System.Reflection.Emit.OpCodes.Conv_I4);
            il.Emit(System.Reflection.Emit.OpCodes.Ret);
            return (Func<object, int>)dm.CreateDelegate(typeof(Func<object, int>));
        }

        private static bool Skip(object pathFind, ushort segment)
        {
            if (!PedStore.Blocked[segment]) return false;
            Func<object, int> getter;
            if (pathFind == null || !_getters.TryGetValue(pathFind.GetType(), out getter) || getter == null) return false;
            int lanes = getter(pathFind);
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

        // the same for methods that return bool (ProcessItemCosts returns false when a segment is not usable)
        public static bool B0(object __instance, ushort __0, ref bool __result)
        {
            if (!Skip(__instance, __0)) return true;
            __result = false; // the game's own "nothing to expand" result
            return false;
        }
        public static bool B1(object __instance, ushort __1, ref bool __result)
        {
            if (!Skip(__instance, __1)) return true;
            __result = false; // the game's own "nothing to expand" result
            return false;
        }
        public static bool B2(object __instance, ushort __2, ref bool __result)
        {
            if (!Skip(__instance, __2)) return true;
            __result = false; // the game's own "nothing to expand" result
            return false;
        }
        public static bool B3(object __instance, ushort __3, ref bool __result)
        {
            if (!Skip(__instance, __3)) return true;
            __result = false; // the game's own "nothing to expand" result
            return false;
        }
        public static bool B4(object __instance, ushort __4, ref bool __result)
        {
            if (!Skip(__instance, __4)) return true;
            __result = false; // the game's own "nothing to expand" result
            return false;
        }
        public static bool B5(object __instance, ushort __5, ref bool __result)
        {
            if (!Skip(__instance, __5)) return true;
            __result = false; // the game's own "nothing to expand" result
            return false;
        }
        public static bool B6(object __instance, ushort __6, ref bool __result)
        {
            if (!Skip(__instance, __6)) return true;
            __result = false; // the game's own "nothing to expand" result
            return false;
        }
        public static bool B7(object __instance, ushort __7, ref bool __result)
        {
            if (!Skip(__instance, __7)) return true;
            __result = false; // the game's own "nothing to expand" result
            return false;
        }
        public static bool B8(object __instance, ushort __8, ref bool __result)
        {
            if (!Skip(__instance, __8)) return true;
            __result = false; // the game's own "nothing to expand" result
            return false;
        }
        public static bool B9(object __instance, ushort __9, ref bool __result)
        {
            if (!Skip(__instance, __9)) return true;
            __result = false; // the game's own "nothing to expand" result
            return false;
        }

        /// <summary>Forgets what was patched (the patches themselves are removed by HarmonySetup.Revert).</summary>
        public static void Reset()
        {
            Patched.Clear();
        }

        /// <summary>Patches PathFind and every class derived from it that has its own ProcessItem* methods. Safe to call again: only new methods are patched.</summary>
        public static void Apply(Harmony harmony)
        {
            try
            {
                Type baseType = AccessTools.TypeByName("PathFind");
                if (baseType == null)
                {
                    Debug.LogWarning("[QuayTools] PathFind not found: 'Remove pedestrian path' is unavailable");
                    return;
                }

                List<Type> types = new List<Type>();
                types.Add(baseType);
                Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
                for (int a = 0; a < assemblies.Length; a++)
                {
                    Type[] all;
                    try
                    {
                        all = assemblies[a].GetTypes();
                    }
                    catch (ReflectionTypeLoadException ex)
                    {
                        all = ex.Types;
                    }
                    catch (Exception)
                    {
                        continue;
                    }
                    for (int i = 0; i < all.Length; i++)
                    {
                        Type t = all[i];
                        if (t != null && t != baseType && baseType.IsAssignableFrom(t)) types.Add(t);
                    }
                }

                Dictionary<Type, Func<object, int>> getters = new Dictionary<Type, Func<object, int>>(_getters);
                System.Text.StringBuilder all2 = new System.Text.StringBuilder();
                int patched = 0;
                for (int ti = 0; ti < types.Count; ti++)
                {
                    Type type = types[ti];
                    Func<object, int> getter = null;
                    if (!getters.TryGetValue(type, out getter)) getter = MakeGetter(type);
                    getters[type] = getter;

                    all2.Append("\n ").Append(type.FullName).Append(getter != null ? string.Empty : "  (no lane-types field: skipped)");
                    if (getter == null) continue;

                    MethodInfo[] methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                    for (int i = 0; i < methods.Length; i++)
                    {
                        MethodInfo m = methods[i];
                        if (!m.Name.StartsWith("ProcessItem", StringComparison.Ordinal)) continue;
                        if (m.IsAbstract || m.GetMethodBody() == null) continue;

                        ParameterInfo[] ps = m.GetParameters();
                        int index = -1;
                        all2.Append("\n  ").Append(m.ReturnType.Name).Append(' ').Append(m.Name).Append('(');
                        for (int k = 0; k < ps.Length; k++)
                        {
                            all2.Append(k > 0 ? ", " : string.Empty).Append(ps[k].ParameterType.Name).Append(' ').Append(ps[k].Name);
                            if (index < 0 && ps[k].ParameterType == typeof(ushort) && ps[k].Name.IndexOf("segment", StringComparison.OrdinalIgnoreCase) >= 0) index = k;
                        }
                        all2.Append(')');

                        bool isBool = m.ReturnType == typeof(bool);
                        if (index < 0 || index > 9 || (m.ReturnType != typeof(void) && !isBool)) continue;
                        if (Patched.Contains(m))
                        {
                            all2.Append("  <- already patched");
                            continue;
                        }

                        try
                        {
                            MethodInfo prefix = typeof(PedestrianPathPatch).GetMethod((isBool ? "B" : "P") + index, BindingFlags.Public | BindingFlags.Static);
                            harmony.Patch(m, new HarmonyMethod(prefix));
                            Patched.Add(m);
                            patched++;
                            all2.Append("  <- patched (segment id at position ").Append(index).Append(')');
                        }
                        catch (Exception ex)
                        {
                            Debug.LogWarning("[QuayTools] Pedestrian block: could not patch " + type.Name + "." + m.Name + ": " + ex.Message);
                        }
                    }
                }
                _getters = getters;

                Debug.Log("[QuayTools] Pedestrian block: patched " + patched + " new method(s) in " + types.Count + " path-finder class(es):" + all2);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[QuayTools] Pedestrian block setup failed: " + ex);
            }
        }
    }
}
