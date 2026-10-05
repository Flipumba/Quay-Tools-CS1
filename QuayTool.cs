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
            None = -1,        // right after the tool is activated: no tool chosen yet, only the tool column is shown
            Segment = 0,      // "Segment Settings": invert, lock, remove pedestrian path, hide default props
            AddNetwork = 1,   // network-model lines ("Network-line")
            PropLine = 2,
            Decal = 3,        // texture paths ("Texture-path")
            Templates = 4,    // save the lines of a segment as a template, apply a template to the selected segments
            Settings = 5      // options of the mod and "remove all modifications"; no segments are selected here
        }

        public const int ModeCount = 6;

        private static readonly Color HoverColor = new Color(0.10f, 0.70f, 1.00f, 0.55f);
        private static readonly Color ChainColor = new Color(0.30f, 1.00f, 0.55f, 0.55f);
        private static readonly Color SelectedColor = new Color(1.00f, 0.75f, 0.10f, 0.60f);
        private static readonly Color EditedColor = new Color(0.25f, 0.85f, 0.95f, 0.22f);
        private static readonly Color LockedColor = new Color(1.00f, 0.30f, 0.30f, 0.55f);
        internal static readonly Color StartColor = new Color(0.20f, 0.95f, 1.00f, 0.95f);
        internal static readonly Color EndColor = new Color(1.00f, 0.35f, 0.90f, 0.95f);
        private static readonly Color WaterColor = new Color(0.20f, 0.60f, 1.00f, 0.95f);

        public static QuayTool Instance { get; private set; }

        public Mode CurrentMode { get; private set; } = Mode.None;

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

        /// <summary>The line (network-line mode) or path (texture-path mode) the panel is editing; it is highlighted on the selected segments.</summary>
        public int ActiveLine;

        public IList<ushort> Selection
        {
            get { return _selected; }
        }

        /// <summary>Modes that already do something. The rest are shown disabled in the panel.</summary>
        public static bool IsImplemented(Mode mode)
        {
            return mode != Mode.None;
        }

        /// <summary>Modes in which segments are selected first and edited in the window.</summary>
        public static bool IsSelectMode(Mode mode)
        {
            return mode != Mode.None && mode != Mode.Settings;
        }

        public static string HintFor(Mode mode)
        {
            switch (mode)
            {
                case Mode.Segment: return Loc.T("hint_segment");
                case Mode.AddNetwork: return Loc.T("hint_network");
                case Mode.Decal: return Loc.T("hint_decal");
                case Mode.PropLine: return Loc.T("hint_props");
                case Mode.Templates: return Loc.T("hint_tpl");
                case Mode.Settings: return Loc.T("hint_settings");
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
            if (mode == Mode.PropLine) PropCatalog.Refresh();
            if (!IsSelectMode(mode)) ClearSelection(); // the selection is shared by all the modes that edit selected segments
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            Instance = this;
            CurrentMode = Mode.None; // every activation starts with the tool column only
            _status = string.Empty;
            if (CurrentMode == Mode.AddNetwork) FenceCatalog.Refresh();
            if (CurrentMode == Mode.Decal) DecalCatalog.Refresh();
            if (CurrentMode == Mode.PropLine) PropCatalog.Refresh();
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
            if (overUi || !IsSelectMode(CurrentMode))
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
            if (IsSelectMode(CurrentMode)) ToggleSelection();
        }

        /// <summary>
        /// Click: only this segment is selected. Shift + click: the whole connected quay. Ctrl + click: adds the segment to the
        /// selection or removes it (with Shift as well: the whole quay is added or removed).
        /// </summary>
        private void ToggleSelection()
        {
            bool ctrl = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);

            if (!ctrl)
            {
                // replace the selection (nothing changes when exactly this one is selected already)
                bool same = _selected.Count == _hoverList.Count;
                for (int i = 0; same && i < _hoverList.Count; i++)
                {
                    if (!_selected.Contains(_hoverList[i])) same = false;
                }
                if (same) return;

                _selected.Clear();
                _selected.AddRange(_hoverList);
                RaiseSelectionChanged();
                return;
            }

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

            RaiseSelectionChanged();
        }

        // ---------- actions requested by the panel ----------

        private void Report(string s)
        {
            _status = s;
        }

        internal void Say(string s)
        {
            _status = s;
        }

        private List<ushort> SelectionCopy()
        {
            return new List<ushort>(_selected);
        }

        // network-model lines

        internal void AddNetLine(NetLine template)
        {
            if (_selected.Count == 0) { _status = Loc.T("select_first"); return; }
            FenceApplier.AddNetLine(SelectionCopy(), template, Report);
        }

        internal void EditNetLine(int index, string property, Action<NetLine> apply)
        {
            if (_selected.Count == 0) return;
            FenceApplier.EditNetLine(SelectionCopy(), index, property, apply);
        }

        public void RemoveNetLine(int index)
        {
            if (_selected.Count == 0) { _status = Loc.T("select_first"); return; }
            FenceApplier.RemoveNetLine(SelectionCopy(), index, Report);
        }

        public void ClearNetLines()
        {
            if (_selected.Count == 0) { _status = Loc.T("select_first"); return; }
            FenceApplier.ClearNetLines(SelectionCopy(), Report);
        }

        // orientation lock

        /// <summary>kind: "lock" (orientation lock), "nop" (remove pedestrian path), "hp" (hide default props).</summary>
        public void SetSegmentFlag(string kind, bool on)
        {
            if (_selected.Count == 0) { _status = Loc.T("select_first"); return; }
            if (kind == "nop") FenceApplier.SetNoPeds(SelectionCopy(), on, Report);
            else if (kind == "hp") FenceApplier.SetHideProps(SelectionCopy(), on, Report);
            else FenceApplier.SetLock(SelectionCopy(), on, Report);
        }

        /// <summary>Flips the Invert flag of the selected segments (the same mechanics as the former Invert tool).</summary>
        public void FlipSelected()
        {
            if (_selected.Count == 0) { _status = Loc.T("select_first"); return; }
            SegmentFlipper.RequestFlipList(SelectionCopy(), Report);
        }

        // prop lines

        internal void AddPropEntry(PropEntry template)
        {
            if (_selected.Count == 0) { _status = Loc.T("select_first"); return; }
            FenceApplier.AddPropEntry(SelectionCopy(), template, Report);
        }

        internal void EditPropEntry(int index, string property, Action<PropEntry> apply)
        {
            if (_selected.Count == 0) return;
            FenceApplier.EditPropEntry(SelectionCopy(), index, property, apply);
        }

        public void RemovePropEntry(int index)
        {
            if (_selected.Count == 0) { _status = Loc.T("select_first"); return; }
            FenceApplier.RemovePropEntry(SelectionCopy(), index, Report);
        }

        public void ClearPropLines()
        {
            if (_selected.Count == 0) { _status = Loc.T("select_first"); return; }
            FenceApplier.ClearPropLines(SelectionCopy(), Report);
        }

        // texture paths

        internal void AddDecal(DecalSettings values)
        {
            if (_selected.Count == 0) { _status = Loc.T("select_first"); return; }
            FenceApplier.AddDecal(SelectionCopy(), values, Report);
        }

        public void RemoveDecal(int index)
        {
            if (_selected.Count == 0) { _status = Loc.T("select_first"); return; }
            FenceApplier.RemoveDecal(SelectionCopy(), index, Report);
        }

        public void ClearDecals()
        {
            if (_selected.Count == 0) { _status = Loc.T("select_first"); return; }
            FenceApplier.ClearDecals(SelectionCopy(), Report);
        }

        internal void EditDecal(int index, string property, Action<DecalSettings> apply)
        {
            if (_selected.Count == 0) return;
            FenceApplier.EditDecal(SelectionCopy(), index, property, apply);
        }

        // reset, undo, redo

        /// <summary>Back to default settings of the current mode for the selected segments.</summary>
        public void ResetSelection()
        {
            if (_selected.Count == 0) { _status = Loc.T("select_first"); return; }
            if (CurrentMode == Mode.Decal) FenceApplier.ResetDecals(SelectionCopy(), Report);
            else if (CurrentMode == Mode.AddNetwork) FenceApplier.ResetNetLines(SelectionCopy(), Report);
        }

        /// <summary>Saves the lines of the first selected segment as a template. Returns the new template or null (the status says why).</summary>
        internal QuayTemplate SaveTemplate(string name, List<QuayTemplate> existing)
        {
            if (_selected.Count == 0) { _status = Loc.T("select_first"); return null; }

            string error;
            QuayTemplate t = TemplateStore.SaveFromSegment(name, _selected[0], existing, out error);
            if (t == null) _status = Loc.T(error);
            else _status = Loc.F("tpl_saved", t.Name);
            return t;
        }

        // ---------- template picture ----------

        /// <summary>True while the picture of a template is rendered (the highlights of the tool are not drawn then).</summary>
        public static volatile bool Capturing;

        private const int ThumbW = 480, ThumbH = 270;

        /// <summary>Renders what the main camera sees (no interface, no highlights) into a png file; done(true) when it worked.</summary>
        internal void CaptureThumbnail(string path, Action<bool> done)
        {
            StartCoroutine(CaptureRoutine(path, done));
        }

        private System.Collections.IEnumerator CaptureRoutine(string path, Action<bool> done)
        {
            yield return new WaitForEndOfFrame();

            bool ok = false;
            RenderTexture previous = RenderTexture.active;
            RenderTexture rt = null;
            Texture2D tex = null;
            Camera cam = Camera.main;
            RenderTexture oldTarget = null;
            try
            {
                if (cam != null)
                {
                    Capturing = true;
                    oldTarget = cam.targetTexture;
                    rt = new RenderTexture(ThumbW, ThumbH, 24, RenderTextureFormat.ARGB32);
                    cam.targetTexture = rt;
                    cam.aspect = ThumbW / (float)ThumbH;
                    cam.Render();

                    RenderTexture.active = rt;
                    tex = new Texture2D(ThumbW, ThumbH, TextureFormat.RGB24, false);
                    tex.ReadPixels(new Rect(0f, 0f, ThumbW, ThumbH), 0, 0);
                    tex.Apply();
                    System.IO.File.WriteAllBytes(path, tex.EncodeToPNG());
                    ok = true;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[QuayTools] Could not render the template picture: " + ex);
            }
            finally
            {
                try
                {
                    if (cam != null)
                    {
                        cam.targetTexture = oldTarget;
                        cam.ResetAspect();
                    }
                }
                catch (Exception) { }
                Capturing = false;
                RenderTexture.active = previous;
                if (rt != null) Destroy(rt);
                if (tex != null) Destroy(tex);
            }

            if (done != null) done(ok);
        }

        /// <summary>Replaces the lines of every selected segment with the lines of the template (one undo step).</summary>
        internal void ApplyTemplate(QuayTemplate template)
        {
            if (_selected.Count == 0) { _status = Loc.T("select_first"); return; }

            int missing;
            QuayTemplate t = template.ResolveModels(out missing);
            Action<string> report = delegate (string s)
            {
                _status = missing > 0 ? s + " " + Loc.F("tpl_missing", missing) : s;
            };

            FenceApplier.Run(SelectionCopy(), null, delegate (ushort id)
            {
                NetLineSet nets;
                PropLine props;
                DecalSet decals;
                t.Build(TemplateStore.SegmentLength(id), out nets, out props, out decals);
                NetLineStore.Set(id, nets);
                PropLineStore.Set(id, props);
                DecalStore.Set(id, decals);
            }, "tpl_applied", report, true);
        }

        /// <summary>
        /// Removes every modification of Quay Tools from every segment of the savegame: network-lines, props-lines, texture-paths,
        /// locks, removed pedestrian paths, hidden default props (one undo step). Flipped segments stay flipped.
        /// </summary>
        internal void ClearAllModifications()
        {
            HashSet<ushort> all = new HashSet<ushort>();
            all.UnionWith(NetLineStore.Keys());
            all.UnionWith(PropLineStore.Keys());
            all.UnionWith(DecalStore.Keys());
            all.UnionWith(LockStore.Snapshot());
            all.UnionWith(PedStore.Snapshot());
            all.UnionWith(HideStore.Snapshot());
            if (all.Count == 0) { _status = Loc.T("clear_all_none"); return; }

            FenceApplier.Run(new List<ushort>(all), null, delegate (ushort id)
            {
                NetLineStore.Remove(id);
                PropLineStore.Remove(id);
                DecalStore.Remove(id);
                LockStore.SetRaw(id, -1);
                PedStore.SetBlocked(id, false);
                HideStore.SetHidden(id, false);
            }, "clear_all_done", Report, true);
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
            if (Capturing) return; // the picture of a template shows the scene only, without our highlights

            // while a slider is dragged (and the option is on) the highlights are hidden so that the result can be seen
            if (Settings.HideHighlightUi && QuayToolPanel.SliderDragging) return;

            if (Settings.MarkEdited)
            {
                // faint highlight of every segment edited by the mod
                List<ushort> edited = EditedMarkers.Edited;
                for (int i = 0; i < edited.Count; i++)
                {
                    if (!_selected.Contains(edited[i])) QuayGeometry.DrawModel(cameraInfo, edited[i], EditedColor);
                }
            }

            if (CurrentMode == Mode.Segment)
            {
                // every locked / pedestrian-free / props-free segment is marked red
                List<ushort> marked = new List<ushort>(LockStore.Snapshot());
                List<ushort> more = PedStore.Snapshot();
                for (int i = 0; i < more.Count; i++) if (!marked.Contains(more[i])) marked.Add(more[i]);
                more = HideStore.Snapshot();
                for (int i = 0; i < more.Count; i++) if (!marked.Contains(more[i])) marked.Add(more[i]);
                for (int i = 0; i < marked.Count; i++)
                {
                    if (!_selected.Contains(marked[i])) QuayGeometry.DrawModel(cameraInfo, marked[i], LockedColor);
                }
            }

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

        private void DrawSelected(RenderManager.CameraInfo cameraInfo, ushort segmentId, bool lineMode)
        {
            QuayGeometry.DrawModel(cameraInfo, segmentId, SelectedColor);
            if (!lineMode) return; // other modes: only the selection highlight

            // thin line along the network-model line that is being edited
            NetLineSet set;
            if (NetLineStore.TryGet(segmentId, out set) && ActiveLine >= 0 && ActiveLine < set.Lines.Count)
            {
                QuayFrame f = QuayGeometry.GetFrame(segmentId);
                float y = set.Lines[ActiveLine].Lateral * FenceStore.Unit * (f.WaterIsRight ? 1f : -1f); // right of start->end positive
                QuayGeometry.DrawStripe(cameraInfo, segmentId, y, 0.7f, WaterColor);
            }

            // rings on the two ends: cyan = start of the segment, magenta = end (for the "close" options)
            NetManager nm = NetManager.instance;
            NetSegment seg = nm.m_segments.m_buffer[segmentId];
            RenderManager.instance.OverlayEffect.DrawCircle(cameraInfo, StartColor, nm.m_nodes.m_buffer[seg.m_startNode].m_position, 5f, -1f, 1280f, false, true);
            RenderManager.instance.OverlayEffect.DrawCircle(cameraInfo, EndColor, nm.m_nodes.m_buffer[seg.m_endNode].m_position, 5f, -1f, 1280f, false, true);
        }
    }
}
