using System;
using System.Collections.Generic;
using System.IO;
using ColossalFramework;
using ICities;
using UnityEngine;

namespace QuayTools
{
    /// <summary>
    /// Segments whose orientation (the Invert flag) is locked: if the game or another mod flips such a segment
    /// by itself (for example while a node is moved), it is flipped back right after the simulation tick.
    /// The stored value is the Invert flag the segment must keep. Saved in the savegame.
    /// </summary>
    internal static class LockStore
    {
        private const int FormatVersion = 1;
        private const int RunawayLimit = 300; // flips within RunawayWindow ticks that count as an endless fight with another mod
        private const int RunawayWindow = 600;

        private static readonly Dictionary<ushort, bool> Map = new Dictionary<ushort, bool>();
        private static readonly Dictionary<ushort, int> Flips = new Dictionary<ushort, int>();
        private static readonly List<ushort> Keys = new List<ushort>();
        private static int _ticks;

        /// <summary>Changes whenever the set of locked segments changes (the overlay and the panel poll it).</summary>
        public static volatile int Version;

        public static bool Any
        {
            get { lock (Map) { return Map.Count > 0; } }
        }

        public static bool IsLocked(ushort segment)
        {
            lock (Map)
            {
                return Map.ContainsKey(segment);
            }
        }

        /// <summary>-1: not locked, otherwise the locked Invert flag (0 or 1). Used by undo snapshots.</summary>
        public static int Get(ushort segment)
        {
            lock (Map)
            {
                bool v;
                if (!Map.TryGetValue(segment, out v)) return -1;
                return v ? 1 : 0;
            }
        }

        public static void SetRaw(ushort segment, int value)
        {
            lock (Map)
            {
                if (value < 0) Map.Remove(segment);
                else Map[segment] = value != 0;
            }
            Version++;
        }

        private static bool CurrentInvert(ushort segment)
        {
            return (NetManager.instance.m_segments.m_buffer[segment].m_flags & NetSegment.Flags.Invert) != NetSegment.Flags.None;
        }

        /// <summary>Locks the segment in its current orientation (simulation thread).</summary>
        public static void Lock(ushort segment)
        {
            lock (Map)
            {
                Map[segment] = CurrentInvert(segment);
                Flips.Remove(segment);
            }
            Version++;
        }

        public static void Unlock(ushort segment)
        {
            lock (Map)
            {
                Map.Remove(segment);
                Flips.Remove(segment);
            }
            Version++;
        }

        /// <summary>The user flipped a locked segment on purpose: the lock keeps the new orientation.</summary>
        public static void Refresh(ushort segment)
        {
            lock (Map)
            {
                if (Map.ContainsKey(segment)) Map[segment] = CurrentInvert(segment);
            }
        }

        public static List<ushort> Snapshot()
        {
            lock (Map)
            {
                return new List<ushort>(Map.Keys);
            }
        }

        public static void Clear()
        {
            lock (Map)
            {
                Map.Clear();
                Flips.Clear();
            }
            Version++;
        }

        /// <summary>
        /// Called after every simulation tick (simulation thread): flips back the locked segments that were turned around.
        /// </summary>
        public static void Enforce()
        {
            lock (Map)
            {
                if (Map.Count == 0) return;
                Keys.Clear();
                Keys.AddRange(Map.Keys);
            }

            _ticks++;
            if (_ticks >= RunawayWindow)
            {
                _ticks = 0;
                lock (Map) { Flips.Clear(); }
            }

            NetManager nm = NetManager.instance;
            NetSegment[] segs = nm.m_segments.m_buffer;

            for (int i = 0; i < Keys.Count; i++)
            {
                ushort id = Keys[i];
                if ((segs[id].m_flags & NetSegment.Flags.Created) == NetSegment.Flags.None)
                {
                    // the segment is gone (its id may be reused by another one)
                    lock (Map) { Map.Remove(id); Flips.Remove(id); }
                    Version++;
                    continue;
                }

                bool wanted;
                lock (Map)
                {
                    if (!Map.TryGetValue(id, out wanted)) continue;
                }
                if (CurrentInvert(id) == wanted) continue;

                int count;
                lock (Map)
                {
                    Flips.TryGetValue(id, out count);
                    count++;
                    Flips[id] = count;
                }
                if (count > RunawayLimit)
                {
                    Debug.LogWarning("[QuayTools] Segment " + id + ": orientation lock released, something keeps flipping the segment back");
                    Unlock(id);
                    continue;
                }

                segs[id].m_flags ^= NetSegment.Flags.Invert;

                ushort a = segs[id].m_startNode, b = segs[id].m_endNode;
                nm.UpdateSegment(id);
                if (a != 0) nm.UpdateNode(a);
                if (b != 0) nm.UpdateNode(b);
            }
        }

        // ---------- savegame ----------

        public static byte[] Save()
        {
            using (MemoryStream ms = new MemoryStream())
            using (BinaryWriter w = new BinaryWriter(ms))
            {
                lock (Map)
                {
                    NetSegment[] segs = NetManager.instance.m_segments.m_buffer;
                    List<ushort> keys = new List<ushort>();
                    foreach (KeyValuePair<ushort, bool> kv in Map)
                    {
                        if ((segs[kv.Key].m_flags & NetSegment.Flags.Created) != NetSegment.Flags.None) keys.Add(kv.Key);
                    }

                    w.Write(FormatVersion);
                    w.Write(keys.Count);
                    for (int i = 0; i < keys.Count; i++)
                    {
                        w.Write((int)keys[i]);
                        w.Write(Map[keys[i]]);
                    }
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
                    int version = r.ReadInt32();
                    if (version != FormatVersion) return;

                    int count = r.ReadInt32();
                    lock (Map)
                    {
                        for (int i = 0; i < count; i++)
                        {
                            ushort id = (ushort)r.ReadInt32();
                            Map[id] = r.ReadBoolean();
                        }
                    }
                }
                Version++;
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[QuayTools] Could not read saved segment locks: " + ex.Message);
                Clear();
            }
        }
    }

    /// <summary>Runs the orientation lock after every simulation tick.</summary>
    public class LockThreading : ThreadingExtensionBase
    {
        public override void OnAfterSimulationTick()
        {
            try
            {
                if (LockStore.Any) LockStore.Enforce();
            }
            catch (Exception ex)
            {
                Debug.LogError("[QuayTools] Orientation lock failed: " + ex);
            }
        }
    }
}
