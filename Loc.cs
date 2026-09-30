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
            { "soon",          new[] { " (coming soon)", " (скоро)" } },
            { "hint_invert",   new[] { "Click a quay to flip it. Shift: whole connected quay. Right click: exit.",
                                       "Клик по набережной разворачивает её. Shift: вся связанная набережная. Правый клик: выход." } },
            { "hint_network",  new[] { "Click quay segments to select them. Shift: whole connected quay. Right click: clear selection, then exit.",
                                       "Кликайте по сегментам набережной, чтобы выделить. Shift: вся связанная набережная. Правый клик: снять выделение, затем выход." } },
            { "hint_decal",    new[] { "Click quay segments to select them, then add a path strip along the middle of the quay. Shift: whole connected quay. Right click: clear selection, then exit.",
                                       "Кликайте по сегментам набережной, чтобы выделить, затем добавьте дорожку по середине набережной. Shift: вся связанная набережная. Правый клик: снять выделение, затем выход." } },
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
            { "dscale",        new[] { "Decal scale (width of one texture tile)", "Масштаб декали (ширина одного тайла текстуры)" } },
            { "dwidth",        new[] { "Mask width (path width)", "Ширина маски (ширина дорожки)" } },
            { "dlateral",      new[] { "Sideways shift (+ to the right of the quay direction)", "Сдвиг вбок (+ вправо по направлению набережной)" } },
            { "dlift",         new[] { "Vertical offset (+ up)", "Смещение по вертикали (+ вверх)" } },
            { "dcolor",        new[] { "Tint (colour)", "Оттенок (Tint)" } },
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
