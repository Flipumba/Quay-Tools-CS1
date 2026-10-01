using System;
using System.Collections.Generic;
using ColossalFramework;
using UnityEngine;

namespace QuayTools
{
    /// <summary>Everything Quay Tools stores for one segment: network-model lines, texture paths, prop lines, lock, pedestrian block.</summary>
    internal class SegSnap
    {
        public NetLineSet Lines;
        public DecalSet Decals;
        public PropLine Props;
        public int Lock = -1; // see LockStore.Get
        public bool NoPeds;
        public bool Hide;

        public static SegSnap Capture(ushort id)
        {
            SegSnap s = new SegSnap();
            NetLineSet n;
            if (NetLineStore.TryGet(id, out n)) s.Lines = n.Clone();
            DecalSet d;
            if (DecalStore.TryGet(id, out d)) s.Decals = d.Clone();
            PropLine p;
            if (PropLineStore.TryGet(id, out p)) s.Props = p.Clone();
            s.Lock = LockStore.Get(id);
            s.NoPeds = PedStore.Has(id);
            s.Hide = HideStore.Has(id);
            return s;
        }

        public bool SameAs(SegSnap o)
        {
            if (o == null) return false;
            if ((Lines == null) != (o.Lines == null)) return false;
            if (Lines != null && !Lines.SameAs(o.Lines)) return false;
            if ((Decals == null) != (o.Decals == null)) return false;
            if (Decals != null && !Decals.SameAs(o.Decals)) return false;
            if ((Props == null) != (o.Props == null)) return false;
            if (Props != null && !Props.SameAs(o.Props)) return false;
            if (Lock != o.Lock || NoPeds != o.NoPeds || Hide != o.Hide) return false;
            return true;
        }

        /// <summary>Simulation thread.</summary>
        public void Restore(ushort id)
        {
            NetLineStore.Set(id, Lines == null ? null : Lines.Clone());
            DecalStore.Set(id, Decals == null ? null : Decals.Clone());
            PropLineStore.Set(id, Props == null ? null : Props.Clone());
            LockStore.SetRaw(id, Lock);
            PedStore.SetBlocked(id, NoPeds);
            HideStore.SetHidden(id, Hide);
        }
    }

    /// <summary>
    /// Undo / redo of Quay Tools edits (network-model lines, prop lines, texture paths, resets).
    /// Own history: it does not go through other undo mods.
    /// </summary>
    internal static class History
    {
        private const int MaxEntries = 100;
        private const float CoalesceSeconds = 1.5f;

        private class Entry
        {
            public string Key;
            public float Stamp;
            public Dictionary<ushort, SegSnap> Before, After;
        }

        private static readonly List<Entry> UndoList = new List<Entry>();
        private static readonly List<Entry> RedoList = new List<Entry>();

        /// <summary>Changes whenever the history changes (the panel polls it to update its buttons).</summary>
        public static volatile int Version;

        /// <summary>Changes after undo, redo and reset (edits that change values behind the controls' back).</summary>
        public static volatile int RestoreVersion;

        public static bool CanUndo
        {
            get { lock (UndoList) { return UndoList.Count > 0; } }
        }

        public static bool CanRedo
        {
            get { lock (UndoList) { return RedoList.Count > 0; } }
        }

        public static void Clear()
        {
            lock (UndoList)
            {
                UndoList.Clear();
                RedoList.Clear();
            }
            Version++;
        }

        private static bool SameSegments(Dictionary<ushort, SegSnap> a, Dictionary<ushort, SegSnap> b)
        {
            if (a.Count != b.Count) return false;
            foreach (ushort k in a.Keys)
            {
                if (!b.ContainsKey(k)) return false;
            }
            return true;
        }

        /// <summary>
        /// Records an edit. Segments that did not really change are dropped; a series of edits with the same key
        /// (dragging a slider) within a short time becomes one step.
        /// </summary>
        public static void Push(string key, float stamp, Dictionary<ushort, SegSnap> before, Dictionary<ushort, SegSnap> after)
        {
            Dictionary<ushort, SegSnap> b = new Dictionary<ushort, SegSnap>();
            Dictionary<ushort, SegSnap> a = new Dictionary<ushort, SegSnap>();
            foreach (KeyValuePair<ushort, SegSnap> kv in before)
            {
                SegSnap other;
                if (!after.TryGetValue(kv.Key, out other)) continue;
                if (kv.Value.SameAs(other)) continue;
                b[kv.Key] = kv.Value;
                a[kv.Key] = other;
            }
            if (b.Count == 0) return;

            lock (UndoList)
            {
                RedoList.Clear();

                if (key != null && UndoList.Count > 0)
                {
                    Entry last = UndoList[UndoList.Count - 1];
                    if (last.Key == key && stamp - last.Stamp < CoalesceSeconds && SameSegments(last.Before, b))
                    {
                        last.After = a;
                        last.Stamp = stamp;
                        Version++;
                        return;
                    }
                }

                Entry e = new Entry();
                e.Key = key;
                e.Stamp = stamp;
                e.Before = b;
                e.After = a;
                UndoList.Add(e);
                if (UndoList.Count > MaxEntries) UndoList.RemoveAt(0);
            }
            Version++;
        }

        public static void Undo(Action<string> report)
        {
            Step(true, report);
        }

        public static void Redo(Action<string> report)
        {
            Step(false, report);
        }

        private static void Step(bool undo, Action<string> report)
        {
            Singleton<SimulationManager>.instance.AddAction(delegate ()
            {
                try
                {
                    Entry e;
                    lock (UndoList)
                    {
                        List<Entry> source = undo ? UndoList : RedoList;
                        List<Entry> target2 = undo ? RedoList : UndoList;
                        if (source.Count == 0)
                        {
                            if (report != null) report(Loc.T(undo ? "nothing_undo" : "nothing_redo"));
                            return;
                        }
                        e = source[source.Count - 1];
                        source.RemoveAt(source.Count - 1);
                        target2.Add(e);
                        e.Key = null; // never merge later edits into a step that was undone / redone
                    }

                    NetManager nm = NetManager.instance;
                    NetSegment[] segs = nm.m_segments.m_buffer;
                    Dictionary<ushort, SegSnap> target = undo ? e.Before : e.After;
                    int n = 0;
                    foreach (KeyValuePair<ushort, SegSnap> kv in target)
                    {
                        if (!FenceApplier.IsAlive(segs, kv.Key)) continue;
                        kv.Value.Restore(kv.Key);
                        FenceApplier.RefreshRender(nm, segs, kv.Key);
                        n++;
                    }

                    Version++;
                    RestoreVersion++;
                    if (report != null) report(Loc.F(undo ? "undone" : "redone", n));
                }
                catch (Exception ex)
                {
                    Debug.LogError("[QuayTools] Undo/redo failed: " + ex);
                    if (report != null) report(Loc.T("failed"));
                }
            });
        }
    }
}
