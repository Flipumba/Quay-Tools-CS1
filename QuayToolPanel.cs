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
        private const float ContentTop = 222f;
        private const float RowHeight = 36f;
        private const int MaxPopupRows = 8;

        public static QuayToolPanel Instance { get; private set; }

        private static readonly string[] TitleKeys = { "mode_invert", "mode_nopeds", "mode_network" };
        private static readonly string[] IconFiles = { "Invert.png", "NoPedestrian.png", "Network.png" };

        private class PickerUi
        {
            public bool Land;
            public UILabel Title;
            public UIButton Header;
            public UISprite Icon;
            public UILabel Name;
            public UISlider H, V;
            public UILabel HValue, VValue;
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
        private UIButton _noConnectButton;
        private UIButton _removeButton;
        private bool _noConnect = true;
        private PickerUi _openPopup;

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

        private static UISlider MakeSlider(UIComponent parent, float x, float y, float width)
        {
            UISlider slider = parent.AddUIComponent<UISlider>();
            slider.size = new Vector2(width, 16f);
            slider.relativePosition = new Vector3(x, y);
            slider.minValue = -100f;
            slider.maxValue = 100f;
            slider.stepSize = 1f;
            slider.value = 0f;

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

            slider.tooltip = "0 = " + (Loc.IsRussian ? "сброс (двойной клик)" : "reset (double click)");
            slider.eventDoubleClick += delegate (UIComponent c, UIMouseEventParameter p) { slider.value = 0f; };
            return slider;
        }

        private static string FormatOffset(float sliderValue)
        {
            return (sliderValue * FenceStore.Unit).ToString("0.0") + Loc.T("meter");
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

            _modeButtons = new UIButton[3];
            for (int i = 0; i < 3; i++)
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

            _hint = MakeLabel(this, string.Empty, 12f, 174f, 0.7f);
            _hint.width = PanelWidth - 24f;
            _hint.wordWrap = true;
            _hint.autoSize = false;
            _hint.height = 44f;

            _status = MakeLabel(this, string.Empty, 12f, ContentTop, 0.8f);
            _status.textColor = new Color32(120, 220, 140, 255);
            _status.width = PanelWidth - 24f;

            BuildAddNetworkSection();
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

            _noConnectButton = _add.AddUIComponent<UIButton>();
            _noConnectButton.width = PanelWidth - 20f;
            _noConnectButton.height = 30f;
            _noConnectButton.relativePosition = new Vector3(10f, y);
            StyleButton(_noConnectButton);
            _noConnectButton.textScale = 0.8f;
            _noConnectButton.textHorizontalAlignment = UIHorizontalAlignment.Left;
            _noConnectButton.textPadding = new RectOffset(10, 0, 0, 0);
            _noConnectButton.eventClicked += delegate (UIComponent c, UIMouseEventParameter p)
            {
                if (_loading) return;
                _noConnect = !_noConnect;
                UpdateNoConnectText();
                QuayTool tool = QuayTool.Instance;
                if (tool != null) tool.ApplyNoConnect(_noConnect);
            };
            UpdateNoConnectText();
            y += 36f;

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

            ui.Title = MakeLabel(_add, Loc.T(land ? "model1" : "model2"), 12f, y, 0.85f);
            ui.Title.textColor = land ? new Color32(140, 255, 160, 255) : new Color32(140, 200, 255, 255);
            y += 20f;

            ui.Header = _add.AddUIComponent<UIButton>();
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
            ui.Name.text = Loc.T("empty");

            PickerUi captured = ui;
            ui.Header.eventClicked += delegate (UIComponent c, UIMouseEventParameter p) { TogglePopup(captured); };
            y += 42f;

            float sliderWidth = PanelWidth - 24f;

            MakeLabel(_add, Loc.T("hoff"), 12f, y, 0.72f);
            ui.HValue = MakeLabel(_add, FormatOffset(0f), PanelWidth - 92f, y, 0.72f);
            ui.HValue.autoSize = false;
            ui.HValue.width = 80f;
            ui.HValue.textAlignment = UIHorizontalAlignment.Right;
            y += 16f;
            ui.H = MakeSlider(_add, 12f, y, sliderWidth);
            ui.H.eventValueChanged += delegate (UIComponent c, float v) { OnSlider(captured, true, v); };
            y += 22f;

            MakeLabel(_add, Loc.T("voff"), 12f, y, 0.72f);
            ui.VValue = MakeLabel(_add, FormatOffset(0f), PanelWidth - 92f, y, 0.72f);
            ui.VValue.autoSize = false;
            ui.VValue.width = 80f;
            ui.VValue.textAlignment = UIHorizontalAlignment.Right;
            y += 16f;
            ui.V = MakeSlider(_add, 12f, y, sliderWidth);
            ui.V.eventValueChanged += delegate (UIComponent c, float v) { OnSlider(captured, false, v); };
            y += 28f;

            return ui;
        }

        private void BuildPopup(PickerUi ui)
        {
            float top = ui.Header.relativePosition.y + ui.Header.height + 2f;

            UIScrollablePanel popup = _add.AddUIComponent<UIScrollablePanel>();
            popup.width = PanelWidth - 20f;
            popup.height = 200f;
            popup.relativePosition = new Vector3(10f, top);
            popup.backgroundSprite = "MenuPanel2";
            popup.autoLayout = true;
            popup.autoLayoutDirection = LayoutDirection.Vertical;
            popup.autoLayoutPadding = new RectOffset(0, 0, 0, 2);
            popup.clipChildren = true;
            popup.scrollWheelDirection = UIOrientation.Vertical;
            popup.isVisible = false;
            ui.Popup = popup;

            UIScrollbar bar = _add.AddUIComponent<UIScrollbar>();
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

            List<FenceEntry> list = FenceCatalog.Entries;
            for (int i = 0; i < list.Count; i++)
            {
                AddRow(ui, list[i]);
            }

            int rows = Mathf.Min(list.Count + 1, MaxPopupRows);
            ui.Popup.height = rows * (RowHeight + 2f) + 4f;
            ui.Bar.height = ui.Popup.height;
        }

        private void AddRow(PickerUi ui, FenceEntry entry)
        {
            UIButton row = ui.Popup.AddUIComponent<UIButton>();
            row.width = ui.Popup.width - 16f;
            row.height = RowHeight;
            StyleButton(row);
            row.textScale = 0.8f;
            row.textHorizontalAlignment = UIHorizontalAlignment.Left;
            row.textVerticalAlignment = UIVerticalAlignment.Middle;
            row.textPadding = new RectOffset(46, 0, 0, 0);

            string title = entry == null ? Loc.T("empty") : entry.Title;
            row.text = title.Length > 34 ? title.Substring(0, 33) + "…" : title;
            row.tooltip = title;

            if (entry != null && entry.Info.m_Atlas != null && !string.IsNullOrEmpty(entry.Info.m_Thumbnail))
            {
                UISprite icon = row.AddUIComponent<UISprite>();
                icon.atlas = entry.Info.m_Atlas;
                icon.spriteName = entry.Info.m_Thumbnail;
                icon.size = new Vector2(30f, 30f);
                icon.relativePosition = new Vector3(8f, 3f);
                icon.isInteractive = false;
            }

            NetInfo picked = entry == null ? null : entry.Info;
            PickerUi captured = ui;
            row.eventClicked += delegate (UIComponent c, UIMouseEventParameter p)
            {
                SetHeader(captured, picked);
                ClosePopup();

                QuayTool tool = QuayTool.Instance;
                if (tool != null && !_loading) tool.ApplyModel(captured.Land, picked);
            };
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
                ui.Name.text = Loc.T("empty");
                ui.Icon.isVisible = false;
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

            ui.Name.text = title.Length > 34 ? title.Substring(0, 33) + "…" : title;

            if (info.m_Atlas != null && !string.IsNullOrEmpty(info.m_Thumbnail))
            {
                ui.Icon.atlas = info.m_Atlas;
                ui.Icon.spriteName = info.m_Thumbnail;
                ui.Icon.isVisible = true;
            }
            else
            {
                ui.Icon.isVisible = false;
            }
        }

        // ---------- controls -> tool ----------

        private void OnSlider(PickerUi ui, bool horizontal, float value)
        {
            UILabel label = horizontal ? ui.HValue : ui.VValue;
            label.text = FormatOffset(value);

            if (_loading) return;
            QuayTool tool = QuayTool.Instance;
            if (tool != null) tool.ApplyOffset(ui.Land, horizontal, Mathf.RoundToInt(value));
        }

        private void UpdateNoConnectText()
        {
            _noConnectButton.text = (_noConnect ? "[x]  " : "[  ]  ") + Loc.T("noconnect");
            _noConnectButton.state = _noConnect ? UIButton.ButtonState.Focused : UIButton.ButtonState.Normal;
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

                _noConnect = s.NoConnect;
                UpdateNoConnectText();
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
            _noConnectButton.isEnabled = enabled;
            _removeButton.isEnabled = enabled;
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
            _add.isVisible = addMode;
            ClosePopup();
            if (addMode) LoadFromSelection();
            UpdateHeight();
        }

        private void UpdateHeight()
        {
            if (!_built) return;

            bool addMode = _add != null && _add.isVisible;
            float need = addMode ? ContentTop + _addHeight + 26f : ContentTop + 30f;

            if (addMode && _openPopup != null)
            {
                float popupBottom = ContentTop + _openPopup.Popup.relativePosition.y + _openPopup.Popup.height + 10f;
                need = Mathf.Max(need, popupBottom);
            }

            height = need;

            // the status line sits just under the visible content
            _status.relativePosition = new Vector3(12f, addMode ? ContentTop + _addHeight + 2f : ContentTop);
        }

        public void SetStatus(string text)
        {
            if (_status != null && _status.text != text) _status.text = text;
        }
    }
}
