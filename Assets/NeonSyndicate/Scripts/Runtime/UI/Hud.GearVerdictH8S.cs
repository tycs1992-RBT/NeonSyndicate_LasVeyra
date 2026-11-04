using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// H8S: the gear pickup toast says at a glance how the new piece compares with the one worn in its slot: "UPGRADE
    /// +12" (item score) with an up arrow in green, "WEAKER -8" with a down arrow in red, "SAME SCORE" in grey, or "NEW
    /// SLOT +33" in cyan when nothing is worn there. The call is taken once when the toast appears (the toast stays a
    /// snapshot of that moment); drawing it allocates nothing. Rules: UpgradeVerdictRulesH8S.JudgeGear.
    /// </summary>
    public sealed partial class Hud
    {
        /// <summary>The toast's call against the piece worn in the item's slot (None without gear).</summary>
        private static void GearToastVerdictH8S(GearItem it, out UpgradeVerdictH8S verdict, out string badge)
        {
            verdict = UpgradeVerdictH8S.None;
            badge = "";
            GearSystem g = Game.Gear;
            if (it == null || g == null) return;
            int slot = (int)it.Slot;
            GearItem worn = slot >= 0 && slot < g.Equipped.Length ? g.Equipped[slot] : null;
            GearVerdictH8S v = UpgradeVerdictRulesH8S.JudgeGear(it, worn);
            verdict = v.Verdict;
            badge = UpgradeVerdictRulesH8S.GearBadge(v);
        }

        /// <summary>The badge, right-aligned on the toast's first row with its arrow at the far right.</summary>
        private static void DrawGearToastBadgeH8S(Rect row, string badge, UpgradeVerdictH8S verdict, float alpha)
        {
            if (string.IsNullOrEmpty(badge) || verdict == UpgradeVerdictH8S.None) return;
            Color c = ProgressionReadoutUiH8S.VerdictColor(verdict);
            c.a *= alpha;
            int fs = ProgressionReadoutUiH8S.Font(15, 19);
            float icon = fs;
            bool arrow = verdict == UpgradeVerdictH8S.Upgrade || verdict == UpgradeVerdictH8S.Weaker || verdict == UpgradeVerdictH8S.EmptySlot;
            if (arrow) ProgressionReadoutUiH8S.DrawVerdictIcon(new Rect(row.xMax - icon, row.y + 2f, icon, icon), verdict, c);
            UIKit.Text(new Rect(row.x, row.y, row.width - (arrow ? icon + 6f : 0f), row.height), badge, fs, c, TextAnchor.UpperRight, FontStyle.Bold);
        }
    }
}
