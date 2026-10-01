using System;
using System.Collections.Generic;
using ColossalFramework;
using UnityEngine;

namespace QuayTools
{
    /// <summary>
    /// All edits of Quay Tools. Every edit runs on the simulation thread, records an undo step and refreshes the
    /// rendering. Network-model lines, prop lines and texture paths live in our own stores (saved in the savegame).
    /// </summary>
    internal static class FenceApplier
    {
        internal static bool IsAlive(NetSegment[] segs, ushort id)
        {
            if (id == 0) return false;
            NetSegment.Flags f = segs[id].m_flags;
            return (f & NetSegment.Flags.Created) != NetSegment.Flags.None &&
                   (f & NetSegment.Flags.Collapsed) == NetSegment.Flags.None;
        }

        internal static void RefreshRender(NetManager nm, NetSegment[] segs, ushort id)
        {
            nm.UpdateSegmentRenderer(id, false);
            nm.UpdateNodeRenderer(segs[id].m_startNode, false);
            nm.UpdateNodeRenderer(segs[id].m_endNode, false);
        }

        /// <summary>
        /// Runs an edit on every listed segment (simulation thread), records one undo step for it and refreshes.
        /// key: edits with the same key in quick succession are merged into one undo step (slider dragging).
        /// doneKey: Loc key of the status message that receives the number of changed segments.
        /// </summary>
        public static void Run(List<ushort> segments, string key, Action<ushort> mutate, string doneKey, Action<string> report, bool reloadUi = false)
        {
            float stamp = Time.realtimeSinceStartup; // main thread
            List<ushort> list = new List<ushort>(segments);

            Singleton<SimulationManager>.instance.AddAction(delegate ()
            {
                try
                {
                    NetManager nm = NetManager.instance;
                    NetSegment[] segs = nm.m_segments.m_buffer;
                    Dictionary<ushort, SegSnap> before = new Dictionary<ushort, SegSnap>();
                    Dictionary<ushort, SegSnap> after = new Dictionary<ushort, SegSnap>();
                    int changed = 0;

                    for (int i = 0; i < list.Count; i++)
                    {
                        ushort id = list[i];
                        if (!IsAlive(segs, id)) continue;

                        before[id] = SegSnap.Capture(id);
                        mutate(id);
                        after[id] = SegSnap.Capture(id);

                        RefreshRender(nm, segs, id);
                        changed++;
                    }

                    History.Push(key, stamp, before, after);
                    if (reloadUi) History.RestoreVersion++;
                    if (report != null && doneKey != null) report(Loc.F(doneKey, changed));
                }
                catch (Exception ex)
                {
                    Debug.LogError("[QuayTools] Edit failed: " + ex);
                    if (report != null) report(Loc.T("failed"));
                }
            });
        }

        // ---------- network-model lines ----------

        /// <summary>Appends a network-model line (a copy of the template) to every listed segment.</summary>
        public static void AddNetLine(List<ushort> segments, NetLine template, Action<string> report)
        {
            NetLine t = template.Clone();
            Run(segments, null, delegate (ushort id)
            {
                NetLineSet set;
                NetLineSet copy = NetLineStore.TryGet(id, out set) ? set.Clone() : new NetLineSet();
                if (copy.Lines.Count >= NetLineStore.MaxLinesPerSegment) return;
                copy.Lines.Add(t.Clone());
                NetLineStore.Set(id, copy);
            }, "line_added", report, true);
        }

        /// <summary>Changes one property of the line with the given index on every listed segment that has it.</summary>
        public static void EditNetLine(List<ushort> segments, int index, string property, Action<NetLine> apply)
        {
            Run(segments, "net|" + index + "|" + property, delegate (ushort id)
            {
                NetLineSet set;
                if (!NetLineStore.TryGet(id, out set) || index < 0 || index >= set.Lines.Count) return;
                NetLineSet copy = set.Clone();
                apply(copy.Lines[index]);
                NetLineStore.Set(id, copy);
            }, null, null);
        }

        public static void RemoveNetLine(List<ushort> segments, int index, Action<string> report)
        {
            Run(segments, null, delegate (ushort id)
            {
                NetLineSet set;
                if (!NetLineStore.TryGet(id, out set) || index < 0 || index >= set.Lines.Count) return;
                NetLineSet copy = set.Clone();
                copy.Lines.RemoveAt(index);
                NetLineStore.Set(id, copy);
            }, "line_removed", report, true);
        }

        public static void ClearNetLines(List<ushort> segments, Action<string> report)
        {
            Run(segments, null, delegate (ushort id)
            {
                NetLineStore.Remove(id);
            }, "line_cleared", report, true);
        }

        /// <summary>Back to default values of all lines (the models stay).</summary>
        public static void ResetNetLines(List<ushort> segments, Action<string> report)
        {
            Run(segments, null, delegate (ushort id)
            {
                NetLineSet set;
                if (!NetLineStore.TryGet(id, out set)) return;
                NetLineSet copy = set.Clone();
                for (int i = 0; i < copy.Lines.Count; i++) copy.Lines[i].ResetValues();
                NetLineStore.Set(id, copy);
            }, "reset_done", report, true);
        }

        // ---------- orientation lock ----------

        public static void SetLock(List<ushort> segments, bool locked, Action<string> report)
        {
            Run(segments, null, delegate (ushort id)
            {
                if (locked) LockStore.Lock(id);
                else LockStore.Unlock(id);
            }, locked ? "lock_done" : "unlock_done", report, true);
        }

        public static void SetNoPeds(List<ushort> segments, bool blocked, Action<string> report)
        {
            Run(segments, null, delegate (ushort id)
            {
                PedStore.SetBlocked(id, blocked);
                if (blocked) PedestrianPathPatch.QueueRepath(id);
            }, blocked ? "nop_done" : "nop_undone", report, true);
            if (blocked) Singleton<SimulationManager>.instance.AddAction(PedestrianPathPatch.RepathPending);
        }

        // ---------- prop lines ----------

        /// <summary>Appends a prop line (a copy of the template) to every listed segment.</summary>
        public static void AddPropEntry(List<ushort> segments, PropEntry template, Action<string> report)
        {
            PropEntry t = template.Clone();
            Run(segments, null, delegate (ushort id)
            {
                PropLine line;
                PropLine copy = PropLineStore.TryGet(id, out line) ? line.Clone() : new PropLine();
                copy.Entries.Add(t.Clone());
                PropLineStore.Set(id, copy);
            }, "prop_added", report, true);
        }

        /// <summary>Changes one property of the prop line with the given index on every listed segment that has it.</summary>
        public static void EditPropEntry(List<ushort> segments, int index, string property, Action<PropEntry> apply)
        {
            Run(segments, "prop|" + index + "|" + property, delegate (ushort id)
            {
                PropLine line;
                if (!PropLineStore.TryGet(id, out line) || index < 0 || index >= line.Entries.Count) return;
                PropLine copy = line.Clone();
                apply(copy.Entries[index]);
                PropLineStore.Set(id, copy);
            }, null, null);
        }

        public static void RemovePropEntry(List<ushort> segments, int index, Action<string> report)
        {
            Run(segments, null, delegate (ushort id)
            {
                PropLine line;
                if (!PropLineStore.TryGet(id, out line) || index < 0 || index >= line.Entries.Count) return;
                PropLine copy = line.Clone();
                copy.Entries.RemoveAt(index);
                PropLineStore.Set(id, copy);
            }, "prop_removed", report, true);
        }

        public static void ClearPropLines(List<ushort> segments, Action<string> report)
        {
            Run(segments, null, delegate (ushort id)
            {
                PropLineStore.Remove(id);
            }, "prop_cleared", report, true);
        }

        // ---------- texture paths ----------

        /// <summary>Appends a texture path (a copy of the template) to every listed segment.</summary>
        public static void AddDecal(List<ushort> segments, DecalSettings values, Action<string> report)
        {
            DecalSettings v = values.Clone();
            Run(segments, null, delegate (ushort id)
            {
                DecalSet set;
                DecalSet copy = DecalStore.TryGet(id, out set) ? set.Clone() : new DecalSet();
                if (copy.Paths.Count >= DecalStore.MaxPathsPerSegment) return;
                copy.Paths.Add(v.Clone());
                DecalStore.Set(id, copy);
            }, "decal_added", report, true);
        }

        public static void RemoveDecal(List<ushort> segments, int index, Action<string> report)
        {
            Run(segments, null, delegate (ushort id)
            {
                DecalSet set;
                if (!DecalStore.TryGet(id, out set) || index < 0 || index >= set.Paths.Count) return;
                DecalSet copy = set.Clone();
                copy.Paths.RemoveAt(index);
                DecalStore.Set(id, copy);
            }, "decal_removed", report, true);
        }

        public static void ClearDecals(List<ushort> segments, Action<string> report)
        {
            Run(segments, null, delegate (ushort id)
            {
                DecalStore.Remove(id);
            }, "decal_cleared", report, true);
        }

        /// <summary>Changes one property of the path with the given index (slider, colour). Dragging is merged into one undo step.</summary>
        public static void EditDecal(List<ushort> segments, int index, string property, Action<DecalSettings> apply)
        {
            Run(segments, "decal|" + index + "|" + property, delegate (ushort id)
            {
                DecalSet set;
                if (!DecalStore.TryGet(id, out set) || index < 0 || index >= set.Paths.Count) return;
                DecalSet copy = set.Clone();
                apply(copy.Paths[index]);
                DecalStore.Set(id, copy);
            }, null, null);
        }

        public static void ResetDecals(List<ushort> segments, Action<string> report)
        {
            Run(segments, null, delegate (ushort id)
            {
                DecalSet set;
                if (!DecalStore.TryGet(id, out set)) return;
                DecalSet copy = set.Clone();
                for (int i = 0; i < copy.Paths.Count; i++)
                {
                    string keep = copy.Paths[i].Prop;
                    copy.Paths[i].ResetToDefaults();
                    copy.Paths[i].Prop = keep;
                }
                DecalStore.Set(id, copy);
            }, "reset_done", report, true);
        }
    }
}
