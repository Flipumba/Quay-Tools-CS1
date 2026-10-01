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
    /// PathFind.ProcessItem(..., ushort segmentID, ...): every overload that has a parameter named segmentID is skipped
    /// for blocked segments, so the pathfinder never expands onto them. Overloads are looked up by reflection, so a game
    /// update that renames things only disables this feature (a line is written to the log).
    /// </summary>
    [HarmonyPatch]
    internal static class PedestrianPathPatch
    {
        public static IEnumerable<MethodBase> TargetMethods()
        {
            Type type = AccessTools.TypeByName("PathFind");
            if (type == null)
            {
                Debug.LogWarning("[QuayTools] PathFind not found: 'Remove pedestrian path' is unavailable");
                yield break;
            }

            int found = 0;
            MethodInfo[] methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            for (int i = 0; i < methods.Length; i++)
            {
                MethodInfo m = methods[i];
                if (m.Name != "ProcessItem" || m.ReturnType != typeof(void)) continue;

                ParameterInfo[] ps = m.GetParameters();
                bool has = false;
                for (int k = 0; k < ps.Length; k++)
                {
                    if (ps[k].Name == "segmentID" && ps[k].ParameterType == typeof(ushort)) has = true;
                }
                if (!has) continue;

                found++;
                yield return m;
            }

            Debug.Log("[QuayTools] Pedestrian block: patching " + found + " PathFind.ProcessItem overload(s)");
        }

        public static bool Prefix(ushort segmentID)
        {
            return !PedStore.Blocked[segmentID];
        }
    }
}
