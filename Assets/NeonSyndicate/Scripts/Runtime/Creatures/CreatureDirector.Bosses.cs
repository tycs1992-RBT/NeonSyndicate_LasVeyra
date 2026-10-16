using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>A boss arena: where a boss lives, what the place is called and when its fight can start.</summary>
    public sealed class BossArena
    {
        public string BossId;
        public string Name;
        public Vector3 Center;
        public float Radius;
        /// <summary>Only inside the Inner World (the Wraith Queen's Crystal Manor).</summary>
        public bool InnerOnly;
        /// <summary>Only after dark (the Mirage Djinn).</summary>
        public bool NightOnly;
        /// <summary>Shown when the arena is found but its fight cannot start yet.</summary>
        public string Hint;
        internal Blip Blip;
        /// <summary>The player stood inside while a mission or event ran (or a fight here just ended): step out first.</summary>
        internal bool NeedsExit;
        /// <summary>Game time until which the arena stays quiet (a mission or a world event nearby, and a short grace after).</summary>
        internal float QuietUntil;
    }

    /// <summary>
    /// Boss arenas and fights: the Glacier Titan in Titan Hollow, the Shard Golem Foreman in the quarry, the Mirage
    /// Djinn on the Mirage Flats deep in the Badlands (at night) and the Mirror Wraith Queen at Crystal Manor inside the
    /// Inner World. Coming near an arena finds it (a map icon); walking in once found starts the fight, never during a
    /// mission or a world event there nor for a few seconds after one, and a player who was already standing inside then
    /// must step out and walk back in. Leaving, dying or (Queen) leaving the Veil resets the fight. A defeated boss stays
    /// defeated (saved in CreatureSave.BossesDefeated); quests can spawn a boss anywhere with SpawnBoss (or Spawn), and
    /// the debug page can re-fight any.
    /// </summary>
    public sealed partial class CreatureDirector
    {
        private readonly List<BossArena> _arenas = new List<BossArena>();
        private readonly HashSet<string> _defeated = new HashSet<string>();
        private readonly HashSet<string> _found = new HashSet<string>();
        private readonly List<BodyJob> _prewarmed = new List<BodyJob>();
        private BossArena _fightArena;
        private NpcController _boss;
        private CreatureBoss _bossBrain;
        private bool _bossScripted;
        private float _bossTick;
        private float _bossEndedAt = -99f;
        private BossArena _prewarmArena;

        /// <summary>Every boss arena (debug page, map).</summary>
        public IList<BossArena> Arenas { get { return _arenas; } }
        /// <summary>The boss being fought right now (null when none).</summary>
        public CharacterBase ActiveBoss { get { return _boss != null && !_boss.IsDead ? _boss : null; } }
        /// <summary>The script of the boss being fought (phases for the boss bar).</summary>
        public CreatureBoss ActiveBossBrain { get { return _boss != null && !_boss.IsDead ? _bossBrain : null; } }

        public bool IsBossDefeated(string bossId) { return !string.IsNullOrEmpty(bossId) && _defeated.Contains(bossId); }
        public bool IsArenaFound(string bossId) { return !string.IsNullOrEmpty(bossId) && _found.Contains(bossId); }

        public static string BossTag(string bossId) { return "boss_" + bossId; }
        public static string BossAddsTag(string bossId) { return "boss_adds_" + bossId; }

        /// <summary>The same face every fight (and the Djinn's mirages share it).</summary>
        private static int BossSeed(string bossId)
        {
            int h = 17;
            for (int i = 0; i < bossId.Length; i++) h = h * 31 + bossId[i];
            return (h & 0x7fffffff) | 1;
        }

        public BossArena ArenaFor(string bossId)
        {
            for (int i = 0; i < _arenas.Count; i++) if (_arenas[i].BossId == bossId) return _arenas[i];
            return null;
        }

        private void InitBosses(CityLayout layout)
        {
            _arenas.Clear();
            _arenas.Add(new BossArena
            {
                BossId = "glacier_titan", Name = "Titan Hollow", Center = CityLayout.TitanHollowCenter, Radius = CityLayout.TitanHollowRadius * 0.82f,
                Hint = "The ice of the hollow groans under your feet."
            });
            SpecialSite q = layout != null ? layout.Site("Quarry") : null;
            if (q != null) _arenas.Add(new BossArena { BossId = "golem_foreman", Name = "the quarry floor", Center = q.Position, Radius = q.Radius * 0.8f, Hint = "Crystal dust hangs over the quarry floor." });
            SpecialSite m = layout != null ? layout.Site("CrystalManor") : null;
            if (m != null)
                _arenas.Add(new BossArena
                {
                    BossId = "wraith_queen", Name = "Crystal Manor", Center = m.Position, Radius = Mathf.Max(30f, m.Radius), InnerOnly = true,
                    Hint = "The manor's mirrors are empty out here. Look again through the Veil."
                });
            _arenas.Add(new BossArena
            {
                BossId = "mirage_djinn", Name = "the Mirage Flats", Center = FindDjinnFlats(layout), Radius = 50f, NightOnly = true,
                Hint = "Heat shimmer on cold sand. Something waits here for the night."
            });
        }

        /// <summary>
        /// A deterministic open spot deep in the south-east Badlands for the Djinn (about (1400, -1400) for the default
        /// city): desert ground far from roads, ridges, lots, landmarks and collectibles, searched from the far corner in.
        /// </summary>
        private static Vector3 FindDjinnFlats(CityLayout layout)
        {
            Vector3 fallback = new Vector3(1350f, 0f, -1350f);
            if (layout == null) return fallback;
            for (float depth = 1400f; depth >= 1050f; depth -= 50f)
            {
                for (int k = 0; k <= 12; k++)
                {
                    // Along the two edges of the square at this depth, nearest the diagonal first.
                    float off = (k + 1) / 2 * 50f;
                    Vector3 p = k % 2 == 0 ? new Vector3(depth, 0f, -depth + off) : new Vector3(depth - off, 0f, -depth);
                    if (Mathf.Min(p.x, -p.z) < 1000f) continue;
                    if (CityLayout.DistrictAt(p) != District.QuartzBadlands || CityLayout.IsFrostline(p.x, p.z)) continue;
                    if (layout.InsideRidge(p, 45f) || layout.NearAnyRoad(p, 70f)) continue;
                    if (NearLot(layout, p, 80f) || NearSite(layout, p, 160f) || NearSpot(layout.CreatureSpots, p, 60f)) continue;
                    return p;
                }
            }
            return fallback;
        }

        private static bool NearLot(CityLayout layout, Vector3 p, float margin)
        {
            for (int i = 0; i < layout.DesertLots.Count; i++) if (RectDistance(layout.DesertLots[i].Area, p) < margin) return true;
            return false;
        }

        private static bool NearSite(CityLayout layout, Vector3 p, float margin)
        {
            for (int i = 0; i < layout.Sites.Count; i++) if (MathUtil.FlatDistance(layout.Sites[i].Position, p) < layout.Sites[i].Radius + margin) return true;
            return false;
        }

        private static bool NearSpot(List<Vector3> spots, Vector3 p, float margin)
        {
            for (int i = 0; i < spots.Count; i++) if (MathUtil.FlatDistance(spots[i], p) < margin) return true;
            return false;
        }

        /// <summary>
        /// True inside (or within margin of) the arena of a boss not yet defeated: the ambient spawner keeps out until
        /// the boss falls (then the quarry and the hollow fill with ordinary wildlife).
        /// </summary>
        private bool InArena(Vector3 p, float margin)
        {
            for (int i = 0; i < _arenas.Count; i++)
            {
                BossArena a = _arenas[i];
                if (_defeated.Contains(a.BossId)) continue;
                if (MathUtil.FlatDistance(a.Center, p) < a.Radius + margin) return true;
            }
            return false;
        }

        // ================================================================== finding arenas, starting fights

        private void UpdateBosses(float dt)
        {
            _bossTick -= dt;
            if (_bossTick > 0f) return;
            _bossTick = 0.25f;
            bool mission = Game.Missions != null && Game.Missions.Active != null;
            // Every arena notes a mission or a world event near it, also while a fight runs elsewhere.
            for (int i = 0; i < _arenas.Count; i++)
            {
                BossArena a = _arenas[i];
                if (mission || EventNear(a)) a.QuietUntil = Time.time + CreatureFightRules.ArenaGrace;
            }
            if (_boss != null) { WatchFight(); return; }
            if (_bossBrain != null || (_boss == null && _fightArena != null)) ClearFightState();
            PlayerController p = Game.Player;
            if (p == null || p.IsDead || QaTools.Busy) return;
            Vector3 pp = Game.PlayerPosition;
            bool inner = Game.Inner != null && Game.Inner.IsInner;
            if (_prewarmArena != null && MathUtil.FlatDistance(pp, _prewarmArena.Center) > _prewarmArena.Radius + 220f)
            {
                // Walked away without a fight: drop the boss body built ahead.
                for (int i = 0; i < _prewarmed.Count; i++) _prewarmed[i].Cancelled = true;
                _prewarmed.Clear();
                _prewarmArena = null;
            }
            for (int i = 0; i < _arenas.Count; i++)
            {
                BossArena a = _arenas[i];
                float d = MathUtil.FlatDistance(pp, a.Center);
                if (d > a.Radius + 90f)
                {
                    a.NeedsExit = false;
                    continue;
                }
                bool quiet = Time.time < a.QuietUntil;
                // Found without fanfare during a mission or an event (the map icon still appears).
                if (!_found.Contains(a.BossId) && (!a.InnerOnly || inner || d < a.Radius)) FindArena(a, !quiet);
                if (_defeated.Contains(a.BossId)) continue;
                bool cooling = Time.time - _bossEndedAt < 8f;
                if (!cooling && d < a.Radius + 80f) PrewarmBoss(a);
                // Mere exploration of a welcome area cannot opt the player into its automatic boss fight.
                // Explicit mission/quest SpawnBoss and StartBossFight calls keep their existing semantics.
                bool can = !cooling && CanFightNow(a, false) && !StarterSafety.BlockAmbientAggression(pp);
                if (!CreatureFightRules.ArenaStarts(d, a.Radius * 0.72f, quiet, can, ref a.NeedsExit)) continue;
                // The debug page starts its own fight a moment after its teleport: never a second boss from walking in.
                if (_debugBoss != null) continue;
                StartFight(a, false);
                return;
            }
        }

        /// <summary>A world event is running at or near the arena (its fight waits, as it does during a mission).</summary>
        private static bool EventNear(BossArena a)
        {
            WorldEvents ev = Game.Events;
            if (ev == null || ev.Active == null) return false;
            IList<EventInstance> list = ev.Active;
            for (int i = 0; i < list.Count; i++)
            {
                EventInstance e = list[i];
                if (e != null && !e.Done && MathUtil.FlatDistance(e.Pos, a.Center) < a.Radius + 150f) return true;
            }
            return false;
        }

        /// <summary>An arena's fight may start now: found, not during a mission, in the right world and at the right hour.</summary>
        private bool CanFightNow(BossArena a, bool force)
        {
            if (force) return true;
            if (!_found.Contains(a.BossId)) return false;
            if (Game.Missions != null && Game.Missions.Active != null) return false;
            if (a.InnerOnly && (Game.Inner == null || !Game.Inner.IsInner)) return false;
            if (a.NightOnly && Game.Atmo != null && Game.Atmo.NightFactor < 0.55f) return false;
            return true;
        }

        /// <summary>Marks an arena found (a map icon; a quest can call RevealArena to point the player at it).</summary>
        private void FindArena(BossArena a, bool announce)
        {
            if (a == null) return;
            bool fresh = _found.Add(a.BossId);
            RefreshArenaBlip(a);
            if (!fresh || !announce) return;
            CreatureSpecies sp = CreatureSpeciesDb.Get(a.BossId);
            if (Game.Hud != null)
            {
                Game.Hud.Banner("BOSS ARENA FOUND", (sp != null ? sp.Name : "A boss") + " haunts " + a.Name + ".", new Color(1f, 0.55f, 0.35f), 4f);
                if (!CanFightNow(a, false) && !string.IsNullOrEmpty(a.Hint)) Game.Hud.Notify(a.Hint, 4f);
            }
            if (Game.Audio != null) Game.Audio.Play2D(SfxId.CrystalChime, 0.6f, 0.6f);
        }

        /// <summary>Puts an arena on the map (found by the player or pointed out by a quest).</summary>
        public void RevealArena(string bossId)
        {
            FindArena(ArenaFor(bossId), true);
        }

        private void RefreshArenaBlip(BossArena a)
        {
            if (!_found.Contains(a.BossId))
            {
                if (a.Blip != null) { Blip.Remove(a.Blip); a.Blip = null; }
                return;
            }
            CreatureSpecies sp = CreatureSpeciesDb.Get(a.BossId);
            bool beaten = _defeated.Contains(a.BossId);
            Color c = beaten ? new Color(0.55f, 0.55f, 0.6f) : new Color(1f, 0.35f, 0.3f);
            string label = (sp != null ? sp.Name : "Boss") + (beaten ? " (defeated)" : "") + ", " + a.Name;
            if (a.Blip == null) a.Blip = Blip.Add(BlipKind.Activity, null, a.Center, c, "B", label, false);
            a.Blip.Color = c;
            a.Blip.Label = label;
            a.Blip.Size = beaten ? 1f : 1.3f;
        }

        /// <summary>Starts a boss's arena fight (force: even if defeated, at any hour, in either world; the debug page).</summary>
        public CharacterBase StartBossFight(string bossId, bool force)
        {
            BossArena a = ArenaFor(bossId);
            if (a == null) return null;
            if (!force && (_defeated.Contains(bossId) || !CanFightNow(a, false))) return null;
            if (_boss != null) EndFight(true, false);
            if (!_found.Contains(bossId)) FindArena(a, false);
            return StartFight(a, true);
        }

        /// <summary>
        /// A quest's boss: spawns it with its full fight script (phases, rings, summons; it becomes ActiveBoss) wherever
        /// the quest says, with no arena rules. The options' tag, hostility, persistence, scales and name are kept; a
        /// Seed of 0 gives the boss its usual face (and the body built ahead near its arena, when there is one).
        /// </summary>
        public CharacterBase SpawnBoss(string bossId, Vector3 position, float yaw, CreatureSpawnOptions options)
        {
            CreatureSpecies sp = CreatureSpeciesDb.Get(bossId);
            if (sp == null || !sp.Boss) return null;
            if (_boss != null) EndFight(true, false);
            CreatureSpawnOptions o = options != null ? Copy(options) : new CreatureSpawnOptions { Persistent = true };
            NpcController n = SpawnBossBody(sp, position, yaw, o, position, 45f);
            if (n != null) _bossScripted = true;
            return n;
        }

        private CharacterBase StartFight(BossArena a, bool forced)
        {
            CreatureSpecies sp = CreatureSpeciesDb.Get(a.BossId);
            if (sp == null) return null;
            Vector3 pp = Game.PlayerPosition;
            Vector3 away = MathUtil.Flat(a.Center - pp);
            if (away.sqrMagnitude < 1f) away = Vector3.forward;
            away.Normalize();
            Vector3 pos = a.Center + away * a.Radius * 0.3f;
            float yaw = MathUtil.YawOf(-away);
            if (a.InnerOnly && Game.Inner != null && !Game.Inner.IsInner && forced) Game.Inner.Shift(true, false);
            NpcController n = SpawnBossBody(sp, pos, yaw, new CreatureSpawnOptions { Hostile = true, Persistent = true }, a.Center, a.Radius);
            if (n == null) return null;
            _fightArena = a;
            _bossScripted = false;
            return n;
        }

        /// <summary>The boss body and its fight script; o is the caller's own copy (empty tag, seed and name get the boss defaults).</summary>
        private NpcController SpawnBossBody(CreatureSpecies sp, Vector3 pos, float yaw, CreatureSpawnOptions o, Vector3 center, float radius)
        {
            if (string.IsNullOrEmpty(o.Tag)) o.Tag = BossTag(sp.Id);
            if (o.Seed == 0) o.Seed = BossSeed(sp.Id);
            if (string.IsNullOrEmpty(o.DisplayName)) o.DisplayName = sp.Name;
            NpcController n = SpawnBody(sp, pos, yaw, o, TakePrewarmed(sp.Id, o.Seed), false, null) as NpcController;
            if (n == null) return null;
            n.CreatureInnerOnly = false;
            _boss = n;
            _bossBrain = n.gameObject.AddComponent<CreatureBoss>();
            _bossBrain.Init(n, sp, center, radius, BossAddsTag(sp.Id));
            PrewarmAdds(sp.Id, o.Seed);
            if (Game.Fx != null)
            {
                Game.Fx.Flash(pos + Vector3.up * 3f, sp.HitColor, 8f, 30f, 0.8f);
                if (sp.HitSurface == ImpactSurface.Shadow) Game.Fx.ShadowBurst(pos + Vector3.up * 2f, sp.HitColor, 30, 3f);
                else if (sp.HitSurface == ImpactSurface.Glass) Game.Fx.GlassBurst(pos + Vector3.up * 2f, sp.HitColor, 40);
                else Game.Fx.RockBurst(pos + Vector3.up, sp.HitColor, 24, 3f);
            }
            if (Game.Cam != null) Game.Cam.Shake(0.6f);
            CreatureAudio.Play(sp, CreatureCry.Roar, pos + Vector3.up * 3f, 4f);
            if (Game.Hud != null) Game.Hud.Banner(sp.Name.ToUpper(), sp.Behaviour, new Color(1f, 0.4f, 0.3f), 4.5f);
            return n;
        }

        /// <summary>Bodies for the boss (when the player nears its arena) and for its summons and copies (when the fight starts).</summary>
        private void PrewarmBoss(BossArena a)
        {
            if (_prewarmArena == a) return;
            _prewarmArena = a;
            CreatureSpecies sp = CreatureSpeciesDb.Get(a.BossId);
            if (sp != null && FindPrewarmed(sp.Id, BossSeed(sp.Id)) == null) _prewarmed.Add(RequestBody(sp, BossSeed(sp.Id), false));
        }

        private void PrewarmAdds(string bossId, int seed)
        {
            switch (bossId)
            {
                case "glacier_titan": OrderCopies("snow_hollow", 0, 4); break;
                case "mirage_djinn": OrderCopies("mirage_djinn", seed, 3); OrderCopies("sand_golem", 0, 2); break;
                case "wraith_queen": OrderCopies("mirror_wraith", 0, 4); break;
                case "golem_foreman": SwarmBody(seed); break;
            }
        }

        private void OrderCopies(string speciesId, int seed, int count)
        {
            CreatureSpecies sp = CreatureSpeciesDb.Get(speciesId);
            if (sp == null) return;
            for (int i = 0; i < count; i++) _prewarmed.Add(RequestBody(sp, seed != 0 ? seed : _rng.Next(1, int.MaxValue / 2), false));
        }

        private BodyJob FindPrewarmed(string speciesId, int seed)
        {
            for (int i = 0; i < _prewarmed.Count; i++)
            {
                BodyJob j = _prewarmed[i];
                if (j.SpeciesId == speciesId && !j.Claimed && !j.Cancelled && (seed == 0 || j.Seed == seed)) return j;
            }
            return null;
        }

        /// <summary>A body ordered ahead for this species (and look seed, when given), or null to build on the spot.</summary>
        private BodyJob TakePrewarmed(string speciesId, int seed)
        {
            BodyJob j = FindPrewarmed(speciesId, seed);
            if (j == null) return null;
            _prewarmed.Remove(j);
            return j;
        }

        /// <summary>
        /// Spawns one of a boss's summons or copies (not mites: they come as a swarm through SpawnGroup), using a body
        /// ordered when the fight began when one is ready. The Djinn's mirages pass the boss's seed to share its face.
        /// </summary>
        internal CharacterBase SpawnBossAdd(string speciesId, Vector3 pos, float yaw, CreatureSpawnOptions o)
        {
            CreatureSpecies sp = CreatureSpeciesDb.Get(speciesId);
            if (sp == null) return null;
            try
            {
                BodyJob j = TakePrewarmed(speciesId, o.Seed);
                CharacterBase b = SpawnBody(sp, pos, yaw, o, j, false, null);
                // Order the next one on a worker thread now, so later phases find their bodies ready as well (the
                // Djinn's copies keep its face).
                if (j != null && b != null && _boss != null) _prewarmed.Add(RequestBody(sp, sp.Boss ? j.Seed : _rng.Next(1, int.MaxValue / 2), false));
                return b;
            }
            catch (System.Exception e)
            {
                Log.Exception("Boss summon " + speciesId, e);
                return null;
            }
        }

        /// <summary>
        /// A body ordered ahead for this summon or copy is built and waiting (the Djinn's hero-detail mirages wait for
        /// one rather than building on the main thread mid-fight).
        /// </summary>
        internal bool BossAddReady(string speciesId, int seed)
        {
            BodyJob j = FindPrewarmed(speciesId, seed);
            return j != null && IsDone(j) && !j.Failed;
        }

        // ================================================================== during the fight

        private void WatchFight()
        {
            if (_boss == null || _boss.IsDead) return;
            if (_bossScripted || _fightArena == null) return;
            PlayerController p = Game.Player;
            BossArena a = _fightArena;
            float d = MathUtil.FlatDistance(Game.PlayerPosition, a.Center);
            bool left = d > a.Radius + 75f;
            bool dead = p == null || p.IsDead || Game.State == GameState.Wasted || Game.State == GameState.Busted;
            bool veil = a.InnerOnly && (Game.Inner == null || !Game.Inner.IsInner);
            if (left || dead || veil) EndFight(false, true);
        }

        /// <summary>Ends the current fight without a winner: the boss and its summons leave; the arena can be tried again.</summary>
        public void EndBossFight()
        {
            EndFight(false, true);
        }

        private void EndFight(bool quiet, bool announce)
        {
            if (_boss != null && !_boss.IsDead)
            {
                CreatureSpecies sp = _boss.CreatureSpecies;
                if (announce && !quiet && Game.Hud != null && sp != null) Game.Hud.Notify("The " + sp.Name + " withdraws. Its arena waits for your return.", 4f);
                for (int i = _live.Count - 1; i >= 0; i--)
                {
                    if (_live[i] != _boss) continue;
                    _live.RemoveAt(i);
                }
                Remove(_boss, true);
                if (sp != null) DespawnTag(BossAddsTag(sp.Id));
            }
            // A player still standing in the arena has to step out before it can start again.
            if (_fightArena != null) _fightArena.NeedsExit = true;
            ClearFightState();
            _bossEndedAt = Time.time;
        }

        private void ClearFightState()
        {
            _boss = null;
            _bossBrain = null;
            _fightArena = null;
            _bossScripted = false;
            _prewarmArena = null;
            for (int i = 0; i < _prewarmed.Count; i++) _prewarmed[i].Cancelled = true;
            _prewarmed.Clear();
        }

        /// <summary>A boss died: it stays defeated, its summons scatter, and a rich drop lands where it fell.</summary>
        private void OnBossDied(CharacterBase body, CreatureSpecies sp, bool byPlayer)
        {
            if (sp == null || !sp.Boss) return;
            NpcController n = body as NpcController;
            if (n != null && n.CreatureMirage) return;
            bool fresh = _defeated.Add(sp.Id);
            DespawnTag(BossAddsTag(sp.Id));
            BossArena a = ArenaFor(sp.Id);
            if (a != null)
            {
                _found.Add(sp.Id);
                RefreshArenaBlip(a);
            }
            if (byPlayer) GearDrop(body.transform.position + Vector3.up * 0.5f, 1f, true);
            if (byPlayer && Game.Loot != null) Game.Loot.OnBossKilled(sp, body.transform.position + Vector3.up * 0.5f);
            // 04D: a real player victory grants the boss's first-win signature entitlement (once, durable) and the
            // arena's local quiet aftermath. Removal, retreat and cleanup never reach this method.
            if (byPlayer && Game.Arsenal != null) Game.Arsenal.OnBossVictory(sp.Id, body.transform.position + Vector3.up * 0.5f);
            if (byPlayer && Game.Encounters != null) Game.Encounters.OnBossVictory(sp.Id, a != null ? a.Center : body.transform.position, a != null ? a.Radius : 40f);
            if (Game.Hud != null) Game.Hud.Banner(sp.Name.ToUpper() + " DEFEATED", fresh ? "Its arena falls quiet. The bestiary entry is complete." : "Defeated again.", new Color(1f, 0.85f, 0.4f), 5f);
            if (Game.Compendium != null) Game.Compendium.MarkScanned(sp.Id, false);
            if (body == _boss)
            {
                _boss = null;
                _bossBrain = null;
                _fightArena = null;
                _bossEndedAt = Time.time;
            }
        }

        private void OnBossVeilShift(bool toInner)
        {
            // Inner World fights end when the Veil closes (handled by WatchFight); other fights carry on.
        }

        // ================================================================== saves

        private void ResetBosses()
        {
            if (_boss != null) EndFight(true, false);
            ClearFightState();
            _defeated.Clear();
            _found.Clear();
            for (int i = 0; i < _arenas.Count; i++) ResetArena(_arenas[i]);
        }

        /// <summary>A new game or a loaded save: the arena starts quiet for a moment (a save made inside it, say after a
        /// contract finished there, does not open the fight the instant it loads).</summary>
        private void ResetArena(BossArena a)
        {
            a.NeedsExit = false;
            a.QuietUntil = Time.time + CreatureFightRules.ArenaGrace;
            RefreshArenaBlip(a);
        }

        private void CaptureBosses(CreatureSave s)
        {
            s.BossesDefeated = new List<string>(_defeated);
            s.ArenasFound = new List<string>(_found);
        }

        private void RestoreBosses(CreatureSave s)
        {
            ClearFightState();
            _defeated.Clear();
            _found.Clear();
            if (s != null)
            {
                if (s.BossesDefeated != null) foreach (string id in s.BossesDefeated) if (!string.IsNullOrEmpty(id)) _defeated.Add(id);
                if (s.ArenasFound != null) foreach (string id in s.ArenasFound) if (!string.IsNullOrEmpty(id)) _found.Add(id);
            }
            for (int i = 0; i < _arenas.Count; i++) ResetArena(_arenas[i]);
        }
    }
}
