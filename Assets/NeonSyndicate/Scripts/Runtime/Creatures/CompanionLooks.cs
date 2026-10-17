using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// The creature looks the three companions grow from. Each starts from an existing species look (so it shares the
    /// proven rig, gait and pose solver) and takes its own proportions, palette and anatomy:
    /// the Quartz Drakeling is the quartz drake made juvenile (a fifth of the size, a bigger head, a shorter neck
    /// and tail, stubby wings, the crystal regalia kept); the Ashshell Warden is a heavy low quadruped from the basilisk
    /// plan with ash-grey hide, smoky quartz and ember seams; the Stormskimmer is a small winged body with no forelegs,
    /// a long streamer tail, steel-blue hide and softly glowing cyan membranes. Evolution adds a little size and glow.
    /// CompanionSculpt then adds the companion-only geometry. Pure and deterministic.
    /// </summary>
    public static class CompanionLooks
    {
        /// <summary>The look for an archetype, appearance seed and evolution stage (0 base, 1 Harmonized, 2 Awakened).</summary>
        public static CreatureLook For(string archetype, int seed, int evolution)
        {
            int evo = Mathf.Clamp(evolution, 0, 2);
            switch (archetype)
            {
                case BondIds.AshWarden: return Warden(seed, evo);
                case BondIds.StormSkimmer: return Skimmer(seed, evo);
                default: return Drakeling(seed, evo);
            }
        }

        private static CreatureLook Drakeling(int seed, int evo)
        {
            CreatureLook k = CreatureLooks.For("quartz_drake", seed).Clone();
            float grow = 1f + 0.06f * evo;
            k.Scale = 0.2f * grow;
            k.HeadSize *= 1.32f;
            k.NeckLength *= 0.72f;
            k.TailLength *= 0.78f;
            k.TailSegments = 8;
            k.WingSpan *= 0.82f;
            k.Bulk *= 0.94f;
            k.LegLength *= 1.04f;
            k.Spines = 6 + (seed & 1);
            k.Horns = evo >= 2 ? 3 : 1;
            k.GlowStrength = 1.15f + 0.2f * evo;
            k.CrystalRegalia = true;
            return k;
        }

        private static CreatureLook Warden(int seed, int evo)
        {
            CreatureLook k = CreatureLooks.For("crystal_basilisk", seed).Clone();
            float grow = 1f + 0.06f * evo;
            k.Scale = 0.9f * grow;
            k.Bulk = 1.75f;
            k.Sprawl = 0.55f;
            k.HeadSize = 1.12f;
            k.NeckLength = 0.24f;
            k.TailLength = 0.95f;
            k.TailSegments = 6;
            k.LegLength = 1.06f;
            k.Horns = 0;
            k.Spines = 0;
            k.HeadStyle = 2;
            k.TailStyle = 0;
            k.Coat = 1;
            k.Plates = 2;
            k.Hide = new Vector2Int(6, 8);
            k.Belly = new Vector2Int(5, 8);
            k.Accent = new Vector2Int(6, 8);
            k.Membrane = k.Hide;
            k.Crystal = new Vector2Int(8, 9);
            k.Glow = new Vector2Int(8, 10);
            k.Claw = new Vector2Int(14, 9);
            k.Mouth = new Vector2Int(8, 10);
            k.GlowStrength = 1f + 0.25f * evo;
            return k;
        }

        private static CreatureLook Skimmer(int seed, int evo)
        {
            CreatureLook k = CreatureLooks.For("violet_wyvern", seed).Clone();
            float grow = 1f + 0.06f * evo;
            k.Plan = CreaturePlan.Winged;
            k.Forelegs = false;
            k.Scale = 0.38f * grow;
            k.WingSpan = 5.2f;
            k.BodyLength = 1.9f;
            k.ShoulderHeight = 0.95f;
            k.Bulk = 0.8f;
            k.NeckLength = 0.62f;
            k.HeadSize = 1.08f;
            k.TailLength = 2.4f;
            k.TailSegments = 9;
            k.Horns = 0;
            k.Spines = 0;
            k.HeadStyle = 3;
            k.TailStyle = 3;
            k.Coat = 1;
            k.Hide = new Vector2Int(10, 8);
            k.Belly = new Vector2Int(9, 8);
            k.Accent = new Vector2Int(2, 8);
            k.Membrane = new Vector2Int(14, 10);
            k.Crystal = new Vector2Int(0, 9);
            k.Glow = new Vector2Int(0, 10);
            k.Claw = new Vector2Int(13, 9);
            k.Mouth = new Vector2Int(14, 10);
            k.GlowStrength = 1.1f + 0.25f * evo;
            return k;
        }

        /// <summary>
        /// Build detail for a companion body (it is usually close to the camera): the warden and the skimmer use the hero
        /// level on High and above and the standard level otherwise. The drakeling's quartz regalia is already dense (about
        /// 10 000 vertices at the standard level), so it stays at standard, and at crowd level on Low.
        /// </summary>
        public static int DetailFor(string archetype, GraphicsPreset preset)
        {
            if (archetype == BondIds.QuartzGuide) return preset <= GraphicsPreset.Low ? 0 : 1;
            return preset >= GraphicsPreset.High ? 2 : 1;
        }
    }
}
