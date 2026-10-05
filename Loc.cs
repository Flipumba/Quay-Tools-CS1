using System.Collections.Generic;
using ColossalFramework.Globalization;

namespace QuayTools
{
    /// <summary>Strings of the interface: the language files in Locales (from Crowdin), the built-in English / Russian table as the fallback.</summary>
    internal static class Loc
    {
        private static readonly Dictionary<string, string[]> Table = new Dictionary<string, string[]>
        {
            { "tpl_ask_tpl", new[] { "This action will delete the template permanently.", "Действие удалит шаблон навсегда." } },
            { "tpl_ask_line", new[] { "This action will delete the line from the template.", "Действие удалит линию из шаблона." } },
            { "tpl_do_delete", new[] { "Delete", "Удалить" } },
            { "tpl_cancel", new[] { "Cancel", "Отменить" } },
            { "tpl_pick", new[] { "Choose a template", "Выберите шаблон" } },
            { "tpl_edit_tip", new[] { "Template settings: name, picture, copy, delete, lines", "Настройки шаблона: название, картинка, копия, удаление, линии" } },
            { "tpl_edit_title", new[] { "Template settings", "Настройки шаблона" } },
            { "tpl_rename", new[] { "Rename", "Переименовать" } },
            { "tpl_nopic", new[] { "No picture", "Нет картинки" } },
            { "tpl_shot", new[] { "New screenshot", "Новый скриншот" } },
            { "tpl_shot_tip", new[] { "Takes a new picture of the template: the scene as the camera sees it now, without the interface. Move the camera first.", "Делает новую картинку шаблона: сцена такой, какой её видит камера сейчас, без интерфейса. Сначала поставьте камеру." } },
            { "tpl_shot_failed", new[] { "Could not take the picture", "Не удалось сделать картинку" } },
            { "tpl_dup", new[] { "Duplicate", "Дублировать" } },
            { "tpl_lines_title", new[] { "Lines of the template:", "Линии шаблона:" } },
            { "tpl_line_del", new[] { "Remove this line from the template", "Убрать эту линию из шаблона" } },
            { "tpl_k_net", new[] { "Network: ", "Сеть: " } },
            { "tpl_k_prop", new[] { "Props: ", "Пропсы: " } },
            { "tpl_k_decal", new[] { "Path: ", "Дорожка: " } },
            { "tpl_empty_model", new[] { "(no model)", "(нет модели)" } },
            { "tpl_plain_path", new[] { "(plain colour)", "(просто цвет)" } },
            { "tpl_renamed", new[] { "Template renamed: {0}", "Шаблон переименован: {0}" } },
            { "tpl_duplicated", new[] { "Template duplicated: {0}", "Шаблон продублирован: {0}" } },
            { "tpl_line_removed", new[] { "Line removed from the template", "Линия убрана из шаблона" } },
            { "set_help", new[] { "Controls help", "Помощь в управлении" } },
            { "set_help_tip", new[] { "Shows which keys and mouse buttons to use", "Показывает, какие клавиши и кнопки мыши использовать" } },
            { "help_title", new[] { "Controls", "Управление" } },
            { "help_text", new[] { "Selecting segments\nClick: select one segment (the previous selection is dropped)\nShift + click: the whole connected quay\nCtrl + click: add a segment to the selection or remove it\nCtrl + Shift + click: add or remove the whole quay\nRight click: clear the selection; right click again: leave the tool\n\nHotkeys\n{act}: open and close the Quay Tools window (the key is set in the mod options)\n{flip} over a quay: flip the segment, no tool needed (the key is set in the mod options)\n{flip} + Shift: flip the whole connected quay\nCtrl + Z / Ctrl + Y (or Ctrl + Shift + Z): undo / redo while the window is open (can be turned off in the mod options)\n\nValues\nDouble click on a slider: reset the value\nA value can be typed in the field next to the slider\n\nLists\nType in the search field to filter; the star marks a favourite (favourites come first)\n\nWindow\nDrag the window by its title.\nThe pencil next to a template opens its settings", "Выделение сегментов\nКлик: выделить один сегмент (прежнее выделение снимается)\nShift + клик: вся связанная набережная\nCtrl + клик: добавить сегмент к выделению или убрать его\nCtrl + Shift + клик: добавить или убрать всю набережную\nПравый клик: снять выделение; ещё раз правый клик: выйти из инструмента\n\nГорячие клавиши\n{act}: открыть и закрыть окно Quay Tools (клавиша задаётся в настройках мода)\n{flip} над набережной: развернуть сегмент, инструмент не нужен (клавиша задаётся в настройках мода)\n{flip} + Shift: развернуть всю связанную набережную\nCtrl + Z / Ctrl + Y (или Ctrl + Shift + Z): отменить / повторить, пока открыто окно (отключается в настройках мода)\n\nЗначения\nДвойной клик по ползунку: сбросить значение\nЗначение можно ввести числом в поле рядом с ползунком\n\nСписки\nПоле поиска фильтрует список; звёздочка отмечает избранное (избранное идёт первым)\n\nОкно\nОкно перетаскивается за заголовок.\nКарандаш у шаблона открывает его настройки" } },
            { "mode_settings", new[] { "Settings", "Настройки" } },
            { "hint_settings", new[] { "Options of Quay Tools, the controls help and the button that clears all edited quays on the current map.", "Настройки Quay Tools, помощь в управлении и кнопка, которая очищает все отредактированные набережные на текущей карте." } },
            { "set_quickflip", new[] { "Quick flip", "Быстрый разворот" } },
            { "set_shadows", new[] { "Path shadows", "Тени дорожек" } },
            { "set_mark", new[] { "Mark edited segments", "Подсвечивать изменённые" } },
            { "set_icon", new[] { "Icon size", "Размер значков" } },
            { "set_clear", new[] { "Clear all quays", "Очистить все набережные" } },
            { "set_clear_tip", new[] { "Clears all edited quays on the current map. Removes from every segment: network-lines, props-lines, texture-paths, locks of the orientation, removed pedestrian paths and hidden default props. Nothing else is touched: the quays themselves, roads and the game's own fences stay, flipped segments stay flipped, and the template files are kept. Asks for a confirmation first. Undo brings everything back.", "Очищает все отредактированные набережные на текущей карте. Убирает с каждого сегмента: сетевые линии, линии пропсов, текстурные дорожки, блокировки разворота, убранные пешеходные дорожки и скрытые стандартные пропсы. Больше ничего не трогает: сами набережные, дороги и собственные заборы игры остаются, развёрнутые сегменты остаются развёрнутыми, файлы шаблонов сохраняются. Сначала спрашивает подтверждение. Отмена возвращает всё." } },
            { "clear_all_ask", new[] { "This action will clear all edited quays on the current map.", "Действие очистит все отредактированные набережные на текущей карте." } },
            { "clear_all_done", new[] { "All modifications removed, segments: {0}", "Все изменения удалены, сегм.: {0}" } },
            { "clear_all_none", new[] { "There are no modifications to remove", "Изменений для удаления нет" } },
            { "mode_templates", new[] { "Templates", "Шаблоны" } },
            { "hint_tpl", new[] { "Save the lines of a segment (network-lines, props-lines, texture-paths) as a template and apply it to other segments. Select segments: the template is saved from the first selected segment and applied to all selected ones, replacing their lines. Undo restores them.", "Сохраняйте линии сегмента (сетевые линии, линии пропсов, текстурные дорожки) как шаблон и применяйте его к другим сегментам. Выделите сегменты: шаблон сохраняется с первого выделенного сегмента и применяется ко всем выделенным, заменяя их линии. Отмена возвращает их." } },
            { "tpl_name_tip", new[] { "Name of the new template (the characters / \\ : * ? \" < > | are not allowed)", "Название нового шаблона (символы / \\ : * ? \" < > | недопустимы)" } },
            { "tpl_save", new[] { "Save as template", "Сохранить как шаблон" } },
            { "tpl_save_tip", new[] { "Saves the models and values of all lines and paths of the first selected segment, with a picture of the scene (no interface). The start / end trims are saved as a share of the segment length.", "Сохраняет модели и значения всех линий и дорожек первого выделенного сегмента вместе с картинкой сцены (без интерфейса). Обрезка начала / конца сохраняется как доля длины сегмента." } },
            { "tpl_list", new[] { "Template:", "Шаблон:" } },
            { "tpl_none", new[] { "(no templates yet)", "(шаблонов пока нет)" } },
            { "tpl_file", new[] { "File: ", "Файл: " } },
            { "tpl_lines", new[] { "Network-lines: {0}, props-lines: {1}, texture-paths: {2}", "Сетевых линий: {0}, линий пропсов: {1}, дорожек: {2}" } },
            { "tpl_apply", new[] { "Apply to selected", "Применить к выделенным" } },
            { "tpl_apply_tip", new[] { "Replaces all network-lines, props-lines and texture-paths of the selected segments with the ones of the template. Undo restores them.", "Заменяет все сетевые линии, линии пропсов и текстурные дорожки выделенных сегментов линиями шаблона. Отмена возвращает их." } },
            { "tpl_delete", new[] { "Delete", "Удалить" } },
            { "tpl_delete_tip", new[] { "Deletes the template file", "Удаляет файл шаблона" } },
            { "tpl_folder", new[] { "Open folder", "Открыть папку" } },
            { "tpl_folder_tip", new[] { "Opens the folder with the template files", "Открывает папку с файлами шаблонов" } },
            { "tpl_noname", new[] { "Type a name for the template first", "Сначала введите название шаблона" } },
            { "tpl_empty", new[] { "The segment has no lines to save", "На сегменте нет линий для сохранения" } },
            { "tpl_save_failed", new[] { "Could not save the template file", "Не удалось сохранить файл шаблона" } },
            { "tpl_saved", new[] { "Template saved: {0}", "Шаблон сохранён: {0}" } },
            { "tpl_deleted", new[] { "Template deleted: {0}", "Шаблон удалён: {0}" } },
            { "tpl_delete_failed", new[] { "Could not delete the template file", "Не удалось удалить файл шаблона" } },
            { "tpl_applied", new[] { "Template applied, segments: {0}", "Шаблон применён, сегм.: {0}" } },
            { "tpl_missing", new[] { "Models not found: {0} (empty slots).", "Моделей не найдено: {0} (пустые слоты)." } },
            { "mode_segment", new[] { "Segment Settings", "Настройки сегмента" } },
            { "hint_segment", new[] { "Select quay segments, then use the buttons and switches: invert the direction, lock the orientation, remove the pedestrian path, hide the default props. Segments with a lock, no pedestrian path or hidden props are red.", "Выделите сегменты набережной, затем используйте кнопки и переключатели: развернуть, заблокировать разворот, убрать пешеходную дорожку, скрыть стандартные пропсы. Сегменты с блокировкой, без пешеходной дорожки или со скрытыми пропсами подсвечены красным." } },
            { "seg_invert", new[] { "Invert segment", "Инвертировать сегмент" } },
            { "seg_invert_tip", new[] { "Flips the direction of the selected segments (press again to flip back).", "Разворачивает выбранные сегменты (нажмите ещё раз, чтобы вернуть)." } },
            { "seg_lock", new[] { "Lock orientation", "Заблокировать разворот" } },
            { "seg_lock_tip", new[] { "The game and other mods can no longer flip the selected segments.", "Игра и другие моды больше не смогут развернуть выбранные сегменты." } },
            { "seg_nop", new[] { "Remove pedestrian path", "Убрать пешеходную дорожку" } },
            { "seg_nop_tip", new[] { "Citizens do not walk on the selected segments.", "Жители не ходят по выбранным сегментам." } },
            { "seg_hide", new[] { "Hide default props", "Скрыть стандартные пропсы" } },
            { "seg_hide_tip", new[] { "Hides the props that come with the quay model (lights, trees, benches). Your own Props-line props stay.", "Скрывает пропсы, которые идут с моделью набережной (фонари, деревья, скамейки). Ваши пропсы из Props-line остаются." } },
            { "mode_invert",   new[] { "Invert segment", "Инвертировать сегмент" } },
            { "mode_hideprops", new[] { "Hide default props", "Скрыть стандартные пропсы" } },
            { "hint_hideprops", new[] { "Select quay segments and press the button. The props that come with the quay model, such as lights, trees and benches, are hidden on them. Your own prop lines stay. Hidden segments are red.", "Выделите сегменты набережной и нажмите кнопку. Стандартные пропсы модели набережной, такие как фонари, деревья и скамейки, на них скрываются. Ваши Props-line остаются. Сегменты со скрытыми пропсами красные." } },
            { "hp_do", new[] { "Hide default props", "Скрыть пропсы" } },
            { "hp_undo", new[] { "Show default props", "Вернуть пропсы" } },
            { "hp_note", new[] { "Only the props of the network model itself are hidden. Props placed with Props-line stay. The change applies to the selected segments only, not to other segments of the same quay type.", "Скрываются только пропсы самой сетевой модели. Пропсы из Props-line остаются. Изменение действует только на выбранные сегменты, а не на другие сегменты того же типа набережной." } },
            { "hp_state", new[] { "Default props hidden: {0} of {1}", "Стандартные пропсы скрыты: {0} из {1}" } },
            { "hp_done", new[] { "Default props hidden on {0} segment(s)", "Пропсы скрыты, сегм.: {0}" } },
            { "hp_undone", new[] { "Default props shown on {0} segment(s)", "Пропсы возвращены, сегм.: {0}" } },
            { "mode_nopeds",   new[] { "Remove pedestrian path", "Убрать пешеходные дорожки" } },
            { "mode_network", new[] { "Network-line: network models along the quay", "Network-line: сетевые модели вдоль набережной" } },
            { "mode_decal", new[] { "Texture-path: textured paths", "Texture-path: дорожки с текстурой" } },
            { "mode_props",    new[] { "Props-line: props along the quay", "Props-line: пропсы вдоль набережной" } },
            { "mode_lock",     new[] { "Lock segment orientation", "Заблокировать разворот сегмента" } },
            { "hint_props", new[] { "Select quay segments, press Add prop line, choose a prop and tune it.", "Выделите сегменты набережной, нажмите «Добавить линию пропсов», выберите проп и настройте линию." } },
            { "hint_lock", new[] { "Select quay segments and press Lock. The game and other mods can no longer flip them. Locked segments are red.", "Выделите сегменты набережной и нажмите «Заблокировать». Игра и другие моды больше не смогут их разворачивать. Заблокированные сегменты красные." } },
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
            { "prop_added",    new[] { "Prop line added on {0} segment(s)", "Линия пропсов добавлена: {0}" } },
            { "prop_removed",  new[] { "Prop line removed on {0} segment(s)", "Линия пропсов удалена: {0}" } },
            { "prop_cleared",  new[] { "Prop lines removed on {0} segment(s)", "Линии пропсов удалены: {0}" } },
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
            { "hint_nopeds", new[] { "Select quay segments and press the button. Pedestrians will not walk on them and will not see them as a path. Blocked segments are red.", "Выделите сегменты набережной и нажмите кнопку. Пешеходы не будут ходить по ним и не будут видеть их как дорожку. Закрытые сегменты красные." } },
            { "nop_do", new[] { "Remove path", "Убрать дорожку" } },
            { "nop_undo", new[] { "Restore path", "Вернуть дорожку" } },
            { "nop_note", new[] { "The pathfinder skips these segments, so citizens neither route over them nor see them as a path. Citizens whose route already crosses one get a new route. Every path that may use pedestrian lanes avoids them, vehicle-only paths are not affected. Use it only on pedestrian quays.", "Поиск пути обходит эти сегменты, поэтому жители не прокладывают по ним маршруты и не видят их как дорожку. Жители, чей маршрут уже проходит по ним, получают новый маршрут. Любой маршрут с пешеходными полосами обходит такие сегменты, маршруты только для транспорта не затрагиваются. Используйте только на пешеходных набережных." } },
            { "nop_state",     new[] { "Pedestrian path removed: {0} of {1}", "Пешеходная дорожка убрана: {0} из {1}" } },
            { "nop_done",      new[] { "Pedestrian path removed on {0} segment(s)", "Дорожка убрана, сегм.: {0}" } },
            { "nop_undone",    new[] { "Pedestrian path restored on {0} segment(s)", "Дорожка возвращена, сегм.: {0}" } },
            { "line_add", new[] { "Add network model", "Добавить Network модель" } },
            { "line_choose", new[] { "Network model", "Network модель" } },
            { "line_none",     new[] { "None", "Нет" } },
            { "line_nolines",  new[] { "No lines on the selection", "На выделении нет линий" } },
            { "line_nav",      new[] { "Line {0} of {1}", "Линия {0} из {1}" } },
            { "line_remove",   new[] { "Remove this line", "Удалить эту линию" } },
            { "line_clear",    new[] { "Remove all lines", "Удалить все линии" } },
            { "line_state",    new[] { "Segments with lines: {0} of {1}", "Сегментов с линиями: {0} из {1}" } },
            { "line_added",    new[] { "Line added on {0} segment(s)", "Линия добавлена: {0}" } },
            { "line_removed",  new[] { "Line removed on {0} segment(s)", "Линия удалена: {0}" } },
            { "line_cleared",  new[] { "Lines removed on {0} segment(s)", "Линии удалены: {0}" } },
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
            { "decal_cleared", new[] { "Paths removed on {0} segment(s)", "Дорожки удалены: {0}" } },
            { "dstrip_tip", new[] { "Plane: one textured strip instead of placed decals, lit when the tint is opaque. The way is chosen when the path is added.", "Плейн: одна текстурная полоса вместо расставленных декалей, получает тени при непрозрачном оттенке. Способ выбирается при добавлении дорожки." } },
            { "tpath_add_decal", new[] { "Add decal", "Добавить декаль" } },
            { "tpath_add_plane", new[] { "Add plane", "Добавить плейн" } },
            { "dstart", new[] { "Line start", "Начало линии" } },
            { "dend", new[] { "Line end", "Конец линии" } },
            { "hidehl", new[] { "Hide highlight while dragging", "Скрыть интерфейс подсветки" } },
            { "hidehl_tip", new[] { "The highlight of quay borders and lines is hidden while a slider is dragged", "Подсветка границ набережной и линий скрывается, пока вы двигаете ползунок" } },
            { "soon",          new[] { " (coming soon)", " (скоро)" } },
            { "hint_invert", new[] { "Click a quay to flip it. Shift flips the whole connected quay.", "Клик по набережной разворачивает её. Shift разворачивает всю связанную набережную." } },
            { "hint_network", new[] { "Select quay segments, press Add Network model, choose a model and tune it.", "Выделите сегменты набережной, нажмите «Добавить Network модель», выберите модель и настройте её." } },
            { "hint_decal", new[] { "Select quay segments, press Add decal or Add plane, choose a texture and tune the path.", "Выделите сегменты набережной, нажмите «Добавить декаль» или «Добавить плейн», выберите текстуру и настройте дорожку." } },
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
            { "decal_added", new[] { "Path added on {0} segment(s)", "Дорожка добавлена: {0}" } },
            { "decal_removed", new[] { "Path removed on {0} segment(s)", "Дорожка удалена: {0}" } },
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
            { "o_new", new[] { "What's new", "Что нового" } },
            { "o_show_wn", new[] { "Show the update window", "Показывать окно обновлений" } },
            { "o_show_wn_tip", new[] { "Shows the \"What's new\" window once after the mod has been updated to a new version.", "Один раз показывает окно «Что нового» после обновления мода до новой версии." } },
            { "o_lang", new[] { "Language", "Язык" } },
            { "o_translate", new[] { "Help translate into your language", "Помочь с переводом на ваш язык" } },
            { "o_keys", new[] { "Hotkeys", "Горячие клавиши" } },
            { "o_key_activate", new[] { "Tool activation", "Активация инструмента" } },
            { "o_key_activate_tip", new[] { "Opens and closes Quay Tools. Click the field and press the new key combination (Esc cancels).", "Открывает и закрывает Quay Tools. Нажмите на поле и затем на новую комбинацию клавиш (Esc отменяет)." } },
            { "o_key_flip", new[] { "Quick flip", "Быстрое инвертирование" } },
            { "o_key_flip_tip", new[] { "Flips the segment under the cursor without the tool; with Shift held the whole connected quay. Click the field and press the new key (Esc cancels).", "Разворачивает сегмент под курсором без инструмента; с зажатым Shift - всю связанную набережную. Нажмите на поле и затем на новую клавишу (Esc отменяет)." } },
            { "o_press", new[] { "Press a key...", "Нажмите клавишу..." } },
            { "o_settings", new[] { "Settings", "Настройки" } },
            { "o_s_undo", new[] { "Undo and redo keys", "Клавиши отмены и повтора" } },
            { "o_s_swap", new[] { "Swap land and water", "Поменять сушу и воду" } },
            { "o_s_any", new[] { "Any network segments", "Любые сетевые сегменты" } },
            { "o_s_hidetips", new[] { "Hide tooltips", "Убрать подсказки" } },
            { "o_s_hidetips_tip", new[] { "Hides the tooltips of the Quay Tools window.", "Скрывает подсказки при наведении в окне Quay Tools." } },
            { "o_support", new[] { "Support", "Поддержка" } },
            { "o_report", new[] { "Report a bug", "Сообщить об ошибке" } },
            { "o_donate", new[] { "Support the author", "Поддержать автора" } },
            { "o_tab_main", new[] { "Main", "Основные" } },
            { "o_tab_more", new[] { "Advanced", "Дополнительно" } },
            { "o_tab_links", new[] { "Links", "Ссылки" } },
            { "o_changelog", new[] { "Mod changelog", "Список изменений" } },
            { "o_show", new[] { "Show", "Показать" } },
            { "o_compat", new[] { "Compatibility check", "Проверка совместимости" } },
            { "o_compat_tip", new[] { "This function only detects compatibility issues for this mod.", "Эта функция проверяет совместимость только этого мода." } },
            { "o_check", new[] { "Check", "Проверить" } },
            { "o_reset", new[] { "Reset configuration", "Сбросить конфигурацию" } },
            { "o_reset_tip", new[] { "Warning! This resets all settings of this mod and cannot be undone.", "Внимание! Эта опция сбросит все настройки этого мода, и эта операция необратима!" } },
            { "o_reset_btn", new[] { "Reset", "Сбросить" } },
            { "o_reset_ask", new[] { "All Quay Tools settings will be set back to their defaults.", "Все настройки Quay Tools будут сброшены к значениям по умолчанию." } },
            { "o_logs", new[] { "Copy logs to desktop", "Скопировать логи на рабочий стол" } },
            { "o_copy", new[] { "Copy", "Копировать" } },
            { "o_copied", new[] { "Copied", "Скопировано" } },
            { "o_copy_fail", new[] { "Failed", "Не удалось" } },
            { "o_ok", new[] { "OK", "OK" } },
            { "o_c_title", new[] { "Quay Tools compatibility check", "Quay Tools: проверка совместимости" } },
            { "o_c_dep", new[] { "Dependencies", "Зависимости" } },
            { "o_c_dep_ok", new[] { "No missing dependencies.", "Нет отсутствующих зависимостей." } },
            { "o_c_dep_bad", new[] { "Harmony (Workshop ID 2040656402) was not found.", "Не найден Harmony (Workshop ID 2040656402)." } },
            { "o_c_ver", new[] { "Game version", "Версия игры" } },
            { "o_c_ver_ok", new[] { "The mod was tested with this game version ({0}).", "Мод проверен на этой версии игры ({0})." } },
            { "o_c_ver_bad", new[] { "The mod was tested with {0}, your game is {1}.", "Мод проверен на версии {0}, у вас {1}." } },
            { "o_c_inc", new[] { "Incompatible mods", "Несовместимые моды" } },
            { "o_c_inc_ok", new[] { "No incompatible mods found.", "Несовместимые моды не обнаружены." } },
            { "o_c_inc_bad", new[] { "Found: {0}", "Найдены: {0}" } },
            { "o_links", new[] { "Links", "Ссылки" } },
            { "o_steam", new[] { "Steam Workshop page", "Страница мода в Steam" } },
            { "o_exp_title", new[] { "Experimental setting", "Экспериментальная настройка" } },
            { "o_exp_text", new[] { "This setting is experimental and has not been tested properly. The tools may behave unexpectedly on roads and other networks. Turn it off if something goes wrong.", "Эта настройка экспериментальна и не протестирована должным образом. Инструменты могут работать непредсказуемо на дорогах и других сетях. Отключите её, если что-то пойдёт не так." } },
            { "wn_tag_new", new[] { "Added", "Добавлено" } },
            { "wn_tag_upd", new[] { "Updated", "Обновлено" } },
            { "wn_tag_fix", new[] { "Fixed", "Исправлено" } },
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
        private static string _code = "en";
        private static string _resolvedFor;
        private static int _generation;
        private static Dictionary<string, string> _file;
        private static List<string> _available;

        // names of the languages in their own language (Crowdin delivers the files as Locales/<code>.json)
        private static readonly string[][] Names =
        {
            new[] { "en", "English" },
            new[] { "ru", "Русский" },
            new[] { "de", "Deutsch" },
            new[] { "fr", "Français" },
            new[] { "es", "Español" },
            new[] { "pl", "Polski" },
            new[] { "it", "Italiano" },
            new[] { "pt-BR", "Português (Brasil)" },
            new[] { "zh-CN", "简体中文 (Chinese)" },
            new[] { "ja", "日本語 (Japanese)" },
            new[] { "ko", "한국어 (Korean)" }
        };

        public static string LanguageName(string code)
        {
            for (int i = 0; i < Names.Length; i++)
            {
                if (Names[i][0] == code) return Names[i][1];
            }
            return code;
        }

        /// <summary>Codes of the languages that have a file in the Locales folder (English is always there).</summary>
        public static List<string> AvailableLanguages
        {
            get
            {
                if (_available != null) return _available;
                List<string> list = new List<string>();
                list.Add("en");
                try
                {
                    string dir = System.IO.Path.Combine(ModPaths.ModDirectory, "Locales");
                    if (System.IO.Directory.Exists(dir))
                    {
                        string[] files = System.IO.Directory.GetFiles(dir, "*.json");
                        for (int i = 0; i < files.Length; i++)
                        {
                            string code = System.IO.Path.GetFileNameWithoutExtension(files[i]);
                            if (code != "en" && !list.Contains(code)) list.Add(code);
                        }
                    }
                }
                catch (System.Exception)
                {
                    // only English
                }
                // known languages first, in the order of the table
                List<string> sorted = new List<string>();
                for (int i = 0; i < Names.Length; i++)
                {
                    if (list.Contains(Names[i][0])) sorted.Add(Names[i][0]);
                }
                for (int i = 0; i < list.Count; i++)
                {
                    if (!sorted.Contains(list[i])) sorted.Add(list[i]);
                }
                _available = sorted;
                return _available;
            }
        }

        private static string GameLanguage()
        {
            try
            {
                if (LocaleManager.instance != null)
                {
                    string code = LocaleManager.instance.language;
                    if (code == "zh") return "zh-CN";
                    if (code == "pt") return "pt-BR";
                    return code;
                }
            }
            catch (System.Exception)
            {
                // English
            }
            return "en";
        }

        private static void Ensure()
        {
            string choice = Settings.UiLanguageCode;
            string key = choice + "|" + (choice == "auto" ? GameLanguage() : string.Empty);
            if (key == _resolvedFor) return;
            _resolvedFor = key;

            string code = choice == "auto" ? GameLanguage() : choice;
            if (!AvailableLanguages.Contains(code) && code != "ru") code = "en";
            _code = code;
            _generation++;
            _file = LoadFile(code);
        }

        private static Dictionary<string, string> LoadFile(string code)
        {
            try
            {
                string path = System.IO.Path.Combine(System.IO.Path.Combine(ModPaths.ModDirectory, "Locales"), code + ".json");
                if (!System.IO.File.Exists(path)) return null;
                return ParseJson(System.IO.File.ReadAllText(path, System.Text.Encoding.UTF8));
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogWarning("[QuayTools] Language file " + code + " could not be read: " + ex.Message);
                return null;
            }
        }

        /// <summary>A flat JSON object of strings ({"key": "text", ...}).</summary>
        private static Dictionary<string, string> ParseJson(string text)
        {
            Dictionary<string, string> map = new Dictionary<string, string>();
            int i = 0;
            while (i < text.Length && text[i] != '{') i++;
            i++;
            while (i < text.Length)
            {
                string key = ReadString(text, ref i);
                if (key == null) break;
                while (i < text.Length && text[i] != ':') i++;
                i++;
                string value = ReadString(text, ref i);
                if (value == null) break;
                map[key] = value;
            }
            return map;
        }

        // reads the next "..." string; null at the end of the object
        private static string ReadString(string t, ref int i)
        {
            while (i < t.Length && t[i] != '"')
            {
                if (t[i] == '}') return null;
                i++;
            }
            if (i >= t.Length) return null;
            i++;
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            while (i < t.Length && t[i] != '"')
            {
                char c = t[i];
                if (c == '\\' && i + 1 < t.Length)
                {
                    i++;
                    char n = t[i];
                    if (n == 'n') sb.Append('\n');
                    else if (n == 't') sb.Append('\t');
                    else if (n == 'r') { }
                    else if (n == 'u' && i + 4 < t.Length)
                    {
                        sb.Append((char)System.Convert.ToInt32(t.Substring(i + 1, 4), 16));
                        i += 4;
                    }
                    else sb.Append(n);
                }
                else
                {
                    sb.Append(c);
                }
                i++;
            }
            i++;
            return sb.ToString();
        }

        /// <summary>Code of the language in use ("en", "ru", "de", "zh-CN"...).</summary>
        public static string Code
        {
            get
            {
                Ensure();
                return _code;
            }
        }

        public static bool IsRussian
        {
            get { return Code == "ru"; }
        }

        /// <summary>Changes whenever the language of the interface changes (the window is built again then).</summary>
        public static int Current
        {
            get
            {
                Ensure();
                return _generation;
            }
        }

        public static string T(string key)
        {
            Ensure();
            string text;
            if (_file != null && _file.TryGetValue(key, out text) && text.Length > 0) return text;
            string[] v;
            if (!Table.TryGetValue(key, out v)) return key;
            return _code == "ru" ? v[1] : v[0]; // built-in English / Russian
        }

        public static string F(string key, params object[] args)
        {
            return string.Format(T(key), args);
        }
    }
}
