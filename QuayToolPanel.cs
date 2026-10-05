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

        private static readonly string[] TitleKeys = { "mode_segment", "mode_network", "mode_props", "mode_decal", "mode_templates", "mode_settings" };
        private static readonly string[] IconFiles = { "Segment.png", "Network.png", "Props.png", "Decal.png", "Templates.png", "Settings.png" };

        /// <summary>A check box drawn as a button: "[x] text".</summary>
        private const float SwitchW = 38f, SwitchH = 20f, SwitchKnob = 14f;

        private class Toggle
        {
            public UIButton Button;
            public bool Value;
            public string TextKey;
            public UIPanel Pill;   // the switch: a pill with a round knob
            public UIPanel Knob;
            public bool Mixed;     // several segments selected with different states
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
            public Texture2D Tex; // a picture instead of the atlas icon (templates)
        }

        /// <summary>One pooled row of a drop-down list.</summary>
        private class RowUi
        {
            public UIButton Button;
            public UISprite Icon;
            public UIButton StarButton;
            public UITextureSprite Star;
            public UITextureSprite Tex;
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
            public bool Textured;                     // wide picture (16:9) in the header and in the rows
            public UITextureSprite Pic;
        }

        private UIDragHandle _drag;
        private UIButton[] _modeButtons;
        private UITextureSprite[] _modeIcon;
        private UILabel _title;
        private UIPanel _toolsPanel;
        private Toggle _hideHl;
        internal static bool SliderDragging;
        private UILabel _status;
        private UIPanel _actionsBack;
        private float _netAct, _decalAct, _propAct;
        private const float BtnStride = 40f;
        private const float ToolBtnH = 46f;
        private static readonly float ToolsMin = 8f + QuayTool.ModeCount * (ToolBtnH + 4f);
        private const float ToggleStride = 38f;
        private bool _built;
        private int _langBuilt;
        private static Vector3 _restorePos;
        private static bool _hasRestorePos;
        private bool _centerPending;
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
        private float _lockAct;
        private UIButton _lockClear;
        private UILabel _lockSel;
        private Toggle _tgLock, _tgNop, _tgHide;
        private UIButton _flipBtn;
        private int _lockVersion = -1;

        // settings section
        private UIPanel _set;
        private float _setHeight;
        private Toggle _setFlip, _setShadows, _setMark;
        private float _setAct;
        private UIButton _setHelp;
        private UIPanel _help;
        private UIScrollablePanel _helpScroll;
        private UIScrollbar _helpBar;
        private UISlicedSprite _helpTrack;
        private UIButton[] _setIcon;
        private UIButton _setClear;

        // templates section
        private UIPanel _tpl;
        private float _tplHeight;
        private UILabel _tplSel, _tplInfo;
        private UITextField _tplName;
        private UIButton _tplSave, _tplApply, _tplPencil;
        private PickerUi _tplUi;
        private List<QuayTemplate> _templates = new List<QuayTemplate>();
        private QuayTemplate _tplPicked;

        // template settings (the third column)
        private const int TplLineRows = 6;
        private UIPanel _tplEdit;
        private float _tplEditHeight;
        private bool _tplEditOpen;
        private string _tplEditFor;
        private UITextField _tplEditName;
        private UITextureSprite _tplEditPic;
        private UILabel _tplEditNoPic, _tplEditPage;
        private UIButton _tplEditRename, _tplEditShot, _tplEditDup, _tplEditDelete, _tplEditFolder, _tplEditPrev, _tplEditNext;
        private UIButton[] _tplLineBtn, _tplLineDel;
        private int _tplLineOffset;
        private UIPanel _dlg;            // the question before something is deleted
        private UILabel _dlgText;
        private Action _dlgYes;

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
            backgroundSprite = Flat.Round;
            atlas = Flat.Atlas;
            color = WinBg;
            isInteractive = true;
            canFocus = true;

            Vector2 res = UIView.GetAView().GetScreenResolution();
            absolutePosition = new Vector3(20f, 120f); // the left edge of the screen; the height is known later, see Start
            _centerPending = !_hasRestorePos;
            if (_hasRestorePos)
            {
                absolutePosition = _restorePos;
                _hasRestorePos = false;
            }
        }

        private readonly Dictionary<UIComponent, string> _hiddenTips = new Dictionary<UIComponent, string>();

        public override void Update()
        {
            base.Update();
            try
            {
                if (!Settings.HideTips)
                {
                    if (_hiddenTips.Count > 0)
                    {
                        foreach (KeyValuePair<UIComponent, string> kv in _hiddenTips)
                        {
                            if (kv.Key != null) kv.Key.tooltip = kv.Value;
                        }
                        _hiddenTips.Clear();
                    }
                    return;
                }

                // tooltips of the components under the mouse are taken away (and given back when the option is off)
                for (UIComponent c = UIInput.hoveredComponent; c != null; c = c.parent)
                {
                    if (c == this) break;
                    if (c.parent == null) return;
                }
                for (UIComponent c = UIInput.hoveredComponent; c != null && c != this; c = c.parent)
                {
                    if (!string.IsNullOrEmpty(c.tooltip))
                    {
                        _hiddenTips[c] = c.tooltip;
                        c.tooltip = string.Empty;
                    }
                }
            }
            catch (Exception)
            {
                // the tooltips just stay
            }
        }

        public override void Start()
        {
            base.Start();
            Build();

            QuayTool tool = QuayTool.Instance;
            if (tool != null) tool.SelectionChanged += OnSelectionChanged;

            Refresh();

            if (_centerPending)
            {
                // first appearance: at the left edge of the screen, in the middle of its height
                _centerPending = false;
                Vector2 screen = UIView.GetAView().GetScreenResolution();
                absolutePosition = new Vector3(20f, Mathf.Max(10f, (screen.y - height) * 0.5f));
            }
        }

        // ---------- small UI helpers ----------

        // flat palette (the colours of the preview picture): dark green-black, one yellow accent
        internal static readonly Color32 WinBg = new Color32(36, 50, 54, 255);
        internal static readonly Color32 ColumnBg = new Color32(27, 39, 43, 255);
        internal static readonly Color32 BoxBg = new Color32(46, 63, 68, 255);
        internal static readonly Color32 BtnBg = new Color32(60, 80, 86, 255);
        internal static readonly Color32 BtnOff = new Color32(44, 58, 62, 255);
        internal static readonly Color32 FieldBg = new Color32(24, 34, 37, 255);
        internal static readonly Color32 Accent = new Color32(250, 200, 40, 255);
        internal static readonly Color32 Dim = new Color32(52, 60, 64, 255);        // slider tracks, fields and empty pictures while nothing is selected
        internal static readonly Color32 DimThumb = new Color32(176, 182, 184, 255);
        internal static readonly Color32 TextMain = new Color32(232, 238, 238, 255);

        /// <summary>Flat button: one plain sprite, the state shows in the colour.</summary>
        internal static void StyleButton(UIButton button)
        {
            button.atlas = Flat.Atlas;
            button.normalBgSprite = Flat.Round;
            button.hoveredBgSprite = Flat.Round;
            button.pressedBgSprite = Flat.Round;
            button.focusedBgSprite = Flat.Round;
            button.disabledBgSprite = Flat.Round;
            button.color = BtnBg;
            button.hoveredColor = BtnBg;   // the hover shows as a thin yellow frame, not as a fill
            button.pressedColor = Accent;  // a click flashes the whole button yellow
            button.focusedColor = BtnBg;
            button.disabledColor = BtnOff;
            button.textColor = TextMain;
            button.hoveredTextColor = new Color32(255, 255, 255, 255);
            button.pressedTextColor = new Color32(24, 34, 36, 255);
            button.focusedTextColor = new Color32(255, 255, 255, 255);
            button.disabledTextColor = new Color32(104, 120, 122, 255);
            Flat.AddFrame(button);
        }

        /// <summary>A button whose look must not change when it keeps the focus after a click (switch rows).</summary>
        private static void NoFocusLook(UIButton b)
        {
            b.focusedColor = b.color;
        }

        private static void FlatField(UITextField f)
        {
            f.atlas = Flat.Atlas;
            f.normalBgSprite = Flat.Round;
            f.hoveredBgSprite = Flat.Round;
            f.focusedBgSprite = Flat.Round;
            f.color = FieldBg;
            f.disabledColor = Dim;
            f.eventIsEnabledChanged += delegate (UIComponent c, bool v) { f.color = v ? FieldBg : Dim; };
            f.textColor = TextMain;
        }

        /// <summary>Delete buttons: the normal button tinted dark red.</summary>
        private static void MakeRed(UIButton b)
        {
            b.color = new Color32(176, 62, 62, 255);
            b.hoveredColor = b.color;
            b.focusedColor = b.color;
            b.pressedColor = new Color32(140, 40, 40, 255);
            b.pressedTextColor = new Color32(255, 255, 255, 255);
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
            track.spriteName = "EmptySprite";
            track.color = new Color32(24, 34, 37, 255);
            track.disabledColor = Dim;
            track.size = new Vector2(width, 8f);
            track.relativePosition = new Vector3(0f, 4f);
            track.isInteractive = false;

            UISlicedSprite thumb = slider.AddUIComponent<UISlicedSprite>();
            thumb.spriteName = "EmptySprite";
            thumb.color = Accent;
            thumb.disabledColor = DimThumb;
            thumb.size = new Vector2(14f, 16f);
            thumb.relativePosition = Vector3.zero;
            slider.thumbObject = thumb;
            slider.disabledColor = new Color32(255, 255, 255, 255);

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
            f.atlas = Flat.Atlas;
            FlatField(f);
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
            t.Button.text = Loc.T(t.TextKey);
            if (t.Pill == null) return;
            bool on = t.Value;
            bool enabled = t.Button.isEnabled;
            // off: dark like the slider tracks (lighter while nothing is selected); on: green
            Color32 off = enabled ? new Color32(24, 34, 37, 255) : Dim;
            t.Pill.color = t.Mixed ? new Color32(220, 55, 55, 255) : on ? new Color32(60, 255, 90, 255) : off;
            t.Pill.opacity = !enabled && (on || t.Mixed) ? 0.45f : 1f;
            t.Knob.color = enabled || on || t.Mixed ? new Color32(255, 255, 255, 255) : DimThumb;
            t.Knob.opacity = !enabled && (on || t.Mixed) ? 0.55f : 1f;
            t.Pill.disabledColor = t.Pill.color; // a disabled panel would turn white
            t.Knob.disabledColor = t.Knob.color;
            t.Knob.relativePosition = new Vector3(on ? SwitchW - SwitchKnob - 3f : 3f, 3f);
        }

        private void SetToggle(Toggle t, bool value)
        {
            t.Value = value;
            t.Mixed = false;
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
            NoFocusLook(b);
            t.Button = b;

            // the switch on the right: label on the left, a pill with a knob on the right
            t.Pill = b.AddUIComponent<UIPanel>();
            t.Pill.backgroundSprite = Flat.Round;
            t.Pill.atlas = Flat.Atlas;
            t.Pill.size = new Vector2(SwitchW, SwitchH);
            t.Pill.relativePosition = new Vector3(w - SwitchW - 10f, (30f - SwitchH) * 0.5f);
            t.Pill.isInteractive = false;
            t.Knob = t.Pill.AddUIComponent<UIPanel>();
            t.Knob.backgroundSprite = Flat.Round;
            t.Knob.atlas = Flat.Atlas;
            t.Knob.color = new Color32(255, 255, 255, 255);
            t.Knob.size = new Vector2(SwitchKnob, SwitchKnob);
            t.Knob.isInteractive = false;

            b.eventClicked += delegate (UIComponent comp, UIMouseEventParameter e)
            {
                if (_loading) return;
                t.Value = t.Mixed ? true : !t.Value;
                t.Mixed = false;
                UpdateToggle(t);
                onChange(t.Value);
            };
            b.eventIsEnabledChanged += delegate (UIComponent comp, bool v) { UpdateToggle(t); };
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
            f.atlas = Flat.Atlas;
            FlatField(f);
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
            _modeIcon = new UITextureSprite[modeCount];
            float btn = ToolsWidth - 20f;
            float btnH = 46f;

            // first column: the tools, on a panel of their own
            _toolsPanel = AddUIComponent<UIPanel>();
            _toolsPanel.size = new Vector2(ToolsWidth - 10f, ToolsMin);
            _toolsPanel.relativePosition = new Vector3(6f, TopY);
            _toolsPanel.backgroundSprite = Flat.Round;
            _toolsPanel.atlas = Flat.Atlas;
            _toolsPanel.color = ColumnBg;
            _toolsPanel.isInteractive = false;

            for (int i = 0; i < modeCount; i++)
            {
                QuayTool.Mode mode = (QuayTool.Mode)i;
                UIButton button = _toolsPanel.AddUIComponent<UIButton>();
                button.width = btn;
                button.height = btnH;
                button.relativePosition = new Vector3(5f, 6f + i * (btnH + 4f));
                StyleButton(button);
                // a tool has a background; the chosen one loses it: only the icon and the yellow frame stay
                button.focusedColor = ColumnBg; // the colour of the column: the background disappears
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
                    _modeIcon[i] = sprite;
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
            _actionsBack.backgroundSprite = Flat.Round;
            _actionsBack.atlas = Flat.Atlas;
            _actionsBack.color = BoxBg;
            _actionsBack.isInteractive = false;
            _actionsBack.isVisible = false;

            BuildNetSection();
            BuildDecalSection();
            BuildLockSection();
            BuildTemplateSection();
            BuildSettingsSection();
            BuildPropSection();
            BuildBar();

            // created after the sections so that no panel lies over it
            _hideHl = MakeToggle(this, ToolsWidth + 10f, TopY, PanelWidth - 20f, "hidehl", null,
                delegate (bool v) { Settings.HideHighlightUi = v; });
            _hideHl.Button.tooltip = Loc.T("hidehl_tip");
            SetToggle(_hideHl, Settings.HideHighlightUi);

            BuildDialog(); // last: it lies over everything
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
            float picW = ui.Textured ? 118f : 68f;
            UIPanel frame = ui.Header.AddUIComponent<UIPanel>();
            frame.size = new Vector2(picW, 68f);
            frame.relativePosition = new Vector3(4f, 4f);
            frame.backgroundSprite = Flat.Round;
            frame.atlas = Flat.Atlas;
            frame.color = FieldBg;
            frame.disabledColor = Dim; // otherwise a disabled panel turns white
            frame.isInteractive = false;

            ui.Icon = ui.Header.AddUIComponent<UISprite>();
            ui.Icon.size = new Vector2(62f, 62f);
            ui.Icon.relativePosition = new Vector3(7f, 7f);
            ui.Icon.isInteractive = false;
            ui.Icon.isVisible = false;

            if (ui.Textured)
            {
                float w = picW - 6f, h = w * 9f / 16f;
                ui.Pic = ui.Header.AddUIComponent<UITextureSprite>();
                ui.Pic.size = new Vector2(w, h);
                ui.Pic.relativePosition = new Vector3(7f, 4f + (68f - h) * 0.5f);
                ui.Pic.isInteractive = false;
                ui.Pic.isVisible = false;
            }

            ui.Name = ui.Header.AddUIComponent<UILabel>();
            ui.Name.textScale = 0.8f;
            ui.Name.autoSize = false;
            ui.Name.wordWrap = true;
            ui.Name.width = PanelWidth - 20f - (picW + 22f) - (ui.Textured ? 44f : 0f);
            ui.Name.height = 56f;
            ui.Name.relativePosition = new Vector3(picW + 14f, 10f);
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
            popup.backgroundSprite = Flat.Round;
            popup.atlas = Flat.Atlas;
            popup.color = BoxBg;
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
            track.spriteName = "EmptySprite";
            track.color = new Color32(24, 34, 37, 255);
            track.relativePosition = Vector3.zero;
            track.size = bar.size;
            bar.trackObject = track;

            UISlicedSprite thumb = track.AddUIComponent<UISlicedSprite>();
            thumb.spriteName = "EmptySprite";
            thumb.color = Accent;
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
            row.textPadding = new RectOffset(ui.Textured ? 68 : 46, 0, 0, 0);
            r.Button = row;

            if (ui.Textured)
            {
                r.Tex = row.AddUIComponent<UITextureSprite>();
                r.Tex.size = new Vector2(52f, 29f);
                r.Tex.relativePosition = new Vector3(8f, (RowHeight - 29f) * 0.5f);
                r.Tex.isInteractive = false;
                r.Tex.isVisible = false;
            }

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
                if (r.Tex != null)
                {
                    bool pic = it != null && it.Tex != null;
                    r.Tex.isVisible = pic;
                    if (pic) r.Tex.texture = it.Tex;
                }

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
                if (ui.Pic != null) ui.Pic.isVisible = false;
                return;
            }

            ui.Name.text = item.Title.Length > 60 ? item.Title.Substring(0, 59) + "…" : item.Title;

            if (ui.Pic != null)
            {
                bool pic = item.Tex != null;
                ui.Pic.isVisible = pic;
                if (pic) ui.Pic.texture = item.Tex;
            }

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
                case QuayTool.Mode.Segment: LoadLockFromSelection(); break;
                case QuayTool.Mode.Templates: UpdateTemplateUi(); break;
                case QuayTool.Mode.Settings: LoadSettingsUi(); break;
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
                Flat.Keep(_modeButtons[i], (int)current == i);
            }

            bool addMode = current == QuayTool.Mode.AddNetwork;
            bool decalMode = current == QuayTool.Mode.Decal;
            bool propMode = current == QuayTool.Mode.PropLine;
            bool lockMode = current == QuayTool.Mode.Segment;
            bool tplMode = current == QuayTool.Mode.Templates;
            bool setMode = current == QuayTool.Mode.Settings;
            bool select = addMode || decalMode || propMode || lockMode || tplMode || setMode;

            if (tplMode && !_tpl.isVisible) ReloadTemplates(null);
            _tpl.isVisible = tplMode;
            _set.isVisible = setMode;
            if (!setMode && _help != null) _help.isVisible = false;
            if (!tplMode) _tplEditOpen = false;
            if (_dlg != null) _dlg.isVisible = false;
            _net.isVisible = addMode;
            _decal.isVisible = decalMode;
            _prop.isVisible = propMode;
            _lock.isVisible = lockMode;
            _bar.isVisible = select;
            ClosePopup();
            if (select) LoadFromSelection();
            UpdateTemplateEdit();
            UpdateBar();
            UpdateHeight();
        }

        private static string DescriptionFor(QuayTool.Mode current)
        {
            string description = Loc.T(TitleKeys[(int)current]) + ": " + QuayTool.HintFor(current);
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
            return m != QuayTool.Mode.None;
        }

        private static bool SliderMode(QuayTool.Mode m)
        {
            return m == QuayTool.Mode.AddNetwork || m == QuayTool.Mode.Decal || m == QuayTool.Mode.PropLine;
        }

        private float LeftOf(QuayTool.Mode m)
        {
            return m == QuayTool.Mode.AddNetwork ? _netLeft : m == QuayTool.Mode.Decal ? _decalLeft : m == QuayTool.Mode.PropLine ? _propLeft : m == QuayTool.Mode.Templates ? _tplHeight : m == QuayTool.Mode.Settings ? _setHeight : _lockHeight;
        }

        private float RightOf(QuayTool.Mode m)
        {
            return m == QuayTool.Mode.AddNetwork ? _netHeight : m == QuayTool.Mode.Decal ? _decalHeightFull : m == QuayTool.Mode.PropLine ? _propHeightFull : m == QuayTool.Mode.Templates ? _tplHeight : m == QuayTool.Mode.Settings ? _setHeight : _lockHeight;
        }

        private const float BarGap = 2f, BarH = 38f, StatusH = 22f;

        /// <summary>One window height for all the tools (the largest of them), so that the window does not jump when a tool is chosen.</summary>
        private float SliderToolsHeight()
        {
            float best = 0f;
            for (int i = 0; i < QuayTool.ModeCount; i++)
            {
                QuayTool.Mode m = (QuayTool.Mode)i;
                best = Mathf.Max(best, TopY + LeftOf(m) + BarGap + BarH + 6f + StatusH + 10f);
                best = Mathf.Max(best, TopY + RightOf(m) + 52f);
            }
            best = Mathf.Max(best, TopY + _tplEditHeight + 10f);
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
            bool tplMode = _tpl != null && _tpl.isVisible;
            bool setMode = _set != null && _set.isVisible;
            bool select = addMode || decalMode || propMode || lockMode || tplMode || setMode;
            bool sliders = addMode || decalMode || propMode;
            bool hasStatus = _status != null && !string.IsNullOrEmpty(_status.text);

            bool tplEdit = tplMode && _tplEdit != null && _tplEdit.isVisible;
            bool setHelp = setMode && _help != null && _help.isVisible;
            int columns = sliders || tplEdit || setHelp ? 2 : select ? 1 : 0;
            SetColumns(columns);

            float section = addMode ? _netHeight : decalMode ? _decalHeight : propMode ? _propHeight : tplMode ? _tplHeight : setMode ? _setHeight : lockMode ? _lockHeight : 0f;
            float left = addMode ? _netLeft : decalMode ? _decalLeft : propMode ? _propLeft : section;
            float act = addMode ? _netAct : decalMode ? _decalAct : propMode ? _propAct : setMode ? _setAct : lockMode ? _lockAct : -1f;

            if (select)
            {
                UIPanel active = addMode ? _net : decalMode ? _decal : propMode ? _prop : tplMode ? _tpl : setMode ? _set : _lock;
                active.height = section;
            }

            float need = Mathf.Max(TopY + ToolsMin + 8f, SliderToolsHeight());
            if (select) need = Mathf.Max(need, TopY + left + BarGap + BarH + 6f + StatusH + 10f);
            if (select && _openPopup != null)
            {
                float popupBottom = TopY + _openPopup.Popup.relativePosition.y + _openPopup.Popup.height + 10f;
                need = Mathf.Max(need, popupBottom);
            }
            height = need;
            _toolsPanel.height = height - TopY - 8f;
            PlaceToolButtons();
            FitHelp();

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

            UIButton first = addMode ? _netRemove : decalMode ? _dRemove : propMode ? _propRemove : setMode ? _setClear : lockMode ? _lockClear : null;
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

        /// <summary>The tools are stacked from the top; the last one (Templates) sits at the bottom of the column.</summary>
        private void PlaceToolButtons()
        {
            if (_modeButtons == null) return;
            for (int i = 0; i < _modeButtons.Length; i++)
            {
                bool last = i == _modeButtons.Length - 1;
                float y = last ? _toolsPanel.height - ToolBtnH - 6f : 6f + i * (ToolBtnH + 4f);
                _modeButtons[i].relativePosition = new Vector3(5f, y);
            }
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
            _dSwatch.backgroundSprite = Flat.Round;
            _dSwatch.atlas = Flat.Atlas;
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
            f.atlas = Flat.Atlas;
            FlatField(f);
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
                Flat.Keep(_dAdd, has && !d.Strip);
                Flat.Keep(_dAddPlane, has && d.Strip);
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
            y += 30f;

            // Invert: one button with a green check; the same mechanics as the former Invert tool
            _flipBtn = _lock.AddUIComponent<UIButton>();
            _flipBtn.width = PanelWidth - 20f;
            _flipBtn.height = 34f;
            _flipBtn.relativePosition = new Vector3(10f, y);
            StyleButton(_flipBtn);
            _flipBtn.textScale = 0.8f;
            _flipBtn.textHorizontalAlignment = UIHorizontalAlignment.Left;
            _flipBtn.textPadding = new RectOffset(52, 0, 0, 0);
            _flipBtn.text = Loc.T("seg_invert");
            _flipBtn.tooltip = Loc.T("seg_invert_tip");
            AddRowIcon(_flipBtn, "Invert.png", 12f);
            _flipBtn.eventClicked += delegate (UIComponent c, UIMouseEventParameter p)
            {
                QuayTool tool = QuayTool.Instance;
                if (tool != null) tool.FlipSelected();
            };
            y += 42f;

            _tgLock = MakeSegmentToggle(y, "seg_lock", "Lock.png", "seg_lock_tip", "lock"); y += 42f;
            _tgNop = MakeSegmentToggle(y, "seg_nop", "NoPedestrian.png", "seg_nop_tip", "nop"); y += 42f;
            _tgHide = MakeSegmentToggle(y, "seg_hide", "HideProps.png", "seg_hide_tip", "hp"); y += 42f;

            // clear: removes everything of the mod from the selected segments except the inversion
            _lockAct = y + 4f;
            _lockClear = _lock.AddUIComponent<UIButton>();
            _lockClear.width = PanelWidth - 20f;
            _lockClear.height = 32f;
            _lockClear.relativePosition = new Vector3(10f, _lockAct);
            StyleButton(_lockClear);
            _lockClear.text = Loc.T("seg_clear");
            _lockClear.textScale = 0.85f;
            _lockClear.tooltip = WrapText(Loc.T("seg_clear_tip"), 64);
            MakeRed(_lockClear);
            _lockClear.eventClicked += delegate (UIComponent c, UIMouseEventParameter p)
            {
                QuayTool tool = QuayTool.Instance;
                if (tool != null) tool.ClearSelectedSegments();
            };
            y = _lockAct + 38f;

            _lockHeight = y;
        }

        private static void AddRowIcon(UIButton row, string file, float x)
        {
            Texture2D icon = ModPaths.LoadIcon(file);
            if (icon == null) return;
            UITextureSprite sprite = row.AddUIComponent<UITextureSprite>();
            sprite.texture = icon;
            sprite.size = new Vector2(26f, 26f);
            sprite.relativePosition = new Vector3(x, (row.height - 26f) * 0.5f);
            sprite.isInteractive = false;
        }

        private Toggle MakeSegmentToggle(float y, string textKey, string iconFile, string tipKey, string kind)
        {
            Toggle t = MakeToggle(_lock, 10f, y, PanelWidth - 20f, textKey, null, delegate (bool v)
            {
                QuayTool tool = QuayTool.Instance;
                if (tool != null) tool.SetSegmentFlag(kind, v);
            });
            t.Button.height = 34f;
            t.Button.textPadding = new RectOffset(48, 0, 0, 0);
            t.Button.textScale = 0.7f;
            t.Pill.relativePosition = new Vector3(PanelWidth - 20f - SwitchW - 10f, (34f - SwitchH) * 0.5f);
            t.Button.tooltip = Loc.T(tipKey);
            AddRowIcon(t.Button, iconFile, 12f);
            return t;
        }

        private void LoadLockFromSelection()
        {
            if (_lock == null) return;

            QuayTool tool = QuayTool.Instance;
            int count = tool == null ? 0 : tool.Selection.Count;
            int locked = 0, peds = 0, hidden = 0;
            for (int i = 0; i < count; i++)
            {
                ushort seg = tool.Selection[i];
                if (LockStore.IsLocked(seg)) locked++;
                if (PedStore.Has(seg)) peds++;
                if (HideStore.Has(seg)) hidden++;
            }

            _lockSel.text = Loc.T("selected") + count;
            _flipBtn.isEnabled = count > 0;
            _tgLock.Button.isEnabled = count > 0;
            _tgNop.Button.isEnabled = count > 0;
            _tgHide.Button.isEnabled = count > 0;
            _lockClear.isEnabled = count > 0;
            SetToggle(_tgLock, count > 0 && locked == count);
            _tgLock.Mixed = locked > 0 && locked < count;
            UpdateToggle(_tgLock);
            SetToggle(_tgNop, count > 0 && peds == count);
            _tgNop.Mixed = peds > 0 && peds < count;
            UpdateToggle(_tgNop);
            SetToggle(_tgHide, count > 0 && hidden == count);
            _tgHide.Mixed = hidden > 0 && hidden < count;
            UpdateToggle(_tgHide);
            if (_resetBtn != null) _resetBtn.isEnabled = false;
        }

        // ---------- settings section ----------

        private void BuildSettingsSection()
        {
            _set = AddUIComponent<UIPanel>();
            _set.width = PanelWidth;
            _set.relativePosition = new Vector3(ToolsWidth, TopY);
            _set.isVisible = false;

            float y = 4f;
            _setFlip = MakeToggle(_set, 10f, y, PanelWidth - 20f, "set_quickflip", null, delegate (bool v) { Settings.QuickFlipEnabled = v; });
            _setFlip.Button.tooltip = WrapText(Loc.T("opt_quickflip"), 64);
            y += ToggleStride;
            _setShadows = MakeToggle(_set, 10f, y, PanelWidth - 20f, "set_shadows", null, delegate (bool v) { Settings.DecalReceiveShadows = v; });
            _setShadows.Button.tooltip = WrapText(Loc.T("opt_shadows"), 64);
            y += ToggleStride;
            _setMark = MakeToggle(_set, 10f, y, PanelWidth - 20f, "set_mark", null, delegate (bool v) { Settings.MarkEdited = v; });
            _setMark.Button.tooltip = WrapText(Loc.T("opt_mark"), 64);
            y += ToggleStride + 6f;

            // size of the floating icons: three buttons
            MakeLabel(_set, Loc.T("set_icon"), 12f, y, 0.8f);
            y += 22f;
            _setIcon = new UIButton[3];
            string[] tips = { "opt_icon1", "opt_icon2", "opt_icon3" };
            float w = (PanelWidth - 20f - 12f) / 3f;
            for (int i = 0; i < 3; i++)
            {
                int size = i + 1;
                UIButton b = _set.AddUIComponent<UIButton>();
                b.size = new Vector2(w, 30f);
                b.relativePosition = new Vector3(10f + i * (w + 6f), y);
                StyleButton(b);
                b.textScale = 0.8f;
                b.text = size.ToString();
                b.tooltip = Loc.T(tips[i]);
                b.eventClicked += delegate (UIComponent c, UIMouseEventParameter p)
                {
                    Settings.MarkIconSize = size;
                    LoadSettingsUi();
                };
                _setIcon[i] = b;
            }
            y += 40f;

            _setHelp = _set.AddUIComponent<UIButton>();
            _setHelp.width = PanelWidth - 20f;
            _setHelp.height = 34f;
            _setHelp.relativePosition = new Vector3(10f, y);
            StyleButton(_setHelp);
            _setHelp.textScale = 0.8f;
            _setHelp.text = Loc.T("set_help");
            _setHelp.tooltip = Loc.T("set_help_tip");
            _setHelp.eventClicked += delegate (UIComponent c, UIMouseEventParameter p)
            {
                _help.isVisible = !_help.isVisible;
                if (_help.isVisible) FillHelp();
                UpdateHeight();
            };
            y += 44f;

            BuildHelp();

            // the button that clears everything sits at the bottom, above the undo / redo / reset bar
            y += 10f;
            _setAct = y;
            _setClear = _set.AddUIComponent<UIButton>();
            _setClear.width = PanelWidth - 20f;
            _setClear.height = 34f;
            _setClear.relativePosition = new Vector3(10f, y);
            StyleButton(_setClear);
            _setClear.textScale = 0.8f;
            _setClear.text = Loc.T("set_clear");
            _setClear.tooltip = WrapText(Loc.T("set_clear_tip"), 64);
            MakeRed(_setClear);
            _setClear.eventClicked += delegate (UIComponent c, UIMouseEventParameter p)
            {
                Ask(Loc.T("clear_all_ask"), delegate
                {
                    QuayTool tool = QuayTool.Instance;
                    if (tool != null) tool.ClearAllModifications();
                }, false);
            };
            y += 44f;

            _setHeight = y;
        }

        /// <summary>The third column of the settings: how to control the mod.</summary>
        private void BuildHelp()
        {
            _help = AddUIComponent<UIPanel>();
            _help.width = PanelWidth;
            _help.relativePosition = new Vector3(ToolsWidth + PanelWidth, TopY);
            _help.isVisible = false;

            MakeLabel(_help, Loc.T("help_title"), 12f, 4f, 0.95f);

            // the text lives in a scrollable list: headings and bulleted lines
            _helpScroll = _help.AddUIComponent<UIScrollablePanel>();
            _helpScroll.relativePosition = new Vector3(8f, 34f);
            _helpScroll.size = new Vector2(PanelWidth - 16f - 14f, 400f);
            _helpScroll.autoLayout = true;
            _helpScroll.autoLayoutDirection = LayoutDirection.Vertical;
            _helpScroll.autoLayoutPadding = new RectOffset(0, 0, 0, 3);
            _helpScroll.clipChildren = true;
            _helpScroll.scrollWheelDirection = UIOrientation.Vertical;
            _helpScroll.builtinKeyNavigation = false;

            _helpBar = _help.AddUIComponent<UIScrollbar>();
            _helpBar.width = 10f;
            _helpBar.orientation = UIOrientation.Vertical;
            _helpBar.pivot = UIPivotPoint.TopLeft;
            _helpBar.minValue = 0f;
            _helpBar.incrementAmount = 40f;
            UISlicedSprite track = _helpBar.AddUIComponent<UISlicedSprite>();
            track.spriteName = "EmptySprite";
            track.color = new Color32(24, 34, 37, 255);
            track.relativePosition = Vector3.zero;
            track.size = new Vector2(10f, 400f);
            _helpBar.trackObject = track;
            _helpTrack = track;
            UISlicedSprite thumb = track.AddUIComponent<UISlicedSprite>();
            thumb.spriteName = "EmptySprite";
            thumb.color = Accent;
            thumb.width = 10f;
            _helpBar.thumbObject = thumb;
            _helpScroll.verticalScrollbar = _helpBar;

            FillHelp();
            _help.height = 560f;
        }

        /// <summary>Fills the help list (again each time it is opened: the hotkeys may have been changed).</summary>
        private void FillHelp()
        {
            List<UIComponent> old = new List<UIComponent>(_helpScroll.components);
            for (int i = 0; i < old.Count; i++) Destroy(old[i].gameObject);
            string[] lines = Settings.HelpText().Split('\n');
            bool heading = true;
            float w = _helpScroll.width;
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (line.Length == 0)
                {
                    heading = true;
                    continue;
                }
                UILabel l = _helpScroll.AddUIComponent<UILabel>();
                l.autoSize = false;
                l.autoHeight = true;
                l.wordWrap = true;
                l.isInteractive = false;
                if (heading)
                {
                    l.width = w;
                    l.text = line;
                    l.textScale = 0.9f;
                    l.textColor = Accent;
                    l.padding = new RectOffset(0, 0, 10, 2);
                    heading = false;
                }
                else
                {
                    l.width = w;
                    l.text = line;
                    l.textScale = 0.78f;
                    l.textColor = TextMain;
                    l.padding = new RectOffset(14, 0, 0, 3);
                    UILabel dot = l.AddUIComponent<UILabel>();
                    dot.text = "\u2022";
                    dot.textScale = 0.8f;
                    dot.textColor = Accent;
                    dot.isInteractive = false;
                    dot.relativePosition = new Vector3(3f, 0f);
                }
            }
        }

        /// <summary>Fits the help list to the window height.</summary>
        private void FitHelp()
        {
            if (_help == null || _helpScroll == null) return;
            float h = Mathf.Max(100f, height - TopY - 8f - 34f - 10f);
            _help.height = h + 44f;
            _helpScroll.height = h;
            _helpTrack.height = h;
            _helpBar.height = h;
            _helpBar.relativePosition = new Vector3(PanelWidth - 18f, 34f);
        }

        private void LoadSettingsUi()
        {
            if (_set == null) return;
            SetToggle(_setFlip, Settings.QuickFlipEnabled);
            SetToggle(_setShadows, Settings.DecalReceiveShadows);
            SetToggle(_setMark, Settings.MarkEdited);
            for (int i = 0; i < 3; i++)
            {
                _setIcon[i].state = Settings.MarkIconSize == i + 1 ? UIButton.ButtonState.Focused : UIButton.ButtonState.Normal;
                Flat.Keep(_setIcon[i], Settings.MarkIconSize == i + 1);
            }
            if (_resetBtn != null) _resetBtn.isEnabled = false;
        }

        // ---------- templates section ----------

        private void BuildTemplateSection()
        {
            _tpl = AddUIComponent<UIPanel>();
            _tpl.width = PanelWidth;
            _tpl.relativePosition = new Vector3(ToolsWidth, TopY);
            _tpl.isVisible = false;

            float y = 0f;
            _tplSel = MakeLabel(_tpl, string.Empty, 12f, y, 0.85f);
            y += 30f;

            _tplName = MakeTextBox(_tpl, 10f, y, PanelWidth - 20f, Loc.T("tpl_name_tip"), delegate (string text)
            {
                string clean = TemplateStore.CleanName(text);
                if (clean != text) _tplName.text = clean; // forbidden characters are dropped, the length is limited
            });
            y += 28f;

            _tplSave = _tpl.AddUIComponent<UIButton>();
            _tplSave.width = PanelWidth - 20f;
            _tplSave.height = 34f;
            _tplSave.relativePosition = new Vector3(10f, y);
            StyleButton(_tplSave);
            _tplSave.textScale = 0.8f;
            _tplSave.text = Loc.T("tpl_save");
            _tplSave.tooltip = Loc.T("tpl_save_tip");
            _tplSave.eventClicked += delegate (UIComponent c, UIMouseEventParameter p) { OnTemplateSave(); };
            y += 46f;

            MakeLabel(_tpl, Loc.T("tpl_list"), 12f, y, 0.85f);
            y += 20f;

            _tplUi = new PickerUi();
            _tplUi.Parent = _tpl;
            _tplUi.EmptyText = Loc.T("tpl_pick");
            _tplUi.Kind = Favorites.Fences; // not used: the templates have no favourites (no keys)
            _tplUi.ShowEmpty = false;
            _tplUi.Textured = true;
            _tplUi.GetItems = delegate (string text)
            {
                List<PickItem> items = new List<PickItem>();
                for (int i = 0; i < _templates.Count; i++)
                {
                    if (!Matches(text, _templates[i].Name, null)) continue;
                    items.Add(TemplateItem(_templates[i]));
                }
                return items;
            };
            _tplUi.OnPicked = delegate (PickItem item)
            {
                _tplPicked = item == null ? null : item.Tag as QuayTemplate;
                _tplLineOffset = 0;
                UpdateTemplateUi();
            };
            BuildHeader(_tpl, _tplUi, y);

            // the pencil on the right of the picked template opens its settings (the third column)
            _tplPencil = _tpl.AddUIComponent<UIButton>();
            _tplPencil.size = new Vector2(36f, 36f);
            _tplPencil.relativePosition = new Vector3(PanelWidth - 10f - 42f, y + 20f);
            StyleButton(_tplPencil);
            _tplPencil.color = FieldBg; // a darker background sets the pencil apart from the row
            _tplPencil.hoveredColor = FieldBg;
            _tplPencil.focusedColor = FieldBg;
            _tplPencil.disabledColor = FieldBg;
            _tplPencil.tooltip = Loc.T("tpl_edit_tip");
            AddRowIcon(_tplPencil, "Pencil.png", 5f);
            _tplPencil.eventClicked += delegate (UIComponent c, UIMouseEventParameter p)
            {
                _tplEditOpen = !_tplEditOpen;
                _tplEditFor = null;
                UpdateTemplateUi();
            };
            y += 84f;

            _tplInfo = MakeLabel(_tpl, string.Empty, 12f, y, 0.7f);
            _tplInfo.autoSize = false;
            _tplInfo.wordWrap = true;
            _tplInfo.width = PanelWidth - 24f;
            _tplInfo.height = 34f;
            y += 38f;

            _tplApply = _tpl.AddUIComponent<UIButton>();
            _tplApply.width = PanelWidth - 20f;
            _tplApply.height = 34f;
            _tplApply.relativePosition = new Vector3(10f, y);
            StyleButton(_tplApply);
            _tplApply.textScale = 0.8f;
            _tplApply.text = Loc.T("tpl_apply");
            _tplApply.tooltip = Loc.T("tpl_apply_tip");
            _tplApply.eventClicked += delegate (UIComponent c, UIMouseEventParameter p)
            {
                QuayTool tool = QuayTool.Instance;
                if (tool != null && _tplPicked != null) tool.ApplyTemplate(_tplPicked);
            };
            y += 42f;

            _tplHeight = y;

            BuildPopup(_tplUi); // last: the list is drawn above the controls below its button
            BuildTemplateEdit();
        }

        private static PickItem TemplateItem(QuayTemplate t)
        {
            PickItem it = new PickItem();
            it.Title = t.Name;
            it.Tex = t.Thumb;
            it.Tag = t;
            return it;
        }

        /// <summary>The third column: name, picture, copy / delete, the lines of the picked template.</summary>
        private void BuildTemplateEdit()
        {
            _tplEdit = AddUIComponent<UIPanel>();
            _tplEdit.width = PanelWidth;
            _tplEdit.relativePosition = new Vector3(ToolsWidth + PanelWidth, TopY);
            _tplEdit.isVisible = false;

            float y = 0f;
            MakeLabel(_tplEdit, Loc.T("tpl_edit_title"), 12f, y, 0.9f);
            y += 28f;

            float renameW = 104f;
            _tplEditName = MakeTextBox(_tplEdit, 10f, y + 4f, PanelWidth - 30f - renameW, Loc.T("tpl_name_tip"), delegate (string text)
            {
                string clean = TemplateStore.CleanName(text);
                if (clean != text) _tplEditName.text = clean;
            });
            _tplEditRename = MakeSmallButton(_tplEdit, PanelWidth - 10f - renameW, y, renameW, Loc.T("tpl_rename"), OnTemplateRename);
            y += 38f;

            // the picture (16:9)
            float picW = PanelWidth - 20f, picH = picW * 9f / 16f;
            UIPanel frame = _tplEdit.AddUIComponent<UIPanel>();
            frame.size = new Vector2(picW, picH);
            frame.relativePosition = new Vector3(10f, y);
            frame.backgroundSprite = Flat.Round;
            frame.atlas = Flat.Atlas;
            frame.color = FieldBg;
            frame.disabledColor = Dim; // otherwise a disabled panel turns white
            frame.isInteractive = false;
            _tplEditPic = _tplEdit.AddUIComponent<UITextureSprite>();
            _tplEditPic.size = new Vector2(picW - 4f, picH - 4f);
            _tplEditPic.relativePosition = new Vector3(12f, y + 2f);
            _tplEditPic.isInteractive = false;
            _tplEditNoPic = MakeLabel(_tplEdit, Loc.T("tpl_nopic"), 20f, y + picH * 0.5f - 8f, 0.8f);
            y += picH + 8f;

            _tplEditShot = MakeSmallButton(_tplEdit, 10f, y, PanelWidth - 20f, Loc.T("tpl_shot"), OnTemplateShot);
            _tplEditShot.height = 34f;
            _tplEditShot.tooltip = Loc.T("tpl_shot_tip");
            y += 42f;

            float half = (PanelWidth - 26f) * 0.5f;
            _tplEditDup = MakeSmallButton(_tplEdit, 10f, y, half, Loc.T("tpl_dup"), OnTemplateDuplicate);
            _tplEditDup.height = 34f;
            _tplEditDelete = MakeSmallButton(_tplEdit, 16f + half, y, half, Loc.T("tpl_delete"), OnTemplateDelete);
            _tplEditDelete.height = 34f;
            _tplEditDelete.tooltip = Loc.T("tpl_delete_tip");
            MakeRed(_tplEditDelete);
            y += 42f;

            _tplEditFolder = MakeSmallButton(_tplEdit, 10f, y, PanelWidth - 20f, Loc.T("tpl_folder"), delegate { TemplateStore.OpenFolder(); });
            _tplEditFolder.height = 34f;
            _tplEditFolder.tooltip = Loc.T("tpl_folder_tip");
            y += 46f;

            MakeLabel(_tplEdit, Loc.T("tpl_lines_title"), 12f, y, 0.85f);
            y += 22f;

            _tplLineBtn = new UIButton[TplLineRows];
            _tplLineDel = new UIButton[TplLineRows];
            for (int i = 0; i < TplLineRows; i++)
            {
                int row = i;
                UIButton b = _tplEdit.AddUIComponent<UIButton>();
                b.width = PanelWidth - 20f - 34f;
                b.height = 28f;
                b.relativePosition = new Vector3(10f, y + i * 30f);
                StyleButton(b);
                b.textScale = 0.75f;
                b.textHorizontalAlignment = UIHorizontalAlignment.Left;
                b.textPadding = new RectOffset(8, 0, 0, 0);
                b.isInteractive = false;
                _tplLineBtn[i] = b;

                UIButton d = _tplEdit.AddUIComponent<UIButton>();
                d.size = new Vector2(28f, 28f);
                d.relativePosition = new Vector3(PanelWidth - 10f - 28f, y + i * 30f);
                StyleButton(d);
                d.text = "X";
                d.textScale = 0.8f;
                d.tooltip = Loc.T("tpl_line_del");
                MakeRed(d);
                d.eventClicked += delegate (UIComponent c, UIMouseEventParameter p) { OnTemplateLineDelete(row); };
                _tplLineDel[i] = d;
            }
            y += TplLineRows * 30f + 2f;

            _tplEditPrev = MakeSmallButton(_tplEdit, 10f, y, 44f, "<", delegate { _tplLineOffset = Mathf.Max(0, _tplLineOffset - TplLineRows); UpdateTemplateEdit(); });
            _tplEditNext = MakeSmallButton(_tplEdit, PanelWidth - 54f, y, 44f, ">", delegate { _tplLineOffset += TplLineRows; UpdateTemplateEdit(); });
            _tplEditPage = MakeLabel(_tplEdit, string.Empty, 62f, y + 7f, 0.75f);
            _tplEditPage.autoSize = false;
            _tplEditPage.width = PanelWidth - 124f;
            _tplEditPage.textAlignment = UIHorizontalAlignment.Center;
            y += 36f;

            _tplEditHeight = y;
        }

        /// <summary>Reads the template files again; pick = the template to select afterwards (null: keep the current one if it still exists).</summary>
        private void ReloadTemplates(QuayTemplate pick)
        {
            string keepId = pick != null ? pick.Id : _tplPicked != null ? _tplPicked.Id : null;
            TemplateStore.Release(_templates);
            _templates = TemplateStore.LoadAll();
            _tplPicked = null;
            for (int i = 0; i < _templates.Count; i++)
            {
                if (_templates[i].Id == keepId) _tplPicked = _templates[i];
            }
            _tplEditFor = null;
            UpdateTemplateUi();
        }

        private QuayTemplate TemplateById(string id)
        {
            for (int i = 0; i < _templates.Count; i++)
            {
                if (_templates[i].Id == id) return _templates[i];
            }
            return null;
        }

        private void UpdateTemplateUi()
        {
            if (_tpl == null) return;

            QuayTool tool = QuayTool.Instance;
            int count = tool == null ? 0 : tool.Selection.Count;
            _tplSel.text = Loc.T("selected") + count;

            SetHeaderItem(_tplUi, _tplPicked == null ? null : TemplateItem(_tplPicked));
            _tplPencil.isVisible = _tplPicked != null;
            if (_tplPicked == null) _tplEditOpen = false;

            if (_tplPicked != null)
            {
                _tplInfo.text = Loc.F("tpl_lines", _tplPicked.Nets.Count, _tplPicked.Props.Count, _tplPicked.Decals.Count);
            }
            else
            {
                _tplInfo.text = string.Empty;
            }

            _tplSave.isEnabled = count > 0;
            _tplName.isEnabled = count > 0;
            _tplApply.isEnabled = count > 0 && _tplPicked != null;
            if (_resetBtn != null) _resetBtn.isEnabled = false;

            UpdateTemplateEdit();
            UpdateHeight();
        }

        private static string NetTitle(string model)
        {
            if (string.IsNullOrEmpty(model)) return Loc.T("tpl_empty_model");
            try
            {
                NetInfo info = PrefabCollection<NetInfo>.FindLoaded(model);
                if (info != null) return PropCatalog.TitleOf(info);
            }
            catch (Exception)
            {
                // the name is shown instead
            }
            return model;
        }

        private static string PropTitle(string key)
        {
            return string.IsNullOrEmpty(key) ? Loc.T("tpl_empty_model") : PropCatalog.TitleOfKey(key);
        }

        private static string DecalTitle(string key)
        {
            if (string.IsNullOrEmpty(key)) return Loc.T("tpl_plain_path");
            try
            {
                PropInfo info = DecalCatalog.Find(key);
                if (info != null) return PropCatalog.TitleOf(info);
            }
            catch (Exception)
            {
                // the name is shown instead
            }
            return key;
        }

        private void UpdateTemplateEdit()
        {
            if (_tplEdit == null) return;

            bool open = _tplEditOpen && _tplPicked != null && _tpl.isVisible;
            _tplEdit.isVisible = open;
            if (!open) return;

            QuayTemplate t = _tplPicked;
            if (_dlg != null && _dlg.isVisible && _tplEditFor != t.Id) _dlg.isVisible = false;
            if (_tplEditFor != t.Id)
            {
                _tplEditFor = t.Id;
                _tplEditName.text = t.Name;
                _tplLineOffset = 0;
            }

            bool pic = t.Thumb != null;
            _tplEditPic.isVisible = pic;
            if (pic) _tplEditPic.texture = t.Thumb;
            _tplEditNoPic.isVisible = !pic;

            // the lines: network-lines, then props-lines, then texture-paths
            int total = t.Nets.Count + t.Props.Count + t.Decals.Count;
            int pages = Mathf.Max(1, (total + TplLineRows - 1) / TplLineRows);
            _tplLineOffset = Mathf.Clamp(_tplLineOffset, 0, (pages - 1) * TplLineRows);
            for (int i = 0; i < TplLineRows; i++)
            {
                int index = _tplLineOffset + i;
                bool has = index < total;
                _tplLineBtn[i].isVisible = has;
                _tplLineDel[i].isVisible = has;
                if (!has) continue;

                int kind, k;
                LineAt(t, index, out kind, out k);
                string text = kind == 0 ? Loc.T("tpl_k_net") + NetTitle(t.Nets[k].Model)
                            : kind == 1 ? Loc.T("tpl_k_prop") + PropTitle(t.Props[k].Prop)
                            : Loc.T("tpl_k_decal") + DecalTitle(t.Decals[k].Prop);
                _tplLineBtn[i].text = Shorten(text);
                _tplLineBtn[i].tooltip = text;
            }
            _tplEditPage.text = (_tplLineOffset / TplLineRows + 1) + " / " + pages;
            _tplEditPrev.isEnabled = _tplLineOffset > 0;
            _tplEditNext.isEnabled = _tplLineOffset + TplLineRows < total;
        }

        /// <summary>The line at a flat index (network-lines first): its kind (0 net, 1 props, 2 path) and its index in that kind.</summary>
        private static void LineAt(QuayTemplate t, int index, out int kind, out int k)
        {
            if (index < t.Nets.Count) { kind = 0; k = index; return; }
            index -= t.Nets.Count;
            if (index < t.Props.Count) { kind = 1; k = index; return; }
            kind = 2;
            k = index - t.Props.Count;
        }

        private static void Say(string text)
        {
            QuayTool tool = QuayTool.Instance;
            if (tool != null) tool.Say(text);
        }

        /// <summary>Takes the picture of the template: the scene as the camera sees it now, without the interface.</summary>
        private void CapturePicture(QuayTemplate t)
        {
            QuayTool tool = QuayTool.Instance;
            if (tool == null) return;

            string id = t.Id;
            tool.CaptureThumbnail(TemplateStore.ThumbPath(t), delegate (bool ok)
            {
                QuayTemplate now = TemplateById(id);
                if (now != null && ok) TemplateStore.LoadThumb(now);
                if (!ok) Say(Loc.T("tpl_shot_failed"));
                UpdateTemplateUi();
            });
        }

        private void OnTemplateSave()
        {
            QuayTool tool = QuayTool.Instance;
            if (tool == null) return;

            string name = TemplateStore.CleanName(_tplName.text).Trim();
            if (name.Length == 0)
            {
                tool.Say(Loc.T("tpl_noname"));
                return;
            }

            ReloadTemplates(null); // the list on disk may have changed (names must stay unique)
            QuayTemplate t = tool.SaveTemplate(name, _templates);
            if (t == null) return;

            _tplName.text = string.Empty;
            ReloadTemplates(t);
            CapturePicture(TemplateById(t.Id));
        }

        private void OnTemplateShot()
        {
            if (_tplPicked == null) return;
            CapturePicture(_tplPicked);
        }

        private void OnTemplateRename()
        {
            if (_tplPicked == null) return;

            string name = TemplateStore.CleanName(_tplEditName.text).Trim();
            if (name.Length == 0)
            {
                Say(Loc.T("tpl_noname"));
                return;
            }

            if (TemplateStore.Rename(_tplPicked, name, _templates)) Say(Loc.F("tpl_renamed", _tplPicked.Name));
            else Say(Loc.T("tpl_save_failed"));
            _tplEditFor = null;
            _templates.Sort(delegate (QuayTemplate a, QuayTemplate b) { return string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase); });
            UpdateTemplateUi();
        }

        private void OnTemplateDuplicate()
        {
            if (_tplPicked == null) return;

            QuayTemplate copy = TemplateStore.Duplicate(_tplPicked, _templates);
            if (copy == null)
            {
                Say(Loc.T("tpl_save_failed"));
                return;
            }

            Say(Loc.F("tpl_duplicated", copy.Name));
            TemplateStore.Release(new List<QuayTemplate> { copy }); // the list is read again below, with its own pictures
            ReloadTemplates(copy);
        }

        /// <summary>Asks "Delete / Cancel" in a solid box (third column, or over the first one when there is no third); yes runs when Delete is pressed.</summary>
        private void Ask(string text, Action yes, bool thirdColumn)
        {
            if (_dlg == null) return;
            ClosePopup();
            _dlg.relativePosition = new Vector3(thirdColumn ? ToolsWidth + PanelWidth + 10f : ToolsWidth + 10f, TopY + 120f);
            _dlgText.text = text;
            _dlgYes = yes;
            _dlg.isVisible = true;
            _dlg.BringToFront();
        }

        private void BuildDialog()
        {
            // a solid box with a lighter frame (nothing shines through)
            _dlg = AddUIComponent<UIPanel>();
            _dlg.size = new Vector2(PanelWidth - 20f, 170f);
            _dlg.backgroundSprite = Flat.Round;
            _dlg.atlas = Flat.Atlas;
            _dlg.color = new Color32(96, 112, 128, 255);
            _dlg.isInteractive = true;
            _dlg.isVisible = false;

            UIPanel fill = _dlg.AddUIComponent<UIPanel>();
            fill.size = new Vector2(_dlg.width - 4f, _dlg.height - 4f);
            fill.relativePosition = new Vector3(2f, 2f);
            fill.backgroundSprite = Flat.Round;
            fill.atlas = Flat.Atlas;
            fill.color = new Color32(30, 40, 46, 255);
            fill.isInteractive = false;

            _dlgText = _dlg.AddUIComponent<UILabel>();
            _dlgText.autoSize = false;
            _dlgText.wordWrap = true;
            _dlgText.textScale = 0.9f;
            _dlgText.textAlignment = UIHorizontalAlignment.Center;
            _dlgText.size = new Vector2(_dlg.width - 30f, 80f);
            _dlgText.relativePosition = new Vector3(15f, 24f);
            _dlgText.isInteractive = false;

            float w = (_dlg.width - 36f) * 0.5f;
            UIButton del = MakeSmallButton(_dlg, 12f, 118f, w, Loc.T("tpl_do_delete"), delegate
            {
                _dlg.isVisible = false;
                Action yes = _dlgYes;
                _dlgYes = null;
                if (yes != null) yes();
            });
            MakeRed(del);
            MakeSmallButton(_dlg, 24f + w, 118f, w, Loc.T("tpl_cancel"), delegate
            {
                _dlg.isVisible = false;
                _dlgYes = null;
            });
        }

        private void OnTemplateDelete()
        {
            if (_tplPicked == null) return;
            Ask(Loc.T("tpl_ask_tpl"), DeleteTemplate, true);
        }

        private void DeleteTemplate()
        {
            if (_tplPicked == null) return;
            string name = _tplPicked.Name;
            bool ok = TemplateStore.Delete(_tplPicked);
            _tplPicked = null;
            _tplEditOpen = false;
            ReloadTemplates(null);
            Say(ok ? Loc.F("tpl_deleted", name) : Loc.T("tpl_delete_failed"));
        }

        private void OnTemplateLineDelete(int row)
        {
            if (_tplPicked == null) return;

            int index = _tplLineOffset + row;
            string id = _tplPicked.Id;
            Ask(Loc.T("tpl_ask_line"), delegate
            {
                QuayTemplate t = TemplateById(id);
                if (t == null) return;
                int kind, k;
                LineAt(t, index, out kind, out k);
                if (TemplateStore.RemoveLine(t, kind, k)) Say(Loc.T("tpl_line_removed"));
                else Say(Loc.T("tpl_save_failed"));
                UpdateTemplateUi();
            }, true);
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
                if (tool != null && tool.CurrentMode == QuayTool.Mode.Segment) LoadLockFromSelection();
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
                    _netUi.Search, _decalUi.Search, _propUi.Search, _tplName, _tplUi.Search, _tplEditName, _pStepV, _pStartV, _pEndV, _pLateralV, _pLiftV
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
