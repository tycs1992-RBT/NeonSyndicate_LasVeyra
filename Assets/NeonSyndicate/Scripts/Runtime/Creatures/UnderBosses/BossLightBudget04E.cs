namespace NeonSyndicate
{
    /// <summary>
    /// 04E: the realtime light budget of an Underheart boss fight (Docs/Underheart04E/ART_AND_PERFORMANCE.md). The
    /// region around the arena already keeps its nearest practical lights on (UnderheartWorld: 8, or 5 on Low, all
    /// shadowless). A fight adds at most one realtime light of its own: the key light on the part the fight teaches you
    /// to strike (the Guardian's valve, the Matron's ember heart, the Vaultbreaker's core, the Regent's regulator), which
    /// brightens while that weak point is open. It never casts shadows and uses the Auto render mode, so the graphics
    /// preset's pixel light count decides whether it costs a per-pixel pass. Every other glow of a fight (furnaces,
    /// vents, crowns, seams, telegraph rings) is an emissive material; the short telegraph and impact flashes come from
    /// the shared, fixed flash pool in Fx and are not counted here. No attack telegraph depends on a fight light.
    /// </summary>
    public static class BossLightBudget04E
    {
        /// <summary>Realtime lights one fight may create, on top of the region's practical lights.</summary>
        public const int MaxFightLights = 1;
        /// <summary>Shadow-casting lights a fight may create.</summary>
        public const int MaxShadowCastingLights = 0;
        /// <summary>The Underheart region's practical lights kept on near the player (UnderheartWorld.UpdateLights).</summary>
        public const int RegionLights = 8;
        public const int RegionLightsLow = 5;

        /// <summary>True while a fight that already created `created` lights may create another.</summary>
        public static bool MayCreate(int created)
        {
            return created < MaxFightLights;
        }

        /// <summary>The most realtime point lights on near an Underheart boss arena (region plus fight), by preset.</summary>
        public static int ArenaTotal(bool lowPreset)
        {
            return (lowPreset ? RegionLightsLow : RegionLights) + MaxFightLights;
        }
    }
}
