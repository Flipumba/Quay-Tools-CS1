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
            { "soon",          new[] { " (coming soon)", " (скоро)" } },
            { "hint_invert",   new[] { "Click a quay to flip it. Shift: whole connected quay. Right click: exit.",
                                       "Клик по набережной разворачивает её. Shift: вся связанная набережная. Правый клик: выход." } },
            { "hint_network",  new[] { "Click quay segments to select them. Shift: whole connected quay. Right click: clear selection, then exit.",
                                       "Кликайте по сегментам набережной, чтобы выделить. Shift: вся связанная набережная. Правый клик: снять выделение, затем выход." } },
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
