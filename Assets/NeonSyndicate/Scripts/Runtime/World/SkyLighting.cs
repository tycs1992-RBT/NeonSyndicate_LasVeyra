namespace NeonSyndicate
{
    /// <summary>
    /// The one reset and preset hub of the Phase 1 lighting systems (Game.ResetStatics calls ResetStatics; Settings.
    /// ApplyGraphics calls OnGraphicsApplied): the sun and sky in Atmosphere, the per-area reflection probes, the
    /// NeonPost volumetrics and the Underheart lamp shafts. Keeping it to one line in each caller keeps those files small.
    /// </summary>
    public static class SkyLighting
    {
        /// <summary>Clears every static the lighting systems keep, for Enter Play Mode without domain reload.</summary>
        public static void ResetStatics()
        {
            ReflectionProbes.ResetStatics();
            UnderheartLampShafts.ResetStatics();
            NeonPost.ResetVolumetricStatics();
            RealismTexturesH8S.ResetStatics();
        }

        /// <summary>The graphics preset changed: everything that cached a preset choice follows it.</summary>
        public static void OnGraphicsApplied()
        {
            if (Game.Atmo != null) Game.Atmo.OnGraphicsApplied();
            if (ReflectionProbes.Instance != null) ReflectionProbes.Instance.Invalidate();
            UnderheartLampShafts.ApplyPreset();
        }
    }
}
