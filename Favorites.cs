using System.Collections.Generic;

namespace QuayTools
{
    /// <summary>Favourite items of the drop-down lists (kept in the mod settings, shared by all savegames).</summary>
    internal static class Favorites
    {
        public const int Fences = 0, Decals = 1, Props = 2;

        private static readonly HashSet<string>[] Sets = new HashSet<string>[3];

        private static HashSet<string> Get(int kind)
        {
            if (kind < 0 || kind >= Sets.Length) kind = 0;
            if (Sets[kind] == null)
            {
                HashSet<string> set = new HashSet<string>();
                string raw = Settings.GetFavorites(kind);
                if (!string.IsNullOrEmpty(raw))
                {
                    string[] parts = raw.Split('|');
                    for (int i = 0; i < parts.Length; i++)
                    {
                        if (parts[i].Length > 0) set.Add(parts[i]);
                    }
                }
                Sets[kind] = set;
            }
            return Sets[kind];
        }

        public static bool Is(int kind, string key)
        {
            return !string.IsNullOrEmpty(key) && Get(kind).Contains(key);
        }

        public static void Toggle(int kind, string key)
        {
            if (string.IsNullOrEmpty(key) || key.IndexOf('|') >= 0) return;
            HashSet<string> set = Get(kind);
            if (!set.Remove(key)) set.Add(key);
            Settings.SetFavorites(kind, string.Join("|", new List<string>(set).ToArray()));
        }
    }
}
