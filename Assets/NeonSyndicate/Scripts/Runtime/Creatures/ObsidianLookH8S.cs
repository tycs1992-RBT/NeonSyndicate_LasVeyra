using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Turns a species look into the H8S creature look: a dark obsidian, basalt or chitin body, crystal shard clusters in
    /// the region's family (CrystalFamilyH8S), glow only in the eyes, cores and seams, and the surface relief family of
    /// the creature material. Applied by the CreatureDirector when it orders a body (never to a companion), and by the
    /// offline preview; the F2 Creatures page can switch new spawns back to the classic look for a side by side. Pure
    /// except for that one flag; the same species and seed always give the same look.
    /// </summary>
    public static class ObsidianLookH8S
    {
        /// <summary>When set, new wild creatures spawn with their pre-H8S look (F2 Creatures page); saves never record it.</summary>
        public static bool Classic;

        public static void ResetStatics()
        {
            Classic = false;
        }

        /// <summary>The species look with the H8S treatment applied (or the classic look while Classic is on).</summary>
        public static CreatureLook For(string speciesId, int seed)
        {
            CreatureLook k = CreatureLooks.For(speciesId, seed);
            if (!Classic) Apply(k, speciesId);
            return k;
        }

        /// <summary>The crystal family of a species: by its region and role (see the reference pack's rules).</summary>
        public static CrystalFamilyH8S Family(string speciesId)
        {
            switch (speciesId)
            {
                case "neon_jackal": return CrystalFamilyH8S.Azure;
                case "sun_griffin": return CrystalFamilyH8S.Azure;
                case "repair_construct": return CrystalFamilyH8S.Azure;
                case "glass_mite": return CrystalFamilyH8S.Amethyst;
                case "crystal_basilisk": return CrystalFamilyH8S.Amethyst;
                case "ash_burrower": return CrystalFamilyH8S.Amethyst;
                case "hollow_shade": return CrystalFamilyH8S.Veil;
                case "violet_wyvern": return CrystalFamilyH8S.Veil;
                case "glasswing_ambusher": return CrystalFamilyH8S.Veil;
                case "frost_stalker": return CrystalFamilyH8S.Rime;
                case "rime_griffin": return CrystalFamilyH8S.Rime;
                case "dune_wyrm": return CrystalFamilyH8S.Ember;
                case "quartz_drake": return CrystalFamilyH8S.Ember;
                case "ember_scavenger": return CrystalFamilyH8S.Ember;
                case "furnace_caster": return CrystalFamilyH8S.Ember;
                case "cinder_moth": return CrystalFamilyH8S.Ember;
                case "kiln_crab": return CrystalFamilyH8S.Ember;
                case "ember_hatchling": return CrystalFamilyH8S.Ember;
                default: return CrystalFamilyH8S.Azure;
            }
        }

        /// <summary>The surface relief family (CreatureHideH8S) of a species; 0 keeps the shared character material.</summary>
        public static int Surface(string speciesId)
        {
            switch (speciesId)
            {
                case "neon_jackal": return CreatureHideH8S.Basalt;
                case "frost_stalker": return CreatureHideH8S.Scales;
                case "crystal_basilisk": return CreatureHideH8S.ChitinPlates;
                case "violet_wyvern": return CreatureHideH8S.Scales;
                case "rime_griffin": return CreatureHideH8S.Scales;
                case "sun_griffin": return CreatureHideH8S.Scales;
                case "dune_wyrm": return CreatureHideH8S.ChitinPlates;
                case "ember_scavenger": return CreatureHideH8S.Scales;
                case "ash_burrower": return CreatureHideH8S.ChitinPlates;
                case "glasswing_ambusher": return CreatureHideH8S.Scales;
                case "furnace_caster": return CreatureHideH8S.Basalt;
                case "repair_construct": return CreatureHideH8S.Basalt;
                case "kiln_crab": return CreatureHideH8S.ChitinPlates;
                default: return CreatureHideH8S.None;
            }
        }

        /// <summary>How rich a species' shard clusters are (0 to 1).</summary>
        public static float Richness(string speciesId)
        {
            switch (speciesId)
            {
                case "crystal_basilisk": return 0.85f;
                case "quartz_drake": return 0.9f;
                case "ash_burrower": return 0.8f;
                case "dune_wyrm": return 0.7f;
                case "furnace_caster": return 0.7f;
                case "glass_mite": return 1f;
                case "neon_jackal": return 0.6f;
                case "violet_wyvern": return 0.6f;
                case "ember_scavenger": return 0.6f;
                case "kiln_crab": return 0.6f;
                case "frost_stalker": return 0.55f;
                case "rime_griffin": return 0.5f;
                case "sun_griffin": return 0.5f;
                case "glasswing_ambusher": return 0.5f;
                case "hollow_shade": return 0.5f;
                case "repair_construct": return 0.35f;
                case "cinder_moth": return 0.3f;
                case "ember_hatchling": return 0.3f;
                default: return 0.5f;
            }
        }

        /// <summary>
        /// Applies the H8S look to a species look in place: flags, the surface family and the palette cells. Proportions,
        /// plan, head and tail styles stay the species' own, so the rig and the hit volumes are untouched; the uniform
        /// crystal combs and slabs are switched off because the builder grows clusters in their place.
        /// </summary>
        public static void Apply(CreatureLook k, string speciesId)
        {
            if (k == null) return;
            CrystalFamilyH8S f = Family(speciesId);
            k.ObsidianH8S = true;
            k.FamilyH8S = f;
            k.SurfaceH8S = Surface(speciesId);
            k.ClusterH8S = Richness(speciesId);
            k.BackFreeH8S = speciesId == "furnace_caster" || speciesId == "repair_construct";
            k.WardLinesH8S = speciesId == "crystal_basilisk" || speciesId == "ash_burrower" || speciesId == "kiln_crab" || speciesId == "quartz_drake";
            Vector2Int body = ObsidianPaletteH8S.BodyCell(ObsidianPaletteH8S.RegionalBody(f));
            Vector2Int under = ObsidianPaletteH8S.BodyCell(ObsidianPaletteH8S.UnderbellyDark);
            Vector2Int plate = ObsidianPaletteH8S.BodyCell(ObsidianPaletteH8S.ChitinPlate);
            Vector2Int dark = ObsidianPaletteH8S.BodyCell(ObsidianPaletteH8S.ChitinDark);
            Vector2Int crystalMid = ObsidianPaletteH8S.CrystalCell(f, ObsidianPaletteH8S.Mid);
            k.Hide = body;
            k.Belly = under;
            k.Accent = under;
            k.Membrane = body;
            k.Crystal = crystalMid;
            k.Glow = ObsidianPaletteH8S.GlowCell(f);
            k.Claw = ObsidianPaletteH8S.HornCell;
            k.Mouth = ObsidianPaletteH8S.MouthDarkCell;
            switch (speciesId)
            {
                case "frost_stalker":
                    // Rime dusted obsidian with dark basalt stripes.
                    k.Accent = ObsidianPaletteH8S.BodyCell(ObsidianPaletteH8S.Basalt);
                    k.Belly = ObsidianPaletteH8S.BodyCell(ObsidianPaletteH8S.AshHide);
                    break;
                case "crystal_basilisk":
                case "ash_burrower":
                case "kiln_crab":
                    k.Belly = plate;
                    k.Accent = plate;
                    break;
                case "rime_griffin":
                case "sun_griffin":
                    // Dark feathers over an obsidian lion body; flight feathers dark with pale edges.
                    k.Hide = ObsidianPaletteH8S.BodyCell(ObsidianPaletteH8S.FeatherDark);
                    k.Accent = body;
                    k.Membrane = ObsidianPaletteH8S.BodyCell(ObsidianPaletteH8S.FeatherDark);
                    k.Belly = ObsidianPaletteH8S.BodyCell(ObsidianPaletteH8S.FeatherDark);
                    k.Claw = ObsidianPaletteH8S.HornCell;
                    break;
                case "violet_wyvern":
                case "cinder_moth":
                    k.Membrane = ObsidianPaletteH8S.BodyCell(ObsidianPaletteH8S.MembraneDark);
                    break;
                case "quartz_drake":
                    k.Membrane = ObsidianPaletteH8S.BodyCell(ObsidianPaletteH8S.MembraneDark);
                    k.Belly = plate;
                    k.Mouth = ObsidianPaletteH8S.EmberThroatCell;
                    break;
                case "glasswing_ambusher":
                    // Panes of dark living glass for wings.
                    k.Membrane = ObsidianPaletteH8S.CrystalCell(f, ObsidianPaletteH8S.Deep);
                    break;
                case "dune_wyrm":
                    k.Hide = ObsidianPaletteH8S.BodyCell(ObsidianPaletteH8S.EmberChitin);
                    k.Belly = plate;
                    k.Accent = dark;
                    k.Mouth = ObsidianPaletteH8S.EmberThroatCell;
                    break;
                case "ember_scavenger":
                case "furnace_caster":
                    k.Mouth = ObsidianPaletteH8S.EmberThroatCell;
                    break;
                case "hollow_shade":
                    // Smoke stays smoke: the Veil's near-black body with violet splinters and a violet eye.
                    k.Hide = ObsidianPaletteH8S.BodyCell(ObsidianPaletteH8S.VeilObsidian);
                    k.Belly = k.Hide;
                    k.Accent = ObsidianPaletteH8S.BodyCell(ObsidianPaletteH8S.UnderbellyDark);
                    k.Membrane = k.Hide;
                    k.Mouth = k.Hide;
                    break;
                case "glass_mite":
                    // A finite mineral cluster on legs: amethyst body, dark chitin head and legs, pale glass wings.
                    k.Hide = crystalMid;
                    k.Belly = ObsidianPaletteH8S.CrystalCell(f, ObsidianPaletteH8S.Deep);
                    k.Accent = dark;
                    k.Membrane = ObsidianPaletteH8S.CrystalCell(f, ObsidianPaletteH8S.Light);
                    k.Mouth = k.Glow;
                    break;
                case "repair_construct":
                    k.Hide = ObsidianPaletteH8S.BodyCell(ObsidianPaletteH8S.Basalt);
                    k.Accent = k.Hide;
                    k.Belly = dark;
                    break;
                case "ember_hatchling":
                    k.Hide = ObsidianPaletteH8S.BodyCell(ObsidianPaletteH8S.EmberChitin);
                    k.Belly = ObsidianPaletteH8S.BodyCell(ObsidianPaletteH8S.AshHide);
                    k.Accent = under;
                    break;
            }
            // The builder grows shard clusters in place of the old uniform combs and shoulder slabs; the drake keeps its
            // quartz plates (its regalia is built on them) and the lizards keep their crown horns.
            k.Spines = 0;
            if (speciesId != "quartz_drake") k.Plates = 0;
        }

        /// <summary>The family of a boss species for its regalia (ember only for fire bosses and boss variants).</summary>
        public static CrystalFamilyH8S BossFamily(string bossId)
        {
            switch (bossId)
            {
                case "glacier_titan": return CrystalFamilyH8S.Rime;
                case "wraith_queen": return CrystalFamilyH8S.Veil;
                case "mirage_djinn": return CrystalFamilyH8S.Ember;
                case "golem_foreman": return CrystalFamilyH8S.Ember;
                case "cinder_matron": return CrystalFamilyH8S.Ember;
                default: return CrystalFamilyH8S.Amethyst;
            }
        }
    }
}
