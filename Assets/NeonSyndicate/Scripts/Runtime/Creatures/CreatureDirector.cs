using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>Options for spawning a creature from code (missions, events, debug).</summary>
    public sealed class CreatureSpawnOptions
    {
        /// <summary>Mission or event tag: AliveWithTag and DespawnTag find creatures by it.</summary>
        public string Tag;
        /// <summary>Hunts the player at once instead of following its temperament.</summary>
        public bool Hostile = true;
        /// <summary>Never despawned for being far away (mission and event creatures).</summary>
        public bool Persistent;
        public bool Elite;
        public float HealthScale = 1f;
        public float DamageScale = 1f;
        /// <summary>Optional name shown on its health bar (bosses and named beasts).</summary>
        public string DisplayName;
        public int Seed;
        /// <summary>Starter habitat residents stay calm until actually damaged. Runtime spawn option only.</summary>
        public bool CalmUntilProvoked;
        /// <summary>Exhibition and practice bodies give no kill, part, loot or progression rewards.</summary>
        public bool NoRewards;
        /// <summary>A nonlethal sparring body: a winning hit leaves one health until the habitat removes it.</summary>
        public bool PracticeTarget;
    }

    /// <summary>A creature died.</summary>
    public struct CreatureKill
    {
        public CharacterBase Body;
        public CreatureSpecies Species;
        public bool ByPlayer;
        public Vector3 Position;
        public string Tag;
    }

    [Serializable]
    public sealed class CreatureSave
    {
        public List<string> Seen = new List<string>();
        public List<string> Scanned = new List<string>();
        public List<string> KillIds = new List<string>();
        public List<int> KillCounts = new List<int>();
        public List<string> BossesDefeated = new List<string>();
        // ---- Appended by the creature runtime (old saves load these as empty lists).
        /// <summary>Boss arenas the player has found: they show on the map, and walking in starts the fight.</summary>
        public List<string> ArenasFound = new List<string>();
        /// <summary>Breakable parts the player has broken, as "species:part" (the bestiary lists them). Old saves: empty.</summary>
        public List<string> PartsBroken = new List<string>();
    }

    /// <summary>
    /// The creature system (Game.Creatures): spawns creatures for the ecology, missions and events, tracks the living,
    /// and reports kills. Spawns of non-humanoid species get their bodies built on worker threads (the creature waits,
    /// hidden, for a few frames when nothing was ready); humanoid creatures are NPCs in creature mode. Deaths give the
    /// player loot (species table plus SkillMods.MaterialFind), Lumen and bestiary kills. Split into
    /// CreatureDirector.Bodies.cs (worker-thread builds), .Ecology.cs (the ambient spawner), .Bosses.cs (arenas and
    /// boss fights) and .Debug.cs (the F2 "Creatures" page).
    /// </summary>
    public sealed partial class CreatureDirector : MonoBehaviour, IWorldSystem
    {
        /// <summary>Raised whenever a creature dies (any cause).</summary>
        public event Action<CreatureKill> Killed;

        /// <summary>Missions can switch the ambient ecology off while they run.</summary>
        public bool AmbientEnabled = true;

        /// <summary>Ambient creatures further than this from the player are removed (mission creatures never are).</summary>
        public const float DespawnDistance = 230f;

        // Every body this director spawned that still exists: living, dying, or (non-humanoids) waiting for its mesh.
        private readonly List<CharacterBase> _live = new List<CharacterBase>();
        private CityLayout _layout;
        private System.Random _rng = new System.Random(4127);
        private GRand _lootRng = new GRand(90017);
        private readonly List<KeyValuePair<string, int>> _lootScratch = new List<KeyValuePair<string, int>>();
        private bool _innerHooked;
        private float _pruneTimer;
        private float _farTimer;

        /// <summary>Every creature body alive, dying or still waiting for its mesh (read only; both kinds).</summary>
        public IList<CharacterBase> Live { get { return _live; } }

        public void Init(CityLayout layout)
        {
            _layout = layout;
            WorldSystems.Register(this);
            Safe("creature audio", () => CreatureAudio.Init(transform));
            Safe("creature bodies", InitBodies);
            Safe("creature ecology", () => InitEcology(layout));
            Safe("creature bosses", () => InitBosses(layout));
            Safe("creature debug page", RegisterDebugPage);
            Safe("creature HUD", CreatureHud.Register);
            HookInner();
        }

        private static void Safe(string what, Action a)
        {
            try { a(); }
            catch (Exception e) { Log.Exception("CreatureDirector " + what, e); }
        }

        private void HookInner()
        {
            if (_innerHooked || Game.Inner == null) return;
            Game.Inner.Shifted += OnVeilShift;
            _innerHooked = true;
        }

        private void OnDestroy()
        {
            if (_innerHooked && Game.Inner != null) Game.Inner.Shifted -= OnVeilShift;
            _innerHooked = false;
            DestroyBodyCaches();
        }

        // ================================================================== spawning

        /// <summary>
        /// Spawns one creature of a species; returns its body (a Creature, or an NpcController for humanoids). A boss
        /// species gets its full fight script (SpawnBoss). A non-humanoid joins a live group of its species with the same
        /// tag within 25 m, or starts one (quests and events that spawn a pack one by one still get pack and swarm
        /// rules; mites share the swarm's body).
        /// </summary>
        public CharacterBase Spawn(string speciesId, Vector3 position, float yaw, CreatureSpawnOptions options)
        {
            CreatureSpecies sp = CreatureSpeciesDb.Get(speciesId);
            if (sp == null)
            {
                Log.Warn("Creatures: unknown species '" + speciesId + "'");
                return null;
            }
            try
            {
                if (sp.Boss) return SpawnBoss(speciesId, position, yaw, options);
                CreatureSpawnOptions o = options ?? new CreatureSpawnOptions();
                CreatureGroup g = sp.IsHumanoid ? null : GroupFor(sp, o, position);
                BodyJob shared = sp.Id == "glass_mite" && g != null ? SwarmBody(g.BodySeed) : null;
                return SpawnBody(sp, position, yaw, o, shared, false, g);
            }
            catch (Exception e)
            {
                Log.Exception("Creature spawn " + speciesId, e);
                return null;
            }
        }

        /// <summary>Metres within which a creature spawned on its own joins a live group of its species and tag.</summary>
        private const float JoinRadius = 25f;

        /// <summary>The live group of this species and tag within JoinRadius, or a new one for the next spawn to join.</summary>
        private CreatureGroup GroupFor(CreatureSpecies sp, CreatureSpawnOptions o, Vector3 position)
        {
            float r2 = JoinRadius * JoinRadius;
            for (int i = 0; i < _live.Count; i++)
            {
                Creature c = _live[i] as Creature;
                if (c == null || c.IsDead || c.Group == null || c.Species != sp || !SameTag(c.Tag, o.Tag)) continue;
                if (MathUtil.FlatSqrDistance(c.transform.position, position) > r2) continue;
                return c.Group;
            }
            CreatureGroup g = NewGroup(sp, position);
            g.BodySeed = o.Seed != 0 ? o.Seed : _rng.Next(1, int.MaxValue);
            return g;
        }

        private static bool SameTag(string a, string b)
        {
            return string.IsNullOrEmpty(a) ? string.IsNullOrEmpty(b) : a == b;
        }

        /// <summary>Spawns a group around a centre point (packs, swarms, waves).</summary>
        public List<CharacterBase> SpawnGroup(string speciesId, Vector3 center, int count, CreatureSpawnOptions options)
        {
            List<CharacterBase> list = new List<CharacterBase>();
            CreatureSpecies sp = CreatureSpeciesDb.Get(speciesId);
            if (sp == null || count <= 0)
            {
                if (sp == null) Log.Warn("Creatures: unknown species '" + speciesId + "'");
                return list;
            }
            CreatureSpawnOptions o = options ?? new CreatureSpawnOptions();
            try
            {
                CreatureGroup g = sp.IsHumanoid ? null : NewGroup(sp, center);
                int bodySeed = o.Seed != 0 ? o.Seed : _rng.Next(1, int.MaxValue);
                if (g != null) g.BodySeed = bodySeed;
                BodyJob shared = sp.Id == "glass_mite" ? SwarmBody(bodySeed) : null;
                float spacing = GroupSpacing(sp);
                for (int i = 0; i < count; i++)
                {
                    Vector3 p = GroupSlot(center, i, count, spacing);
                    float yaw = MathUtil.YawOf(p - center) + (float)_rng.NextDouble() * 40f - 20f;
                    CreatureSpawnOptions each = o;
                    if (o.Seed != 0)
                    {
                        each = Copy(o);
                        each.Seed = o.Seed + i * 7919;
                    }
                    CharacterBase c = SpawnBody(sp, p, yaw, each, shared, false, g);
                    if (c != null) list.Add(c);
                }
            }
            catch (Exception e) { Log.Exception("Creature group " + speciesId, e); }
            return list;
        }

        private static CreatureSpawnOptions Copy(CreatureSpawnOptions o)
        {
            return new CreatureSpawnOptions
            {
                Tag = o.Tag, Hostile = o.Hostile, Persistent = o.Persistent, Elite = o.Elite, HealthScale = o.HealthScale,
                DamageScale = o.DamageScale, DisplayName = o.DisplayName, Seed = o.Seed,
                CalmUntilProvoked = o.CalmUntilProvoked, NoRewards = o.NoRewards, PracticeTarget = o.PracticeTarget
            };
        }

        private CreatureGroup NewGroup(CreatureSpecies sp, Vector3 home)
        {
            CreatureGroup g = new CreatureGroup();
            g.SpeciesId = sp.Id;
            g.Home = home;
            g.Swarm = sp.Temperament == Temperament.Swarm;
            return g;
        }

        /// <summary>Metres between pack members (mites cluster, big bodies spread out).</summary>
        private static float GroupSpacing(CreatureSpecies sp)
        {
            if (sp.Id == "glass_mite") return 0.7f;
            if (sp.GreatBeast || sp.Boss) return 9f;
            if (sp.IsHumanoid) return sp.Humanoid == HumanoidCreature.ShardGolem || sp.Humanoid == HumanoidCreature.SandGolem ? 4f : 2.2f;
            return 2.6f;
        }

        /// <summary>Member i of n on a loose spiral around the centre (the first stands on it).</summary>
        private static Vector3 GroupSlot(Vector3 center, int i, int n, float spacing)
        {
            if (i == 0 || n <= 1) return center;
            float a = i * 2.39996f;
            float r = spacing * Mathf.Sqrt(i);
            return center + new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r);
        }

        /// <summary>
        /// Creates the body. ready carries a body built ahead on a worker thread (or the swarm's shared body); without one
        /// a non-humanoid waits hidden for its worker build and a humanoid builds its mesh now.
        /// </summary>
        private CharacterBase SpawnBody(CreatureSpecies sp, Vector3 pos, float yaw, CreatureSpawnOptions o, BodyJob ready, bool ambient, CreatureGroup group)
        {
            int seed = o.Seed != 0 ? o.Seed : _rng.Next(1, int.MaxValue);
            pos = PlaceOnGround(sp, pos);
            bool inner = Game.Inner != null && Game.Inner.IsInner && !o.Persistent;
            CharacterBase body;
            bool built = ready != null && BodyReady(ready) && !ready.Failed && (ready.Shared || !ready.Claimed);
            if (sp.IsHumanoid)
            {
                int detail = HumanoidDetail(sp);
                Appearance look;
                if (built && ready.Humanoid && ready.HData != null && ready.Detail == detail)
                {
                    look = ready.HLook;
                    ready.Claimed = true;
                    BodyFactory.Offer(look, detail, ready.HData);
                }
                else
                {
                    look = HumanoidLook(sp, seed);
                    // A body ordered ahead that is not finished yet: drop it (this one builds now).
                    if (ready != null && !ready.Shared && !ready.Claimed) ready.Cancelled = true;
                }
                NpcController n = NpcController.SpawnCreature(sp, look, pos, yaw, o);
                n.CreatureAmbient = ambient;
                n.CreatureInnerOnly = inner;
                body = n;
            }
            else
            {
                Creature c = Creature.Spawn(sp, pos, yaw, o, null, null);
                c.Seed = seed;
                c.Ambient = ambient;
                c.InnerOnly = inner;
                if (group != null) group.Add(c);
                if (built && !ready.Humanoid && ready.Body != null)
                {
                    if (!ready.Shared) ready.Claimed = true;
                    c.Materialize(ready.Body, ready.Shared ? ready.SharedMesh : null);
                }
                else WaitForBody(c, sp, seed, o.Elite, ready);
                body = c;
            }
            _live.Add(body);
            return body;
        }

        /// <summary>The look a humanoid creature spawns with (a Mirror Wraith wears the player's shape).</summary>
        private static Appearance HumanoidLook(CreatureSpecies sp, int seed)
        {
            System.Random r = new System.Random(seed);
            if (sp.Humanoid == HumanoidCreature.MirrorWraith && Game.Player != null && Game.Player.Rig != null)
                return Appearance.MirrorWraith(Game.Player.Rig.Look);
            return Appearance.ForCreature(sp.Humanoid, r);
        }

        private static int HumanoidDetail(CreatureSpecies sp)
        {
            return sp.Boss ? 2 : BodyFactory.AmbientDetail;
        }

        /// <summary>Puts a spawn on the ground (flyers given a point well above it keep their height).</summary>
        private static Vector3 PlaceOnGround(CreatureSpecies sp, Vector3 pos)
        {
            float g;
            if (GroundAt(pos, 3f, out g))
            {
                if (!(sp.Flies && pos.y > g + 2.5f)) pos.y = g;
            }
            else if (GroundAt(pos, 80f, out g)) pos.y = g;
            return pos;
        }

        /// <summary>Ground height under p, casting from up metres above it.</summary>
        public static bool GroundAt(Vector3 p, float up, out float y)
        {
            RaycastHit hit;
            if (Physics.Raycast(new Vector3(p.x, p.y + up, p.z), Vector3.down, out hit, up + 60f, Layers.WorldMask, QueryTriggerInteraction.Ignore))
            {
                y = hit.point.y;
                return true;
            }
            y = p.y;
            return false;
        }

        // ================================================================== queries

        /// <summary>
        /// True when a shot from muzzle toward aim (its hit point, or the end of its path) passes close to a living
        /// creature within range of the muzzle, calm ones included: the player is clearly shooting at wildlife.
        /// </summary>
        public bool CreatureInLineOfFire(Vector3 muzzle, Vector3 aim, float range)
        {
            float r2 = range * range;
            for (int i = 0; i < _live.Count; i++)
            {
                CharacterBase b = _live[i];
                if (b == null || b.IsDead) continue;
                Vector3 c = b.ChestPosition;
                if ((c - muzzle).sqrMagnitude > r2) continue;
                if (CreatureFightRules.NearShotLine(muzzle, aim, c, 2.5f + Mathf.Max(0.4f, b.BodyRadius))) return true;
            }
            return false;
        }

        /// <summary>True when a living hostile creature is within radius (police ignore gunfire during creature fights).</summary>
        public bool HostileNear(Vector3 position, float radius)
        {
            float r2 = radius * radius;
            // Calm creatures count only close by (a shot fired at a sleeping golem is still a creature fight).
            float calm = Mathf.Min(radius, 25f);
            float c2 = calm * calm;
            for (int i = 0; i < _live.Count; i++)
            {
                CharacterBase b = _live[i];
                if (b == null || b.IsDead) continue;
                float d2 = MathUtil.FlatSqrDistance(b.transform.position, position);
                if (d2 > r2) continue;
                if (d2 <= c2 || InFight(b)) return true;
            }
            return false;
        }

        /// <summary>A creature that is fighting, chasing, stalking or warning someone right now.</summary>
        private static bool InFight(CharacterBase b)
        {
            Creature c = b as Creature;
            if (c != null) return c.Ready && (c.Mode == CreatureMode.Hunt || c.Mode == CreatureMode.Warn || c.Mode == CreatureMode.Stalk || c.Provoked);
            NpcController n = b as NpcController;
            return n != null && (n.State == NpcState.Fight || n.CreatureWarning || n.CreatureProvoked);
        }

        public int AliveWithTag(string tag)
        {
            if (string.IsNullOrEmpty(tag)) return 0;
            int n = 0;
            for (int i = 0; i < _live.Count; i++)
            {
                CharacterBase b = _live[i];
                if (b != null && !b.IsDead && TagOf(b) == tag) n++;
            }
            return n;
        }

        /// <summary>The living creatures with a tag (fills into, cleared first).</summary>
        public void WithTag(string tag, List<CharacterBase> into)
        {
            if (into == null) return;
            into.Clear();
            for (int i = 0; i < _live.Count; i++)
            {
                CharacterBase b = _live[i];
                if (b != null && !b.IsDead && TagOf(b) == tag) into.Add(b);
            }
        }

        /// <summary>
        /// Removes every creature with a tag. When the boss being fought has it (a quest cleaning up its boss), the fight
        /// ends properly first: its summons and copies go with it and the arena is left to try again.
        /// </summary>
        public void DespawnTag(string tag)
        {
            if (string.IsNullOrEmpty(tag)) return;
            if (_boss != null && !_boss.IsDead && TagOf(_boss) == tag) EndFight(true, false);
            for (int i = _live.Count - 1; i >= 0; i--)
            {
                CharacterBase b = _live[i];
                if (b == null) { _live.RemoveAt(i); continue; }
                if (TagOf(b) != tag) continue;
                Remove(b, true);
                _live.RemoveAt(i);
            }
        }

        /// <summary>Living creatures right now (bodies still waiting for their mesh included).</summary>
        public int AliveCount
        {
            get
            {
                int n = 0;
                for (int i = 0; i < _live.Count; i++) if (_live[i] != null && !_live[i].IsDead) n++;
                return n;
            }
        }

        public static CreatureSpecies SpeciesOf(CharacterBase b)
        {
            Creature c = b as Creature;
            if (c != null) return c.Species;
            NpcController n = b as NpcController;
            return n != null ? n.CreatureSpecies : null;
        }

        public static string TagOf(CharacterBase b)
        {
            Creature c = b as Creature;
            if (c != null) return c.Tag;
            NpcController n = b as NpcController;
            return n != null ? n.CreatureTag : null;
        }

        private static bool IsAmbient(CharacterBase b)
        {
            Creature c = b as Creature;
            if (c != null) return c.Ambient;
            NpcController n = b as NpcController;
            return n != null && n.CreatureAmbient;
        }

        private static bool IsPersistent(CharacterBase b)
        {
            Creature c = b as Creature;
            if (c != null) return c.Persistent;
            NpcController n = b as NpcController;
            return n != null && n.Persistent;
        }

        private static bool IsInnerOnly(CharacterBase b)
        {
            Creature c = b as Creature;
            if (c != null) return c.InnerOnly;
            NpcController n = b as NpcController;
            return n != null && n.CreatureInnerOnly;
        }

        private static bool EngagedWithPlayer(CharacterBase b)
        {
            Creature c = b as Creature;
            if (c != null) return c.IsEngaged;
            NpcController n = b as NpcController;
            return n != null && n.CreatureEngaged;
        }

        private static bool IsEliteBody(CharacterBase b)
        {
            Creature c = b as Creature;
            if (c != null) return c.Elite;
            NpcController n = b as NpcController;
            return n != null && n.CreatureOptions != null && n.CreatureOptions.Elite;
        }

        /// <summary>
        /// How tall a creature the camera should frame right now (0 when none): the lock target when it is a creature,
        /// else the biggest boss or great beast fighting the player within 40 m.
        /// </summary>
        public float FramingSize(Vector3 playerPos, CharacterBase locked)
        {
            float best = 0f;
            if (locked != null && !locked.IsDead && SpeciesOf(locked) != null) best = Mathf.Max(locked.BodyHeight, locked.BodyRadius);
            CharacterBase boss = ActiveBoss;
            if (boss != null && MathUtil.FlatDistance(boss.transform.position, playerPos) < 45f) best = Mathf.Max(best, boss.BodyHeight);
            for (int i = 0; i < Creature.All.Count; i++)
            {
                Creature c = Creature.All[i];
                if (!c.IsBossLike || !c.IsEngaged) continue;
                if (MathUtil.FlatDistance(c.transform.position, playerPos) > 40f) continue;
                best = Mathf.Max(best, Mathf.Max(c.BodyHeight, c.BodyRadius));
            }
            return best;
        }

        // ================================================================== deaths, loot and rewards

        /// <summary>
        /// Every creature death comes here (Creature.OnDied and creature NPCs): bestiary kill, materials from the species
        /// table, Lumen, career stats, the Killed event, and boss bookkeeping.
        /// </summary>
        public void ReportDeath(CharacterBase body, CreatureSpecies sp, bool byPlayer, DamageInfo info)
        {
            if (body == null) return;
            Creature creature = body as Creature;
            NpcController humanoid = body as NpcController;
            CreatureSpawnOptions options = creature != null ? creature.Options : (humanoid != null ? humanoid.CreatureOptions : null);
            // Both body rigs obey NoRewards. Suppress quest-counting Killed as for the existing practice targets.
            // Humanoid OnKilled still runs in its caller, independently of this reward/reporting path.
            if (options != null && options.NoRewards) return;
            Vector3 at = body.ChestPosition;
            string tag = TagOf(body);
            try
            {
                if (byPlayer && sp != null)
                {
                    bool elite = IsEliteBody(body);
                    if (Game.Compendium != null) Game.Compendium.RecordKill(sp.Id);
                    GiveLoot(sp, elite, at);
                    GiveLumen(sp, elite, at);
                    if (Game.Stats != null)
                    {
                        Game.Stats.Add(StatId.Kills, 1);
                        if (info.Headshot) Game.Stats.Add(StatId.Headshots, 1);
                    }
                    if (Game.Abilities != null) Game.Abilities.OnKill();
                    if (Game.Gear != null && (elite || sp.GreatBeast) && !sp.Boss) GearDrop(at, elite ? 0.35f : 0.6f, false);
                    if (Game.Loot != null && !sp.Boss) Game.Loot.OnCreatureKilled(sp, elite, at);
                    CreatureParts.OnOwnerKilled(body, sp, at);
                }
                OnBossDied(body, sp, byPlayer);
            }
            catch (Exception e) { Log.Exception("Creature death rewards", e); }
            RaiseKilled(new CreatureKill { Body = body, Species = sp, ByPlayer = byPlayer, Position = body.transform.position, Tag = tag });
        }

        private void GiveLoot(CreatureSpecies sp, bool elite, Vector3 at)
        {
            if (Game.Materials == null || sp.Loot == null || sp.Loot.Length == 0) return;
            _lootScratch.Clear();
            CreatureLoot.Roll(sp.Loot, SkillMods.MaterialFind, elite, _lootRng, _lootScratch);
            for (int i = 0; i < _lootScratch.Count; i++) Game.Materials.Add(_lootScratch[i].Key, _lootScratch[i].Value, true);
            if (_lootScratch.Count > 0 && Game.Audio != null) Game.Audio.PlayAt(SfxId.CrystalChime, at, 0.35f, 1.3f, 30f);
        }

        /// <summary>Lumen for a kill: the species' value, more for elites, the difficulty and gear bonuses on top.</summary>
        public static int LumenFor(CreatureSpecies sp, bool elite)
        {
            if (sp == null) return 0;
            float v = sp.Lumen * (elite ? 3f : 1f);
            if (Settings.Difficulty == DifficultyLevel.Veteran) v *= 1.25f;
            if (Game.Gear != null) v *= Game.Gear.LumenMultiplier;
            // The Mind tree's Lumen Siphon applies to creature kills the same way it does to people.
            v *= SkillMods.LumenGain;
            return Mathf.Max(0, Mathf.RoundToInt(v));
        }

        private static void GiveLumen(CreatureSpecies sp, bool elite, Vector3 at)
        {
            int v = LumenFor(sp, elite);
            if (v > 0 && Game.Progress != null) Game.Progress.GrantLumen(at, v);
        }

        /// <summary>A gear drop in a light beam at a kill (bosses and great beasts are generous).</summary>
        private void GearDrop(Vector3 at, float chance, bool boss)
        {
            GearSystem g = Game.Gear;
            if (g == null || _rng.NextDouble() > chance) return;
            GearSlot slot = (GearSlot)_rng.Next(GearSystem.SlotCount);
            Rarity r = g.RollRarity(true, boss ? 0.6f : 0.2f);
            if (boss && r < Rarity.Epic) r = Rarity.Epic;
            g.SpawnDrop(at, g.Generate(slot, r, g.CurrentPower + (boss ? 3 : 1)), WeaponId.Count);
        }

        public void RaiseKilled(CreatureKill k)
        {
            if (Killed == null) return;
            foreach (Delegate d in Killed.GetInvocationList())
            {
                try { ((Action<CreatureKill>)d)(k); }
                catch (Exception e) { Log.Exception("Creature killed handler", e); }
            }
        }

        // ================================================================== frame

        private void Update()
        {
            float dt = Time.deltaTime;
            if (!_innerHooked) HookInner();
            // Each part guarded on its own (no delegates, so nothing is allocated per frame).
            try { PollBodies(); }
            catch (Exception e) { Log.Exception("CreatureDirector bodies", e); }
            _pruneTimer -= dt;
            if (_pruneTimer <= 0f)
            {
                _pruneTimer = 0.5f;
                for (int i = _live.Count - 1; i >= 0; i--) if (_live[i] == null) _live.RemoveAt(i);
            }
            _farTimer -= dt;
            if (_farTimer <= 0f)
            {
                _farTimer = 1f;
                try
                {
                    DespawnFar();
                    // Golden shots and the perf flythrough need the same world every run: no wildlife wandering in.
                    bool qa = QaTools.Busy;
                    if (qa && !_qaCleared) ClearForQa();
                    _qaCleared = qa;
                }
                catch (Exception e) { Log.Exception("CreatureDirector despawn", e); }
            }
            if (!Game.IsPlaying) return;
            try { UpdateEcology(dt); }
            catch (Exception e)
            {
                Log.Exception("CreatureDirector ecology", e);
                _nextAttempt = Time.time + 10f;
            }
            try { UpdateBosses(dt); }
            catch (Exception e)
            {
                Log.Exception("CreatureDirector bosses", e);
                _bossTick = 2f;
            }
        }

        /// <summary>Creatures the player left far behind fade away (mission creatures are Persistent); dead bodies finish their own dissolve.</summary>
        private void DespawnFar()
        {
            Vector3 pp = Game.PlayerPosition;
            float d2 = DespawnDistance * DespawnDistance;
            for (int i = _live.Count - 1; i >= 0; i--)
            {
                CharacterBase b = _live[i];
                if (b == null) { _live.RemoveAt(i); continue; }
                // The boss being fought runs its script to the end (leaving its arena ends an arena fight instead).
                if (b.IsDead || IsPersistent(b) || b == _boss) continue;
                if (MathUtil.FlatSqrDistance(b.transform.position, pp) < d2) continue;
                Remove(b, false);
                _live.RemoveAt(i);
            }
        }

        private bool _qaCleared;

        /// <summary>A QA capture started: every creature that is not part of a mission or a boss fight is removed.</summary>
        private void ClearForQa()
        {
            for (int i = _live.Count - 1; i >= 0; i--)
            {
                CharacterBase b = _live[i];
                if (b == null) { _live.RemoveAt(i); continue; }
                if (IsPersistent(b)) continue;
                Remove(b, false);
                _live.RemoveAt(i);
            }
            CancelPlans();
        }

        /// <summary>The Veil opened or closed: Inner World beings fade with it, and the city's calm wildlife stays behind.</summary>
        private void OnVeilShift(bool toInner)
        {
            for (int i = _live.Count - 1; i >= 0; i--)
            {
                CharacterBase b = _live[i];
                if (b == null) { _live.RemoveAt(i); continue; }
                if (b.IsDead) continue;
                bool remove;
                if (toInner) remove = !IsInnerOnly(b) && IsAmbient(b) && !IsPersistent(b) && !EngagedWithPlayer(b);
                else remove = IsInnerOnly(b);
                if (!remove) continue;
                Remove(b, true);
                _live.RemoveAt(i);
            }
            // Groups still being built belong to the world just left (no city wildlife in the Veil, no Hollows in the city).
            CancelPlans();
            _nextAttempt = Time.time + 2f;
            OnBossVeilShift(toInner);
        }

        /// <summary>Removes one body now (an optional puff of smoke or sparks).</summary>
        private void Remove(CharacterBase b, bool puff)
        {
            if (b == null) return;
            Creature c = b as Creature;
            if (c != null)
            {
                c.Despawn(puff);
                return;
            }
            if (puff && Game.Fx != null && !b.IsDead)
            {
                if (b.Health.Surface == ImpactSurface.Shadow) Game.Fx.ShadowBurst(b.ChestPosition, b.Health.SurfaceTint, 10, 1f);
                else if (b.Health.Surface == ImpactSurface.Glass) Game.Fx.GlassBurst(b.ChestPosition, b.Health.SurfaceTint, 14);
                else Game.Fx.RockBurst(b.ChestPosition, b.Health.SurfaceTint, 8, 0.8f);
            }
            Destroy(b.gameObject);
        }

        /// <summary>Removes every creature at once (debug, loading, a new game).</summary>
        public void DespawnAll(bool puff)
        {
            for (int i = _live.Count - 1; i >= 0; i--) if (_live[i] != null) Remove(_live[i], puff);
            _live.Clear();
            CancelBodyJobs();
            ClearEcologyPlans();
        }

        /// <summary>Kills every living creature (debug): deaths, loot and Lumen as if the player did it.</summary>
        public void KillAll()
        {
            List<CharacterBase> copy = new List<CharacterBase>(_live);
            for (int i = 0; i < copy.Count; i++)
            {
                CharacterBase b = copy[i];
                if (b == null || b.IsDead) continue;
                Creature c = b as Creature;
                if (c != null && !c.Ready) { Remove(c, false); continue; }
                GameObject src = Game.Player != null ? Game.Player.gameObject : null;
                // Past every guard and burrow: the filter steps aside for the killing blow.
                IDamageFilter filter = b.Health.Filter;
                b.Health.Filter = null;
                b.Health.Kill(src, DamageType.Explosion);
                if (b != null && b.Health != null) b.Health.Filter = filter;
            }
        }

        // ================================================================== saves

        public void ResetAll()
        {
            DespawnAll(false);
            AmbientEnabled = true;
            ResetBosses();
        }

        public void Capture(SaveData d)
        {
            if (d.Creatures == null) d.Creatures = new CreatureSave();
            CaptureBosses(d.Creatures);
        }

        public void Restore(SaveData d)
        {
            DespawnAll(false);
            RestoreBosses(d.Creatures);
        }
    }
}
