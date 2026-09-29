using System;
using System.Collections.Generic;
using ColossalFramework;
using UnityEngine;

namespace QuayTools
{
    /// <summary>
    /// Writes a fence NetInfo into a segment's left/right fence slot, exactly what the vanilla
    /// fence tool does for roads (NetTool.UpgradeRoadFenceImpl), but without its RoadBaseAI check.
    /// The game itself saves, renders and deletes these fields together with the segment.
    /// </summary>
    internal static class FenceApplier
    {
        /// <param name="fence">Network model to place, or null to remove the fence.</param>
        public static void RequestSetFence(List<ushort> segments, List<bool> left, NetInfo fence, Action<string> report)
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
                        if (id == 0) continue;

                        NetSegment.Flags flags = segs[id].m_flags;
                        if ((flags & NetSegment.Flags.Created) == NetSegment.Flags.None) continue;
                        if ((flags & NetSegment.Flags.Collapsed) != NetSegment.Flags.None) continue;

                        if (left[i])
                        {
                            segs[id].LeftFenceInfo = fence;
                        }
                        else
                        {
                            segs[id].RightFenceInfo = fence;
                        }

                        nm.UpdateSegmentRenderer(id, false);
                        nm.UpdateNodeRenderer(segs[id].m_startNode, false);
                        nm.UpdateNodeRenderer(segs[id].m_endNode, false);
                        changed++;
                    }

                    string verb = fence == null ? "Removed fence from " : "Placed fence on ";
                    report(verb + changed + (changed == 1 ? " segment" : " segments"));
                }
                catch (Exception ex)
                {
                    Debug.LogError("[QuayTools] Setting fence failed: " + ex);
                    report("Fence change failed, see log");
                }
            });
        }
    }
}
