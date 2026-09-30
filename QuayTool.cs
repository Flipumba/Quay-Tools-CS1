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
            AddNetwork = 2,
            Decal = 3
        }

        private static readonly Color HoverColor = new Color(0.10f, 0.70f, 1.00f, 0.55f);
        private static readonly Color ChainColor = new Color(0.30f, 1.00f, 0.55f, 0.55f);
        private static readonly Color SelectedColor = new Color(1.00f, 0.75f, 0.10f, 0.60f);
        private static readonly Color LandColor = new Color(0.30f, 1.00f, 0.45f, 0.95f);
        internal static readonly Color StartColor = new Color(0.20f, 0.95f, 1.00f, 0.95f);
        internal static readonly Color EndColor = new Color(1.00f, 0.35f, 0.90f, 0.95f);
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
            return mode == Mode.Invert || mode == Mode.AddNetwork || mode == Mode.Decal;
        }

        /// <summary>Modes in which segments are selected first and edited in the window.</summary>
        public static bool IsSelectMode(Mode mode)
        {
            return mode == Mode.AddNetwork || mode == Mode.Decal;
        }

        public static string HintFor(Mode mode)
        {
            switch (mode)
            {
                case Mode.Invert: return Loc.T("hint_invert");
                case Mode.AddNetwork: return Loc.T("hint_network");
                case Mode.Decal: return Loc.T("hint_decal");
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
            if (mode == Mode.Decal) DecalCatalog.Refresh();
            if (!IsSelectMode(mode)) ClearSelection(); // the selection is shared by the network and decal modes
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            Instance = this;
            if (CurrentMode == Mode.AddNetwork) FenceCatalog.Refresh();
            if (CurrentMode == Mode.Decal) DecalCatalog.Refresh();
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
                if (IsSelectMode(CurrentMode) && _selected.Count > 0)
                {
                    ClearSelection();
                }
                else
                {
                    ToolsModifierControl.SetTool<DefaultTool>();
                }
                return;
            }

            HandleUndoKeys();
            PruneSelection();
            UpdateHover(overUi);

            QuayToolPanel panel = QuayToolPanel.Instance;
            if (panel != null)
            {
                panel.SetStatus(_status);
                panel.SyncHistory();
            }

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
                case Mode.Decal:
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

        private List<ushort> SelectionCopy()
        {
            return new List<ushort>(_selected);
        }

        public void ApplyModel(bool landSide, NetInfo model)
        {
            if (_selected.Count == 0) { _status = Loc.T("select_first"); return; }
            FenceApplier.SetModel(SelectionCopy(), landSide, model, Report);
        }

        public void ApplyOffset(bool landSide, bool horizontal, int value)
        {
            if (_selected.Count == 0) return;
            FenceApplier.SetOffset(SelectionCopy(), landSide, horizontal, value);
        }

        public void ApplyCap(bool atStart, bool value)
        {
            if (_selected.Count == 0) return;
            FenceApplier.SetCap(SelectionCopy(), atStart, value);
        }

        public void RemoveModels()
        {
            if (_selected.Count == 0) { _status = Loc.T("select_first"); return; }
            FenceApplier.ClearModels(SelectionCopy(), Report);
        }

        // decal paths

        internal void AddDecal(DecalSettings values)
        {
            if (_selected.Count == 0) { _status = Loc.T("select_first"); return; }
            FenceApplier.AddDecal(SelectionCopy(), values, Report);
        }

        public void RemoveDecal()
        {
            if (_selected.Count == 0) { _status = Loc.T("select_first"); return; }
            FenceApplier.RemoveDecal(SelectionCopy(), Report);
        }

        internal void EditDecal(string property, Action<DecalSettings> apply)
        {
            if (_selected.Count == 0) return;
            FenceApplier.EditDecal(SelectionCopy(), property, apply);
        }

        // reset, undo, redo

        /// <summary>Back to default settings of the current mode for the selected segments.</summary>
        public void ResetSelection()
        {
            if (_selected.Count == 0) { _status = Loc.T("select_first"); return; }
            if (CurrentMode == Mode.Decal) FenceApplier.ResetDecals(SelectionCopy(), Report);
            else FenceApplier.ResetFences(SelectionCopy(), Report);
        }

        public void Undo()
        {
            History.Undo(Report);
        }

        public void Redo()
        {
            History.Redo(Report);
        }

        /// <summary>Ctrl+Z = undo, Ctrl+Y or Ctrl+Shift+Z = redo, only while this tool is active and no text field has focus.</summary>
        private void HandleUndoKeys()
        {
            if (!Settings.UndoHotkeysEnabled) return;
            if (!(Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))) return;

            QuayToolPanel panel = QuayToolPanel.Instance;
            if (panel != null && panel.IsTyping) return;

            bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            if (Input.GetKeyDown(KeyCode.Z))
            {
                if (shift) Redo(); else Undo();
            }
            else if (Input.GetKeyDown(KeyCode.Y))
            {
                Redo();
            }
        }

        // ---------- drawing ----------

        public override void RenderOverlay(RenderManager.CameraInfo cameraInfo)
        {
            base.RenderOverlay(cameraInfo);

            if (IsSelectMode(CurrentMode))
            {
                for (int i = 0; i < _selected.Count; i++)
                {
                    DrawSelected(cameraInfo, _selected[i], CurrentMode == Mode.AddNetwork);
                }
            }

            if (_hoverList.Count == 0) return;

            Color hover = _hoverIsChain ? ChainColor : HoverColor;
            for (int i = 0; i < _hoverList.Count; i++)
            {
                if (IsSelectMode(CurrentMode) && _selected.Contains(_hoverList[i])) continue;
                QuayGeometry.DrawModel(cameraInfo, _hoverList[i], hover);
            }
        }

        private static void DrawSelected(RenderManager.CameraInfo cameraInfo, ushort segmentId, bool fenceMode)
        {
            QuayGeometry.DrawModel(cameraInfo, segmentId, SelectedColor);
            if (!fenceMode) return; // decal mode: only the selection highlight

            // thin lines on the two model edges: green = land side (model 1), blue = water side (model 2)
            QuayFrame f = QuayGeometry.GetFrame(segmentId);
            QuayGeometry.DrawStripe(cameraInfo, segmentId, f.LandEdge, 0.7f, LandColor);
            QuayGeometry.DrawStripe(cameraInfo, segmentId, f.WaterEdge, 0.7f, WaterColor);

            // rings on the two ends: cyan = start of the segment, magenta = end (for the "close fence" options)
            NetManager nm = NetManager.instance;
            NetSegment seg = nm.m_segments.m_buffer[segmentId];
            RenderManager.instance.OverlayEffect.DrawCircle(cameraInfo, StartColor, nm.m_nodes.m_buffer[seg.m_startNode].m_position, 5f, -1f, 1280f, false, true);
            RenderManager.instance.OverlayEffect.DrawCircle(cameraInfo, EndColor, nm.m_nodes.m_buffer[seg.m_endNode].m_position, 5f, -1f, 1280f, false, true);
        }
    }
}
