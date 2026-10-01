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
            { "mode_network",  new[] { "Add network model", "Добавить network-модель" } },
            { "mode_decal",    new[] { "Add decal path", "Добавить decal-дорожку" } },
            { "mode_props",    new[] { "Props-line: props along the quay", "Props-line: пропсы вдоль набережной" } },
            { "mode_lock",     new[] { "Lock segment orientation", "Заблокировать разворот сегмента" } },
            { "hint_props",    new[] { "Select quay segments (Shift: whole connected quay), press + to add a prop line, choose a prop and tune it. Right click: clear selection, then exit.",
                                       "Выделите сегменты набережной (Shift: вся связанная набережная), нажмите +, выберите проп и настройте линию. Правый клик: снять выделение, затем выход." } },
            { "hint_lock",     new[] { "Select quay segments and press Lock: the game and other mods can no longer flip them (red = locked). Right click: clear selection, then exit.",
                                       "Выделите сегменты набережной и нажмите «Заблокировать»: игра и другие моды больше не смогут их разворачивать (красные = заблокированы). Правый клик: снять выделение, затем выход." } },
            { "adv_open",      new[] { "Extra: fence ends, scale, detach  >>", "Дополнительно: края, масштаб, отсоединение  >>" } },
            { "adv_close",     new[] { "Extra: fence ends, scale, detach  <<", "Дополнительно: края, масштаб, отсоединение  <<" } },
            { "adv_title",     new[] { "Fence ends, scale and detaching", "Края заборов, масштаб и отсоединение" } },
            { "fstart",        new[] { "Fence start along the quay (+ extends)", "Начало забора вдоль набережной (+ удлиняет)" } },
            { "fend",          new[] { "Fence end along the quay (+ extends)", "Конец забора вдоль набережной (+ удлиняет)" } },
            { "fscale",        new[] { "Width (thickness) scale", "Масштаб по ширине (толщине)" } },
            { "detachstart",   new[] { "Detach fences at segment start", "Отсоединить заборы в начале сегмента" } },
            { "detachend",     new[] { "Detach fences at segment end", "Отсоединить заборы в конце сегмента" } },
            { "detach_note",   new[] { "A detached fence is not joined to the neighbouring segment at that end, so the segment can be set up on its own. Shifting a fence end detaches that fence automatically.",
                                       "Отсоединённый забор не соединяется с соседним сегментом на этом конце, поэтому сегмент можно настроить отдельно. Сдвиг края забора отсоединяет его автоматически." } },
            { "lock_do",       new[] { "Lock selected segments", "Заблокировать выбранные" } },
            { "lock_undo",     new[] { "Unlock selected segments", "Разблокировать выбранные" } },
            { "lock_note",     new[] { "A locked segment keeps its orientation: if the game or another mod flips it (for example while a node is moved), it is flipped back at once. The Invert tool of Quay Tools still works and the lock keeps the new orientation.",
                                       "Заблокированный сегмент сохраняет направление: если игра или другой мод развернёт его (например, при перемещении узла), он тут же развернётся обратно. Инструмент «Инвертировать» из Quay Tools работает как обычно, блокировка запоминает новое направление." } },
            { "lock_state",    new[] { "Locked: {0} of {1}", "Заблокировано: {0} из {1}" } },
            { "lock_done",     new[] { "Locked {0} segment(s)", "Заблокировано сегментов: {0}" } },
            { "unlock_done",   new[] { "Unlocked {0} segment(s)", "Разблокировано сегментов: {0}" } },
            { "prop_add",      new[] { "Add prop line", "Добавить линию пропсов" } },
            { "prop_choose",   new[] { "Prop", "Проп" } },
            { "prop_search_tip", new[] { "Type part of a name to search the props", "Введите часть названия для поиска пропсов" } },
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
            { "pstart",        new[] { "Line start (+ extends, - trims)", "Начало линии (+ удлиняет, - укорачивает)" } },
            { "pend",          new[] { "Line end (+ extends, - trims)", "Конец линии (+ удлиняет, - укорачивает)" } },
            { "plateral",      new[] { "Shift forward / back across the quay (+ toward water)", "Смещение вперёд / назад поперёк набережной (+ к воде)" } },
            { "plift",         new[] { "Height (+ up, - down)", "Высота (+ вверх, - вниз)" } },
            { "pangle",        new[] { "Rotation of the props", "Поворот пропсов" } },
            { "prandrot",      new[] { "Random rotation", "Случайный поворот" } },
            { "pscale",        new[] { "Prop scale", "Масштаб пропсов" } },
            { "prandscale",    new[] { "Random size variation", "Случайный разброс размера" } },
            { "penabled",      new[] { "Line enabled", "Линия включена" } },
            { "missing",       new[] { " (not found)", " (не найден)" } },
            { "hint_nopeds",   new[] { "Select quay segments and press the button: pedestrians will not walk on them and will not see them as a path (red = blocked). Right click: clear selection, then exit.",
                                       "Выделите сегменты набережной и нажмите кнопку: пешеходы не будут ходить по ним и не будут видеть их как дорожку (красные = закрыты). Правый клик: снять выделение, затем выход." } },
            { "nop_do",        new[] { "Remove pedestrian path on selected", "Убрать пешеходную дорожку на выбранных" } },
            { "nop_undo",      new[] { "Restore pedestrian path on selected", "Вернуть пешеходную дорожку на выбранных" } },
            { "nop_note",      new[] { "The pathfinder skips these segments, so citizens neither route over them nor see them as a path. Citizens already walking along one keep going until their walk ends. Vehicle paths are blocked on them too, so use it only on pedestrian quays.",
                                       "Поиск пути обходит эти сегменты, поэтому жители не прокладывают по ним маршруты и не видят их как дорожку. Уже идущие жители дойдут до конца своего маршрута. Транспорт тоже не будет ездить по таким сегментам, поэтому используйте только на пешеходных набережных." } },
            { "nop_state",     new[] { "Pedestrian path removed: {0} of {1}", "Пешеходная дорожка убрана: {0} из {1}" } },
            { "nop_done",      new[] { "Pedestrian path removed on {0} segment(s)", "Пешеходная дорожка убрана на сегментах: {0}" } },
            { "nop_undone",    new[] { "Pedestrian path restored on {0} segment(s)", "Пешеходная дорожка возвращена на сегментах: {0}" } },
            { "soon",          new[] { " (coming soon)", " (скоро)" } },
            { "hint_invert",   new[] { "Click a quay to flip it. Shift: whole connected quay. Right click: exit.",
                                       "Клик по набережной разворачивает её. Shift: вся связанная набережная. Правый клик: выход." } },
            { "hint_network",  new[] { "Click quay segments to select them. Shift: whole connected quay. Right click: clear selection, then exit.",
                                       "Кликайте по сегментам набережной, чтобы выделить. Shift: вся связанная набережная. Правый клик: снять выделение, затем выход." } },
            { "hint_decal",    new[] { "Click quay segments to select them (Shift: whole connected quay), pick a decal and press Add. Right click: clear selection, then exit.",
                                       "Кликайте по сегментам набережной, чтобы выделить (Shift: вся связанная набережная), выберите декаль и нажмите «Добавить». Правый клик: снять выделение, затем выход." } },
            { "hint_soon",     new[] { "Coming soon.", "Скоро." } },
            { "selected",      new[] { "Selected segments: ", "Выбрано сегментов: " } },
            { "select_first",  new[] { "Select quay segments first", "Сначала выделите сегменты набережной" } },
            { "model1",        new[] { "Model 1 (land side)", "Модель 1 (со стороны суши)" } },
            { "model2",        new[] { "Model 2 (water side)", "Модель 2 (со стороны воды)" } },
            { "empty",         new[] { "Empty", "Пусто" } },
            { "hoff",          new[] { "Horizontal offset (+ toward water)", "Смещение по горизонтали (+ к воде)" } },
            { "voff",          new[] { "Vertical offset (+ up)", "Смещение по вертикали (+ вверх)" } },
            { "capstart",      new[] { "Close fence at segment start", "Закрыть забор в начале сегмента" } },
            { "capend",        new[] { "Close fence at segment end", "Закрыть забор в конце сегмента" } },
            { "remove",        new[] { "Remove models", "Убрать модели" } },
            { "typehint",      new[] { "Type a value in metres (Enter). Limit: +-{0}", "Введите значение в метрах (Enter). Предел: +-{0}" } },
            { "meter",         new[] { " m", " м" } },
            { "flipped1",      new[] { "Flipped 1 segment", "Развёрнут 1 сегмент" } },
            { "flippedN",      new[] { "Flipped {0} segments", "Развёрнуто сегментов: {0}" } },
            { "notquay",       new[] { "Not a quay: {0}", "Это не набережная: {0}" } },
            { "gone",          new[] { "Segment no longer exists", "Сегмента больше нет" } },
            { "failed",        new[] { "Action failed, see the game log", "Действие не удалось, смотрите лог игры" } },
            { "applied",       new[] { "Applied to {0} segment(s)", "Применено к сегментам: {0}" } },
            { "removed",       new[] { "Models removed from {0} segment(s)", "Модели убраны с сегментов: {0}" } },
            { "dprop",         new[] { "Decal", "Декаль" } },
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
            { "decal_added",   new[] { "Path set on {0} segment(s)", "Дорожка задана на сегментах: {0}" } },
            { "decal_removed", new[] { "Path removed from {0} segment(s)", "Дорожка убрана с сегментов: {0}" } },
            { "decal_state",   new[] { "Paths on selection: {0} of {1}", "Дорожек на выделении: {0} из {1}" } },
            { "undo",          new[] { "Undo", "Отменить" } },
            { "redo",          new[] { "Redo", "Повторить" } },
            { "reset",         new[] { "Reset", "Сброс" } },
            { "undo_tip",      new[] { "Undo the last change (Ctrl+Z)", "Отменить последнее изменение (Ctrl+Z)" } },
            { "redo_tip",      new[] { "Redo (Ctrl+Y or Ctrl+Shift+Z)", "Повторить (Ctrl+Y или Ctrl+Shift+Z)" } },
            { "reset_tip",     new[] { "Reset the offsets and options of the selected segments to default values", "Вернуть смещения и опции выбранных сегментов к значениям по умолчанию" } },
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
