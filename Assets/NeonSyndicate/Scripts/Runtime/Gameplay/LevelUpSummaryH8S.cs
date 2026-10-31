using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>The player's level, attributes and the stats they feed, at one moment (before or after a shrine visit).</summary>
    public struct LevelSnapshotH8S
    {
        public bool Valid;
        public int Level;
        public int Vigor, Endurance, Might, Focus;
        public float MaxHealth, MaxStamina, MaxEnergy;
        /// <summary>Progression.MeleeMultiplier, GunMultiplier and PowerMultiplier at that moment.</summary>
        public float Melee, Gun, Power;
    }

    /// <summary>
    /// H8S: the plain summary of what changed across one or more level ups (a shrine visit can raise several levels).
    /// The title names the level reached; the body says, for every attribute that rose, what it now gives in real
    /// numbers taken from the live player before and after (max health, stamina and energy include gear and skills),
    /// and the skill points it brought. Presentation only. Engine-free, tested by ProgressionReadoutH8STests.
    /// </summary>
    public static class LevelUpSummaryH8S
    {
        /// <summary>True when b is a later, higher level than a and both were captured.</summary>
        public static bool Rose(LevelSnapshotH8S a, LevelSnapshotH8S b)
        {
            return a.Valid && b.Valid && b.Level > a.Level;
        }

        /// <summary>"LEVEL 9", or "LEVEL 7 → 9" for several levels at once (empty when nothing rose).</summary>
        public static string Title(LevelSnapshotH8S a, LevelSnapshotH8S b)
        {
            if (!Rose(a, b)) return "";
            return b.Level - a.Level == 1 ? "LEVEL " + b.Level : "LEVEL " + a.Level + UpgradeVerdictRulesH8S.To + b.Level;
        }

        private static string Whole(float v) { return Mathf.RoundToInt(v).ToString(); }

        private static string Factor(float v) { return "x" + v.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture); }

        private static string Rise(string name, int from, int to)
        {
            return name + " " + from + UpgradeVerdictRulesH8S.To + to;
        }

        /// <summary>The longest body Body returns for two or fewer risen attributes (the banner holds about two lines).</summary>
        public const int LongFormMax = 150;
        /// <summary>The longest body Body returns with three or four risen attributes (the short form).</summary>
        public const int ShortFormMax = 110;

        /// <summary>
        /// The body, skill points first ("+1 skill point, 2 to spend in Pause > SKILLS."), then the attributes that rose: one
        /// full clause each for one or two ("Vigor 5 → 6: max health 270 → 284"), or the short form for three or four
        /// ("Vigor +2, Endurance +1, Might +1.", also used when the full clauses would pass LongFormMax), so the banner never
        /// runs past its two lines. pointsToSpend below 0 leaves the count out.
        /// </summary>
        public static string Body(LevelSnapshotH8S a, LevelSnapshotH8S b, int pointsToSpend)
        {
            if (!Rose(a, b)) return "";
            int points = b.Level - a.Level;
            string text = "+" + points + " skill point" + (points == 1 ? "" : "s");
            if (pointsToSpend >= 0) text += ", " + pointsToSpend + " to spend in Pause > SKILLS";
            text += ".";
            int risen = (b.Vigor > a.Vigor ? 1 : 0) + (b.Endurance > a.Endurance ? 1 : 0) + (b.Might > a.Might ? 1 : 0) + (b.Focus > a.Focus ? 1 : 0);
            if (risen == 0) return text;
            string shortForm = "";
            if (b.Vigor > a.Vigor) shortForm = Short(shortForm, "Vigor", b.Vigor - a.Vigor);
            if (b.Endurance > a.Endurance) shortForm = Short(shortForm, "Endurance", b.Endurance - a.Endurance);
            if (b.Might > a.Might) shortForm = Short(shortForm, "Might", b.Might - a.Might);
            if (b.Focus > a.Focus) shortForm = Short(shortForm, "Focus", b.Focus - a.Focus);
            shortForm = text + " " + shortForm + ".";
            if (risen > 2) return shortForm;
            string full = "";
            if (b.Vigor > a.Vigor) full = Join(full, Rise("Vigor", a.Vigor, b.Vigor) + ": max health " + Whole(a.MaxHealth) + UpgradeVerdictRulesH8S.To + Whole(b.MaxHealth));
            if (b.Endurance > a.Endurance) full = Join(full, Rise("Endurance", a.Endurance, b.Endurance) + ": max stamina " + Whole(a.MaxStamina) + UpgradeVerdictRulesH8S.To + Whole(b.MaxStamina));
            if (b.Might > a.Might) full = Join(full, Rise("Might", a.Might, b.Might) + ": melee " + Factor(a.Melee) + UpgradeVerdictRulesH8S.To + Factor(b.Melee) + ", guns " + Factor(a.Gun) + UpgradeVerdictRulesH8S.To + Factor(b.Gun));
            if (b.Focus > a.Focus) full = Join(full, Rise("Focus", a.Focus, b.Focus) + ": max energy " + Whole(a.MaxEnergy) + UpgradeVerdictRulesH8S.To + Whole(b.MaxEnergy) + ", powers " + Factor(a.Power) + UpgradeVerdictRulesH8S.To + Factor(b.Power));
            full = text + " " + full + ".";
            // Two long clauses with big numbers can still overrun the banner: then the short form.
            return full.Length <= LongFormMax ? full : shortForm;
        }

        private static string Short(string text, string name, int by)
        {
            return (text.Length > 0 ? text + ", " : "") + name + " +" + by;
        }

        private static string Join(string text, string part)
        {
            return text.Length > 0 ? text + ". " + part : part;
        }
    }
}
