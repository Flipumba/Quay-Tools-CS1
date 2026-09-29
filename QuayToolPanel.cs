using System.Collections.Generic;
using ColossalFramework.UI;
using UnityEngine;

namespace QuayTools
{
    /// <summary>
    /// Small floating window: mode buttons on top; in "Add network model" mode a paged list of
    /// fence models appears below. Shown while QuayTool is active.
    /// </summary>
    public class QuayToolPanel : UIPanel
    {
        private const float PanelWidth = 250f;
        private const float ButtonHeight = 40f;
        private const float BaseHeight = 240f;
        private const float PickerTop = 236f;
        private const float PickerHeight = 340f;
        private const int RowsPerPage = 8;
        private const float RowHeight = 32f;

        public static QuayToolPanel Instance { get; private set; }

        private static readonly string[] Titles = { "Invert segment", "Remove pedestrian path", "Add network model" };
        private static readonly string[] IconFiles = { "Invert.png", "NoPedestrian.png", "Network.png" };

        private UIButton[] _buttons;
        private UILabel _hint;
        private UILabel _status;
        private bool _built;

        // picker (fence list)
        private UIPanel _picker;
        private UIButton _removeButton;
        private UIButton[] _rows;
        private UISprite[] _rowIcons;
        private UIButton _prev;
        private UIButton _next;
        private UILabel _pageLabel;
        private int _page;

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
            if (Instance != null) Instance.Hide();
        }

        public static void DestroyPanel()
        {
            if (Instance != null)
            {
                Destroy(Instance.gameObject);
                Instance = null;
            }
        }

        public override void Awake()
        {
            base.Awake();
            width = PanelWidth;
            height = BaseHeight;
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
            Refresh();
        }

        private static void StyleButton(UIButton button)
        {
            button.normalBgSprite = "ButtonMenu";
            button.hoveredBgSprite = "ButtonMenuHovered";
            button.pressedBgSprite = "ButtonMenuPressed";
            button.focusedBgSprite = "ButtonMenuFocused";
            button.disabledBgSprite = "ButtonMenuDisabled";
        }

        private void Build()
        {
            if (_built) return;
            _built = true;

            UIDragHandle drag = AddUIComponent<UIDragHandle>();
            drag.width = width;
            drag.height = 34f;
            drag.relativePosition = Vector3.zero;
            drag.target = this;

            UILabel title = AddUIComponent<UILabel>();
            title.text = "Quay Tools";
            title.textScale = 1.0f;
            title.relativePosition = new Vector3(12f, 9f);
            title.isInteractive = false;

            _buttons = new UIButton[3];
            for (int i = 0; i < 3; i++)
            {
                QuayTool.Mode mode = (QuayTool.Mode)i;
                UIButton button = AddUIComponent<UIButton>();
                button.width = PanelWidth - 20f;
                button.height = ButtonHeight;
                button.relativePosition = new Vector3(10f, 38f + i * (ButtonHeight + 4f));
                StyleButton(button);
                button.text = Titles[i];
                button.textScale = 0.85f;
                button.textHorizontalAlignment = UIHorizontalAlignment.Left;
                button.textVerticalAlignment = UIVerticalAlignment.Middle;
                button.textPadding = new RectOffset(48, 0, 0, 0);
                button.isEnabled = QuayTool.IsImplemented(mode);
                button.tooltip = button.isEnabled ? Titles[i] : Titles[i] + " (coming soon)";

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

                _buttons[i] = button;
            }

            _hint = AddUIComponent<UILabel>();
            _hint.width = PanelWidth - 24f;
            _hint.wordWrap = true;
            _hint.autoSize = false;
            _hint.height = 40f;
            _hint.textScale = 0.7f;
            _hint.relativePosition = new Vector3(12f, 172f);
            _hint.isInteractive = false;

            _status = AddUIComponent<UILabel>();
            _status.width = PanelWidth - 24f;
            _status.textScale = 0.8f;
            _status.textColor = new Color32(120, 220, 140, 255);
            _status.relativePosition = new Vector3(12f, 214f);
            _status.isInteractive = false;

            BuildPicker();
        }

        private void BuildPicker()
        {
            _picker = AddUIComponent<UIPanel>();
            _picker.width = PanelWidth;
            _picker.height = PickerHeight;
            _picker.relativePosition = new Vector3(0f, PickerTop);
            _picker.isVisible = false;

            _removeButton = _picker.AddUIComponent<UIButton>();
            _removeButton.width = PanelWidth - 20f;
            _removeButton.height = 30f;
            _removeButton.relativePosition = new Vector3(10f, 0f);
            StyleButton(_removeButton);
            _removeButton.text = "Remove fence";
            _removeButton.textScale = 0.8f;
            _removeButton.eventClicked += delegate (UIComponent c, UIMouseEventParameter p)
            {
                QuayTool tool = QuayTool.Instance;
                if (tool == null) return;
                tool.RemoveFence = !tool.RemoveFence;
                RefreshPicker();
            };

            _rows = new UIButton[RowsPerPage];
            _rowIcons = new UISprite[RowsPerPage];
            for (int r = 0; r < RowsPerPage; r++)
            {
                int slot = r;
                UIButton row = _picker.AddUIComponent<UIButton>();
                row.width = PanelWidth - 20f;
                row.height = RowHeight;
                row.relativePosition = new Vector3(10f, 34f + r * (RowHeight + 2f));
                StyleButton(row);
                row.textScale = 0.75f;
                row.textHorizontalAlignment = UIHorizontalAlignment.Left;
                row.textVerticalAlignment = UIVerticalAlignment.Middle;
                row.textPadding = new RectOffset(40, 0, 0, 0);
                row.eventClicked += delegate (UIComponent c, UIMouseEventParameter p) { OnRowClicked(slot); };

                UISprite icon = row.AddUIComponent<UISprite>();
                icon.size = new Vector2(28f, 28f);
                icon.relativePosition = new Vector3(6f, 2f);
                icon.isInteractive = false;

                _rows[r] = row;
                _rowIcons[r] = icon;
            }

            float navTop = 34f + RowsPerPage * (RowHeight + 2f) + 2f;

            _prev = _picker.AddUIComponent<UIButton>();
            _prev.width = 50f;
            _prev.height = 28f;
            _prev.relativePosition = new Vector3(10f, navTop);
            StyleButton(_prev);
            _prev.text = "<";
            _prev.eventClicked += delegate (UIComponent c, UIMouseEventParameter p) { _page--; RefreshPicker(); };

            _next = _picker.AddUIComponent<UIButton>();
            _next.width = 50f;
            _next.height = 28f;
            _next.relativePosition = new Vector3(PanelWidth - 60f, navTop);
            StyleButton(_next);
            _next.text = ">";
            _next.eventClicked += delegate (UIComponent c, UIMouseEventParameter p) { _page++; RefreshPicker(); };

            _pageLabel = _picker.AddUIComponent<UILabel>();
            _pageLabel.width = 100f;
            _pageLabel.autoSize = false;
            _pageLabel.height = 20f;
            _pageLabel.textAlignment = UIHorizontalAlignment.Center;
            _pageLabel.textScale = 0.75f;
            _pageLabel.relativePosition = new Vector3((PanelWidth - 100f) / 2f, navTop + 6f);
            _pageLabel.isInteractive = false;
        }

        private void OnRowClicked(int slot)
        {
            List<FenceEntry> list = FenceCatalog.Entries;
            int index = _page * RowsPerPage + slot;
            if (index < 0 || index >= list.Count) return;

            QuayTool tool = QuayTool.Instance;
            if (tool == null) return;

            tool.SelectedFence = list[index].Info;
            tool.RemoveFence = false;
            RefreshPicker();
        }

        public void Refresh()
        {
            if (!_built) return;

            QuayTool tool = QuayTool.Instance;
            QuayTool.Mode current = tool != null ? tool.CurrentMode : QuayTool.Mode.Invert;

            for (int i = 0; i < _buttons.Length; i++)
            {
                if (!_buttons[i].isEnabled) continue;
                _buttons[i].state = (int)current == i ? UIButton.ButtonState.Focused : UIButton.ButtonState.Normal;
            }

            _hint.text = QuayTool.HintFor(current);

            bool addMode = current == QuayTool.Mode.AddNetwork;
            _picker.isVisible = addMode;
            height = BaseHeight + (addMode ? PickerHeight : 0f);
            if (addMode) RefreshPicker();
        }

        private void RefreshPicker()
        {
            if (!_built || _picker == null) return;

            QuayTool tool = QuayTool.Instance;
            List<FenceEntry> list = FenceCatalog.Entries;

            int pageCount = Mathf.Max(1, (list.Count + RowsPerPage - 1) / RowsPerPage);
            _page = Mathf.Clamp(_page, 0, pageCount - 1);

            bool removing = tool != null && tool.RemoveFence;
            _removeButton.state = removing ? UIButton.ButtonState.Focused : UIButton.ButtonState.Normal;

            for (int r = 0; r < RowsPerPage; r++)
            {
                int index = _page * RowsPerPage + r;
                UIButton row = _rows[r];
                UISprite icon = _rowIcons[r];

                if (index >= list.Count)
                {
                    row.isVisible = false;
                    continue;
                }

                FenceEntry entry = list[index];
                row.isVisible = true;

                string title = entry.Title;
                if (title.Length > 26) title = title.Substring(0, 25) + "…";
                row.text = title;
                row.tooltip = entry.Title;

                bool selected = tool != null && !removing && tool.SelectedFence == entry.Info;
                row.state = selected ? UIButton.ButtonState.Focused : UIButton.ButtonState.Normal;

                if (entry.Info.m_Atlas != null && !string.IsNullOrEmpty(entry.Info.m_Thumbnail))
                {
                    icon.atlas = entry.Info.m_Atlas;
                    icon.spriteName = entry.Info.m_Thumbnail;
                    icon.isVisible = true;
                }
                else
                {
                    icon.isVisible = false;
                }
            }

            _prev.isEnabled = _page > 0;
            _next.isEnabled = _page < pageCount - 1;
            _pageLabel.text = list.Count == 0 ? "No network models" : (_page + 1) + " / " + pageCount;
        }

        public void SetStatus(string text)
        {
            if (_status != null && _status.text != text) _status.text = text;
        }
    }
}
