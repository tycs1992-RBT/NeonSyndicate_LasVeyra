using System;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Species presets for the bestiary. Each species has a fixed character (proportions, anatomy, coat, crystal and
    /// glow colours); the seed adds small individual differences: a few percent of size and build, the number of
    /// crystal spines, and which colour of its family the coat, crystal and glow take. Pure C# and deterministic (the
    /// same id and seed always give the same look, on any thread and in the offline preview).
    /// </summary>
    public static class CreatureLooks
    {
        /// <summary>Every species id For understands, in bestiary order.</summary>
        public static readonly string[] Species =
        {
            "neon_jackal", "frost_stalker", "crystal_basilisk", "violet_wyvern", "rime_griffin",
            "sun_griffin", "quartz_drake", "dune_wyrm", "hollow_shade", "glass_mite",
            // 04E Underheart (appended): five underground roles on their own bodies and the three residents. The
            // Extraction Sentinel is a humanoid creature and has no entry here.
            "ember_scavenger", "ash_burrower", "glasswing_ambusher", "furnace_caster", "repair_construct",
            "cinder_moth", "kiln_crab", "ember_hatchling"
        };

        /// <summary>The look for a species id (unknown ids fall back to the Neon Jackal) with seeded variation.</summary>
        public static CreatureLook For(string speciesId, int seed)
        {
            string id = speciesId ?? "";
            Rng r = new Rng(Hash(id) ^ (uint)(seed * 747796405 + 2891336453u));
            CreatureLook k = new CreatureLook();
            k.Seed = seed;
            switch (id)
            {
                case "frost_stalker": FrostStalker(k, ref r); break;
                case "crystal_basilisk": CrystalBasilisk(k, ref r); break;
                case "violet_wyvern": VioletWyvern(k, ref r); break;
                case "rime_griffin": Griffin(k, ref r, false); break;
                case "sun_griffin": Griffin(k, ref r, true); break;
                case "quartz_drake": QuartzDrake(k, ref r); break;
                case "dune_wyrm": DuneWyrm(k, ref r); break;
                case "hollow_shade": HollowShade(k, ref r); break;
                case "glass_mite": GlassMite(k, ref r); break;
                case "ember_scavenger": EmberScavenger(k, ref r); break;
                case "ash_burrower": AshBurrower(k, ref r); break;
                case "glasswing_ambusher": GlasswingAmbusher(k, ref r); break;
                case "furnace_caster": FurnaceCaster(k, ref r); break;
                case "repair_construct": RepairConstruct(k, ref r); break;
                case "cinder_moth": CinderMoth(k, ref r); break;
                case "kiln_crab": KilnCrab(k, ref r); break;
                case "ember_hatchling": EmberHatchling(k, ref r); break;
                default: NeonJackal(k, ref r); break;
            }
            return k;
        }

        // ================================================================== species

        /// <summary>Pack hunter of the night city: lean, long-legged, huge ears, a ridge of crystal spines and cyan or
        /// magenta eye slits and seams.</summary>
        private static void NeonJackal(CreatureLook k, ref Rng r)
        {
            bool magenta = r.Chance(0.5f);
            k.Plan = CreaturePlan.Quadruped;
            k.Scale = r.Range(0.93f, 1.07f);
            k.BodyLength = 1.3f;
            k.ShoulderHeight = 0.62f * r.Range(0.97f, 1.03f);
            k.Bulk = 0.84f * r.Range(0.95f, 1.06f);
            k.NeckLength = 0.3f;
            k.HeadSize = r.Range(0.96f, 1.04f);
            k.TailLength = 0.46f * r.Range(0.92f, 1.08f);
            k.TailSegments = 5;
            k.LegLength = r.Range(0.97f, 1.04f);
            k.Spines = 6 + r.Next(3);
            k.HeadStyle = 0;
            k.EarStyle = 1;
            k.TailStyle = 1;
            k.Coat = 0;
            k.Hide = new Vector2Int(r.Chance(0.6f) ? 0 : 1, 8);
            k.Belly = new Vector2Int(2, 8);
            k.Accent = new Vector2Int(2, 8);
            k.Membrane = k.Hide;
            k.Crystal = new Vector2Int(magenta ? 1 : 0, 9);
            k.Glow = new Vector2Int(magenta ? 1 : 0, 10);
            k.Claw = new Vector2Int(10, 9);
            k.Mouth = new Vector2Int(14, 9);
        }

        /// <summary>Big-cat predator of the snowfields: pale pelt with frost markings, icy crystal shoulder plates, a long
        /// tail and blue light.</summary>
        private static void FrostStalker(CreatureLook k, ref Rng r)
        {
            bool white = r.Chance(0.35f);
            k.Plan = CreaturePlan.Quadruped;
            k.Scale = r.Range(0.94f, 1.06f);
            k.BodyLength = 2.6f;
            k.ShoulderHeight = 0.98f * r.Range(0.97f, 1.03f);
            k.Bulk = 1.12f * r.Range(0.95f, 1.06f);
            k.NeckLength = 0.3f;
            k.HeadSize = r.Range(0.96f, 1.05f);
            k.TailLength = 1.7f * r.Range(0.93f, 1.07f);
            k.TailSegments = 8;
            k.LegLength = r.Range(0.97f, 1.03f);
            k.Spines = 2 + r.Next(2);
            k.HeadStyle = 1;
            k.EarStyle = 2;
            k.TailStyle = 0;
            k.Coat = 0;
            k.Plates = 1;
            k.Hide = new Vector2Int(3, 8);
            k.Belly = new Vector2Int(3, 8);
            k.Accent = new Vector2Int(4, 8);
            k.Membrane = k.Hide;
            k.Crystal = new Vector2Int(white ? 4 : 2, 9);
            k.Glow = new Vector2Int(white ? 4 : 2, 10);
            k.Claw = new Vector2Int(10, 9);
            k.Mouth = new Vector2Int(9, 10);
        }

        /// <summary>Low, heavy lizard of the Inner World: sprawling legs, an armoured back of crystal scutes, a crown of
        /// crystal horns around a glowing gem and a violet gaze.</summary>
        private static void CrystalBasilisk(CreatureLook k, ref Rng r)
        {
            k.Plan = CreaturePlan.Quadruped;
            k.Scale = r.Range(0.94f, 1.06f);
            k.BodyLength = 1.9f;
            k.ShoulderHeight = 0.55f * r.Range(0.97f, 1.04f);
            k.Bulk = 1.55f * r.Range(0.95f, 1.06f);
            k.NeckLength = 0.3f;
            k.HeadSize = r.Range(1.08f, 1.18f);
            k.TailLength = 1.35f * r.Range(0.92f, 1.08f);
            k.TailSegments = 6;
            k.LegLength = r.Range(0.96f, 1.04f);
            k.Sprawl = 1f;
            k.Horns = 2;
            k.Spines = 4 + r.Next(3);
            k.HeadStyle = 2;
            k.EarStyle = 0;
            k.TailStyle = 0;
            k.Coat = 1;
            k.Plates = 2;
            k.Hide = new Vector2Int(5, 8);
            k.Belly = new Vector2Int(6, 8);
            k.Accent = new Vector2Int(5, 8);
            k.Membrane = k.Hide;
            k.Crystal = new Vector2Int(r.Chance(0.7f) ? 3 : 8, 9);
            k.Glow = new Vector2Int(3, 10);
            k.Claw = new Vector2Int(10, 9);
            k.Mouth = new Vector2Int(9, 10);
        }

        /// <summary>Wings for arms and two strong hind legs, a long neck and tail ending in a crystal blade, violet
        /// membranes with glowing veins.</summary>
        private static void VioletWyvern(CreatureLook k, ref Rng r)
        {
            k.Plan = CreaturePlan.Winged;
            k.Forelegs = false;
            k.Scale = r.Range(0.94f, 1.06f);
            k.WingSpan = 5f * r.Range(0.96f, 1.04f);
            k.BodyLength = 2.0f;
            k.ShoulderHeight = 1.0f;
            k.Bulk = 0.95f * r.Range(0.95f, 1.05f);
            k.NeckLength = 0.85f * r.Range(0.95f, 1.05f);
            k.HeadSize = r.Range(0.96f, 1.04f);
            k.TailLength = 2.0f * r.Range(0.94f, 1.06f);
            k.TailSegments = 8;
            k.LegLength = r.Range(0.97f, 1.03f);
            k.Horns = 1;
            k.Spines = 8 + r.Next(3);
            k.HeadStyle = 3;
            k.TailStyle = 3;
            k.Coat = 1;
            k.Hide = new Vector2Int(7, 8);
            k.Belly = new Vector2Int(6, 8);
            k.Accent = new Vector2Int(7, 8);
            k.Membrane = new Vector2Int(8, 8);
            k.Crystal = new Vector2Int(3, 9);
            k.Glow = new Vector2Int(r.Chance(0.75f) ? 3 : 1, 10);
            k.Claw = new Vector2Int(10, 9);
            k.Mouth = new Vector2Int(9, 10);
        }

        /// <summary>Eagle head and feathered wings on a big-cat body. Rime: frost white and steel blue with ice light;
        /// Sun: desert gold and tawny with amber light and a feathered crest.</summary>
        private static void Griffin(CreatureLook k, ref Rng r, bool sun)
        {
            k.Plan = CreaturePlan.Winged;
            k.Forelegs = true;
            k.Scale = r.Range(0.94f, 1.06f);
            k.WingSpan = 6f * r.Range(0.96f, 1.04f);
            k.BodyLength = 2.4f;
            k.ShoulderHeight = 1.12f * r.Range(0.97f, 1.03f);
            k.Bulk = 1f * r.Range(0.95f, 1.05f);
            k.NeckLength = 0.45f;
            k.HeadSize = r.Range(0.96f, 1.04f);
            k.TailLength = 1.15f * r.Range(0.93f, 1.07f);
            k.TailSegments = 6;
            k.LegLength = r.Range(0.97f, 1.03f);
            k.Beak = true;
            k.Feathers = true;
            k.Crest = sun;
            k.Mane = sun;
            k.HeadStyle = 4;
            k.EarStyle = 3;
            k.TailStyle = 2;
            k.Coat = 2;
            k.Spines = 0;
            if (sun)
            {
                bool gold = r.Chance(0.5f);
                k.Hide = new Vector2Int(11, 8);
                k.Accent = new Vector2Int(12, 8);
                k.Membrane = new Vector2Int(11, 8);
                k.Crystal = new Vector2Int(gold ? 7 : 5, 9);
                k.Glow = new Vector2Int(gold ? 7 : 5, 10);
                k.Claw = new Vector2Int(12, 9);
            }
            else
            {
                bool white = r.Chance(0.4f);
                k.Hide = new Vector2Int(9, 8);
                k.Accent = new Vector2Int(10, 8);
                k.Membrane = new Vector2Int(9, 8);
                k.Crystal = new Vector2Int(white ? 4 : 2, 9);
                k.Glow = new Vector2Int(white ? 4 : 2, 10);
                k.Claw = new Vector2Int(13, 9);
            }
            k.Belly = k.Hide;
            k.Mouth = new Vector2Int(9, 10);
        }

        /// <summary>The desert's apex beast: a dragon with quartz plates and horns, rose membranes and amber or rose light.</summary>
        private static void QuartzDrake(CreatureLook k, ref Rng r)
        {
            bool rose = r.Chance(0.5f);
            k.Plan = CreaturePlan.Winged;
            k.Forelegs = true;
            k.Scale = r.Range(0.95f, 1.05f);
            k.WingSpan = 9f * r.Range(0.97f, 1.03f);
            k.BodyLength = 4.2f;
            k.ShoulderHeight = 1.62f * r.Range(0.97f, 1.03f);
            k.Bulk = 1.3f * r.Range(0.96f, 1.05f);
            k.NeckLength = 1.2f * r.Range(0.95f, 1.05f);
            k.HeadSize = r.Range(1.08f, 1.16f);
            k.TailLength = 2.9f * r.Range(0.95f, 1.05f);
            k.TailSegments = 9;
            k.LegLength = r.Range(0.97f, 1.03f);
            k.Horns = r.Chance(0.7f) ? 1 : 3;
            k.Spines = 10 + r.Next(3);
            k.HeadStyle = 3;
            k.TailStyle = 3;
            k.Coat = 1;
            k.Plates = 3;
            k.CrystalRegalia = true;
            k.Hide = new Vector2Int(13, 8);
            k.Belly = new Vector2Int(6, 8);
            k.Accent = new Vector2Int(13, 8);
            k.Membrane = new Vector2Int(14, 8);
            k.Crystal = new Vector2Int(rose ? 6 : 9, 9);
            k.Glow = new Vector2Int(rose ? 6 : 5, 10);
            k.Claw = new Vector2Int(10, 9);
            k.Mouth = new Vector2Int(8, 10);
        }

        /// <summary>A burrowing worm about 14 m long: segmented sand-gold armour rings, a ring maw of crystal teeth that
        /// opens like a flower, an ember glow inside.</summary>
        private static void DuneWyrm(CreatureLook k, ref Rng r)
        {
            k.Plan = CreaturePlan.Serpent;
            k.Scale = r.Range(0.94f, 1.06f);
            k.BodyLength = 14f;
            // For the serpent the shoulder height is the body's thickness.
            k.ShoulderHeight = 1.75f * r.Range(0.96f, 1.04f);
            k.Bulk = r.Range(0.96f, 1.04f);
            k.HeadSize = 1f;
            k.Segments = 16;
            k.HeadStyle = 5;
            k.Coat = 3;
            k.Spines = 0;
            k.Hide = new Vector2Int(15, 8);
            k.Belly = new Vector2Int(14, 9);
            k.Accent = new Vector2Int(14, 9);
            k.Membrane = k.Hide;
            k.Crystal = new Vector2Int(r.Chance(0.5f) ? 5 : 7, 9);
            k.Glow = new Vector2Int(8, 10);
            k.Claw = new Vector2Int(11, 9);
            k.Mouth = new Vector2Int(8, 10);
        }

        /// <summary>A floating core of smoke with one burning eye and trailing tendrils, violet and black.</summary>
        private static void HollowShade(CreatureLook k, ref Rng r)
        {
            k.Plan = CreaturePlan.Wisp;
            k.Scale = r.Range(0.92f, 1.08f);
            k.BodyLength = 1.2f;
            k.ShoulderHeight = 1.25f;
            k.Bulk = r.Range(0.95f, 1.05f);
            k.Tendrils = 6 + r.Next(2);
            k.HeadStyle = 6;
            k.Coat = 4;
            k.Hide = new Vector2Int(13, 10);
            k.Belly = new Vector2Int(13, 10);
            k.Accent = new Vector2Int(15, 10);
            k.Membrane = k.Hide;
            k.Crystal = new Vector2Int(8, 9);
            k.Glow = new Vector2Int(r.Chance(0.8f) ? 3 : 1, 10);
            k.Claw = new Vector2Int(15, 9);
            k.Mouth = new Vector2Int(13, 10);
        }

        /// <summary>A palm-sized crystal insect that comes in swarms; every mite takes its own crystal colour.</summary>
        private static void GlassMite(CreatureLook k, ref Rng r)
        {
            int[] crystals = { 0, 1, 2, 3, 9 };
            int[] glows = { 0, 1, 2, 3, 12 };
            int pick = r.Next(crystals.Length);
            k.Plan = CreaturePlan.Mite;
            k.Scale = r.Range(0.85f, 1.15f);
            k.BodyLength = 0.09f;
            k.ShoulderHeight = 0.035f;
            k.HeadStyle = 6;
            k.Coat = 5;
            k.Crystal = new Vector2Int(crystals[pick], 9);
            k.Hide = k.Crystal;
            k.Belly = k.Crystal;
            k.Accent = new Vector2Int(8, 9);
            k.Membrane = new Vector2Int(8, 9);
            k.Glow = new Vector2Int(glows[pick], 10);
            k.Claw = new Vector2Int(8, 9);
            k.Mouth = k.Glow;
        }

        // ================================================================== 04E Underheart roles and residents
        // Each of these is its own body plan and proportion set (not a recolor of the template it replaced in the 04E
        // foundation): the palette cells come from the shared character palette's creature rows (8 hides, 9 crystal and
        // hard parts, 10 glow), picked for the fire-crystal underground (rose-umber, ash grey, smoky quartz, amber, ember).

        /// <summary>Small pack harasser of the service shafts: low and hunched with no ears, a scaled hide, scavenged ash
        /// shoulder plates, a ridge of many short ember spines and a crystal blade on a long whip tail.</summary>
        private static void EmberScavenger(CreatureLook k, ref Rng r)
        {
            k.Plan = CreaturePlan.Quadruped;
            k.Scale = r.Range(0.93f, 1.06f);
            k.BodyLength = 1.05f;
            k.ShoulderHeight = 0.42f * r.Range(0.97f, 1.03f);
            k.Bulk = 0.8f * r.Range(0.95f, 1.06f);
            k.NeckLength = 0.24f;
            k.HeadSize = r.Range(1.02f, 1.1f);
            k.TailLength = 0.85f * r.Range(0.92f, 1.08f);
            k.TailSegments = 6;
            k.LegLength = r.Range(0.9f, 0.96f);
            k.Sprawl = 0.45f;
            k.Spines = 10 + r.Next(3);
            k.HeadStyle = 0;
            k.EarStyle = 0;
            k.TailStyle = 3;
            k.Coat = 1;
            k.Plates = 1;
            k.Hide = new Vector2Int(13, 8);
            k.Belly = new Vector2Int(6, 8);
            k.Accent = new Vector2Int(0, 8);
            k.Membrane = k.Hide;
            k.Crystal = new Vector2Int(r.Chance(0.7f) ? 5 : 7, 9);
            k.Glow = new Vector2Int(8, 10);
            k.Claw = new Vector2Int(10, 9);
            k.Mouth = new Vector2Int(9, 10);
        }

        /// <summary>Armoured digger: wide and very low on short sprawled legs, a shovel beak instead of jaws, smoky quartz
        /// plates over the back and flanks, a short stub tail and dim ember seams between the plates.</summary>
        private static void AshBurrower(CreatureLook k, ref Rng r)
        {
            k.Plan = CreaturePlan.Quadruped;
            k.Scale = r.Range(0.95f, 1.05f);
            k.BodyLength = 2.2f;
            k.ShoulderHeight = 0.62f * r.Range(0.97f, 1.03f);
            k.Bulk = 1.95f * r.Range(0.96f, 1.05f);
            k.NeckLength = 0.16f;
            k.HeadSize = r.Range(1.18f, 1.26f);
            k.TailLength = 0.42f * r.Range(0.92f, 1.08f);
            k.TailSegments = 4;
            k.LegLength = r.Range(0.8f, 0.86f);
            k.Sprawl = 1f;
            k.Beak = true;
            k.Spines = 3;
            k.HeadStyle = 4;
            k.EarStyle = 0;
            k.TailStyle = 0;
            k.Coat = 1;
            k.Plates = 3;
            k.Hide = new Vector2Int(2, 8);
            k.Belly = new Vector2Int(6, 8);
            k.Accent = new Vector2Int(5, 8);
            k.Membrane = k.Hide;
            k.Crystal = new Vector2Int(4, 9);
            k.Glow = new Vector2Int(r.Chance(0.6f) ? 8 : 5, 10);
            k.Claw = new Vector2Int(14, 9);
            k.Mouth = new Vector2Int(9, 10);
        }

        /// <summary>Ceiling ambusher: a light body on two hind legs, a short cat-like face with tall pointed ears, and very
        /// large wings for arms whose membranes are panes of clear living glass veined with white-hot light.</summary>
        private static void GlasswingAmbusher(CreatureLook k, ref Rng r)
        {
            k.Plan = CreaturePlan.Winged;
            k.Forelegs = false;
            k.Scale = r.Range(0.94f, 1.06f);
            k.WingSpan = 4.4f * r.Range(0.96f, 1.04f);
            k.BodyLength = 1.15f;
            k.ShoulderHeight = 0.72f;
            k.Bulk = 0.68f * r.Range(0.95f, 1.05f);
            k.NeckLength = 0.5f * r.Range(0.95f, 1.05f);
            k.HeadSize = r.Range(0.92f, 0.98f);
            k.TailLength = 1.45f * r.Range(0.94f, 1.06f);
            k.TailSegments = 8;
            k.LegLength = r.Range(1.02f, 1.08f);
            k.Spines = 4 + r.Next(2);
            k.HeadStyle = 1;
            k.EarStyle = 1;
            k.TailStyle = 3;
            k.Coat = 1;
            k.Hide = new Vector2Int(5, 8);
            k.Belly = new Vector2Int(2, 8);
            k.Accent = new Vector2Int(5, 8);
            k.Membrane = new Vector2Int(9, 9);
            k.Crystal = new Vector2Int(9, 9);
            k.Glow = new Vector2Int(r.Chance(0.75f) ? 12 : 4, 10);
            k.Claw = new Vector2Int(15, 9);
            k.Mouth = new Vector2Int(12, 10);
        }

        /// <summary>Kiln-backed caster: a hunched, heavy body on short stubby legs with a high humped back, citrine
        /// chimney spines, a short horned face and a white-hot throat it lobs its bolts from. The furnace crucible and the
        /// two chimney stacks on its back are runtime pieces (UnderheartAccessories04E) that brighten before every throw.</summary>
        private static void FurnaceCaster(CreatureLook k, ref Rng r)
        {
            k.Plan = CreaturePlan.Quadruped;
            k.Scale = r.Range(0.95f, 1.05f);
            k.BodyLength = 1.25f;
            k.ShoulderHeight = 0.92f * r.Range(0.97f, 1.03f);
            k.Bulk = 1.65f * r.Range(0.96f, 1.05f);
            k.NeckLength = 0.18f;
            k.HeadSize = r.Range(0.9f, 0.96f);
            k.TailLength = 0.35f * r.Range(0.92f, 1.08f);
            k.TailSegments = 3;
            k.LegLength = r.Range(0.72f, 0.78f);
            k.Horns = 1;
            k.Spines = 7 + r.Next(2);
            k.HeadStyle = 1;
            k.EarStyle = 0;
            k.TailStyle = 0;
            k.Coat = 1;
            k.Plates = 1;
            k.Hide = new Vector2Int(14, 9);
            k.Belly = new Vector2Int(13, 8);
            k.Accent = new Vector2Int(14, 9);
            k.Membrane = k.Hide;
            k.Crystal = new Vector2Int(7, 9);
            k.Glow = new Vector2Int(8, 10);
            k.Claw = new Vector2Int(10, 9);
            k.Mouth = new Vector2Int(12, 10);
        }

        /// <summary>Repair and cooling construct: a small round steel-blue shell carried high on long spindly legs like a
        /// tick, a short sensor face, no tail to speak of and cyan cooling light (its weld arm and coolant dome are runtime
        /// pieces).</summary>
        private static void RepairConstruct(CreatureLook k, ref Rng r)
        {
            k.Plan = CreaturePlan.Quadruped;
            k.Scale = r.Range(0.95f, 1.05f);
            k.BodyLength = 0.55f;
            k.ShoulderHeight = 0.84f * r.Range(0.98f, 1.02f);
            k.Bulk = 0.78f * r.Range(0.96f, 1.04f);
            k.NeckLength = 0.06f;
            k.HeadSize = r.Range(0.7f, 0.76f);
            k.TailLength = 0.12f;
            k.TailSegments = 3;
            k.LegLength = r.Range(1.48f, 1.54f);
            k.Sprawl = 0.75f;
            k.Spines = 0;
            k.HeadStyle = 1;
            k.EarStyle = 0;
            k.TailStyle = 0;
            k.Coat = 1;
            k.Plates = 2;
            k.Hide = new Vector2Int(13, 9);
            k.Belly = new Vector2Int(8, 9);
            k.Accent = new Vector2Int(13, 9);
            k.Membrane = k.Hide;
            k.Crystal = new Vector2Int(0, 9);
            k.Glow = new Vector2Int(r.Chance(0.6f) ? 0 : 14, 10);
            k.Claw = new Vector2Int(13, 9);
            k.Mouth = new Vector2Int(14, 10);
        }

        /// <summary>Harmless cinder moth: a palm-sized flyer with broad ash-grey wings veined in ember and a fuzzy head.</summary>
        private static void CinderMoth(CreatureLook k, ref Rng r)
        {
            k.Plan = CreaturePlan.Winged;
            k.Forelegs = false;
            k.Scale = 0.3f * r.Range(0.9f, 1.1f);
            k.WingSpan = 1.7f * r.Range(0.95f, 1.05f);
            k.BodyLength = 0.55f;
            k.ShoulderHeight = 0.32f;
            k.Bulk = 0.9f;
            k.NeckLength = 0.08f;
            k.HeadSize = 1.25f;
            k.TailLength = 0.35f;
            k.TailSegments = 3;
            k.LegLength = 0.8f;
            k.Spines = 0;
            k.HeadStyle = 1;
            k.EarStyle = 3;
            k.TailStyle = 0;
            k.Coat = 0;
            k.Hide = new Vector2Int(12, 8);
            k.Belly = new Vector2Int(6, 8);
            k.Accent = new Vector2Int(12, 8);
            k.Membrane = new Vector2Int(6, 8);
            k.Crystal = new Vector2Int(5, 9);
            k.Glow = new Vector2Int(r.Chance(0.5f) ? 8 : 5, 10);
            k.Claw = new Vector2Int(14, 9);
            k.Mouth = new Vector2Int(9, 10);
        }

        /// <summary>Kiln crab: a squat, very wide and short shell on sprawled legs, a blunt face with a crown of amber
        /// points and kiln crystals on its back (the two pincers are runtime pieces on its front legs).</summary>
        private static void KilnCrab(CreatureLook k, ref Rng r)
        {
            k.Plan = CreaturePlan.Quadruped;
            k.Scale = r.Range(0.92f, 1.08f);
            k.BodyLength = 0.58f;
            k.ShoulderHeight = 0.3f * r.Range(0.97f, 1.03f);
            k.Bulk = 2.6f * r.Range(0.95f, 1.05f);
            k.NeckLength = 0.04f;
            k.HeadSize = r.Range(0.8f, 0.86f);
            k.TailLength = 0.1f;
            k.TailSegments = 3;
            k.LegLength = r.Range(0.82f, 0.88f);
            k.Sprawl = 1f;
            k.Horns = 2;
            k.Spines = 4 + r.Next(2);
            k.HeadStyle = 1;
            k.EarStyle = 0;
            k.TailStyle = 0;
            k.Coat = 1;
            k.Plates = 2;
            k.Hide = new Vector2Int(13, 8);
            k.Belly = new Vector2Int(12, 8);
            k.Accent = new Vector2Int(13, 8);
            k.Membrane = k.Hide;
            k.Crystal = new Vector2Int(5, 9);
            k.Glow = new Vector2Int(8, 10);
            k.Claw = new Vector2Int(14, 9);
            k.Mouth = new Vector2Int(9, 10);
        }

        /// <summary>Ember hatchling: a fluffy juvenile with an oversized head, a short round face, small round ears, a
        /// bushy tail and warm amber light (it grows into nobody knows what).</summary>
        private static void EmberHatchling(CreatureLook k, ref Rng r)
        {
            k.Plan = CreaturePlan.Quadruped;
            k.Scale = 0.42f * r.Range(0.92f, 1.08f);
            k.BodyLength = 0.95f;
            k.ShoulderHeight = 0.5f;
            k.Bulk = 1.05f * r.Range(0.95f, 1.05f);
            k.NeckLength = 0.24f;
            k.HeadSize = r.Range(1.32f, 1.4f);
            k.TailLength = 0.7f * r.Range(0.92f, 1.08f);
            k.TailSegments = 5;
            k.LegLength = r.Range(0.95f, 1.0f);
            k.Spines = 2;
            k.HeadStyle = 1;
            k.EarStyle = 2;
            k.TailStyle = 1;
            k.Coat = 0;
            k.Hide = new Vector2Int(12, 8);
            k.Belly = new Vector2Int(11, 8);
            k.Accent = new Vector2Int(13, 8);
            k.Membrane = k.Hide;
            k.Crystal = new Vector2Int(5, 9);
            k.Glow = new Vector2Int(5, 10);
            k.Claw = new Vector2Int(14, 9);
            k.Mouth = new Vector2Int(9, 10);
        }

        // ================================================================== randomness

        /// <summary>FNV-1a: a string hash that is the same in every runtime (string.GetHashCode is randomised per process
        /// in some runtimes, which would make looks differ between the game and the preview).</summary>
        private static uint Hash(string s)
        {
            uint h = 2166136261u;
            for (int i = 0; i < s.Length; i++)
            {
                h ^= s[i];
                h *= 16777619u;
            }
            return h;
        }

        /// <summary>A tiny deterministic generator (PCG-style hash steps), allocation free.</summary>
        private struct Rng
        {
            private uint _s;

            public Rng(uint seed)
            {
                _s = seed == 0u ? 0x9E3779B9u : seed;
                NextUInt();
            }

            public uint NextUInt()
            {
                _s = _s * 747796405u + 2891336453u;
                uint w = ((_s >> (int)((_s >> 28) + 4u)) ^ _s) * 277803737u;
                return (w >> 22) ^ w;
            }

            public float Value() { return (NextUInt() & 0xFFFFFF) / 16777216f; }
            public float Range(float a, float b) { return a + (b - a) * Value(); }
            public int Next(int n) { return n <= 1 ? 0 : (int)(NextUInt() % (uint)n); }
            public bool Chance(float p) { return Value() < p; }
        }
    }
}
