using System;
using System.Collections.Generic;
using ColossalFramework.UI;
using UnityEngine;

namespace QuayTools
{
    /// <summary>
    /// Floating window. Top: tool (mode) buttons. In "network model" mode the window grows downwards:
    /// number of selected segments, two model blocks (land side / water side) each with a drop-down list
    /// of models with previews and two offset sliders, the "do not join" toggle and a remove button.
    /// </summary>
    public class QuayToolPanel : UIPanel
    {
        private const float PanelWidth = 330f;
        private const float ButtonHeight = 40f;
        private const float ContentTop = 304f;
        private const float RowHeight = 36f;
        private const int MaxPopupRows = 8;

        public static QuayToolPanel Instance { get; private set; }

        private static readonly string[] TitleKeys = { "mode_invert", "mode_nopeds", "mode_network", "mode_decal" };
        private static readonly string[] IconFiles = { "Invert.png", "NoPedestrian.png", "Network.png", "Decal.png" };

        /// <summary>One row of a drop-down list (Tag: NetInfo for fence models, DecalEntry for decals).</summary>
        private class PickItem
        {
            public string Title;
            public UITextureAtlas Atlas;
            public string Thumb;
            public object Tag;
        }

        private class PickerUi
        {
            public bool Land;
            public UIComponent Parent;
            public string EmptyText;
            public Func<List<PickItem>> GetItems;
            public Action<PickItem> OnPicked; // null item = the "empty" row
            public UILabel Title;
            public UIButton Header;
            public UISprite Icon;
            public UILabel Name;
            public UISlider H, V;
            public UITextField HValue, VValue;
            public UIScrollablePanel Popup;
            public UIScrollbar Bar;
        }

        private UIButton[] _modeButtons;
        private UILabel _hint;
        private UILabel _status;
        private bool _built;
        private bool _loading;

        private UIPanel _add;
        private float _addHeight;
        private UILabel _selectionLabel;
        private PickerUi _landUi;
        private PickerUi _waterUi;
        private UIButton _capStartButton, _capEndButton;
        private UIButton _removeButton;
        private bool _capStart, _capEnd;
        private PickerUi _openPopup;

        // decal path section
        private UIPanel _decal;
        private float _decalHeight;
        private UILabel _decalSel, _decalState;
        private UISlider _dWidth, _dScale, _dLateral, _dLift;
        private UITextField _dWidthV, _dScaleV, _dLateralV, _dLiftV;
        private PickerUi _decalUi;
        private UIButton[] _dColors;
        private UIButton _dAdd, _dRemove;
        private readonly DecalSettings _brush = new DecalSettings(); // values used when a path is added; edits apply to existing paths

        // bottom bar (selection modes)
        private UIPanel _bar;
        private UIButton _undoBtn, _redoBtn, _resetBtn;
        private int _historyVersion = -1;
        private int _restoreVersion;

        // ---------- lifetime ----------

        public static void ShowPanel()
        {
            if (Instance == null)
            {
                UIView view = UIView.GetAView();
                if (view == null) return;
                Instance = view.AddUIComponent(typeof(QuayToolPanel)) as QuayToolPanel;
            }
            if (Instance != null)
            {
                Instance.Show();
                Instance.Refresh();
            }
        }

        public static void HidePanel()
        {
            if (Instance != null)
            {
                Instance.ClosePopup();
                Instance.Hide();
            }
        }

        public static void DestroyPanel()
        {
            if (Instance != null)
            {
                QuayTool tool = QuayTool.Instance;
                if (tool != null) tool.SelectionChanged -= Instance.OnSelectionChanged;
                Destroy(Instance.gameObject);
                Instance = null;
            }
        }

        public override void Awake()
        {
            base.Awake();
            width = PanelWidth;
            height = ContentTop + 30f;
            backgroundSprite = "MenuPanel2";
            isInteractive = true;
            canFocus = true;

            Vector2 res = UIView.GetAView().GetScreenResolution();
            absolutePosition = new Vector3(Mathf.Max(20f, res.x - PanelWidth - 20f), 120f);
        }

        public override void Start()
        {
            base.Start();
            Build();

            QuayTool tool = QuayTool.Instance;
            if (tool != null) tool.SelectionChanged += OnSelectionChanged;

            Refresh();
        }

        // ---------- small UI helpers ----------

        private static void StyleButton(UIButton button)
        {
            button.normalBgSprite = "ButtonMenu";
            button.hoveredBgSprite = "ButtonMenuHovered";
            button.pressedBgSprite = "ButtonMenuPressed";
            button.focusedBgSprite = "ButtonMenuFocused";
            button.disabledBgSprite = "ButtonMenuDisabled";
        }

        private static UILabel MakeLabel(UIComponent parent, string text, float x, float y, float scale)
        {
            UILabel label = parent.AddUIComponent<UILabel>();
            label.text = text;
            label.textScale = scale;
            label.relativePosition = new Vector3(x, y);
            label.isInteractive = false;
            return label;
        }

        private static UISlider MakeSlider(UIComponent parent, float x, float y, float width, float min, float max, float reset)
        {
            UISlider slider = parent.AddUIComponent<UISlider>();
            slider.size = new Vector2(width, 16f);
            slider.relativePosition = new Vector3(x, y);
            slider.minValue = min;
            slider.maxValue = max;
            slider.stepSize = 1f;
            slider.value = reset;

            UISlicedSprite track = slider.AddUIComponent<UISlicedSprite>();
            track.spriteName = "ScrollbarTrack";
            track.size = new Vector2(width, 8f);
            track.relativePosition = new Vector3(0f, 4f);
            track.isInteractive = false;

            UISlicedSprite thumb = slider.AddUIComponent<UISlicedSprite>();
            thumb.spriteName = "ScrollbarThumb";
            thumb.size = new Vector2(14f, 16f);
            thumb.relativePosition = Vector3.zero;
            slider.thumbObject = thumb;

            slider.tooltip = Loc.IsRussian ? "Двойной клик: сбросить значение" : "Double click: reset the value";
            slider.eventDoubleClick += delegate (UIComponent c, UIMouseEventParameter p) { slider.value = reset; };
            return slider;
        }

        private static UITextField MakeField(UIComponent parent, float x, float y, Action<float> onMeters, float limitMeters)
        {
            UITextField f = parent.AddUIComponent<UITextField>();
            f.atlas = UIView.GetAView().defaultAtlas;
            f.normalBgSprite = "TextFieldPanel";
            f.hoveredBgSprite = "TextFieldPanelHovered";
            f.focusedBgSprite = "TextFieldPanel";
            f.selectionSprite = "EmptySprite";
            f.builtinKeyNavigation = true;
            f.isInteractive = true;
            f.readOnly = false;
            f.horizontalAlignment = UIHorizontalAlignment.Right;
            f.verticalAlignment = UIVerticalAlignment.Middle;
            f.textScale = 0.72f;
            f.padding = new RectOffset(4, 4, 3, 0);
            f.size = new Vector2(64f, 18f);
            f.relativePosition = new Vector3(x, y);
            f.text = "0.0";
            f.tooltip = Loc.F("typehint", limitMeters);

            UITextField captured = f;
            f.eventTextSubmitted += delegate (UIComponent c, string s) { CommitField(captured, s, onMeters); };
            f.eventLostFocus += delegate (UIComponent c, UIFocusEventParameter e) { CommitField(captured, captured.text, onMeters); };
            return f;
        }

        private static void CommitField(UITextField f, string s, Action<float> onMeters)
        {
            float v;
            string clean = (s ?? string.Empty).Trim().Replace(',', '.');
            StringBuilderClean(ref clean);
            if (!float.TryParse(clean, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out v))
            {
                onMeters(float.NaN); // restore the old text
                return;
            }
            onMeters(v);
        }

        private static void StringBuilderClean(ref string s)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            for (int i = 0; i < s.Length; i++)
            {
                char ch = s[i];
                if (char.IsDigit(ch) || ch == '.' || (ch == '-' && sb.Length == 0)) sb.Append(ch);
            }
            s = sb.ToString();
        }

        private static string FormatOffset(float sliderValue)
        {
            return (sliderValue * FenceStore.Unit).ToString("0.0", System.Globalization.CultureInfo.InvariantCulture);
        }

        // ---------- building ----------

        private void Build()
        {
            if (_built) return;
            _built = true;

            UIDragHandle drag = AddUIComponent<UIDragHandle>();
            drag.width = width;
            drag.height = 34f;
            drag.relativePosition = Vector3.zero;
            drag.target = this;

            MakeLabel(this, "Quay Tools", 12f, 9f, 1.0f);

            _modeButtons = new UIButton[4];
            for (int i = 0; i < 4; i++)
            {
                QuayTool.Mode mode = (QuayTool.Mode)i;
                UIButton button = AddUIComponent<UIButton>();
                button.width = PanelWidth - 20f;
                button.height = ButtonHeight;
                button.relativePosition = new Vector3(10f, 38f + i * (ButtonHeight + 4f));
                StyleButton(button);
                button.text = Loc.T(TitleKeys[i]);
                button.textScale = 0.85f;
                button.textHorizontalAlignment = UIHorizontalAlignment.Left;
                button.textVerticalAlignment = UIVerticalAlignment.Middle;
                button.textPadding = new RectOffset(48, 0, 0, 0);
                button.isEnabled = QuayTool.IsImplemented(mode);
                button.tooltip = button.isEnabled ? Loc.T(TitleKeys[i]) : Loc.T(TitleKeys[i]) + Loc.T("soon");

                Texture2D icon = ModPaths.LoadIcon(IconFiles[i]);
                if (icon != null)
                {
                    UITextureSprite sprite = button.AddUIComponent<UITextureSprite>();
                    sprite.texture = icon;
                    sprite.size = new Vector2(30f, 30f);
                    sprite.relativePosition = new Vector3(8f, 5f);
                    sprite.isInteractive = false;
                }

                button.eventClicked += delegate (UIComponent c, UIMouseEventParameter p)
                {
                    QuayTool tool = QuayTool.Instance;
                    if (tool != null) tool.SetMode(mode);
                    Refresh();
                };

                _modeButtons[i] = button;
            }

            _hint = MakeLabel(this, string.Empty, 12f, 216f, 0.7f);
            _hint.width = PanelWidth - 24f;
            _hint.wordWrap = true;
            _hint.autoSize = false;
            _hint.height = 82f;

            _status = MakeLabel(this, string.Empty, 12f, ContentTop, 0.8f);
            _status.textColor = new Color32(120, 220, 140, 255);
            _status.width = PanelWidth - 24f;

            BuildAddNetworkSection();
            BuildDecalSection();
            BuildBar();
        }

        private UIButton MakeCapButton(float y, bool atStart)
        {
            UIButton b = _add.AddUIComponent<UIButton>();
            b.width = PanelWidth - 20f;
            b.height = 30f;
            b.relativePosition = new Vector3(10f, y);
            StyleButton(b);
            b.textScale = 0.8f;
            b.textHorizontalAlignment = UIHorizontalAlignment.Left;
            b.textPadding = new RectOffset(10, 0, 0, 0);
            Color32 c = atStart ? (Color32)QuayTool.StartColor : (Color32)QuayTool.EndColor;
            b.textColor = c;
            b.hoveredTextColor = c;
            b.pressedTextColor = c;
            b.focusedTextColor = c;
            b.eventClicked += delegate (UIComponent comp, UIMouseEventParameter e)
            {
                if (_loading) return;
                if (atStart) _capStart = !_capStart; else _capEnd = !_capEnd;
                UpdateCapText();
                QuayTool tool = QuayTool.Instance;
                if (tool != null) tool.ApplyCap(atStart, atStart ? _capStart : _capEnd);
            };
            return b;
        }

        private void BuildAddNetworkSection()
        {
            _add = AddUIComponent<UIPanel>();
            _add.width = PanelWidth;
            _add.relativePosition = new Vector3(0f, ContentTop);
            _add.isVisible = false;

            float y = 0f;
            _selectionLabel = MakeLabel(_add, string.Empty, 12f, y, 0.85f);
            y += 24f;

            _landUi = BuildBlock(true, ref y);
            _waterUi = BuildBlock(false, ref y);

            _capStartButton = MakeCapButton(y, true);
            _capEndButton = MakeCapButton(y + 34f, false);
            UpdateCapText();
            y += 72f;

            _removeButton = _add.AddUIComponent<UIButton>();
            _removeButton.width = PanelWidth - 20f;
            _removeButton.height = 32f;
            _removeButton.relativePosition = new Vector3(10f, y);
            StyleButton(_removeButton);
            _removeButton.text = Loc.T("remove");
            _removeButton.textScale = 0.85f;
            _removeButton.eventClicked += delegate (UIComponent c, UIMouseEventParameter p)
            {
                QuayTool tool = QuayTool.Instance;
                if (tool == null) return;
                ClosePopup();
                tool.RemoveModels();
                LoadFromSelection();
            };
            y += 38f;

            // status line sits under the section
            _addHeight = y;

            // popups are created last so that they are drawn above the controls below their button
            BuildPopup(_landUi);
            BuildPopup(_waterUi);
        }

        private PickerUi BuildBlock(bool land, ref float y)
        {
            PickerUi ui = new PickerUi();
            ui.Land = land;

            ui.Parent = _add;
            ui.EmptyText = Loc.T("empty");
            ui.GetItems = delegate ()
            {
                List<PickItem> items = new List<PickItem>();
                List<FenceEntry> list = FenceCatalog.Entries;
                for (int i = 0; i < list.Count; i++)
                {
                    PickItem it = new PickItem();
                    it.Title = list[i].Title;
                    it.Atlas = list[i].Info.m_Atlas;
                    it.Thumb = list[i].Info.m_Thumbnail;
                    it.Tag = list[i].Info;
                    items.Add(it);
                }
                return items;
            };
            bool landCaptured = land;
            ui.OnPicked = delegate (PickItem item)
            {
                QuayTool tool = QuayTool.Instance;
                if (tool != null && !_loading) tool.ApplyModel(landCaptured, item == null ? null : (NetInfo)item.Tag);
            };

            ui.Title = MakeLabel(_add, Loc.T(land ? "model1" : "model2"), 12f, y, 0.85f);
            ui.Title.textColor = land ? new Color32(140, 255, 160, 255) : new Color32(140, 200, 255, 255);
            y += 20f;

            BuildHeader(_add, ui, y);
            PickerUi captured = ui;
            y += 42f;

            float sliderWidth = PanelWidth - 24f;

            MakeLabel(_add, Loc.T("hoff"), 12f, y, 0.72f);
            MakeLabel(_add, Loc.T("meter").Trim(), PanelWidth - 26f, y + 1f, 0.72f);
            ui.HValue = MakeField(_add, PanelWidth - 94f, y - 1f, delegate (float m) { OnField(captured, true, m); }, FenceStore.MaxUnits * FenceStore.Unit);
            y += 20f;
            ui.H = MakeSlider(_add, 12f, y, sliderWidth, -FenceStore.MaxUnits, FenceStore.MaxUnits, 0f);
            ui.H.eventValueChanged += delegate (UIComponent c, float v) { OnSlider(captured, true, v); };
            y += 22f;

            MakeLabel(_add, Loc.T("voff"), 12f, y, 0.72f);
            MakeLabel(_add, Loc.T("meter").Trim(), PanelWidth - 26f, y + 1f, 0.72f);
            ui.VValue = MakeField(_add, PanelWidth - 94f, y - 1f, delegate (float m) { OnField(captured, false, m); }, FenceStore.MaxUnits * FenceStore.Unit);
            y += 20f;
            ui.V = MakeSlider(_add, 12f, y, sliderWidth, -FenceStore.MaxUnits, FenceStore.MaxUnits, 0f);
            ui.V.eventValueChanged += delegate (UIComponent c, float v) { OnSlider(captured, false, v); };
            y += 28f;

            return ui;
        }

        /// <summary>The button that shows the chosen entry (icon + name) and opens the drop-down list.</summary>
        private void BuildHeader(UIComponent parent, PickerUi ui, float y)
        {
            ui.Header = parent.AddUIComponent<UIButton>();
            ui.Header.width = PanelWidth - 20f;
            ui.Header.height = 38f;
            ui.Header.relativePosition = new Vector3(10f, y);
            StyleButton(ui.Header);

            ui.Icon = ui.Header.AddUIComponent<UISprite>();
            ui.Icon.size = new Vector2(32f, 32f);
            ui.Icon.relativePosition = new Vector3(6f, 3f);
            ui.Icon.isInteractive = false;
            ui.Icon.isVisible = false;

            ui.Name = ui.Header.AddUIComponent<UILabel>();
            ui.Name.textScale = 0.8f;
            ui.Name.autoSize = false;
            ui.Name.width = PanelWidth - 20f - 60f;
            ui.Name.height = 20f;
            ui.Name.relativePosition = new Vector3(46f, 10f);
            ui.Name.isInteractive = false;
            ui.Name.text = ui.EmptyText;

            PickerUi captured = ui;
            ui.Header.eventClicked += delegate (UIComponent c, UIMouseEventParameter p) { TogglePopup(captured); };
        }

        private void BuildPopup(PickerUi ui)
        {
            float top = ui.Header.relativePosition.y + ui.Header.height + 2f;

            UIScrollablePanel popup = ui.Parent.AddUIComponent<UIScrollablePanel>();
            popup.width = PanelWidth - 20f;
            popup.height = 200f;
            popup.relativePosition = new Vector3(10f, top);
            popup.backgroundSprite = "MenuPanel2";
            popup.autoLayout = true;
            popup.autoLayoutDirection = LayoutDirection.Vertical;
            popup.autoLayoutPadding = new RectOffset(0, 0, 0, 2);
            popup.clipChildren = true;
            popup.scrollWheelDirection = UIOrientation.Vertical;
            PickerUi wheelUi = ui;
            popup.eventMouseWheel += delegate (UIComponent c, UIMouseEventParameter e) { OnPopupWheel(wheelUi, e); };
            popup.isVisible = false;
            ui.Popup = popup;

            UIScrollbar bar = ui.Parent.AddUIComponent<UIScrollbar>();
            bar.width = 12f;
            bar.height = popup.height;
            bar.orientation = UIOrientation.Vertical;
            bar.pivot = UIPivotPoint.TopLeft;
            bar.relativePosition = new Vector3(popup.relativePosition.x + popup.width - 12f, top);
            bar.minValue = 0f;
            bar.value = 0f;
            bar.incrementAmount = 40f;
            bar.isVisible = false;

            UISlicedSprite track = bar.AddUIComponent<UISlicedSprite>();
            track.spriteName = "ScrollbarTrack";
            track.relativePosition = Vector3.zero;
            track.size = bar.size;
            bar.trackObject = track;

            UISlicedSprite thumb = track.AddUIComponent<UISlicedSprite>();
            thumb.spriteName = "ScrollbarThumb";
            thumb.width = 10f;
            bar.thumbObject = thumb;

            popup.verticalScrollbar = bar;
            ui.Bar = bar;
        }

        // ---------- drop-down list ----------

        private void FillPopup(PickerUi ui)
        {
            List<UIComponent> old = new List<UIComponent>(ui.Popup.components);
            for (int i = 0; i < old.Count; i++)
            {
                ui.Popup.RemoveUIComponent(old[i]);
                Destroy(old[i].gameObject);
            }

            AddRow(ui, null);

            List<PickItem> list = ui.GetItems();
            for (int i = 0; i < list.Count; i++)
            {
                AddRow(ui, list[i]);
            }

            int rows = Mathf.Min(list.Count + 1, MaxPopupRows);
            ui.Popup.height = rows * (RowHeight + 2f) + 4f;
            ui.Bar.height = ui.Popup.height;
        }

        private void AddRow(PickerUi ui, PickItem entry)
        {
            UIButton row = ui.Popup.AddUIComponent<UIButton>();
            row.width = ui.Popup.width - 16f;
            row.height = RowHeight;
            StyleButton(row);
            row.textScale = 0.8f;
            row.textHorizontalAlignment = UIHorizontalAlignment.Left;
            row.textVerticalAlignment = UIVerticalAlignment.Middle;
            row.textPadding = new RectOffset(46, 0, 0, 0);

            string title = entry == null ? ui.EmptyText : entry.Title;
            row.text = title.Length > 34 ? title.Substring(0, 33) + "…" : title;
            row.tooltip = title;

            if (entry != null && entry.Atlas != null && !string.IsNullOrEmpty(entry.Thumb))
            {
                UISprite icon = row.AddUIComponent<UISprite>();
                icon.atlas = entry.Atlas;
                icon.spriteName = entry.Thumb;
                icon.size = new Vector2(30f, 30f);
                icon.relativePosition = new Vector3(8f, 3f);
                icon.isInteractive = false;
            }

            PickerUi wheelUi = ui;
            row.eventMouseWheel += delegate (UIComponent c, UIMouseEventParameter e) { OnPopupWheel(wheelUi, e); };

            PickItem picked = entry;
            PickerUi captured = ui;
            row.eventClicked += delegate (UIComponent c, UIMouseEventParameter p)
            {
                SetHeaderItem(captured, picked);
                ClosePopup();
                if (!_loading) captured.OnPicked(picked);
            };
        }

        private static void OnPopupWheel(PickerUi ui, UIMouseEventParameter e)
        {
            float content = ui.Popup.components.Count * (RowHeight + 2f) + 4f;
            float max = Mathf.Max(0f, content - ui.Popup.height);
            float y = Mathf.Clamp(ui.Popup.scrollPosition.y - e.wheelDelta * (RowHeight + 2f), 0f, max);
            ui.Popup.scrollPosition = new Vector2(0f, y);
            e.Use();
        }

        private void TogglePopup(PickerUi ui)
        {
            if (_openPopup == ui)
            {
                ClosePopup();
                return;
            }

            ClosePopup();
            FillPopup(ui);
            ui.Popup.isVisible = true;
            ui.Bar.isVisible = true;
            ui.Popup.BringToFront();
            ui.Bar.BringToFront();
            _openPopup = ui;
            UpdateHeight();
        }

        private void ClosePopup()
        {
            if (_openPopup == null) return;
            _openPopup.Popup.isVisible = false;
            _openPopup.Bar.isVisible = false;
            _openPopup = null;
            UpdateHeight();
        }

        private void SetHeader(PickerUi ui, NetInfo info)
        {
            if (info == null)
            {
                SetHeaderItem(ui, null);
                return;
            }

            string title = info.name;
            try
            {
                string t = info.GetUncheckedLocalizedTitle();
                if (!string.IsNullOrEmpty(t)) title = t;
            }
            catch (Exception)
            {
                // keep the prefab name
            }

            PickItem item = new PickItem();
            item.Title = title;
            item.Atlas = info.m_Atlas;
            item.Thumb = info.m_Thumbnail;
            item.Tag = info;
            SetHeaderItem(ui, item);
        }

        private void SetHeaderItem(PickerUi ui, PickItem item)
        {
            if (item == null)
            {
                ui.Name.text = ui.EmptyText;
                ui.Icon.isVisible = false;
                return;
            }

            ui.Name.text = item.Title.Length > 34 ? item.Title.Substring(0, 33) + "…" : item.Title;

            if (item.Atlas != null && !string.IsNullOrEmpty(item.Thumb))
            {
                ui.Icon.atlas = item.Atlas;
                ui.Icon.spriteName = item.Thumb;
                ui.Icon.isVisible = true;
            }
            else
            {
                ui.Icon.isVisible = false;
            }
        }

        // ---------- controls -> tool ----------

        private void OnField(PickerUi ui, bool horizontal, float meters)
        {
            OnFieldFor(horizontal ? ui.H : ui.V, horizontal ? ui.HValue : ui.VValue, meters);
        }

        /// <summary>A value typed in metres: clamps it to the slider range and moves the slider (which applies it).</summary>
        private static void OnFieldFor(UISlider slider, UITextField field, float meters)
        {
            if (float.IsNaN(meters))
            {
                field.text = FormatOffset(slider.value);
                return;
            }

            float units = Mathf.Clamp(Mathf.Round(meters / FenceStore.Unit), slider.minValue, slider.maxValue);
            if (Mathf.Approximately(units, slider.value))
            {
                field.text = FormatOffset(units);
                return;
            }
            slider.value = units; // raises eventValueChanged -> the slider handler applies it
        }

        private void OnSlider(PickerUi ui, bool horizontal, float value)
        {
            UITextField label = horizontal ? ui.HValue : ui.VValue;
            label.text = FormatOffset(value);

            if (_loading) return;
            QuayTool tool = QuayTool.Instance;
            if (tool != null) tool.ApplyOffset(ui.Land, horizontal, Mathf.RoundToInt(value));
        }

        private void UpdateCapText()
        {
            _capStartButton.text = (_capStart ? "[x]  " : "[  ]  ") + Loc.T("capstart");
            _capStartButton.state = _capStart ? UIButton.ButtonState.Focused : UIButton.ButtonState.Normal;
            _capEndButton.text = (_capEnd ? "[x]  " : "[  ]  ") + Loc.T("capend");
            _capEndButton.state = _capEnd ? UIButton.ButtonState.Focused : UIButton.ButtonState.Normal;
        }

        // ---------- tool -> controls ----------

        private void OnSelectionChanged()
        {
            ClosePopup();
            LoadFromSelection();
        }

        /// <summary>Shows the settings of the first selected segment in the controls.</summary>
        private void LoadFromSelection()
        {
            if (!_built || _add == null) return;

            QuayTool current = QuayTool.Instance;
            if (current != null && current.CurrentMode == QuayTool.Mode.Decal)
            {
                LoadDecalFromSelection();
                return;
            }

            QuayTool tool = QuayTool.Instance;
            int count = tool == null ? 0 : tool.Selection.Count;

            _loading = true;
            try
            {
                _selectionLabel.text = Loc.T("selected") + count;
                SetControlsEnabled(count > 0);

                NetInfo landModel = null, waterModel = null;
                FenceSettings s = new FenceSettings();

                if (count > 0)
                {
                    ushort id = tool.Selection[0];
                    NetSegment[] segs = NetManager.instance.m_segments.m_buffer;
                    QuayFrame frame = QuayGeometry.GetFrame(id);

                    bool landSlotLeft = QuayGeometry.SlotIsLeft(true, frame.WaterIsRight);
                    landModel = landSlotLeft ? segs[id].LeftFenceInfo : segs[id].RightFenceInfo;
                    waterModel = landSlotLeft ? segs[id].RightFenceInfo : segs[id].LeftFenceInfo;

                    FenceSettings stored;
                    if (FenceStore.TryGet(id, out stored)) s = stored;
                }

                SetHeader(_landUi, landModel);
                SetHeader(_waterUi, waterModel);

                _landUi.H.value = s.LandH;
                _landUi.V.value = s.LandV;
                _waterUi.H.value = s.WaterH;
                _waterUi.V.value = s.WaterV;
                _landUi.HValue.text = FormatOffset(s.LandH);
                _landUi.VValue.text = FormatOffset(s.LandV);
                _waterUi.HValue.text = FormatOffset(s.WaterH);
                _waterUi.VValue.text = FormatOffset(s.WaterV);

                _capStart = s.CapStart;
                _capEnd = s.CapEnd;
                UpdateCapText();
            }
            finally
            {
                _loading = false;
            }
        }

        private void SetControlsEnabled(bool enabled)
        {
            PickerUi[] blocks = { _landUi, _waterUi };
            for (int i = 0; i < blocks.Length; i++)
            {
                blocks[i].Header.isEnabled = enabled;
                blocks[i].H.isEnabled = enabled;
                blocks[i].V.isEnabled = enabled;
            }
            _capStartButton.isEnabled = enabled;
            _capEndButton.isEnabled = enabled;
            _removeButton.isEnabled = enabled;
            if (_resetBtn != null) _resetBtn.isEnabled = enabled;
        }

        // ---------- refresh / layout ----------

        public void Refresh()
        {
            if (!_built) return;

            QuayTool tool = QuayTool.Instance;
            QuayTool.Mode current = tool != null ? tool.CurrentMode : QuayTool.Mode.Invert;

            for (int i = 0; i < _modeButtons.Length; i++)
            {
                if (!_modeButtons[i].isEnabled) continue;
                _modeButtons[i].state = (int)current == i ? UIButton.ButtonState.Focused : UIButton.ButtonState.Normal;
            }

            _hint.text = QuayTool.HintFor(current);

            bool addMode = current == QuayTool.Mode.AddNetwork;
            bool decalMode = current == QuayTool.Mode.Decal;
            _add.isVisible = addMode;
            _decal.isVisible = decalMode;
            _bar.isVisible = addMode || decalMode;
            ClosePopup();
            if (addMode || decalMode) LoadFromSelection();
            UpdateBar();
            UpdateHeight();
        }

        private void UpdateHeight()
        {
            if (!_built) return;

            bool addMode = _add != null && _add.isVisible;
            bool decalMode = _decal != null && _decal.isVisible;
            bool select = addMode || decalMode;

            float section = addMode ? _addHeight : decalMode ? _decalHeight : 0f;
            float barY = ContentTop + section;
            float need = select ? barY + 42f + 24f : ContentTop + 30f;

            if (select && _openPopup != null)
            {
                float popupBottom = ContentTop + _openPopup.Popup.relativePosition.y + _openPopup.Popup.height + 10f;
                need = Mathf.Max(need, popupBottom);
            }

            height = need;

            if (select) _bar.relativePosition = new Vector3(0f, barY);

            // the status line sits just under the visible content
            _status.relativePosition = new Vector3(12f, select ? barY + 42f : ContentTop);
        }

        // ---------- decal path section ----------

        /// <summary>A label, a slider and a value field (metres) in one block; returns the y of the next block.</summary>
        private float MakeValueRow(UIComponent parent, float y, string labelKey, float min, float max, float reset,
                                   out UISlider slider, out UITextField field, Action<int> onValue)
        {
            UISlider sl = MakeSlider(parent, 12f, y + 20f, PanelWidth - 24f, min, max, reset);
            UITextField fl = null;
            MakeLabel(parent, Loc.T(labelKey), 12f, y, 0.72f);
            MakeLabel(parent, Loc.T("meter").Trim(), PanelWidth - 26f, y + 1f, 0.72f);
            fl = MakeField(parent, PanelWidth - 94f, y - 1f, delegate (float m) { OnFieldFor(sl, fl, m); }, max * FenceStore.Unit);
            fl.text = FormatOffset(reset);

            UITextField flCaptured = fl;
            sl.eventValueChanged += delegate (UIComponent c, float v)
            {
                int units = Mathf.RoundToInt(v);
                flCaptured.text = FormatOffset(units);
                onValue(units);
            };

            slider = sl;
            field = fl;
            return y + 44f;
        }

        private void BuildDecalSection()
        {
            _decal = AddUIComponent<UIPanel>();
            _decal.width = PanelWidth;
            _decal.relativePosition = new Vector3(0f, ContentTop);
            _decal.isVisible = false;

            float y = 0f;
            _decalSel = MakeLabel(_decal, string.Empty, 12f, y, 0.85f);
            y += 26f;

            // decal chooser
            _decalUi = new PickerUi();
            _decalUi.Parent = _decal;
            _decalUi.EmptyText = Loc.T("decal_solid");
            _decalUi.GetItems = delegate ()
            {
                List<PickItem> items = new List<PickItem>();
                List<DecalEntry> list = DecalCatalog.Entries;
                for (int i = 0; i < list.Count; i++)
                {
                    PickItem it = new PickItem();
                    it.Title = list[i].Title;
                    it.Atlas = list[i].Info.m_Atlas;
                    it.Thumb = list[i].Info.m_Thumbnail;
                    it.Tag = list[i];
                    items.Add(it);
                }
                return items;
            };
            _decalUi.OnPicked = OnDecalPicked;

            MakeLabel(_decal, Loc.T("dprop"), 12f, y, 0.85f);
            y += 20f;
            BuildHeader(_decal, _decalUi, y);
            y += 44f;

            y = MakeValueRow(_decal, y, "dwidth", DecalStore.MinWidth, DecalStore.MaxWidth, DecalSettings.DefaultWidth, out _dWidth, out _dWidthV,
                delegate (int u) { OnDecalValue("width", u, delegate (DecalSettings d, int v) { d.Width = v; }); });
            y = MakeValueRow(_decal, y, "dscale", DecalStore.MinScale, DecalStore.MaxScale, DecalSettings.DefaultScale, out _dScale, out _dScaleV,
                delegate (int u) { OnDecalValue("scale", u, delegate (DecalSettings d, int v) { d.Scale = v; }); });
            y = MakeValueRow(_decal, y, "dlateral", -FenceStore.MaxUnits, FenceStore.MaxUnits, 0f, out _dLateral, out _dLateralV,
                delegate (int u) { OnDecalValue("lateral", u, delegate (DecalSettings d, int v) { d.Lateral = v; }); });
            y = MakeValueRow(_decal, y, "dlift", -FenceStore.MaxUnits, FenceStore.MaxUnits, 0f, out _dLift, out _dLiftV,
                delegate (int u) { OnDecalValue("lift", u, delegate (DecalSettings d, int v) { d.Lift = v; }); });

            MakeLabel(_decal, Loc.T("dcolor"), 12f, y, 0.72f);
            y += 18f;
            _dColors = new UIButton[DecalStore.Colors.Length];
            for (int i = 0; i < _dColors.Length; i++)
            {
                int index = i;
                UIButton b = _decal.AddUIComponent<UIButton>();
                b.size = new Vector2(44f, 26f);
                b.relativePosition = new Vector3(12f + i * 49f, y);
                StyleButton(b);
                Color tint = DecalStore.Colors[i];
                tint.a = 1f;
                b.color = tint;
                b.textScale = 0.9f;
                b.eventClicked += delegate (UIComponent comp, UIMouseEventParameter e)
                {
                    _brush.ColorIndex = index;
                    UpdateColorButtons();
                    if (_loading) return;
                    QuayTool tool = QuayTool.Instance;
                    if (tool != null) tool.EditDecal("color", delegate (DecalSettings d) { d.ColorIndex = index; });
                };
                _dColors[i] = b;
            }
            UpdateColorButtons();
            y += 36f;

            _decalState = MakeLabel(_decal, string.Empty, 12f, y, 0.75f);
            y += 22f;

            _dAdd = _decal.AddUIComponent<UIButton>();
            _dAdd.width = PanelWidth - 20f;
            _dAdd.height = 32f;
            _dAdd.relativePosition = new Vector3(10f, y);
            StyleButton(_dAdd);
            _dAdd.text = Loc.T("decal_add");
            _dAdd.textScale = 0.85f;
            _dAdd.eventClicked += delegate (UIComponent c, UIMouseEventParameter p)
            {
                QuayTool tool = QuayTool.Instance;
                if (tool == null) return;
                ClosePopup();
                tool.AddDecal(_brush);
            };
            y += 38f;

            _dRemove = _decal.AddUIComponent<UIButton>();
            _dRemove.width = PanelWidth - 20f;
            _dRemove.height = 32f;
            _dRemove.relativePosition = new Vector3(10f, y);
            StyleButton(_dRemove);
            _dRemove.text = Loc.T("decal_remove");
            _dRemove.textScale = 0.85f;
            _dRemove.eventClicked += delegate (UIComponent c, UIMouseEventParameter p)
            {
                QuayTool tool = QuayTool.Instance;
                if (tool != null) tool.RemoveDecal();
            };
            y += 38f;

            _decalHeight = y;

            // the list is created last so that it is drawn above the controls below its button
            BuildPopup(_decalUi);
        }

        /// <summary>A decal was chosen in the list: remember it, use its natural size as tile size, apply it to existing paths.</summary>
        private void OnDecalPicked(PickItem item)
        {
            DecalEntry entry = item == null ? null : (DecalEntry)item.Tag;
            string prop = entry == null ? null : entry.Info.name;
            int scale = _brush.Scale;

            if (entry != null)
            {
                scale = Mathf.Clamp(Mathf.RoundToInt(entry.NaturalSize / FenceStore.Unit), DecalStore.MinScale, DecalStore.MaxScale);
                bool was = _loading;
                _loading = true; // do not send the scale as a separate edit
                _dScale.value = scale;
                _dScaleV.text = FormatOffset(scale);
                _loading = was;
                _brush.Scale = scale;
            }
            _brush.Prop = prop;

            QuayTool tool = QuayTool.Instance;
            if (tool == null) return;
            int applyScale = scale;
            bool setScale = entry != null;
            tool.EditDecal("prop", delegate (DecalSettings d)
            {
                d.Prop = prop;
                if (setScale) d.Scale = applyScale;
            });
        }

        private void ShowBrushHeader()
        {
            PropInfo info = DecalCatalog.Find(_brush.Prop);
            if (info == null)
            {
                SetHeaderItem(_decalUi, null);
                return;
            }

            string title = info.name;
            try
            {
                string t = info.GetUncheckedLocalizedTitle();
                if (!string.IsNullOrEmpty(t)) title = t;
            }
            catch (Exception)
            {
                // keep the prefab name
            }

            PickItem item = new PickItem();
            item.Title = title;
            item.Atlas = info.m_Atlas;
            item.Thumb = info.m_Thumbnail;
            SetHeaderItem(_decalUi, item);
        }

        private void UpdateColorButtons()
        {
            if (_dColors == null) return;
            for (int i = 0; i < _dColors.Length; i++)
            {
                bool on = i == _brush.ColorIndex;
                _dColors[i].state = on ? UIButton.ButtonState.Focused : UIButton.ButtonState.Normal;
                _dColors[i].text = on ? "*" : string.Empty;
                _dColors[i].textColor = i == 0 || i == 1 ? new Color32(20, 20, 20, 255) : new Color32(255, 255, 255, 255);
                _dColors[i].focusedTextColor = _dColors[i].textColor;
                _dColors[i].hoveredTextColor = _dColors[i].textColor;
            }
        }

        /// <summary>A decal slider moved: remember it for the next "add" and apply it to the selected paths.</summary>
        private void OnDecalValue(string property, int units, Action<DecalSettings, int> set)
        {
            set(_brush, units);
            if (_loading) return;

            QuayTool tool = QuayTool.Instance;
            if (tool != null) tool.EditDecal(property, delegate (DecalSettings d) { set(d, units); });
        }

        /// <summary>Shows the first selected path in the controls; without a path the controls keep their values.</summary>
        private void LoadDecalFromSelection()
        {
            if (_decal == null) return;

            QuayTool tool = QuayTool.Instance;
            int count = tool == null ? 0 : tool.Selection.Count;

            _loading = true;
            try
            {
                _decalSel.text = Loc.T("selected") + count;
                SetDecalControlsEnabled(count > 0);

                int have = 0;
                DecalSettings first = null;
                for (int i = 0; i < count; i++)
                {
                    DecalSettings d;
                    if (DecalStore.TryGet(tool.Selection[i], out d))
                    {
                        have++;
                        if (first == null) first = d;
                    }
                }

                if (first != null)
                {
                    _brush.Width = first.Width;
                    _brush.Lateral = first.Lateral;
                    _brush.Lift = first.Lift;
                    _brush.ColorIndex = first.ColorIndex;
                    _brush.Prop = first.Prop;
                    _brush.Scale = first.Scale;

                    _dWidth.value = first.Width;
                    _dScale.value = first.Scale;
                    _dLateral.value = first.Lateral;
                    _dLift.value = first.Lift;
                    UpdateColorButtons();
                }

                _dWidthV.text = FormatOffset(_dWidth.value);
                _dScaleV.text = FormatOffset(_dScale.value);
                _dLateralV.text = FormatOffset(_dLateral.value);
                _dLiftV.text = FormatOffset(_dLift.value);
                ShowBrushHeader();
                _decalState.text = Loc.F("decal_state", have, count);
            }
            finally
            {
                _loading = false;
            }
        }

        /// <summary>Puts the decal controls (the values used for the next "add") back to their defaults.</summary>
        private void ResetDecalBrush()
        {
            _loading = true;
            try
            {
                string keepProp = _brush.Prop; // like fence models, the chosen decal stays
                _brush.ResetToDefaults();
                _brush.Prop = keepProp;
                _dWidth.value = _brush.Width;
                _dScale.value = _brush.Scale;
                _dScaleV.text = FormatOffset(_dScale.value);
                _dLateral.value = _brush.Lateral;
                _dLift.value = _brush.Lift;
                _dWidthV.text = FormatOffset(_dWidth.value);
                _dLateralV.text = FormatOffset(_dLateral.value);
                _dLiftV.text = FormatOffset(_dLift.value);
                UpdateColorButtons();
            }
            finally
            {
                _loading = false;
            }
        }

        private void SetDecalControlsEnabled(bool enabled)
        {
            _dWidth.isEnabled = enabled;
            _dScale.isEnabled = enabled;
            _decalUi.Header.isEnabled = enabled;
            _dLateral.isEnabled = enabled;
            _dLift.isEnabled = enabled;
            _dAdd.isEnabled = enabled;
            _dRemove.isEnabled = enabled;
            for (int i = 0; i < _dColors.Length; i++) _dColors[i].isEnabled = enabled;
            if (_resetBtn != null) _resetBtn.isEnabled = enabled;
        }

        // ---------- undo / redo / reset bar ----------

        private void BuildBar()
        {
            _bar = AddUIComponent<UIPanel>();
            _bar.width = PanelWidth;
            _bar.height = 38f;
            _bar.relativePosition = new Vector3(0f, ContentTop);
            _bar.isVisible = false;

            float w = (PanelWidth - 20f - 12f) / 3f;
            _undoBtn = MakeBarButton(0f, w, "undo", "undo_tip", delegate
            {
                QuayTool tool = QuayTool.Instance;
                if (tool != null) tool.Undo();
            });
            _redoBtn = MakeBarButton(w + 6f, w, "redo", "redo_tip", delegate
            {
                QuayTool tool = QuayTool.Instance;
                if (tool != null) tool.Redo();
            });
            _resetBtn = MakeBarButton(2f * (w + 6f), w, "reset", "reset_tip", delegate
            {
                QuayTool tool = QuayTool.Instance;
                if (tool == null) return;
                if (tool.CurrentMode == QuayTool.Mode.Decal) ResetDecalBrush();
                tool.ResetSelection();
            });
        }

        private UIButton MakeBarButton(float x, float w, string textKey, string tipKey, Action onClick)
        {
            UIButton b = _bar.AddUIComponent<UIButton>();
            b.size = new Vector2(w, 30f);
            b.relativePosition = new Vector3(10f + x, 4f);
            StyleButton(b);
            b.text = Loc.T(textKey);
            b.textScale = 0.8f;
            b.tooltip = Loc.T(tipKey);
            b.eventClicked += delegate (UIComponent c, UIMouseEventParameter p) { onClick(); };
            return b;
        }

        private void UpdateBar()
        {
            if (_undoBtn == null) return;
            _undoBtn.isEnabled = History.CanUndo;
            _redoBtn.isEnabled = History.CanRedo;
        }

        /// <summary>Called every frame by the tool: keeps the undo/redo buttons current and reloads the controls after undo/redo/reset.</summary>
        public void SyncHistory()
        {
            if (!_built) return;

            if (_historyVersion != History.Version)
            {
                _historyVersion = History.Version;
                UpdateBar();
            }

            if (_restoreVersion != History.RestoreVersion)
            {
                _restoreVersion = History.RestoreVersion;
                LoadFromSelection();
            }
        }

        /// <summary>True while one of our text fields has the keyboard focus (Ctrl+Z must not act then).</summary>
        public bool IsTyping
        {
            get
            {
                if (!_built) return false;
                UITextField[] fields = { _landUi.HValue, _landUi.VValue, _waterUi.HValue, _waterUi.VValue, _dWidthV, _dScaleV, _dLateralV, _dLiftV };
                for (int i = 0; i < fields.Length; i++)
                {
                    if (fields[i] != null && fields[i].hasFocus) return true;
                }
                return false;
            }
        }

        public void SetStatus(string text)
        {
            if (_status != null && _status.text != text) _status.text = text;
        }
    }
}
