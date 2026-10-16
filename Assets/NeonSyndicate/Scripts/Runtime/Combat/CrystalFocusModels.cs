using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Procedural models for the six crystal foci and their world props (owner: models and audio). Origin at the grip,
    /// +Z along the emitter, +Y up, with a "Muzzle" child and, on two-handed foci, a "Foregrip" child.
    /// </summary>
    public static class CrystalFocusModels
    {
        public static void ResetStatics() { }

        /// <summary>The held model. Until the art pass lands this falls back to the ordinary caster mesh.</summary>
        public static GameObject Create(CrystalBehavior b, LootGun g, string name, Transform parent, out Transform muzzle)
        {
            return LootGunModels.CreateBase(g, name, parent, out muzzle);
        }
    }

    /// <summary>Synthesized crystal weapon audio (owner: models and audio).</summary>
    public static class CrystalSounds
    {
        public static void ResetStatics() { }
        public static void Init(Transform parent) { }
    }

    /// <summary>One reset hub for every static the models and audio pass adds (called from Game.ResetStatics).</summary>
    public static class CrystalArtStatics
    {
        public static void ResetStatics()
        {
            CrystalFocusModels.ResetStatics();
            CrystalSounds.ResetStatics();
        }
    }
}
