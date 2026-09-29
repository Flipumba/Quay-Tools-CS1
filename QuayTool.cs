using System.Collections.Generic;
using ColossalFramework.Math;
using ColossalFramework.UI;
using UnityEngine;

namespace QuayTools
{
    /// <summary>
    /// One tool with several modes. The player picks the mode in QuayToolPanel.
    /// Hovered quay segments are highlighted; a left click applies the current mode.
    /// </summary>
    public class QuayTool : ToolBase
    {
        public enum Mode
        {
            Invert = 0,
            RemovePedestrian = 1,
            AddNetwork = 2
        }

        private static readonly Color SingleColor = new Color(0.10f, 0.70f, 1.00f, 0.65f);
        private static readonly Color ChainColor = new Color(0.30f, 1.00f, 0.55f, 0.65f);
        private static readonly Color SideColor = new Color(1.00f, 0.75f, 0.10f, 0.85f);
        private static readonly Color FaintColor = new Color(0.10f, 0.70f, 1.00f, 0.25f);

        // The vanilla fence tool calls the cursor side "left" when RaycastOutput.m_netSegmentSide < 0.
        // We assume that the LEFT fence is on the geometric left of the start->end direction.
        // If the highlighted stripe shows up on the opposite side of the cursor in game, set this to false.
        internal static readonly bool LeftIsGeometricLeft = true;

        public static QuayTool Instance { get; private set; }

        public Mode CurrentMode { get; private set; }

        /// <summary>Network model that AddNetwork mode places (ignored while RemoveFence is true).</summary>
        public NetInfo SelectedFence;

        /// <summary>When true, AddNetwork mode removes fences instead of placing them.</summary>
        public bool RemoveFence;

        private bool _ready;
        private volatile string _status = string.Empty;

        private ushort _hoverSegment;
        private bool _hoverIsChain;
        private readonly List<ushort> _hoverList = new List<ushort>();
        private readonly List<bool> _hoverSides = new List<bool>();

        private ushort _cacheSegment;
        private bool _cacheLeft;
        private bool _cacheChain;
        private Mode _cacheMode;

        /// <summary>Modes that already do something. The rest are shown disabled in the panel.</summary>
        public static bool IsImplemented(Mode mode)
        {
            return mode == Mode.Invert || mode == Mode.AddNetwork;
        }

        public static string HintFor(Mode mode)
        {
            switch (mode)
            {
                case Mode.Invert:
                    return "Click a quay to flip it. Hold Shift for the whole connected quay. Right click to exit.";
                case Mode.RemovePedestrian:
                    return "Coming soon.";
                case Mode.AddNetwork:
                    return "Pick a model, then click the side of a quay. Shift: whole connected quay. Right click to exit.";
            }
            return string.Empty;
        }

        internal void MarkReady()
        {
            _ready = true;
            Instance = this;
        }

        public void SetMode(Mode mode)
        {
            if (!IsImplemented(mode)) return;
            CurrentMode = mode;
            _status = string.Empty;
            _cacheSegment = 0;
            if (mode == Mode.AddNetwork) FenceCatalog.Refresh();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            Instance = this;
            if (_ready) QuayToolPanel.ShowPanel();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            ClearHover();
            if (_ready) QuayToolPanel.HidePanel();
        }

        protected override void OnToolUpdate()
        {
            base.OnToolUpdate();

            bool overUi = UIView.IsInsideUI();

            if (!overUi && Input.GetMouseButtonDown(1))
            {
                ToolsModifierControl.SetTool<DefaultTool>();
                return;
            }

            UpdateHover(overUi);

            QuayToolPanel panel = QuayToolPanel.Instance;
            if (panel != null) panel.SetStatus(_status);

            if (_hoverSegment != 0 && Input.GetMouseButtonDown(0))
            {
                ApplyClick();
            }
        }

        private void ClearHover()
        {
            _hoverSegment = 0;
            _hoverList.Clear();
            _hoverSides.Clear();
            _cacheSegment = 0;
        }

        private void UpdateHover(bool overUi)
        {
            if (overUi)
            {
                ClearHover();
                return;
            }

            ushort id;
            float side;
            Vector3 hit;
            if (!NetRaycaster.TryGetSegmentUnderCursor(out id, out side, out hit))
            {
                ClearHover();
                return;
            }

            NetInfo info = NetManager.instance.m_segments.m_buffer[id].Info;
            if (!SegmentFlipper.IsQuay(info))
            {
                ClearHover();
                return;
            }

            bool chain = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            bool left = CurrentMode == Mode.AddNetwork && side < 0f;

            if (_hoverSegment != 0 && id == _cacheSegment && left == _cacheLeft &&
                chain == _cacheChain && CurrentMode == _cacheMode)
            {
                return; // nothing changed, keep the cached highlight
            }

            _hoverSegment = id;
            _hoverIsChain = chain;
            _hoverList.Clear();
            _hoverSides.Clear();

            if (chain)
            {
                Dictionary<ushort, bool> orientation = new Dictionary<ushort, bool>();
                List<ushort> segments = SegmentFlipper.CollectChainOriented(id, orientation);
                for (int i = 0; i < segments.Count; i++)
                {
                    _hoverList.Add(segments[i]);
                    // Segments running the other way have their left/right swapped.
                    _hoverSides.Add(orientation[segments[i]] ? left : !left);
                }
            }
            else
            {
                _hoverList.Add(id);
                _hoverSides.Add(left);
            }

            _cacheSegment = id;
            _cacheLeft = left;
            _cacheChain = chain;
            _cacheMode = CurrentMode;
        }

        private void ApplyClick()
        {
            switch (CurrentMode)
            {
                case Mode.Invert:
                    SegmentFlipper.RequestFlip(_hoverSegment, _hoverIsChain, delegate (string s) { _status = s; });
                    _cacheSegment = 0;
                    break;

                case Mode.AddNetwork:
                    if (!RemoveFence && SelectedFence == null)
                    {
                        _status = "Pick a network model first";
                        break;
                    }

                    NetInfo fence = RemoveFence ? null : SelectedFence;
                    FenceApplier.RequestSetFence(
                        new List<ushort>(_hoverList), new List<bool>(_hoverSides), fence,
                        delegate (string s) { _status = s; });
                    _cacheSegment = 0;
                    break;
            }
        }

        public override void RenderOverlay(RenderManager.CameraInfo cameraInfo)
        {
            base.RenderOverlay(cameraInfo);

            if (_hoverList.Count == 0) return;

            for (int i = 0; i < _hoverList.Count; i++)
            {
                if (CurrentMode == Mode.AddNetwork)
                {
                    DrawSegment(cameraInfo, _hoverList[i], FaintColor, 0f);
                    DrawSide(cameraInfo, _hoverList[i], _hoverSides[i], SideColor);
                }
                else
                {
                    DrawSegment(cameraInfo, _hoverList[i], _hoverIsChain ? ChainColor : SingleColor, 0f);
                }
            }
        }

        private static void DrawSegment(RenderManager.CameraInfo cameraInfo, ushort segmentId, Color color, float sizeOverride)
        {
            NetManager nm = NetManager.instance;
            NetSegment seg = nm.m_segments.m_buffer[segmentId];
            if ((seg.m_flags & NetSegment.Flags.Created) == NetSegment.Flags.None) return;

            Vector3 a = nm.m_nodes.m_buffer[seg.m_startNode].m_position;
            Vector3 d = nm.m_nodes.m_buffer[seg.m_endNode].m_position;
            Vector3 b, c;
            NetSegment.CalculateMiddlePoints(a, seg.m_startDirection, d, seg.m_endDirection, false, false, out b, out c);

            Bezier3 bezier = new Bezier3();
            bezier.a = a;
            bezier.b = b;
            bezier.c = c;
            bezier.d = d;

            float size = sizeOverride > 0f ? sizeOverride : Mathf.Max(seg.Info.m_halfWidth * 2f, 4f);
            RenderManager.instance.OverlayEffect.DrawBezier(
                cameraInfo, color, bezier, size, -100000f, -100000f, -1f, 1280f, false, true);
        }

        /// <summary>Draws a narrow stripe along one edge of the segment: where the fence will stand.</summary>
        private static void DrawSide(RenderManager.CameraInfo cameraInfo, ushort segmentId, bool left, Color color)
        {
            NetManager nm = NetManager.instance;
            NetSegment seg = nm.m_segments.m_buffer[segmentId];
            if ((seg.m_flags & NetSegment.Flags.Created) == NetSegment.Flags.None) return;

            Vector3 a = nm.m_nodes.m_buffer[seg.m_startNode].m_position;
            Vector3 d = nm.m_nodes.m_buffer[seg.m_endNode].m_position;

            // Unity: Cross(direction, up) points to the LEFT of that direction.
            Vector3 leftAtStart = Vector3.Cross(seg.m_startDirection, Vector3.up).normalized;
            Vector3 leftAtEnd = Vector3.Cross(-seg.m_endDirection, Vector3.up).normalized;

            float offset = seg.Info.m_halfWidth * ((left == LeftIsGeometricLeft) ? 1f : -1f);
            Vector3 a2 = a + leftAtStart * offset;
            Vector3 d2 = d + leftAtEnd * offset;

            Vector3 b, c;
            NetSegment.CalculateMiddlePoints(a2, seg.m_startDirection, d2, seg.m_endDirection, false, false, out b, out c);

            Bezier3 bezier = new Bezier3();
            bezier.a = a2;
            bezier.b = b;
            bezier.c = c;
            bezier.d = d2;

            RenderManager.instance.OverlayEffect.DrawBezier(
                cameraInfo, color, bezier, 3f, -100000f, -100000f, -1f, 1280f, false, true);
        }
    }
}
