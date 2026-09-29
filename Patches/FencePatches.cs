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
    /// NetNode.GetEndFences(ref uint): the game closes the fences of both sides across a dead end.
    /// For quay segments with "do not join" checked we report "no end fence" (65535).
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

                if (FenceStore.NoConnect(segment))
                {
                    __0 = 65535u;
                    return false;
                }
                break;
            }
            return true;
        }
    }
}
