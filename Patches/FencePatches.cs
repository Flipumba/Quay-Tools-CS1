using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace QuayTools.Patches
{
    /// <summary>Non-zero while the game is building fence geometry on this thread.</summary>
    internal static class FenceContext
    {
        [ThreadStatic]
        public static int Depth;
    }

    /// <summary>Marks the three game routines that build fences, so that only fence corners are changed.</summary>
    [HarmonyPatch]
    internal static class FenceContextPatch
    {
        public static IEnumerable<MethodBase> TargetMethods()
        {
            MethodBase m = Find(typeof(NetSegment), "RefreshRoadFence");
            if (m != null) yield return m;

            m = Find(typeof(NetNode), "RefreshEndFenceData");
            if (m != null) yield return m;

            m = Find(typeof(NetNode), "RefreshBendFenceData");
            if (m != null) yield return m;
        }

        private static MethodBase Find(Type type, string name)
        {
            try
            {
                MethodBase m = AccessTools.Method(type, name);
                if (m == null) Debug.LogWarning("[QuayTools] Game method not found: " + type.Name + "." + name);
                return m;
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[QuayTools] Game method lookup failed: " + type.Name + "." + name + ": " + ex.Message);
                return null;
            }
        }

        public static void Prefix()
        {
            FenceContext.Depth++;
        }

        public static Exception Finalizer(Exception __exception)
        {
            FenceContext.Depth--;
            if (FenceContext.Depth < 0) FenceContext.Depth = 0;
            return __exception;
        }
    }

    /// <summary>
    /// NetSegment.CalculateCorner(ushort segment, bool heightOffset, bool start, bool left,
    ///                            ref Vector3 pos, ref Vector3 dir, ref bool smooth, float offset)
    /// Inside fence building (node end/bend fences): moves the corner sideways and vertically.
    /// </summary>
    [HarmonyPatch]
    internal static class CornerPatch
    {
        public static MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(NetSegment), "CalculateCorner", new Type[]
            {
                typeof(ushort), typeof(bool), typeof(bool), typeof(bool),
                typeof(Vector3).MakeByRefType(), typeof(Vector3).MakeByRefType(), typeof(bool).MakeByRefType(),
                typeof(float)
            });
        }

        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        public static void Postfix(ushort __0, bool __2, bool __3, ref Vector3 __4)
        {
            if (FenceContext.Depth <= 0) return;
            FenceStore.TryAdjustCorner(__0, __2, __3, ref __4);
        }
    }

    /// <summary>
    /// NetSegment.RefreshRoadFence(CameraInfo, ushort segment, int layerMask, NetInfo info, NetInfo fenceInfo,
    ///                             ref RenderManager.Instance data, bool leftFence)
    /// After the game built a fence of one of our segments, rebuilds its shape from the final corners (so that other
    /// mods that change node corners are respected) and, for terrain-following models, supplies a deck height map.
    /// </summary>
    [HarmonyPatch]
    internal static class FenceHeightPatch
    {
        private static bool _failed;

        public static MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(NetSegment), "RefreshRoadFence");
        }

        public static void Prefix(ref RenderManager.Instance __5, out bool __state)
        {
            __state = __5.m_dirty;
        }

        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        public static void Postfix(ref NetSegment __instance, bool __state, ushort __1, NetInfo __3, NetInfo __4,
                                   ref RenderManager.Instance __5, bool __6)
        {
            if (!__state || _failed) return;
            if (__3 == null || __4 == null) return;
            if (!FenceStore.Has(__1)) return;

            try
            {
                FenceHeight.Rebuild(ref __instance, __1, __3, __4, ref __5, __6);
            }
            catch (Exception ex)
            {
                _failed = true;
                Debug.LogError("[QuayTools] Fence rebuild failed, disabled: " + ex);
            }
        }
    }

    /// <summary>
    /// NetNode.GetEndFences(ref uint): the game closes a dead end with a fence (an arc) when both sides have one.
    /// For our segments the closing fence is optional per end (see FenceSettings.CapStart / CapEnd).
    /// </summary>
    [HarmonyPatch]
    internal static class EndFencePatch
    {
        public static MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(NetNode), "GetEndFences");
        }

        public static bool Prefix(ref NetNode __instance, ref uint __0)
        {
            for (int i = 0; i < 8; i++)
            {
                ushort segment = __instance.GetSegment(i);
                if (segment == 0) continue;

                FenceSettings s;
                if (!FenceStore.TryGet(segment, out s)) return true; // not ours: vanilla behaviour

                NetManager nm = NetManager.instance;
                NetSegment[] segs = nm.m_segments.m_buffer;
                bool isStart = nm.m_nodes.m_buffer[segs[segment].m_startNode].m_position == __instance.m_position;

                NetInfo fence = segs[segment].LeftFenceInfo;
                if (fence == null) fence = segs[segment].RightFenceInfo;

                bool close = isStart ? s.CapStart : s.CapEnd;
                __0 = close && fence != null ? (uint)fence.m_prefabDataIndex : 65535u;
                return false;
            }
            return true;
        }
    }

    /// <summary>
    /// NetNode.RefreshBendFenceData(ushort node, NetInfo info, NetInfo fenceInfo, ref uint instance, ref Instance data,
    ///                              bool left1, bool left2, ushort segment1, ushort segment2, int index)
    /// Rebuilds the fence at bend nodes from our corners (so that offsets, heights and node edits are respected).
    /// </summary>
    [HarmonyPatch]
    internal static class BendFencePatch
    {
        private static bool _failed;

        public static MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(NetNode), "RefreshBendFenceData");
        }

        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        public static void Postfix(ref NetNode __instance, ushort __0, NetInfo __2, ref RenderManager.Instance __4,
                                   bool __5, bool __6, ushort __7, ushort __8, int __9)
        {
            if (_failed || __2 == null) return;
            try
            {
                FenceHeight.RebuildBend(ref __instance, __0, __2, ref __4, __5, __6, __7, __8, __9);
            }
            catch (Exception ex)
            {
                _failed = true;
                Debug.LogError("[QuayTools] Bend fence rebuild failed, disabled: " + ex);
            }
        }
    }

    /// <summary>
    /// NetNode.RefreshEndFenceData(ushort node, int index, NetInfo info, NetInfo fenceInfo, uint fences, ref Instance data)
    /// Replaces the arc that closes a dead end by a straight fence across the quay.
    /// </summary>
    [HarmonyPatch]
    internal static class EndFenceShapePatch
    {
        private static bool _failed;

        public static MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(NetNode), "RefreshEndFenceData");
        }

        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        public static void Postfix(ref NetNode __instance, ushort __0, int __1, NetInfo __2, NetInfo __3,
                                   ref RenderManager.Instance __5)
        {
            if (_failed || __2 == null || __3 == null) return;
            try
            {
                FenceHeight.RebuildEnd(ref __instance, __0, __1, __2, __3, ref __5);
            }
            catch (Exception ex)
            {
                _failed = true;
                Debug.LogError("[QuayTools] End fence rebuild failed, disabled: " + ex);
            }
        }
    }
}
