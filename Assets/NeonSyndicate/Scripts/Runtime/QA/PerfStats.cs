using System;
using System.Collections.Generic;

namespace NeonSyndicate
{
    /// <summary>
    /// Summary statistics for a list of per-frame measurements (frame times in milliseconds, allocations in bytes):
    /// average, percentiles, maximum and the worst frames. Pure math, so the offline tests can check it.
    /// </summary>
    public static class PerfStats
    {
        public static float Average(IList<float> values)
        {
            if (values == null || values.Count == 0) return 0f;
            double sum = 0;
            for (int i = 0; i < values.Count; i++) sum += values[i];
            return (float)(sum / values.Count);
        }

        public static float Max(IList<float> values)
        {
            if (values == null || values.Count == 0) return 0f;
            float m = values[0];
            for (int i = 1; i < values.Count; i++) if (values[i] > m) m = values[i];
            return m;
        }

        public static float Min(IList<float> values)
        {
            if (values == null || values.Count == 0) return 0f;
            float m = values[0];
            for (int i = 1; i < values.Count; i++) if (values[i] < m) m = values[i];
            return m;
        }

        public static float Sum(IList<float> values)
        {
            if (values == null) return 0f;
            double sum = 0;
            for (int i = 0; i < values.Count; i++) sum += values[i];
            return (float)sum;
        }

        /// <summary>
        /// The value below which the given percent of the measurements fall (p50 is the median, p99 the slowest 1%),
        /// interpolated linearly between the two nearest ranks.
        /// </summary>
        public static float Percentile(IList<float> values, float percent)
        {
            if (values == null || values.Count == 0) return 0f;
            float[] sorted = new float[values.Count];
            values.CopyTo(sorted, 0);
            Array.Sort(sorted);
            return PercentileOfSorted(sorted, percent);
        }

        /// <summary>Like Percentile, for an array that is already sorted from smallest to largest.</summary>
        public static float PercentileOfSorted(float[] sorted, float percent)
        {
            if (sorted == null || sorted.Length == 0) return 0f;
            if (sorted.Length == 1) return sorted[0];
            double p = Math.Max(0.0, Math.Min(100.0, percent)) / 100.0;
            double rank = p * (sorted.Length - 1);
            int lo = (int)Math.Floor(rank);
            int hi = Math.Min(sorted.Length - 1, lo + 1);
            double f = rank - lo;
            return (float)(sorted[lo] + (sorted[hi] - sorted[lo]) * f);
        }

        /// <summary>Indices of the largest values, largest first (ties keep their original order).</summary>
        public static int[] WorstIndices(IList<float> values, int count)
        {
            if (values == null || values.Count == 0 || count <= 0) return new int[0];
            int[] idx = new int[values.Count];
            for (int i = 0; i < idx.Length; i++) idx[i] = i;
            Array.Sort(idx, (a, b) =>
            {
                int c = values[b].CompareTo(values[a]);
                return c != 0 ? c : a.CompareTo(b);
            });
            int n = Math.Min(count, idx.Length);
            int[] result = new int[n];
            Array.Copy(idx, result, n);
            return result;
        }

        /// <summary>How many measurements are above a limit (for example frames slower than 33.3 ms).</summary>
        public static int CountAbove(IList<float> values, float limit)
        {
            if (values == null) return 0;
            int n = 0;
            for (int i = 0; i < values.Count; i++) if (values[i] > limit) n++;
            return n;
        }
    }
}
