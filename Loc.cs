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
            { "mode_nopeds",   new[] { "Remove pedestrian path", "Убрать пешеходные дорожки" } },
            { "mode_network", new[] { "Network-line: network models along the quay", "Network-line: сетевые модели вдоль набережной" } },
            { "mode_decal", new[] { "Texture-path: textured paths", "Texture-path: дорожки с текстурой" } },
            { "mode_props",    new[] { "Props-line: props along the quay", "Props-line: пропсы вдоль набережной" } },
            { "mode_lock",     new[] { "Lock segment orientation", "Заблокировать разворот сегмента" } },
            { "hint_props",    new[] { "Select quay segments (Shift: whole connected quay), press + to add a prop line, choose a prop and tune it. Right click: clear selection, then exit.",
                                       "Выделите сегменты набережной (Shift: вся связанная набережная), нажмите +, выберите проп и настройте линию. Правый клик: снять выделение, затем выход." } },
            { "hint_lock",     new[] { "Select quay segments and press Lock: the game and other mods can no longer flip them (red = locked). Right click: clear selection, then exit.",
                                       "Выделите сегменты набережной и нажмите «Заблокировать»: игра и другие моды больше не смогут их разворачивать (красные = заблокированы). Правый клик: снять выделение, затем выход." } },
            { "lock_do",       new[] { "Lock selected segments", "Заблокировать выбранные" } },
            { "lock_undo",     new[] { "Unlock selected segments", "Разблокировать выбранные" } },
            { "lock_note",     new[] { "A locked segment keeps its orientation: if the game or another mod flips it (for example while a node is moved), it is flipped back at once. The Invert tool of Quay Tools still works and the lock keeps the new orientation.",
                                       "Заблокированный сегмент сохраняет направление: если игра или другой мод развернёт его (например, при перемещении узла), он тут же развернётся обратно. Инструмент «Инвертировать» из Quay Tools работает как обычно, блокировка запоминает новое направление." } },
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
            { "pstep",         new[] { "Step between props", "Шаг между пропсами" } },
            { "pstart", new[] { "Line start: trim (0 = from node middle)", "Начало линии: обрезка (0 = от середины узла)" } },
            { "pend", new[] { "Line end: trim (0 = to node middle)", "Конец линии: обрезка (0 = до середины узла)" } },
            { "plateral", new[] { "Forward / back across quay (+ water)", "Вперёд/назад поперёк (+ к воде)" } },
            { "plift",         new[] { "Height (+ up, - down)", "Высота (+ вверх, - вниз)" } },
            { "pangle",        new[] { "Rotation of the props", "Поворот пропсов" } },
            { "prandrot",      new[] { "Random rotation", "Случайный поворот" } },
            { "pscale",        new[] { "Prop scale", "Масштаб пропсов" } },
            { "prandscale",    new[] { "Random size variation", "Случайный разброс размера" } },
            { "missing",       new[] { " (not found)", " (не найден)" } },
            { "hint_nopeds",   new[] { "Select quay segments and press the button: pedestrians will not walk on them and will not see them as a path (red = blocked). Right click: clear selection, then exit.",
                                       "Выделите сегменты набережной и нажмите кнопку: пешеходы не будут ходить по ним и не будут видеть их как дорожку (красные = закрыты). Правый клик: снять выделение, затем выход." } },
            { "nop_do", new[] { "Remove path on selected", "Убрать дорожку на выбранных" } },
            { "nop_undo", new[] { "Restore path on selected", "Вернуть дорожку на выбранных" } },
            { "nop_note",      new[] { "The pathfinder skips these segments, so citizens neither route over them nor see them as a path. Citizens already walking along one keep going until their walk ends. Vehicle paths are blocked on them too, so use it only on pedestrian quays.",
                                       "Поиск пути обходит эти сегменты, поэтому жители не прокладывают по ним маршруты и не видят их как дорожку. Уже идущие жители дойдут до конца своего маршрута. Транспорт тоже не будет ездить по таким сегментам, поэтому используйте только на пешеходных набережных." } },
            { "nop_state",     new[] { "Pedestrian path removed: {0} of {1}", "Пешеходная дорожка убрана: {0} из {1}" } },
            { "nop_done",      new[] { "Pedestrian path removed on {0} segment(s)", "Пешеходная дорожка убрана на сегментах: {0}" } },
            { "nop_undone",    new[] { "Pedestrian path restored on {0} segment(s)", "Пешеходная дорожка возвращена на сегментах: {0}" } },
            { "line_add",      new[] { "Add network-model line", "Добавить линию сетевой модели" } },
            { "line_choose",   new[] { "Network model", "Сетевая модель" } },
            { "line_none",     new[] { "None", "Нет" } },
            { "line_nolines",  new[] { "No lines on the selection", "На выделении нет линий" } },
            { "line_nav",      new[] { "Line {0} of {1}", "Линия {0} из {1}" } },
            { "line_remove",   new[] { "Remove this line", "Удалить эту линию" } },
            { "line_clear",    new[] { "Remove all lines", "Удалить все линии" } },
            { "line_state",    new[] { "Segments with lines: {0} of {1}", "Сегментов с линиями: {0} из {1}" } },
            { "line_added",    new[] { "Line added on {0} segment(s)", "Линия добавлена на сегментах: {0}" } },
            { "line_removed",  new[] { "Line removed on {0} segment(s)", "Линия удалена на сегментах: {0}" } },
            { "line_cleared",  new[] { "Lines removed on {0} segment(s)", "Линии удалены на сегментах: {0}" } },
            { "nstart",        new[] { "Line start: trim (0 = from node middle)", "Начало линии: обрезка (0 = от середины узла)" } },
            { "nend",          new[] { "Line end: trim (0 = to node middle)", "Конец линии: обрезка (0 = до середины узла)" } },
            { "nlateral",      new[] { "Forward / back across quay (+ water)", "Вперёд/назад поперёк (+ к воде)" } },
            { "nlift",         new[] { "Height (+ up, - down)", "Высота (+ вверх, - вниз)" } },
            { "nscale",        new[] { "Width (thickness) scale", "Масштаб по ширине (толщине)" } },
            { "nflip",         new[] { "Turn the model around (face the land)", "Развернуть модель (лицом к суше)" } },
            { "ncapstart",     new[] { "Close line at segment start", "Закрыть линию в начале сегмента" } },
            { "ncapend",       new[] { "Close line at segment end", "Закрыть линию в конце сегмента" } },
            { "tpath_add",     new[] { "Add texture path", "Добавить дорожку с текстурой" } },
            { "tpath_nolines", new[] { "No paths on the selection", "На выделении нет дорожек" } },
            { "tpath_nav",     new[] { "Path {0} of {1}", "Дорожка {0} из {1}" } },
            { "tpath_remove",  new[] { "Remove this path", "Удалить эту дорожку" } },
            { "tpath_clear",   new[] { "Remove all paths", "Удалить все дорожки" } },
            { "decal_cleared", new[] { "Paths removed on {0} segment(s)", "Дорожки удалены на сегментах: {0}" } },
            { "dstrip",        new[] { "Alternative rendering: one textured strip", "Альтернативная отрисовка: одна текстурная полоса" } },
            { "dstrip_tip",    new[] { "Instead of placing game decals step by step, the path is one strip with a composed texture (cropped by the path edges, unlit)", "Вместо расстановки игровых декалей по шагам дорожка рисуется одной полосой с составной текстурой (обрезается по краям дорожки, без освещения)" } },
            { "soon",          new[] { " (coming soon)", " (скоро)" } },
            { "hint_invert",   new[] { "Click a quay to flip it. Shift: whole connected quay. Right click: exit.",
                                       "Клик по набережной разворачивает её. Shift: вся связанная набережная. Правый клик: выход." } },
            { "hint_network", new[] { "Select quay segments (Shift: whole connected quay), press + to add a line, choose a network model and tune it. Right click: clear selection, then exit.", "Выделите сегменты набережной (Shift: вся связанная набережная), нажмите +, выберите сетевую модель и настройте линию. Правый клик: снять выделение, затем выход." } },
            { "hint_decal", new[] { "Select quay segments (Shift: whole connected quay), press + to add a path, pick a decal and tune it. Right click: clear selection, then exit.", "Выделите сегменты набережной (Shift: вся связанная набережная), нажмите +, выберите декаль и настройте дорожку. Правый клик: снять выделение, затем выход." } },
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
            { "dprop", new[] { "Decal", "Декаль" } },
            { "decal_solid",   new[] { "Plain colour (no decal)", "Просто цвет (без декали)" } },
            { "dscale",        new[] { "Decal scale (tile)", "Масштаб декали (тайл)" } },
            { "dwidth",        new[] { "Mask width", "Ширина маски" } },
            { "dlateral",      new[] { "Sideways shift (+ toward water)", "Сдвиг вбок (+ к воде)" } },
            { "dstep",         new[] { "Decal step (0 = tile size)", "Шаг декалей (0 = размер тайла)" } },
            { "dbox",          new[] { "Decal box height (thickness)", "Высота бокса декали (толщина)" } },
            { "dlift",         new[] { "Height offset (+ up)", "Высота (+ вверх)" } },
            { "dcolor",        new[] { "Tint (colour, RGBA)", "Оттенок (RGBA)" } },
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
            { "nothing",       new[] { "No quay under the cursor", "Под курсором нет набережной" } },
        };

        public static bool IsRussian
        {
            get
            {
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
