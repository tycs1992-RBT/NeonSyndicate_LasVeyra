using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    [Serializable]
    public sealed class FactionSave
    {
        public List<string> Ids = new List<string>();
        public List<int> Rep = new List<int>();
        // Progression work (appended): fractional carry parallel to Ids, and perks a guild has taught.
        public List<float> Carry = new List<float>();
        public List<string> Perks = new List<string>();
    }

    /// <summary>One HUD toast for a reputation change (several changes to one faction in a row merge into one).</summary>
    public sealed class RepToast
    {
        public string FactionId;
        public int Delta;
        public int Now;
        public string Reason;
        /// <summary>Set when the change crossed into a new rank.</summary>
        public bool RankChanged;
        public float Time;
        /// <summary>Bumped whenever the toast changes, so the HUD rebuilds its text only then (see TextVersion).</summary>
        public int Version;
        /// <summary>The Version the cached HUD text below was built for (-1: not built yet).</summary>
        public int TextVersion = -1;
        public string DeltaText;
        public string LineText;
        public string RankText;
    }

    /// <summary>
    /// Reputation with every faction (Game.Factions), -100 to +100, and what it does: ranks from Hunted to Family,
    /// a cost with each faction's rival whenever you gain, gang heat that follows syndicate standing (hostile gangs stay
    /// aggressive, friendly ones forgive minor offences and cool down faster), shop discounts on faction turf, and guild
    /// perks (Forge tiers, the Menders' free healing and extra tonic, the Frost Wardens' shop, the Frequency Order's Veil
    /// perk). Kills move reputation automatically; quests call AddRep. The FACTIONS page and the HUD toasts read it.
    /// </summary>
    public sealed partial class FactionSystem : MonoBehaviour, IWorldSystem
    {
        /// <summary>Faction id, old value, new value.</summary>
        public event Action<string, int, int> RepChanged;

        private readonly RepTrack _track = new RepTrack();
        private readonly HashSet<string> _perks = new HashSet<string>();
        private readonly List<RepChange> _changes = new List<RepChange>();
        private readonly List<KeyValuePair<string, float>> _deltas = new List<KeyValuePair<string, float>>();
        private readonly List<RepToast> _toasts = new List<RepToast>();
        private CreatureDirector _hookedCreatures;
        private float _tick;
        private Transform _root;
        private bool _built;
        /// <summary>In-game hours (Day x 24 + Hours) already forgiven up to; -1 until the clock is first read.</summary>
        private float _lastHour = -1f;
        /// <summary>Per gang: whether its heat is past the attack-on-sight line, and since when (Time.time).</summary>
        private readonly bool[] _atWar = new bool[8];
        private readonly float[] _warSince = new float[8];
        /// <summary>Guild contacts keep at least this far from every other prompt (and from the guild's quest giver).</summary>
        private const float ContactGap = 4f;
        /// <summary>QuestPlaces.GiverPosition stands a guild's quest giver this far to the right of its POI's door.</summary>
        private const float GiverSideOffset = 4.5f;

        /// <summary>Toasts currently on screen (newest last).</summary>
        public IList<RepToast> Toasts { get { return _toasts; } }

        public void Init()
        {
            WorldSystems.Register(this);
            MenuPages.Register(new MenuPage { Title = "FACTIONS", Order = 25, Draw = FactionsPage.Draw });
            HudOverlays.Register("faction_toasts", FactionsPage.DrawToasts);
            _root = new GameObject("Faction Contacts").transform;
            _root.SetParent(transform, false);
        }

        private void OnDestroy()
        {
            if (_hookedCreatures != null) _hookedCreatures.Killed -= OnCreatureKilled;
            _hookedCreatures = null;
        }

        // ------------------------------------------------------------------ reputation

        public int Rep(string factionId)
        {
            return _track.Get(factionId);
        }

        /// <summary>The value with its fractional carry (for bars).</summary>
        public float RepExact(string factionId)
        {
            return _track.Exact(factionId);
        }

        public FactionRank RankOf(string factionId)
        {
            return FactionRules.RankOf(Rep(factionId));
        }

        /// <summary>Changes reputation (clamped to -100..100) and tells listeners. Reason shows in the toast.</summary>
        public void AddRep(string factionId, int amount, string reason)
        {
            if (FactionDb.Get(factionId) == null || amount == 0) return;
            AddRepExact(factionId, amount, reason);
        }

        /// <summary>
        /// Changes reputation by a possibly fractional amount: gains are boosted by SkillMods.RepGain and cost the
        /// faction's rival half of the base gain; fractions accumulate until they make a whole point.
        /// </summary>
        public void AddRepExact(string factionId, float amount, string reason)
        {
            if (FactionDb.Get(factionId) == null || amount == 0f) return;
            _changes.Clear();
            _track.Apply(factionId, amount, SkillMods.RepGain, true, _changes);
            Raise(reason);
        }

        /// <summary>Debug: sets a value directly (no rival effect, no skill bonus).</summary>
        public void SetRep(string factionId, int value)
        {
            _changes.Clear();
            _track.Set(factionId, value, _changes);
            Raise("debug");
        }

        private void Raise(string reason)
        {
            Raise(reason, false);
        }

        /// <summary>Tells listeners about the collected changes. quiet shows a HUD toast only for a change of rank (the hourly grudge fade).</summary>
        private void Raise(string reason, bool quiet)
        {
            if (_changes.Count == 0) return;
            bool rankMoved = false;
            // Copy first: a listener may change reputation again.
            RepChange[] list = _changes.ToArray();
            foreach (RepChange c in list)
            {
                bool moved = FactionRules.RankOf(c.Old) != FactionRules.RankOf(c.New);
                rankMoved |= moved;
                if (!quiet || moved) AddToast(c, reason, moved);
                if (RepChanged == null) continue;
                foreach (Delegate d in RepChanged.GetInvocationList())
                {
                    try { ((Action<string, int, int>)d)(c.Id, c.Old, c.New); }
                    catch (Exception e) { Log.Exception("RepChanged listener", e); }
                }
            }
            if (rankMoved)
            {
                SkillTree.MarkDirty();
                ApplyGangTuning();
            }
        }

        private void AddToast(RepChange c, string reason, bool rankMoved)
        {
            int delta = c.New - c.Old;
            for (int i = _toasts.Count - 1; i >= 0; i--)
            {
                RepToast t = _toasts[i];
                if (t.FactionId != c.Id || t.Time < 0.6f) continue;
                t.Delta += delta;
                t.Now = c.New;
                t.RankChanged |= rankMoved;
                if (!string.IsNullOrEmpty(reason)) t.Reason = reason;
                t.Time = rankMoved ? 4.5f : 3f;
                t.Version++;
                _toasts.RemoveAt(i);
                _toasts.Add(t);
                return;
            }
            _toasts.Add(new RepToast { FactionId = c.Id, Delta = delta, Now = c.New, Reason = reason, RankChanged = rankMoved, Time = rankMoved ? 4.5f : 3f });
            if (_toasts.Count > 5) _toasts.RemoveAt(0);
            if (rankMoved && Game.Audio != null) Game.Audio.Play2D(SfxId.Notify, 0.5f, c.New > c.Old ? 1.15f : 0.8f);
        }

        // ------------------------------------------------------------------ automatic hooks

        /// <summary>
        /// The player killed a person: a gang member costs that syndicate reputation (its rival likes it). Scripted
        /// enemies (missions, rampages, story guards, quests and world events) and self-defense kills while the gang is at
        /// war with the player cost nothing (FactionRules.GangKillCounts), so normal play cannot spiral into Hunted.
        /// </summary>
        public void OnNpcKilled(NpcController n)
        {
            if (n == null || n.GangId < 0) return;
            int g = n.GangId;
            float heat = g < Gangs.Heat.Length ? Gangs.Heat[g] : 0f;
            float atWar = g < _atWar.Length && _atWar[g] ? Time.time - _warSince[g] : 0f;
            if (!FactionRules.GangKillCounts(IsScripted(n), heat, HeatFloorOf(g), atWar)) return;
            _deltas.Clear();
            bool silver = Game.Skills != null && Game.Skills.Has(SkillDb.SilverTongue);
            FactionRules.GangKill(n.GangId, n.Elite, silver, _deltas);
            ApplyDeltas(_deltas, "killed one of theirs", "a rival fell");
        }

        /// <summary>Mission, rampage, story, quest and world-event characters: Persistent, or tagged by what spawned them.</summary>
        private static bool IsScripted(NpcController n)
        {
            return n.Persistent || !string.IsNullOrEmpty(n.MissionTag);
        }

        /// <summary>The lowest heat a gang can have right now: its story floor, or its rank's floor (unless the Law of Power ended every grudge).</summary>
        private float HeatFloorOf(int gang)
        {
            float story = gang >= 0 && gang < Gangs.MinHeat.Length ? Gangs.MinHeat[gang] : 0f;
            FactionDef f = FactionDb.ForGang(gang);
            bool grudgesEnded = Game.Abilities != null && Game.Abilities.Power;
            float rank = f != null && !grudgesEnded ? FactionRules.HeatFloor(RankOf(f.Id)) : 0f;
            return Mathf.Max(story, rank);
        }

        /// <summary>Forgets every war clock; a gang already past the attack-on-sight line counts as at war since long ago.</summary>
        private void ResetWars()
        {
            for (int g = 0; g < _atWar.Length; g++)
            {
                _atWar[g] = g < Gangs.Heat.Length && Gangs.Heat[g] > FactionRules.AttackOnSightHeat;
                _warSince[g] = float.NegativeInfinity;
            }
        }

        /// <summary>Notes when each gang's heat crosses the attack-on-sight line (a war starts) and when it falls back.</summary>
        private void TrackWars()
        {
            int n = Mathf.Min(_atWar.Length, Gangs.Heat.Length);
            for (int g = 0; g < n; g++)
            {
                if (Gangs.Heat[g] <= FactionRules.AttackOnSightHeat) _atWar[g] = false;
                else if (!_atWar[g])
                {
                    _atWar[g] = true;
                    _warSince[g] = Time.time;
                }
            }
        }

        private void OnCreatureKilled(CreatureKill k)
        {
            if (!k.ByPlayer || k.Species == null) return;
            _deltas.Clear();
            FactionRules.CreatureKill(k.Species, _deltas);
            ApplyDeltas(_deltas, "killed a " + k.Species.Name, "killed a " + k.Species.Name);
        }

        private void ApplyDeltas(List<KeyValuePair<string, float>> deltas, string lossReason, string gainReason)
        {
            KeyValuePair<string, float>[] list = deltas.ToArray();
            foreach (KeyValuePair<string, float> kv in list) AddRepExact(kv.Key, kv.Value, kv.Value < 0f ? lossReason : gainReason);
        }

        private void Update()
        {
            // Guild contacts are placed once the boot has finished, clear of every shop and marker.
            if (!_built && Game.State != GameState.Loading)
            {
                _built = true;
                try { BuildContacts(); }
                catch (Exception e) { Log.Exception("Faction contacts", e); }
            }
            if (_hookedCreatures != Game.Creatures)
            {
                if (_hookedCreatures != null) _hookedCreatures.Killed -= OnCreatureKilled;
                _hookedCreatures = Game.Creatures;
                if (_hookedCreatures != null) _hookedCreatures.Killed += OnCreatureKilled;
            }
            float udt = Time.unscaledDeltaTime;
            for (int i = _toasts.Count - 1; i >= 0; i--)
            {
                // Toasts wait while the game is paused (menus, the Forge).
                if (Game.IsPlaying) _toasts[i].Time -= udt;
                if (_toasts[i].Time <= 0f) _toasts.RemoveAt(i);
            }
            TrackWars();
            _tick -= Time.deltaTime;
            if (_tick > 0f || !Game.IsPlaying) return;
            _tick = 0.5f;
            HourlyForgiveness();
            ApplyGangTuning();
            ForgiveMinorOffences();
        }

        /// <summary>
        /// Gang grudges fade: for every in-game hour that passes, every syndicate below 0 comes FactionRules.HourForgiveness
        /// closer to it (sleeping at a safehouse counts its hours). A frozen or rewound clock (QA captures, loads) only
        /// resynchronises, so it never hands out forgiveness. Only a change of rank shows a toast.
        /// </summary>
        private void HourlyForgiveness()
        {
            Atmosphere atmo = Game.Atmo;
            if (atmo == null) return;
            float now = atmo.Day * 24f + atmo.Hours;
            if (_lastHour < 0f || now < _lastHour || atmo.Frozen)
            {
                _lastHour = now;
                return;
            }
            int hours = Mathf.FloorToInt(now - _lastHour);
            if (hours <= 0) return;
            _lastHour += hours;
            hours = Mathf.Min(hours, 240);
            foreach (FactionDef f in FactionDb.All)
            {
                if (f.Kind != FactionKind.Syndicate) continue;
                float before = RepExact(f.Id);
                float after = before;
                for (int i = 0; i < hours && after < 0f; i++) after += FactionRules.HourForgiveness(after);
                if (after <= before) continue;
                _changes.Clear();
                _track.Apply(f.Id, after - before, 0f, false, _changes);
                Raise("old grudges fade", true);
            }
        }

        // ------------------------------------------------------------------ gang heat

        /// <summary>
        /// Syndicate standing drives gang heat: Hostile or worse keeps the heat above the attack-on-sight line (unless the
        /// Law of Power ended every grudge), Friendly or better scales offences down and cools heat faster.
        /// </summary>
        private void ApplyGangTuning()
        {
            bool grudgesEnded = Game.Abilities != null && Game.Abilities.Power;
            for (int g = 0; g < Gangs.Heat.Length; g++)
            {
                FactionDef f = FactionDb.ForGang(g);
                FactionRank r = f != null ? RankOf(f.Id) : FactionRank.Neutral;
                Gangs.SetTolerance(g, FactionRules.HeatScale(r), FactionRules.CoolScale(r));
                float floor = grudgesEnded ? 0f : FactionRules.HeatFloor(r);
                if (floor > 0f && Gangs.Heat[g] < floor) Gangs.Heat[g] = floor;
            }
        }

        /// <summary>
        /// Members of a Friendly (or better) syndicate who turned on the player over a minor offence (a gunshot nearby, a
        /// shove, a stray aim) calm down after a couple of seconds, unless the player actually hurt them or the whole gang
        /// is already out for blood. Scripted enemies (mission guards, rampage and event crews, quest foes) keep fighting.
        /// </summary>
        private void ForgiveMinorOffences()
        {
            PlayerController p = Game.Player;
            if (p == null) return;
            float now = Time.time;
            for (int i = 0; i < NpcController.All.Count; i++)
            {
                NpcController n = NpcController.All[i];
                if (n == null || n.IsDead || n.Role != NpcRole.Gang || n.GangId < 0 || !n.Hostile || IsScripted(n)) continue;
                if (n.Target == null || !n.Target.IsPlayer) continue;
                if (n.GangId < Gangs.Heat.Length && Gangs.Heat[n.GangId] > FactionRules.AttackOnSightHeat) continue;
                FactionDef f = FactionDb.ForGang(n.GangId);
                if (f == null || !FactionRules.ForgivesMinorOffences(RankOf(f.Id))) continue;
                bool hurt = n.Health != null && n.Health.LastAttacker == p.gameObject && now - n.Health.LastDamageTime < 8f;
                if (hurt) continue;
                if (n.HostileUntil > now + 2f) n.HostileUntil = now + 2f;
            }
        }

        // ------------------------------------------------------------------ perks and discounts

        public bool HasPerk(string perkId)
        {
            return perkId != null && _perks.Contains(perkId);
        }

        private bool CityRoyalty { get { return Game.Skills != null && Game.Skills.Has(SkillDb.CityRoyalty); } }

        /// <summary>A faction's own discount at its rank (doubled by City Royalty, at most 40%).</summary>
        public float FactionDiscount(string factionId)
        {
            float d = FactionRules.Discount(RankOf(factionId));
            return CityRoyalty ? Mathf.Min(0.4f, d * 2f) : d;
        }

        /// <summary>The best faction discount at shops in a district (the syndicates and companies that call it home).</summary>
        public float ShopDiscountAt(District d)
        {
            FactionDef f = ShopFactionAt(d);
            return f != null ? FactionDiscount(f.Id) : 0f;
        }

        /// <summary>The faction whose discount applies in a district (the best one), or null when nobody gives one.</summary>
        public FactionDef ShopFactionAt(District d)
        {
            FactionDef best = null;
            float bd = 0f;
            foreach (FactionDef f in FactionDb.All)
            {
                // Guilds price their own services (the Forge, the outpost) and the cult runs no shops.
                if (f.Home != d || (f.Kind != FactionKind.Syndicate && f.Kind != FactionKind.Corporate)) continue;
                float v = FactionRules.Discount(RankOf(f.Id));
                if (v > bd) { bd = v; best = f; }
            }
            return best;
        }

        /// <summary>Shop header line: "Ember Court turf: 10% off (Trusted)", or empty.</summary>
        public string ShopDiscountText(District d)
        {
            FactionDef f = ShopFactionAt(d);
            if (f == null) return "";
            return f.Name + " turf: " + Mathf.RoundToInt(FactionDiscount(f.Id) * 100f) + "% off (" + RankOf(f.Id) + ")";
        }

        /// <summary>Adds guild and cult perks to the modifier set (called by SkillTree.RecomputeNow).</summary>
        public void Accumulate(SkillModValues v)
        {
            FactionRank menders = RankOf("menders");
            if (menders >= FactionRank.Trusted) v.Add(SkillStat.TonicCharges, 1f);
            if (menders >= FactionRank.Family) v.Add(SkillStat.TonicHeal, FactionRules.MendersTonicHealFamily);
            FactionRank order = RankOf("frequency_order");
            if (HasPerk(FactionRules.VeilAttunement) && !FactionRules.Refuses(order))
            {
                v.Multiply(SkillStat.VeilDrain, FactionRules.VeilAttuneDrain);
                v.Multiply(SkillStat.VeilShiftCost, FactionRules.VeilAttuneShift);
                if (order >= FactionRank.Trusted) v.Multiply(SkillStat.VeilDrain, FactionRules.OrderTrustedDrain);
                if (order >= FactionRank.Family) v.Multiply(SkillStat.VeilShiftCost, FactionRules.OrderFamilyShift);
            }
            if (RankOf("frost_wardens") >= FactionRank.Family) v.Add(SkillStat.FrostResist, FactionRules.WardenFamilyFrost);
            FactionRank cult = RankOf("hollow_cult");
            if (cult == FactionRank.Friendly) v.Add(SkillStat.PowerDamage, FactionRules.CultFriendlyPower);
            else if (cult == FactionRank.Trusted) v.Add(SkillStat.PowerDamage, FactionRules.CultTrustedPower);
            else if (cult == FactionRank.Family)
            {
                v.Add(SkillStat.PowerDamage, FactionRules.CultFamilyPower);
                v.Multiply(SkillStat.VeilDrain, FactionRules.CultFamilyDrain);
            }
        }

        // ------------------------------------------------------------------ guild contacts

        private void BuildContacts()
        {
            CityLayout L = Game.Layout;
            if (L == null) return;
            Poi mara = L.FindPoi("Mara Solane");
            if (mara != null) Contact(mara, "Menders", "Press E to see the Menders (Mara Solane's people)", UseMenders, true);
            Poi iris = L.FindPoi("Iris Moon");
            if (iris != null) Contact(iris, "Frequency Order", "Press E to speak with the Frequency Order", UseOrder, false);
        }

        private void Contact(Poi poi, string name, string prompt, Action use, bool menders)
        {
            Vector3 at = PedManager.Ground(poi.Position);
            Vector3 right = MathUtil.RightOf(poi.Facing.sqrMagnitude > 0.01f ? poi.Facing.normalized : Vector3.forward);
            Vector3 spot;
            // Beside the story contact (whose mission marker stands on the exact spot), never on top of it, and on the
            // left of the door like Dante Rook's forge: the guild's quest giver stands to the right (it may be placed
            // later), so that spot is kept clear by ContactGap too. A crowded corner falls back to the usual 2.2 m gap,
            // then a step further out, and only then to the old rule (better a close prompt than no contact at all).
            Vector3 giver = at + right * GiverSideOffset;
            if (!ForgeStations.FindSpot(at, -right, 2.8f, ContactGap, giver, ContactGap, out spot) &&
                !ForgeStations.FindSpot(at, -right, 2.8f, ForgeStations.PromptGap, giver, ContactGap, out spot) &&
                !ForgeStations.FindSpot(at, -right, 4.2f, ForgeStations.PromptGap, giver, ContactGap, out spot))
            {
                if (!ForgeStations.FindSpot(at, -right, 2.8f, out spot))
                {
                    Log.Warn("Factions: no free spot beside " + poi.Name + ", so the " + name + " contact was not placed.");
                    return;
                }
                Log.Info("Factions: the " + name + " contact had to stand close to the guild's quest giver at " + poi.Name + ".");
            }
            // The prop stands between the prompt and the door, on ground already known to be open.
            Vector3 toDoor = MathUtil.Flat(at - spot);
            Vector3 prop = spot + (toDoor.sqrMagnitude > 0.01f ? toDoor.normalized : -right) * (menders ? 0.6f : 0.7f);
            if (menders) ForgeStations.MendersSign(prop, MathUtil.YawOf(poi.Facing), _root);
            else
            {
                ForgeStations.ListeningCrystal(prop, _root);
                Blip b = Blip.Add(BlipKind.Service, null, spot, new Color(0.62f, 0.45f, 1f), "~", "Frequency Order (Iris Moon)", false);
                b.Size = 0.9f;
            }
            Interactable it = Interactable.Create(name + " contact", spot, 1.8f, prompt, use, _root);
            it.Condition = () => !CombatDirector.InCombat && (Game.Wanted == null || Game.Wanted.Stars == 0);
        }

        private void UseMenders()
        {
            if (Game.Menus == null || Game.Player == null) return;
            FactionRank r = RankOf("menders");
            bool free = r >= FactionRank.Friendly;
            int price = free ? 0 : FactionRules.MendersHealPrice;
            string text = "The Menders accept restitution even when treatment is refused. Standing: " + r + " (" + Rep("menders") + ").\n" +
                          (free ? "As a friend you are healed for free, tonics refilled." : "Healing costs " + MathUtil.Money(price) + ". Friends of the Menders are treated for free.") +
                          "\nA donation of $500 earns their trust.";
            Game.Menus.ShowChoice("THE MENDERS", text, new[] { free ? "Heal (free)" : "Heal (" + MathUtil.Money(price) + ")", "Donate $500", "Leave" }, i =>
            {
                PlayerController p = Game.Player;
                if (p == null) return;
                if (i == 0)
                {
                    if (FactionRules.Refuses(RankOf("menders"))) { if(Game.Hud!=null)Game.Hud.Notify("Treatment requires Neutral standing. Donations still restore trust.",4f); return; }
                    if (price > 0 && (Game.Economy == null || !Game.Economy.Spend(price, "Menders"))) return;
                    p.Health.Heal(p.Health.MaxHealth);
                    p.Health.Burning = 0f;
                    if (free && p.Combat != null) p.Combat.RefillTonics();
                    if (Game.Audio != null) Game.Audio.Play2D(SfxId.HealthPickup, 0.7f);
                    if (Game.Hud != null) Game.Hud.Notify(free ? "The Menders patch you up and refill your tonics." : "The Menders patch you up.", 3f);
                }
                else if (i == 1)
                {
                    if (Game.Economy == null || !Game.Economy.Spend(500, "Donation")) return;
                    AddRep("menders", 3, "donation");
                }
            });
        }

        private void UseOrder()
        {
            if (Game.Menus == null || Game.Player == null) return;
            FactionRank r = RankOf("frequency_order");
            bool learned = HasPerk(FactionRules.VeilAttunement);
            bool canLearn = !learned && r >= FactionRank.Friendly;
            int shards = Game.Materials != null ? Game.Materials.Count("rift_shard") : 0;
            string text = "Iris Moon's mystics listen to the Inner World through the static. Standing: " + r + " (" + Rep("frequency_order") + ").\n" +
                          (learned ? "You know Veil Attunement." : (canLearn ? "They will teach you Veil Attunement." : "Become Friendly and they will teach you Veil Attunement.")) +
                          "\nRift-shard offerings are accepted at every standing and restore trust (you carry " + shards + ").";
            string learn = learned ? "Attunement (known)" : (canLearn ? "Learn Veil Attunement" : "Attunement (Friendly)");
            Game.Menus.ShowChoice("THE FREQUENCY ORDER", text, new[] { learn, "Offer a Rift Shard", "Leave" }, i =>
            {
                if (i == 0)
                {
                    if (!canLearn)
                    {
                        if (Game.Hud != null) Game.Hud.Notify(learned ? "You already hear the Veil the way they do." : "The Order teaches only its friends.", 3f);
                        return;
                    }
                    _perks.Add(FactionRules.VeilAttunement);
                    SkillTree.MarkDirty();
                    if (Game.Hud != null) Game.Hud.Banner("VEIL ATTUNEMENT", "The Veil drains less and opens more easily.", new Color(0.62f, 0.45f, 1f), 4f);
                    if (Game.Audio != null) Game.Audio.Play2D(SfxId.CrystalChime, 0.7f, 0.9f);
                }
                else if (i == 1)
                {
                    if (Game.Materials == null || !Game.Materials.Remove("rift_shard", 1))
                    {
                        if (Game.Hud != null) Game.Hud.Notify("You have no rift shards. Rifts leave them behind when they close.", 3f);
                        return;
                    }
                    AddRep("frequency_order", 3, "a rift shard");
                }
            });
        }

        // ------------------------------------------------------------------ IWorldSystem

        public void ResetAll()
        {
            _track.Clear();
            _perks.Clear();
            _toasts.Clear();
            _lastHour = -1f;
            ResetWars();
            Gangs.ResetTolerance();
            SkillTree.MarkDirty();
        }

        public void Capture(SaveData d)
        {
            d.Factions = new FactionSave();
            _track.Capture(d.Factions);
            foreach (string p in _perks) d.Factions.Perks.Add(p);
        }

        public void Restore(SaveData d)
        {
            _track.Restore(d.Factions);
            _perks.Clear();
            if (d.Factions != null && d.Factions.Perks != null) foreach (string p in d.Factions.Perks) if (!string.IsNullOrEmpty(p)) _perks.Add(p);
            _toasts.Clear();
            _lastHour = -1f;
            ApplyGangTuning();
            // The save's gang heat is already loaded: a gang that was at war stays at war (no fresh grace period).
            ResetWars();
            ProgressionHooks.AfterRestore(d);
        }
    }
}
