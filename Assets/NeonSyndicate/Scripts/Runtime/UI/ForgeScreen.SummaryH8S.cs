using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// H8S progression readability at the Forge's ARMORY tab. Under the NOW and AFTER columns, the item score before and
    /// after the change and, for an equipped gun, the gear score before and after (a backpack gun is said to count once
    /// equipped). After paying, the status line adds what changed in numbers ("ascended to Epic. Heat-cycle damage per
    /// second 112 → 131 (+17%), item score 22 → 26."). The preview line is cached per gun state; the summary is built
    /// on the click only. Rules: UpgradeVerdictRulesH8S. No cost, price or result changes.
    /// </summary>
    public sealed partial class ForgeScreen
    {
        private LootGun _scoreNowH8S;
        private int _scoreNowVersionH8S = int.MinValue;
        private int _scoreAfterKeyH8S = int.MinValue;
        private int _scoreSlotH8S = -2;
        private int _scoreGearH8S = int.MinValue;
        private string _scoreLineH8S = "";

        /// <summary>The words appended to a successful Forge change's status (built once, on the click).</summary>
        private static string ForgeChangeSummaryH8S(LootGun now, LootGun after)
        {
            if (now == null || after == null) return "";
            GunVerdictH8S v = UpgradeVerdictRulesH8S.JudgeGun(LootArmory.StatsOf(after), LootArmory.StatsOf(now));
            string text = UpgradeVerdictRulesH8S.ChangeSummary(v, UpgradeVerdictRulesH8S.GunItemScore(now), UpgradeVerdictRulesH8S.GunItemScore(after));
            return text.Length > 0 ? " " + text : "";
        }

        /// <summary>The item and gear score line under the before and after columns. Returns the y below it.</summary>
        private float DrawScoreLineH8S(Rect r, float y, LootGun now, LootGun after)
        {
            LootArmory la = Game.Loot;
            if (la == null || now == null || after == null) return y;
            int slot = la.SlotOf(now);
            int gear = GearScore.Current;
            // The preview gun is built again on every pass, so its state (not the object) is the key.
            int afterKey = ((int)after.Rarity * 64 + after.Level) * 8 + after.Ascensions + (int)after.Element * 4096;
            if (now != _scoreNowH8S || now.Version != _scoreNowVersionH8S || afterKey != _scoreAfterKeyH8S || slot != _scoreSlotH8S || gear != _scoreGearH8S)
            {
                _scoreNowH8S = now;
                _scoreNowVersionH8S = now.Version;
                _scoreAfterKeyH8S = afterKey;
                _scoreSlotH8S = slot;
                _scoreGearH8S = gear;
                int before = UpgradeVerdictRulesH8S.GunItemScore(now);
                int afterScore = UpgradeVerdictRulesH8S.GunItemScore(after);
                _scoreLineH8S = UpgradeVerdictRulesH8S.ForgeScoreLine(before, afterScore, slot, gear, slot >= 0 ? UpgradeVerdictRulesH8S.ScoreAfterSwap(gear, before, afterScore) : gear);
            }
            int fs = ProgressionReadoutUiH8S.Font(15, 18);
            UIKit.Text(new Rect(r.x, y, r.width, fs * 2f + 8f), _scoreLineH8S, fs, UIKit.Gold, TextAnchor.UpperLeft, FontStyle.Bold, true, true);
            return y + fs * 2f + 10f;
        }
    }
}
