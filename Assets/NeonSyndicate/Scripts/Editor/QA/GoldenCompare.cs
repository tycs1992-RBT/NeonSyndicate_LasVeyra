using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace NeonSyndicate.EditorTools
{
    /// <summary>One image in a golden comparison report.</summary>
    [Serializable]
    public sealed class GoldenCompareImage
    {
        public string File;
        public string Verdict;
        public float MeanAbsDiff;
        public float PercentChanged;
        public int MaxDiff;
        public int Width;
        public int Height;
        public bool Resized;
        public string Heatmap;
    }

    /// <summary>report.json of a golden comparison (the same numbers as report.html, for scripts and AI sessions).</summary>
    [Serializable]
    public sealed class GoldenCompareReport
    {
        public string Before;
        public string After;
        public string ComparedAt;
        public int Threshold;
        public List<string> SettingsThatDiffer = new List<string>();
        public List<string> OnlyBefore = new List<string>();
        public List<string> OnlyAfter = new List<string>();
        public List<GoldenCompareImage> Images = new List<GoldenCompareImage>();
    }

    /// <summary>
    /// Compares two golden-shot folders (Neon Syndicate > QA > Compare Golden Shots): for every PNG found in both, the
    /// mean absolute difference, the percent of pixels changed beyond a threshold and a heatmap PNG. Writes report.html
    /// (open it in a browser: before, after and heatmap side by side, biggest changes first), report.md and report.json
    /// into GoldenShots/Compare_before_vs_after, and lists settings that differ between the two manifests. Also runs
    /// headless: -executeMethod NeonSyndicate.EditorTools.GoldenCompare.CompareFromCommandLine -goldenBefore dir
    /// -goldenAfter dir [-goldenThreshold 12].
    /// </summary>
    public static class GoldenCompare
    {
        private const string Tag = "[Neon Syndicate] ";

        // ------------------------------------------------------------------ entry points

        public static void CompareWithDialogs()
        {
            string root = QaTools.GoldenRoot;
            Directory.CreateDirectory(root);
            List<string> captures = CaptureFolders(root);
            string before = null, after = null;
            if (captures.Count >= 2)
            {
                string a = captures[captures.Count - 2], b = captures[captures.Count - 1];
                int choice = EditorUtility.DisplayDialogComplex("Compare Golden Shots",
                    "Compare the two newest captures?\n\nBefore: " + Path.GetFileName(a) + "\nAfter: " + Path.GetFileName(b),
                    "Compare these two", "Cancel", "Pick folders...");
                if (choice == 1) return;
                if (choice == 0)
                {
                    before = a;
                    after = b;
                }
            }
            if (before == null)
            {
                before = EditorUtility.OpenFolderPanel("Golden shots BEFORE the change (the older folder)", root, "");
                if (string.IsNullOrEmpty(before)) return;
                after = EditorUtility.OpenFolderPanel("Golden shots AFTER the change (the newer folder)", root, "");
                if (string.IsNullOrEmpty(after)) return;
            }
            string report = Compare(before, after, ImageDiff.DefaultThreshold, true);
            if (report == null) return;
            EditorUtility.RevealInFinder(report);
            Application.OpenURL(new Uri(report).AbsoluteUri);
        }

        /// <summary>Batch-mode entry point. Exits with 0 when the report is written and 1 when the comparison could not run.</summary>
        public static void CompareFromCommandLine()
        {
            string before = Argument("-goldenBefore"), after = Argument("-goldenAfter");
            int threshold = ImageDiff.DefaultThreshold;
            string t = Argument("-goldenThreshold");
            if (!string.IsNullOrEmpty(t)) int.TryParse(t, NumberStyles.Integer, CultureInfo.InvariantCulture, out threshold);
            string report = null;
            if (string.IsNullOrEmpty(before) || string.IsNullOrEmpty(after)) Debug.LogError(Tag + "Golden compare needs -goldenBefore <folder> and -goldenAfter <folder>.");
            else report = Compare(before, after, threshold, false);
            if (Application.isBatchMode) EditorApplication.Exit(report != null ? 0 : 1);
        }

        /// <summary>Compares two folders and returns the path of report.html, or null if it could not run.</summary>
        public static string Compare(string beforeDir, string afterDir, int threshold, bool interactive)
        {
            if (!Directory.Exists(beforeDir) || !Directory.Exists(afterDir))
            {
                Fail("Golden compare: both folders must exist (" + beforeDir + ", " + afterDir + ").", interactive);
                return null;
            }
            beforeDir = Path.GetFullPath(beforeDir);
            afterDir = Path.GetFullPath(afterDir);
            GoldenCompareReport report = new GoldenCompareReport
            {
                Before = beforeDir,
                After = afterDir,
                ComparedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
                Threshold = threshold
            };
            string outDir = Path.Combine(Path.GetDirectoryName(afterDir), "Compare_" + Path.GetFileName(beforeDir) + "_vs_" + Path.GetFileName(afterDir));
            Directory.CreateDirectory(outDir);
            CompareManifests(beforeDir, afterDir, report.SettingsThatDiffer);
            SortedSet<string> a = Pngs(beforeDir), b = Pngs(afterDir);
            foreach (string f in a) if (!b.Contains(f)) report.OnlyBefore.Add(f);
            foreach (string f in b) if (!a.Contains(f)) report.OnlyAfter.Add(f);
            List<string> both = new List<string>();
            foreach (string f in a) if (b.Contains(f)) both.Add(f);
            if (both.Count == 0)
            {
                Fail("Golden compare: the two folders have no screenshots with the same file name.", interactive);
                return null;
            }
            try
            {
                for (int i = 0; i < both.Count; i++)
                {
                    string file = both[i];
                    if (interactive && EditorUtility.DisplayCancelableProgressBar("Compare Golden Shots", file, i / (float)both.Count))
                    {
                        Debug.LogWarning(Tag + "Golden compare cancelled.");
                        return null;
                    }
                    GoldenCompareImage img = CompareImage(Path.Combine(beforeDir, file), Path.Combine(afterDir, file), outDir, file, threshold);
                    if (img != null) report.Images.Add(img);
                }
            }
            finally
            {
                if (interactive) EditorUtility.ClearProgressBar();
            }
            report.Images.Sort((x, y) => y.PercentChanged.CompareTo(x.PercentChanged));
            string html = Path.Combine(outDir, "report.html");
            File.WriteAllText(Path.Combine(outDir, "report.json"), JsonUtility.ToJson(report, true));
            File.WriteAllText(Path.Combine(outDir, "report.md"), BuildMarkdown(report));
            File.WriteAllText(html, BuildHtml(report, outDir));
            int changed = 0;
            foreach (GoldenCompareImage img in report.Images) if (img.Verdict != "same") changed++;
            Debug.Log(Tag + "Golden compare: " + report.Images.Count + " images compared, " + changed + " changed. Report: " + html);
            return html;
        }

        // ------------------------------------------------------------------ comparing

        private static GoldenCompareImage CompareImage(string beforePath, string afterPath, string outDir, string file, int threshold)
        {
            Texture2D ta = Load(beforePath), tb = Load(afterPath);
            Texture2D heat = null;
            try
            {
                if (ta == null || tb == null)
                {
                    Debug.LogWarning(Tag + "Golden compare: could not read " + file + ".");
                    return null;
                }
                int w = ta.width, h = ta.height;
                Color32[] pa = ta.GetPixels32();
                Color32[] pb = tb.GetPixels32();
                bool resized = tb.width != w || tb.height != h;
                if (resized) pb = ImageDiff.Resize(pb, tb.width, tb.height, w, h);
                Color32[] map = new Color32[w * h];
                ImageDiffResult r = ImageDiff.Compare(pa, pb, w, h, threshold, map);
                heat = new Texture2D(w, h, TextureFormat.RGB24, false);
                heat.SetPixels32(map);
                heat.Apply(false);
                string heatName = "heat_" + file;
                File.WriteAllBytes(Path.Combine(outDir, heatName), heat.EncodeToPNG());
                return new GoldenCompareImage
                {
                    File = file,
                    Verdict = Verdict(r.PercentChanged, resized),
                    MeanAbsDiff = r.MeanAbsDiff,
                    PercentChanged = r.PercentChanged,
                    MaxDiff = r.MaxDiff,
                    Width = w,
                    Height = h,
                    Resized = resized,
                    Heatmap = heatName
                };
            }
            finally
            {
                if (ta != null) UnityEngine.Object.DestroyImmediate(ta);
                if (tb != null) UnityEngine.Object.DestroyImmediate(tb);
                if (heat != null) UnityEngine.Object.DestroyImmediate(heat);
            }
        }

        /// <summary>A plain-words label: same (under 0.1% of pixels), small (under 2%), changed (under 15%) or big.</summary>
        public static string Verdict(float percentChanged, bool resized)
        {
            string v = percentChanged < 0.1f ? "same" : percentChanged < 2f ? "small" : percentChanged < 15f ? "changed" : "big";
            return resized && v == "same" ? "small" : v;
        }

        private static Texture2D Load(string path)
        {
            try
            {
                Texture2D t = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (t.LoadImage(File.ReadAllBytes(path), false)) return t;
                UnityEngine.Object.DestroyImmediate(t);
            }
            catch (Exception e)
            {
                Debug.LogWarning(Tag + "Golden compare: " + path + ": " + e.Message);
            }
            return null;
        }

        private static void CompareManifests(string beforeDir, string afterDir, List<string> diffs)
        {
            GoldenManifest a = ReadManifest(beforeDir), b = ReadManifest(afterDir);
            if (a == null || b == null)
            {
                diffs.Add("manifest.json missing in " + (a == null ? "the before folder" : "the after folder") + " (settings not compared)");
                return;
            }
            Diff(diffs, "Unity version", a.UnityVersion, b.UnityVersion);
            Diff(diffs, "Render path", a.RenderPath, b.RenderPath);
            Diff(diffs, "Platform", a.Platform, b.Platform);
            Diff(diffs, "Graphics device", a.GraphicsDevice, b.GraphicsDevice);
            Diff(diffs, "Resolution", a.Width + "x" + a.Height, b.Width + "x" + b.Height);
            Diff(diffs, "Graphics preset", a.GraphicsPreset, b.GraphicsPreset);
            Diff(diffs, "Post effects", a.PostEffects.ToString(), b.PostEffects.ToString());
            Diff(diffs, "Ambient occlusion", a.AmbientOcclusion.ToString(), b.AmbientOcclusion.ToString());
            Diff(diffs, "Motion blur", a.MotionBlur.ToString(), b.MotionBlur.ToString());
            Diff(diffs, "Eye adaptation", a.AutoExposure.ToString(), b.AutoExposure.ToString());
            Diff(diffs, "Film grain in shots", a.FilmGrainInShots.ToString(), b.FilmGrainInShots.ToString());
            Diff(diffs, "Brightness", a.Brightness.ToString("0.00", CultureInfo.InvariantCulture), b.Brightness.ToString("0.00", CultureInfo.InvariantCulture));
            Diff(diffs, "Seed", a.Seed.ToString(CultureInfo.InvariantCulture), b.Seed.ToString(CultureInfo.InvariantCulture));
            Diff(diffs, "Game version", a.GameVersion, b.GameVersion);
        }

        private static void Diff(List<string> diffs, string what, string a, string b)
        {
            if ((a ?? "") != (b ?? "")) diffs.Add(what + ": " + a + " -> " + b);
        }

        private static GoldenManifest ReadManifest(string dir)
        {
            string p = Path.Combine(dir, "manifest.json");
            if (!File.Exists(p)) return null;
            try { return JsonUtility.FromJson<GoldenManifest>(File.ReadAllText(p)); }
            catch (Exception) { return null; }
        }

        private static SortedSet<string> Pngs(string dir)
        {
            SortedSet<string> set = new SortedSet<string>(StringComparer.Ordinal);
            foreach (string f in Directory.GetFiles(dir, "*.png"))
            {
                string name = Path.GetFileName(f);
                if (!name.StartsWith("heat_", StringComparison.Ordinal)) set.Add(name);
            }
            return set;
        }

        /// <summary>Capture folders (not comparison reports) under the golden root, oldest first by name (names are dates).</summary>
        private static List<string> CaptureFolders(string root)
        {
            List<string> list = new List<string>();
            foreach (string d in Directory.GetDirectories(root))
                if (!Path.GetFileName(d).StartsWith("Compare_", StringComparison.Ordinal)) list.Add(d);
            list.Sort(StringComparer.Ordinal);
            return list;
        }

        // ------------------------------------------------------------------ reports

        private static string N(float v)
        {
            return v.ToString("0.00", CultureInfo.InvariantCulture);
        }

        private static string BuildMarkdown(GoldenCompareReport r)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("# Golden shots comparison\n\n");
            sb.Append("- Before: ").Append(r.Before).Append('\n');
            sb.Append("- After: ").Append(r.After).Append('\n');
            sb.Append("- Compared: ").Append(r.ComparedAt).Append(", a pixel counts as changed when a colour channel moved by more than ").Append(r.Threshold).Append(" of 255\n\n");
            if (r.SettingsThatDiffer.Count > 0)
            {
                sb.Append("## Settings that differ\n\n");
                foreach (string s in r.SettingsThatDiffer) sb.Append("- ").Append(s).Append('\n');
                sb.Append('\n');
            }
            sb.Append("| Image | Verdict | Pixels changed | Mean difference | Largest difference |\n|---|---|---|---|---|\n");
            foreach (GoldenCompareImage i in r.Images)
                sb.Append("| ").Append(i.File).Append(" | ").Append(i.Verdict).Append(i.Resized ? " (sizes differ)" : "").Append(" | ").Append(N(i.PercentChanged)).Append("% | ")
                    .Append(N(i.MeanAbsDiff)).Append(" | ").Append(i.MaxDiff).Append(" |\n");
            if (r.OnlyBefore.Count > 0) sb.Append("\nOnly in before: ").Append(string.Join(", ", r.OnlyBefore.ToArray())).Append('\n');
            if (r.OnlyAfter.Count > 0) sb.Append("\nOnly in after: ").Append(string.Join(", ", r.OnlyAfter.ToArray())).Append('\n');
            return sb.ToString();
        }

        private static string Link(string outDir, string target)
        {
            // Relative links keep the report working when the GoldenShots folder is moved or zipped.
            Uri from = new Uri(outDir.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal) ? outDir : outDir + Path.DirectorySeparatorChar);
            Uri to = new Uri(target);
            return from.MakeRelativeUri(to).ToString();
        }

        private static string Html(string s)
        {
            return (s ?? "").Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");
        }

        private static string BuildHtml(GoldenCompareReport r, string outDir)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("<!doctype html>\n<html><head><meta charset=\"utf-8\"><title>Golden shots comparison</title>\n<style>\n");
            sb.Append("body{background:#111318;color:#e6e6ea;font-family:system-ui,sans-serif;margin:24px}");
            sb.Append("h1{font-size:22px}h2{font-size:17px;margin:28px 0 8px}table{border-collapse:collapse}td,th{padding:4px 10px;text-align:left}");
            sb.Append(".row{display:flex;gap:8px}.row figure{margin:0;flex:1}.row img{width:100%;border:1px solid #333}");
            sb.Append("figcaption{font-size:12px;color:#9aa}.same{color:#7c7}.small{color:#cc6}.changed{color:#f93}.big{color:#f55}");
            sb.Append(".card{background:#1a1d24;padding:12px;margin:14px 0;border-radius:6px}\n</style></head><body>\n");
            sb.Append("<h1>Golden shots comparison</h1>\n<p>Before: ").Append(Html(r.Before)).Append("<br>After: ").Append(Html(r.After))
                .Append("<br>Compared ").Append(Html(r.ComparedAt)).Append(". A pixel counts as changed when a colour channel moved by more than ")
                .Append(r.Threshold).Append(" of 255. In the heatmap, grey is unchanged; blue, magenta, orange and white are bigger and bigger changes.</p>\n");
            if (r.SettingsThatDiffer.Count > 0)
            {
                sb.Append("<h2>Settings that differ</h2><ul>");
                foreach (string s in r.SettingsThatDiffer) sb.Append("<li>").Append(Html(s)).Append("</li>");
                sb.Append("</ul>\n");
            }
            if (r.OnlyBefore.Count > 0) sb.Append("<p>Only in before: ").Append(Html(string.Join(", ", r.OnlyBefore.ToArray()))).Append("</p>\n");
            if (r.OnlyAfter.Count > 0) sb.Append("<p>Only in after: ").Append(Html(string.Join(", ", r.OnlyAfter.ToArray()))).Append("</p>\n");
            sb.Append("<h2>Summary (biggest changes first)</h2><table><tr><th>Image</th><th>Verdict</th><th>Pixels changed</th><th>Mean difference</th><th>Largest</th></tr>\n");
            foreach (GoldenCompareImage i in r.Images)
                sb.Append("<tr><td><a href=\"#").Append(Html(i.File)).Append("\">").Append(Html(i.File)).Append("</a></td><td class=\"").Append(i.Verdict).Append("\">")
                    .Append(i.Verdict).Append(i.Resized ? " (sizes differ)" : "").Append("</td><td>").Append(N(i.PercentChanged)).Append("%</td><td>")
                    .Append(N(i.MeanAbsDiff)).Append("</td><td>").Append(i.MaxDiff).Append("</td></tr>\n");
            sb.Append("</table>\n");
            foreach (GoldenCompareImage i in r.Images)
            {
                string a = Html(Link(outDir, Path.Combine(r.Before, i.File))), b = Html(Link(outDir, Path.Combine(r.After, i.File)));
                sb.Append("<div class=\"card\" id=\"").Append(Html(i.File)).Append("\"><b>").Append(Html(i.File)).Append("</b> <span class=\"").Append(i.Verdict).Append("\">")
                    .Append(i.Verdict).Append("</span>: ").Append(N(i.PercentChanged)).Append("% of pixels changed, mean difference ").Append(N(i.MeanAbsDiff)).Append("\n<div class=\"row\">");
                sb.Append("<figure><a href=\"").Append(a).Append("\"><img src=\"").Append(a).Append("\"></a><figcaption>before</figcaption></figure>");
                sb.Append("<figure><a href=\"").Append(b).Append("\"><img src=\"").Append(b).Append("\"></a><figcaption>after</figcaption></figure>");
                sb.Append("<figure><a href=\"").Append(Html(i.Heatmap)).Append("\"><img src=\"").Append(Html(i.Heatmap)).Append("\"></a><figcaption>heatmap</figcaption></figure>");
                sb.Append("</div></div>\n");
            }
            sb.Append("</body></html>\n");
            return sb.ToString();
        }

        // ------------------------------------------------------------------ helpers

        private static void Fail(string message, bool interactive)
        {
            Debug.LogError(Tag + message);
            if (interactive) EditorUtility.DisplayDialog("Compare Golden Shots", message, "OK");
        }

        private static string Argument(string name)
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
                if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase)) return args[i + 1];
            return null;
        }
    }
}
