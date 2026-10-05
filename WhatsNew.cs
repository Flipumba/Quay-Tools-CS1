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
                Version = new Version(0, 3, 5),
                En = new[]
                {
                    "n|Decal step and Decal box height sliders for placed decals.",
                    "u|Node Controller Renewal nodes: gaps are bridged with the neighbour's real path end."
                },
                Ru = new[]
                {
                    "n|Новые ползунки для декалей: шаг и высота проекции.",
                    "u|Узлы Node Controller Renewal: промежутки соединяются по реальному концу пути соседа."
                }
            },
            new Entry
            {
                Version = new Version(0, 3, 6),
                En = new[]
                {
                    "u|Node Controller Renewal nodes: bridging of gaps rewritten.",
                    "u|Placed decals over a gap use a taller projection box."
                },
                Ru = new[]
                {
                    "u|Узлы Node Controller Renewal: соединение промежутков переписано.",
                    "u|Декали над промежутком используют более высокую область проекции."
                }
            },
            new Entry
            {
                Version = new Version(0, 4, 0),
                En = new[]
                {
                    "n|Edited segments are highlighted and marked with tool icons.",
                    "n|New tool Lock segment orientation.",
                    "n|Fences: detach, shift the start and end, scale the width.",
                    "n|New tool Props-line.",
                    "u|Decal paths at sharp bends of nodes follow one curve.",
                    "u|Tool buttons are a compact row of icons.",
                    "n|New tool Remove pedestrian path."
                },
                Ru = new[]
                {
                    "n|Изменённые сегменты подсвечиваются и помечаются значками инструментов.",
                    "n|Новый инструмент «Заблокировать разворот».",
                    "n|Заборы: отсоединение, сдвиг начала и конца, масштаб по ширине.",
                    "n|Новый инструмент Props-line.",
                    "u|Дорожки из декалей на резких изгибах узлов идут по одной кривой.",
                    "u|Кнопки инструментов стали компактным рядом значков.",
                    "n|Новый инструмент «Убрать пешеходную дорожку»."
                }
            },
            new Entry
            {
                Version = new Version(0, 4, 1),
                En = new[]
                {
                    "n|Drop-down lists show all items, with a search field and favourites.",
                    "n|Values can be typed in a field next to the slider.",
                    "u|Props-line follows the bends and height of nodes; trees can be placed.",
                    "n|New: Flip model for fence models.",
                    "f|Remove pedestrian path reworked: vehicles keep driving on the segment."
                },
                Ru = new[]
                {
                    "n|Выпадающие списки показывают все элементы, есть поиск и избранное.",
                    "n|Значения можно вводить числом рядом с ползунком.",
                    "u|Props-line повторяет изгибы и высоту узлов; можно ставить деревья.",
                    "n|Новое: «Развернуть модель» для моделей заборов.",
                    "f|Переделано «Убрать пешеходную дорожку»: транспорт продолжает ехать по сегменту."
                }
            },
            new Entry
            {
                Version = new Version(0, 5, 0),
                En = new[]
                {
                    "u|Network-line reworked: any number of lines per segment.",
                    "u|Texture-path reworked: any number of paths per segment.",
                    "u|Turn the model around is a real half turn.",
                    "f|Remove pedestrian path covers the method that builds walking routes.",
                    "u|All sliders with value fields share one layout."
                },
                Ru = new[]
                {
                    "u|Network-line переделан: любое количество линий на сегмент.",
                    "u|Texture-path переделан: любое количество дорожек на сегмент.",
                    "u|«Развернуть модель» теперь настоящий разворот на 180°.",
                    "f|«Убрать пешеходную дорожку» охватывает метод, который строит пешеходные маршруты.",
                    "u|У всех ползунков с полями значений единая раскладка."
                }
            },
            new Entry
            {
                Version = new Version(0, 5, 1),
                En = new[]
                {
                    "f|Network-lines were invisible with mods that patch segment rendering.",
                    "f|Remove pedestrian path did not stop pedestrians.",
                    "n|New option: size of the floating tool icons.",
                    "u|Texture-path strips look for a lit shader."
                },
                Ru = new[]
                {
                    "f|Network-линии были невидимы при модах, меняющих отрисовку сегментов.",
                    "f|«Убрать пешеходную дорожку» не останавливало пешеходов.",
                    "n|Новая опция: размер плавающих значков инструментов.",
                    "u|Полосы Texture-path ищут шейдер со светом."
                }
            },
            new Entry
            {
                Version = new Version(0, 5, 2),
                En = new[]
                {
                    "f|Network-lines disappeared after the first frame.",
                    "f|Remove pedestrian path had no effect with TM:PE.",
                    "u|Texture-path strips: default height 1 m, shadows on opaque strips."
                },
                Ru = new[]
                {
                    "f|Network-линии пропадали после первого кадра.",
                    "f|«Убрать пешеходную дорожку» не работало с TM:PE.",
                    "u|Полосы Texture-path: высота по умолчанию 1 м, тени на непрозрачных полосах."
                }
            },
            new Entry
            {
                Version = new Version(0, 5, 3),
                En = new[]
                {
                    "f|Network-lines follow the quay (bends, S-curves, heights).",
                    "u|Citizens already walking over a blocked segment are sent on their way again."
                },
                Ru = new[]
                {
                    "f|Network-линии повторяют набережную (изгибы, S-образные кривые, высоты).",
                    "u|Горожане, уже идущие по заблокированному сегменту, получают новый маршрут."
                }
            },
            new Entry
            {
                Version = new Version(0, 5, 4),
                En = new[]
                {
                    "f|Network-lines twisted at sharp corners of narrowed nodes.",
                    "f|Remove pedestrian path now also stops \"any means\" paths."
                },
                Ru = new[]
                {
                    "f|Network-линии скручивались на острых углах суженных узлов.",
                    "f|«Убрать пешеходную дорожку» теперь останавливает и смешанные маршруты."
                }
            },
            new Entry
            {
                Version = new Version(0, 5, 5),
                En = new[]
                {
                    "u|Lines across a node (Network-line, Props-line, Texture-path) rebuilt after the source of Node Controller Renewal.",
                    "n|New value Offset X for Props-line and Texture-path.",
                    "n|Texture-path: Line start / Line end; Add decal and Add plane buttons.",
                    "n|New tool Hide default props.",
                    "n|New switch Hide highlight while dragging.",
                    "u|New window layout in three columns, one fixed size for all tools.",
                    "f|Fixed kinks and breaks at the joints between a segment and a node."
                },
                Ru = new[]
                {
                    "u|Линии через узел (Network-line, Props-line, Texture-path) переделаны по исходникам Node Controller Renewal.",
                    "n|Новое значение «Смещение по X» для Props-line и Texture-path.",
                    "n|Texture-path: «Начало линии» / «Конец линии»; кнопки «Добавить декаль» и «Добавить плоскость».",
                    "n|Новый инструмент «Скрыть стандартные пропсы».",
                    "n|Новый переключатель «Скрыть подсветку при перетаскивании».",
                    "u|Новая раскладка окна в три колонки, один размер для всех инструментов.",
                    "f|Исправлены изломы и разрывы на стыках сегмента и узла."
                }
            },
            new Entry
            {
                Version = new Version(0, 5, 6),
                En = new[]
                {
                    "n|New option: interface language (Auto, English, Русский).",
                    "n|Texture-path: new Colour multiply value.",
                    "u|Texture-path: colour without a texture removed for decal paths.",
                    "u|Network-line: Line start / Line end renamed and moved after Offset Z."
                },
                Ru = new[]
                {
                    "n|Новая опция: язык интерфейса (Авто, English, Русский).",
                    "n|Texture-path: новое значение «Умножение цвета».",
                    "u|Texture-path: для декалей убран выбор «цвет без текстуры».",
                    "u|Network-line: «Начало линии» / «Конец линии» переименованы и перенесены после «Смещения по Z»."
                }
            },
            new Entry
            {
                Version = new Version(0, 5, 7),
                En = new[]
                {
                    "n|New \"What's new\" window.",
                    "f|Texture-path: colour multiply fixed.",
                    "u|Props-line: lights of lamps, Follow the slope, LOD turns with the prop.",
                    "u|Invert segment: line start, end and offsets follow the water side; props and tiles continue across flipped segments.",
                    "u|Interface updated: empty columns collapsed, tool descriptions are tooltips, undo / redo block at the bottom."
                },
                Ru = new[]
                {
                    "n|Новое окно «Что нового».",
                    "f|Texture-path: исправлено умножение цвета.",
                    "u|Props-line: свет фонарей, следование уклону, LOD поворачивается вместе с пропом.",
                    "u|Invert segment: начало, конец и смещения линий привязаны к стороне воды, пропсы и тайлы продолжаются через перевёрнутые сегменты.",
                    "u|Обновлён интерфейс: пустые колонки свёрнуты, описания инструментов стали подсказками, блок отмены внизу."
                }
            },
            new Entry
            {
                Version = new Version(0, 6, 0),
                En = new[]
                {
                    "n|New tool Segment Settings: invert, lock, remove pedestrian path and hide default props in one window.",
                    "n|New tool Templates: save the lines of a segment with a picture and apply them to other segments.",
                    "n|New tool Settings: options moved from the mod options, controls help and a button that clears all edited quays.",
                    "u|Check boxes are now switches.",
                    "u|Selection: click selects one segment, Shift the whole quay, Ctrl adds or removes segments.",
                    "u|The window has the same height for all tools.",
                    "u|New flat look of the window.",
                    "u|The mod options are redesigned: tabs, free hotkeys, all options in one place, a switch to hide tooltips, links."
                },
                Ru = new[]
                {
                    "n|Новый инструмент «Настройки сегмента»: разворот, блокировка, удаление пешеходной дорожки и скрытие стандартных пропсов в одном окне.",
                    "n|Новый инструмент «Шаблоны»: сохраняйте линии сегмента с картинкой и применяйте их к другим сегментам.",
                    "n|Новый инструмент «Настройки»: опции перенесены из настроек мода, помощь в управлении и кнопка очистки всех отредактированных набережных.",
                    "u|Флажки заменены переключателями.",
                    "u|Выделение: клик выделяет один сегмент, Shift - всю набережную, Ctrl добавляет или убирает сегменты.",
                    "u|У окна одинаковая высота для всех инструментов.",
                    "u|Новый плоский вид окна.",
                    "u|Страница настроек мода переделана: вкладки, свободные клавиши, все настройки в одном месте, переключатель скрытия подсказок, ссылки."
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
                if (!Settings.ShowWhatsNewWindow)
                {
                    Settings.LastSeenVersion = current.ToString();
                    return;
                }

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
                news.RemoveRange(1, news.Count - 1); // only the newest version
                _changelog = false;
                _helpWindow = false;
                _wantModal = false;
                Begin(news, current);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[QuayTools] What's new window failed: " + ex.Message);
            }
        }

        /// <summary>Shows the changes of the newest version (button in the mod options).</summary>
        public static void ShowLatest()
        {
            try
            {
                List<Entry> news = new List<Entry>();
                Entry best = null;
                for (int i = 0; i < Entries.Length; i++)
                {
                    if (best == null || Entries[i].Version > best.Version) best = Entries[i];
                }
                if (best != null) news.Add(best);
                _changelog = false;
                _helpWindow = false;
                _wantModal = true; // opened from the options: the options window is modal and would block our clicks
                Begin(news, Current);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[QuayTools] What's new window failed: " + ex.Message);
            }
        }

        /// <summary>The controls help in a window of its own.</summary>
        public static void ShowHelp()
        {
            try
            {
                _changelog = false;
                _helpWindow = true;
                _wantModal = true;
                Begin(new List<Entry>(), Current);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[QuayTools] Help window failed: " + ex.Message);
            }
        }

        /// <summary>The list of all versions (newest first) with folding cards.</summary>
        public static void ShowChangelog()
        {
            try
            {
                List<Entry> news = new List<Entry>(Entries);
                news.Sort(delegate (Entry a, Entry b) { return b.Version.CompareTo(a.Version); });
                _changelog = true;
                _helpWindow = false;
                _wantModal = true;
                Begin(news, Current);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[QuayTools] Changelog window failed: " + ex.Message);
            }
        }

        private static bool _changelog;
        private static bool _helpWindow;

        private static string Strip(string item)
        {
            return item.Length > 2 && item[1] == '|' ? item.Substring(2) : item;
        }

        private static char TagOf(string item)
        {
            return item.Length > 2 && item[1] == '|' ? item[0] : 'u';
        }

        private static bool _wantModal;
        private static bool _modal;
        private static GameObject _host;
        private static Texture2D _blurTex;

        public static void Close()
        {
            if (_modal)
            {
                _modal = false;
                try { UIView.PopModal(); } catch (Exception) { }
            }
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

        private static UIScrollablePanel CreateScroll(UIPanel box, float width, float height, float pad)
        {
            float top = 64f;
            float bottom = 70f;
            float viewH = height - top - bottom;
            float viewW = width - 2f * pad - 14f;

            UIScrollablePanel scroll = box.AddUIComponent<UIScrollablePanel>();
            scroll.relativePosition = new Vector3(pad, top);
            scroll.size = new Vector2(viewW, viewH);
            scroll.autoLayout = true;
            scroll.autoLayoutDirection = LayoutDirection.Vertical;
            scroll.autoLayoutPadding = new RectOffset(0, 0, 0, 8);
            scroll.clipChildren = true;
            scroll.scrollWheelDirection = UIOrientation.Vertical;
            scroll.builtinKeyNavigation = false;

            UIScrollbar bar = box.AddUIComponent<UIScrollbar>();
            bar.width = 10f;
            bar.height = viewH;
            bar.orientation = UIOrientation.Vertical;
            bar.pivot = UIPivotPoint.TopLeft;
            bar.relativePosition = new Vector3(width - pad - 10f, top);
            bar.minValue = 0f;
            bar.incrementAmount = 40f;
            UISlicedSprite track = bar.AddUIComponent<UISlicedSprite>();
            track.spriteName = "EmptySprite";
            track.color = new Color32(24, 34, 37, 255);
            track.relativePosition = Vector3.zero;
            track.size = new Vector2(10f, viewH);
            bar.trackObject = track;
            UISlicedSprite thumb = track.AddUIComponent<UISlicedSprite>();
            thumb.spriteName = "EmptySprite";
            thumb.color = new Color32(250, 200, 40, 255);
            thumb.width = 10f;
            bar.thumbObject = thumb;
            scroll.verticalScrollbar = bar;
            return scroll;
        }

        private static void BuildHelpWindow(UIPanel box, float width, float height, float pad)
        {
            UIScrollablePanel scroll = CreateScroll(box, width, height, pad);
            string[] lines = Settings.HelpText().Split('\n');
            bool heading = true;
            float w = scroll.width;
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (line.Length == 0)
                {
                    heading = true;
                    continue;
                }
                UILabel l = scroll.AddUIComponent<UILabel>();
                l.autoSize = false;
                l.autoHeight = true;
                l.wordWrap = true;
                l.isInteractive = false;
                l.width = w;
                l.text = line;
                if (heading)
                {
                    l.textScale = 1.0f;
                    l.textColor = new Color32(250, 200, 40, 255);
                    l.padding = new RectOffset(0, 0, 12, 2);
                    heading = false;
                }
                else
                {
                    l.textScale = 0.85f;
                    l.padding = new RectOffset(16, 0, 0, 3);
                    UILabel dot = l.AddUIComponent<UILabel>();
                    dot.text = "\u2022";
                    dot.textScale = 0.9f;
                    dot.textColor = new Color32(250, 200, 40, 255);
                    dot.isInteractive = false;
                    dot.relativePosition = new Vector3(4f, 0f);
                }
            }
        }

        private static string BadgeText(char tag)
        {
            return Loc.T(tag == 'n' ? "wn_tag_new" : tag == 'f' ? "wn_tag_fix" : "wn_tag_upd");
        }

        private static void BuildChangelog(UIPanel box, List<Entry> news, bool ru, float width, float height, float pad)
        {
            float top = 64f;
            float bottom = 70f;
            float viewH = height - top - bottom;
            float viewW = width - 2f * pad - 14f;
            UIScrollablePanel scroll = CreateScroll(box, width, height, pad);

            int perLine = Mathf.Max(20, (int)((viewW - 150f) / 6.6f));
            for (int i = 0; i < news.Count; i++)
            {
                string[] items = ru ? news[i].Ru : news[i].En;
                UIPanel card = scroll.AddUIComponent<UIPanel>();
                card.atlas = Flat.Atlas;
                card.backgroundSprite = Flat.Round;
                card.color = new Color32(46, 63, 68, 255);
                card.width = viewW;

                UIButton head = card.AddUIComponent<UIButton>();
                head.size = new Vector2(viewW, 38f);
                head.relativePosition = Vector3.zero;
                QuayToolPanel.StyleButton(head);
                head.text = "v" + news[i].Version.ToString();
                head.textHorizontalAlignment = UIHorizontalAlignment.Left;
                head.textPadding = new RectOffset(14, 0, 0, 0);
                head.textScale = 1f;

                UILabel sign = head.AddUIComponent<UILabel>();
                sign.textScale = 1.1f;
                sign.textColor = new Color32(250, 200, 40, 255);
                sign.relativePosition = new Vector3(viewW - 28f, 7f);
                sign.isInteractive = false;

                UIPanel body = card.AddUIComponent<UIPanel>();
                body.relativePosition = new Vector3(0f, 44f);
                body.width = viewW;
                body.isInteractive = false;

                float y = 0f;
                for (int k = 0; k < items.Length; k++)
                {
                    string text = Strip(items[k]);
                    char tag = TagOf(items[k]);
                    int lines = Mathf.Max(1, Mathf.CeilToInt((float)text.Length / perLine));

                    UIPanel badge = body.AddUIComponent<UIPanel>();
                    badge.atlas = Flat.Atlas;
                    badge.backgroundSprite = Flat.Round;
                    badge.color = tag == 'n' ? new Color32(46, 150, 84, 255) : tag == 'f' ? new Color32(212, 130, 36, 255) : new Color32(66, 86, 190, 255);
                    badge.size = new Vector2(104f, 20f);
                    badge.relativePosition = new Vector3(14f, y + 3f);
                    badge.isInteractive = false;
                    UILabel bl = badge.AddUIComponent<UILabel>();
                    bl.autoSize = false;
                    bl.width = 104f;
                    bl.height = 20f;
                    bl.textAlignment = UIHorizontalAlignment.Center;
                    bl.verticalAlignment = UIVerticalAlignment.Middle;
                    bl.textScale = 0.72f;
                    bl.textColor = new Color32(255, 255, 255, 255);
                    bl.text = BadgeText(tag);
                    bl.relativePosition = Vector3.zero;
                    bl.isInteractive = false;

                    UILabel tl = body.AddUIComponent<UILabel>();
                    tl.autoSize = false;
                    tl.wordWrap = true;
                    tl.width = viewW - 140f;
                    tl.height = lines * 17f;
                    tl.textScale = 0.8f;
                    tl.text = text;
                    tl.relativePosition = new Vector3(126f, y + 3f);
                    tl.isInteractive = false;

                    y += Mathf.Max(26f, lines * 17f + 8f);
                }
                body.height = y;
                float bodyH = y;

                bool open = i == 0;
                Action apply = delegate ()
                {
                    body.isVisible = open;
                    sign.text = open ? "-" : "+";
                    card.height = open ? 44f + bodyH + 8f : 38f;
                };
                apply();
                head.eventClicked += delegate (UIComponent c, UIMouseEventParameter p)
                {
                    open = !open;
                    apply();
                };
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
                _blocker.atlas = Flat.Atlas;
                _blocker.backgroundSprite = Flat.Solid;
                _blocker.color = new Color32(0, 0, 0, 170);
            }
            _blocker.isInteractive = true;
            _blocker.canFocus = true;
            _blocker.BringToFront();
            if (_wantModal)
            {
                try
                {
                    UIView.PushModal(_blocker);
                    _modal = true;
                }
                catch (Exception) { }
            }
            if (blur != null)
            {
                UITextureSprite back = _blocker.AddUIComponent<UITextureSprite>();
                back.size = res;
                back.relativePosition = Vector3.zero;
                back.texture = blur;
                back.isInteractive = false;
            }

            float width = _changelog || _helpWindow ? 660f : 540f;
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
                    sb.Append("<color #fac828>\u2022</color>  ").Append(Strip(items[k]));
                    if (k < items.Length - 1 || i < news.Count - 1) sb.Append('\n');
                    lines += 1 + Strip(items[k]).Length / 62;   // wrapped lines, estimated
                }
            }

            float bodyHeight = lines * 26f + 8f;
            float height = 56f + 34f + bodyHeight + 70f;
            height = Mathf.Min(height, res.y - 80f);
            if (_changelog || _helpWindow) height = Mathf.Min(res.y - 80f, 780f);

            UIPanel box = _blocker.AddUIComponent<UIPanel>();
            box.atlas = Flat.Atlas;
            box.backgroundSprite = Flat.Round;                   // flat window with a thin frame
            box.color = new Color32(36, 50, 54, 255);
            box.size = new Vector2(width, height);
            box.relativePosition = new Vector3((res.x - width) * 0.5f, (res.y - height) * 0.5f);
            box.isInteractive = true;

            UIPanel fill = box.AddUIComponent<UIPanel>();         // solid dark fill
            fill.atlas = Flat.Atlas;
            fill.backgroundSprite = Flat.FrameSprite;
            fill.color = new Color32(70, 92, 99, 255);
            fill.size = new Vector2(width, height);
            fill.relativePosition = Vector3.zero;
            fill.isInteractive = false;

            UILabel title = box.AddUIComponent<UILabel>();
            title.text = _helpWindow ? Loc.T("set_help") : _changelog ? Loc.T("o_changelog") : Loc.T("wn_title");
            title.textScale = 1.25f;
            title.relativePosition = new Vector3(pad, 18f);
            title.isInteractive = false;

            if (_helpWindow)
            {
                BuildHelpWindow(box, width, height, pad);
            }
            else if (_changelog)
            {
                BuildChangelog(box, news, ru, width, height, pad);
            }
            else
            {
                UILabel ver = box.AddUIComponent<UILabel>();
                ver.text = Loc.F("wn_version", current.ToString());
                ver.textScale = 0.9f;
                ver.textColor = new Color32(250, 200, 40, 255);
                ver.relativePosition = new Vector3(pad, 54f);
                ver.isInteractive = false;

                UILabel body = box.AddUIComponent<UILabel>();
                body.autoSize = false;
                body.wordWrap = true;
                body.processMarkup = true;
                body.width = textWidth;
                body.height = bodyHeight;
                body.textScale = 0.95f;
                body.text = sb.ToString();
                body.relativePosition = new Vector3(pad, 88f);
                body.isInteractive = false;

            }

            UIButton ok = box.AddUIComponent<UIButton>();
            ok.size = new Vector2(120f, 38f);
            ok.relativePosition = new Vector3((width - 120f) * 0.5f, height - 56f);
            QuayToolPanel.StyleButton(ok);
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
