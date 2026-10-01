using System;
using System.Collections.Generic;
using ColossalFramework;
using UnityEngine;

namespace QuayTools
{
    /// <summary>Core logic: detect quays, collect connected chains, toggle the Invert flag.</summary>
    internal static class SegmentFlipper
    {
        private const int MaxChainLength = 2000;

        /// <summary>
        /// A segment counts as a quay if its network AI is QuayAI (or a subclass of it).
        /// Fallback for assets with a custom AI: "quay" in the prefab or item class name.
        /// "Allow any network" in the options bypasses the check.
        /// </summary>
        public static bool IsQuay(NetInfo info)
        {
            if (info == null) return false;

            if (Settings.AllowAnyNetwork) return true;

            if (info.m_netAI is QuayAI) return true;

            if (ContainsQuay(info.name)) return true;
            if (info.m_class != null && ContainsQuay(info.m_class.name)) return true;
            return false;
        }

        private static bool ContainsQuay(string s)
        {
            return !string.IsNullOrEmpty(s) && s.IndexOf("quay", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool IsCreated(ushort segmentId)
        {
            if (segmentId == 0) return false;
            NetSegment.Flags f = NetManager.instance.m_segments.m_buffer[segmentId].m_flags;
            return (f & NetSegment.Flags.Created) != NetSegment.Flags.None;
        }

        /// <summary>Segments at a node, excluding zeros.</summary>
        private static int CollectNodeSegments(ushort nodeId, ushort[] buffer)
        {
            NetNode[] nodes = NetManager.instance.m_nodes.m_buffer;
            int count = 0;
            for (int i = 0; i < 8; i++)
            {
                ushort seg = nodes[nodeId].GetSegment(i);
                if (seg != 0 && count < buffer.Length)
                {
                    buffer[count++] = seg;
                }
            }
            return count;
        }

        /// <summary>
        /// Collects the segment plus all quay segments connected to it through
        /// simple (2-segment) nodes, so a junction stops the chain.
        /// </summary>
        public static List<ushort> CollectChain(ushort startSegment)
        {
            return CollectChainOriented(startSegment, new Dictionary<ushort, bool>());
        }

        /// <summary>
        /// Same as CollectChain, and additionally fills orientation[segment] = true when that segment
        /// runs in the same direction (start node to end node) as the first one, false when reversed.
        /// Needed for anything that has a left/right side.
        /// </summary>
        public static List<ushort> CollectChainOriented(ushort startSegment, Dictionary<ushort, bool> orientation)
        {
            NetManager nm = NetManager.instance;
            NetSegment[] segs = nm.m_segments.m_buffer;

            List<ushort> result = new List<ushort>();
            HashSet<ushort> seen = new HashSet<ushort>();
            Queue<ushort> queue = new Queue<ushort>();
            ushort[] tmp = new ushort[8];

            NetInfo baseInfo = segs[startSegment].Info;

            orientation.Clear();
            orientation[startSegment] = true;
            seen.Add(startSegment);
            queue.Enqueue(startSegment);

            while (queue.Count > 0 && result.Count < MaxChainLength)
            {
                ushort current = queue.Dequeue();
                result.Add(current);

                ushort[] ends = { segs[current].m_startNode, segs[current].m_endNode };
                for (int e = 0; e < ends.Length; e++)
                {
                    ushort node = ends[e];
                    if (node == 0) continue;

                    int n = CollectNodeSegments(node, tmp);
                    if (n != 2) continue; // junction or dead end: stop here

                    ushort other = tmp[0] == current ? tmp[1] : tmp[0];
                    if (other == current || seen.Contains(other)) continue;
                    if (!IsCreated(other)) continue;
                    if (segs[other].Info != baseInfo) continue; // same quay type only

                    bool sameDirection =
                        (segs[current].m_endNode == node && segs[other].m_startNode == node) ||
                        (segs[current].m_startNode == node && segs[other].m_endNode == node);
                    orientation[other] = sameDirection ? orientation[current] : !orientation[current];

                    seen.Add(other);
                    queue.Enqueue(other);
                }
            }

            return result;
        }

        /// <summary>Toggles Invert on one segment and asks the game to rebuild it. Simulation thread only.</summary>
        private static void FlipOne(ushort segmentId)
        {
            NetManager nm = NetManager.instance;
            NetSegment[] segs = nm.m_segments.m_buffer;

            segs[segmentId].m_flags ^= NetSegment.Flags.Invert;

            // Inverting mirrors the quay model, so land and water swap sides; our lines are placed relative to the water
            // side and follow it by themselves.
            LockStore.Refresh(segmentId); // a deliberate flip: a locked segment keeps the new orientation

            ushort startNode = segs[segmentId].m_startNode;
            ushort endNode = segs[segmentId].m_endNode;

            nm.UpdateSegment(segmentId);
            if (startNode != 0) nm.UpdateNode(startNode);
            if (endNode != 0) nm.UpdateNode(endNode);
        }

        /// <summary>
        /// Queues the flip on the simulation thread. Returns a short status message for the UI
        /// through the callback (called on the simulation thread).
        /// </summary>
        public static void RequestFlip(ushort segmentId, bool wholeChain, Action<string> report)
        {
            Singleton<SimulationManager>.instance.AddAction(delegate ()
            {
                try
                {
                    if (!IsCreated(segmentId))
                    {
                        report(Loc.T("gone"));
                        return;
                    }

                    NetInfo info = NetManager.instance.m_segments.m_buffer[segmentId].Info;
                    if (!IsQuay(info))
                    {
                        report(Loc.F("notquay", info != null ? info.name : "?"));
                        return;
                    }

                    List<ushort> list;
                    if (wholeChain)
                    {
                        list = CollectChain(segmentId);
                    }
                    else
                    {
                        list = new List<ushort>();
                        list.Add(segmentId);
                    }

                    for (int i = 0; i < list.Count; i++)
                    {
                        FlipOne(list[i]);
                    }

                    report(list.Count == 1 ? Loc.T("flipped1") : Loc.F("flippedN", list.Count));
                }
                catch (Exception ex)
                {
                    Debug.LogError("[QuayTools] Flip failed: " + ex);
                    report(Loc.T("failed"));
                }
            });
        }
    }
}
