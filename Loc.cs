using System.Collections.Generic;
using ColossalFramework.Globalization;

namespace QuayTools
{
    /// <summary>Tiny two-language string table: Russian when the game language is Russian, English otherwise.</summary>
    internal static class Loc
    {
        private static readonly Dictionary<string, string[]> Table = new Dictionary<string, string[]>
        {
            { "mode_invert",   new[] { "Invert segment", "Инвертировать сегмент" } },
            { "mode_hideprops", new[] { "Hide default props", "Скрыть стандартные пропсы" } },
            { "hint_hideprops", new[] { "Select quay segments and press the button. The props that come with the quay model, such as lights, trees and benches, are hidden on them. Your own prop lines stay. Hidden segments are red. Right click clears the selection, then exits.", "Выделите сегменты набережной и нажмите кнопку. Стандартные пропсы модели набережной, такие как фонари, деревья и скамейки, на них скрываются. Ваши Props-line остаются. Сегменты со скрытыми пропсами красные. Правый клик снимает выделение, затем выходит." } },
            { "hp_do", new[] { "Hide default props", "Скрыть пропсы" } },
            { "hp_undo", new[] { "Show default props", "Вернуть пропсы" } },
            { "hp_note", new[] { "Only the props of the network model itself are hidden. Props placed with Props-line stay. The change applies to the selected segments only, not to other segments of the same quay type.", "Скрываются только пропсы самой сетевой модели. Пропсы из Props-line остаются. Изменение действует только на выбранные сегменты, а не на другие сегменты того же типа набережной." } },
            { "hp_state", new[] { "Default props hidden: {0} of {1}", "Стандартные пропсы скрыты: {0} из {1}" } },
            { "hp_done", new[] { "Default props hidden on {0} segment(s)", "Стандартные пропсы скрыты на сегментах: {0}" } },
            { "hp_undone", new[] { "Default props shown on {0} segment(s)", "Стандартные пропсы возвращены на сегментах: {0}" } },
            { "mode_nopeds",   new[] { "Remove pedestrian path", "Убрать пешеходные дорожки" } },
            { "mode_network", new[] { "Network-line: network models along the quay", "Network-line: сетевые модели вдоль набережной" } },
            { "mode_decal", new[] { "Texture-path: textured paths", "Texture-path: дорожки с текстурой" } },
            { "mode_props",    new[] { "Props-line: props along the quay", "Props-line: пропсы вдоль набережной" } },
            { "mode_lock",     new[] { "Lock segment orientation", "Заблокировать разворот сегмента" } },
            { "hint_props", new[] { "Select quay segments, press Add prop line, choose a prop and tune it. Shift selects the whole connected quay. Right click clears the selection, then exits.", "Выделите сегменты набережной, нажмите «Добавить линию пропсов», выберите проп и настройте линию. Shift выделяет всю связанную набережную. Правый клик снимает выделение, затем выходит." } },
            { "hint_lock", new[] { "Select quay segments and press Lock. The game and other mods can no longer flip them. Locked segments are red. Right click clears the selection, then exits.", "Выделите сегменты набережной и нажмите «Заблокировать». Игра и другие моды больше не смогут их разворачивать. Заблокированные сегменты красные. Правый клик снимает выделение, затем выходит." } },
            { "lock_do", new[] { "Lock selected", "Заблокировать выбранные" } },
            { "lock_undo", new[] { "Unlock selected", "Разблокировать выбранные" } },
            { "lock_note", new[] { "A locked segment keeps its orientation: if the game or another mod flips it, it is flipped back at once. The Invert tool of Quay Tools still works and the lock keeps the new orientation.", "Заблокированный сегмент сохраняет направление: если игра или другой мод развернёт его, он тут же развернётся обратно. Инструмент «Инвертировать» работает как обычно, блокировка запоминает новое направление." } },
            { "lock_state",    new[] { "Locked: {0} of {1}", "Заблокировано: {0} из {1}" } },
            { "lock_done",     new[] { "Locked {0} segment(s)", "Заблокировано сегментов: {0}" } },
            { "unlock_done",   new[] { "Unlocked {0} segment(s)", "Разблокировано сегментов: {0}" } },
            { "prop_add",      new[] { "Add prop line", "Добавить линию пропсов" } },
            { "prop_choose",   new[] { "Prop", "Проп" } },
            { "prop_search_tip", new[] { "Type part of a name to search", "Введите часть названия для поиска" } },
            { "prop_none",     new[] { "None", "Нет" } },
            { "prop_nolines",  new[] { "No prop lines on the selection", "На выделении нет линий пропсов" } },
            { "prop_nav",      new[] { "Prop line {0} of {1}", "Линия пропсов {0} из {1}" } },
            { "prop_remove",   new[] { "Remove this line", "Удалить эту линию" } },
            { "prop_clear",    new[] { "Remove all lines", "Удалить все линии" } },
            { "prop_state",    new[] { "Segments with prop lines: {0} of {1}", "Сегментов с линиями пропсов: {0} из {1}" } },
            { "prop_added",    new[] { "Prop line added on {0} segment(s)", "Линия пропсов добавлена на сегментах: {0}" } },
            { "prop_removed",  new[] { "Prop line removed on {0} segment(s)", "Линия пропсов удалена на сегментах: {0}" } },
            { "prop_cleared",  new[] { "Prop lines removed on {0} segment(s)", "Линии пропсов удалены на сегментах: {0}" } },
            { "pstep", new[] { "Prop step", "Шаг пропсов" } },
            { "pstart", new[] { "Line start", "Начало линии" } },
            { "pend", new[] { "Line end", "Конец линии" } },
            { "pshiftx", new[] { "Offset X", "Смещение по X" } },
            { "plateral", new[] { "Offset Y", "Смещение по Y" } },
            { "plift", new[] { "Offset Z", "Смещение по Z" } },
            { "pangle", new[] { "Prop rotation", "Поворот пропсов" } },
            { "ptilt", new[] { "Follow the slope", "Следовать уклону" } },
            { "ptilt_tip", new[] { "The prop leans with the height curve of the quay (up and down slopes) instead of standing level.", "Модель наклоняется вслед за кривой высот набережной (подъём и спуск), а не стоит ровно." } },
            { "prandrot", new[] { "Random rotation", "Случайный поворот" } },
            { "pscale", new[] { "Prop scale", "Масштаб пропсов" } },
            { "prandscale", new[] { "Random scale", "Случайный масштаб" } },
            { "missing",       new[] { " (not found)", " (не найден)" } },
            { "hint_nopeds", new[] { "Select quay segments and press the button. Pedestrians will not walk on them and will not see them as a path. Blocked segments are red. Right click clears the selection, then exits.", "Выделите сегменты набережной и нажмите кнопку. Пешеходы не будут ходить по ним и не будут видеть их как дорожку. Закрытые сегменты красные. Правый клик снимает выделение, затем выходит." } },
            { "nop_do", new[] { "Remove path", "Убрать дорожку" } },
            { "nop_undo", new[] { "Restore path", "Вернуть дорожку" } },
            { "nop_note", new[] { "The pathfinder skips these segments, so citizens neither route over them nor see them as a path. Citizens whose route already crosses one get a new route. Every path that may use pedestrian lanes avoids them, vehicle-only paths are not affected. Use it only on pedestrian quays.", "Поиск пути обходит эти сегменты, поэтому жители не прокладывают по ним маршруты и не видят их как дорожку. Жители, чей маршрут уже проходит по ним, получают новый маршрут. Любой маршрут с пешеходными полосами обходит такие сегменты, маршруты только для транспорта не затрагиваются. Используйте только на пешеходных набережных." } },
            { "nop_state",     new[] { "Pedestrian path removed: {0} of {1}", "Пешеходная дорожка убрана: {0} из {1}" } },
            { "nop_done",      new[] { "Pedestrian path removed on {0} segment(s)", "Пешеходная дорожка убрана на сегментах: {0}" } },
            { "nop_undone",    new[] { "Pedestrian path restored on {0} segment(s)", "Пешеходная дорожка возвращена на сегментах: {0}" } },
            { "line_add", new[] { "Add network model", "Добавить Network модель" } },
            { "line_choose", new[] { "Network model", "Network модель" } },
            { "line_none",     new[] { "None", "Нет" } },
            { "line_nolines",  new[] { "No lines on the selection", "На выделении нет линий" } },
            { "line_nav",      new[] { "Line {0} of {1}", "Линия {0} из {1}" } },
            { "line_remove",   new[] { "Remove this line", "Удалить эту линию" } },
            { "line_clear",    new[] { "Remove all lines", "Удалить все линии" } },
            { "line_state",    new[] { "Segments with lines: {0} of {1}", "Сегментов с линиями: {0} из {1}" } },
            { "line_added",    new[] { "Line added on {0} segment(s)", "Линия добавлена на сегментах: {0}" } },
            { "line_removed",  new[] { "Line removed on {0} segment(s)", "Линия удалена на сегментах: {0}" } },
            { "line_cleared",  new[] { "Lines removed on {0} segment(s)", "Линии удалены на сегментах: {0}" } },
            { "nstart", new[] { "Line start", "Начало линии" } },
            { "nend", new[] { "Line end", "Конец линии" } },
            { "nlateral", new[] { "Offset Y", "Смещение по Y" } },
            { "nlift", new[] { "Offset Z", "Смещение по Z" } },
            { "nscale", new[] { "Model width", "Ширина модели" } },
            { "nflip", new[] { "Turn the model around", "Развернуть модель" } },
            { "ncapstart",     new[] { "Close line at segment start", "Закрыть линию в начале сегмента" } },
            { "ncapend",       new[] { "Close line at segment end", "Закрыть линию в конце сегмента" } },
            { "tpath_add",     new[] { "Add texture path", "Добавить дорожку с текстурой" } },
            { "tpath_nolines", new[] { "No paths on the selection", "На выделении нет дорожек" } },
            { "tpath_nav",     new[] { "Path {0} of {1}", "Дорожка {0} из {1}" } },
            { "tpath_remove",  new[] { "Remove this path", "Удалить эту дорожку" } },
            { "tpath_clear",   new[] { "Remove all paths", "Удалить все дорожки" } },
            { "decal_cleared", new[] { "Paths removed on {0} segment(s)", "Дорожки удалены на сегментах: {0}" } },
            { "dstrip_tip", new[] { "Plane: one textured strip instead of placed decals, lit when the tint is opaque. The way is chosen when the path is added.", "Плейн: одна текстурная полоса вместо расставленных декалей, получает тени при непрозрачном оттенке. Способ выбирается при добавлении дорожки." } },
            { "tpath_add_decal", new[] { "Add decal", "Добавить декаль" } },
            { "tpath_add_plane", new[] { "Add plane", "Добавить плейн" } },
            { "dstart", new[] { "Line start", "Начало линии" } },
            { "dend", new[] { "Line end", "Конец линии" } },
            { "hidehl", new[] { "Hide highlight while dragging", "Скрыть интерфейс подсветки" } },
            { "hidehl_tip", new[] { "The highlight of quay borders and lines is hidden while a slider is dragged", "Подсветка границ набережной и линий скрывается, пока вы двигаете ползунок" } },
            { "soon",          new[] { " (coming soon)", " (скоро)" } },
            { "hint_invert", new[] { "Click a quay to flip it. Shift flips the whole connected quay. Right click exits.", "Клик по набережной разворачивает её. Shift разворачивает всю связанную набережную. Правый клик выходит." } },
            { "hint_network", new[] { "Select quay segments, press Add Network model, choose a model and tune it. Shift selects the whole connected quay. Right click clears the selection, then exits.", "Выделите сегменты набережной, нажмите «Добавить Network модель», выберите модель и настройте её. Shift выделяет всю связанную набережную. Правый клик снимает выделение, затем выходит." } },
            { "hint_decal", new[] { "Select quay segments, press Add decal or Add plane, choose a texture and tune the path. Shift selects the whole connected quay. Right click clears the selection, then exits.", "Выделите сегменты набережной, нажмите «Добавить декаль» или «Добавить плейн», выберите текстуру и настройте дорожку. Shift выделяет всю связанную набережную. Правый клик снимает выделение, затем выходит." } },
            { "hint_soon",     new[] { "Coming soon.", "Скоро." } },
            { "selected",      new[] { "Selected segments: ", "Выбрано сегментов: " } },
            { "select_first",  new[] { "Select quay segments first", "Сначала выделите сегменты набережной" } },
            { "typehint",      new[] { "Type a value in metres (Enter). Limit: +-{0}", "Введите значение в метрах (Enter). Предел: +-{0}" } },
            { "meter",         new[] { " m", " м" } },
            { "flipped1",      new[] { "Flipped 1 segment", "Развёрнут 1 сегмент" } },
            { "flippedN",      new[] { "Flipped {0} segments", "Развёрнуто сегментов: {0}" } },
            { "notquay",       new[] { "Not a quay: {0}", "Это не набережная: {0}" } },
            { "gone",          new[] { "Segment no longer exists", "Сегмента больше нет" } },
            { "failed",        new[] { "Action failed, see the game log", "Действие не удалось, смотрите лог игры" } },
            { "dprop", new[] { "Texture", "Текстура" } },
            { "decal_solid", new[] { "Colour (no texture)", "Цвет (Без текстуры)" } },
            { "dscale", new[] { "Texture scale", "Масштаб текстуры" } },
            { "dwidth", new[] { "Path width", "Ширина дорожки" } },
            { "dshiftx", new[] { "Offset X", "Смещение по X" } },
            { "dlateral", new[] { "Offset Y", "Смещение по Y" } },
            { "dstep", new[] { "Texture step", "Шаг текстуры" } },
            { "dbox", new[] { "Projection size", "Размер проекции" } },
            { "dlift", new[] { "Offset Z", "Смещение по Z" } },
            { "dcolor", new[] { "Tint RGBA", "Оттенок RGBA" } },
            { "decal_add",     new[] { "Add / apply path", "Добавить / применить дорожку" } },
            { "decal_remove",  new[] { "Remove path", "Убрать дорожку" } },
            { "decal_added", new[] { "Path added on {0} segment(s)", "Дорожка добавлена на сегментах: {0}" } },
            { "decal_removed", new[] { "Path removed on {0} segment(s)", "Дорожка удалена на сегментах: {0}" } },
            { "decal_state", new[] { "Segments with paths: {0} of {1}", "Сегментов с дорожками: {0} из {1}" } },
            { "search_tip", new[] { "Search by name", "Поиск по названию" } },
            { "typehint_int", new[] { "Type a value from {0} to {1}", "Введите значение от {0} до {1}" } },
            { "tree_tag", new[] { "[tree] ", "[дерево] " } },
            { "star_tip", new[] { "Favourite: shown first in the list", "Избранное: показывается первым в списке" } },
            { "undo",          new[] { "Undo", "Отменить" } },
            { "redo",          new[] { "Redo", "Повторить" } },
            { "reset",         new[] { "Reset", "Сброс" } },
            { "undo_tip",      new[] { "Undo the last change (Ctrl+Z)", "Отменить последнее изменение (Ctrl+Z)" } },
            { "redo_tip",      new[] { "Redo (Ctrl+Y or Ctrl+Shift+Z)", "Повторить (Ctrl+Y или Ctrl+Shift+Z)" } },
            { "reset_tip", new[] { "Reset the values of the lines of the selected segments to defaults", "Вернуть значения линий выбранных сегментов к значениям по умолчанию" } },
            { "undone",        new[] { "Undone ({0} segment(s))", "Отменено (сегментов: {0})" } },
            { "redone",        new[] { "Redone ({0} segment(s))", "Повторено (сегментов: {0})" } },
            { "nothing_undo",  new[] { "Nothing to undo", "Нечего отменять" } },
            { "nothing_redo",  new[] { "Nothing to redo", "Нечего повторять" } },
            { "reset_done",    new[] { "Reset to defaults ({0} segment(s))", "Сброшено на значения по умолчанию (сегментов: {0})" } },
            { "dcolormul", new[] { "Colour multiply", "Умножение цвета" } },
            { "dcolormul_tip", new[] { "0: the tint works the way the game applies it (only where the decal allows a colour). 1: the colour covers the whole texture (a white tint gives a white decal). Works for decals and planes.", "0: оттенок работает так, как его применяет игра (только там, где декаль допускает цвет). 1: цвет полностью покрывает текстуру (белый оттенок даёт белую декаль). Работает для декалей и плейнов." } },
            { "opt_lang", new[] { "Language / Язык", "Language / Язык" } },
            { "wn_title", new[] { "What's new in Quay Tools?", "Что нового в Quay Tools?" } },
            { "wn_version", new[] { "Version {0}", "Версия {0}" } },
            { "opt_whatsnew", new[] { "Show what's new", "Показать «Что нового»" } },
            { "wn_ok", new[] { "OK", "OK" } },
            { "opt_lang_auto", new[] { "Auto (game language) / Авто", "Auto (game language) / Авто" } },
            { "opt_quickflip", new[] { "Enable quick-flip hotkey (Ctrl + key over a quay, no tool needed)", "Включить быстрый разворот (Ctrl + клавиша над набережной, инструмент не нужен)" } },
            { "opt_hotkey", new[] { "Quick-flip hotkey", "Клавиша быстрого разворота" } },
            { "opt_swap", new[] { "Swap land/water sides for fences (use only if fences go to the wrong side everywhere)", "Поменять местами сушу и воду для заборов (только если заборы везде уходят не на ту сторону)" } },
            { "opt_undokeys", new[] { "Ctrl+Z / Ctrl+Y undo and redo while the Quay Tools window is open (turn off if it clashes with another undo mod)", "Отмена и повтор по Ctrl+Z / Ctrl+Y, пока открыто окно Quay Tools (выключите, если мешает другому моду)" } },
            { "opt_shadows", new[] { "Texture-path strips receive shadows (lit shader; turn off for the unlit look if the colours look wrong)", "Дорожки Texture-path принимают тени (освещаемый шейдер; выключите, если цвета выглядят неправильно)" } },
            { "opt_bridge", new[] { "Texture and prop paths: at sharp bends of nodes (Node Controller Renewal) bridge the gap along the centre line", "Дорожки и пропсы: на резких изгибах узлов (Node Controller Renewal) вести линию по центру промежутка" } },
            { "opt_mark", new[] { "While the tool is active, highlight edited segments and show tool icons above them", "Пока инструмент активен, подсвечивать изменённые сегменты и показывать над ними значки инструментов" } },
            { "opt_iconsize", new[] { "Size of the floating tool icons above edited segments", "Размер значков инструментов над изменёнными сегментами" } },
            { "opt_icon1", new[] { "1 (small)", "1 (малый)" } },
            { "opt_icon2", new[] { "2 (default)", "2 (по умолчанию)" } },
            { "opt_icon3", new[] { "3 (large)", "3 (большой)" } },
            { "opt_anynet", new[] { "Allow tools on any network segment (not only quays)", "Разрешить инструменты на любых сетевых сегментах (не только на набережных)" } },
            { "tip_reset_value", new[] { "Double click: reset the value", "Двойной клик: сбросить значение" } },
            { "tip_hex", new[] { "Colour #RRGGBB or #RRGGBBAA (hex)", "Цвет #RRGGBB или #RRGGBBAA (hex)" } },
            { "nothing",       new[] { "No quay under the cursor", "Под курсором нет набережной" } },
        };

        /// <summary>Language of the mod interface: the choice in the mod options (0 = by the game language, 1 = English, 2 = Russian).</summary>
        public static bool IsRussian
        {
            get
            {
                int choice = Settings.UiLanguage;
                if (choice == 1) return false;
                if (choice == 2) return true;
                try
                {
                    return LocaleManager.instance != null && LocaleManager.instance.language == "ru";
                }
                catch (System.Exception)
                {
                    return false;
                }
            }
        }

        /// <summary>0 = English, 1 = Russian: changes when the language of the game or the option changes.</summary>
        public static int Current
        {
            get { return IsRussian ? 1 : 0; }
        }

        public static string T(string key)
        {
            string[] v;
            if (!Table.TryGetValue(key, out v)) return key;
            return IsRussian ? v[1] : v[0];
        }

        public static string F(string key, params object[] args)
        {
            return string.Format(T(key), args);
        }
    }
}
