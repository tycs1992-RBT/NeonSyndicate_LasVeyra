using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>How hot a crystal focus is, as the player needs to read it at a glance.</summary>
    public enum HeatBandH8S
    {
        /// <summary>Below WarmAt: nothing to think about.</summary>
        Cool,
        /// <summary>Building up.</summary>
        Warm,
        /// <summary>At or past HighAt: worth venting early (reload) before a forced vent.</summary>
        High,
        /// <summary>The next shot (or the next half second of beam) overheats the focus.</summary>
        Critical,
        /// <summary>A vent is running: the focus cannot fire.</summary>
        Venting
    }

    /// <summary>The one-shot cue a heat band change plays.</summary>
    public enum HeatCueH8S
    {
        /// <summary>No sound.</summary>
        None,
        /// <summary>Two soft rising ticks: heat ran high.</summary>
        Warn,
        /// <summary>Three sharp ticks: the next shot overheats.</summary>
        Critical,
        /// <summary>The cool release: the vent ended, the focus can fire again.</summary>
        Ready
    }

    /// <summary>
    /// H8S crystal focus heat readability rules (engine-free, tested offline). The heat itself stays exactly as the 04D
    /// heat book keeps it (CrystalHeat); this only reads it: a band (Cool, Warm, High, Critical, Venting) the HUD names
    /// in words beside a threshold tick on the existing heat bar, how many shots (or seconds of beam) are left before the
    /// overheat, and a single cue per crossing (into High, into Critical, out of a vent) so the player hears heat coming
    /// without looking. Focus balance is untouched.
    /// </summary>
    public static class FocusHeatRulesH8S
    {
        /// <summary>Heat from which the focus counts as warm.</summary>
        public const float WarmAt = 0.45f;
        /// <summary>Heat from which the focus counts as high (the tick on the heat bar sits here).</summary>
        public const float HighAt = 0.75f;
        /// <summary>For the beam (heat per second): critical when this much beam time is left.</summary>
        public const float BeamCriticalSeconds = 0.5f;

        /// <summary>
        /// The band of a focus: Venting while a vent runs; Critical when adding nextHeat would overheat it; then High,
        /// Warm or Cool by the heat. Broken numbers count as cool.
        /// </summary>
        public static HeatBandH8S Band(float heat, bool venting, float nextHeat)
        {
            if (venting) return HeatBandH8S.Venting;
            if (!(heat > 0f)) return HeatBandH8S.Cool;
            if (nextHeat > 0f && heat + nextHeat >= 1f) return HeatBandH8S.Critical;
            if (heat >= HighAt) return HeatBandH8S.High;
            if (heat >= WarmAt) return HeatBandH8S.Warm;
            return HeatBandH8S.Cool;
        }

        /// <summary>
        /// The heat the next trigger pull adds: the beam's heat per second over BeamCriticalSeconds, a ready charge its
        /// charged heat, any other shot its plain heat.
        /// </summary>
        public static float NextHeat(bool beam, bool chargeReady, float heat, float chargedHeat)
        {
            if (beam) return Mathf.Max(0f, heat) * BeamCriticalSeconds;
            return Mathf.Max(0f, chargeReady ? chargedHeat : heat);
        }

        /// <summary>Shots that can still be fired before the one that overheats (-1 when a shot adds no heat).</summary>
        public static int ShotsLeft(float heat, float perShot)
        {
            if (!(perShot > 0f)) return -1;
            float room = 1f - Mathf.Clamp01(heat) - 1e-4f;
            if (room <= 0f) return 0;
            return Mathf.FloorToInt(room / perShot);
        }

        /// <summary>Seconds of beam left before the overheat (-1 when the beam adds no heat).</summary>
        public static float BeamSecondsLeft(float heat, float perSecond)
        {
            if (!(perSecond > 0f)) return -1f;
            return Mathf.Max(0f, 1f - Mathf.Clamp01(heat)) / perSecond;
        }

        /// <summary>
        /// The cue for a band change: Ready when a vent ends; Critical when the next shot newly overheats; Warn when heat
        /// newly runs high from cool or warm. Cooling down, or staying in a band, plays nothing.
        /// </summary>
        public static HeatCueH8S Cue(HeatBandH8S from, HeatBandH8S to)
        {
            if (from == HeatBandH8S.Venting) return to == HeatBandH8S.Venting ? HeatCueH8S.None : HeatCueH8S.Ready;
            if (to == HeatBandH8S.Critical && from != HeatBandH8S.Critical) return HeatCueH8S.Critical;
            if (to == HeatBandH8S.High && (from == HeatBandH8S.Cool || from == HeatBandH8S.Warm)) return HeatCueH8S.Warn;
            return HeatCueH8S.None;
        }

        private const string HighLabel = "HEAT HIGH";
        private const string CriticalLabel = "NEXT SHOT OVERHEATS";
        private const string KeyVent = "[R]";
        private const string PadVent = "[D-pad Down]";

        /// <summary>The most shots the left count names exactly; more reads "12+ LEFT".</summary>
        public const int MaxShotsText = 12;

        private static readonly string[] ShotsTable = BuildShots();
        private static readonly string[] TenthsTable = BuildTenths();

        /// <summary>The words the HUD shows for a band (empty when there is nothing to say; the vent has its own label).</summary>
        public static string Label(HeatBandH8S band)
        {
            switch (band)
            {
                case HeatBandH8S.High: return HighLabel;
                case HeatBandH8S.Critical: return CriticalLabel;
                default: return "";
            }
        }

        /// <summary>
        /// The band's words with the vent button (reload: R, or a tap of the D-pad down on every pad family). Constant
        /// strings, so the HUD never builds one per frame. Empty for bands with nothing to say.
        /// </summary>
        public static string Prompt(HeatBandH8S band, bool pad)
        {
            switch (band)
            {
                case HeatBandH8S.High: return pad ? HighLabel + "   " + PadVent + " VENT EARLY" : HighLabel + "   " + KeyVent + " VENT EARLY";
                case HeatBandH8S.Critical: return pad ? CriticalLabel + "   " + PadVent + " VENT NOW" : CriticalLabel + "   " + KeyVent + " VENT NOW";
                default: return "";
            }
        }

        /// <summary>"3 LEFT" for shots left before the overheat (cached; empty for a negative count).</summary>
        public static string ShotsText(int shots)
        {
            if (shots < 0) return "";
            return ShotsTable[Mathf.Min(shots, MaxShotsText)];
        }

        /// <summary>"1.4 s" rounded up to the tenth (a countdown never shows 0.0 while time is left; cached up to 9.9 s).</summary>
        public static string SecondsText(float seconds)
        {
            if (!(seconds > 0f)) return TenthsTable[0];
            return TenthsTable[Mathf.Clamp(Mathf.CeilToInt(seconds * 10f - 1e-3f), 0, TenthsTable.Length - 1)];
        }

        private static string[] BuildShots()
        {
            string[] t = new string[MaxShotsText + 1];
            for (int i = 0; i < MaxShotsText; i++) t[i] = i + " LEFT";
            t[MaxShotsText] = MaxShotsText + "+ LEFT";
            return t;
        }

        private static string[] BuildTenths()
        {
            string[] t = new string[100];
            for (int i = 0; i < t.Length; i++) t[i] = (i / 10) + "." + (i % 10) + " s";
            return t;
        }
    }
}
