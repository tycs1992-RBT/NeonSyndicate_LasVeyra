using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>Which world tier a species belongs to (also its rough danger).</summary>
    public enum CreatureTier
    {
        OuterCity = 1,
        InnerWorld = 2,
        Frostline = 3,
        Badlands = 4
    }

    /// <summary>How a species behaves toward the player before a fight starts.</summary>
    public enum Temperament
    {
        /// <summary>Attacks on sight.</summary>
        Aggressive,
        /// <summary>Warns, then attacks if you come close or stay.</summary>
        Territorial,
        /// <summary>Hides and strikes from behind; often unseen until close (the Third Eye reveals it).</summary>
        Stalker,
        /// <summary>Many small bodies acting as one.</summary>
        Swarm,
        /// <summary>A great beast or boss that patrols its ground.</summary>
        Apex
    }

    /// <summary>Habitat tags the ecology spawner matches against the player's surroundings.</summary>
    public static class Habitat
    {
        public const string CityNight = "city_night";
        public const string Parks = "parks";
        public const string Construction = "construction";
        public const string Grave = "grave";
        public const string Inner = "inner";
        public const string Frost = "frost";
        public const string FrostLake = "frost_lake";
        public const string Badlands = "badlands";
        public const string Quarry = "quarry";
        public const string Canyon = "canyon";
        public const string Arena = "arena";
    }

    /// <summary>One loot roll: chance per kill and a count range.</summary>
    public struct LootRoll
    {
        public string Material;
        public float Chance;
        public int Min, Max;

        public LootRoll(string material, float chance, int min, int max)
        {
            Material = material;
            Chance = chance;
            Min = min;
            Max = max;
        }
    }

    /// <summary>
    /// A creature species: identity, body, habitat, first-pass combat numbers, loot and lore. CreatureSpeciesDb holds
    /// the bestiary; the creature runtime and the ecology spawner read it, quests and events refer to species by Id.
    /// </summary>
    public sealed class CreatureSpecies
    {
        public string Id;
        public string Name;
        public string Plural;
        public CreatureTier Tier;
        /// <summary>Non-humanoid body: CreatureLooks id ("neon_jackal"). Empty for humanoid creatures.</summary>
        public string LookId;
        /// <summary>Humanoid body on the people rig (Appearance.Creature); None for non-humanoid creatures.</summary>
        public HumanoidCreature Humanoid;
        public Temperament Temperament;
        public bool Boss;
        /// <summary>A great beast: boss-sized health bar but it roams like wildlife.</summary>
        public bool GreatBeast;
        public string[] Habitats = new string[0];
        public bool Night = true;
        public bool Day;
        public int PackMin = 1, PackMax = 1;
        public float Health = 100f;
        public float Poise = 40f;
        /// <summary>Base damage of its main attack (difficulty scaling is applied by the runtime).</summary>
        public float Damage = 12f;
        public float WalkSpeed = 1.6f;
        public float RunSpeed = 6f;
        /// <summary>Flight speed for flyers (0 = cannot fly).</summary>
        public float FlySpeed;
        /// <summary>Distance at which it notices the player (halved while crouched and still).</summary>
        public float Sense = 30f;
        public Element Weakness = Element.None;
        public Element Resists = Element.None;
        public int Lumen = 40;
        public LootRoll[] Loot = new LootRoll[0];
        public string Behaviour = "";
        public string Weak = "";
        public string Lore = "";
        // ---- Appended by the creature runtime (combat and look data). CreatureSpeciesDb fills these per id.
        /// <summary>Attack set of a non-humanoid species (see CreatureAttackDb); humanoid creatures use HumanoidMove.</summary>
        public CreatureAttack[] Attacks = new CreatureAttack[0];
        /// <summary>How far an ambient creature follows you from its home before it gives up and goes back (metres).</summary>
        public float Leash = 70f;
        /// <summary>Multiplier on the rest between attacks (above 1 is more patient).</summary>
        public float Tempo = 1f;
        /// <summary>Territorial creatures start their warning display inside this radius...</summary>
        public float WarnRadius = 18f;
        /// <summary>...and attack inside this one (or after the warning runs out).</summary>
        public float StrikeRadius = 7f;
        /// <summary>Flyers: cruising height above the ground while circling (metres).</summary>
        public float CruiseHeight = 14f;
        /// <summary>What a hit throws up: crystal sparks, shadow smoke or glass glints (never blood).</summary>
        public ImpactSurface HitSurface = ImpactSurface.Crystal;
        /// <summary>Tint for hit sparks, smoke and the death burst.</summary>
        public Color HitColor = new Color(0.4f, 0.9f, 1f);
        public CreatureDeath DeathStyle = CreatureDeath.Crystal;

        public bool IsHumanoid { get { return Humanoid != HumanoidCreature.None; } }
        public bool Flies { get { return FlySpeed > 0f; } }
    }

    /// <summary>The bestiary. Ids are stable: saves, quests, events and loot refer to them.</summary>
    public static class CreatureSpeciesDb
    {
        public static readonly CreatureSpecies[] All =
        {
            // ---------------------------------------------------------------- tier 1: the Outer City at night
            new CreatureSpecies
            {
                Id = "glass_mite", Name = "Glass Mite", Plural = "Glass Mites", Tier = CreatureTier.OuterCity, LookId = "glass_mite",
                Temperament = Temperament.Swarm, Habitats = new[] { Habitat.Construction, Habitat.CityNight, Habitat.Quarry },
                PackMin = 8, PackMax = 16, Health = 8f, Poise = 1f, Damage = 3f, WalkSpeed = 2f, RunSpeed = 7.5f, Sense = 22f,
                Weakness = Element.Ember, Lumen = 4,
                Loot = new[] { new LootRoll("glass_chitin", 0.35f, 1, 1), new LootRoll("mite_dust", 0.2f, 1, 2) },
                Behaviour = "Swarms out of scaffolds and quarries after dark and nibbles anything warm.",
                Weak = "Fire and area attacks scatter a whole swarm at once.",
                Lore = "Crystal insects born from the dust of cut shard. Construction crews leave lights on all night to keep them away."
            },
            new CreatureSpecies
            {
                Id = "neon_jackal", Name = "Neon Jackal", Plural = "Neon Jackals", Tier = CreatureTier.OuterCity, LookId = "neon_jackal",
                Temperament = Temperament.Aggressive, Habitats = new[] { Habitat.Parks, Habitat.CityNight, Habitat.Badlands },
                PackMin = 3, PackMax = 5, Health = 70f, Poise = 25f, Damage = 12f, WalkSpeed = 1.8f, RunSpeed = 9f, Sense = 34f,
                Weakness = Element.Frost, Lumen = 18,
                Loot = new[] { new LootRoll("jackal_fang", 0.7f, 1, 2), new LootRoll("neon_hide", 0.25f, 1, 1) },
                Behaviour = "Hunts in packs: one feints, the others flank. Howls call the rest.",
                Weak = "Break the pack leader first; the rest lose their nerve.",
                Lore = "Strays that drank from crystal runoff. Their seams glow brighter the hungrier they get."
            },
            new CreatureSpecies
            {
                Id = "shard_golem", Name = "Shard Golem", Plural = "Shard Golems", Tier = CreatureTier.OuterCity, Humanoid = HumanoidCreature.ShardGolem,
                Temperament = Temperament.Territorial, Habitats = new[] { Habitat.Construction, Habitat.Quarry },
                PackMin = 1, PackMax = 1, Health = 420f, Poise = 160f, Damage = 30f, WalkSpeed = 1.3f, RunSpeed = 3.6f, Sense = 22f,
                Weakness = Element.Volt, Resists = Element.Frost, Lumen = 90,
                Loot = new[] { new LootRoll("shard_chunk", 0.9f, 2, 4), new LootRoll("golem_core", 0.15f, 1, 1) },
                Behaviour = "Slow and patient, guards the ground it rose from. Slams shake the street.",
                Weak = "Dodge the slam, then hit the glowing chest core.",
                Lore = "Where shard is cut too deep, the stone remembers it was alive. Crews call them the foremen nobody hired."
            },
            new CreatureSpecies
            {
                Id = "hollow_shade", Name = "Hollow Shade", Plural = "Hollow Shades", Tier = CreatureTier.OuterCity, LookId = "hollow_shade",
                Temperament = Temperament.Stalker, Habitats = new[] { Habitat.Grave, Habitat.CityNight, Habitat.Inner },
                PackMin = 1, PackMax = 2, Health = 60f, Poise = 15f, Damage = 10f, WalkSpeed = 2.2f, RunSpeed = 6.5f, FlySpeed = 6f, Sense = 26f,
                Weakness = Element.Volt, Resists = Element.Void, Lumen = 30,
                Loot = new[] { new LootRoll("shade_essence", 0.6f, 1, 1), new LootRoll("hollow_essence", 0.4f, 1, 1) },
                Behaviour = "Drifts unseen until it is close; drains crystal energy with every touch.",
                Weak = "The Third Eye shows it plainly. Light and lightning tear it apart.",
                Lore = "A wisp of the Inner World that slipped through a crack. It follows anyone whose Third Eye is open."
            },
            // ---------------------------------------------------------------- tier 2: the Inner World
            new CreatureSpecies
            {
                Id = "hollow", Name = "Hollow", Plural = "Hollows", Tier = CreatureTier.InnerWorld, Humanoid = HumanoidCreature.Hollow,
                Temperament = Temperament.Aggressive, Habitats = new[] { Habitat.Inner, Habitat.Grave },
                PackMin = 2, PackMax = 4, Health = 110f, Poise = 40f, Damage = 16f, WalkSpeed = 1.6f, RunSpeed = 6.8f, Sense = 32f,
                Weakness = Element.Volt, Resists = Element.Void, Lumen = 35,
                Loot = new[] { new LootRoll("hollow_essence", 0.8f, 1, 2), new LootRoll("void_thread", 0.2f, 1, 1) },
                Behaviour = "Shadow stalkers that blink through the dark to strike from your blind side.",
                Weak = "Watch for the shimmer before a blink, then turn and parry.",
                Lore = "Hollows were people once, the Frequency Order says. Nobody asks them who."
            },
            new CreatureSpecies
            {
                Id = "mirror_wraith", Name = "Mirror Wraith", Plural = "Mirror Wraiths", Tier = CreatureTier.InnerWorld, Humanoid = HumanoidCreature.MirrorWraith,
                Temperament = Temperament.Stalker, Habitats = new[] { Habitat.Inner },
                PackMin = 1, PackMax = 1, Health = 220f, Poise = 70f, Damage = 20f, WalkSpeed = 1.5f, RunSpeed = 5.6f, Sense = 28f,
                Weakness = Element.Ember, Resists = Element.Void, Lumen = 70,
                Loot = new[] { new LootRoll("mirror_glass", 0.5f, 1, 1), new LootRoll("void_thread", 0.5f, 1, 2) },
                Behaviour = "Wears your shape and copies your guard. Frontal blows glance off its glass.",
                Weak = "Circle it or break the guard with a charged heavy.",
                Lore = "The Inner World remembers everyone who looks into it. Sometimes it sends the memory back."
            },
            new CreatureSpecies
            {
                Id = "crystal_basilisk", Name = "Crystal Basilisk", Plural = "Crystal Basilisks", Tier = CreatureTier.InnerWorld, LookId = "crystal_basilisk",
                Temperament = Temperament.Territorial, Habitats = new[] { Habitat.Inner, Habitat.Quarry },
                PackMin = 1, PackMax = 1, Health = 380f, Poise = 120f, Damage = 24f, WalkSpeed = 1.2f, RunSpeed = 5f, Sense = 24f,
                Weakness = Element.Frost, Lumen = 85,
                Loot = new[] { new LootRoll("basilisk_scale", 0.8f, 1, 3), new LootRoll("crown_gem", 0.2f, 1, 1) },
                Behaviour = "Its crown gaze slows anything that holds still in front of it. The tail sweeps wide.",
                Weak = "Keep moving out of the gaze, strike the crown gem.",
                Lore = "A lizard of the mirror city, slow as a bank vault and about as hard to open."
            },
            new CreatureSpecies
            {
                Id = "violet_wyvern", Name = "Violet Wyvern", Plural = "Violet Wyverns", Tier = CreatureTier.InnerWorld, LookId = "violet_wyvern",
                Temperament = Temperament.Aggressive, Habitats = new[] { Habitat.Inner },
                PackMin = 1, PackMax = 2, Health = 300f, Poise = 90f, Damage = 22f, WalkSpeed = 1.4f, RunSpeed = 5f, FlySpeed = 16f, Sense = 60f,
                Weakness = Element.Volt, Lumen = 80,
                Loot = new[] { new LootRoll("wyvern_membrane", 0.8f, 1, 2), new LootRoll("void_thread", 0.4f, 1, 1) },
                Behaviour = "Circles high over the mirror streets, dives, breathes violet fire, climbs away.",
                Weak = "Shoot it in the dive; a wing hit brings it to the ground.",
                Lore = "Wyverns nest on the floating stones above the Inner World. They hate the color of street lights."
            },
            // ---------------------------------------------------------------- tier 3: the Frostline
            new CreatureSpecies
            {
                Id = "frost_stalker", Name = "Frost Stalker", Plural = "Frost Stalkers", Tier = CreatureTier.Frostline, LookId = "frost_stalker",
                Temperament = Temperament.Stalker, Habitats = new[] { Habitat.Frost },
                PackMin = 1, PackMax = 2, Health = 260f, Poise = 80f, Damage = 24f, WalkSpeed = 1.7f, RunSpeed = 10f, Sense = 40f, Day = true,
                Weakness = Element.Ember, Resists = Element.Frost, Lumen = 75,
                Loot = new[] { new LootRoll("rime_pelt", 0.8f, 1, 2), new LootRoll("frost_fang", 0.4f, 1, 1) },
                Behaviour = "Tracks you through the snowfields, then pounces from the blind side.",
                Weak = "Its pounce leaves it open: dodge sideways and counter.",
                Lore = "Wardens tell new recruits: if you can see one, it already decided to let you."
            },
            new CreatureSpecies
            {
                Id = "rime_griffin", Name = "Rime Griffin", Plural = "Rime Griffins", Tier = CreatureTier.Frostline, LookId = "rime_griffin",
                Temperament = Temperament.Territorial, Habitats = new[] { Habitat.Frost, Habitat.FrostLake },
                PackMin = 1, PackMax = 1, Health = 340f, Poise = 100f, Damage = 26f, WalkSpeed = 1.6f, RunSpeed = 7f, FlySpeed = 18f, Sense = 45f, Day = true,
                Weakness = Element.Volt, Resists = Element.Frost, Lumen = 95,
                Loot = new[] { new LootRoll("rime_feather", 0.8f, 1, 3), new LootRoll("griffin_heart", 0.12f, 1, 1) },
                Behaviour = "Guards the frozen lake. Screams a warning, then dives talons first.",
                Weak = "Back off when it screams and it may let you go. Otherwise shoot it out of the dive.",
                Lore = "The Frost Wardens believe a griffin that trusts you will one day carry you. None has, yet."
            },
            new CreatureSpecies
            {
                Id = "snow_hollow", Name = "Snow Hollow", Plural = "Snow Hollows", Tier = CreatureTier.Frostline, Humanoid = HumanoidCreature.SnowHollow,
                Temperament = Temperament.Aggressive, Habitats = new[] { Habitat.Frost },
                PackMin = 2, PackMax = 3, Health = 140f, Poise = 50f, Damage = 18f, WalkSpeed = 1.5f, RunSpeed = 6.2f, Sense = 30f,
                Weakness = Element.Ember, Resists = Element.Frost, Lumen = 45,
                Loot = new[] { new LootRoll("frost_essence", 0.8f, 1, 2), new LootRoll("hollow_essence", 0.4f, 1, 1) },
                Behaviour = "Hollows that froze in the snowfields. Their touch chills your stamina.",
                Weak = "Fire thaws them mid-swing.",
                Lore = "Some Hollows wandered too far from the Veil and the cold kept them."
            },
            new CreatureSpecies
            {
                Id = "glacier_titan", Name = "Glacier Titan", Plural = "Glacier Titans", Tier = CreatureTier.Frostline, Humanoid = HumanoidCreature.GlacierTitan,
                Temperament = Temperament.Apex, Boss = true, Habitats = new[] { Habitat.Arena }, Day = true,
                PackMin = 1, PackMax = 1, Health = 4200f, Poise = 900f, Damage = 55f, WalkSpeed = 2.2f, RunSpeed = 5.5f, Sense = 60f,
                Weakness = Element.Ember, Resists = Element.Frost, Lumen = 1200,
                Loot = new[] { new LootRoll("titan_heart", 1f, 1, 1), new LootRoll("glacier_shard", 1f, 3, 5) },
                Behaviour = "Shakes the ground with every step, breathes frost and raises ice spikes. Calls Snow Hollows when hurt.",
                Weak = "Stay out of the spike circles, go for the knees, burn the core when it kneels.",
                Lore = "Older than the city. The Wardens' outpost was built to watch the hollow where it sleeps."
            },
            // ---------------------------------------------------------------- tier 4: the Quartz Badlands
            new CreatureSpecies
            {
                Id = "dune_wyrm", Name = "Dune Wyrm", Plural = "Dune Wyrms", Tier = CreatureTier.Badlands, LookId = "dune_wyrm",
                Temperament = Temperament.Apex, GreatBeast = true, Habitats = new[] { Habitat.Badlands }, Day = true,
                PackMin = 1, PackMax = 1, Health = 1800f, Poise = 500f, Damage = 40f, WalkSpeed = 4f, RunSpeed = 14f, Sense = 70f,
                Weakness = Element.Frost, Resists = Element.Ember, Lumen = 600,
                Loot = new[] { new LootRoll("wyrm_scale", 1f, 2, 4), new LootRoll("ember_gland", 0.5f, 1, 1) },
                Behaviour = "Hunts by vibration under the sand. Engines and gunfire draw it. Erupts beneath its prey.",
                Weak = "Stand still on rock and it loses you. Hit the throat core while it is surfaced.",
                Lore = "Truckers on the canyon road drive with the radio off. Nobody says why out loud."
            },
            new CreatureSpecies
            {
                Id = "quartz_drake", Name = "Quartz Drake", Plural = "Quartz Drakes", Tier = CreatureTier.Badlands, LookId = "quartz_drake",
                Temperament = Temperament.Apex, GreatBeast = true, Habitats = new[] { Habitat.Badlands, Habitat.Canyon }, Day = true,
                PackMin = 1, PackMax = 1, Health = 2400f, Poise = 600f, Damage = 45f, WalkSpeed = 2f, RunSpeed = 8f, FlySpeed = 22f, Sense = 90f,
                Weakness = Element.Frost, Resists = Element.Ember, Lumen = 900,
                Loot = new[] { new LootRoll("drake_plate", 1f, 2, 3), new LootRoll("quartz_heart", 0.35f, 1, 1) },
                Behaviour = "The desert's apex. Roosts on the canyon rim, strafes with ember breath, lands to fight up close.",
                Weak = "Its chest plates crack under frost. Bring a lot of it.",
                Lore = "Every gang in Las Veyra has a story about the drake. All of them end with somebody running."
            },
            new CreatureSpecies
            {
                Id = "sun_griffin", Name = "Sun Griffin", Plural = "Sun Griffins", Tier = CreatureTier.Badlands, LookId = "sun_griffin",
                Temperament = Temperament.Territorial, Habitats = new[] { Habitat.Badlands, Habitat.Canyon }, Day = true,
                PackMin = 1, PackMax = 2, Health = 320f, Poise = 95f, Damage = 24f, WalkSpeed = 1.6f, RunSpeed = 7f, FlySpeed = 18f, Sense = 45f,
                Weakness = Element.Volt, Resists = Element.Ember, Lumen = 90,
                Loot = new[] { new LootRoll("sun_feather", 0.8f, 1, 3), new LootRoll("griffin_heart", 0.12f, 1, 1) },
                Behaviour = "Hunts at noon with the sun behind it. Dives out of the glare.",
                Weak = "Fight with the sun at your back and it cannot hide in the light.",
                Lore = "The Quartz Kings paint its crest on their jackets. None of them has seen one land."
            },
            new CreatureSpecies
            {
                Id = "sand_golem", Name = "Sand Golem", Plural = "Sand Golems", Tier = CreatureTier.Badlands, Humanoid = HumanoidCreature.SandGolem,
                Temperament = Temperament.Territorial, Habitats = new[] { Habitat.Badlands, Habitat.Quarry }, Day = true,
                PackMin = 1, PackMax = 1, Health = 480f, Poise = 180f, Damage = 32f, WalkSpeed = 1.3f, RunSpeed = 3.6f, Sense = 22f,
                Weakness = Element.Frost, Lumen = 100,
                Loot = new[] { new LootRoll("sand_core", 0.6f, 1, 1), new LootRoll("shard_chunk", 0.7f, 1, 3) },
                Behaviour = "Rises out of the dunes when someone camps too long. Throws packed sand like cannon shot.",
                Weak = "Water and frost turn it to mud. The core sits behind the chest plate.",
                Lore = "Old trailer park residents leave a cup of water on the step at night. For the golems, they say."
            },
            new CreatureSpecies
            {
                Id = "mirage_djinn", Name = "Mirage Djinn", Plural = "Mirage Djinn", Tier = CreatureTier.Badlands, Humanoid = HumanoidCreature.MirageDjinn,
                Temperament = Temperament.Apex, Boss = true, Habitats = new[] { Habitat.Arena },
                PackMin = 1, PackMax = 1, Health = 3600f, Poise = 800f, Damage = 50f, WalkSpeed = 2f, RunSpeed = 6f, Sense = 70f,
                Weakness = Element.Frost, Resists = Element.Ember, Lumen = 1300,
                Loot = new[] { new LootRoll("mirage_heart", 1f, 1, 1), new LootRoll("ember_gland", 1f, 1, 2) },
                Behaviour = "Splits into mirages, hurls sand storms and calls up Sand Golems. Only one image is real.",
                Weak = "The real Djinn casts a shadow. The mirages do not.",
                Lore = "It grants three wishes, the story goes. Nobody tells the rest of the story."
            },
            // ---------------------------------------------------------------- bosses of the city and the Inner World
            new CreatureSpecies
            {
                Id = "golem_foreman", Name = "Shard Golem Foreman", Plural = "Shard Golem Foremen", Tier = CreatureTier.OuterCity, Humanoid = HumanoidCreature.GolemForeman,
                Temperament = Temperament.Apex, Boss = true, Habitats = new[] { Habitat.Arena }, Day = true,
                PackMin = 1, PackMax = 1, Health = 3000f, Poise = 700f, Damage = 48f, WalkSpeed = 1.6f, RunSpeed = 4.2f, Sense = 50f,
                Weakness = Element.Volt, Resists = Element.Frost, Lumen = 1000,
                Loot = new[] { new LootRoll("foreman_core", 1f, 1, 1), new LootRoll("golem_core", 1f, 1, 2) },
                Behaviour = "Rules the quarry. Calls mite swarms, rains crystal and slams the ground in rings.",
                Weak = "Lightning stuns the crown. Break the hazard plates to expose the core.",
                Lore = "The first golem the quarry ever woke. It still runs the shift."
            },
            new CreatureSpecies
            {
                Id = "wraith_queen", Name = "Mirror Wraith Queen", Plural = "Mirror Wraith Queens", Tier = CreatureTier.InnerWorld, Humanoid = HumanoidCreature.WraithQueen,
                Temperament = Temperament.Apex, Boss = true, Habitats = new[] { Habitat.Arena },
                PackMin = 1, PackMax = 1, Health = 3400f, Poise = 750f, Damage = 46f, WalkSpeed = 1.6f, RunSpeed = 6f, Sense = 60f,
                Weakness = Element.Ember, Resists = Element.Void, Lumen = 1250,
                Loot = new[] { new LootRoll("queen_mirror", 1f, 1, 1), new LootRoll("mirror_glass", 1f, 2, 3) },
                Behaviour = "Copies your fighting style, splits your reflection into wraiths and shatters glass across the floor.",
                Weak = "Her copies share her guard: break one and they all crack.",
                Lore = "She holds court in the Inner World's Crystal Manor, wearing the face of whoever came last."
            }
        };

        private static Dictionary<string, CreatureSpecies> _byId;

        // Runs once, right after All is built: attaches the combat data to every species.
        static CreatureSpeciesDb()
        {
            AttachCombatData();
        }

        public static void ResetStatics()
        {
            _byId = null;
        }

        /// <summary>Attack sets, leash and warning radii, flight height, hit effects and death styles per species.</summary>
        private static void AttachCombatData()
        {
            foreach (CreatureSpecies s in All)
            {
                s.Attacks = CreatureAttackDb.For(s.Id);
                if (s.Temperament == Temperament.Apex) s.Leash = 140f;
                s.WarnRadius = Mathf.Max(10f, s.Sense * 0.6f);
                s.StrikeRadius = Mathf.Max(4f, s.Sense * 0.22f);
                switch (s.Id)
                {
                    case "glass_mite": s.HitColor = new Color(0.55f, 0.95f, 1f); s.Tempo = 0.6f; s.DeathStyle = CreatureDeath.Crystal; break;
                    case "neon_jackal": s.HitColor = new Color(0.3f, 0.95f, 1f); s.Tempo = 0.9f; break;
                    case "shard_golem": s.HitColor = new Color(0.3f, 0.95f, 1f); s.DeathStyle = CreatureDeath.Crumble; s.Tempo = 1.2f; break;
                    case "hollow_shade": s.HitSurface = ImpactSurface.Shadow; s.HitColor = new Color(0.55f, 0.25f, 0.95f); s.DeathStyle = CreatureDeath.Smoke; s.CruiseHeight = 0.6f; break;
                    case "hollow": s.HitSurface = ImpactSurface.Shadow; s.HitColor = new Color(0.6f, 0.25f, 1f); s.DeathStyle = CreatureDeath.Smoke; break;
                    case "mirror_wraith": s.HitSurface = ImpactSurface.Glass; s.HitColor = new Color(0.75f, 0.95f, 1f); s.DeathStyle = CreatureDeath.Shatter; break;
                    case "crystal_basilisk": s.HitColor = new Color(0.7f, 0.4f, 1f); s.Tempo = 1.3f; break;
                    case "violet_wyvern": s.HitColor = new Color(0.75f, 0.35f, 1f); s.CruiseHeight = 18f; break;
                    case "frost_stalker": s.HitColor = new Color(0.6f, 0.9f, 1f); s.WarnRadius = 0f; break;
                    case "rime_griffin": s.HitColor = new Color(0.7f, 0.92f, 1f); s.CruiseHeight = 16f; s.WarnRadius = 32f; s.StrikeRadius = 12f; break;
                    case "snow_hollow": s.HitSurface = ImpactSurface.Shadow; s.HitColor = new Color(0.7f, 0.9f, 1f); s.DeathStyle = CreatureDeath.Smoke; break;
                    case "glacier_titan": s.HitColor = new Color(0.6f, 0.9f, 1f); s.DeathStyle = CreatureDeath.BossBurst; s.Leash = 0f; s.Tempo = 1.1f; break;
                    case "dune_wyrm": s.HitColor = new Color(1f, 0.6f, 0.2f); s.Leash = 220f; s.Tempo = 1.2f; break;
                    case "quartz_drake": s.HitColor = new Color(1f, 0.55f, 0.75f); s.CruiseHeight = 24f; s.Leash = 200f; s.Tempo = 1.1f; break;
                    case "sun_griffin": s.HitColor = new Color(1f, 0.8f, 0.35f); s.CruiseHeight = 18f; s.WarnRadius = 30f; s.StrikeRadius = 12f; break;
                    case "sand_golem": s.HitColor = new Color(1f, 0.72f, 0.35f); s.DeathStyle = CreatureDeath.Crumble; s.Tempo = 1.2f; break;
                    case "mirage_djinn": s.HitSurface = ImpactSurface.Shadow; s.HitColor = new Color(1f, 0.75f, 0.3f); s.DeathStyle = CreatureDeath.BossBurst; s.Leash = 0f; break;
                    case "golem_foreman": s.HitColor = new Color(0.35f, 0.95f, 1f); s.DeathStyle = CreatureDeath.BossBurst; s.Leash = 0f; s.Tempo = 1.1f; break;
                    case "wraith_queen": s.HitSurface = ImpactSurface.Glass; s.HitColor = new Color(0.8f, 0.95f, 1f); s.DeathStyle = CreatureDeath.BossBurst; s.Leash = 0f; break;
                }
            }
        }

        public static CreatureSpecies Get(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            if (_byId == null)
            {
                _byId = new Dictionary<string, CreatureSpecies>();
                foreach (CreatureSpecies s in All) _byId[s.Id] = s;
            }
            CreatureSpecies c;
            return _byId.TryGetValue(id, out c) ? c : null;
        }
    }
}
