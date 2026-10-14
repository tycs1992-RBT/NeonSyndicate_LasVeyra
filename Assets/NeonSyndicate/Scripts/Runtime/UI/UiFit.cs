using System;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>How the pause menu tabs are laid out: rows, tabs per row, tab width and label font size.</summary>
    public struct TabLayout
    {
        public int Rows;
        public int PerRow;
        public float Width;
        public int Font;
    }

    /// <summary>
    /// Pure layout maths for labels that must fit their box (engine-free, tested offline). Text widths come from a
    /// measuring function, so the game passes UIKit.Measure and the tests a stand-in font. Used by the pause menu tabs
    /// and the SKILLS page so narrow screens (16:10, 5:4, small windows) shrink or wrap text instead of spilling it.
    /// </summary>
    public static class UiFit
    {
        /// <summary>Gap between two tabs.</summary>
        public const float TabGap = 10f;
        /// <summary>Screen width the tab strip leaves for the margins and the RESUME button.</summary>
        public const float TabRoomMargin = 420f;
        public const float TabMaxWidth = 190f;
        /// <summary>Room kept free inside a tab around its label.</summary>
        public const float TabPadding = 8f;
        /// <summary>The smallest label font a single row of tabs may use; below it the tabs wrap onto two rows.</summary>
        public const int TabMinReadableFont = 15;
        /// <summary>The smallest label font ever used.</summary>
        public const int TabMinFont = 12;

        /// <summary>The largest font size from max down to min whose measured width fits room (min when none does).</summary>
        public static int LargestFont(int max, int min, float room, Func<int, float> widthAt)
        {
            if (widthAt == null || max <= min) return Math.Max(min, max);
            for (int s = max; s > min; s--) if (widthAt(s) <= room) return s;
            return min;
        }

        /// <summary>
        /// Lays out count tabs across a menu screen W wide (UIKit units, 1080 high): one row when the widest label fits at
        /// TabMinReadableFont or larger, otherwise two rows of wider tabs. widestAt(size) is the widest label at a size.
        /// </summary>
        public static TabLayout Tabs(int count, float W, Func<int, float> widestAt)
        {
            int n = Math.Max(1, count);
            float room = Mathf.Max(0f, W - TabRoomMargin);
            TabLayout one = Row(n, n, room, widestAt);
            if (one.Font >= TabMinReadableFont || n < 2) return one;
            return Row(n, (n + 1) / 2, room, widestAt);
        }

        private static TabLayout Row(int n, int perRow, float room, Func<int, float> widestAt)
        {
            TabLayout t;
            t.PerRow = Math.Max(1, perRow);
            t.Rows = (n + t.PerRow - 1) / t.PerRow;
            t.Width = Mathf.Max(40f, Mathf.Min(TabMaxWidth, room / t.PerRow - TabGap));
            // As before: 22 for a few tabs, 18 when more than eight share a row.
            t.Font = LargestFont(t.PerRow > 8 ? 18 : 22, TabMinFont, t.Width - TabPadding, widestAt);
            return t;
        }
    }
}
