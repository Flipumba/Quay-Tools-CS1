using ColossalFramework;
using UnityEngine;

namespace QuayTools
{
    /// <summary>
    /// ToolBase.RayCast is protected static, so we need a derived class to reach it.
    /// This class is never instantiated or registered as a tool.
    /// </summary>
    internal class NetRaycaster : ToolBase
    {
        public static bool TryGetSegmentUnderCursor(out ushort segmentId)
        {
            float side;
            Vector3 hit;
            return TryGetSegmentUnderCursor(out segmentId, out side, out hit);
        }

        /// <summary>
        /// side is the game's own value (RaycastOutput.m_netSegmentSide): the vanilla fence tool
        /// treats side &lt; 0 as the LEFT fence of the segment.
        /// </summary>
        public static bool TryGetSegmentUnderCursor(out ushort segmentId, out float side, out Vector3 hitPos)
        {
            segmentId = 0;
            side = 0f;
            hitPos = Vector3.zero;

            Camera cam = Camera.main;
            if (cam == null) return false;

            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            RaycastInput input = new RaycastInput(ray, cam.farClipPlane);

            // Quays live in the Road service in vanilla; other quay assets may sit in Beautification.
            input.m_netService.m_service = ItemClass.Service.Road;
            input.m_netService.m_itemLayers = ItemClass.Layer.Default | ItemClass.Layer.MetroTunnels;
            input.m_netService2.m_service = ItemClass.Service.Beautification;
            input.m_netService2.m_itemLayers = ItemClass.Layer.Default;

            input.m_ignoreSegmentFlags = NetSegment.Flags.None;
            input.m_ignoreNodeFlags = NetNode.Flags.All; // we want segments only
            input.m_ignoreTerrain = true;

            RaycastOutput output;
            if (!RayCast(input, out output)) return false;

            segmentId = output.m_netSegment;
            side = output.m_netSegmentSide;
            hitPos = output.m_hitPos;
            return segmentId != 0;
        }
    }
}
