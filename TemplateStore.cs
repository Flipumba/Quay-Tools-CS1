using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

namespace QuayTools
{
    /// <summary>
    /// A saved set of lines of one segment: network-model lines, prop lines and texture paths (models and values).
    /// Start / end trims are stored as fractions of the segment length, so a template fits segments of any length.
    /// </summary>
    internal class QuayTemplate
    {
        public const int FormatVersion = 1;

        public string Name;
        public string Id;        // GUID: the real identity of the template
        public string FileName;  // technical file name (name + short code), without the folder
        public Texture2D Thumb;  // the picture (screenshot without the interface), null when there is none

        public readonly List<NetLine> Nets = new List<NetLine>();
        public readonly List<float[]> NetTrims = new List<float[]>();     // start, end (fractions of the length)
        public readonly List<PropEntry> Props = new List<PropEntry>();
        public readonly List<float[]> PropTrims = new List<float[]>();
        public readonly List<DecalSettings> Decals = new List<DecalSettings>();
        public readonly List<float[]> DecalTrims = new List<float[]>();

        public int LineCount
        {
            get { return Nets.Count + Props.Count + Decals.Count; }
        }

        /// <summary>A copy in which the models that are not loaded in this game are emptied. missing = how many different models were missing.</summary>
        public QuayTemplate ResolveModels(out int missing)
        {
            QuayTemplate c = new QuayTemplate();
            c.Name = Name;
            c.Id = Id;
            c.FileName = FileName;
            c.Thumb = Thumb;
            HashSet<string> lost = new HashSet<string>();

            for (int i = 0; i < Nets.Count; i++)
            {
                NetLine l = Nets[i].Clone();
                if (!string.IsNullOrEmpty(l.Model) && PrefabCollection<NetInfo>.FindLoaded(l.Model) == null)
                {
                    lost.Add(l.Model);
                    l.Model = null;
                }
                c.Nets.Add(l);
                c.NetTrims.Add(NetTrims[i]);
            }
            for (int i = 0; i < Props.Count; i++)
            {
                PropEntry e = Props[i].Clone();
                if (!string.IsNullOrEmpty(e.Prop) && !PropExists(e.Prop))
                {
                    lost.Add(e.Prop);
                    e.Prop = null;
                }
                c.Props.Add(e);
                c.PropTrims.Add(PropTrims[i]);
            }
            for (int i = 0; i < Decals.Count; i++)
            {
                DecalSettings d = Decals[i].Clone();
                if (!string.IsNullOrEmpty(d.Prop) && DecalCatalog.Find(d.Prop) == null)
                {
                    lost.Add(d.Prop);
                    d.Prop = null;
                }
                c.Decals.Add(d);
                c.DecalTrims.Add(DecalTrims[i]);
            }

            missing = lost.Count;
            return c;
        }

        /// <summary>A stored prop key: trees have the "tree:" prefix (see PropCatalog), props are plain prefab names.</summary>
        private static bool PropExists(string key)
        {
            if (PropCatalog.IsTreeKey(key)) return PropCatalog.FindTree(key.Substring(PropCatalog.TreePrefix.Length)) != null;
            return PropCatalog.Find(key) != null;
        }

        private static int Trim(float fraction, float length, int max)
        {
            int units = Mathf.RoundToInt(Mathf.Clamp01(fraction) * length / FenceStore.Unit);
            return -Mathf.Clamp(units, 0, max);
        }

        /// <summary>The lines of this template for a segment of the given length (fresh copies).</summary>
        public void Build(float length, out NetLineSet nets, out PropLine props, out DecalSet decals)
        {
            nets = new NetLineSet();
            for (int i = 0; i < Nets.Count && nets.Lines.Count < NetLineStore.MaxLinesPerSegment; i++)
            {
                NetLine l = Nets[i].Clone();
                l.StartShift = Trim(NetTrims[i][0], length, NetLine.MaxShift);
                l.EndShift = Trim(NetTrims[i][1], length, NetLine.MaxShift);
                nets.Lines.Add(l);
            }

            props = new PropLine();
            for (int i = 0; i < Props.Count && props.Entries.Count < NetLineStore.MaxLinesPerSegment; i++)
            {
                PropEntry e = Props[i].Clone();
                e.StartShift = Trim(PropTrims[i][0], length, PropEntry.MaxShift);
                e.EndShift = Trim(PropTrims[i][1], length, PropEntry.MaxShift);
                props.Entries.Add(e);
            }

            decals = new DecalSet();
            for (int i = 0; i < Decals.Count && decals.Paths.Count < DecalStore.MaxPathsPerSegment; i++)
            {
                DecalSettings d = Decals[i].Clone();
                d.StartShift = Trim(DecalTrims[i][0], length, DecalSettings.MaxShift);
                d.EndShift = Trim(DecalTrims[i][1], length, DecalSettings.MaxShift);
                decals.Paths.Add(d);
            }
        }
    }

    /// <summary>Templates are small text files in the game's local data folder (QuayTools/Templates); they are shared by all cities.</summary>
    internal static class TemplateStore
    {
        public const string Extension = ".qtpl";
        public const int MaxNameLength = 40;

        private const string Header = "QuayTools template";
        private static readonly char[] Forbidden = { '/', '\\', ':', '*', '?', '"', '<', '>', '|' };
        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        public static string Folder
        {
            get { return Path.Combine(Path.Combine(Application.persistentDataPath, "QuayTools"), "Templates"); }
        }

        // ---------- names ----------

        public static bool IsForbidden(char c)
        {
            if (c < ' ') return true;
            return Array.IndexOf(Forbidden, c) >= 0;
        }

        /// <summary>Removes the characters that cannot be used in a file name and cuts the name to its maximum length.</summary>
        public static string CleanName(string name)
        {
            if (name == null) return string.Empty;
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < name.Length && sb.Length < MaxNameLength; i++)
            {
                if (!IsForbidden(name[i])) sb.Append(name[i]);
            }
            return sb.ToString();
        }

        private static string FileSafe(string name)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < name.Length; i++)
            {
                char c = name[i];
                sb.Append(char.IsLetterOrDigit(c) || c == '-' || c == '_' || c == '.' ? c : '_');
            }
            string s = sb.ToString().Trim('.', '_');
            return s.Length == 0 ? "template" : s;
        }

        /// <summary>The name, or "name (2)", "name (3)" ... when a template with that name exists already.</summary>
        public static string UniqueName(string name, List<QuayTemplate> existing)
        {
            string candidate = name;
            int n = 2;
            while (HasName(existing, candidate))
            {
                candidate = name + " (" + n + ")";
                n++;
            }
            return candidate;
        }

        private static bool HasName(List<QuayTemplate> list, string name)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (string.Equals(list[i].Name, name, StringComparison.OrdinalIgnoreCase)) return true;
            }
            return false;
        }

        // ---------- loading ----------

        public static List<QuayTemplate> LoadAll()
        {
            List<QuayTemplate> list = new List<QuayTemplate>();
            try
            {
                string folder = Folder;
                if (!Directory.Exists(folder)) return list;

                string[] files = Directory.GetFiles(folder, "*" + Extension);
                for (int i = 0; i < files.Length; i++)
                {
                    try
                    {
                        QuayTemplate t = Read(files[i]);
                        if (t != null)
                        {
                            LoadThumb(t);
                            list.Add(t);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning("[QuayTools] Template file skipped (" + Path.GetFileName(files[i]) + "): " + ex.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[QuayTools] Could not read the templates folder: " + ex.Message);
            }

            list.Sort(delegate (QuayTemplate a, QuayTemplate b) { return string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase); });
            return list;
        }

        private static float F(string s)
        {
            return float.Parse(s, NumberStyles.Float, Inv);
        }

        private static int I(string s)
        {
            return int.Parse(s, NumberStyles.Integer, Inv);
        }

        private static bool B(string s)
        {
            return s == "1";
        }

        private static string Unesc(string s)
        {
            return Uri.UnescapeDataString(s);
        }

        private static string Esc(string s)
        {
            return Uri.EscapeDataString(s ?? string.Empty);
        }

        private static float Clamp01(float f)
        {
            return Mathf.Clamp01(f);
        }

        private static QuayTemplate Read(string path)
        {
            string[] lines = File.ReadAllLines(path, Encoding.UTF8);
            if (lines.Length == 0 || lines[0].Trim() != Header) throw new InvalidDataException("not a template file");

            QuayTemplate t = new QuayTemplate();
            t.FileName = Path.GetFileName(path);
            int version = 0;

            for (int i = 1; i < lines.Length; i++)
            {
                string line = lines[i];
                int eq = line.IndexOf('=');
                if (eq <= 0) continue;
                string key = line.Substring(0, eq);
                string[] p = line.Substring(eq + 1).Split(';');

                switch (key)
                {
                    case "format": version = I(p[0]); break;
                    case "name": t.Name = Unesc(p[0]); break;
                    case "id": t.Id = p[0]; break;
                    case "net":
                    {
                        NetLine l = new NetLine();
                        l.Model = p[0].Length == 0 ? null : Unesc(p[0]);
                        l.Lateral = Mathf.Clamp(I(p[1]), -NetLine.MaxOffset, NetLine.MaxOffset);
                        l.Lift = Mathf.Clamp(I(p[2]), -NetLine.MaxOffset, NetLine.MaxOffset);
                        l.Scale = Mathf.Clamp(I(p[5]), NetLine.ScaleMin, NetLine.ScaleMax);
                        l.Flip = B(p[6]);
                        l.CapStart = B(p[7]);
                        l.CapEnd = B(p[8]);
                        t.Nets.Add(l);
                        t.NetTrims.Add(new[] { Clamp01(F(p[3])), Clamp01(F(p[4])) });
                        break;
                    }
                    case "prop":
                    {
                        PropEntry e = new PropEntry();
                        e.Prop = p[0].Length == 0 ? null : Unesc(p[0]);
                        e.Step = Mathf.Clamp(I(p[1]), PropEntry.StepMin, PropEntry.StepMax);
                        e.Lateral = Mathf.Clamp(I(p[4]), -PropEntry.MaxOffset, PropEntry.MaxOffset);
                        e.Lift = Mathf.Clamp(I(p[5]), -PropEntry.MaxOffset, PropEntry.MaxOffset);
                        e.ShiftX = Mathf.Clamp(I(p[6]), -PropEntry.MaxOffset, PropEntry.MaxOffset);
                        e.Angle = I(p[7]);
                        e.RandomRotation = B(p[8]);
                        e.Tilt = B(p[9]);
                        e.Scale = Mathf.Clamp(I(p[10]), PropEntry.ScaleMin, PropEntry.ScaleMax);
                        e.ScaleRandom = Mathf.Clamp(I(p[11]), 0, PropEntry.RandomMax);
                        t.Props.Add(e);
                        t.PropTrims.Add(new[] { Clamp01(F(p[2])), Clamp01(F(p[3])) });
                        break;
                    }
                    case "decal":
                    {
                        DecalSettings d = new DecalSettings();
                        d.Prop = p[0].Length == 0 ? null : Unesc(p[0]);
                        d.Width = Mathf.Clamp(I(p[1]), DecalStore.MinWidth, DecalStore.MaxWidth);
                        d.Lateral = Mathf.Clamp(I(p[2]), -FenceStore.MaxUnits, FenceStore.MaxUnits);
                        d.Lift = Mathf.Clamp(I(p[3]), -FenceStore.MaxUnits, FenceStore.MaxUnits);
                        d.ShiftX = Mathf.Clamp(I(p[4]), -FenceStore.MaxUnits, FenceStore.MaxUnits);
                        d.R = (byte)Mathf.Clamp(I(p[5]), 0, 255);
                        d.G = (byte)Mathf.Clamp(I(p[6]), 0, 255);
                        d.B = (byte)Mathf.Clamp(I(p[7]), 0, 255);
                        d.A = (byte)Mathf.Clamp(I(p[8]), 0, 255);
                        d.Scale = Mathf.Clamp(I(p[9]), DecalStore.MinScale, DecalStore.MaxScale);
                        d.Step = Mathf.Clamp(I(p[10]), 0, DecalStore.MaxStep);
                        d.Box = Mathf.Clamp(I(p[11]), DecalStore.MinBox, DecalStore.MaxBox);
                        d.Strip = B(p[12]);
                        d.ColorMul = Mathf.Clamp(I(p[15]), 0, 10);
                        t.Decals.Add(d);
                        t.DecalTrims.Add(new[] { Clamp01(F(p[13])), Clamp01(F(p[14])) });
                        break;
                    }
                }
            }

            if (version < 1 || version > QuayTemplate.FormatVersion) throw new InvalidDataException("unknown format " + version);
            if (string.IsNullOrEmpty(t.Name)) throw new InvalidDataException("no name");
            if (string.IsNullOrEmpty(t.Id)) t.Id = Guid.NewGuid().ToString("N");
            return t;
        }

        // ---------- saving ----------

        private static string Fr(int shift, float length)
        {
            float f = length < 1f ? 0f : Mathf.Clamp01(-shift * FenceStore.Unit / length);
            return f.ToString("0.#####", Inv);
        }

        private static string Bit(bool b)
        {
            return b ? "1" : "0";
        }

        /// <summary>
        /// Saves the lines of a segment as a new template. Returns null when the segment has no lines (reason in error) or the
        /// file could not be written.
        /// </summary>
        public static QuayTemplate SaveFromSegment(string name, ushort segment, List<QuayTemplate> existing, out string error)
        {
            error = null;

            NetLineSet nets;
            PropLine props;
            DecalSet decals;
            NetLineStore.TryGet(segment, out nets);
            PropLineStore.TryGet(segment, out props);
            DecalStore.TryGet(segment, out decals);

            QuayTemplate t = new QuayTemplate();
            float length = SegmentLength(segment);

            if (nets != null)
            {
                for (int i = 0; i < nets.Lines.Count; i++)
                {
                    NetLine l = nets.Lines[i].Clone();
                    t.Nets.Add(l);
                    t.NetTrims.Add(new[] { Parse01(Fr(l.StartShift, length)), Parse01(Fr(l.EndShift, length)) });
                }
            }
            if (props != null)
            {
                for (int i = 0; i < props.Entries.Count; i++)
                {
                    PropEntry e = props.Entries[i].Clone();
                    t.Props.Add(e);
                    t.PropTrims.Add(new[] { Parse01(Fr(e.StartShift, length)), Parse01(Fr(e.EndShift, length)) });
                }
            }
            if (decals != null)
            {
                for (int i = 0; i < decals.Paths.Count; i++)
                {
                    DecalSettings d = decals.Paths[i].Clone();
                    t.Decals.Add(d);
                    t.DecalTrims.Add(new[] { Parse01(Fr(d.StartShift, length)), Parse01(Fr(d.EndShift, length)) });
                }
            }

            if (t.LineCount == 0)
            {
                error = "tpl_empty";
                return null;
            }

            t.Name = UniqueName(name, existing);
            t.Id = Guid.NewGuid().ToString("N");
            t.FileName = FileSafe(t.Name) + "_" + t.Id.Substring(0, 6) + Extension;

            if (!Write(t))
            {
                error = "tpl_save_failed";
                return null;
            }
            return t;
        }

        /// <summary>Writes the template file (new or changed).</summary>
        public static bool Write(QuayTemplate t)
        {
            try
            {
                Directory.CreateDirectory(Folder);
                File.WriteAllText(Path.Combine(Folder, t.FileName), Serialize(t), new UTF8Encoding(false));
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[QuayTools] Could not save the template: " + ex);
                return false;
            }
        }

        // ---------- picture ----------

        public static string ThumbPath(QuayTemplate t)
        {
            return Path.Combine(Folder, Path.GetFileNameWithoutExtension(t.FileName) + ".png");
        }

        /// <summary>Reads the picture of a template from its png file (null when there is none).</summary>
        public static void LoadThumb(QuayTemplate t)
        {
            if (t.Thumb != null)
            {
                UnityEngine.Object.Destroy(t.Thumb);
                t.Thumb = null;
            }

            try
            {
                string path = ThumbPath(t);
                if (!File.Exists(path)) return;

                Texture2D tex = new Texture2D(2, 2, TextureFormat.RGB24, false);
                if (tex.LoadImage(File.ReadAllBytes(path)))
                {
                    tex.wrapMode = TextureWrapMode.Clamp;
                    t.Thumb = tex;
                }
                else
                {
                    UnityEngine.Object.Destroy(tex);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[QuayTools] Could not read the template picture: " + ex.Message);
            }
        }

        /// <summary>Frees the pictures of a list of templates that is no longer used.</summary>
        public static void Release(List<QuayTemplate> list)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].Thumb != null)
                {
                    UnityEngine.Object.Destroy(list[i].Thumb);
                    list[i].Thumb = null;
                }
            }
        }

        // ---------- changes ----------

        /// <summary>A copy under a new name (" (2)" ...), with the same picture. Returns null on failure.</summary>
        public static QuayTemplate Duplicate(QuayTemplate src, List<QuayTemplate> existing)
        {
            QuayTemplate c = new QuayTemplate();
            c.Name = UniqueName(src.Name, existing);
            c.Id = Guid.NewGuid().ToString("N");
            c.FileName = FileSafe(c.Name) + "_" + c.Id.Substring(0, 6) + Extension;
            for (int i = 0; i < src.Nets.Count; i++) { c.Nets.Add(src.Nets[i].Clone()); c.NetTrims.Add((float[])src.NetTrims[i].Clone()); }
            for (int i = 0; i < src.Props.Count; i++) { c.Props.Add(src.Props[i].Clone()); c.PropTrims.Add((float[])src.PropTrims[i].Clone()); }
            for (int i = 0; i < src.Decals.Count; i++) { c.Decals.Add(src.Decals[i].Clone()); c.DecalTrims.Add((float[])src.DecalTrims[i].Clone()); }

            if (!Write(c)) return null;
            try
            {
                string pic = ThumbPath(src);
                if (File.Exists(pic)) File.Copy(pic, ThumbPath(c), true);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[QuayTools] Could not copy the template picture: " + ex.Message);
            }
            LoadThumb(c);
            return c;
        }

        /// <summary>Gives the template a new name (kept unique among the others); the file name stays.</summary>
        public static bool Rename(QuayTemplate t, string name, List<QuayTemplate> all)
        {
            List<QuayTemplate> others = new List<QuayTemplate>();
            for (int i = 0; i < all.Count; i++)
            {
                if (all[i] != t) others.Add(all[i]);
            }
            string old = t.Name;
            t.Name = UniqueName(name, others);
            if (Write(t)) return true;
            t.Name = old;
            return false;
        }

        /// <summary>Removes one line of the template: kind 0 = network-line, 1 = props-line, 2 = texture-path.</summary>
        public static bool RemoveLine(QuayTemplate t, int kind, int index)
        {
            if (kind == 0 && index < t.Nets.Count) { t.Nets.RemoveAt(index); t.NetTrims.RemoveAt(index); }
            else if (kind == 1 && index < t.Props.Count) { t.Props.RemoveAt(index); t.PropTrims.RemoveAt(index); }
            else if (kind == 2 && index < t.Decals.Count) { t.Decals.RemoveAt(index); t.DecalTrims.RemoveAt(index); }
            else return false;
            return Write(t);
        }

        private static float Parse01(string s)
        {
            return float.Parse(s, NumberStyles.Float, Inv);
        }

        private static string Serialize(QuayTemplate t)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(Header);
            sb.AppendLine("format=" + QuayTemplate.FormatVersion);
            sb.AppendLine("name=" + Esc(t.Name));
            sb.AppendLine("id=" + t.Id);

            for (int i = 0; i < t.Nets.Count; i++)
            {
                NetLine l = t.Nets[i];
                sb.AppendLine("net=" + Esc(l.Model) + ";" + l.Lateral + ";" + l.Lift + ";" + FrOf(t.NetTrims[i][0]) + ";" + FrOf(t.NetTrims[i][1]) + ";" +
                              l.Scale + ";" + Bit(l.Flip) + ";" + Bit(l.CapStart) + ";" + Bit(l.CapEnd));
            }
            for (int i = 0; i < t.Props.Count; i++)
            {
                PropEntry e = t.Props[i];
                sb.AppendLine("prop=" + Esc(e.Prop) + ";" + e.Step + ";" + FrOf(t.PropTrims[i][0]) + ";" + FrOf(t.PropTrims[i][1]) + ";" +
                              e.Lateral + ";" + e.Lift + ";" + e.ShiftX + ";" + e.Angle + ";" + Bit(e.RandomRotation) + ";" + Bit(e.Tilt) + ";" +
                              e.Scale + ";" + e.ScaleRandom);
            }
            for (int i = 0; i < t.Decals.Count; i++)
            {
                DecalSettings d = t.Decals[i];
                sb.AppendLine("decal=" + Esc(d.Prop) + ";" + d.Width + ";" + d.Lateral + ";" + d.Lift + ";" + d.ShiftX + ";" + d.R + ";" + d.G + ";" +
                              d.B + ";" + d.A + ";" + d.Scale + ";" + d.Step + ";" + d.Box + ";" + Bit(d.Strip) + ";" + FrOf(t.DecalTrims[i][0]) + ";" +
                              FrOf(t.DecalTrims[i][1]) + ";" + d.ColorMul);
            }
            return sb.ToString();
        }

        private static string FrOf(float f)
        {
            return f.ToString("0.#####", Inv);
        }

        public static bool Delete(QuayTemplate t)
        {
            try
            {
                string path = Path.Combine(Folder, t.FileName);
                if (File.Exists(path)) File.Delete(path);
                string pic = ThumbPath(t);
                if (File.Exists(pic)) File.Delete(pic);
                if (t.Thumb != null)
                {
                    UnityEngine.Object.Destroy(t.Thumb);
                    t.Thumb = null;
                }
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[QuayTools] Could not delete the template: " + ex.Message);
                return false;
            }
        }

        public static void OpenFolder()
        {
            try
            {
                Directory.CreateDirectory(Folder);
                Application.OpenURL("file://" + Folder);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[QuayTools] Could not open the templates folder: " + ex.Message);
            }
        }

        /// <summary>Length of a segment in metres (0 when unknown).</summary>
        public static float SegmentLength(ushort segment)
        {
            try
            {
                NetSegment[] segs = NetManager.instance.m_segments.m_buffer;
                float len = segs[segment].m_averageLength;
                if (len > 0.5f) return len;

                NetNode[] nodes = NetManager.instance.m_nodes.m_buffer;
                return Vector3.Distance(nodes[segs[segment].m_startNode].m_position, nodes[segs[segment].m_endNode].m_position);
            }
            catch (Exception)
            {
                return 0f;
            }
        }
    }
}
