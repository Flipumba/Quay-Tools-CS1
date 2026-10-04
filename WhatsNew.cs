using System;
using System.Collections.Generic;
using ColossalFramework.UI;
using UnityEngine;

namespace QuayTools
{
    /// <summary>
    /// "What's new in Quay Tools?" window. Shown once after a new version of the mod is run for the first time
    /// (when a map is loaded). The last version the player has seen is stored in the mod settings, so the
    /// window does not appear again on other maps. A first installation shows nothing.
    /// </summary>
    internal static class WhatsNew
    {
        /// <summary>One version: short lines in English and Russian. Keep every line general ("Props-line: new features").</summary>
        private class Entry
        {
            public Version Version;
            public string[] En;
            public string[] Ru;
        }

        private static readonly Entry[] Entries =
        {
            new Entry
            {
                Version = new Version(0, 5, 6),
                En = new[]
                {
                    "New option: interface language (Auto, English, Русский).",
                    "Texture-path: new Colour multiply value.",
                    "Texture-path: colour without a texture removed for decal paths.",
                    "Network-line: Line start / Line end renamed and moved after Offset Z."
                },
                Ru = new[]
                {
                    "Новая опция: язык интерфейса (Авто, English, Русский).",
                    "Texture-path: новое значение «Умножение цвета».",
                    "Texture-path: для декалей убран выбор «цвет без текстуры».",
                    "Network-line: «Начало линии» / «Конец линии» переименованы и перенесены после «Смещения по Z»."
                }
            },
            new Entry
            {
                Version = new Version(0, 5, 7),
                En = new[]
                {
                    "New \"What's new\" window.",
                    "Texture-path: colour multiply fixed.",
                    "Props-line: lights of lamps, Follow the slope, LOD turns with the prop.",
                    "Invert segment: line start, end and offsets now follow the water side, props and tiles continue across flipped segments.",
                    "Interface updated: empty columns are collapsed, tool descriptions are tooltips, undo / redo block at the bottom, floating icons no longer cover the window."
                },
                Ru = new[]
                {
                    "Новое окно «Что нового».",
                    "Texture-path: исправлено умножение цвета.",
                    "Props-line: свет фонарей, следование уклону, LOD поворачивается вместе с пропом.",
                    "Invert segment: начало, конец и смещения линий привязаны к стороне воды, пропсы и тайлы продолжаются через перевёрнутые сегменты.",
                    "Обновлён интерфейс: пустые колонки свёрнуты, описания инструментов стали подсказками, блок отмены внизу, плавающие значки больше не перекрывают окно."
                }
            }
        };

        private static UIPanel _blocker;

        /// <summary>The version of the running mod (major.minor.build).</summary>
        private static Version Current
        {
            get
            {
                Version v = typeof(WhatsNew).Assembly.GetName().Version;
                return new Version(v.Major, v.Minor, Math.Max(0, v.Build));
            }
        }

        private static bool TryParse(string s, out Version v)
        {
            v = null;
            if (string.IsNullOrEmpty(s)) return false;
            try
            {
                v = new Version(s);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static void ShowIfNeeded()
        {
            try
            {
                Version current = Current;
                Version seen;
                if (!TryParse(Settings.LastSeenVersion, out seen))
                {
                    // first installation (or an unreadable value): remember the version and show nothing
                    Settings.LastSeenVersion = current.ToString();
                    return;
                }
                if (seen >= current) return;

                List<Entry> news = new List<Entry>();
                for (int i = 0; i < Entries.Length; i++)
                {
                    if (Entries[i].Version > seen && Entries[i].Version <= current) news.Add(Entries[i]);
                }
                if (news.Count == 0)
                {
                    // a version without news: no window
                    Settings.LastSeenVersion = current.ToString();
                    return;
                }
                news.Sort(delegate (Entry a, Entry b) { return b.Version.CompareTo(a.Version); });
                Begin(news, current);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[QuayTools] What's new window failed: " + ex.Message);
            }
        }

        /// <summary>Shows all entries (newest first) regardless of the stored version (button in the mod options).</summary>
        public static void ShowLatest()
        {
            try
            {
                List<Entry> news = new List<Entry>();
                for (int i = 0; i < Entries.Length; i++) news.Add(Entries[i]);
                news.Sort(delegate (Entry a, Entry b) { return b.Version.CompareTo(a.Version); });
                Begin(news, Current);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[QuayTools] What's new window failed: " + ex.Message);
            }
        }

        private static GameObject _host;
        private static Texture2D _blurTex;

        public static void Close()
        {
            if (_blocker != null)
            {
                UnityEngine.Object.Destroy(_blocker.gameObject);
                _blocker = null;
            }
            if (_host != null)
            {
                UnityEngine.Object.Destroy(_host);
                _host = null;
            }
            if (_blurTex != null)
            {
                UnityEngine.Object.Destroy(_blurTex);
                _blurTex = null;
            }
        }

        /// <summary>Takes a picture of the screen at the end of the frame (before the window exists), then builds the window.</summary>
        private static void Begin(List<Entry> news, Version current)
        {
            Close();
            _host = new GameObject("QuayToolsWhatsNew");
            _host.AddComponent<WhatsNewHost>().Run(news, current);
        }

        private class WhatsNewHost : MonoBehaviour
        {
            public void Run(List<Entry> news, Version current)
            {
                StartCoroutine(Routine(news, current));
            }

            private System.Collections.IEnumerator Routine(List<Entry> news, Version current)
            {
                yield return new WaitForEndOfFrame();
                Texture2D blur = null;
                try
                {
                    blur = CaptureBlurred();
                }
                catch (Exception ex)
                {
                    Debug.LogWarning("[QuayTools] Blurred background failed: " + ex.Message);
                }
                try
                {
                    _blurTex = blur;
                    Build(news, current, blur);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning("[QuayTools] What's new window failed: " + ex.Message);
                }
            }
        }

        /// <summary>The screen shrunk to a small picture and blurred; drawn stretched (bilinear) behind the window. Null if it cannot be read.</summary>
        private static Texture2D CaptureBlurred()
        {
            int w = Screen.width, h = Screen.height;
            if (w < 64 || h < 64) return null;

            Texture2D full = new Texture2D(w, h, TextureFormat.RGB24, false);
            full.ReadPixels(new Rect(0, 0, w, h), 0, 0, false);
            Color32[] src = full.GetPixels32();
            UnityEngine.Object.Destroy(full);

            const int f = 12;
            int sw = Mathf.Max(8, w / f), sh = Mathf.Max(8, h / f);
            float[] r = new float[sw * sh], g = new float[sw * sh], b = new float[sw * sh];
            int[] n = new int[sw * sh];
            for (int y = 0; y < sh * f && y < h; y += 2)
            {
                int row = (y / f) * sw;
                int srow = y * w;
                for (int x = 0; x < sw * f && x < w; x += 2)
                {
                    int i = row + x / f;
                    Color32 c = src[srow + x];
                    r[i] += c.r; g[i] += c.g; b[i] += c.b; n[i]++;
                }
            }
            for (int i = 0; i < n.Length; i++)
            {
                float k = n[i] > 0 ? 1f / n[i] : 0f;
                r[i] *= k; g[i] *= k; b[i] *= k;
            }
            for (int pass = 0; pass < 3; pass++)
            {
                BoxBlur(r, sw, sh); BoxBlur(g, sw, sh); BoxBlur(b, sw, sh);
            }

            Color32[] px = new Color32[sw * sh];
            const float dark = 0.62f;   // the blurred picture is also darkened a little so that the window stands out
            for (int i = 0; i < px.Length; i++)
            {
                px[i] = new Color32(
                    (byte)Mathf.Clamp(r[i] * dark, 0f, 255f),
                    (byte)Mathf.Clamp(g[i] * dark, 0f, 255f),
                    (byte)Mathf.Clamp(b[i] * dark, 0f, 255f), 255);
            }
            Texture2D tex = new Texture2D(sw, sh, TextureFormat.RGB24, false);
            tex.SetPixels32(px);
            tex.Apply(false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            return tex;
        }

        /// <summary>One box blur pass (radius 2) along both axes.</summary>
        private static void BoxBlur(float[] a, int w, int h)
        {
            float[] t = new float[a.Length];
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float sum = 0f; int c = 0;
                    for (int d = -2; d <= 2; d++)
                    {
                        int xx = x + d;
                        if (xx < 0 || xx >= w) continue;
                        sum += a[y * w + xx]; c++;
                    }
                    t[y * w + x] = sum / c;
                }
            }
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float sum = 0f; int c = 0;
                    for (int d = -2; d <= 2; d++)
                    {
                        int yy = y + d;
                        if (yy < 0 || yy >= h) continue;
                        sum += t[yy * w + x]; c++;
                    }
                    a[y * w + x] = sum / c;
                }
            }
        }

        private static void Build(List<Entry> news, Version current, Texture2D blur)
        {
            UIView view = UIView.GetAView();
            if (view == null) return;
            if (_blocker != null)
            {
                UnityEngine.Object.Destroy(_blocker.gameObject);
                _blocker = null;
            }

            bool ru = Loc.IsRussian;
            Vector2 res = view.GetScreenResolution();

            // full-screen dim layer: it also blocks clicks on the game below
            _blocker = view.AddUIComponent(typeof(UIPanel)) as UIPanel;
            _blocker.size = res;
            _blocker.absolutePosition = Vector3.zero;
            if (blur == null)
            {
                // the screen could not be read: plain dimming
                _blocker.backgroundSprite = "GenericPanel";
                _blocker.color = new Color32(0, 0, 0, 170);
            }
            _blocker.isInteractive = true;
            _blocker.canFocus = true;
            _blocker.BringToFront();
            if (blur != null)
            {
                UITextureSprite back = _blocker.AddUIComponent<UITextureSprite>();
                back.size = res;
                back.relativePosition = Vector3.zero;
                back.texture = blur;
                back.isInteractive = false;
            }

            const float width = 540f;
            const float pad = 24f;
            float textWidth = width - 2f * pad;

            // text of the window: one block per version (a version header only when several versions are shown)
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            int lines = 0;
            for (int i = 0; i < news.Count; i++)
            {
                if (news.Count > 1)
                {
                    if (i > 0) { sb.Append('\n'); lines++; }
                    sb.Append("v").Append(news[i].Version.ToString()).Append('\n');
                    lines++;
                }
                string[] items = ru ? news[i].Ru : news[i].En;
                for (int k = 0; k < items.Length; k++)
                {
                    sb.Append("•  ").Append(items[k]);
                    if (k < items.Length - 1 || i < news.Count - 1) sb.Append('\n');
                    lines += 1 + items[k].Length / 62;   // wrapped lines, estimated
                }
            }

            float bodyHeight = lines * 26f + 8f;
            float height = 56f + 34f + bodyHeight + 70f;
            height = Mathf.Min(height, res.y - 80f);

            UIPanel box = _blocker.AddUIComponent<UIPanel>();
            box.backgroundSprite = "GenericPanel";               // solid border
            box.color = new Color32(96, 116, 124, 255);
            box.size = new Vector2(width, height);
            box.relativePosition = new Vector3((res.x - width) * 0.5f, (res.y - height) * 0.5f);
            box.isInteractive = true;

            UIPanel fill = box.AddUIComponent<UIPanel>();         // solid dark fill
            fill.backgroundSprite = "GenericPanel";
            fill.color = new Color32(30, 40, 46, 255);
            fill.size = new Vector2(width - 4f, height - 4f);
            fill.relativePosition = new Vector3(2f, 2f);
            fill.isInteractive = false;

            UILabel title = box.AddUIComponent<UILabel>();
            title.text = Loc.T("wn_title");
            title.textScale = 1.25f;
            title.relativePosition = new Vector3(pad, 18f);
            title.isInteractive = false;

            UILabel ver = box.AddUIComponent<UILabel>();
            ver.text = Loc.F("wn_version", current.ToString());
            ver.textScale = 0.9f;
            ver.textColor = new Color32(250, 200, 40, 255);
            ver.relativePosition = new Vector3(pad, 54f);
            ver.isInteractive = false;

            UILabel body = box.AddUIComponent<UILabel>();
            body.autoSize = false;
            body.wordWrap = true;
            body.width = textWidth;
            body.height = bodyHeight;
            body.textScale = 0.95f;
            body.text = sb.ToString();
            body.relativePosition = new Vector3(pad, 88f);
            body.isInteractive = false;

            UIButton ok = box.AddUIComponent<UIButton>();
            ok.size = new Vector2(120f, 38f);
            ok.relativePosition = new Vector3((width - 120f) * 0.5f, height - 56f);
            ok.normalBgSprite = "ButtonMenu";
            ok.hoveredBgSprite = "ButtonMenuHovered";
            ok.pressedBgSprite = "ButtonMenuPressed";
            ok.focusedBgSprite = "ButtonMenuFocused";
            ok.text = Loc.T("wn_ok");
            ok.textScale = 1f;
            string versionText = current.ToString();
            ok.eventClicked += delegate (UIComponent c, UIMouseEventParameter p)
            {
                Settings.LastSeenVersion = versionText;
                Close();
            };
        }
    }
}
