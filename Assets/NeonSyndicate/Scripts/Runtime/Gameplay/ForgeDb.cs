using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>Where a Forge stands: the light forge beside a Resonance Shrine, Dante Rook's Forgewright forge, the Frost Wardens outpost.</summary>
    public enum ForgeStation
    {
        Shrine = 0,
        Forgewright = 1,
        Outpost = 2
    }

    public enum RecipeKind
    {
        Infusion,
        Brew,
        Upgrade
    }

    /// <summary>What a data-driven unlock condition looks at.</summary>
    public enum UnlockKind
    {
        /// <summary>Forgewrights rank at least Value (a FactionRank).</summary>
        ForgeRank,
        /// <summary>The faction in Arg at least rank Value.</summary>
        FactionRank,
        /// <summary>The Third Eye is unlocked.</summary>
        ThirdEye,
        /// <summary>The story mission flag in Arg is set (MissionManager.Flags).</summary>
        StoryFlag,
        /// <summary>The quest with id Arg is complete (QuestBook.IsDone).</summary>
        QuestDone,
        /// <summary>Story progress (missions passed) at least Value.</summary>
        StoryProgress
    }

    /// <summary>One condition a recipe needs before it is known.</summary>
    public struct UnlockRule
    {
        public UnlockKind Kind;
        public string Arg;
        public int Value;

        public UnlockRule(UnlockKind kind, string arg, int value)
        {
            Kind = kind;
            Arg = arg;
            Value = value;
        }
    }

    /// <summary>A price: a list of materials (each id at most once) and cash.</summary>
    public sealed class ForgeCost
    {
        public readonly List<KeyValuePair<string, int>> Materials = new List<KeyValuePair<string, int>>();
        public int Cash;

        /// <summary>Adds materials, merging with an entry for the same id.</summary>
        public ForgeCost Add(string id, int count)
        {
            if (string.IsNullOrEmpty(id) || count <= 0) return this;
            for (int i = 0; i < Materials.Count; i++)
            {
                if (Materials[i].Key != id) continue;
                Materials[i] = new KeyValuePair<string, int>(id, Materials[i].Value + count);
                return this;
            }
            Materials.Add(new KeyValuePair<string, int>(id, count));
            return this;
        }

        public int CountOf(string id)
        {
            foreach (KeyValuePair<string, int> kv in Materials) if (kv.Key == id) return kv.Value;
            return 0;
        }

        public ForgeCost Clone()
        {
            ForgeCost c = new ForgeCost { Cash = Cash };
            c.Materials.AddRange(Materials);
            return c;
        }
    }

    /// <summary>One rank of an upgrade: its price and the rank needed with the gating faction.</summary>
    public sealed class UpgradeStep
    {
        public ForgeCost Cost;
        public FactionRank Need;
    }

    /// <summary>
    /// A Forge recipe that is not a gear piece (gear is designed in the Forge from slot, rarity, set and element):
    /// a weapon infusion, a timed brew or a ranked permanent upgrade. Everything about it is data, including what
    /// unlocks it.
    /// </summary>
    public sealed class RecipeDef
    {
        public string Id;
        public string Name;
        public RecipeKind Kind;
        public string Text = "";
        public ForgeCost Cost = new ForgeCost();
        /// <summary>Every rule must hold (plus the station) before the recipe is known, unless it was granted.</summary>
        public UnlockRule[] Rules = new UnlockRule[0];
        /// <summary>Needs Dante's forge or the outpost (not a shrine).</summary>
        public bool FullForge;
        /// <summary>Only at the Frost Wardens outpost.</summary>
        public bool OutpostOnly;
        // Infusions.
        public Element Element;
        // Brews: a timed effect.
        public float Seconds;
        public SkillEffect[] Effects = new SkillEffect[0];
        // Upgrades: one step per rank; the rank requirement is with RankFaction.
        public UpgradeStep[] Steps = new UpgradeStep[0];
        public string RankFaction = "forgewrights";

        public int MaxRank { get { return Steps.Length; } }
    }

    /// <summary>A gear piece the player designs at the Forge.</summary>
    public struct GearOrder
    {
        // Request only, never saved. Zero retains every original designed order.
        public int Regional;
        public GearSlot Slot;
        public Rarity Rarity;
        /// <summary>Set index (GearSystem.SetNames) or -1.</summary>
        public int Set;
        public Element Element;
        /// <summary>Mythic only: the boss heart material id the piece is forged around.</summary>
        public string Heart;
    }

    /// <summary>What the Forge needs to know about the world to decide which recipes are known (tests use a fake).</summary>
    public interface IForgeContext
    {
        FactionRank RankOf(string factionId);
        bool ThirdEye { get; }
        bool HasStoryFlag(string flag);
        bool QuestDone(string questId);
        int StoryProgress { get; }
        /// <summary>Recipes granted directly (quest rewards, debug), known regardless of rules.</summary>
        bool Granted(string recipeId);
    }

    /// <summary>Cash and materials the Forge pays from (the game uses Economy and the material satchel; tests use a fake).</summary>
    public interface IForgeWallet
    {
        int Cash { get; }
        int Count(string materialId);
        bool TakeCash(int amount);
        void GiveCash(int amount);
        /// <summary>All-or-nothing: takes every material or none.</summary>
        bool TakeMaterials(IList<KeyValuePair<string, int>> materials);
    }

    /// <summary>The recipe catalog. Ids are stable: saves store known recipes, crafted counts and upgrade ranks by id.</summary>
    public static class ForgeDb
    {
        public static readonly RecipeDef[] All = Build();

        /// <summary>Boss hearts a Mythic piece can be forged around.</summary>
        public static readonly string[] Hearts = { "titan_heart", "quartz_heart", "mirage_heart", "foreman_core", "queen_mirror" };

        private static Dictionary<string, RecipeDef> _byId;
        private static readonly List<KeyValuePair<string, string>> _questUnlocks = new List<KeyValuePair<string, string>>();

        public static void ResetStatics()
        {
            _byId = null;
            _questUnlocks.Clear();
        }

        public static RecipeDef Get(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            if (_byId == null)
            {
                Dictionary<string, RecipeDef> d = new Dictionary<string, RecipeDef>();
                foreach (RecipeDef r in All) d[r.Id] = r;
                _byId = d;
            }
            RecipeDef rec;
            return _byId.TryGetValue(id, out rec) ? rec : null;
        }

        /// <summary>
        /// Data-driven quest rewards: when the quest with questId completes, recipeId becomes known for good. Quest
        /// authors register these at boot (ids that never complete simply never unlock anything).
        /// </summary>
        public static void RegisterQuestUnlock(string questId, string recipeId)
        {
            if (string.IsNullOrEmpty(questId) || Get(recipeId) == null) return;
            foreach (KeyValuePair<string, string> kv in _questUnlocks) if (kv.Key == questId && kv.Value == recipeId) return;
            _questUnlocks.Add(new KeyValuePair<string, string>(questId, recipeId));
        }

        /// <summary>Recipes a completed quest unlocks (appended to into).</summary>
        public static void UnlocksFor(string questId, List<string> into)
        {
            if (into == null || string.IsNullOrEmpty(questId)) return;
            foreach (KeyValuePair<string, string> kv in _questUnlocks) if (kv.Key == questId) into.Add(kv.Value);
        }

        public static IList<KeyValuePair<string, string>> QuestUnlocks { get { return _questUnlocks; } }

        // ------------------------------------------------------------------ catalog

        private static ForgeCost C(int cash, params object[] pairs)
        {
            ForgeCost c = new ForgeCost { Cash = cash };
            for (int i = 0; i + 1 < pairs.Length; i += 2) c.Add((string)pairs[i], (int)pairs[i + 1]);
            return c;
        }

        private static UpgradeStep Step(FactionRank need, ForgeCost cost) { return new UpgradeStep { Need = need, Cost = cost }; }

        private static RecipeDef[] Build()
        {
            UnlockRule neutral = new UnlockRule(UnlockKind.ForgeRank, null, (int)FactionRank.Neutral);
            UnlockRule friendly = new UnlockRule(UnlockKind.ForgeRank, null, (int)FactionRank.Friendly);
            UnlockRule eye = new UnlockRule(UnlockKind.ThirdEye, null, 0);
            UnlockRule wardens = new UnlockRule(UnlockKind.FactionRank, "frost_wardens", (int)FactionRank.Neutral);
            List<RecipeDef> l = new List<RecipeDef>
            {
                // Weapon infusions: re-tune the equipped Weapon Core, which sets the element of every melee weapon.
                new RecipeDef { Id = "infuse_volt", Name = "Volt Infusion", Kind = RecipeKind.Infusion, Element = Element.Volt, Cost = C(400, "glass_chitin", 4, "neon_hide", 1), Rules = new[] { neutral },
                    Text = "Tune your equipped Weapon Core to Volt: melee hits can arc lightning into another enemy." },
                new RecipeDef { Id = "infuse_ember", Name = "Ember Infusion", Kind = RecipeKind.Infusion, Element = Element.Ember, Cost = C(400, "wyrm_scale", 2, "sun_feather", 1), Rules = new[] { neutral },
                    Text = "Tune your equipped Weapon Core to Ember: melee hits can set enemies burning." },
                new RecipeDef { Id = "infuse_frost", Name = "Frost Infusion", Kind = RecipeKind.Infusion, Element = Element.Frost, Cost = C(400, "frost_essence", 4, "frost_fang", 1), Rules = new[] { neutral },
                    Text = "Tune your equipped Weapon Core to Frost: melee hits can shatter an enemy's poise." },
                new RecipeDef { Id = "infuse_void", Name = "Void Infusion", Kind = RecipeKind.Infusion, Element = Element.Void, Cost = C(400, "hollow_essence", 4, "void_thread", 1), Rules = new[] { neutral },
                    Text = "Tune your equipped Weapon Core to Void: melee hits can drain health back to you." },

                // Brews: timed effects, crafted anywhere.
                new RecipeDef { Id = "brew_hunters_oil", Name = "Hunter's Oil", Kind = RecipeKind.Brew, Seconds = 360f, Cost = C(120, "jackal_fang", 3, "mite_dust", 2),
                    Effects = new[] { new SkillEffect(SkillStat.CreatureDamage, 0.2f, false), new SkillEffect(SkillStat.MaterialFind, 0.1f, false) },
                    Text = "For 6 minutes: +20% damage to creatures and +10% chance on creature loot rolls." },
                new RecipeDef { Id = "brew_warding_salve", Name = "Warding Salve", Kind = RecipeKind.Brew, Seconds = 240f, Cost = C(150, "shard_chunk", 2, "hollow_essence", 2),
                    Effects = new[] { new SkillEffect(SkillStat.DamageTaken, -0.15f, false) },
                    Text = "For 4 minutes: take 15% less damage." },
                new RecipeDef { Id = "brew_frost_tea", Name = "Frost Ward Tea", Kind = RecipeKind.Brew, Seconds = 480f, Cost = C(100, "rime_pelt", 2, "frost_essence", 2),
                    Effects = new[] { new SkillEffect(SkillStat.FrostResist, 0.25f, false) },
                    Text = "For 8 minutes: take 25% less damage deep in the Frostline." },
                new RecipeDef { Id = "brew_menders_draught", Name = "Mender's Draught", Kind = RecipeKind.Brew, Seconds = 600f, Cost = C(120, "mite_dust", 3, "frost_essence", 1, "shard_chunk", 1),
                    Effects = new[] { new SkillEffect(SkillStat.TonicHeal, 0.25f, false) },
                    Text = "For 10 minutes: Shard Tonics heal 25% more." },
                new RecipeDef { Id = "brew_veil_incense", Name = "Veil Incense", Kind = RecipeKind.Brew, Seconds = 300f, Cost = C(200, "shade_essence", 1, "void_thread", 1), Rules = new[] { eye },
                    Effects = new[] { new SkillEffect(SkillStat.VeilDrain, 0.65f, true) },
                    Text = "For 5 minutes: the Veil drains 35% less crystal energy. Needs the Third Eye." },
                new RecipeDef { Id = "brew_focus_draught", Name = "Focus Draught", Kind = RecipeKind.Brew, Seconds = 180f, Cost = C(200, "glass_chitin", 2, "rift_shard", 1), Rules = new[] { friendly },
                    Effects = new[] { new SkillEffect(SkillStat.PowerCost, 0.8f, true), new SkillEffect(SkillStat.PowerDamage, 0.1f, false) },
                    Text = "For 3 minutes: crystal powers cost 20% less and deal 10% more. Forgewrights Friendly." },

                // Upgrades: permanent, ranked, need a full forge.
                new RecipeDef { Id = "upg_tonic_flask", Name = "Tonic Flask", Kind = RecipeKind.Upgrade, FullForge = true, Rules = new[] { neutral },
                    Effects = new[] { new SkillEffect(SkillStat.TonicCharges, 1f, false) },
                    Steps = new[] { Step(FactionRank.Neutral, C(1500, "shard_chunk", 6, "shade_essence", 2)), Step(FactionRank.Trusted, C(4000, "golem_core", 1, "rift_shard", 3)) },
                    Text = "A crystal-lined flask: +1 Shard Tonic charge per rank." },
                new RecipeDef { Id = "upg_energy_cell", Name = "Energy Cell", Kind = RecipeKind.Upgrade, FullForge = true, Rules = new[] { neutral },
                    Effects = new[] { new SkillEffect(SkillStat.EnergyMax, 15f, false) },
                    Steps = new[] { Step(FactionRank.Neutral, C(1200, "glass_chitin", 6, "neon_hide", 2)), Step(FactionRank.Friendly, C(2500, "neon_hide", 4, "golem_core", 1)), Step(FactionRank.Trusted, C(5000, "golem_core", 2, "crown_gem", 1)) },
                    Text = "A golem-core battery stitched into your coat: +15 max crystal energy per rank." },
                new RecipeDef { Id = "upg_frost_lining", Name = "Frost Lining", Kind = RecipeKind.Upgrade, FullForge = true, OutpostOnly = true, RankFaction = "frost_wardens", Rules = new[] { wardens },
                    Effects = new[] { new SkillEffect(SkillStat.FrostResist, 0.1f, false) },
                    Steps = new[] { Step(FactionRank.Neutral, C(900, "rime_pelt", 4, "frost_fang", 1)), Step(FactionRank.Friendly, C(2000, "rime_feather", 2, "frost_fang", 2)), Step(FactionRank.Trusted, C(4500, "glacier_shard", 1, "rime_feather", 3)) },
                    Text = "The Wardens' fur and feather lining: take 10% less damage deep in the Frostline per rank. Frost Wardens outpost only." },
                new RecipeDef { Id = "upg_veil_anchor", Name = "Veil Anchor", Kind = RecipeKind.Upgrade, FullForge = true, Rules = new[] { neutral, eye },
                    Effects = new[] { new SkillEffect(SkillStat.VeilDrain, 0.88f, true) },
                    Steps = new[] { Step(FactionRank.Neutral, C(1500, "void_thread", 3, "shade_essence", 2)), Step(FactionRank.Friendly, C(3000, "mirror_glass", 1, "void_thread", 3)), Step(FactionRank.Trusted, C(6000, "mirror_glass", 2, "crown_gem", 1)) },
                    Text = "A mirror-glass anchor that holds you to the Outer City: the Veil drains 12% less per rank. Needs the Third Eye." },

                // 04E Underheart brews (reward recipes: granted by uh_recipe_coolant and uh_utility_recipe, or known after their quests).
                new RecipeDef { Id = "brew_coolant_draught", Name = "Coolant Draught", Kind = RecipeKind.Brew, Seconds = 480f, Cost = C(90, "coolant_salt", 3, "ash_resin", 1),
                    Rules = new[] { new UnlockRule(UnlockKind.QuestDone, "uh05_06_pressure", 0) },
                    Effects = new[] { new SkillEffect(SkillStat.DamageTaken, -0.06f, false) },
                    Text = "For 8 minutes: underground heat and fire hurt 30% less (the ward reads it), and you take 6% less damage overall." },
                new RecipeDef { Id = "brew_lantern_oil", Name = "Lantern Oil", Kind = RecipeKind.Brew, Seconds = 600f, Cost = C(80, "ember_crystal", 1, "coolant_salt", 2),
                    Rules = new[] { new UnlockRule(UnlockKind.QuestDone, "uh05_s03_shelter", 0) },
                    Effects = new[] { new SkillEffect(SkillStat.MaterialFind, 0.1f, false), new SkillEffect(SkillStat.StaminaRegen, 0.15f, false) },
                    Text = "For 10 minutes: +10% chance on creature loot rolls and stamina recovers 15% faster." }
            };
            l.Add(new RecipeDef { Id="brew_stillwater_04h",Name="Stillwater Tonic",Kind=RecipeKind.Brew,Seconds=360f,
                Cost=C(120,"shard_chunk",3,"coolant_salt",1),
                Rules=new[] {new UnlockRule(UnlockKind.QuestDone,"uh05_04h_01_quiet_circuit",0)},
                Effects=new[] {new SkillEffect(SkillStat.StaminaRegen,.12f,false),new SkillEffect(SkillStat.GuardCost,-.05f,false)},
                Text="For 6 minutes: stamina refills 12% faster and guard costs 5% less. Learned from The Quiet Circuit." });
            l.Add(new RecipeDef {Id="brew_ascendant_edge_04i",Name="Ascendant Edge Oil",Kind=RecipeKind.Brew,Seconds=480f,
                Cost=C(160,"shard_chunk",4,"coolant_salt",2,"ember_crystal",1),
                Rules=new[]{new UnlockRule(UnlockKind.QuestDone,"uh05_04i_04_recovery",0)},
                Effects=new[]{new SkillEffect(SkillStat.MeleeDamage,.08f,false),new SkillEffect(SkillStat.GuardCost,-.04f,false)},
                Text="For 8 minutes: melee damage +8%, guard cost -4%. Learned by recovering the Ascension coupler."});
            l.Add(new RecipeDef {Id="brew_cityward_04i",Name="Cityward Infusion",Kind=RecipeKind.Brew,Seconds=480f,
                Cost=C(140,"glass_chitin",3,"coolant_salt",2),
                Rules=new[]{new UnlockRule(UnlockKind.QuestDone,"uh05_04i_05_ascension",0)},
                Effects=new[]{new SkillEffect(SkillStat.StaminaMax,10f,false),new SkillEffect(SkillStat.DamageTaken,-.04f,false)},
                Text="For 8 minutes: +10 maximum stamina, incoming damage -4%. Learned from the completed Ascension circuit."});
            l.Add(new RecipeDef {Id="brew_vanguard_edge_04k",Name="Vanguard Edge Temper",Kind=RecipeKind.Brew,Seconds=360f,
                Cost=C(130,"shard_chunk",4,"coolant_salt",1),
                Rules=new[]{new UnlockRule(UnlockKind.QuestDone,VanguardRules04K.Quest,0)},
                Effects=new[]{new SkillEffect(SkillStat.MeleeDamage,.10f,false),new SkillEffect(SkillStat.GuardCost,.06f,false)},
                Text="For 6 minutes: melee damage +10%, guard stamina cost +6%. A brittle edge trades defense for impact."});
            l.Add(new RecipeDef {Id="brew_bell_ward_04k",Name="Bellglass Ward Wash",Kind=RecipeKind.Brew,Seconds=360f,
                Cost=C(130,"glass_chitin",4,"coolant_salt",1),
                Rules=new[]{new UnlockRule(UnlockKind.QuestDone,VanguardRules04K.Quest,0)},
                Effects=new[]{new SkillEffect(SkillStat.DamageTaken,-.07f,false),new SkillEffect(SkillStat.StaminaRegen,-.05f,false)},
                Text="For 6 minutes: incoming damage -7%, stamina recovery -5%. A heavier defensive coating."});
            ForgeTreatments04M.Append(l);
            return l.ToArray();
        }
    }

    /// <summary>
    /// Forge arithmetic and rules (engine-free, tested offline): gear piece prices by rarity, slot, set and element,
    /// unlock checks, discounts, salvage prices and the all-or-nothing payment.
    /// </summary>
    public static class ForgeMath
    {
        /// <summary>Frame materials per slot: common, uncommon and rare.</summary>
        private static readonly string[][] Frames =
        {
            new[] { "glass_chitin", "neon_hide", "crown_gem" },        // Visor
            new[] { "shard_chunk", "basilisk_scale", "drake_plate" },  // Shell
            new[] { "jackal_fang", "wyrm_scale", "golem_core" },       // Gauntlets
            new[] { "rime_pelt", "wyvern_membrane", "griffin_heart" }, // Striders
            new[] { "mite_dust", "void_thread", "mirror_glass" },      // Implant
            new[] { "shard_chunk", "sand_core", "golem_core" }         // Weapon Core
        };

        /// <summary>Element essences: common, uncommon and rare.</summary>
        private static string[] Essence(Element e)
        {
            switch (e)
            {
                case Element.Volt: return new[] { "glass_chitin", "neon_hide", "golem_core" };
                case Element.Ember: return new[] { "wyrm_scale", "sun_feather", "ember_gland" };
                case Element.Frost: return new[] { "frost_essence", "frost_fang", "glacier_shard" };
                case Element.Void: return new[] { "hollow_essence", "void_thread", "mirror_glass" };
                default: return null;
            }
        }

        /// <summary>Set materials: Neon Revenant, Frostline Warden, Rift Walker.</summary>
        private static readonly string[] SetMaterial = { "neon_hide", "rime_feather", "rift_shard" };

        /// <summary>The smith's fee for each rarity.</summary>
        public static readonly int[] GearFee = { 150, 300, 700, 1500, 3500, 8000 };

        /// <summary>Why a gear order cannot be forged as asked (null when it can). Uses the gear rules in GearSystem.</summary>
        public static string GearOrderProblem(GearOrder o)
        {
            if (o.Set >= 0 && !GearSystem.SetAllowed(o.Slot, o.Rarity)) return "Sets come on Rare or better Visors, Shells, Gauntlets and Striders.";
            if (o.Set >= GearSystem.SetNames.Length) return "Unknown set.";
            if (o.Element != Element.None && !GearSystem.ElementAllowed(o.Slot, o.Rarity)) return "Only Weapon Cores and Epic or better pieces carry an element.";
            if (o.Element == Element.None && GearSystem.ElementRequired(o.Slot)) return "A Weapon Core needs an element.";
            if (o.Rarity == Rarity.Mythic && System.Array.IndexOf(ForgeDb.Hearts, o.Heart) < 0) return "A Mythic piece needs a boss heart.";
            return null;
        }

        /// <summary>The full price of a gear order before discounts (materials scale with rarity; Mythic adds a boss heart).</summary>
        public static ForgeCost GearCost(GearOrder o)
        {
            ForgeCost c = new ForgeCost();
            int slot = Mathf.Clamp((int)o.Slot, 0, Frames.Length - 1);
            string[] f = Frames[slot];
            int r = Mathf.Clamp((int)o.Rarity, 0, (int)Rarity.Mythic);
            c.Cash = GearFee[r];
            switch ((Rarity)r)
            {
                case Rarity.Common: c.Add(f[0], 3).Add("mite_dust", 2); break;
                case Rarity.Uncommon: c.Add(f[0], 5).Add("mite_dust", 3); break;
                case Rarity.Rare: c.Add(f[0], 6).Add(f[1], 2); break;
                case Rarity.Epic: c.Add(f[0], 8).Add(f[1], 4); break;
                case Rarity.Legendary: c.Add(f[1], 6).Add(f[2], 2); break;
                default: c.Add(f[1], 8).Add(f[2], 3).Add(o.Heart, 1); break;
            }
            string[] e = Essence(o.Element);
            if (e != null)
            {
                // There is no common Ember material, so low-rarity Ember pieces take one uncommon wyrm scale instead of two.
                if (r <= (int)Rarity.Uncommon) c.Add(e[0], o.Element == Element.Ember ? 1 : 2);
                else if (r <= (int)Rarity.Epic) c.Add(e[1], 2);
                else c.Add(e[2], 1).Add(e[1], 2);
            }
            if (o.Set >= 0 && o.Set < SetMaterial.Length) c.Add(SetMaterial[o.Set], r >= (int)Rarity.Legendary ? 4 : 2);
            return c;
        }

        /// <summary>Elemental trigger chance each infusion adds to the Weapon Core with Prismatic Infusion.</summary>
        public const float PrismaticStep = 0.1f;
        /// <summary>Prismatic Infusion raises the Core's elemental trigger chance up to this total (it never lowers a better roll).</summary>
        public const float PrismaticCap = 0.3f;

        /// <summary>
        /// The Weapon Core's elemental trigger chance after an infusion with Prismatic Infusion: PrismaticStep more, up to
        /// PrismaticCap, and never lower than before (high-rarity cores can roll more than the cap).
        /// </summary>
        public static float PrismaticProc(float before)
        {
            if (float.IsNaN(before)) return PrismaticStep;
            return Mathf.Max(before, Mathf.Min(PrismaticCap, before + PrismaticStep));
        }

        /// <summary>Forgewrights reputation earned by forging a piece of a rarity.</summary>
        public static int GearRep(Rarity r)
        {
            if (r >= Rarity.Legendary) return 4;
            if (r >= Rarity.Rare) return 2;
            return 1;
        }

        /// <summary>Two discounts stacked (each applies to what is left), capped at 50% off.</summary>
        public static float CombineDiscounts(float a, float b)
        {
            float d = 1f - (1f - Mathf.Clamp01(a)) * (1f - Mathf.Clamp01(b));
            return Mathf.Clamp(d, 0f, 0.5f);
        }

        public static int Discounted(int cash, float discount)
        {
            if (cash <= 0) return 0;
            return Mathf.Max(1, Mathf.RoundToInt(cash * (1f - Mathf.Clamp(discount, 0f, 0.5f))));
        }

        /// <summary>A cost with its cash discounted (materials never are).</summary>
        public static ForgeCost WithDiscount(ForgeCost c, float discount)
        {
            ForgeCost d = c.Clone();
            d.Cash = Discounted(c.Cash, discount);
            return d;
        }

        /// <summary>What a Forgewright pays for one material.</summary>
        public static int SalvagePrice(MaterialDef m, float bonus)
        {
            if (m == null) return 0;
            return Mathf.Max(1, Mathf.RoundToInt(m.Value * (1f + Mathf.Max(0f, bonus))));
        }

        // ------------------------------------------------------------------ unlocks

        public static bool RuleHolds(UnlockRule r, IForgeContext ctx)
        {
            if (ctx == null) return false;
            switch (r.Kind)
            {
                case UnlockKind.ForgeRank: return (int)ctx.RankOf("forgewrights") >= r.Value;
                case UnlockKind.FactionRank: return (int)ctx.RankOf(r.Arg) >= r.Value;
                case UnlockKind.ThirdEye: return ctx.ThirdEye;
                case UnlockKind.StoryFlag: return ctx.HasStoryFlag(r.Arg);
                case UnlockKind.QuestDone: return ctx.QuestDone(r.Arg);
                case UnlockKind.StoryProgress: return ctx.StoryProgress >= r.Value;
                default: return false;
            }
        }

        /// <summary>A recipe is known when it was granted, or when every one of its rules holds.</summary>
        public static bool Known(RecipeDef r, IForgeContext ctx)
        {
            if (r == null || ctx == null) return false;
            if (ctx.Granted(r.Id)) return true;
            foreach (UnlockRule u in r.Rules) if (!RuleHolds(u, ctx)) return false;
            return true;
        }

        /// <summary>The first rule that does not hold, in plain words (empty when known).</summary>
        public static string LockReason(RecipeDef r, IForgeContext ctx)
        {
            if (r == null || ctx == null || Known(r, ctx)) return "";
            foreach (UnlockRule u in r.Rules)
            {
                if (RuleHolds(u, ctx)) continue;
                switch (u.Kind)
                {
                    case UnlockKind.ForgeRank: return "Needs Forgewrights " + (FactionRank)u.Value + ".";
                    case UnlockKind.FactionRank:
                    {
                        FactionDef f = FactionDb.Get(u.Arg);
                        return "Needs " + (f != null ? f.Name : u.Arg) + " " + (FactionRank)u.Value + ".";
                    }
                    case UnlockKind.ThirdEye: return "Needs the Third Eye.";
                    case UnlockKind.StoryFlag: return "Unlocks later in the story.";
                    case UnlockKind.QuestDone: return "Unlocks with a quest.";
                    case UnlockKind.StoryProgress: return "Unlocks after story mission " + u.Value + ".";
                }
            }
            return "Locked.";
        }

        /// <summary>Whether a station can make a recipe.</summary>
        public static bool StationAllows(RecipeDef r, ForgeStation s)
        {
            if (r == null) return false;
            if (r.OutpostOnly) return s == ForgeStation.Outpost;
            if (r.FullForge) return s != ForgeStation.Shrine;
            return true;
        }

        /// <summary>Gear pieces need a full forge.</summary>
        public static bool StationForgesGear(ForgeStation s)
        {
            return s != ForgeStation.Shrine;
        }

        // ------------------------------------------------------------------ paying

        /// <summary>Lists what is missing for a cost (empty when affordable). Each line: name, have, need.</summary>
        public static List<string> Missing(ForgeCost c, IForgeWallet w)
        {
            List<string> l = new List<string>();
            if (c == null || w == null) return l;
            foreach (KeyValuePair<string, int> kv in c.Materials)
            {
                int have = w.Count(kv.Key);
                if (have < kv.Value) l.Add(MaterialDb.NameOf(kv.Key) + " " + have + "/" + kv.Value);
            }
            if (w.Cash < c.Cash) l.Add("Cash " + MathUtil.Money(w.Cash) + "/" + MathUtil.Money(c.Cash));
            return l;
        }

        public static bool CanPay(ForgeCost c, IForgeWallet w)
        {
            if (c == null || w == null) return false;
            if (w.Cash < c.Cash) return false;
            foreach (KeyValuePair<string, int> kv in c.Materials) if (w.Count(kv.Key) < kv.Value) return false;
            return true;
        }

        /// <summary>Pays a cost completely or not at all: cash first, then materials (the cash comes back if they fail).</summary>
        public static bool TryPay(ForgeCost c, IForgeWallet w)
        {
            if (!CanPay(c, w)) return false;
            if (c.Cash > 0 && !w.TakeCash(c.Cash)) return false;
            if (c.Materials.Count > 0 && !w.TakeMaterials(c.Materials))
            {
                if (c.Cash > 0) w.GiveCash(c.Cash);
                return false;
            }
            return true;
        }
    }
}
