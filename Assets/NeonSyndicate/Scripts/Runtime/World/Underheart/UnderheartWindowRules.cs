using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// The descent window's effect on the Ash Reliquary (04E, "Down to the Crown"). EARLY: the Crown breathes low, so the
    /// reliquary's heat columns run cool, but its lamps are out (dim practical lights, dark ambient light and fog). LATE:
    /// the heat columns run hot, but the columns are lit (brighter lamps, the lit column bands and their lights). With no
    /// window, and everywhere outside the Ash Reliquary, everything stays at its normal level. Pure numbers with no engine
    /// calls, shared by the Underheart runtime and the EditMode tests. The window values are the descent_window choice
    /// values ("early", "late"), so a stored decision is a window id as it stands.
    /// </summary>
    public static class UnderheartWindowRules
    {
        public const string None = "";
        public const string Early = "early";
        public const string Late = "late";

        /// <summary>Heat per second of the reliquary's breathing heat columns with no window (they always breathe).</summary>
        public const float ColumnHeat = 12f;
        /// <summary>Seconds per breath cycle of the heat columns (each breathes for 55% of it, staggered into a ripple).</summary>
        public const float ColumnCycle = 5f;

        public const float EarlyHeatScale = 0.5f;
        public const float LateHeatScale = 1.5f;
        /// <summary>Practical lights in the reliquary: a lamp-out glow early, a little brighter late.</summary>
        public const float EarlyLightScale = 0.22f;
        public const float LateLightScale = 1.15f;
        /// <summary>The reliquary's ambient light and fog colour: dark early, brighter late.</summary>
        public const float EarlyAmbientScale = 0.4f;
        public const float LateAmbientScale = 1.2f;
        /// <summary>Heat-ring glow and vent particles: never dimmer than this, so a boundary still reads in the dark.</summary>
        public const float MinVentGlow = 0.85f;

        /// <summary>Burning damage per second while fire keeps the body alight (CharacterHealth ticks 2.6 every 0.25 s).</summary>
        public const float BurnPerSecond = 10.4f;
        /// <summary>Burning lingers this long after stepping out of underground heat (the Underheart caps it).</summary>
        public const float BurnAfter = 0.5f;
        /// <summary>The player's guaranteed health: the base before any gear (PlayerController sets 200).</summary>
        public const float GuaranteedHealth = 200f;

        /// <summary>True for "early" and "late".</summary>
        public static bool IsWindow(string window)
        {
            return window == Early || window == Late;
        }

        /// <summary>The window itself for "early" and "late", "" for anything else.</summary>
        public static string Normalize(string window)
        {
            return IsWindow(window) ? window : None;
        }

        /// <summary>True for the region a descent window changes (the Ash Reliquary only).</summary>
        public static bool AppliesTo(string region)
        {
            return region == UnderheartIds.AshReliquary;
        }

        /// <summary>The multiplier on damaging heat (lava and breathing vents) in a region under a window.</summary>
        public static float HeatScale(string window, string region)
        {
            if (!AppliesTo(region)) return 1f;
            switch (window)
            {
                case Early: return EarlyHeatScale;
                case Late: return LateHeatScale;
                default: return 1f;
            }
        }

        /// <summary>The multiplier on practical light intensity in a region (render group) under a window.</summary>
        public static float LightScale(string window, string region)
        {
            if (!AppliesTo(region)) return 1f;
            switch (window)
            {
                case Early: return EarlyLightScale;
                case Late: return LateLightScale;
                default: return 1f;
            }
        }

        /// <summary>The multiplier on the region's ambient light and fog colour under a window.</summary>
        public static float AmbientScale(string window, string region)
        {
            if (!AppliesTo(region)) return 1f;
            switch (window)
            {
                case Early: return EarlyAmbientScale;
                case Late: return LateAmbientScale;
                default: return 1f;
            }
        }

        /// <summary>Heat-ring glow and vent particle rate in a region: hotter reads hotter, but never below MinVentGlow.</summary>
        public static float VentGlowScale(string window, string region)
        {
            return Mathf.Max(MinVentGlow, HeatScale(window, region));
        }

        /// <summary>True when the reliquary's column bands and column lights are on (the late window).</summary>
        public static bool ColumnsLit(string window)
        {
            return window == Late;
        }

        /// <summary>A plain label for HUD, log and debug text.</summary>
        public static string Title(string window)
        {
            switch (window)
            {
                case Early: return "early window (cool heat columns, lamps out)";
                case Late: return "late window (hot heat columns, lit columns)";
                default: return "no window";
            }
        }

        /// <summary>
        /// Damage from standing in heat for secondsInside: nothing during the grace, then the heat plus burning per second,
        /// plus the burning that lingers after stepping out. Pass a grace of 0 for a worst case that ignores the grace.
        /// </summary>
        public static float ExposureDamage(float heatPerSecond, float secondsInside, float graceSeconds)
        {
            float t = Mathf.Max(0f, secondsInside - Mathf.Max(0f, graceSeconds));
            if (t <= 0f || heatPerSecond <= 0f) return 0f;
            return t * (heatPerSecond + BurnPerSecond) + BurnAfter * BurnPerSecond;
        }
    }
}
