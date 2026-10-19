using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// The Resonance loop at runtime: guns charge powers, powers prime enemies, guns detonate primed enemies.
    /// Gun and melee hits on hostiles refill the crystal bar (ResonanceRules: about the same per second for every gun,
    /// capped per second). Offensive powers leave a short mark (Kindled, Stormlit, Unmoored, Glassbound) shown as a
    /// glowing ring and crystal shell on the target. The first gun hit on a marked foe sets off a Resonance Burst:
    /// x2.5 damage (bosses x1.75), a splash by mark, a refilled magazine (a crystal caster vents its heat instead), a
    /// gold HUD marker, a synthesized boom and three short first-time hints. CharacterHealth calls BurstStateFor,
    /// OnBurst and OnPlayerHit; the powers call Prime. Bursts raise BurstFired (gear sets can listen).
    /// </summary>
    public sealed partial class ResonanceSystem : MonoBehaviour
    {
        /// <summary>Raised after every Resonance Burst (the target and its mark). Gear sets and gadgets may listen.</summary>
        public static event Action<CharacterBase, ResonanceState> BurstFired;

        private static ResonanceSystem _instance;
        private static readonly ResonanceMarkBook _book = new ResonanceMarkBook();
        private static readonly Dictionary<int, CharacterBase> _bodies = new Dictionary<int, CharacterBase>();
        private static readonly ChargeBucket _bucket = new ChargeBucket(ResonanceRules.MaxChargePerSecond);
        private static readonly List<int> _ids = new List<int>();
        private static readonly List<CharacterBase> _work = new List<CharacterBase>();
        private static float _energyPulse;
        private static float _burstMarkerUntil;
        private static ResonanceState _burstMarkerState;
        private static int _bursts;
        private static float _energyGained;

        public static void ResetStatics()
        {
            _instance = null;
            BurstFired = null;
            _book.Clear();
            _bodies.Clear();
            _bucket.Reset();
            _ids.Clear();
            _work.Clear();
            _energyPulse = 0f;
            _burstMarkerUntil = 0f;
            _burstMarkerState = ResonanceState.None;
            _bursts = 0;
            _energyGained = 0f;
            ResetHudStatics();
            ResetChains04N();_serial04N=0;
        }

        /// <summary>Creates the system (boot step "Resonance"): the mark visuals, the sounds, HUD overlays, the F2 page.</summary>
        public static void Ensure()
        {
            if (_instance != null) return;
            GameObject go = new GameObject("Resonance");
            _instance = go.AddComponent<ResonanceSystem>();
            _instance.Init();
        }

        private void Init()
        {
            try { CombatSounds.Init(transform); }
            catch (Exception e) { Log.Exception("Combat sounds", e); }
            LoadHints();
            InitChains04N();
            HudOverlays.Register("resonance", DrawHud);
            HudOverlays.Register("damage_numbers", DamageNumbers.Draw);
            HudOverlays.Register("creature_parts", CreatureParts.DrawOverlay);
            DebugPages.Register(new MenuPage { Title = "Combat loop", Order = 49, Draw = DrawDebug });
        }

        /// <summary>0 to 1 glow on the crystal bar after an energy gain (the HUD draws it).</summary>
        public static float EnergyPulse { get { return _energyPulse; } }
        public static int BurstCount { get { return _bursts; } }
        public static float EnergyGained { get { return _energyGained; } }

        // ================================================================== marks

        /// <summary>True for bosses and great beasts (smaller burst multiplier, shorter marks).</summary>
        public static bool IsBossLike(CharacterBase c)
        {
            // 04E: an Underheart boss is marked through its lock-on stand-in (ResonanceSystem.UnderBoss04E).
            if (c is UBossLockProxy04E) return true;
            Creature cr = c as Creature;
            if (cr != null) return cr.IsBossLike;
            NpcController n = c as NpcController;
            return n != null && n.IsCreature && (n.IsBossCreature || n.CreatureSpecies.GreatBeast);
        }

        /// <summary>The mark currently on a character (None when unmarked).</summary>
        public static ResonanceState StateOf(CharacterBase c)
        {
            if (c == null) return ResonanceState.None;
            return _book.StateOf(c.GetInstanceID(), Time.time);
        }

        /// <summary>0 to 1 of a mark's time left.</summary>
        public static float MarkRemaining(CharacterBase c)
        {
            return c == null ? 0f : _book.Remaining(c.GetInstanceID(), Time.time);
        }

        /// <summary>Marks a living hostile with a power's state (the newest mark replaces the old one).</summary>
        public static void Prime(CharacterBase c, ResonanceState s)
        {
            if (c == null || c.IsDead || c.IsPlayer || s == ResonanceState.None || !(c.IsHostileTo() || UnderBossMarkable(c))) return;
            int id = c.GetInstanceID();
            bool fresh = _book.StateOf(id, Time.time) != s;
            _book.Prime(id, s, Time.time, ResonanceRules.MarkDuration(IsBossLike(c)));
            _bodies[id] = c;
            if (fresh)
            {
                Color col = ResonanceRules.StateColor(s);
                if (Game.Fx != null) Game.Fx.Flash(c.ChestPosition, col, 2f, 4f, 0.2f);
                CombatSounds.Play(CombatSounds.Clip.Prime, c.ChestPosition, 0.45f, 1f + 0.06f * (int)s, 0.8f, 50f);
            }
            HintPrimed();
        }

        /// <summary>Marks a power's targets by its spell id (Abilities.Spells).</summary>
        public static void PrimeSpell(CharacterBase c, string spellId)
        {
            Prime(c, ResonanceRules.StateForSpell(spellId));
        }

        /// <summary>Marks up to max hostiles within radius of center (in front of the camera when frontOnly), nearest first.</summary>
        public static int PrimeAround(Vector3 center, float radius, int max, ResonanceState s, bool frontOnly)
        {
            PlayerController p = Game.Player;
            if (p == null) return 0;
            _work.Clear();
            _work.AddRange(LootCombat.FoesAround(center, radius, p.gameObject));
            // 04E: the running Underheart boss is a candidate too (its body measured to the nearest hit volume).
            AddUnderBossCandidate(center, radius, _work);
            Vector3 fwd = Game.Cam != null ? Game.Cam.FlatForward : p.transform.forward;
            _work.Sort((a, b) => (a.transform.position - center).sqrMagnitude.CompareTo((b.transform.position - center).sqrMagnitude));
            int n = 0;
            for (int i = 0; i < _work.Count && n < max; i++)
            {
                CharacterBase c = _work[i];
                if (frontOnly && Vector3.Dot(MathUtil.Flat(c.transform.position - center).normalized, fwd) < 0.2f) continue;
                Prime(c, s);
                if (StateOf(c) == s) n++;
            }
            _work.Clear();
            return n;
        }

        // ================================================================== bursts

        /// <summary>The mark a hit would detonate: only the player's gun rounds (with a weapon) set off a burst.</summary>
        public static ResonanceState BurstStateFor(CharacterBase target, DamageInfo info)
        {
            if (target == null || !ResonanceRules.Detonates(info.Type, info.WeaponId, info.FromPlayer)) return ResonanceState.None;
            return _book.StateOf(target.GetInstanceID(), Time.time);
        }

        /// <summary>
        /// A burst just landed on target (CharacterHealth already applied the multiplier): the mark is consumed, the
        /// splash and the effects play, the gun's magazine refills and the HUD and listeners hear about it.
        /// burstHit is the multiplied hit before armour.
        /// </summary>
        public static void OnBurst(CharacterBase target, DamageInfo info, ResonanceState s, float burstHit)
        {
            if (target == null) return;
            ResonanceState consumed;
            _book.Consume(target.GetInstanceID(), Time.time, out consumed);
            _bodies.Remove(target.GetInstanceID());
            _bursts++;
            _burstMarkerUntil = Time.unscaledTime + 0.9f;
            _burstMarkerState = s;
            Vector3 at = target.ChestPosition;
            GameObject shooter = info.Source;
            BurstFx(at, s, target);
            Splash(target, at, s, burstHit, shooter);
            RefillGun(info);
            CombatDirector.HitStop(0.05f);
            if (Game.Cam != null) Game.Cam.Shake(0.22f);
            HintBurst();
            if (BurstFired != null)
            {
                foreach (Delegate d in BurstFired.GetInvocationList())
                {
                    try { ((Action<CharacterBase, ResonanceState>)d)(target, s); }
                    catch (Exception e) { Log.Exception("Resonance burst listener", e); }
                }
            }
        }

        private static void BurstFx(Vector3 at, ResonanceState s, CharacterBase target)
        {
            Color col = ResonanceRules.StateColor(s);
            Fx fx = Game.Fx;
            if (fx != null)
            {
                fx.Flash(at, col, 7f, 9f, 0.35f);
                fx.Flash(at, new Color(1f, 0.85f, 0.4f), 4f, 5f, 0.15f);
                fx.CrystalBurst(at, col, 34, 1.4f);
                fx.Sparks(at, Vector3.up * 5f, 24);
                switch (s)
                {
                    case ResonanceState.Kindled:
                        for (int i = 0; i < 10; i++) fx.FirePuff(at + UnityEngine.Random.insideUnitSphere * 1.2f, 0.9f);
                        break;
                    case ResonanceState.Stormlit:
                        fx.Sparks(at, Vector3.up * 8f, 30);
                        break;
                    case ResonanceState.Unmoored:
                        fx.GroundDust(target.transform.position, 2.2f, 14);
                        break;
                    case ResonanceState.Glassbound:
                        fx.GlassBurst(at, col, 36);
                        break;
                }
            }
            CombatSounds.Play(CombatSounds.Clip.Burst, at, 1f, 0.92f + 0.05f * (int)s, 0.55f, 160f);
        }

        /// <summary>What a burst does to the foes around the target, by mark.</summary>
        private static void Splash(CharacterBase target, Vector3 at, ResonanceState s, float burstHit, GameObject shooter)
        {
            float dmg = ResonanceRules.SplashDamage(s, burstHit);
            float radius = ResonanceRules.SplashRadius(s);
            if (dmg <= 0f || radius <= 0f) return;
            if (s == ResonanceState.Stormlit)
            {
                LootCombat.Chain(target, at, dmg, ResonanceRules.StormJumps, radius, shooter);
                return;
            }
            _work.Clear();
            _work.AddRange(LootCombat.FoesAround(at, radius, shooter));
            for (int i = _work.Count - 1; i >= 0; i--)
            {
                CharacterBase c = _work[i];
                if (c == null || c == target || c.IsDead) continue;
                Vector3 dir = c.ChestPosition - at;
                switch (s)
                {
                    case ResonanceState.Kindled:
                    {
                        DamageInfo f = DamageInfo.Make(dmg, c.ChestPosition, dir, shooter, DamageType.Fire);
                        c.Health.TakeDamage(f);
                        if (Game.Fx != null) Game.Fx.FirePuff(c.ChestPosition, 0.7f);
                        break;
                    }
                    case ResonanceState.Unmoored:
                    {
                        DamageInfo b = DamageInfo.Make(dmg, c.ChestPosition, dir, shooter, DamageType.Explosion);
                        b.Force = 10f;
                        c.Health.TakeDamage(b);
                        if (!c.IsDead) c.Knockdown(MathUtil.Flat(dir).normalized * 5f + Vector3.up * 4f, c.ChestPosition, 1.4f);
                        break;
                    }
                    case ResonanceState.Glassbound:
                    {
                        DamageInfo g = DamageInfo.Make(dmg, c.ChestPosition, dir, shooter, DamageType.Explosion);
                        g.Force = 4f;
                        c.Health.TakeDamage(g);
                        if (!c.IsDead) LootCombat.ApplyElement(c, Element.Frost, 1f, dmg, shooter);
                        if (Game.Fx != null) Game.Fx.GlassBurst(c.ChestPosition, ResonanceRules.StateColor(s), 10);
                        break;
                    }
                }
            }
            _work.Clear();
            // An Underheart boss never burns and is never thrown (its mark only multiplies the hit; the splash still lands on its adds).
            if (IsUnderBoss(target)) return;
            if (s == ResonanceState.Kindled && target != null && !target.IsDead) target.Health.Burning = Mathf.Max(target.Health.Burning, 3f);
            if (s == ResonanceState.Unmoored && target != null && !target.IsDead) target.Knockdown(Vector3.up * 5f, target.ChestPosition, 1.2f);
        }

        /// <summary>The gun that set off the burst gets a full magazine from its reserve (a crystal caster vents instead).</summary>
        private static void RefillGun(DamageInfo info)
        {
            PlayerController p = Game.Player;
            if (p == null || p.Weapons == null) return;
            WeaponController wc = p.Weapons;
            if (wc.Current == null || (int)wc.Current.Id != info.WeaponId) return;
            int rounds = wc.ResonanceRefill();
            if (rounds > 0 && Game.Hud != null) Game.Hud.CombatText("Magazine refilled", new Color(0.6f, 1f, 0.75f));
        }

        // ================================================================== guns charge powers

        /// <summary>A player hit on a hostile that did damage: crystal energy back for guns and melee, capped per second.</summary>
        public static void OnPlayerHit(CharacterBase target, DamageInfo info, bool killed)
        {
            Abilities ab = Game.Abilities;
            if (ab == null) return;
            float want = 0f;
            if (info.Type == DamageType.Bullet && info.WeaponId >= 0 && info.WeaponId < (int)WeaponId.Count)
            {
                WeaponDef d = Weapons.Get((WeaponId)info.WeaponId);
                if (d == null || d.Fire == FireKind.Melee || d.Fire == FireKind.Throw) return;
                bool loot = d.IsLoot;
                float shot = ResonanceRules.ShotSeconds(d.Interval, loot && d.HasTrait(LootTrait.SpinUp), loot && d.HasTrait(LootTrait.ChargeShot), LootGenerator.ChargeSeconds);
                int pellets = Mathf.Max(1, d.Pellets) * (loot && d.HasTrait(LootTrait.DoubleBarrel) ? 2 : 1);
                want = ResonanceRules.GunCharge(shot, pellets, info.Headshot || info.Crit, killed);
            }
            else if (info.Type == DamageType.Melee) want = ResonanceRules.MeleeCharge + (killed ? ResonanceRules.GunKillBonus : 0f);
            if (want <= 0f) return;
            float room = Mathf.Max(0f, ab.MaxEnergy - ab.Energy);
            if (room <= 0.01f) return;
            float got = _bucket.Take(Mathf.Min(want, room), Time.time);
            if (got <= 0f) return;
            ab.AddEnergy(got);
            _energyGained += got;
            if (got > 0.25f) _energyPulse = 1f;
            HintCharged(ab);
        }

        // ================================================================== per frame

        private void Update()
        {
            float udt = Time.unscaledDeltaTime;
            _energyPulse = Mathf.MoveTowards(_energyPulse, 0f, udt * 2.5f);
            DamageNumbers.Tick(udt);
            PruneMarks();
        }

        private void LateUpdate()
        {
            UpdateMarkVisuals(Time.unscaledTime);
        }

        private static void PruneMarks()
        {
            _ids.Clear();
            _book.Prune(Time.time, _ids);
            for (int i = 0; i < _ids.Count; i++) _bodies.Remove(_ids[i]);
            _ids.Clear();
            _book.Ids(_ids);
            for (int i = 0; i < _ids.Count; i++)
            {
                CharacterBase c;
                if (!_bodies.TryGetValue(_ids[i], out c) || c == null || c.IsDead)
                {
                    _book.Remove(_ids[i]);
                    _bodies.Remove(_ids[i]);
                }
            }
        }
    }
}
