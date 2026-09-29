using System;
using System.Collections.Generic;
using ColossalFramework;
using UnityEngine;

namespace QuayTools
{
    /// <summary>
    /// Writes fence NetInfos into segment fence slots (like the vanilla fence tool does for roads, without its
    /// RoadBaseAI check) and refreshes the rendering. The game itself saves, renders and deletes these fields.
    /// Slots: the game's "left" slot is drawn on the geometric right side of the segment.
    /// </summary>
    internal static class FenceApplier
    {
        private static bool IsAlive(NetSegment[] segs, ushort id)
        {
            if (id == 0) return false;
            NetSegment.Flags f = segs[id].m_flags;
            return (f & NetSegment.Flags.Created) != NetSegment.Flags.None &&
                   (f & NetSegment.Flags.Collapsed) == NetSegment.Flags.None;
        }

        private static void RefreshRender(NetManager nm, NetSegment[] segs, ushort id)
        {
            nm.UpdateSegmentRenderer(id, false);
            nm.UpdateNodeRenderer(segs[id].m_startNode, false);
            nm.UpdateNodeRenderer(segs[id].m_endNode, false);
        }

        /// <summary>Puts a model (or null = none) on the land or the water side of every listed segment.</summary>
        public static void SetModel(List<ushort> segments, bool landSide, NetInfo model, Action<string> report)
        {
            Singleton<SimulationManager>.instance.AddAction(delegate ()
            {
                try
                {
                    NetManager nm = NetManager.instance;
                    NetSegment[] segs = nm.m_segments.m_buffer;
                    int changed = 0;

                    for (int i = 0; i < segments.Count; i++)
                    {
                        ushort id = segments[i];
                        if (!IsAlive(segs, id)) continue;

                        QuayFrame frame = QuayGeometry.GetFrame(id);
                        bool slotLeft = QuayGeometry.SlotIsLeft(landSide, frame.WaterIsRight);

                        if (slotLeft) segs[id].LeftFenceInfo = model;
                        else segs[id].RightFenceInfo = model;

                        if (model != null)
                        {
                            FenceStore.GetOrCreate(id);
                        }
                        else if (segs[id].LeftFenceInfo == null && segs[id].RightFenceInfo == null)
                        {
                            FenceStore.Remove(id);
                        }

                        RefreshRender(nm, segs, id);
                        changed++;
                    }

                    if (report != null) report(Loc.F("applied", changed));
                }
                catch (Exception ex)
                {
                    Debug.LogError("[QuayTools] Setting fence model failed: " + ex);
                    if (report != null) report(Loc.T("failed"));
                }
            });
        }

        /// <summary>Removes both models (and our settings) from the listed segments.</summary>
        public static void ClearModels(List<ushort> segments, Action<string> report)
        {
            Singleton<SimulationManager>.instance.AddAction(delegate ()
            {
                try
                {
                    NetManager nm = NetManager.instance;
                    NetSegment[] segs = nm.m_segments.m_buffer;
                    int changed = 0;

                    for (int i = 0; i < segments.Count; i++)
                    {
                        ushort id = segments[i];
                        if (!IsAlive(segs, id)) continue;

                        segs[id].LeftFenceInfo = null;
                        segs[id].RightFenceInfo = null;
                        FenceStore.Remove(id);
                        RefreshRender(nm, segs, id);
                        changed++;
                    }

                    if (report != null) report(Loc.F("removed", changed));
                }
                catch (Exception ex)
                {
                    Debug.LogError("[QuayTools] Removing fence models failed: " + ex);
                    if (report != null) report(Loc.T("failed"));
                }
            });
        }

        /// <summary>Rebuilds the fences after their offsets or the "do not join" flag changed.</summary>
        public static void Refresh(List<ushort> segments)
        {
            Singleton<SimulationManager>.instance.AddAction(delegate ()
            {
                try
                {
                    NetManager nm = NetManager.instance;
                    NetSegment[] segs = nm.m_segments.m_buffer;

                    for (int i = 0; i < segments.Count; i++)
                    {
                        ushort id = segments[i];
                        if (IsAlive(segs, id)) RefreshRender(nm, segs, id);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError("[QuayTools] Fence refresh failed: " + ex);
                }
            });
        }
    }
}
