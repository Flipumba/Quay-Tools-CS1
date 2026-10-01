using UnityEngine;

namespace QuayTools
{
    /// <summary>
    /// Shared constants of the sliders (metres per unit, limits) and the helper that reads the real corners of a
    /// segment. (The old per-segment fence settings of v0.4.x were replaced by NetLineStore.)
    /// </summary>
    internal static class FenceStore
    {
        /// <summary>Metres per slider unit (slider range is +-MaxUnits).</summary>
        public const float Unit = 0.1f;

        /// <summary>Slider/field limit in units: 500 units = 50 m.</summary>
        public const int MaxUnits = 500;

        /// <summary>Limit of the end trims in units: 500 units = 50 m.</summary>
        public const int MaxShift = 500;

        /// <summary>Corner of a segment end as computed by the game and other mods (no fence offset).</summary>
        public static void GetRawCorner(ushort segment, bool start, bool left, out Vector3 pos, out Vector3 dir, out bool smooth)
        {
            NetSegment[] segs = NetManager.instance.m_segments.m_buffer;
            segs[segment].CalculateCorner(segment, true, start, left, out pos, out dir, out smooth, 0f);
        }
    }
}
