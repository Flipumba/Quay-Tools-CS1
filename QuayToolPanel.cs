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
        private const float ToolsWidth = 66f;   // first column: the tool buttons
        private const float TopY = 48f;          // top of the three columns (below the title)
        private const float RowHeight = 36f;
        private const int MaxPopupRows = 8;
        private const float ListTop = 34f;
        private const float RowStride = RowHeight + 2f;

        private static Texture2D _starOff, _starOn;

        public static QuayToolPanel Instance { get; private set; }

        private static readonly string[] TitleKeys = { "mode_invert", "mode_lock", "mode_nopeds", "mode_hideprops", "mode_network", "mode_props", "mode_decal" };
        private static readonly string[] IconFiles = { "Invert.png", "Lock.png", "NoPedestrian.png", "HideProps.png", "Network.png", "Props.png", "Decal.png" };

        /// <summary>A check box drawn as a button: "[x] text".</summary>
        private class Toggle
        {
            public UIButton Button;
            public bool Value;
            public string TextKey;
        }

        /// <summary>One row of a drop-down list (Tag: NetInfo for fence models, DecalEntry for decals).</summary>
        private class PickItem
        {
            public string Title;
            public UITextureAtlas Atlas;
            public string Thumb;
            public object Tag;
            public string Key;   // what identifies the item for favourites
            public bool Fav;
        }

        /// <summary>One pooled row of a drop-down list.</summary>
        private class RowUi
        {
            public UIButton Button;
            public UISprite Icon;
            public UIButton StarButton;
            public UITextureSprite Star;
            public PickItem Item;
            public bool Empty;
        }

        private class PickerUi
        {
            public int Kind;                          // Favorites kind
            public UIComponent Parent;
            public string EmptyText;
            public bool ShowEmpty = true;             // the empty row at the top of the list
            public Func<string, List<PickItem>> GetItems; // argument: search text
            public Action<PickItem> OnPicked;         // null item = the "empty" row
            public UILabel Title;
            public UIButton Header;
            public UISprite Icon;
            public UILabel Name;
            public UIPanel Popup;
            public UITextField Search;
            public UIScrollbar Bar;
            public readonly List<RowUi> Rows = new List<RowUi>();
            public List<PickItem> Entries = new List<PickItem>(); // what the list shows (null = the empty row)
            public int Offset;                        // first visible entry
            public bool SyncBar;
        }

        private UIDragHandle _drag;
        private UIButton[] _modeButtons;
        private UILabel _title;
        private UIPanel _toolsPanel;
        private Toggle _hideHl;
        internal static bool SliderDragging;
        private UILabel _status;
        private UIPanel _actionsBack;
        private float _netAct, _decalAct, _propAct;
        private const float BtnStride = 40f;
        private const float ToggleStride = 38f;
        private bool _built;
        private int _langBuilt;
        private static Vector3 _restorePos;
        private static bool _hasRestorePos;
        private bool _loading;

        private PickerUi _openPopup;

        // network-model line section
        private UIPanel _net, _netRight;
        private float _netHeight, _netLeft;
        private UILabel _netSel, _netNav, _netState;
        private UIButton _netPrev, _netNext, _netAdd, _netRemove, _netClear;
        private PickerUi _netUi;
        private UISlider _nStart, _nEnd, _nLateral, _nLift, _nScale;
        private UITextField _nStartV, _nEndV, _nLateralV, _nLiftV, _nScaleV;
        private Toggle _nFlip, _nCapStart, _nCapEnd;
        private int _netIndex;
        private int _netCount;
        private string _lastNetModel;

        // orientation lock section
        private UIPanel _lock;
        private float _lockHeight;
        private UILabel _lockSel, _lockState;
        private UIButton _lockBtn, _unlockBtn;
        private int _lockVersion = -1;

        // prop line section
        private UIPanel _prop, _propRight;
        private float _propHeight, _propLeft;
        private UILabel _propSel, _propNav, _propState;
        private UIButton _propPrev, _propNext, _propAdd, _propRemove, _propClear;
        private PickerUi _propUi;
        private UISlider _pShiftX, _pStep, _pStart, _pEnd, _pLateral, _pLift, _pAngle, _pScale, _pRand;
        private UITextField _pShiftXV, _pStepV, _pStartV, _pEndV, _pLateralV, _pLiftV;
        private UITextField _pAngleV, _pScaleV, _pRandV;
        private Toggle _pRotate, _pTilt;
        private UIPanel _propRot, _propRest;   // rotation controls (hidden for trees) and everything below them
        private float _propHeightFull, _propHeightTree;
        private int _propIndex;
        private int _propCount;

        // texture path section
        private UIPanel _decal, _decalRight;
        private float _decalHeight, _decalLeft, _decalRestTop, _decalHeightFull, _decalHeightPlane;
        private UILabel _decalSel, _decalNav, _decalState;
        private UISlider _dMul, _dShiftX, _dWidth, _dScale, _dLateral, _dLift, _dStep, _dBox, _dStart, _dEnd;
        private UITextField _dMulV, _dShiftXV, _dWidthV, _dScaleV, _dLateralV, _dLiftV, _dStepV, _dBoxV, _dStartV, _dEndV;
        private UIPanel _decalBox, _decalRest;   // projection size (hidden for a plane) and everything below it
        private PickerUi _decalUi;
        private UISlider[] _dRgba;     // R, G, B, A
        private UILabel[] _dRgbaV;
        private UIPanel _dSwatch;
        private UITextField _dHex;
        private bool _colorSync;
        private UIButton _dAdd, _dAddPlane, _dRemove, _dClear, _decalPrev, _decalNext;
        private int _decalIndex;
        private int _decalCount;
        private string _lastDecalProp;
        private int _lastDecalScale = DecalSettings.DefaultScale;
        private readonly DecalSettings _brush = new DecalSettings(); // the values of the path shown in the controls

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
            width = ToolsWidth;
            height = TopY + 600f;
            backgroundSprite = "MenuPanel2";
            isInteractive = true;
            canFocus = true;

            Vector2 res = UIView.GetAView().GetScreenResolution();
            absolutePosition = new Vector3(Mathf.Max(20f, res.x - (ToolsWidth + PanelWidth * 2f) - 20f), 120f);
            if (_hasRestorePos)
            {
                absolutePosition = _restorePos;
                _hasRestorePos = false;
            }
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

        /// <summary>Delete buttons: the normal button tinted dark red.</summary>
        private static void MakeRed(UIButton b)
        {
            b.color = new Color32(176, 62, 62, 255);
            b.hoveredTextColor = new Color32(255, 255, 255, 255);
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

            slider.eventMouseDown += delegate (UIComponent c, UIMouseEventParameter p) { SliderDragging = true; };
            slider.eventMouseUp += delegate (UIComponent c, UIMouseEventParameter p) { SliderDragging = false; };
            thumb.eventMouseDown += delegate (UIComponent c, UIMouseEventParameter p) { SliderDragging = true; };
            thumb.eventMouseUp += delegate (UIComponent c, UIMouseEventParameter p) { SliderDragging = false; };
            slider.tooltip = Loc.T("tip_reset_value");
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

        private static UIButton MakeSmallButton(UIComponent parent, float x, float y, float w, string text, Action onClick)
        {
            UIButton b = parent.AddUIComponent<UIButton>();
            b.size = new Vector2(w, 30f);
            b.relativePosition = new Vector3(x, y);
            StyleButton(b);
            b.text = text;
            b.textScale = 0.85f;
            b.eventClicked += delegate (UIComponent c, UIMouseEventParameter p) { onClick(); };
            return b;
        }

        private static void UpdateToggle(Toggle t)
        {
            t.Button.text = (t.Value ? "[x]  " : "[  ]  ") + Loc.T(t.TextKey);
            t.Button.state = t.Value ? UIButton.ButtonState.Focused : UIButton.ButtonState.Normal;
        }

        private void SetToggle(Toggle t, bool value)
        {
            t.Value = value;
            UpdateToggle(t);
        }

        /// <summary>A check box (button with "[x]"); onChange is not called while controls are being loaded.</summary>
        private Toggle MakeToggle(UIComponent parent, float x, float y, float w, string textKey, Color32? color, Action<bool> onChange)
        {
            Toggle t = new Toggle();
            t.TextKey = textKey;

            UIButton b = parent.AddUIComponent<UIButton>();
            b.width = w;
            b.height = 30f;
            b.relativePosition = new Vector3(x, y);
            StyleButton(b);
            b.textScale = 0.8f;
            b.textHorizontalAlignment = UIHorizontalAlignment.Left;
            b.textPadding = new RectOffset(10, 0, 0, 0);
            if (color.HasValue)
            {
                Color32 c = color.Value;
                b.textColor = c;
                b.hoveredTextColor = c;
                b.pressedTextColor = c;
                b.focusedTextColor = c;
            }
            t.Button = b;
            b.eventClicked += delegate (UIComponent comp, UIMouseEventParameter e)
            {
                if (_loading) return;
                t.Value = !t.Value;
                UpdateToggle(t);
                onChange(t.Value);
            };
            UpdateToggle(t);
            return t;
        }

        /// <summary>A label, a slider and a value field for whole numbers (with a unit such as %); returns the y of the next block.</summary>
        private float MakeIntRow(UIComponent parent, float y, string labelKey, int min, int max, int reset, string suffix,
                                 out UISlider slider, out UITextField value, Action<int> onValue)
        {
            MakeLabel(parent, Loc.T(labelKey), 12f, y, 0.72f);
            MakeLabel(parent, suffix.Trim(), PanelWidth - 24f, y + 21f, 0.72f);

            UISlider sl = MakeSlider(parent, 12f, y + 20f, PanelWidth - 114f, min, max, reset);
            UITextField fl = null;
            fl = MakeField(parent, PanelWidth - 92f, y + 19f, delegate (float v)
            {
                if (float.IsNaN(v))
                {
                    fl.text = Mathf.RoundToInt(sl.value).ToString();
                    return;
                }
                float target = Mathf.Clamp(Mathf.Round(v), min, max);
                if (Mathf.Approximately(target, sl.value)) fl.text = ((int)target).ToString();
                else sl.value = target; // raises eventValueChanged -> applies it
            }, max);
            fl.tooltip = Loc.F("typehint_int", min, max);
            fl.text = reset.ToString();

            UITextField flCaptured = fl;
            sl.eventValueChanged += delegate (UIComponent c, float v)
            {
                int u = Mathf.RoundToInt(v);
                flCaptured.text = u.ToString();
                onValue(u);
            };

            slider = sl;
            value = fl;
            return y + 44f;
        }

        private UITextField MakeTextBox(UIComponent parent, float x, float y, float w, string tooltip, Action<string> onChanged)
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
            f.horizontalAlignment = UIHorizontalAlignment.Left;
            f.verticalAlignment = UIVerticalAlignment.Middle;
            f.textScale = 0.75f;
            f.padding = new RectOffset(6, 4, 4, 0);
            f.size = new Vector2(w, 22f);
            f.relativePosition = new Vector3(x, y);
            f.text = string.Empty;
            f.tooltip = tooltip;
            f.eventTextChanged += delegate (UIComponent c, string t) { onChanged(t); };
            return f;
        }

        // ---------- building ----------

        private void Build()
        {
            if (_built) return;
            _built = true;
            _langBuilt = Loc.Current;

            _drag = AddUIComponent<UIDragHandle>();
            _drag.width = width;
            _drag.height = 34f;
            _drag.relativePosition = Vector3.zero;
            _drag.target = this;

            _title = MakeLabel(this, "Quay Tools", 12f, 9f, 1.0f);

            int modeCount = QuayTool.ModeCount;
            _modeButtons = new UIButton[modeCount];
            float btn = ToolsWidth - 20f;
            float btnH = 46f;

            // first column: the tools, on a panel of their own
            _toolsPanel = AddUIComponent<UIPanel>();
            _toolsPanel.size = new Vector2(ToolsWidth - 10f, 8f + modeCount * (btnH + 4f));
            _toolsPanel.relativePosition = new Vector3(6f, TopY);
            _toolsPanel.backgroundSprite = "GenericPanel";
            _toolsPanel.color = new Color32(18, 24, 30, 255);
            _toolsPanel.isInteractive = false;

            for (int i = 0; i < modeCount; i++)
            {
                QuayTool.Mode mode = (QuayTool.Mode)i;
                UIButton button = _toolsPanel.AddUIComponent<UIButton>();
                button.width = btn;
                button.height = btnH;
                button.relativePosition = new Vector3(5f, 6f + i * (btnH + 4f));
                StyleButton(button);
                button.isEnabled = QuayTool.IsImplemented(mode);
                button.tooltip = button.isEnabled ? DescriptionFor((QuayTool.Mode)i) : Loc.T(TitleKeys[i]) + Loc.T("soon");

                Texture2D icon = ModPaths.LoadIcon(IconFiles[i]);
                if (icon != null)
                {
                    UITextureSprite sprite = button.AddUIComponent<UITextureSprite>();
                    sprite.texture = icon;
                    sprite.size = new Vector2(32f, 32f);
                    sprite.relativePosition = new Vector3((btn - 32f) * 0.5f, (btnH - 32f) * 0.5f);
                    sprite.isInteractive = false;
                }
                else
                {
                    button.text = (i + 1).ToString();
                }

                button.eventClicked += delegate (UIComponent c, UIMouseEventParameter p)
                {
                    QuayTool tool = QuayTool.Instance;
                    if (tool != null) tool.SetMode(mode);
                    Refresh();
                };

                _modeButtons[i] = button;
            }

            _status = MakeLabel(this, string.Empty, ToolsWidth + 12f, TopY, 0.8f);
            _status.textColor = new Color32(120, 220, 140, 255);
            _status.width = PanelWidth - 24f;

            _actionsBack = AddUIComponent<UIPanel>();
            _actionsBack.backgroundSprite = "GenericPanel";
            _actionsBack.color = new Color32(34, 44, 54, 255);
            _actionsBack.isInteractive = false;
            _actionsBack.isVisible = false;

            BuildNetSection();
            BuildDecalSection();
            BuildLockSection();
            BuildPropSection();
            BuildBar();

            // created after the sections so that no panel lies over it
            _hideHl = MakeToggle(this, ToolsWidth + 10f, TopY, PanelWidth - 20f, "hidehl", null,
                delegate (bool v) { Settings.HideHighlightUi = v; });
            _hideHl.Button.tooltip = Loc.T("hidehl_tip");
            SetToggle(_hideHl, Settings.HideHighlightUi);
        }

        // ---------- network-model line section ----------

        private void BuildNetSection()
        {
            _net = AddUIComponent<UIPanel>();
            _net.width = PanelWidth * 2f;
            _net.relativePosition = new Vector3(ToolsWidth, TopY);
            _net.isVisible = false;

            _netRight = _net.AddUIComponent<UIPanel>();
            _netRight.width = PanelWidth;
            _netRight.relativePosition = new Vector3(PanelWidth, 0f);

            // ---- left column: lines, add, model chooser, direction, closing pieces, remove buttons
            float y = 0f;
            _netSel = MakeLabel(_net, string.Empty, 12f, y, 0.85f);
            y += 24f;

            _netPrev = MakeSmallButton(_net, 10f, y, 34f, "<", delegate { StepNet(-1); });
            _netNav = MakeLabel(_net, string.Empty, 54f, y + 8f, 0.8f);
            _netNext = MakeSmallButton(_net, PanelWidth - 44f, y, 34f, ">", delegate { StepNet(1); });
            y += 36f;

            _netAdd = _net.AddUIComponent<UIButton>();
            _netAdd.width = PanelWidth - 20f;
            _netAdd.height = 32f;
            _netAdd.relativePosition = new Vector3(10f, y);
            StyleButton(_netAdd);
            _netAdd.text = "+  " + Loc.T("line_add");
            _netAdd.textScale = 0.85f;
            _netAdd.eventClicked += delegate (UIComponent c, UIMouseEventParameter p)
            {
                QuayTool tool = QuayTool.Instance;
                if (tool == null) return;
                ClosePopup();
                _netIndex = int.MaxValue; // show the new (last) line once it exists
                NetLine template = new NetLine();
                template.Model = _lastNetModel;
                tool.AddNetLine(template);
            };
            y += BtnStride;

            MakeLabel(_net, Loc.T("line_choose"), 12f, y, 0.85f);
            y += 20f;

            _netUi = new PickerUi();
            _netUi.Parent = _net;
            _netUi.EmptyText = Loc.T("line_none");
            _netUi.Kind = Favorites.Fences;
            _netUi.GetItems = delegate (string text)
            {
                List<PickItem> items = new List<PickItem>();
                List<FenceEntry> list = FenceCatalog.Entries;
                for (int i = 0; i < list.Count; i++)
                {
                    if (!Matches(text, list[i].Title, list[i].Info.name)) continue;
                    PickItem it = new PickItem();
                    it.Title = list[i].Title;
                    it.Atlas = list[i].Info.m_Atlas;
                    it.Thumb = list[i].Info.m_Thumbnail;
                    it.Tag = list[i].Info;
                    it.Key = list[i].Info.name;
                    items.Add(it);
                }
                return items;
            };
            _netUi.OnPicked = OnNetPicked;
            BuildHeader(_net, _netUi, y);
            y += 84f;

            _nFlip = MakeToggle(_net, 10f, y, PanelWidth - 20f, "nflip", null,
                delegate (bool v) { OnNetValue("flip", delegate (NetLine l) { l.Flip = v; }); });
            y += ToggleStride;
            _nCapStart = MakeToggle(_net, 10f, y, PanelWidth - 20f, "ncapstart", (Color32)QuayTool.StartColor,
                delegate (bool v) { OnNetValue("capstart", delegate (NetLine l) { l.CapStart = v; }); });
            y += ToggleStride;
            _nCapEnd = MakeToggle(_net, 10f, y, PanelWidth - 20f, "ncapend", (Color32)QuayTool.EndColor,
                delegate (bool v) { OnNetValue("capend", delegate (NetLine l) { l.CapEnd = v; }); });
            y += ToggleStride + 14f;
            _netAct = y;

            _netRemove = _net.AddUIComponent<UIButton>();
            _netRemove.width = PanelWidth - 20f;
            _netRemove.height = 32f;
            _netRemove.relativePosition = new Vector3(10f, y);
            StyleButton(_netRemove);
            _netRemove.text = Loc.T("line_remove");
            _netRemove.textScale = 0.85f;
            MakeRed(_netRemove);
            _netRemove.eventClicked += delegate (UIComponent c, UIMouseEventParameter p)
            {
                QuayTool tool = QuayTool.Instance;
                if (tool != null) tool.RemoveNetLine(_netIndex);
            };
            y += BtnStride;

            _netClear = _net.AddUIComponent<UIButton>();
            _netClear.width = PanelWidth - 20f;
            _netClear.height = 32f;
            _netClear.relativePosition = new Vector3(10f, y);
            StyleButton(_netClear);
            _netClear.text = Loc.T("line_clear");
            _netClear.textScale = 0.85f;
            MakeRed(_netClear);
            _netClear.eventClicked += delegate (UIComponent c, UIMouseEventParameter p)
            {
                QuayTool tool = QuayTool.Instance;
                if (tool == null) return;
                ClosePopup();
                tool.ClearNetLines();
            };
            y += BtnStride;
            _netLeft = y; // the undo / redo / reset bar follows here

            // ---- right column: all the values
            float y2 = 0f;
            y2 = MakeValueRow(_netRight, y2, "nlateral", -NetLine.MaxOffset, NetLine.MaxOffset, 0f, out _nLateral, out _nLateralV,
                delegate (int u) { OnNetValue("lateral", delegate (NetLine l) { l.Lateral = u; }); });
            y2 = MakeValueRow(_netRight, y2, "nlift", -NetLine.MaxOffset, NetLine.MaxOffset, 0f, out _nLift, out _nLiftV,
                delegate (int u) { OnNetValue("lift", delegate (NetLine l) { l.Lift = u; }); });
            y2 = MakeValueRow(_netRight, y2, "nstart", -NetLine.MaxShift, 0f, 0f, out _nStart, out _nStartV,
                delegate (int u) { OnNetValue("start", delegate (NetLine l) { l.StartShift = u; }); });
            y2 = MakeValueRow(_netRight, y2, "nend", -NetLine.MaxShift, 0f, 0f, out _nEnd, out _nEndV,
                delegate (int u) { OnNetValue("end", delegate (NetLine l) { l.EndShift = u; }); });
            y2 = MakeIntRow(_netRight, y2, "nscale", NetLine.ScaleMin, NetLine.ScaleMax, NetLine.ScaleDefault, " %", out _nScale, out _nScaleV,
                delegate (int u) { OnNetValue("scale", delegate (NetLine l) { l.Scale = u; }); });

            _netState = MakeLabel(_netRight, string.Empty, 12f, y2, 0.75f);
            y2 += 22f;

            _netHeight = Mathf.Max(_netLeft, y2) + 4f;

            // the list is created last so that it is drawn above the controls below its button
            BuildPopup(_netUi);
        }

        private void OnNetValue(string key, Action<NetLine> apply)
        {
            if (_loading) return;
            QuayTool tool = QuayTool.Instance;
            if (tool != null) tool.EditNetLine(_netIndex, key, apply);
        }

        private void OnNetPicked(PickItem item)
        {
            string name = item == null ? null : ((NetInfo)item.Tag).name;
            if (name != null) _lastNetModel = name;
            QuayTool tool = QuayTool.Instance;
            if (tool != null) tool.EditNetLine(_netIndex, "model", delegate (NetLine l) { l.Model = name; });
        }

        private void StepNet(int direction)
        {
            if (_netCount < 2) return;
            _netIndex += direction;
            if (_netIndex >= _netCount) _netIndex = 0;
            if (_netIndex < 0) _netIndex = _netCount - 1;
            ClosePopup();
            LoadNetFromSelection();
        }

        private void ShowNetHeader(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                SetHeaderItem(_netUi, null);
                return;
            }

            NetInfo info = FenceCatalog.Find(name);
            if (info == null)
            {
                PickItem item = new PickItem();
                item.Title = name + Loc.T("missing");
                SetHeaderItem(_netUi, item);
                return;
            }
            SetHeader(_netUi, info);
        }

        /// <summary>Shows the current line of the first selected segment that has any.</summary>
        private void LoadNetFromSelection()
        {
            if (_net == null) return;

            QuayTool tool = QuayTool.Instance;
            int count = tool == null ? 0 : tool.Selection.Count;

            _loading = true;
            try
            {
                _netSel.text = Loc.T("selected") + count;

                NetLineSet set = null;
                int have = 0;
                for (int i = 0; i < count; i++)
                {
                    NetLineSet s;
                    if (NetLineStore.TryGet(tool.Selection[i], out s) && s.Lines.Count > 0)
                    {
                        have++;
                        if (set == null) set = s;
                    }
                }

                _netCount = set == null ? 0 : set.Lines.Count;
                _netIndex = _netCount == 0 ? 0 : Mathf.Clamp(_netIndex, 0, _netCount - 1);
                if (tool != null) tool.ActiveLine = _netCount == 0 ? -1 : _netIndex;
                NetLine l = set != null ? set.Lines[_netIndex] : new NetLine();

                _netNav.text = _netCount == 0 ? Loc.T("line_nolines") : Loc.F("line_nav", _netIndex + 1, _netCount);

                _nStart.value = l.StartShift;
                _nEnd.value = l.EndShift;
                _nLateral.value = l.Lateral;
                _nLift.value = l.Lift;
                _nScale.value = l.Scale;
                _nStartV.text = FormatOffset(_nStart.value);
                _nEndV.text = FormatOffset(_nEnd.value);
                _nLateralV.text = FormatOffset(_nLateral.value);
                _nLiftV.text = FormatOffset(_nLift.value);
                _nScaleV.text = l.Scale.ToString();
                SetToggle(_nFlip, l.Flip);
                SetToggle(_nCapStart, l.CapStart);
                SetToggle(_nCapEnd, l.CapEnd);
                ShowNetHeader(l.Model);

                bool any = count > 0, has = _netCount > 0;
                _netAdd.isEnabled = any;
                _netPrev.isEnabled = has && _netCount > 1;
                _netNext.isEnabled = has && _netCount > 1;
                _netRemove.isEnabled = has;
                _netClear.isEnabled = has;
                _netUi.Header.isEnabled = has;
                UISlider[] sliders = { _nStart, _nEnd, _nLateral, _nLift, _nScale };
                for (int i = 0; i < sliders.Length; i++) sliders[i].isEnabled = has;
                _nFlip.Button.isEnabled = has;
                _nCapStart.Button.isEnabled = has;
                _nCapEnd.Button.isEnabled = has;
                if (_resetBtn != null) _resetBtn.isEnabled = has;

                _netState.text = Loc.F("line_state", have, count);
            }
            finally
            {
                _loading = false;
            }
        }

        /// <summary>The button that shows the chosen entry (icon + name) and opens the drop-down list.</summary>
        private void BuildHeader(UIComponent parent, PickerUi ui, float y)
        {
            ui.Header = parent.AddUIComponent<UIButton>();
            ui.Header.width = PanelWidth - 20f;
            ui.Header.height = 76f;
            ui.Header.relativePosition = new Vector3(10f, y);
            StyleButton(ui.Header);

            // frame of the picture (an empty frame when there is no picture)
            UIPanel frame = ui.Header.AddUIComponent<UIPanel>();
            frame.size = new Vector2(68f, 68f);
            frame.relativePosition = new Vector3(4f, 4f);
            frame.backgroundSprite = "GenericPanel";
            frame.color = new Color32(14, 18, 22, 255);
            frame.isInteractive = false;

            ui.Icon = ui.Header.AddUIComponent<UISprite>();
            ui.Icon.size = new Vector2(62f, 62f);
            ui.Icon.relativePosition = new Vector3(7f, 7f);
            ui.Icon.isInteractive = false;
            ui.Icon.isVisible = false;

            ui.Name = ui.Header.AddUIComponent<UILabel>();
            ui.Name.textScale = 0.8f;
            ui.Name.autoSize = false;
            ui.Name.wordWrap = true;
            ui.Name.width = PanelWidth - 20f - 90f;
            ui.Name.height = 56f;
            ui.Name.relativePosition = new Vector3(82f, 10f);
            ui.Name.isInteractive = false;
            ui.Name.text = ui.EmptyText;

            PickerUi captured = ui;
            ui.Header.eventClicked += delegate (UIComponent c, UIMouseEventParameter p) { TogglePopup(captured); };
        }

        private static bool Matches(string text, string title, string name)
        {
            string t = (text ?? string.Empty).Trim();
            if (t.Length == 0) return true;
            return title.IndexOf(t, StringComparison.OrdinalIgnoreCase) >= 0 || (name != null && name.IndexOf(t, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private static void LoadStars()
        {
            if (_starOff == null) _starOff = ModPaths.LoadIcon("Star.png");
            if (_starOn == null) _starOn = ModPaths.LoadIcon("StarOn.png");
        }

        /// <summary>
        /// The list is virtual: a search box, MaxPopupRows pooled rows and a scroll bar. The popup is a child of the
        /// section it belongs to and is created last in it, so it is drawn above the controls below its button.
        /// </summary>
        private void BuildPopup(PickerUi ui)
        {
            LoadStars();
            float top = ui.Header.relativePosition.y + ui.Header.height + 2f;

            UIPanel popup = ui.Parent.AddUIComponent<UIPanel>();
            popup.width = PanelWidth - 20f;
            popup.height = ListTop + MaxPopupRows * RowStride + 6f;
            popup.relativePosition = new Vector3(10f, top);
            popup.backgroundSprite = "MenuPanel2";
            PickerUi wheelPopup = ui;
            popup.eventMouseWheel += delegate (UIComponent c, UIMouseEventParameter e) { OnPopupWheel(wheelPopup, e); };
            popup.isVisible = false;
            ui.Popup = popup;

            PickerUi captured = ui;
            ui.Search = MakeTextBox(popup, 6f, 5f, popup.width - 12f, Loc.T("search_tip"), delegate (string t)
            {
                if (_openPopup == captured) ApplyFilter(captured, true);
            });

            for (int i = 0; i < MaxPopupRows; i++) CreateRow(ui, i);

            UIScrollbar bar = popup.AddUIComponent<UIScrollbar>();
            bar.width = 12f;
            bar.height = MaxPopupRows * RowStride;
            bar.orientation = UIOrientation.Vertical;
            bar.pivot = UIPivotPoint.TopLeft;
            bar.relativePosition = new Vector3(popup.width - 16f, ListTop);
            bar.minValue = 0f;
            bar.maxValue = MaxPopupRows;
            bar.scrollSize = MaxPopupRows;
            bar.incrementAmount = 1f;
            bar.value = 0f;

            UISlicedSprite track = bar.AddUIComponent<UISlicedSprite>();
            track.spriteName = "ScrollbarTrack";
            track.relativePosition = Vector3.zero;
            track.size = bar.size;
            bar.trackObject = track;

            UISlicedSprite thumb = track.AddUIComponent<UISlicedSprite>();
            thumb.spriteName = "ScrollbarThumb";
            thumb.width = 10f;
            bar.thumbObject = thumb;

            bar.eventValueChanged += delegate (UIComponent c, float v)
            {
                if (captured.SyncBar) return;
                captured.Offset = Mathf.RoundToInt(v);
                RefreshRows(captured);
            };
            bar.isVisible = false;
            ui.Bar = bar;
        }

        private void CreateRow(PickerUi ui, int index)
        {
            RowUi r = new RowUi();
            float w = ui.Popup.width - 8f - 16f;

            UIButton row = ui.Popup.AddUIComponent<UIButton>();
            row.width = w;
            row.height = RowHeight;
            row.relativePosition = new Vector3(4f, ListTop + index * RowStride);
            StyleButton(row);
            row.textScale = 0.8f;
            row.textHorizontalAlignment = UIHorizontalAlignment.Left;
            row.textVerticalAlignment = UIVerticalAlignment.Middle;
            row.textPadding = new RectOffset(46, 0, 0, 0);
            r.Button = row;

            r.Icon = row.AddUIComponent<UISprite>();
            r.Icon.size = new Vector2(30f, 30f);
            r.Icon.relativePosition = new Vector3(8f, 3f);
            r.Icon.isInteractive = false;
            r.Icon.isVisible = false;

            r.StarButton = row.AddUIComponent<UIButton>();
            r.StarButton.size = new Vector2(26f, 26f);
            r.StarButton.relativePosition = new Vector3(w - 32f, 5f);
            r.StarButton.tooltip = Loc.T("star_tip");
            r.Star = r.StarButton.AddUIComponent<UITextureSprite>();
            r.Star.size = new Vector2(22f, 22f);
            r.Star.relativePosition = new Vector3(2f, 2f);
            r.Star.isInteractive = false;

            PickerUi captured = ui;
            r.StarButton.eventClicked += delegate (UIComponent c, UIMouseEventParameter p)
            {
                p.Use();
                if (r.Item == null || string.IsNullOrEmpty(r.Item.Key)) return;
                Favorites.Toggle(captured.Kind, r.Item.Key);
                ApplyFilter(captured, false);
            };

            row.eventMouseWheel += delegate (UIComponent c, UIMouseEventParameter e) { OnPopupWheel(captured, e); };
            r.StarButton.eventMouseWheel += delegate (UIComponent c, UIMouseEventParameter e) { OnPopupWheel(captured, e); };
            row.eventClicked += delegate (UIComponent c, UIMouseEventParameter p)
            {
                PickItem picked = r.Empty ? null : r.Item;
                SetHeaderItem(captured, picked);
                ClosePopup();
                if (!_loading) captured.OnPicked(picked);
            };

            ui.Rows.Add(r);
        }

        /// <summary>Rebuilds the list for the current search text: favourites first, then the rest (the empty row only without a search).</summary>
        private void ApplyFilter(PickerUi ui, bool resetOffset)
        {
            List<PickItem> list = ui.GetItems(ui.Search.text);
            List<PickItem> favs = new List<PickItem>(), rest = new List<PickItem>();
            for (int i = 0; i < list.Count; i++)
            {
                list[i].Fav = Favorites.Is(ui.Kind, list[i].Key);
                if (list[i].Fav) favs.Add(list[i]); else rest.Add(list[i]);
            }

            ui.Entries.Clear();
            if (ui.ShowEmpty && string.IsNullOrEmpty(ui.Search.text.Trim())) ui.Entries.Add(null);
            ui.Entries.AddRange(favs);
            ui.Entries.AddRange(rest);

            int total = ui.Entries.Count;
            int visible = Mathf.Min(MaxPopupRows, total);
            int maxOffset = Mathf.Max(0, total - MaxPopupRows);
            ui.Offset = resetOffset ? 0 : Mathf.Clamp(ui.Offset, 0, maxOffset);

            ui.Popup.height = ListTop + Mathf.Max(visible, 1) * RowStride + 6f;
            ui.SyncBar = true;
            ui.Bar.minValue = 0f;
            ui.Bar.maxValue = Mathf.Max(total, MaxPopupRows);
            ui.Bar.scrollSize = MaxPopupRows;
            ui.Bar.height = Mathf.Max(visible, 1) * RowStride;
            if (ui.Bar.trackObject != null) ui.Bar.trackObject.height = ui.Bar.height;
            ui.Bar.value = ui.Offset;
            ui.Bar.isVisible = total > MaxPopupRows;
            ui.SyncBar = false;

            RefreshRows(ui);
            UpdateHeight();
        }

        private static string Shorten(string title)
        {
            return title.Length > 30 ? title.Substring(0, 29) + "…" : title;
        }

        private void RefreshRows(PickerUi ui)
        {
            for (int i = 0; i < ui.Rows.Count; i++)
            {
                RowUi r = ui.Rows[i];
                int index = ui.Offset + i;
                if (index >= ui.Entries.Count)
                {
                    r.Button.isVisible = false;
                    r.Item = null;
                    continue;
                }

                PickItem it = ui.Entries[index];
                r.Item = it;
                r.Empty = it == null;
                r.Button.isVisible = true;

                string title = it == null ? ui.EmptyText : it.Title;
                r.Button.text = Shorten(title);
                r.Button.tooltip = title;

                bool icon = it != null && it.Atlas != null && !string.IsNullOrEmpty(it.Thumb);
                if (icon)
                {
                    r.Icon.atlas = it.Atlas;
                    r.Icon.spriteName = it.Thumb;
                }
                r.Icon.isVisible = icon;

                Texture2D star = it != null && it.Fav ? _starOn : _starOff;
                r.StarButton.isVisible = it != null && !string.IsNullOrEmpty(it.Key) && star != null;
                if (star != null) r.Star.texture = star;
            }
        }

        private void OnPopupWheel(PickerUi ui, UIMouseEventParameter e)
        {
            int maxOffset = Mathf.Max(0, ui.Entries.Count - MaxPopupRows);
            if (maxOffset > 0)
            {
                int step = e.wheelDelta > 0f ? -2 : 2;
                ui.Offset = Mathf.Clamp(ui.Offset + step, 0, maxOffset);
                ui.SyncBar = true;
                ui.Bar.value = ui.Offset;
                ui.SyncBar = false;
                RefreshRows(ui);
            }
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
            ui.Search.text = string.Empty;
            _openPopup = ui;
            ui.Popup.isVisible = true;
            ApplyFilter(ui, true);

            // the whole section (and the list inside it) is drawn above the undo / redo / reset bar
            ui.Parent.BringToFront();
            ui.Popup.BringToFront();
            ui.Search.Focus();
            UpdateHeight();
        }

        private void ClosePopup()
        {
            if (_openPopup == null) return;
            _openPopup.Popup.isVisible = false;
            _openPopup.Bar.isVisible = false;
            _openPopup.Search.Unfocus();
            _openPopup = null;
            if (_bar != null) _bar.BringToFront();
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

            ui.Name.text = item.Title.Length > 60 ? item.Title.Substring(0, 59) + "…" : item.Title;

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

        // ---------- tool -> controls ----------

        private void OnSelectionChanged()
        {
            ClosePopup();
            LoadFromSelection();
        }

        /// <summary>Shows the settings of the first selected segment in the controls of the current mode.</summary>
        private void LoadFromSelection()
        {
            if (!_built || _net == null) return;

            QuayTool current = QuayTool.Instance;
            if (current == null) return;

            switch (current.CurrentMode)
            {
                case QuayTool.Mode.AddNetwork: LoadNetFromSelection(); break;
                case QuayTool.Mode.Decal: LoadDecalFromSelection(); break;
                case QuayTool.Mode.PropLine: LoadPropFromSelection(); break;
                case QuayTool.Mode.Lock:
                case QuayTool.Mode.RemovePedestrian:
                case QuayTool.Mode.HideProps: LoadLockFromSelection(); break;
            }
        }

        // ---------- refresh / layout ----------

        /// <summary>Opens or closes the columns right of the tool column (0 = only the tools).</summary>
        private void SetColumns(int columns)
        {
            float target = ToolsWidth + PanelWidth * columns;
            if (!Mathf.Approximately(width, target))
            {
                if (target > width)
                {
                    Vector2 res = UIView.GetAView().GetScreenResolution();
                    Vector3 p = absolutePosition;
                    if (p.x + target > res.x - 10f)
                    {
                        p.x = Mathf.Max(10f, res.x - target - 10f);
                        absolutePosition = p;
                    }
                }
                width = target;
                if (_drag != null) _drag.width = target;
            }

            // the title has to fit above the narrow tool column: two lines there, one line when the window is wider
            if (_title != null)
            {
                if (columns == 0)
                {
                    _title.autoSize = false;
                    _title.textAlignment = UIHorizontalAlignment.Center;
                    _title.text = "Quay\nTools";
                    _title.textScale = 0.8f;
                    _title.width = ToolsWidth;
                    _title.height = 40f;
                    _title.relativePosition = new Vector3(0f, 6f);
                }
                else
                {
                    _title.text = "Quay Tools";
                    _title.autoSize = true;
                    _title.textAlignment = UIHorizontalAlignment.Left;
                    _title.textScale = 1.0f;
                    _title.relativePosition = new Vector3(12f, 9f);
                }
            }
        }

        public void Refresh()
        {
            if (!_built) return;

            QuayTool tool = QuayTool.Instance;
            QuayTool.Mode current = tool != null ? tool.CurrentMode : QuayTool.Mode.None;

            for (int i = 0; i < _modeButtons.Length; i++)
            {
                if (!_modeButtons[i].isEnabled) continue;
                _modeButtons[i].state = (int)current == i ? UIButton.ButtonState.Focused : UIButton.ButtonState.Normal;
            }

            bool addMode = current == QuayTool.Mode.AddNetwork;
            bool decalMode = current == QuayTool.Mode.Decal;
            bool propMode = current == QuayTool.Mode.PropLine;
            bool lockMode = current == QuayTool.Mode.Lock || current == QuayTool.Mode.RemovePedestrian || current == QuayTool.Mode.HideProps;
            bool select = addMode || decalMode || propMode || lockMode;

            _net.isVisible = addMode;
            _decal.isVisible = decalMode;
            _prop.isVisible = propMode;
            _lock.isVisible = lockMode;
            _bar.isVisible = select;
            ClosePopup();
            if (select) LoadFromSelection();
            UpdateBar();
            UpdateHeight();
        }

        private static string DescriptionFor(QuayTool.Mode current)
        {
            string description = Loc.T(TitleKeys[(int)current]) + ": " + QuayTool.HintFor(current);
            if (current == QuayTool.Mode.Lock) description += "\n\n" + Loc.T("lock_note");
            else if (current == QuayTool.Mode.RemovePedestrian) description += "\n\n" + Loc.T("nop_note");
            else if (current == QuayTool.Mode.HideProps) description += "\n\n" + Loc.T("hp_note");
            return WrapText(description, 64);
        }

        /// <summary>Breaks a text into lines of about the given width (the tooltip of the game does not wrap long lines).</summary>
        private static string WrapText(string text, int width)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            string[] paragraphs = text.Split('\n');
            for (int p = 0; p < paragraphs.Length; p++)
            {
                if (p > 0) sb.Append('\n');
                string[] words = paragraphs[p].Split(' ');
                int len = 0;
                for (int w = 0; w < words.Length; w++)
                {
                    if (len > 0 && len + 1 + words[w].Length > width)
                    {
                        sb.Append('\n');
                        len = 0;
                    }
                    else if (len > 0)
                    {
                        sb.Append(' ');
                        len++;
                    }
                    sb.Append(words[w]);
                    len += words[w].Length;
                }
            }
            return sb.ToString();
        }

        private static bool SelectMode(QuayTool.Mode m)
        {
            return m == QuayTool.Mode.AddNetwork || m == QuayTool.Mode.Decal || m == QuayTool.Mode.PropLine ||
                   m == QuayTool.Mode.Lock || m == QuayTool.Mode.RemovePedestrian || m == QuayTool.Mode.HideProps;
        }

        private static bool SliderMode(QuayTool.Mode m)
        {
            return m == QuayTool.Mode.AddNetwork || m == QuayTool.Mode.Decal || m == QuayTool.Mode.PropLine;
        }

        private float LeftOf(QuayTool.Mode m)
        {
            return m == QuayTool.Mode.AddNetwork ? _netLeft : m == QuayTool.Mode.Decal ? _decalLeft : m == QuayTool.Mode.PropLine ? _propLeft : _lockHeight;
        }

        private float RightOf(QuayTool.Mode m)
        {
            return m == QuayTool.Mode.AddNetwork ? _netHeight : m == QuayTool.Mode.Decal ? _decalHeightFull : m == QuayTool.Mode.PropLine ? _propHeightFull : _lockHeight;
        }

        private const float BarGap = 2f, BarH = 38f, StatusH = 22f;

        /// <summary>One window height for the tools with values (the largest of them); the other tools take what they need.</summary>
        private float SliderToolsHeight()
        {
            float best = 0f;
            for (int i = 0; i < QuayTool.ModeCount; i++)
            {
                QuayTool.Mode m = (QuayTool.Mode)i;
                if (!SliderMode(m)) continue;
                best = Mathf.Max(best, TopY + LeftOf(m) + BarGap + BarH + 6f + StatusH + 10f);
                best = Mathf.Max(best, TopY + RightOf(m) + 52f);
            }
            return best;
        }

        /// <summary>
        /// Columns: 1 = tools (always), 2 = the controls of the tool with the undo / redo / reset bar and the status, 3 = the
        /// sliders (values). Empty columns are closed: right after the activation only the tools are shown; Invert shows the
        /// second column only while it has something to say. The description of a tool is its tooltip.
        /// </summary>
        private void UpdateHeight()
        {
            if (!_built) return;

            QuayTool tool = QuayTool.Instance;
            QuayTool.Mode mode = tool != null ? tool.CurrentMode : QuayTool.Mode.None;
            bool addMode = _net != null && _net.isVisible;
            bool decalMode = _decal != null && _decal.isVisible;
            bool propMode = _prop != null && _prop.isVisible;
            bool lockMode = _lock != null && _lock.isVisible;
            bool select = addMode || decalMode || propMode || lockMode;
            bool sliders = addMode || decalMode || propMode;
            bool hasStatus = _status != null && !string.IsNullOrEmpty(_status.text);

            int columns = sliders ? 2 : select ? 1 : (mode == QuayTool.Mode.Invert && hasStatus) ? 1 : 0;
            SetColumns(columns);

            float section = addMode ? _netHeight : decalMode ? _decalHeight : propMode ? _propHeight : lockMode ? _lockHeight : 0f;
            float left = addMode ? _netLeft : decalMode ? _decalLeft : propMode ? _propLeft : section;
            float act = addMode ? _netAct : decalMode ? _decalAct : propMode ? _propAct : -1f;

            if (select)
            {
                UIPanel active = addMode ? _net : decalMode ? _decal : propMode ? _prop : _lock;
                active.height = section;
            }

            float need = TopY + _toolsPanel.height + 8f;
            if (select) need = Mathf.Max(need, TopY + left + BarGap + BarH + 6f + StatusH + 10f);
            if (sliders) need = Mathf.Max(need, SliderToolsHeight());
            if (select && _openPopup != null)
            {
                float popupBottom = TopY + _openPopup.Popup.relativePosition.y + _openPopup.Popup.height + 10f;
                need = Mathf.Max(need, popupBottom);
            }
            height = need;

            float x = ToolsWidth;
            _hideHl.Button.isVisible = sliders;
            _actionsBack.isVisible = select;
            _status.isVisible = columns > 0;

            if (!select)
            {
                _status.relativePosition = new Vector3(x + 12f, TopY + 4f);
                return;
            }

            // the block with the delete buttons and the undo / redo / reset bar sits at the very bottom of the window
            float oldBarY = TopY + left + BarGap;
            float barY = Mathf.Max(oldBarY, height - 10f - 4f - BarH); // the lower edge of the block is level with the lower edge of the highlight switch (height - 10)
            float delta = barY - oldBarY;
            _bar.relativePosition = new Vector3(x, barY);

            UIButton first = addMode ? _netRemove : decalMode ? _dRemove : propMode ? _propRemove : null;
            UIButton second = addMode ? _netClear : decalMode ? _dClear : propMode ? _propClear : null;
            MoveDown(first, delta);
            MoveDown(second, delta);

            // the delete / undo / redo / reset buttons sit on a lighter box of their own
            float backTop = (act >= 0f ? TopY + act - 6f : oldBarY - 4f) + delta;
            _actionsBack.relativePosition = new Vector3(x + 4f, backTop);
            _actionsBack.size = new Vector2(PanelWidth - 8f, barY + BarH + 4f - backTop);

            _status.relativePosition = new Vector3(x + 12f, backTop - 20f); // the text about undone actions sits above the block

            // the highlight switch: bottom of the third column, only where there are sliders
            _hideHl.Button.relativePosition = new Vector3(x + PanelWidth + 10f, height - 40f);
            if (_openPopup == null) _hideHl.Button.BringToFront();
        }

        private readonly Dictionary<UIComponent, float> _baseY = new Dictionary<UIComponent, float>();

        /// <summary>Puts a button at its own place in the section plus delta (its place without a shift is remembered).</summary>
        private void MoveDown(UIComponent c, float delta)
        {
            if (c == null) return;
            float baseY;
            if (!_baseY.TryGetValue(c, out baseY))
            {
                baseY = c.relativePosition.y;
                _baseY[c] = baseY;
            }
            c.relativePosition = new Vector3(c.relativePosition.x, baseY + delta);
        }

        // ---------- texture path section ----------

        /// <summary>A label, a slider and a value field (metres) in one block; the slider ends right before the value field. Returns the y of the next block.</summary>
        private float MakeValueRow(UIComponent parent, float y, string labelKey, float min, float max, float reset,
                                   out UISlider slider, out UITextField field, Action<int> onValue, string unit = null)
        {
            UISlider sl = MakeSlider(parent, 12f, y + 20f, PanelWidth - 114f, min, max, reset);
            UITextField fl = null;
            MakeLabel(parent, Loc.T(labelKey), 12f, y, 0.72f);
            MakeLabel(parent, unit != null ? unit : Loc.T("meter").Trim(), PanelWidth - 24f, y + 21f, 0.72f);
            fl = MakeField(parent, PanelWidth - 92f, y + 19f, delegate (float m) { OnFieldFor(sl, fl, m); }, Mathf.Max(Mathf.Abs(min), Mathf.Abs(max)) * FenceStore.Unit);
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
            _decal.width = PanelWidth * 2f;
            _decal.relativePosition = new Vector3(ToolsWidth, TopY);
            _decal.isVisible = false;

            _decalRight = _decal.AddUIComponent<UIPanel>();
            _decalRight.width = PanelWidth;
            _decalRight.relativePosition = new Vector3(PanelWidth, 0f);

            // ---- left column: paths, add (decal / plane), texture chooser, remove buttons
            float y = 0f;
            _decalSel = MakeLabel(_decal, string.Empty, 12f, y, 0.85f);
            y += 24f;

            _decalPrev = MakeSmallButton(_decal, 10f, y, 34f, "<", delegate { StepDecal(-1); });
            _decalNav = MakeLabel(_decal, string.Empty, 54f, y + 8f, 0.8f);
            _decalNext = MakeSmallButton(_decal, PanelWidth - 44f, y, 34f, ">", delegate { StepDecal(1); });
            y += 36f;

            // the way of drawing is chosen when the path is added and stays with it
            float half = (PanelWidth - 20f - 6f) * 0.5f;
            _dAdd = MakeAddButton(10f, y, half, "tpath_add_decal", false);
            _dAddPlane = MakeAddButton(10f + half + 6f, y, half, "tpath_add_plane", true);
            _dAddPlane.tooltip = Loc.T("dstrip_tip");
            y += BtnStride;

            // texture chooser
            _decalUi = new PickerUi();
            _decalUi.Parent = _decal;
            _decalUi.EmptyText = Loc.T("decal_solid");
            _decalUi.Kind = Favorites.Decals;
            _decalUi.GetItems = delegate (string text)
            {
                List<PickItem> items = new List<PickItem>();
                List<DecalEntry> list = DecalCatalog.Entries;
                for (int i = 0; i < list.Count; i++)
                {
                    if (!Matches(text, list[i].Title, list[i].Info.name)) continue;
                    PickItem it = new PickItem();
                    it.Title = list[i].Title;
                    it.Atlas = list[i].Info.m_Atlas;
                    it.Thumb = list[i].Info.m_Thumbnail;
                    it.Tag = list[i];
                    it.Key = list[i].Info.name;
                    items.Add(it);
                }
                return items;
            };
            _decalUi.OnPicked = OnDecalPicked;

            MakeLabel(_decal, Loc.T("dprop"), 12f, y, 0.85f);
            y += 20f;
            BuildHeader(_decal, _decalUi, y);
            y += 84f;

            y += 10f;
            _decalAct = y;
            _dRemove = _decal.AddUIComponent<UIButton>();
            _dRemove.width = PanelWidth - 20f;
            _dRemove.height = 32f;
            _dRemove.relativePosition = new Vector3(10f, y);
            StyleButton(_dRemove);
            _dRemove.text = Loc.T("tpath_remove");
            _dRemove.textScale = 0.85f;
            MakeRed(_dRemove);
            _dRemove.eventClicked += delegate (UIComponent c, UIMouseEventParameter p)
            {
                QuayTool tool = QuayTool.Instance;
                if (tool != null) tool.RemoveDecal(_decalIndex);
            };
            y += BtnStride;

            _dClear = _decal.AddUIComponent<UIButton>();
            _dClear.width = PanelWidth - 20f;
            _dClear.height = 32f;
            _dClear.relativePosition = new Vector3(10f, y);
            StyleButton(_dClear);
            _dClear.text = Loc.T("tpath_clear");
            _dClear.textScale = 0.85f;
            MakeRed(_dClear);
            _dClear.eventClicked += delegate (UIComponent c, UIMouseEventParameter p)
            {
                QuayTool tool = QuayTool.Instance;
                if (tool == null) return;
                ClosePopup();
                tool.ClearDecals();
            };
            y += BtnStride;
            _decalLeft = y; // the undo / redo / reset bar follows here

            // ---- right column: all the values
            float y2 = 0f;
            y2 = MakeValueRow(_decalRight, y2, "dwidth", DecalStore.MinWidth, DecalStore.MaxWidth, DecalSettings.DefaultWidth, out _dWidth, out _dWidthV,
                delegate (int u) { OnDecalValue("width", delegate (DecalSettings d) { d.Width = u; }); });
            y2 = MakeValueRow(_decalRight, y2, "dscale", DecalStore.MinScale, DecalStore.MaxScale, DecalSettings.DefaultScale, out _dScale, out _dScaleV,
                delegate (int u) { OnDecalValue("scale", delegate (DecalSettings d) { d.Scale = u; }); });
            y2 = MakeValueRow(_decalRight, y2, "dstep", 0, DecalStore.MaxStep, 0f, out _dStep, out _dStepV,
                delegate (int u) { OnDecalValue("step", delegate (DecalSettings d) { d.Step = u; }); });

            // the projection size only matters for placed decals: hidden for a plane, the controls below move up
            _decalBox = _decalRight.AddUIComponent<UIPanel>();
            _decalBox.width = PanelWidth;
            _decalBox.height = 44f;
            _decalBox.relativePosition = new Vector3(0f, y2);
            MakeValueRow(_decalBox, 0f, "dbox", DecalStore.MinBox, DecalStore.MaxBox, DecalSettings.DefaultBox, out _dBox, out _dBoxV,
                delegate (int u) { OnDecalValue("box", delegate (DecalSettings d) { d.Box = u; }); });
            y2 += 44f;

            _decalRest = _decalRight.AddUIComponent<UIPanel>();
            _decalRest.width = PanelWidth;
            _decalRest.relativePosition = new Vector3(0f, y2);
            float restTop = y2;
            float yr = 0f;
            yr = MakeValueRow(_decalRest, yr, "dshiftx", -FenceStore.MaxUnits, FenceStore.MaxUnits, 0f, out _dShiftX, out _dShiftXV,
                delegate (int u) { OnDecalValue("shiftx", delegate (DecalSettings d) { d.ShiftX = u; }); });
            yr = MakeValueRow(_decalRest, yr, "dlateral", -FenceStore.MaxUnits, FenceStore.MaxUnits, 0f, out _dLateral, out _dLateralV,
                delegate (int u) { OnDecalValue("lateral", delegate (DecalSettings d) { d.Lateral = u; }); });
            yr = MakeValueRow(_decalRest, yr, "dlift", -FenceStore.MaxUnits, FenceStore.MaxUnits, 0f, out _dLift, out _dLiftV,
                delegate (int u) { OnDecalValue("lift", delegate (DecalSettings d) { d.Lift = u; }); });
            yr = MakeValueRow(_decalRest, yr, "dstart", -DecalStore.MaxShift, 0f, 0f, out _dStart, out _dStartV,
                delegate (int u) { OnDecalValue("start", delegate (DecalSettings d) { d.StartShift = u; }); });
            yr = MakeValueRow(_decalRest, yr, "dend", -DecalStore.MaxShift, 0f, 0f, out _dEnd, out _dEndV,
                delegate (int u) { OnDecalValue("end", delegate (DecalSettings d) { d.EndShift = u; }); });

            MakeLabel(_decalRest, Loc.T("dcolor"), 12f, yr, 0.72f);
            _dSwatch = _decalRest.AddUIComponent<UIPanel>();
            _dSwatch.size = new Vector2(40f, 18f);
            _dSwatch.relativePosition = new Vector3(PanelWidth - 140f, yr - 1f);
            _dSwatch.backgroundSprite = "GenericPanel";
            _dSwatch.isInteractive = false;
            _dHex = MakeHexField(_decalRest, PanelWidth - 94f, yr - 1f);
            yr += 24f;

            string[] channelNames = { "R", "G", "B", "A" };
            _dRgba = new UISlider[4];
            _dRgbaV = new UILabel[4];
            for (int i = 0; i < 4; i++)
            {
                int channel = i;
                MakeLabel(_decalRest, channelNames[i], 12f, yr, 0.72f);
                UISlider sl = MakeSlider(_decalRest, 30f, yr, PanelWidth - 30f - 50f, 0f, 255f, 255f);
                _dRgba[i] = sl;
                _dRgbaV[i] = MakeLabel(_decalRest, "255", PanelWidth - 40f, yr, 0.72f);
                sl.eventValueChanged += delegate (UIComponent c, float v)
                {
                    if (_colorSync) return;
                    byte value = (byte)Mathf.Clamp(Mathf.RoundToInt(v), 0, 255);
                    SetChannel(_brush, channel, value);
                    UpdateColorUi();
                    PushColor();
                };
                yr += 22f;
            }
            UpdateColorUi();
            yr += 4f;

            yr = MakeValueRow(_decalRest, yr, "dcolormul", 0f, 10f, 0f, out _dMul, out _dMulV,
                delegate (int u) { OnDecalValue("colormul", delegate (DecalSettings d) { d.ColorMul = u; }); }, " ");
            _dMul.tooltip = Loc.T("dcolormul_tip");
            yr += 4f;

            _decalState = MakeLabel(_decalRest, string.Empty, 12f, yr, 0.75f);
            yr += 22f;

            _decalRest.height = yr;
            _decalRestTop = restTop;
            _decalHeightFull = Mathf.Max(_decalLeft, restTop + yr) + 4f;
            _decalHeightPlane = Mathf.Max(_decalLeft, restTop - 44f + yr) + 4f;
            _decalHeight = _decalHeightFull;

            // the list is created last so that it is drawn above the controls below its button
            BuildPopup(_decalUi);
        }

        /// <summary>One of the two "add path" buttons: adds a path drawn as a decal (plane = false) or as a plane.</summary>
        private UIButton MakeAddButton(float x, float y, float w, string textKey, bool plane)
        {
            UIButton b = _decal.AddUIComponent<UIButton>();
            b.width = w;
            b.height = 32f;
            b.relativePosition = new Vector3(x, y);
            StyleButton(b);
            b.text = "+  " + Loc.T(textKey);
            b.textScale = 0.8f;
            b.eventClicked += delegate (UIComponent c, UIMouseEventParameter p)
            {
                QuayTool tool = QuayTool.Instance;
                if (tool == null) return;
                ClosePopup();
                _decalIndex = int.MaxValue; // show the new (last) path once it exists
                DecalSettings template = new DecalSettings();
                template.Strip = plane;
                template.Lift = plane ? DecalSettings.StripLift : 0; // a plane lies lower than the quay surface: 1 m by default
                template.Prop = _lastDecalProp;
                if (_lastDecalProp != null) template.Scale = _lastDecalScale;
                else template.Scale = NaturalPavementScale();
                if (!plane && template.Prop == null)
                {
                    // a decal path always has a decal: the first one of the list until the player chooses
                    List<DecalEntry> list = DecalCatalog.Entries;
                    if (list.Count > 0)
                    {
                        template.Prop = list[0].Info.name;
                        template.Scale = Mathf.Clamp(Mathf.RoundToInt(list[0].NaturalSize / FenceStore.Unit), DecalStore.MinScale, DecalStore.MaxScale);
                    }
                }
                tool.AddDecal(template);
            };
            return b;
        }

        /// <summary>Tile size (units of 0.1 m) of the theme pavement texture: the natural size of a path without a decal.</summary>
        private static int NaturalPavementScale()
        {
            return Mathf.Clamp(Mathf.RoundToInt(DecalRenderer.PavementTileMetres() / FenceStore.Unit), DecalStore.MinScale, DecalStore.MaxScale);
        }

        /// <summary>The projection size is hidden for a plane and the controls below move up.</summary>
        private void UpdateDecalLayout(bool plane)
        {
            if (_decalBox == null) return;
            _decalBox.isVisible = !plane;
            _decalRest.relativePosition = new Vector3(0f, _decalRestTop - (plane ? 44f : 0f));
            _decalHeight = plane ? _decalHeightPlane : _decalHeightFull;
            UpdateHeight();
        }

        private void StepDecal(int direction)
        {
            if (_decalCount < 2) return;
            _decalIndex += direction;
            if (_decalIndex >= _decalCount) _decalIndex = 0;
            if (_decalIndex < 0) _decalIndex = _decalCount - 1;
            ClosePopup();
            LoadDecalFromSelection();
        }

        /// <summary>A decal was chosen in the list: use its natural size as tile size and apply both to the current path.</summary>
        private void OnDecalPicked(PickItem item)
        {
            DecalEntry entry = item == null ? null : (DecalEntry)item.Tag;
            string prop = entry == null ? null : entry.Info.name;
            int scale = _brush.Scale;

            // a decal brings its natural size; no texture means the theme pavement, which has its own
            scale = entry != null
                ? Mathf.Clamp(Mathf.RoundToInt(entry.NaturalSize / FenceStore.Unit), DecalStore.MinScale, DecalStore.MaxScale)
                : NaturalPavementScale();
            {
                bool was = _loading;
                _loading = true; // do not send the scale as a separate edit
                _dScale.value = scale;
                _dScaleV.text = FormatOffset(scale);
                _loading = was;
                _brush.Scale = scale;
            }
            _brush.Prop = prop;
            _lastDecalProp = prop;
            _lastDecalScale = scale;

            QuayTool tool = QuayTool.Instance;
            if (tool == null) return;
            int applyScale = scale;
            bool setScale = true;
            tool.EditDecal(_decalIndex, "prop", delegate (DecalSettings d)
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

        private static void SetChannel(DecalSettings d, int channel, byte v)
        {
            if (channel == 0) d.R = v;
            else if (channel == 1) d.G = v;
            else if (channel == 2) d.B = v;
            else d.A = v;
        }

        private static string HexOf(DecalSettings d)
        {
            return "#" + d.R.ToString("X2") + d.G.ToString("X2") + d.B.ToString("X2") + d.A.ToString("X2");
        }

        /// <summary>Shows the colour of the current path in the sliders, the swatch and the hex field (without pushing it anywhere).</summary>
        private void UpdateColorUi()
        {
            if (_dRgba == null || _brush == null) return;
            _colorSync = true;
            try
            {
                _dRgba[0].value = _brush.R;
                _dRgba[1].value = _brush.G;
                _dRgba[2].value = _brush.B;
                _dRgba[3].value = _brush.A;
                _dRgbaV[0].text = _brush.R.ToString();
                _dRgbaV[1].text = _brush.G.ToString();
                _dRgbaV[2].text = _brush.B.ToString();
                _dRgbaV[3].text = _brush.A.ToString();
                _dSwatch.color = new Color32(_brush.R, _brush.G, _brush.B, 255);
                if (!_dHex.hasFocus) _dHex.text = HexOf(_brush);
            }
            finally
            {
                _colorSync = false;
            }
        }

        /// <summary>Applies the colour to the current path of the selected segments (one undo step while dragging).</summary>
        private void PushColor()
        {
            if (_loading) return;
            QuayTool tool = QuayTool.Instance;
            if (tool == null) return;
            byte r = _brush.R, g = _brush.G, b = _brush.B, a = _brush.A;
            tool.EditDecal(_decalIndex, "color", delegate (DecalSettings d) { d.R = r; d.G = g; d.B = b; d.A = a; });
        }

        private UITextField MakeHexField(UIComponent parent, float x, float y)
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
            f.size = new Vector2(70f, 18f);
            f.relativePosition = new Vector3(x - 6f, y);
            f.text = "#FFFFFFFF";
            f.tooltip = Loc.T("tip_hex");
            f.eventTextSubmitted += delegate (UIComponent c, string t) { CommitHex(t); };
            f.eventLostFocus += delegate (UIComponent c, UIFocusEventParameter e) { CommitHex(_dHex.text); };
            return f;
        }

        private void CommitHex(string text)
        {
            string t = (text ?? string.Empty).Trim().TrimStart('#');
            uint value;
            bool ok = (t.Length == 6 || t.Length == 8) &&
                      uint.TryParse(t, System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture, out value);
            if (ok)
            {
                value = uint.Parse(t, System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture);
                if (t.Length == 6) value = (value << 8) | 0xFFu;
                byte r = (byte)(value >> 24), g = (byte)(value >> 16), b = (byte)(value >> 8), a = (byte)value;
                if (r != _brush.R || g != _brush.G || b != _brush.B || a != _brush.A)
                {
                    _brush.R = r;
                    _brush.G = g;
                    _brush.B = b;
                    _brush.A = a;
                    UpdateColorUi();
                    PushColor();
                    return;
                }
            }
            _dHex.text = HexOf(_brush); // invalid or unchanged: show the current value
        }

        /// <summary>A decal slider moved: keep the shown values in step and apply it to the current path of the selected segments.</summary>
        private void OnDecalValue(string key, Action<DecalSettings> apply)
        {
            apply(_brush);
            if (_loading) return;

            QuayTool tool = QuayTool.Instance;
            if (tool != null) tool.EditDecal(_decalIndex, key, apply);
        }

        /// <summary>Shows the current path of the first selected segment that has any.</summary>
        private void LoadDecalFromSelection()
        {
            if (_decal == null) return;

            QuayTool tool = QuayTool.Instance;
            int count = tool == null ? 0 : tool.Selection.Count;

            _loading = true;
            try
            {
                _decalSel.text = Loc.T("selected") + count;

                DecalSet set = null;
                int have = 0;
                for (int i = 0; i < count; i++)
                {
                    DecalSet s;
                    if (DecalStore.TryGet(tool.Selection[i], out s) && s.Paths.Count > 0)
                    {
                        have++;
                        if (set == null) set = s;
                    }
                }

                _decalCount = set == null ? 0 : set.Paths.Count;
                bool has0 = _decalCount > 0;
                _decalIndex = _decalCount == 0 ? 0 : Mathf.Clamp(_decalIndex, 0, _decalCount - 1);
                if (tool != null) tool.ActiveLine = _decalCount == 0 ? -1 : _decalIndex;
                DecalSettings d = set != null ? set.Paths[_decalIndex] : new DecalSettings();

                _decalNav.text = _decalCount == 0 ? Loc.T("tpath_nolines") : Loc.F("tpath_nav", _decalIndex + 1, _decalCount);

                _brush.Width = d.Width;
                _brush.Lateral = d.Lateral;
                _brush.ShiftX = d.ShiftX;
                _brush.ColorMul = d.ColorMul;
                _brush.Lift = d.Lift;
                _brush.R = d.R;
                _brush.G = d.G;
                _brush.B = d.B;
                _brush.A = d.A;
                _brush.Prop = d.Prop;
                _brush.Scale = d.Scale;
                _brush.Step = d.Step;
                _brush.Box = d.Box;
                _brush.Strip = d.Strip;
                _brush.StartShift = d.StartShift;
                _brush.EndShift = d.EndShift;

                _dWidth.value = d.Width;
                _dScale.value = d.Scale;
                _dStep.value = d.Step;
                _dBox.value = d.Box;
                _dShiftX.value = d.ShiftX;
                _dMul.value = d.ColorMul;
                _dLateral.value = d.Lateral;
                _dLift.value = d.Lift;
                _dStart.value = d.StartShift;
                _dEnd.value = d.EndShift;
                _dWidthV.text = FormatOffset(_dWidth.value);
                _dScaleV.text = FormatOffset(_dScale.value);
                _dStepV.text = FormatOffset(_dStep.value);
                _dBoxV.text = FormatOffset(_dBox.value);
                _dShiftXV.text = FormatOffset(_dShiftX.value);
                _dMulV.text = FormatOffset(_dMul.value);
                _dLateralV.text = FormatOffset(_dLateral.value);
                _dLiftV.text = FormatOffset(_dLift.value);
                _dStartV.text = FormatOffset(_dStart.value);
                _dEndV.text = FormatOffset(_dEnd.value);
                UpdateColorUi();
                ShowBrushHeader();
                UpdateDecalLayout(has0 && d.Strip);
                bool decalPath = has0 && !d.Strip;
                _decalUi.ShowEmpty = !decalPath;
                _decalUi.EmptyText = Loc.T(decalPath ? "line_none" : "decal_solid");
                ShowBrushHeader();

                bool any = count > 0, has = _decalCount > 0;
                _dAdd.isEnabled = any;
                _dAddPlane.isEnabled = any;
                // the button of the way the current path is drawn stays pressed (the way cannot be changed afterwards)
                _dAdd.state = has && !d.Strip ? UIButton.ButtonState.Focused : UIButton.ButtonState.Normal;
                _dAddPlane.state = has && d.Strip ? UIButton.ButtonState.Focused : UIButton.ButtonState.Normal;
                _decalPrev.isEnabled = has && _decalCount > 1;
                _decalNext.isEnabled = has && _decalCount > 1;
                _dRemove.isEnabled = has;
                _dClear.isEnabled = has;
                _decalUi.Header.isEnabled = has;
                UISlider[] sliders = { _dShiftX, _dWidth, _dScale, _dStep, _dBox, _dLateral, _dLift, _dStart, _dEnd };
                for (int i = 0; i < sliders.Length; i++) sliders[i].isEnabled = has;
                _dMul.isEnabled = has;
                for (int i = 0; i < _dRgba.Length; i++) _dRgba[i].isEnabled = has;
                _dHex.isEnabled = has;
                if (_resetBtn != null) _resetBtn.isEnabled = has;

                _decalState.text = Loc.F("decal_state", have, count);
            }
            finally
            {
                _loading = false;
            }
        }

        // ---------- orientation lock section ----------

        private void BuildLockSection()
        {
            _lock = AddUIComponent<UIPanel>();
            _lock.width = PanelWidth;
            _lock.relativePosition = new Vector3(ToolsWidth, TopY);
            _lock.isVisible = false;

            float y = 0f;
            _lockSel = MakeLabel(_lock, string.Empty, 12f, y, 0.85f);
            y += 26f;

            _lockState = MakeLabel(_lock, string.Empty, 12f, y, 0.75f);
            y += 26f;

            _lockBtn = _lock.AddUIComponent<UIButton>();
            _lockBtn.width = PanelWidth - 20f;
            _lockBtn.height = 32f;
            _lockBtn.relativePosition = new Vector3(10f, y);
            StyleButton(_lockBtn);
            _lockBtn.text = Loc.T("lock_do");
            _lockBtn.textScale = 0.85f;
            _lockBtn.eventClicked += delegate (UIComponent c, UIMouseEventParameter p)
            {
                QuayTool tool = QuayTool.Instance;
                if (tool != null) tool.SetLock(true);
            };
            y += BtnStride;

            _unlockBtn = _lock.AddUIComponent<UIButton>();
            _unlockBtn.width = PanelWidth - 20f;
            _unlockBtn.height = 32f;
            _unlockBtn.relativePosition = new Vector3(10f, y);
            StyleButton(_unlockBtn);
            _unlockBtn.text = Loc.T("lock_undo");
            _unlockBtn.textScale = 0.85f;
            _unlockBtn.eventClicked += delegate (UIComponent c, UIMouseEventParameter p)
            {
                QuayTool tool = QuayTool.Instance;
                if (tool != null) tool.SetLock(false);
            };
            y += BtnStride;

            _lockHeight = y;
        }

        private void LoadLockFromSelection()
        {
            if (_lock == null) return;

            QuayTool tool = QuayTool.Instance;
            int count = tool == null ? 0 : tool.Selection.Count;
            bool peds = tool != null && tool.CurrentMode == QuayTool.Mode.RemovePedestrian;
            bool hide = tool != null && tool.CurrentMode == QuayTool.Mode.HideProps;
            string kind = peds ? "nop" : hide ? "hp" : "lock";
            int locked = 0;
            for (int i = 0; i < count; i++)
            {
                ushort seg = tool.Selection[i];
                if (peds ? PedStore.Has(seg) : hide ? HideStore.Has(seg) : LockStore.IsLocked(seg)) locked++;
            }

            _lockBtn.text = Loc.T(kind + "_do");
            _unlockBtn.text = Loc.T(kind + "_undo");
            _lockSel.text = Loc.T("selected") + count;
            _lockState.text = Loc.F(kind + "_state", locked, count);
            _lockBtn.isEnabled = count > 0 && locked < count;
            _unlockBtn.isEnabled = locked > 0;
            if (_resetBtn != null) _resetBtn.isEnabled = false;
        }

        // ---------- prop line section ----------

        private void BuildPropSection()
        {
            _prop = AddUIComponent<UIPanel>();
            _prop.width = PanelWidth * 2f;
            _prop.relativePosition = new Vector3(ToolsWidth, TopY);
            _prop.isVisible = false;

            _propRight = _prop.AddUIComponent<UIPanel>();
            _propRight.width = PanelWidth;
            _propRight.relativePosition = new Vector3(PanelWidth, 0f);

            // ---- left column: lines, add, prop chooser, remove buttons
            float y = 0f;
            _propSel = MakeLabel(_prop, string.Empty, 12f, y, 0.85f);
            y += 24f;

            _propPrev = MakeSmallButton(_prop, 10f, y, 34f, "<", delegate { StepProp(-1); });
            _propNav = MakeLabel(_prop, string.Empty, 54f, y + 8f, 0.8f);
            _propNext = MakeSmallButton(_prop, PanelWidth - 44f, y, 34f, ">", delegate { StepProp(1); });
            y += 36f;

            _propAdd = _prop.AddUIComponent<UIButton>();
            _propAdd.width = PanelWidth - 20f;
            _propAdd.height = 32f;
            _propAdd.relativePosition = new Vector3(10f, y);
            StyleButton(_propAdd);
            _propAdd.text = "+  " + Loc.T("prop_add");
            _propAdd.textScale = 0.85f;
            _propAdd.eventClicked += delegate (UIComponent c, UIMouseEventParameter p)
            {
                QuayTool tool = QuayTool.Instance;
                if (tool == null) return;
                ClosePopup();
                _propIndex = int.MaxValue; // show the new (last) line once it exists
                tool.AddPropEntry(new PropEntry());
            };
            y += BtnStride;

            MakeLabel(_prop, Loc.T("prop_choose"), 12f, y, 0.85f);
            y += 20f;

            _propUi = new PickerUi();
            _propUi.Parent = _prop;
            _propUi.EmptyText = Loc.T("prop_none");
            _propUi.Kind = Favorites.Props;
            _propUi.GetItems = delegate (string text)
            {
                List<PickItem> items = new List<PickItem>();
                List<PropCatalogEntry> list = PropCatalog.Search(text);
                for (int i = 0; i < list.Count; i++)
                {
                    PickItem it = new PickItem();
                    it.Title = list[i].Title;
                    it.Atlas = list[i].Info != null ? list[i].Info.m_Atlas : list[i].Tree.m_Atlas;
                    it.Thumb = list[i].Info != null ? list[i].Info.m_Thumbnail : list[i].Tree.m_Thumbnail;
                    it.Tag = list[i];
                    it.Key = list[i].Key;
                    items.Add(it);
                }
                return items;
            };
            _propUi.OnPicked = OnPropPicked;
            BuildHeader(_prop, _propUi, y);
            y += 84f;

            y += 10f;
            _propAct = y;
            _propRemove = _prop.AddUIComponent<UIButton>();
            _propRemove.width = PanelWidth - 20f;
            _propRemove.height = 32f;
            _propRemove.relativePosition = new Vector3(10f, y);
            StyleButton(_propRemove);
            _propRemove.text = Loc.T("prop_remove");
            _propRemove.textScale = 0.85f;
            MakeRed(_propRemove);
            _propRemove.eventClicked += delegate (UIComponent c, UIMouseEventParameter p)
            {
                QuayTool tool = QuayTool.Instance;
                if (tool != null) tool.RemovePropEntry(_propIndex);
            };
            y += BtnStride;

            _propClear = _prop.AddUIComponent<UIButton>();
            _propClear.width = PanelWidth - 20f;
            _propClear.height = 32f;
            _propClear.relativePosition = new Vector3(10f, y);
            StyleButton(_propClear);
            _propClear.text = Loc.T("prop_clear");
            _propClear.textScale = 0.85f;
            MakeRed(_propClear);
            _propClear.eventClicked += delegate (UIComponent c, UIMouseEventParameter p)
            {
                QuayTool tool = QuayTool.Instance;
                if (tool == null) return;
                ClosePopup();
                tool.ClearPropLines();
            };
            y += BtnStride;
            _propLeft = y; // the undo / redo / reset bar follows here

            // ---- right column: all the values
            float y2 = 0f;
            y2 = MakeValueRow(_propRight, y2, "pstep", PropEntry.StepMin, PropEntry.StepMax, PropEntry.StepDefault, out _pStep, out _pStepV,
                delegate (int u) { OnPropValue("step", delegate (PropEntry e) { e.Step = u; }); });
            y2 = MakeIntRow(_propRight, y2, "pscale", PropEntry.ScaleMin, PropEntry.ScaleMax, PropEntry.ScaleDefault, " %", out _pScale, out _pScaleV,
                delegate (int u) { OnPropValue("scale", delegate (PropEntry e) { e.Scale = u; }); });
            y2 = MakeIntRow(_propRight, y2, "prandscale", 0, PropEntry.RandomMax, 0, " %", out _pRand, out _pRandV,
                delegate (int u) { OnPropValue("rand", delegate (PropEntry e) { e.ScaleRandom = u; }); });

            // rotation controls are hidden for trees (a tree has no direction); they sit in their own panel
            const float RotHeight = 44f + 36f + 36f;
            _propRot = _propRight.AddUIComponent<UIPanel>();
            _propRot.width = PanelWidth;
            _propRot.height = RotHeight;
            _propRot.relativePosition = new Vector3(0f, y2);
            float yr = 0f;
            yr = MakeIntRow(_propRot, yr, "pangle", 0, 359, 0, " °", out _pAngle, out _pAngleV,
                delegate (int u) { OnPropValue("angle", delegate (PropEntry e) { e.Angle = u; }); });
            _pRotate = MakeToggle(_propRot, 10f, yr, PanelWidth - 20f, "prandrot", null,
                delegate (bool v) { OnPropValue("rotate", delegate (PropEntry e) { e.RandomRotation = v; }); });
            yr += 36f;
            _pTilt = MakeToggle(_propRot, 10f, yr, PanelWidth - 20f, "ptilt", null,
                delegate (bool v) { OnPropValue("tilt", delegate (PropEntry e) { e.Tilt = v; }); });
            _pTilt.Button.tooltip = Loc.T("ptilt_tip");
            y2 += RotHeight;

            _propRest = _propRight.AddUIComponent<UIPanel>();
            _propRest.width = PanelWidth;
            _propRest.relativePosition = new Vector3(0f, y2);
            float restTop = y2;
            float y3 = 0f;
            y3 = MakeValueRow(_propRest, y3, "pshiftx", -PropEntry.MaxOffset, PropEntry.MaxOffset, 0f, out _pShiftX, out _pShiftXV,
                delegate (int u) { OnPropValue("shiftx", delegate (PropEntry e) { e.ShiftX = u; }); });
            y3 = MakeValueRow(_propRest, y3, "plateral", -PropEntry.MaxOffset, PropEntry.MaxOffset, 0f, out _pLateral, out _pLateralV,
                delegate (int u) { OnPropValue("lateral", delegate (PropEntry e) { e.Lateral = u; }); });
            y3 = MakeValueRow(_propRest, y3, "plift", -PropEntry.MaxOffset, PropEntry.MaxOffset, 0f, out _pLift, out _pLiftV,
                delegate (int u) { OnPropValue("lift", delegate (PropEntry e) { e.Lift = u; }); });
            y3 = MakeValueRow(_propRest, y3, "pstart", -PropEntry.MaxShift, 0f, 0f, out _pStart, out _pStartV,
                delegate (int u) { OnPropValue("start", delegate (PropEntry e) { e.StartShift = u; }); });
            y3 = MakeValueRow(_propRest, y3, "pend", -PropEntry.MaxShift, 0f, 0f, out _pEnd, out _pEndV,
                delegate (int u) { OnPropValue("end", delegate (PropEntry e) { e.EndShift = u; }); });

            _propState = MakeLabel(_propRest, string.Empty, 12f, y3, 0.75f);
            y3 += 22f;

            _propRest.height = y3;
            _propHeightFull = Mathf.Max(_propLeft, restTop + y3) + 4f;
            _propHeightTree = Mathf.Max(_propLeft, restTop - RotHeight + y3) + 4f;
            _propHeight = _propHeightFull;

            // the list is created last so that it is drawn above the controls below its button
            BuildPopup(_propUi);
        }

        private void OnPropValue(string key, Action<PropEntry> apply)
        {
            if (_loading) return;
            QuayTool tool = QuayTool.Instance;
            if (tool != null) tool.EditPropEntry(_propIndex, key, apply);
        }

        /// <summary>A tree has no direction: the rotation controls are hidden for it and the controls below move up.</summary>
        private void UpdateRotationControls(string prop)
        {
            bool isTree = false;
            if (!string.IsNullOrEmpty(prop))
            {
                PropInfo pi;
                TreeInfo ti;
                isTree = PropCatalog.Resolve(prop, out pi, out ti) && ti != null;
            }
            _propRot.isVisible = !isTree;
            _propRest.relativePosition = new Vector3(0f, _propRot.relativePosition.y + (isTree ? 0f : _propRot.height));
            _propHeight = isTree ? _propHeightTree : _propHeightFull;
            UpdateHeight();
        }

        private void OnPropPicked(PickItem item)
        {
            string name = item == null ? null : ((PropCatalogEntry)item.Tag).Key;
            UpdateRotationControls(name);
            QuayTool tool = QuayTool.Instance;
            if (tool != null) tool.EditPropEntry(_propIndex, "prop", delegate (PropEntry e) { e.Prop = name; });
        }

        private void StepProp(int direction)
        {
            if (_propCount < 2) return;
            _propIndex += direction;
            if (_propIndex >= _propCount) _propIndex = 0;
            if (_propIndex < 0) _propIndex = _propCount - 1;
            ClosePopup();
            LoadPropFromSelection();
        }

        private void ShowPropHeader(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                SetHeaderItem(_propUi, null);
                return;
            }

            PropInfo info;
            TreeInfo tree;
            PickItem item = new PickItem();
            if (!PropCatalog.Resolve(name, out info, out tree))
            {
                item.Title = name + Loc.T("missing");
            }
            else
            {
                item.Title = PropCatalog.TitleOfKey(name);
                item.Atlas = info != null ? info.m_Atlas : tree.m_Atlas;
                item.Thumb = info != null ? info.m_Thumbnail : tree.m_Thumbnail;
            }
            SetHeaderItem(_propUi, item);
        }

        /// <summary>Shows the current prop line of the first selected segment that has any.</summary>
        private void LoadPropFromSelection()
        {
            if (_prop == null) return;

            QuayTool tool = QuayTool.Instance;
            int count = tool == null ? 0 : tool.Selection.Count;

            _loading = true;
            try
            {
                _propSel.text = Loc.T("selected") + count;

                PropLine line = null;
                int have = 0;
                for (int i = 0; i < count; i++)
                {
                    PropLine l;
                    if (PropLineStore.TryGet(tool.Selection[i], out l) && l.Entries.Count > 0)
                    {
                        have++;
                        if (line == null) line = l;
                    }
                }

                _propCount = line == null ? 0 : line.Entries.Count;
                _propIndex = _propCount == 0 ? 0 : Mathf.Clamp(_propIndex, 0, _propCount - 1);
                PropEntry e = line != null ? line.Entries[_propIndex] : new PropEntry();

                _propNav.text = _propCount == 0 ? Loc.T("prop_nolines") : Loc.F("prop_nav", _propIndex + 1, _propCount);

                _pStep.value = e.Step;
                _pStart.value = e.StartShift;
                _pEnd.value = e.EndShift;
                _pShiftX.value = e.ShiftX;
                _pLateral.value = e.Lateral;
                _pLift.value = e.Lift;
                _pAngle.value = e.Angle;
                _pScale.value = e.Scale;
                _pRand.value = e.ScaleRandom;
                _pStepV.text = FormatOffset(_pStep.value);
                _pStartV.text = FormatOffset(_pStart.value);
                _pEndV.text = FormatOffset(_pEnd.value);
                _pShiftXV.text = FormatOffset(_pShiftX.value);
                _pLateralV.text = FormatOffset(_pLateral.value);
                _pLiftV.text = FormatOffset(_pLift.value);
                _pAngleV.text = e.Angle.ToString();
                _pScaleV.text = e.Scale.ToString();
                _pRandV.text = e.ScaleRandom.ToString();
                SetToggle(_pRotate, e.RandomRotation);
                SetToggle(_pTilt, e.Tilt);
                ShowPropHeader(e.Prop);
                UpdateRotationControls(e.Prop);

                bool any = count > 0, has = _propCount > 0;
                _propAdd.isEnabled = any;
                _propPrev.isEnabled = has && _propCount > 1;
                _propNext.isEnabled = has && _propCount > 1;
                _propRemove.isEnabled = has;
                _propClear.isEnabled = has;
                _propUi.Header.isEnabled = has;
                UISlider[] sliders = { _pShiftX, _pStep, _pStart, _pEnd, _pLateral, _pLift, _pAngle, _pScale, _pRand };
                for (int i = 0; i < sliders.Length; i++) sliders[i].isEnabled = has;
                _pRotate.Button.isEnabled = has;
                _pTilt.Button.isEnabled = has;
                if (_resetBtn != null) _resetBtn.isEnabled = false;

                _propState.text = Loc.F("prop_state", have, count);
            }
            finally
            {
                _loading = false;
            }
        }

        // ---------- undo / redo / reset bar ----------

        private void BuildBar()
        {
            _bar = AddUIComponent<UIPanel>();
            _bar.width = PanelWidth;
            _bar.height = 38f;
            _bar.relativePosition = new Vector3(ToolsWidth, TopY);
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

            // the language of the game or the option changed: the texts are created once, so the window is built again
            if (_langBuilt != Loc.Current)
            {
                bool visible = isVisible;
                _restorePos = absolutePosition;
                _hasRestorePos = true;
                DestroyPanel();
                if (visible) ShowPanel();
                return;
            }

            if (SliderDragging && !Input.GetMouseButton(0)) SliderDragging = false;

            if (_historyVersion != History.Version)
            {
                _historyVersion = History.Version;
                UpdateBar();
            }

            if (_lockVersion != LockStore.Version + PedStore.Version + HideStore.Version)
            {
                _lockVersion = LockStore.Version + PedStore.Version + HideStore.Version;
                QuayTool tool = QuayTool.Instance;
                if (tool != null && (tool.CurrentMode == QuayTool.Mode.Lock || tool.CurrentMode == QuayTool.Mode.RemovePedestrian || tool.CurrentMode == QuayTool.Mode.HideProps)) LoadLockFromSelection();
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
                UITextField[] fields =
                {
                    _dMulV, _dShiftXV, _pShiftXV, _dWidthV, _dScaleV, _dStepV, _dBoxV, _dLateralV, _dLiftV, _dStartV, _dEndV, _dHex,
                    _nStartV, _nEndV, _nLateralV, _nLiftV, _nScaleV, _pAngleV, _pScaleV, _pRandV,
                    _netUi.Search, _decalUi.Search, _propUi.Search, _pStepV, _pStartV, _pEndV, _pLateralV, _pLiftV
                };
                for (int i = 0; i < fields.Length; i++)
                {
                    if (fields[i] != null && fields[i].hasFocus) return true;
                }
                return false;
            }
        }

        public void SetStatus(string text)
        {
            if (_status != null && _status.text != text)
            {
                bool hadText = !string.IsNullOrEmpty(_status.text);
                _status.text = text;
                if (hadText != !string.IsNullOrEmpty(text)) UpdateHeight(); // the second column of Invert opens and closes with its message
            }
        }
    }
}
