using System;
using System.Collections.Generic;
using ColossalFramework;
using UnityEngine;

namespace QuayTools
{
    /// <summary>
    /// All edits of Quay Tools. Every edit runs on the simulation thread, records an undo step and refreshes the
    /// rendering. Fence models are written into the segment fence slots (like the vanilla fence tool does for roads,
    /// without its RoadBaseAI check); the game itself saves, renders and deletes those fields.
    /// Slots: the game's "left" slot is drawn on the geometric right side of the segment.
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

        // ---------- fences ----------

        /// <summary>Puts a model (or null = none) on the land or the water side of every listed segment.</summary>
        public static void SetModel(List<ushort> segments, bool landSide, NetInfo model, Action<string> report)
        {
            Run(segments, null, delegate (ushort id)
            {
                NetSegment[] segs = NetManager.instance.m_segments.m_buffer;

                // make sure the slots match the current orientation before we address them by land/water
                FenceStore.Reconcile(id);

                QuayFrame frame = QuayGeometry.GetFrame(id);
                bool slotLeft = QuayGeometry.SlotIsLeft(landSide, frame.WaterIsRight);

                if (slotLeft) segs[id].LeftFenceInfo = model;
                else segs[id].RightFenceInfo = model;

                if (model != null)
                {
                    FenceStore.GetOrCreate(id);
                }
                else
                {
                    FenceSettings s;
                    if (segs[id].LeftFenceInfo == null && segs[id].RightFenceInfo == null &&
                        (!FenceStore.TryGet(id, out s) || IsDefault(s)))
                    {
                        FenceStore.Remove(id);
                        FenceHeight.Release(id);
                    }
                }
            }, "applied", report);
        }

        private static bool IsDefault(FenceSettings s)
        {
            return s.IsDefault();
        }

        public static void SetOffset(List<ushort> segments, bool landSide, bool horizontal, int value)
        {
            string key = "off|" + landSide + "|" + horizontal;
            Run(segments, key, delegate (ushort id)
            {
                FenceStore.Reconcile(id);
                FenceSettings s = FenceStore.GetOrCreate(id);
                if (landSide)
                {
                    if (horizontal) s.LandH = value; else s.LandV = value;
                }
                else
                {
                    if (horizontal) s.WaterH = value; else s.WaterV = value;
                }
            }, null, null);
        }

        /// <summary>Changes any other fence setting (end shifts, width scale, detaching). Dragging is merged into one undo step.</summary>
        public static void EditFence(List<ushort> segments, string property, Action<FenceSettings> apply)
        {
            Run(segments, "fence|" + property, delegate (ushort id)
            {
                FenceStore.Reconcile(id);
                FenceSettings s = FenceStore.GetOrCreate(id);
                apply(s);
            }, null, null);
        }

        public static void SetCap(List<ushort> segments, bool atStart, bool value)
        {
            Run(segments, null, delegate (ushort id)
            {
                FenceSettings s = FenceStore.GetOrCreate(id);
                if (atStart) s.CapStart = value; else s.CapEnd = value;
            }, null, null);
        }

        /// <summary>Removes both models (and our settings) from the listed segments.</summary>
        public static void ClearModels(List<ushort> segments, Action<string> report)
        {
            Run(segments, null, delegate (ushort id)
            {
                NetSegment[] segs = NetManager.instance.m_segments.m_buffer;
                segs[id].LeftFenceInfo = null;
                segs[id].RightFenceInfo = null;
                FenceStore.Remove(id);
                FenceHeight.Release(id);
            }, "removed", report, true);
        }

        /// <summary>Back to default settings: no offsets, no closing fences (the models stay).</summary>
        public static void ResetFences(List<ushort> segments, Action<string> report)
        {
            Run(segments, null, delegate (ushort id)
            {
                FenceSettings s;
                if (!FenceStore.TryGet(id, out s)) return;
                s.ResetOffsets();

                NetSegment[] segs = NetManager.instance.m_segments.m_buffer;
                if (segs[id].LeftFenceInfo == null && segs[id].RightFenceInfo == null)
                {
                    FenceStore.Remove(id);
                    FenceHeight.Release(id);
                }
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
            }, blocked ? "nop_done" : "nop_undone", report, true);
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

        // ---------- decal paths ----------

        /// <summary>Adds a decal path to every listed segment (or updates an existing one) with the given values.</summary>
        public static void AddDecal(List<ushort> segments, DecalSettings values, Action<string> report)
        {
            DecalSettings v = values.Clone();
            Run(segments, null, delegate (ushort id)
            {
                DecalStore.Set(id, v.Clone());
            }, "decal_added", report);
        }

        public static void RemoveDecal(List<ushort> segments, Action<string> report)
        {
            Run(segments, null, delegate (ushort id)
            {
                DecalStore.Remove(id);
            }, "decal_removed", report);
        }

        /// <summary>Changes one property of the existing decal paths (slider, colour). Dragging is merged into one undo step.</summary>
        public static void EditDecal(List<ushort> segments, string property, Action<DecalSettings> apply)
        {
            Run(segments, "decal|" + property, delegate (ushort id)
            {
                DecalSettings s;
                if (!DecalStore.TryGet(id, out s)) return;
                s = s.Clone();
                apply(s);
                DecalStore.Set(id, s);
            }, null, null);
        }

        public static void ResetDecals(List<ushort> segments, Action<string> report)
        {
            Run(segments, null, delegate (ushort id)
            {
                DecalSettings s;
                if (!DecalStore.TryGet(id, out s)) return;
                s = s.Clone();
                s.ResetToDefaults();
                DecalStore.Set(id, s);
            }, "reset_done", report, true);
        }
    }
}
