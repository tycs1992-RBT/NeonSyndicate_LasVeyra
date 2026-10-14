using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>What comparing two images found.</summary>
    public struct ImageDiffResult
    {
        public int Width;
        public int Height;
        /// <summary>Average absolute difference per colour channel, from 0 (identical) to 255.</summary>
        public float MeanAbsDiff;
        /// <summary>Percent of pixels where at least one channel changed by more than the threshold.</summary>
        public float PercentChanged;
        public int ChangedPixels;
        /// <summary>The largest single-channel difference found, from 0 to 255.</summary>
        public int MaxDiff;
        public int Threshold;
    }

    /// <summary>
    /// Pixel comparison for golden screenshots (used by the GoldenCompare editor tool): mean absolute difference, the
    /// share of pixels that changed beyond a threshold, and a heatmap where unchanged pixels show as a dim grey copy of
    /// the first image and changed pixels glow from blue (small change) through magenta and orange to white (large).
    /// Pure math on Color32 arrays (bottom row first, as Texture2D.GetPixels32 returns them), so it is tested offline.
    /// </summary>
    public static class ImageDiff
    {
        /// <summary>Default per-channel change (out of 255) below which a pixel counts as unchanged, about 5%.</summary>
        public const int DefaultThreshold = 12;
        /// <summary>A channel difference at or above this shows as full white in the heatmap.</summary>
        public const int HeatFullScale = 96;

        /// <summary>
        /// Compares two images of the same size. heatmap may be null; otherwise it must hold width * height pixels and
        /// receives the heatmap. A mismatch in array sizes returns a result with every pixel counted as changed.
        /// </summary>
        public static ImageDiffResult Compare(Color32[] a, Color32[] b, int width, int height, int threshold, Color32[] heatmap)
        {
            ImageDiffResult r = new ImageDiffResult { Width = width, Height = height, Threshold = threshold };
            int n = width * height;
            if (a == null || b == null || n <= 0 || a.Length < n || b.Length < n)
            {
                r.ChangedPixels = Mathf.Max(0, n);
                r.PercentChanged = 100f;
                r.MeanAbsDiff = 255f;
                r.MaxDiff = 255;
                return r;
            }
            bool heat = heatmap != null && heatmap.Length >= n;
            long sum = 0;
            int changed = 0, maxDiff = 0;
            for (int i = 0; i < n; i++)
            {
                Color32 pa = a[i], pb = b[i];
                int dr = pa.r > pb.r ? pa.r - pb.r : pb.r - pa.r;
                int dg = pa.g > pb.g ? pa.g - pb.g : pb.g - pa.g;
                int db = pa.b > pb.b ? pa.b - pb.b : pb.b - pa.b;
                sum += dr + dg + db;
                int d = dr > dg ? dr : dg;
                if (db > d) d = db;
                if (d > maxDiff) maxDiff = d;
                bool isChanged = d > threshold;
                if (isChanged) changed++;
                if (heat)
                {
                    if (isChanged) heatmap[i] = HeatColor(d / (float)HeatFullScale);
                    else
                    {
                        byte grey = (byte)((pa.r * 77 + pa.g * 151 + pa.b * 28) >> 8);
                        grey = (byte)(grey * 3 / 10);
                        heatmap[i] = new Color32(grey, grey, grey, 255);
                    }
                }
            }
            r.MeanAbsDiff = (float)(sum / (3.0 * n));
            r.ChangedPixels = changed;
            r.PercentChanged = 100f * changed / n;
            r.MaxDiff = maxDiff;
            return r;
        }

        /// <summary>Heat ramp for 0..1: dark blue, blue, magenta, orange, then white.</summary>
        public static Color32 HeatColor(float t)
        {
            t = Mathf.Clamp01(t);
            Color c0 = new Color(0.05f, 0.05f, 0.35f);
            Color c1 = new Color(0.1f, 0.35f, 1f);
            Color c2 = new Color(0.95f, 0.15f, 0.85f);
            Color c3 = new Color(1f, 0.55f, 0.1f);
            Color c4 = new Color(1f, 1f, 1f);
            Color c;
            if (t < 0.25f) c = Color.Lerp(c0, c1, t / 0.25f);
            else if (t < 0.5f) c = Color.Lerp(c1, c2, (t - 0.25f) / 0.25f);
            else if (t < 0.75f) c = Color.Lerp(c2, c3, (t - 0.5f) / 0.25f);
            else c = Color.Lerp(c3, c4, (t - 0.75f) / 0.25f);
            return new Color32((byte)Mathf.RoundToInt(c.r * 255f), (byte)Mathf.RoundToInt(c.g * 255f), (byte)Mathf.RoundToInt(c.b * 255f), 255);
        }

        /// <summary>Nearest-neighbour resize, used when two captures were taken at different resolutions.</summary>
        public static Color32[] Resize(Color32[] src, int srcWidth, int srcHeight, int width, int height)
        {
            Color32[] dst = new Color32[Mathf.Max(0, width * height)];
            if (src == null || srcWidth <= 0 || srcHeight <= 0 || width <= 0 || height <= 0) return dst;
            for (int y = 0; y < height; y++)
            {
                int sy = Mathf.Min(srcHeight - 1, (int)((y + 0.5f) * srcHeight / height));
                for (int x = 0; x < width; x++)
                {
                    int sx = Mathf.Min(srcWidth - 1, (int)((x + 0.5f) * srcWidth / width));
                    dst[y * width + x] = src[sy * srcWidth + sx];
                }
            }
            return dst;
        }
    }
}
