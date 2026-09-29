using System;
using System.Collections.Generic;
using ColossalFramework.UI;
using UnityEngine;

namespace QuayTools
{
    /// <summary>
    /// One tool with several modes, chosen in QuayToolPanel.
    /// Invert: click a quay to flip it. AddNetwork: select segments, then set fence models in the panel.
    /// Highlights follow the visible quay model, not the whole (much wider) network.
    /// </summary>
    public class QuayTool : ToolBase
    {
        public enum Mode
        {
            Invert = 0,
            RemovePedestrian = 1,
            AddNetwork = 2
        }

        private static readonly Color HoverColor = new Color(0.10f, 0.70f, 1.00f, 0.55f);
        private static readonly Color ChainColor = new Color(0.30f, 1.00f, 0.55f, 0.55f);
        private static readonly Color SelectedColor = new Color(1.00f, 0.75f, 0.10f, 0.60f);
        private static readonly Color LandColor = new Color(0.30f, 1.00f, 0.45f, 0.95f);
        private static readonly Color WaterColor = new Color(0.20f, 0.60f, 1.00f, 0.95f);

        public static QuayTool Instance { get; private set; }

        public Mode CurrentMode { get; private set; }

        /// <summary>Raised when the selection changes; the panel reloads its controls.</summary>
        public event Action SelectionChanged;

        private bool _ready;
        private volatile string _status = string.Empty;

        private readonly List<ushort> _selected = new List<ushort>();
        private ushort _hoverSegment;
        private bool _hoverIsChain;
        private readonly List<ushort> _hoverList = new List<ushort>();

        private ushort _cacheSegment;
        private bool _cacheChain;

        public IList<ushort> Selection
        {
            get { return _selected; }
        }

        /// <summary>Modes that already do something. The rest are shown disabled in the panel.</summary>
        public static bool IsImplemented(Mode mode)
        {
            return mode == Mode.Invert || mode == Mode.AddNetwork;
        }

        public static string HintFor(Mode mode)
        {
            switch (mode)
            {
                case Mode.Invert: return Loc.T("hint_invert");
                case Mode.AddNetwork: return Loc.T("hint_network");
            }
            return Loc.T("hint_soon");
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
            else ClearSelection();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            Instance = this;
            if (CurrentMode == Mode.AddNetwork) FenceCatalog.Refresh();
            if (_ready) QuayToolPanel.ShowPanel();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            ClearHover();
            ClearSelection();
            if (_ready) QuayToolPanel.HidePanel();
        }

        // ---------- selection ----------

        public void ClearSelection()
        {
            if (_selected.Count == 0) return;
            _selected.Clear();
            RaiseSelectionChanged();
        }

        private void RaiseSelectionChanged()
        {
            Action handler = SelectionChanged;
            if (handler != null) handler();
        }

        private void PruneSelection()
        {
            NetSegment[] segs = NetManager.instance.m_segments.m_buffer;
            bool changed = false;
            for (int i = _selected.Count - 1; i >= 0; i--)
            {
                if ((segs[_selected[i]].m_flags & NetSegment.Flags.Created) == NetSegment.Flags.None)
                {
                    _selected.RemoveAt(i);
                    changed = true;
                }
            }
            if (changed) RaiseSelectionChanged();
        }

        // ---------- per-frame ----------

        protected override void OnToolUpdate()
        {
            base.OnToolUpdate();

            bool overUi = UIView.IsInsideUI();

            if (!overUi && Input.GetMouseButtonDown(1))
            {
                if (CurrentMode == Mode.AddNetwork && _selected.Count > 0)
                {
                    ClearSelection();
                }
                else
                {
                    ToolsModifierControl.SetTool<DefaultTool>();
                }
                return;
            }

            PruneSelection();
            UpdateHover(overUi);

            QuayToolPanel panel = QuayToolPanel.Instance;
            if (panel != null) panel.SetStatus(_status);

            if (_hoverSegment != 0 && !overUi && Input.GetMouseButtonDown(0))
            {
                ApplyClick();
            }
        }

        private void ClearHover()
        {
            _hoverSegment = 0;
            _hoverList.Clear();
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
            if (!NetRaycaster.TryGetSegmentUnderCursor(out id))
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
            if (_hoverSegment != 0 && id == _cacheSegment && chain == _cacheChain) return;

            _hoverSegment = id;
            _hoverIsChain = chain;
            _hoverList.Clear();

            if (chain) _hoverList.AddRange(SegmentFlipper.CollectChain(id));
            else _hoverList.Add(id);

            _cacheSegment = id;
            _cacheChain = chain;
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
                    ToggleSelection();
                    break;
            }
        }

        private void ToggleSelection()
        {
            if (_hoverIsChain)
            {
                bool allSelected = true;
                for (int i = 0; i < _hoverList.Count; i++)
                {
                    if (!_selected.Contains(_hoverList[i])) { allSelected = false; break; }
                }

                for (int i = 0; i < _hoverList.Count; i++)
                {
                    ushort id = _hoverList[i];
                    if (allSelected) _selected.Remove(id);
                    else if (!_selected.Contains(id)) _selected.Add(id);
                }
            }
            else
            {
                if (!_selected.Remove(_hoverSegment)) _selected.Add(_hoverSegment);
            }

            RaiseSelectionChanged();
        }

        // ---------- actions requested by the panel ----------

        private void Report(string s)
        {
            _status = s;
        }

        public void ApplyModel(bool landSide, NetInfo model)
        {
            if (_selected.Count == 0) { _status = Loc.T("select_first"); return; }
            FenceApplier.SetModel(new List<ushort>(_selected), landSide, model, Report);
        }

        public void ApplyOffset(bool landSide, bool horizontal, int value)
        {
            if (_selected.Count == 0) return;

            for (int i = 0; i < _selected.Count; i++)
            {
                FenceSettings s = FenceStore.GetOrCreate(_selected[i]);
                if (landSide)
                {
                    if (horizontal) s.LandH = value; else s.LandV = value;
                }
                else
                {
                    if (horizontal) s.WaterH = value; else s.WaterV = value;
                }
            }
            FenceApplier.Refresh(new List<ushort>(_selected));
        }

        public void ApplyNoConnect(bool noConnect)
        {
            if (_selected.Count == 0) return;

            for (int i = 0; i < _selected.Count; i++)
            {
                FenceStore.GetOrCreate(_selected[i]).NoConnect = noConnect;
            }
            FenceApplier.Refresh(new List<ushort>(_selected));
        }

        public void RemoveModels()
        {
            if (_selected.Count == 0) { _status = Loc.T("select_first"); return; }
            FenceApplier.ClearModels(new List<ushort>(_selected), Report);
        }

        // ---------- drawing ----------

        public override void RenderOverlay(RenderManager.CameraInfo cameraInfo)
        {
            base.RenderOverlay(cameraInfo);

            if (CurrentMode == Mode.AddNetwork)
            {
                for (int i = 0; i < _selected.Count; i++)
                {
                    DrawSelected(cameraInfo, _selected[i]);
                }
            }

            if (_hoverList.Count == 0) return;

            Color hover = _hoverIsChain ? ChainColor : HoverColor;
            for (int i = 0; i < _hoverList.Count; i++)
            {
                if (CurrentMode == Mode.AddNetwork && _selected.Contains(_hoverList[i])) continue;
                QuayGeometry.DrawModel(cameraInfo, _hoverList[i], hover);
            }
        }

        private static void DrawSelected(RenderManager.CameraInfo cameraInfo, ushort segmentId)
        {
            QuayGeometry.DrawModel(cameraInfo, segmentId, SelectedColor);

            // thin lines on the two model edges: green = land side (model 1), blue = water side (model 2)
            QuayFrame f = QuayGeometry.GetFrame(segmentId);
            QuayGeometry.DrawStripe(cameraInfo, segmentId, f.LandEdge, 0.7f, LandColor);
            QuayGeometry.DrawStripe(cameraInfo, segmentId, f.WaterEdge, 0.7f, WaterColor);
        }
    }
}
