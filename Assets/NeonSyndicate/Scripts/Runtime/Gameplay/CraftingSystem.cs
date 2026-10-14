using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    [Serializable]
    public sealed class CraftSave
    {
        /// <summary>Recipes granted directly (quest rewards, debug); the rest are known by rank and conditions.</summary>
        public List<string> Known = new List<string>();
        /// <summary>How often each recipe was crafted (upgrade ranks are these counts) and gear forged per rarity.</summary>
        public List<string> CraftedIds = new List<string>();
        public List<int> CraftedCounts = new List<int>();
        // Progression work (appended): active brews and their seconds left.
        public List<string> BuffIds = new List<string>();
        public List<float> BuffTimes = new List<float>();
    }

    /// <summary>
    /// The Forge (Game.Crafting): turns creature materials into gear pieces the player designs (slot, rarity, set,
    /// element), weapon infusions, timed brews and permanent upgrades, buys materials back for cash and runs the Frost
    /// Wardens' shop. Stations: Dante Rook's Forgewright forge in Old Silver and the Frost Wardens outpost (full forges)
    /// and a light forge beside every Resonance Shrine (brews and infusions). Recipes and their unlocks are data
    /// (ForgeDb); gear is made by GearSystem.GenerateCrafted, so the gear rules live in one place.
    /// </summary>
    public sealed class CraftingSystem : MonoBehaviour, IWorldSystem
    {
        public const string ForgewrightsId = "forgewrights";
        public const string WardensId = "frost_wardens";
        public static readonly GearSlot[] WardenSlots = { GearSlot.Visor, GearSlot.Shell, GearSlot.Gauntlets, GearSlot.Striders };
        public const int WardenTeaPrice = 350;

        private readonly HashSet<string> _granted = new HashSet<string>();
        private readonly Dictionary<string, int> _crafted = new Dictionary<string, int>();
        private readonly Dictionary<string, float> _buffs = new Dictionary<string, float>();
        private readonly List<string> _tmp = new List<string>();
        private readonly GameWallet _wallet = new GameWallet();
        private GameContext _ctx;
        private QuestBook _hookedQuests;
        private Transform _root;
        private ForgeScreen _screen;
        private CityLayout _layout;
        private bool _built;

        public IForgeWallet Wallet { get { return _wallet; } }
        public IForgeContext Context { get { return _ctx; } }

        public void Init(CityLayout layout)
        {
            WorldSystems.Register(this);
            _ctx = new GameContext(this);
            _root = new GameObject("Forge Stations").transform;
            _root.SetParent(transform, false);
            _screen = gameObject.AddComponent<ForgeScreen>();
            HudOverlays.Register("forge_brews", ForgeScreen.DrawBrews);
            // The stations are placed once the boot has finished, so they can keep clear of every shop and marker.
            _layout = layout;
        }

        private void OnDestroy()
        {
            if (_hookedQuests != null) _hookedQuests.QuestCompleted -= OnQuestCompleted;
            _hookedQuests = null;
        }

        // ------------------------------------------------------------------ stations

        private void BuildStations(CityLayout layout)
        {
            if (layout == null) return;
            Color forge = new Color(1f, 0.6f, 0.2f);
            Poi dante = layout.FindPoi("Dante Rook");
            if (dante != null)
            {
                Vector3 at = PedManager.Ground(dante.Position);
                Vector3 right = MathUtil.RightOf(dante.Facing.sqrMagnitude > 0.01f ? dante.Facing : Vector3.forward);
                Vector3 spot;
                if (ForgeStations.FindSpot(at, -right, 3.2f, out spot))
                    Station(spot, MathUtil.YawOf(dante.Facing), 1f, ForgeStation.Forgewright, "Dante Rook's forge", "Forge: Dante Rook (Forgewrights)", forge);
                else Log.Warn("Forge: no free spot beside Dante Rook, so his forge was not placed.");
            }
            if (layout.WardenOutpost != null)
            {
                Vector3 want = PedManager.Ground(CityLayout.WardenOutpostCenter + new Vector3(9f, 0f, 2f));
                Vector3 spot;
                if (ForgeStations.FindSpot(want, Vector3.back, 0.05f, out spot) || ForgeStations.FindSpot(want, Vector3.back, 3f, out spot))
                    Station(spot, 180f, 1f, ForgeStation.Outpost, "the Frost Wardens' forge", "Forge: Frost Wardens outpost", new Color(0.55f, 0.85f, 1f));
                else Log.Warn("Forge: no free spot in the Frost Wardens Outpost, so its forge was not placed.");
            }
            if (Game.Progress != null)
            {
                int missing = 0;
                foreach (Shrine s in Game.Progress.Shrines)
                {
                    Vector3 spot;
                    if (ForgeStations.FindSpot(s.Position, Vector3.left, 3.2f, out spot)) Station(spot, 90f, 0.7f, ForgeStation.Shrine, "the shrine forge", null, forge);
                    else missing++;
                }
                if (missing > 0) Log.Warn("Forge: " + missing + " shrine forge(s) had no free spot and were not placed.");
            }
        }

        private void Station(Vector3 spot, float yaw, float scale, ForgeStation kind, string name, string blipLabel, Color color)
        {
            ForgeStations.Anvil(spot, yaw, scale, _root, "Forge (" + name + ")");
            string title = kind == ForgeStation.Shrine ? "SHRINE FORGE" : (kind == ForgeStation.Outpost ? "FROST WARDENS' FORGE" : "DANTE ROOK'S FORGE");
            Interactable it = Interactable.Create("Forge " + name, spot, scale >= 1f ? 2.2f : 1.6f, "Press E to use " + name, () => UseStation(kind, title), _root);
            it.Condition = () => !CombatDirector.InCombat && (Game.Wanted == null || Game.Wanted.Stars == 0);
            it.DynamicPrompt = () => PromptFor(kind, name);
            if (!string.IsNullOrEmpty(blipLabel))
            {
                Blip b = Blip.Add(BlipKind.Service, null, spot, color, "A", blipLabel, false);
                b.Size = 0.95f;
            }
        }

        private string PromptFor(ForgeStation kind, string name)
        {
            if (kind == ForgeStation.Forgewright && FactionRules.Refuses(RankOf(ForgewrightsId))) return "Dante Rook won't work with you (Forgewrights: " + RankOf(ForgewrightsId) + ")";
            if (kind == ForgeStation.Outpost && FactionRules.Refuses(RankOf(WardensId))) return "The Frost Wardens turn you away (" + RankOf(WardensId) + ")";
            if (kind == ForgeStation.Shrine) return "Press E to use the shrine forge (brews and infusions)";
            return "Press E to use " + name;
        }

        private void UseStation(ForgeStation kind, string title)
        {
            if (kind == ForgeStation.Forgewright && FactionRules.Refuses(RankOf(ForgewrightsId)))
            {
                if (Game.Hud != null) Game.Hud.Notify("Dante Rook won't work with you. Win the Forgewrights back first.", 3f);
                return;
            }
            if (kind == ForgeStation.Outpost && FactionRules.Refuses(RankOf(WardensId)))
            {
                if (Game.Hud != null) Game.Hud.Notify("The Frost Wardens keep their forge from you.", 3f);
                return;
            }
            OpenForge(kind, title);
        }

        /// <summary>Opens the Forge screen for a station (also used by the debug page).</summary>
        public void OpenForge(ForgeStation kind, string title)
        {
            if (_screen != null) _screen.Open(kind, title);
        }

        public bool ScreenOpen { get { return _screen != null && _screen.IsOpen; } }

        // ------------------------------------------------------------------ knowledge

        private static FactionRank RankOf(string factionId)
        {
            return Game.Factions != null ? Game.Factions.RankOf(factionId) : FactionRank.Neutral;
        }

        public FactionRank ForgeRank { get { return RankOf(ForgewrightsId); } }
        public Rarity MaxRarity { get { return FactionRules.MaxForgeRarity(ForgeRank); } }

        public bool Knows(RecipeDef r) { return ForgeMath.Known(r, _ctx); }
        public string LockReason(RecipeDef r) { return ForgeMath.LockReason(r, _ctx); }
        public bool IsGranted(string recipeId) { return recipeId != null && _granted.Contains(recipeId); }

        /// <summary>Makes a recipe known for good (quest rewards). Quiet when it was already granted.</summary>
        public void Grant(string recipeId, bool announce)
        {
            RecipeDef r = ForgeDb.Get(recipeId);
            if (r == null || !_granted.Add(recipeId)) return;
            if (announce && Game.Hud != null) Game.Hud.Notify("New Forge recipe: " + r.Name + ".", 4f);
        }

        /// <summary>Quest authors: completing questId makes recipeId known (see ForgeDb.RegisterQuestUnlock).</summary>
        public void RegisterQuestUnlock(string questId, string recipeId)
        {
            ForgeDb.RegisterQuestUnlock(questId, recipeId);
            if (Game.Quests != null && Game.Quests.IsDone(questId)) Grant(recipeId, false);
        }

        public int Crafted(string id)
        {
            int n;
            return id != null && _crafted.TryGetValue(id, out n) ? n : 0;
        }

        public int UpgradeRank(RecipeDef r)
        {
            return r != null && r.Kind == RecipeKind.Upgrade ? Mathf.Min(Crafted(r.Id), r.MaxRank) : 0;
        }

        /// <summary>Seconds left on a brew (0 when inactive).</summary>
        public float BuffLeft(string id)
        {
            float t;
            return id != null && _buffs.TryGetValue(id, out t) ? t : 0f;
        }

        public IEnumerable<KeyValuePair<string, float>> ActiveBuffs { get { return _buffs; } }

        /// <summary>How many brews are running.</summary>
        public int ActiveBuffCount { get { return _buffs.Count; } }

        /// <summary>
        /// A number that changes whenever a brew starts or ends or the whole second shown for one ticks over, so the HUD
        /// rebuilds its brew line only then (reads the dictionary directly: no allocation).
        /// </summary>
        public int BuffClockKey
        {
            get
            {
                int key = _buffs.Count;
                foreach (KeyValuePair<string, float> kv in _buffs) key = key * 397 + kv.Key.GetHashCode() * 31 + Mathf.CeilToInt(kv.Value);
                return key;
            }
        }

        public GearItem EquippedCore { get { return Game.Gear != null ? Game.Gear.Equipped[(int)GearSlot.Core] : null; } }

        private static bool Prismatic { get { return Game.Skills != null && Game.Skills.Has(SkillDb.PrismaticInfusion); } }

        // ------------------------------------------------------------------ prices

        /// <summary>Forge fees discount: Forgewrights standing combined with the skill discount.</summary>
        public float FeeDiscount
        {
            get { return ForgeMath.CombineDiscounts(Game.Factions != null ? Game.Factions.FactionDiscount(ForgewrightsId) : 0f, SkillMods.Discount); }
        }

        public float WardenDiscount
        {
            get { return ForgeMath.CombineDiscounts(Game.Factions != null ? Game.Factions.FactionDiscount(WardensId) : 0f, SkillMods.Discount); }
        }

        /// <summary>The undiscounted cost of a recipe's next craft (the next rank for upgrades; null when maxed).</summary>
        public ForgeCost RecipeCost(RecipeDef r)
        {
            if (r == null) return null;
            if (r.Kind != RecipeKind.Upgrade) return r.Cost;
            int rank = UpgradeRank(r);
            return rank < r.MaxRank ? r.Steps[rank].Cost : null;
        }

        /// <summary>What the next craft costs after discounts (null when maxed).</summary>
        public ForgeCost RecipePrice(RecipeDef r)
        {
            ForgeCost c = RecipeCost(r);
            return c != null ? ForgeMath.WithDiscount(c, FeeDiscount) : null;
        }

        public ForgeCost GearPrice(GearOrder o)
        {
            return ForgeMath.WithDiscount(ForgeMath.GearCost(o), FeeDiscount);
        }

        // ------------------------------------------------------------------ crafting

        /// <summary>Why a recipe cannot be crafted at a station right now (null when it can).</summary>
        public string RecipeProblem(RecipeDef r, ForgeStation s)
        {
            if (r == null) return "Unknown recipe.";
            if (!ForgeMath.StationAllows(r, s)) return r.OutpostOnly ? "Only at the Frost Wardens outpost." : "Needs a full forge: Dante Rook's in Old Silver or the Frost Wardens outpost.";
            if (!Knows(r)) return LockReason(r);
            if (r.Kind == RecipeKind.Upgrade)
            {
                int rank = UpgradeRank(r);
                if (rank >= r.MaxRank) return "Fully upgraded.";
                UpgradeStep step = r.Steps[rank];
                if (RankOf(r.RankFaction) < step.Need)
                {
                    FactionDef f = FactionDb.Get(r.RankFaction);
                    return "Rank " + (rank + 1) + " needs " + (f != null ? f.Name : r.RankFaction) + " " + step.Need + ".";
                }
            }
            if (r.Kind == RecipeKind.Infusion)
            {
                GearItem core = EquippedCore;
                if (core == null) return "Equip a Weapon Core first (forge one on the GEAR tab).";
                if (core.Element == r.Element && (!Prismatic || CoreProc(core) >= ForgeMath.PrismaticCap - 0.001f)) return "Your Weapon Core is already tuned to " + GearSystem.ElementName(r.Element) + ".";
            }
            ForgeCost c = RecipePrice(r);
            if (c == null) return "Fully upgraded.";
            if (!ForgeMath.CanPay(c, _wallet)) return "Missing: " + string.Join(", ", ForgeMath.Missing(c, _wallet).ToArray());
            return null;
        }

        /// <summary>Crafts a recipe: pays, applies it and reports in message. Returns false (paying nothing) when it cannot.</summary>
        public bool Craft(RecipeDef r, ForgeStation s, out string message)
        {
            message = RecipeProblem(r, s);
            if (message != null) return false;
            ForgeCost c = RecipePrice(r);
            if (!ForgeMath.TryPay(c, _wallet))
            {
                message = "You can't pay for that.";
                return false;
            }
            switch (r.Kind)
            {
                case RecipeKind.Infusion: message = Infuse(r); break;
                case RecipeKind.Brew:
                    _buffs[r.Id] = r.Seconds;
                    message = r.Name + " is active for " + Clock(r.Seconds) + ".";
                    break;
                default:
                    message = r.Name + " rank " + (UpgradeRank(r) + 1) + " fitted.";
                    break;
            }
            Count(r.Id, 1);
            if (r.Kind == RecipeKind.Upgrade || r.Kind == RecipeKind.Brew)
            {
                if (Game.Skills != null) Game.Skills.RecomputeNow();
                // A new flask comes filled.
                PlayerController p = Game.Player;
                if (r.Id == "upg_tonic_flask" && p != null && p.Combat != null) p.Combat.Tonics = Mathf.Min(p.Combat.Tonics + 1, p.Combat.MaxTonics);
            }
            int rep = r.Kind == RecipeKind.Upgrade ? 2 : (r.Kind == RecipeKind.Infusion ? 1 : 0);
            if (rep > 0 && Game.Factions != null) Game.Factions.AddRep(ForgewrightsId, rep, "forged " + r.Name);
            Feedback();
            return true;
        }

        private string Infuse(RecipeDef r)
        {
            GearItem core = EquippedCore;
            if (core == null || Game.Gear == null) return "";
            Element old = core.Element;
            core.Element = r.Element;
            string oldName = GearSystem.ElementName(old);
            if (old != r.Element && !string.IsNullOrEmpty(oldName) && core.Name != null && core.Name.StartsWith(oldName + " "))
                core.Name = GearSystem.ElementName(r.Element) + core.Name.Substring(oldName.Length);
            string extra = "";
            if (Prismatic)
            {
                // Never lowers a Core that rolled more than the Prismatic cap; the bonus text shows only a real increase.
                float before = CoreProc(core);
                float after = ForgeMath.PrismaticProc(before);
                if (after > before + 0.0001f)
                {
                    SetCoreProc(core, after);
                    extra = " Prismatic: +" + Mathf.RoundToInt((after - before) * 100f) + "% elemental trigger chance.";
                }
            }
            Game.Gear.Recalculate();
            return core.Name + " now carries " + GearSystem.ElementName(r.Element) + "." + extra;
        }

        private static float CoreProc(GearItem it)
        {
            float v = 0f;
            foreach (Affix a in it.Affixes) if (a.Type == AffixType.ElementPower) v += a.Value;
            return v;
        }

        private static void SetCoreProc(GearItem it, float value)
        {
            for (int i = 0; i < it.Affixes.Count; i++)
            {
                if (it.Affixes[i].Type != AffixType.ElementPower) continue;
                it.Affixes[i] = new Affix { Type = AffixType.ElementPower, Value = value };
                return;
            }
            it.Affixes.Add(new Affix { Type = AffixType.ElementPower, Value = value });
        }

        /// <summary>Why a gear order cannot be forged at a station right now (null when it can).</summary>
        public string GearProblem(GearOrder o, ForgeStation s)
        {
            if (Game.Gear == null) return "Gear is not available.";
            if (!ForgeMath.StationForgesGear(s)) return "Gear needs a full forge: Dante Rook's in Old Silver or the Frost Wardens outpost.";
            if (o.Rarity > MaxRarity) return GearSystem.RarityName(o.Rarity) + " gear needs Forgewrights " + FactionRules.RankForRarity(o.Rarity) + ".";
            string p = ForgeMath.GearOrderProblem(o);
            if (p != null) return p;
            if (Game.Gear.Items.Count >= GearSystem.InventoryCap) return "Your gear inventory is full. Salvage something first.";
            ForgeCost c = GearPrice(o);
            if (!ForgeMath.CanPay(c, _wallet)) return "Missing: " + string.Join(", ", ForgeMath.Missing(c, _wallet).ToArray());
            return null;
        }

        /// <summary>Forges a designed gear piece into the inventory. Returns false (paying nothing) when it cannot.</summary>
        public bool ForgeGear(GearOrder o, ForgeStation s, out string message)
        {
            message = GearProblem(o, s);
            if (message != null) return false;
            if (!ForgeMath.TryPay(GearPrice(o), _wallet))
            {
                message = "You can't pay for that.";
                return false;
            }
            GearItem it = Game.Gear.GenerateCrafted(o.Slot, o.Rarity, Game.Gear.CurrentPower + 1, o.Set, o.Element);
            Game.Gear.Add(it, true);
            Count("gear_" + o.Rarity.ToString().ToLowerInvariant(), 1);
            if (Game.Factions != null)
            {
                Game.Factions.AddRep(ForgewrightsId, ForgeMath.GearRep(o.Rarity), "forged " + GearSystem.RarityName(o.Rarity) + " gear");
                if (s == ForgeStation.Outpost) Game.Factions.AddRep(WardensId, 1, "forged at the outpost");
            }
            message = "Forged " + it.Name + " (" + GearSystem.RarityName(it.Rarity) + " " + GearSystem.SlotName(it.Slot) + "). Equip it on the GEAR tab.";
            Feedback();
            return true;
        }

        private void Feedback()
        {
            if (Game.Audio != null) Game.Audio.Play2D(SfxId.MetalHit, 0.7f, 1.2f);
            Pad.Rumble(0.3f, 0.4f, 0.15f);
        }

        // ------------------------------------------------------------------ salvage

        public int SalvagePrice(string materialId)
        {
            return ForgeMath.SalvagePrice(MaterialDb.Get(materialId), SkillMods.SalvageBonus + FactionRules.SalvageRankBonus(ForgeRank));
        }

        /// <summary>Sells materials to the Forgewrights for cash (and a little of their goodwill).</summary>
        public bool Sell(string materialId, int count, out string message)
        {
            message = "";
            if (Game.Materials == null || Game.Economy == null) return false;
            count = Mathf.Min(count, Game.Materials.Count(materialId));
            if (count <= 0) { message = "You have none of those."; return false; }
            int cash = SalvagePrice(materialId) * count;
            if (!Game.Materials.Remove(materialId, count)) return false;
            Game.Economy.Earn(cash, "Salvage");
            if (Game.Factions != null) Game.Factions.AddRepExact(ForgewrightsId, cash / 500f, "sold materials");
            message = "Sold " + count + " " + MaterialDb.NameOf(materialId) + " for " + MathUtil.Money(cash) + ".";
            if (Game.Audio != null) Game.Audio.Play2D(SfxId.CashPickup, 0.6f);
            return true;
        }

        // ------------------------------------------------------------------ the Frost Wardens' shop

        public FactionRank WardenRank { get { return RankOf(WardensId); } }
        public bool WardenShopOpen { get { return WardenRank >= FactionRank.Friendly; } }
        public Rarity WardenRarity { get { return FactionRules.WardenRarity(WardenRank); } }

        public int WardenPrice(GearSlot slot)
        {
            Rarity r = WardenRarity;
            int basePrice = r >= Rarity.Legendary ? 11000 : (r >= Rarity.Epic ? 5200 : 2400);
            return ForgeMath.Discounted(basePrice, WardenDiscount);
        }

        public int WardenTeaCost { get { return ForgeMath.Discounted(WardenTeaPrice, WardenDiscount); } }

        public string WardenProblem(GearSlot slot)
        {
            if (!WardenShopOpen) return "The Wardens sell their gear to friends (Frost Wardens Friendly).";
            if (Game.Gear == null) return "Gear is not available.";
            if (Game.Gear.Items.Count >= GearSystem.InventoryCap) return "Your gear inventory is full. Salvage something first.";
            if (Game.Economy == null || Game.Economy.Money < WardenPrice(slot)) return "You need " + MathUtil.Money(WardenPrice(slot)) + ".";
            return null;
        }

        /// <summary>Buys a Frostline Warden set piece (cold-weather gear) for cash.</summary>
        public bool BuyWarden(GearSlot slot, out string message)
        {
            message = WardenProblem(slot);
            if (message != null) return false;
            if (!Game.Economy.Spend(WardenPrice(slot), "Frost Wardens")) { message = "You can't afford that."; return false; }
            Rarity r = WardenRarity;
            GearItem it = Game.Gear.GenerateCrafted(slot, r, Game.Gear.CurrentPower + 1, 1, Element.Frost);
            Game.Gear.Add(it, true);
            if (Game.Factions != null) Game.Factions.AddRep(WardensId, 1, "bought warden gear");
            message = "Bought " + it.Name + ". Equip it on the GEAR tab.";
            Feedback();
            return true;
        }

        public bool BuyWardenTea(out string message)
        {
            message = null;
            if (!WardenShopOpen) { message = "The Wardens sell to friends (Frost Wardens Friendly)."; return false; }
            if (Game.Economy == null || !Game.Economy.Spend(WardenTeaCost, "Frost Wardens")) { message = "You can't afford that."; return false; }
            RecipeDef tea = ForgeDb.Get("brew_frost_tea");
            _buffs["brew_frost_tea"] = tea != null ? tea.Seconds : 480f;
            if (Game.Skills != null) Game.Skills.RecomputeNow();
            message = "Frost Ward Tea is active for " + Clock(BuffLeft("brew_frost_tea")) + ".";
            return true;
        }

        // ------------------------------------------------------------------ modifiers

        /// <summary>Adds upgrade ranks and active brews to the modifier set (called by SkillTree.RecomputeNow).</summary>
        public void Accumulate(SkillModValues v)
        {
            foreach (RecipeDef r in ForgeDb.All)
            {
                int ranks = r.Kind == RecipeKind.Upgrade ? UpgradeRank(r) : (r.Kind == RecipeKind.Brew && BuffLeft(r.Id) > 0f ? 1 : 0);
                if (ranks <= 0) continue;
                foreach (SkillEffect e in r.Effects)
                {
                    if (e.Multiply)
                    {
                        float k = 1f;
                        for (int i = 0; i < ranks; i++) k *= e.PerRank;
                        v.Multiply(e.Stat, k);
                    }
                    else v.Add(e.Stat, e.PerRank * ranks);
                }
            }
        }

        // ------------------------------------------------------------------ frame

        private void Update()
        {
            if (!_built && Game.State != GameState.Loading)
            {
                _built = true;
                try { BuildStations(_layout); }
                catch (Exception e) { Log.Exception("Forge stations", e); }
            }
            if (_hookedQuests != Game.Quests)
            {
                if (_hookedQuests != null) _hookedQuests.QuestCompleted -= OnQuestCompleted;
                _hookedQuests = Game.Quests;
                if (_hookedQuests != null) _hookedQuests.QuestCompleted += OnQuestCompleted;
            }
            if (_buffs.Count == 0) return;
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            _tmp.Clear();
            foreach (KeyValuePair<string, float> kv in _buffs) _tmp.Add(kv.Key);
            bool expired = false;
            foreach (string id in _tmp)
            {
                float left = _buffs[id] - dt;
                if (left > 0f)
                {
                    _buffs[id] = left;
                    continue;
                }
                _buffs.Remove(id);
                expired = true;
                RecipeDef r = ForgeDb.Get(id);
                if (r != null && Game.Hud != null) Game.Hud.Notify(r.Name + " wore off.", 2.5f);
            }
            if (expired) SkillTree.MarkDirty();
        }

        private void OnQuestCompleted(string questId)
        {
            _tmp.Clear();
            ForgeDb.UnlocksFor(questId, _tmp);
            string[] ids = _tmp.ToArray();
            foreach (string id in ids) Grant(id, true);
        }

        private void Count(string id, int n)
        {
            _crafted[id] = Crafted(id) + n;
        }

        public static string Clock(float seconds)
        {
            int s = Mathf.Max(0, Mathf.CeilToInt(seconds));
            return (s / 60) + ":" + (s % 60).ToString("00");
        }

        /// <summary>Debug: knows every recipe.</summary>
        public void GrantAll()
        {
            foreach (RecipeDef r in ForgeDb.All) _granted.Add(r.Id);
        }

        // ------------------------------------------------------------------ IWorldSystem

        public void ResetAll()
        {
            _granted.Clear();
            _crafted.Clear();
            _buffs.Clear();
            if (_screen != null) _screen.CloseSilently();
            SkillTree.MarkDirty();
        }

        public void Capture(SaveData d)
        {
            d.Crafting = new CraftSave();
            foreach (string id in _granted) d.Crafting.Known.Add(id);
            foreach (KeyValuePair<string, int> kv in _crafted)
            {
                if (kv.Value <= 0) continue;
                d.Crafting.CraftedIds.Add(kv.Key);
                d.Crafting.CraftedCounts.Add(kv.Value);
            }
            foreach (KeyValuePair<string, float> kv in _buffs)
            {
                if (kv.Value <= 0f) continue;
                d.Crafting.BuffIds.Add(kv.Key);
                d.Crafting.BuffTimes.Add(kv.Value);
            }
        }

        public void Restore(SaveData d)
        {
            _granted.Clear();
            _crafted.Clear();
            _buffs.Clear();
            CraftSave c = d.Crafting;
            if (c != null)
            {
                if (c.Known != null) foreach (string id in c.Known) if (ForgeDb.Get(id) != null) _granted.Add(id);
                if (c.CraftedIds != null && c.CraftedCounts != null)
                {
                    int n = Mathf.Min(c.CraftedIds.Count, c.CraftedCounts.Count);
                    for (int i = 0; i < n; i++) if (!string.IsNullOrEmpty(c.CraftedIds[i]) && c.CraftedCounts[i] > 0) _crafted[c.CraftedIds[i]] = c.CraftedCounts[i];
                }
                if (c.BuffIds != null && c.BuffTimes != null)
                {
                    int n = Mathf.Min(c.BuffIds.Count, c.BuffTimes.Count);
                    for (int i = 0; i < n; i++)
                    {
                        RecipeDef r = ForgeDb.Get(c.BuffIds[i]);
                        if (r != null && r.Kind == RecipeKind.Brew && c.BuffTimes[i] > 0f) _buffs[r.Id] = Mathf.Min(c.BuffTimes[i], r.Seconds);
                    }
                }
            }
            // Quest unlocks registered by quest authors: catch up on quests finished in this save.
            if (Game.Quests != null)
                foreach (KeyValuePair<string, string> kv in ForgeDb.QuestUnlocks)
                    if (Game.Quests.IsDone(kv.Key)) _granted.Add(kv.Value);
            ProgressionHooks.AfterRestore(d);
        }

        // ------------------------------------------------------------------ adapters

        /// <summary>Pays from the player's cash (Economy) and material satchel (Game.Materials).</summary>
        private sealed class GameWallet : IForgeWallet
        {
            public int Cash { get { return Game.Economy != null ? Game.Economy.Money : 0; } }
            public int Count(string materialId) { return Game.Materials != null ? Game.Materials.Count(materialId) : 0; }
            public bool TakeCash(int amount) { return amount <= 0 || (Game.Economy != null && Game.Economy.Spend(amount, "Forge")); }

            public void GiveCash(int amount)
            {
                if (Game.Economy != null && amount > 0) Game.Economy.Money += amount;
            }

            public bool TakeMaterials(IList<KeyValuePair<string, int>> materials) { return Game.Materials != null && Game.Materials.Spend(materials); }
        }

        /// <summary>Reads the live game for the unlock rules.</summary>
        private sealed class GameContext : IForgeContext
        {
            private readonly CraftingSystem _forge;

            public GameContext(CraftingSystem forge)
            {
                _forge = forge;
            }

            public FactionRank RankOf(string factionId) { return CraftingSystem.RankOf(factionId); }
            public bool ThirdEye { get { return Game.Abilities != null && Game.Abilities.ThirdEyeUnlocked; } }
            public bool HasStoryFlag(string flag) { return Game.Missions != null && flag != null && Game.Missions.Flags.Contains(flag); }
            public bool QuestDone(string questId) { return Game.Quests != null && questId != null && Game.Quests.IsDone(questId); }
            public int StoryProgress { get { return Game.Missions != null ? Game.Missions.StoryProgress : 0; } }
            public bool Granted(string recipeId) { return _forge.IsGranted(recipeId); }
        }
    }
}
