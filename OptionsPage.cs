using System;
using System.IO;
using System.Collections.Generic;
using ColossalFramework;
using ColossalFramework.UI;
using ICities;
using UnityEngine;

namespace QuayTools
{
    /// <summary>
    /// The mod page in the game's options, drawn with our own flat controls: a header, "What's new", language,
    /// hotkeys, settings and support blocks.
    /// </summary>
    internal static class OptionsPage
    {
        private const string CrowdinUrl = "https://ru.crowdin.com/project/quay-tools-for-cities-skylines";
        private const string GithubUrl = "https://github.com/Flipumba/Quay-Tools-CS1";
        private const string BoostyUrl = "https://boosty.to/flipdraw/donate";

        private static readonly Color32 Page = new Color32(30, 42, 46, 255);
        private static readonly Color32 Box = new Color32(40, 55, 60, 255);
        private static readonly Color32 Row = new Color32(60, 80, 86, 255);
        private static readonly Color32 Field = new Color32(24, 34, 37, 255);
        private static readonly Color32 Accent = new Color32(250, 200, 40, 255);
        private static readonly Color32 Text = new Color32(232, 238, 238, 255);
        private static readonly Color32 Green = new Color32(60, 255, 90, 255);

        private const float Pad = 14f;
        private const float Gap = 12f;
        private const float RowH = 30f;

        private static UIComponent _holder;
        private static UIPanel _root;
        private static float _width;
        private static float _minHeight;
        private static UIPanel _page;
        private static UIPanel[] _pages;
        private static float[] _pageHeights;
        private static UIButton[] _tabs;
        private static int _tab;
        private static UIPanel _dlg;
        private const float TabsH = 36f;
        private const string TestedGameVersion = "1.21.1-f9";
        private const string SteamUrl = "https://steamcommunity.com/sharedfiles/filedetails/?id=3810565217";

        // Workshop IDs of mods known to conflict with Quay Tools (none known yet)
        private static readonly ulong[] IncompatibleIds = new ulong[0];

        public static void Build(UIHelperBase helper)
        {
            try
            {
                UIHelperBase group = helper.AddGroup(" ");
                UIHelper h = group as UIHelper;
                UIPanel content = h != null ? h.self as UIPanel : null;
                if (content == null) throw new Exception("no content panel");
                UIPanel groupPanel = content.parent as UIPanel;

                UILabel title = groupPanel != null ? groupPanel.Find<UILabel>("Label") : null;
                if (title != null) title.isVisible = false;

                _width = groupPanel != null && groupPanel.width > 300f ? groupPanel.width - 8f : 680f;
                _minHeight = 0f;
                if (groupPanel != null && groupPanel.parent != null) _minHeight = groupPanel.parent.height - 40f;

                content.autoLayout = false;
                content.relativePosition = Vector3.zero;
                _holder = content;
                Rebuild();
                if (content.gameObject.GetComponent<HeightKeeper>() == null) content.gameObject.AddComponent<HeightKeeper>();
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[QuayTools] Options page failed: " + ex);
            }
        }

        private static void Rebuild()
        {
            _dlg = null;
            if (_root != null) UnityEngine.Object.Destroy(_root.gameObject);
            _root = _holder.AddUIComponent<UIPanel>();
            _root.atlas = Flat.Atlas;
            _root.backgroundSprite = Flat.Round;
            _root.color = Page;
            _root.relativePosition = Vector3.zero;

            // tabs on top
            string[] tabNames = { Loc.T("o_tab_main"), Loc.T("o_tab_more"), Loc.T("o_tab_links") };
            _tabs = new UIButton[3];
            float tw = (_width - 2f * Pad - 2f * 8f) / 3f;
            for (int i = 0; i < 3; i++)
            {
                int index = i;
                _tabs[i] = Button(_root, Pad + i * (tw + 8f), Pad, tw, TabsH - 4f, tabNames[i], delegate () { SetTab(index); });
                _tabs[i].textScale = 0.95f;
            }

            _pages = new UIPanel[3];
            _pageHeights = new float[3];
            UIPanel langBlock = null;
            for (int i = 0; i < 3; i++)
            {
                _page = _root.AddUIComponent<UIPanel>();
                _page.relativePosition = new Vector3(0f, Pad + TabsH + 4f);
                _page.width = _width;
                _pages[i] = _page;
                float y = 0f;
                if (i == 0)
                {
                    y = Header(y);
                    y = WhatsNewBlock(y);
                    y = LanguageBlock(y, out langBlock);
                    y = HotkeyBlock(y);
                    y = SettingsBlock(y);
                }
                else if (i == 1)
                {
                    y = AdvancedBlock(y);
                }
                else
                {
                    y = LinksBlock(y);
                }
                _pageHeights[i] = y;
                _page.height = y;
            }
            if (langBlock != null) langBlock.BringToFront(); // its list opens over the blocks below
            SetTab(_tab);
        }

        /// <summary>The game's layout of the group may shrink its panels back; clicks outside of a parent are lost, so the heights are kept.</summary>
        private sealed class HeightKeeper : MonoBehaviour
        {
            private bool _done;

            private void LateUpdate()
            {
                if (_holder == null || _root == null) return;
                float target = _root.height;
                float extra = 8f;
                UIComponent up = _holder.parent;
                if (!_done)
                {
                    // the game's layout of the group would shrink it again and again: switch it off once
                    _done = true;
                    UIPanel gp = up as UIPanel;
                    if (gp != null) gp.autoLayout = false;
                }
                if (_holder.height < target) _holder.height = target;
                for (int i = 0; i < 2 && up != null && !(up is UIScrollablePanel); i++, up = up.parent)
                {
                    if (up.height < target + extra) up.height = target + extra;
                }
            }
        }

        private static void SetTab(int index)
        {
            _tab = index;
            CloseDialog();
            for (int i = 0; i < 3; i++)
            {
                _pages[i].isVisible = i == index;
                UIButton t = _tabs[i];
                bool on = i == index;
                Color32 bg = on ? Accent : Row;
                Color32 fg = on ? new Color32(24, 34, 36, 255) : Text;
                t.color = bg;
                t.hoveredColor = bg;
                t.focusedColor = bg;
                t.textColor = fg;
                t.hoveredTextColor = fg;
                t.focusedTextColor = fg;
            }
            float height = Mathf.Max(Pad + TabsH + 4f + _pageHeights[index] + Pad - Gap, _minHeight);
            _root.size = new Vector2(_width, height);
            _holder.size = new Vector2(_width, height);
            if (_holder.parent != null) _holder.parent.height = height + 8f;
        }

        // ---------- blocks ----------

        private static UIPanel NewBlock(float y, string headingKey)
        {
            UIPanel b = _page.AddUIComponent<UIPanel>();
            b.atlas = Flat.Atlas;
            b.backgroundSprite = Flat.Round;
            b.color = Box;
            b.relativePosition = new Vector3(Pad, y);
            b.width = _width - 2f * Pad;
            if (headingKey != null)
            {
                UILabel l = b.AddUIComponent<UILabel>();
                l.text = Loc.T(headingKey);
                l.textScale = 1.1f;
                l.textColor = Text;
                l.relativePosition = new Vector3(Pad, 10f);
                l.isInteractive = false;
            }
            return b;
        }

        private static float Header(float y)
        {
            UIPanel b = NewBlock(y, null);
            b.height = 124f;
            b.clipChildren = true;

            Texture2D bg = ModPaths.LoadIcon("OptionsHeader.png");
            if (bg != null)
            {
                UITextureSprite pic = b.AddUIComponent<UITextureSprite>();
                pic.texture = bg;
                pic.size = b.size;
                pic.relativePosition = Vector3.zero;
                pic.isInteractive = false;
            }

            Texture2D icon = ModPaths.LoadIcon("QuayTools.png");
            if (icon != null)
            {
                UITextureSprite ic = b.AddUIComponent<UITextureSprite>();
                ic.texture = icon;
                ic.size = new Vector2(84f, 84f);
                ic.relativePosition = new Vector3(24f, 20f);
                ic.isInteractive = false;
            }

            UILabel name = b.AddUIComponent<UILabel>();
            name.text = "QUAY TOOLS";
            name.textScale = 2.6f;
            name.textColor = new Color32(255, 255, 255, 255);
            name.relativePosition = new Vector3(128f, 26f);
            name.isInteractive = false;

            Version v = typeof(OptionsPage).Assembly.GetName().Version;
            UILabel ver = b.AddUIComponent<UILabel>();
            ver.text = "v" + v.Major + "." + v.Minor + "." + v.Build;
            ver.textScale = 1.0f;
            ver.textColor = Accent;
            ver.relativePosition = new Vector3(131f, 82f);
            ver.isInteractive = false;
            return y + b.height + Gap;
        }

        private static float WhatsNewBlock(float y)
        {
            UIPanel b = NewBlock(y, "o_new");
            float iw = b.width - 2f * Pad;
            float cy = 42f;

            UIButton show = Button(b, Pad, cy, 250f, 32f, Loc.T("opt_whatsnew"), delegate () { WhatsNew.ShowLatest(); });
            cy += 32f + 10f;

            Switch(b, Pad, cy, iw, Loc.T("o_show_wn"), Loc.T("o_show_wn_tip"),
                delegate () { return Settings.ShowWhatsNewWindow; },
                delegate (bool v) { Settings.ShowWhatsNewWindow = v; });
            cy += RowH + 12f;

            b.height = cy;
            return y + b.height + Gap;
        }

        private static float LanguageBlock(float y, out UIPanel block)
        {
            UIPanel b = NewBlock(y, "o_lang");
            block = b;
            float iw = b.width - 2f * Pad;
            float cy = 42f;

            List<string> codes = new List<string>();
            codes.Add("auto");
            codes.AddRange(Loc.AvailableLanguages);
            string[] names = new string[codes.Count];
            for (int i = 0; i < codes.Count; i++) names[i] = codes[i] == "auto" ? Loc.T("opt_lang_auto") : Loc.LanguageName(codes[i]);
            int shown = Mathf.Max(0, codes.IndexOf(Settings.UiLanguageCode));
            UIButton header = Button(b, Pad, cy, 250f, 32f, names[shown], null);
            header.textHorizontalAlignment = UIHorizontalAlignment.Left;
            header.textPadding = new RectOffset(10, 0, 0, 0);

            UILabel arrow = header.AddUIComponent<UILabel>();
            arrow.text = "v";
            arrow.textScale = 0.8f;
            arrow.textColor = Accent;
            arrow.relativePosition = new Vector3(250f - 22f, 9f);
            arrow.isInteractive = false;

            UIPanel list = b.AddUIComponent<UIPanel>();
            list.atlas = Flat.Atlas;
            list.backgroundSprite = Flat.Round;
            list.color = Field;
            list.size = new Vector2(250f, names.Length * 32f + 8f);
            list.relativePosition = new Vector3(Pad, cy + 34f);
            list.isVisible = false;
            for (int i = 0; i < names.Length; i++)
            {
                int index = i;
                UIButton item = Button(list, 4f, 4f + i * 32f, 242f, 30f, names[i], delegate ()
                {
                    list.isVisible = false;
                    if (Settings.UiLanguageCode == codes[index]) return;
                    Settings.UiLanguageCode = codes[index];
                    Rebuild(); // the page is drawn again in the chosen language
                });
                item.textHorizontalAlignment = UIHorizontalAlignment.Left;
                item.textPadding = new RectOffset(10, 0, 0, 0);
            }
            header.eventClicked += delegate (UIComponent c, UIMouseEventParameter p)
            {
                list.isVisible = !list.isVisible;
                if (list.isVisible) list.BringToFront();
            };
            cy += 32f + 14f;

            // a link to the translation project
            UILabel cap = b.AddUIComponent<UILabel>();
            cap.text = Loc.T("o_translate");
            cap.textScale = 0.85f;
            cap.textColor = Text;
            cap.relativePosition = new Vector3(Pad, cy);
            cap.isInteractive = false;
            cy += 24f;
            Banner(b, Pad, cy, "Banner_crowdin.png", CrowdinUrl);
            cy += 62f + 14f;

            b.height = cy;
            return y + b.height + Gap;
        }

        private static float HotkeyBlock(float y)
        {
            UIPanel b = NewBlock(y, "o_keys");
            float iw = b.width - 2f * Pad;
            float cy = 42f;

            KeyRow(b, cy, iw, Loc.T("o_key_activate"), Loc.T("o_key_activate_tip"), Settings.ActivationKey, true);
            cy += RowH + 8f;
            KeyRow(b, cy, iw, Loc.T("o_key_flip"), Loc.T("o_key_flip_tip"), Settings.FlipKey, false);
            cy += RowH + 12f;
            Button(b, Pad, cy, 250f, 32f, Loc.T("set_help"), delegate () { WhatsNew.ShowHelp(); }).tooltip = Loc.T("set_help_tip");
            cy += 32f + 12f;

            b.height = cy;
            return y + b.height + Gap;
        }

        private static float SettingsBlock(float y)
        {
            UIPanel b = NewBlock(y, "o_settings");
            float iw = b.width - 2f * Pad;
            float cy = 42f;

            Switch(b, Pad, cy, iw, Loc.T("set_quickflip"), Loc.T("opt_quickflip"),
                delegate () { return Settings.QuickFlipEnabled; }, delegate (bool v) { Settings.QuickFlipEnabled = v; });
            cy += RowH + 4f;
            Switch(b, Pad, cy, iw, Loc.T("o_s_undo"), Loc.T("opt_undokeys"),
                delegate () { return Settings.UndoHotkeysSetting; }, delegate (bool v) { Settings.UndoHotkeysSetting = v; });
            cy += RowH + 4f;
            Switch(b, Pad, cy, iw, Loc.T("set_shadows"), Loc.T("opt_shadows"),
                delegate () { return Settings.DecalReceiveShadows; }, delegate (bool v) { Settings.DecalReceiveShadows = v; });
            cy += RowH + 4f;
            Switch(b, Pad, cy, iw, Loc.T("set_mark"), Loc.T("opt_mark"),
                delegate () { return Settings.MarkEdited; }, delegate (bool v) { Settings.MarkEdited = v; });
            cy += RowH + 4f;
            Switch(b, Pad, cy, iw, Loc.T("o_s_hidetips"), Loc.T("o_s_hidetips_tip"),
                delegate () { return Settings.HideTips; }, delegate (bool v) { Settings.HideTips = v; });
            cy += RowH + 8f;

            // size of the floating icons
            UILabel label = b.AddUIComponent<UILabel>();
            label.text = Loc.T("set_icon");
            label.textScale = 0.8f;
            label.textColor = Text;
            label.tooltip = Loc.T("opt_iconsize");
            label.relativePosition = new Vector3(Pad + 10f, cy + 7f);
            label.isInteractive = true;
            UIButton[] sizes = new UIButton[3];
            float bw = 54f;
            for (int i = 0; i < 3; i++)
            {
                int size = i + 1;
                UIButton sb = Button(b, b.width - Pad - 3f * bw - 2f * 6f + i * (bw + 6f), cy, bw, RowH, size.ToString(), null);
                sb.tooltip = Loc.T("opt_iconsize");
                sizes[i] = sb;
            }
            Action sync = delegate ()
            {
                for (int i = 0; i < 3; i++) Flat.Keep(sizes[i], Settings.MarkIconSize == i + 1);
            };
            for (int i = 0; i < 3; i++)
            {
                int size = i + 1;
                sizes[i].eventClicked += delegate (UIComponent c, UIMouseEventParameter p)
                {
                    Settings.MarkIconSize = size;
                    sync();
                };
            }
            sync();
            cy += RowH + 8f;

            // experimental: last in the list, with a warning icon
            Switch(b, Pad, cy, iw, Loc.T("o_s_any"), Loc.T("opt_anynet"),
                delegate () { return Settings.AllowAnyNetworkSetting; },
                delegate (bool v)
                {
                    Settings.AllowAnyNetworkSetting = v;
                    if (v)
                    {
                        List<DlgRow> rows = new List<DlgRow>();
                        rows.Add(new DlgRow { Icon = "Warning.png", Title = Loc.T("o_exp_title"), Text = Loc.T("o_exp_text") });
                        ShowDialog(Loc.T("o_s_any"), rows, Loc.T("o_ok"), null, null, true);
                    }
                }, "Warning.png");
            cy += RowH + 12f;

            b.height = cy;
            return y + b.height + Gap;
        }

        private static float LinksBlock(float y)
        {
            UIPanel b = NewBlock(y, "o_links");
            float iw = b.width - 2f * Pad;
            float colW = (iw - 14f) * 0.5f;
            float cy = 42f;

            LinkCell(b, Pad, cy, colW, Loc.T("o_translate"), "Banner_crowdin.png", CrowdinUrl);
            LinkCell(b, Pad + colW + 14f, cy, colW, Loc.T("o_report"), "Banner_github.png", GithubUrl);
            cy += 24f + colW * 125f / 620f + 16f;
            LinkCell(b, Pad, cy, colW, Loc.T("o_donate"), "Banner_boosty.png", BoostyUrl);
            LinkCell(b, Pad + colW + 14f, cy, colW, Loc.T("o_steam"), "Banner_steam.png", SteamUrl);
            cy += 24f + colW * 125f / 620f + 16f;

            b.height = cy;
            return y + b.height + Gap;
        }

        private static void LinkCell(UIComponent parent, float x, float y, float w, string caption, string file, string url)
        {
            UILabel cap = parent.AddUIComponent<UILabel>();
            cap.text = caption;
            cap.textScale = 0.85f;
            cap.textColor = Text;
            cap.relativePosition = new Vector3(x, y);
            cap.isInteractive = false;
            Banner(parent, x, y + 24f, file, url, w);
        }

        // ---------- the "Advanced" tab ----------

        private static float AdvancedBlock(float y)
        {
            UIPanel b = NewBlock(y, null);
            float iw = b.width - 2f * Pad;
            float cy = 6f;

            cy = AdvRow(b, cy, iw, Loc.T("o_changelog"), null, Loc.T("o_show"), null, delegate (UIButton btn) { WhatsNew.ShowChangelog(); }, true);
            cy = AdvRow(b, cy, iw, Loc.T("o_compat"), Loc.T("o_compat_tip"), Loc.T("o_check"), null, delegate (UIButton btn) { ShowCompatibility(); }, true);
            cy = AdvRow(b, cy, iw, Loc.T("o_reset"), Loc.T("o_reset_tip"), Loc.T("o_reset_btn"), null, delegate (UIButton btn) { AskReset(); }, true);
            cy = AdvRow(b, cy, iw, Loc.T("o_logs"), null, Loc.T("o_copy"), null, delegate (UIButton btn) { CopyLogs(btn); }, false);

            b.height = cy + 6f;
            return y + b.height + Gap;
        }

        /// <summary>A row of the advanced tab: title (and a grey note) on the left, a button on the right; returns the next y.</summary>
        private static float AdvRow(UIPanel parent, float y, float w, string title, string note, string button, string tip, Action<UIButton> onClick, bool line)
        {
            float h = note == null ? 46f : 66f;
            UILabel t = parent.AddUIComponent<UILabel>();
            t.text = title;
            t.textScale = 0.95f;
            t.textColor = Text;
            t.relativePosition = new Vector3(Pad, y + (note == null ? 14f : 8f));
            t.isInteractive = false;
            if (note != null)
            {
                UILabel n = parent.AddUIComponent<UILabel>();
                n.autoSize = false;
                n.wordWrap = true;
                n.width = w - 200f;
                n.height = 32f;
                n.text = note;
                n.textScale = 0.72f;
                n.textColor = new Color32(176, 190, 192, 255);
                n.relativePosition = new Vector3(Pad, y + 32f);
                n.isInteractive = false;
            }
            UIButton btn = null;
            btn = Button(parent, Pad + w - 160f, y + (h - 32f) * 0.5f, 160f, 32f, button, delegate () { onClick(btn); });
            if (line)
            {
                UIPanel sep = parent.AddUIComponent<UIPanel>();
                sep.atlas = Flat.Atlas;
                sep.backgroundSprite = Flat.Solid;
                sep.color = Row;
                sep.size = new Vector2(w, 1f);
                sep.relativePosition = new Vector3(Pad, y + h);
                sep.isInteractive = false;
            }
            return y + h + 1f;
        }

        private static void CopyLogs(UIButton btn)
        {
            string result = Loc.T("o_copy_fail");
            try
            {
                string src = Path.Combine(Application.dataPath, "output_log.txt");
                string desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                string dst = Path.Combine(desktop, "QuayTools_output_log.txt");
                if (File.Exists(src))
                {
                    using (FileStream input = new FileStream(src, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    using (FileStream output = new FileStream(dst, FileMode.Create, FileAccess.Write))
                    {
                        byte[] buffer = new byte[81920];
                        int n;
                        while ((n = input.Read(buffer, 0, buffer.Length)) > 0) output.Write(buffer, 0, n);
                    }
                    result = Loc.T("o_copied");
                    btn.tooltip = dst;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[QuayTools] Copying the log failed: " + ex.Message);
            }
            btn.text = result;
        }

        // ---------- dialogs ----------

        private sealed class DlgRow
        {
            public string Icon;
            public string Title;
            public string Text;
        }

        private static void CloseDialog()
        {
            if (_dlg != null) UnityEngine.Object.Destroy(_dlg.gameObject);
            _dlg = null;
        }

        private static void ShowDialog(string title, List<DlgRow> rows, string ok, Action onOk, string cancel, bool atBottom = false)
        {
            CloseDialog();
            _dlg = _root.AddUIComponent<UIPanel>();
            _dlg.size = _root.size; // no fill (a translucent parent would make the box translucent); it only catches the clicks
            _dlg.relativePosition = Vector3.zero;
            _dlg.isInteractive = true;
            _dlg.BringToFront();

            float w = Mathf.Min(480f, _width - 40f);
            UIPanel box = _dlg.AddUIComponent<UIPanel>();
            box.atlas = Flat.Atlas;
            box.backgroundSprite = Flat.Round;
            box.color = new Color32(36, 50, 54, 255);
            box.isInteractive = true;
            box.opacity = 1f;
            _dlg.opacity = 1f;

            UILabel t = box.AddUIComponent<UILabel>();
            t.text = title;
            t.textScale = 1.05f;
            t.textColor = new Color32(255, 255, 255, 255);
            t.relativePosition = new Vector3(Pad + 4f, 16f);
            t.isInteractive = false;

            float y = 52f;
            int charsPerLine = Mathf.Max(20, (int)((w - 2f * Pad - 70f) / 7.4f));
            for (int i = 0; i < rows.Count; i++)
            {
                DlgRow r = rows[i];
                int lines = Mathf.Max(1, Mathf.CeilToInt((float)r.Text.Length / charsPerLine));
                float h = (string.IsNullOrEmpty(r.Title) ? 16f : 40f) + lines * 17f + 4f;
                UIPanel card = box.AddUIComponent<UIPanel>();
                card.atlas = Flat.Atlas;
                card.backgroundSprite = Flat.Round;
                card.color = Box;
                card.size = new Vector2(w - 2f * Pad, h);
                card.relativePosition = new Vector3(Pad, y);
                card.isInteractive = false;

                float tx = Pad;
                if (r.Icon != null)
                {
                    Texture2D ic = ModPaths.LoadIcon(r.Icon);
                    if (ic != null)
                    {
                        UITextureSprite sp = card.AddUIComponent<UITextureSprite>();
                        sp.texture = ic;
                        sp.size = new Vector2(28f, 28f);
                        sp.relativePosition = new Vector3(12f, 10f);
                        sp.isInteractive = false;
                    }
                    tx = 52f;
                }
                float ty = 10f;
                if (!string.IsNullOrEmpty(r.Title))
                {
                    UILabel rt = card.AddUIComponent<UILabel>();
                    rt.text = r.Title;
                    rt.textScale = 0.95f;
                    rt.textColor = Text;
                    rt.relativePosition = new Vector3(tx, ty);
                    rt.isInteractive = false;
                    ty += 24f;
                }
                UILabel rx = card.AddUIComponent<UILabel>();
                rx.autoSize = false;
                rx.wordWrap = true;
                rx.width = card.width - tx - 12f;
                rx.height = lines * 17f;
                rx.text = r.Text;
                rx.textScale = 0.78f;
                rx.textColor = new Color32(190, 202, 204, 255);
                rx.relativePosition = new Vector3(tx, ty);
                rx.isInteractive = false;
                y += h + 8f;
            }

            float bh = 34f;
            if (cancel != null)
            {
                float bw = (w - 2f * Pad - 8f) * 0.5f;
                UIButton yes = Button(box, Pad, y + 4f, bw, bh, ok, delegate () { CloseDialog(); if (onOk != null) onOk(); });
                yes.color = new Color32(176, 62, 62, 255);
                yes.hoveredColor = yes.color;
                yes.focusedColor = yes.color;
                Button(box, Pad + bw + 8f, y + 4f, bw, bh, cancel, delegate () { CloseDialog(); });
            }
            else
            {
                Button(box, Pad, y + 4f, w - 2f * Pad, bh, ok, delegate () { CloseDialog(); if (onOk != null) onOk(); });
            }
            y += bh + 4f + Pad;

            box.size = new Vector2(w, y);
            box.relativePosition = new Vector3((_root.width - w) * 0.5f, atBottom ? Mathf.Max(0f, _root.height - y - 24f) : Pad + TabsH + 40f);
        }

        private static void AskReset()
        {
            List<DlgRow> rows = new List<DlgRow>();
            rows.Add(new DlgRow { Icon = "Warn.png", Title = Loc.T("o_reset"), Text = Loc.T("o_reset_ask") });
            ShowDialog(Loc.T("o_reset"), rows, Loc.T("o_reset_btn"), delegate ()
            {
                Settings.ResetAll();
                Rebuild();
            }, Loc.T("tpl_cancel"));
        }

        private static void ShowCompatibility()
        {
            List<DlgRow> rows = new List<DlgRow>();

            bool harmony = false;
            try { harmony = CitiesHarmony.API.HarmonyHelper.IsHarmonyInstalled; } catch (Exception) { }
            rows.Add(new DlgRow { Icon = harmony ? "Check.png" : "Warn.png", Title = Loc.T("o_c_dep"), Text = Loc.T(harmony ? "o_c_dep_ok" : "o_c_dep_bad") });

            string game = BuildConfig.applicationVersion;
            bool same = game == TestedGameVersion;
            rows.Add(new DlgRow
            {
                Icon = same ? "Check.png" : "Warn.png",
                Title = Loc.T("o_c_ver"),
                Text = same ? Loc.F("o_c_ver_ok", game) : Loc.F("o_c_ver_bad", TestedGameVersion, game)
            });

            List<string> found = new List<string>();
            try
            {
                foreach (ColossalFramework.Plugins.PluginManager.PluginInfo info in ColossalFramework.Plugins.PluginManager.instance.GetPluginsInfo())
                {
                    if (!info.isEnabled) continue;
                    ulong id = info.publishedFileID.AsUInt64;
                    for (int i = 0; i < IncompatibleIds.Length; i++)
                    {
                        if (IncompatibleIds[i] == id) found.Add(info.name);
                    }
                }
            }
            catch (Exception) { }
            rows.Add(new DlgRow
            {
                Icon = found.Count == 0 ? "Check.png" : "Warn.png",
                Title = Loc.T("o_c_inc"),
                Text = found.Count == 0 ? Loc.T("o_c_inc_ok") : Loc.F("o_c_inc_bad", string.Join(", ", found.ToArray()))
            });

            ShowDialog(Loc.T("o_c_title"), rows, Loc.T("o_ok"), null, null);
        }

        // ---------- controls ----------

        private static UIButton Button(UIComponent parent, float x, float y, float w, float h, string text, Action onClick)
        {
            UIButton b = parent.AddUIComponent<UIButton>();
            b.size = new Vector2(w, h);
            b.relativePosition = new Vector3(x, y);
            QuayToolPanel.StyleButton(b);
            b.textScale = 0.85f;
            b.text = text;
            if (onClick != null) b.eventClicked += delegate (UIComponent c, UIMouseEventParameter p) { onClick(); };
            return b;
        }

        /// <summary>A row with a switch on the right (dark pill with a knob, green when on).</summary>
        private static void Switch(UIComponent parent, float x, float y, float w, string text, string tip, Func<bool> get, Action<bool> set, string icon = null)
        {
            UIButton b = Button(parent, x, y, w, RowH, text, null);
            b.textScale = 0.8f;
            b.textHorizontalAlignment = UIHorizontalAlignment.Left;
            b.textPadding = new RectOffset(10, 0, 0, 0);
            b.focusedColor = b.color;
            if (!string.IsNullOrEmpty(tip)) b.tooltip = Wrap(tip, 64);
            if (icon != null)
            {
                Texture2D tex = ModPaths.LoadIcon(icon);
                if (tex != null)
                {
                    b.textPadding = new RectOffset(40, 0, 0, 0);
                    UITextureSprite sp = b.AddUIComponent<UITextureSprite>();
                    sp.texture = tex;
                    sp.size = new Vector2(22f, 22f);
                    sp.relativePosition = new Vector3(10f, 4f);
                    sp.isInteractive = false;
                }
            }

            UIPanel pill = b.AddUIComponent<UIPanel>();
            pill.atlas = Flat.Atlas;
            pill.backgroundSprite = Flat.Round;
            pill.size = new Vector2(38f, 20f);
            pill.relativePosition = new Vector3(w - 38f - 10f, 5f);
            pill.isInteractive = false;
            UIPanel knob = pill.AddUIComponent<UIPanel>();
            knob.atlas = Flat.Atlas;
            knob.backgroundSprite = Flat.Round;
            knob.color = new Color32(255, 255, 255, 255);
            knob.size = new Vector2(14f, 14f);
            knob.isInteractive = false;

            Action sync = delegate ()
            {
                bool on = get();
                pill.color = on ? Green : Field;
                knob.relativePosition = new Vector3(on ? 38f - 14f - 3f : 3f, 3f);
            };
            b.eventClicked += delegate (UIComponent c, UIMouseEventParameter p)
            {
                set(!get());
                sync();
            };
            sync();
        }

        private static void KeyRow(UIComponent parent, float y, float w, string text, string tip, SavedInputKey key, bool allowShift)
        {
            UILabel l = parent.AddUIComponent<UILabel>();
            l.text = text;
            l.textScale = 0.85f;
            l.textColor = Text;
            l.relativePosition = new Vector3(Pad + 10f, y + 7f);
            l.tooltip = Wrap(tip, 64);
            l.isInteractive = true;

            UIButton b = Button(parent, Pad + w - 200f, y, 200f, RowH, KeyText(key), null);
            b.tooltip = Wrap(tip, 64);
            b.canFocus = true;
            bool capturing = false;
            b.eventClicked += delegate (UIComponent c, UIMouseEventParameter p)
            {
                capturing = true;
                b.text = Loc.T("o_press");
                b.Focus();
            };
            b.eventKeyDown += delegate (UIComponent c, UIKeyEventParameter p)
            {
                if (!capturing) return;
                p.Use();
                KeyCode k = p.keycode;
                if (k == KeyCode.Escape)
                {
                    capturing = false;
                    b.text = KeyText(key);
                    return;
                }
                if (IsModifier(k)) return;
                key.value = SavedInputKey.Encode(k, p.control, allowShift && p.shift, p.alt);
                capturing = false;
                b.text = KeyText(key);
                b.Unfocus();
            };
            b.eventLostFocus += delegate (UIComponent c, UIFocusEventParameter p)
            {
                if (!capturing) return;
                capturing = false;
                b.text = KeyText(key);
            };
        }

        private static bool IsModifier(KeyCode k)
        {
            return k == KeyCode.LeftControl || k == KeyCode.RightControl || k == KeyCode.LeftShift || k == KeyCode.RightShift ||
                   k == KeyCode.LeftAlt || k == KeyCode.RightAlt || k == KeyCode.LeftCommand || k == KeyCode.RightCommand;
        }

        private static string KeyText(SavedInputKey key)
        {
            string s = string.Empty;
            if (key.Control) s += "Ctrl + ";
            if (key.Shift) s += "Shift + ";
            if (key.Alt) s += "Alt + ";
            string name = key.Key.ToString();
            if (name.StartsWith("Alpha")) name = name.Substring(5);
            return s + name;
        }

        /// <summary>A picture button that opens a web page.</summary>
        private static void Banner(UIComponent parent, float x, float y, string file, string url, float w = 310f)
        {
            Texture2D tex = ModPaths.LoadIcon(file);
            UIButton b = parent.AddUIComponent<UIButton>();
            b.size = new Vector2(w, w * 125f / 620f);
            b.relativePosition = new Vector3(x, y);
            b.tooltip = url;
            if (tex == null)
            {
                QuayToolPanel.StyleButton(b);
                b.text = url;
                b.eventClicked += delegate (UIComponent c, UIMouseEventParameter p) { Application.OpenURL(url); };
                return;
            }
            UITextureSprite pic = b.AddUIComponent<UITextureSprite>();
            pic.texture = tex;
            pic.size = b.size;
            pic.relativePosition = Vector3.zero;
            pic.isInteractive = false;
            pic.opacity = 0.9f;
            b.eventMouseEnter += delegate (UIComponent c, UIMouseEventParameter p) { pic.opacity = 1f; };
            b.eventMouseLeave += delegate (UIComponent c, UIMouseEventParameter p) { pic.opacity = 0.9f; };
            b.eventClicked += delegate (UIComponent c, UIMouseEventParameter p) { Application.OpenURL(url); };
        }

        private static string Wrap(string text, int width)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            string[] words = text.Split(' ');
            int line = 0;
            for (int i = 0; i < words.Length; i++)
            {
                if (line > 0 && line + 1 + words[i].Length > width)
                {
                    sb.Append('\n');
                    line = 0;
                }
                else if (i > 0)
                {
                    sb.Append(' ');
                    line++;
                }
                sb.Append(words[i]);
                line += words[i].Length;
            }
            return sb.ToString();
        }
    }
}
