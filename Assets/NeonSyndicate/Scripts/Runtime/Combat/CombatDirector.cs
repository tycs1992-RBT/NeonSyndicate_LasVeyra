using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Runs the flow of every fight: how many enemies may swing or shoot at the player at once (attack tokens),
    /// difficulty tuning, hit-stop and slow-motion beats, the shared melee hit resolution used by the player and
    /// enemies, and whether the player is "in combat" (camera pull-back, HUD).
    /// </summary>
    public sealed class CombatDirector : MonoBehaviour
    {
        private static CombatDirector _instance;
        private static readonly List<NpcController> _melee = new List<NpcController>();
        private static readonly List<NpcController> _shooters = new List<NpcController>();
        private static readonly Dictionary<NpcController, float> _shotSince = new Dictionary<NpcController, float>();
        private static readonly Collider[] _overlap = new Collider[128];
        private static readonly List<CharacterBase> _hitScratch = new List<CharacterBase>();
        // Creatures share the melee limit with NPCs (a whole swarm holds one token through its first living mite).
        private static readonly List<Creature> _creatureMelee = new List<Creature>();
        private static readonly List<Creature> _creatureRanged = new List<Creature>();
        private static float _hitStopUntil;
        private static float _slowUntil;
        private static float _lastPlayerHostile = -99f;
        private static float _combat;

        public static void ResetStatics()
        {
            _instance = null;
            _melee.Clear();
            _shooters.Clear();
            _shotSince.Clear();
            _creatureMelee.Clear();
            _creatureRanged.Clear();
            _hitStopUntil = 0f;
            _slowUntil = 0f;
            _lastPlayerHostile = -99f;
            _combat = 0f;
            CombatPressureRules.ResetStatics();
        }

        public static void Ensure()
        {
            if (_instance != null) return;
            GameObject go = new GameObject("Combat Director");
            _instance = go.AddComponent<CombatDirector>();
        }

        // ------------------------------------------------------------------ difficulty

        private static int D { get { return (int)Settings.Difficulty; } }

        /// <summary>Multiplier on all damage the player takes.</summary>
        public static float PlayerDamageTaken { get { return CombatPressureRules.IncomingDamage(Settings.Difficulty); } }
        public static int MaxMeleeAttackers { get { return D == 0 ? 1 : (D == 1 ? 2 : 3); } }
        public static int MaxShooters { get { return D == 0 ? 1 : (D == 1 ? 2 : 4); } }
        /// <summary>Enemy gun spread multiplier (bigger misses more).</summary>
        public static float EnemySpread { get { return CombatPressureRules.GunSpread(Settings.Difficulty); } }
        /// <summary>Seconds an enemy needs after spotting the player before the first aimed shot.</summary>
        public static float ReactionTime { get { return CombatPressureRules.ReactionSeconds(Settings.Difficulty); } }
        public static float ParryWindow { get { return D == 0 ? 0.3f : (D == 1 ? 0.22f : 0.16f); } }
        public static float EnemyWindupScale { get { return CombatPressureRules.WindupScale(Settings.Difficulty); } }
        public static int TonicCharges { get { return D == 0 ? 5 : (D == 1 ? 4 : 3); } }
        public static float TonicHeal { get { return D == 0 ? 0.65f : (D == 1 ? 0.5f : 0.4f); } }
        /// <summary>Fraction of max health that regenerates on its own out of combat.</summary>
        public static float RegenCap { get { return D == 0 ? 1f : (D == 1 ? 0.6f : 0.35f); } }
        /// <summary>Seconds between last-stand saves (a killing blow from high health leaves 1 HP). 0 = off.</summary>
        public static float LastStandCooldown { get { return D == 0 ? 20f : (D == 1 ? 45f : 0f); } }
        /// <summary>Fraction of carried Lumen dropped at death (the rest is lost if you die before recovering it).</summary>
        public static bool LumenDropsOnDeath { get { return D != 0; } }

        // ------------------------------------------------------------------ attack tokens

        private static void Prune(List<NpcController> list)
        {
            for (int i = list.Count - 1; i >= 0; i--)
            {
                NpcController n = list[i];
                if (n == null || n.IsDead || n.IsRagdolled || n.State != NpcState.Fight || n.Target == null || !n.Target.IsPlayer)
                {
                    if (n != null) _shotSince.Remove(n);
                    list.RemoveAt(i);
                }
            }
        }

        /// <summary>An enemy asks for permission to start a melee attack on the player.</summary>
        public static bool TryMeleeToken(NpcController n)
        {
            Prune(_melee);
            if (_melee.Contains(n)) return true;
            if (_melee.Count + CreatureMeleeCount >= MaxMeleeAttackers) return false;
            _melee.Add(n);
            return true;
        }

        public static void ReleaseMeleeToken(NpcController n)
        {
            _melee.Remove(n);
        }

        public static bool HasMeleeToken(NpcController n)
        {
            return _melee.Contains(n);
        }

        /// <summary>An enemy asks to fire aimed shots at the player. Tokens time out so everyone gets a turn.</summary>
        public static bool TryShotToken(NpcController n)
        {
            if (n == null) return false;
            Prune(_shooters);
            Prune(_creatureRanged);
            if (_shooters.Contains(n))
            {
                float since;
                if (_shotSince.TryGetValue(n, out since) && Time.time - since > 3.2f && _shooters.Count + _creatureRanged.Count >= MaxShooters && AnyWaiting(n))
                {
                    ReleaseShotToken(n);
                    return false;
                }
                return true;
            }
            if (!CombatPressureRules.CanAdmitRanged(_shooters.Count, _creatureRanged.Count, MaxShooters)) return false;
            _shooters.Add(n);
            _shotSince[n] = Time.time;
            return true;
        }

        public static void ReleaseShotToken(NpcController n)
        {
            _shooters.Remove(n);
            _shotSince.Remove(n);
        }

        private static bool AnyWaiting(NpcController except)
        {
            foreach (NpcController o in NpcController.All)
            {
                if (o == except || o.IsDead || o.State != NpcState.Fight || o.Target == null || !o.Target.IsPlayer) continue;
                if (!_shooters.Contains(o)) return true;
            }
            foreach (Creature c in Creature.All)
                if (c != null && c.WantsToken && !_creatureRanged.Contains(c)) return true;
            return false;
        }

        public static int ActiveMelee { get { Prune(_melee); return _melee.Count + CreatureMeleeCount; } }
        public static int ActiveShooters { get { Prune(_shooters); Prune(_creatureRanged); return _shooters.Count + _creatureRanged.Count; } }

        // ------------------------------------------------------------------ creature tokens

        private static void Prune(List<Creature> list)
        {
            for (int i = list.Count - 1; i >= 0; i--)
            {
                Creature c = list[i];
                if (c == null || !c.WantsToken)
                {
                    if (c != null) c.TokenPruned();
                    list.RemoveAt(i);
                }
            }
        }

        private static int CreatureMeleeCount { get { Prune(_creatureMelee); return _creatureMelee.Count; } }

        /// <summary>A creature asks to start a melee attack on the player. NPC and creature attackers share MaxMeleeAttackers.</summary>
        public static bool TryCreatureToken(Creature c)
        {
            if (c == null) return false;
            Prune(_creatureMelee);
            if (_creatureMelee.Contains(c)) return true;
            Prune(_melee);
            if (_melee.Count + _creatureMelee.Count >= MaxMeleeAttackers) return false;
            _creatureMelee.Add(c);
            return true;
        }

        public static void ReleaseCreatureToken(Creature c)
        {
            _creatureMelee.Remove(c);
        }

        /// <summary>True when this creature holds a melee token or could take one now.</summary>
        public static bool CreatureTokenFree(Creature c)
        {
            if (_creatureMelee.Contains(c)) return true;
            Prune(_melee);
            return _melee.Count + CreatureMeleeCount < MaxMeleeAttackers;
        }

        /// <summary>Creature breath/spit/gaze shares the same MaxShooters budget as aimed NPC firearm attacks.</summary>
        public static bool TryCreatureRanged(Creature c)
        {
            if (c == null) return false;
            Prune(_creatureRanged);
            Prune(_shooters);
            if (_creatureRanged.Contains(c)) return true;
            if (!CombatPressureRules.CanAdmitRanged(_shooters.Count, _creatureRanged.Count, MaxShooters)) return false;
            _creatureRanged.Add(c);
            return true;
        }

        public static void ReleaseCreatureRanged(Creature c)
        {
            _creatureRanged.Remove(c);
        }

        // ------------------------------------------------------------------ combat state

        /// <summary>Called whenever an enemy engages the player or the player hits someone.</summary>
        public static void MarkCombat()
        {
            _lastPlayerHostile = Time.time;
        }

        /// <summary>0..1, eased: drives the combat camera pull-back.</summary>
        public static float CombatFactor { get { return _combat; } }

        public static bool InCombat { get { return Time.time - _lastPlayerHostile < 6f; } }

        // ------------------------------------------------------------------ hit-stop and slow motion

        /// <summary>A tiny freeze on impact so hits land with weight.</summary>
        public static void HitStop(float seconds)
        {
            Ensure();
            if (seconds <= 0f) return;
            _hitStopUntil = Mathf.Max(_hitStopUntil, Time.unscaledTime + Mathf.Min(seconds, 0.12f));
            GameTime.Request("hitstop", 0.06f);
        }

        /// <summary>Short slow-motion beat (parries, perfect dodges, last stands).</summary>
        public static void Slow(float seconds, float scale)
        {
            Ensure();
            _slowUntil = Mathf.Max(_slowUntil, Time.unscaledTime + seconds);
            GameTime.Request("combatslow", scale);
        }

        private void Update()
        {
            float now = Time.unscaledTime;
            if (_hitStopUntil > 0f && now >= _hitStopUntil)
            {
                _hitStopUntil = 0f;
                GameTime.Release("hitstop");
            }
            if (_slowUntil > 0f && now >= _slowUntil)
            {
                _slowUntil = 0f;
                GameTime.Release("combatslow");
            }
            if (!Game.IsPlaying)
            {
                if (_hitStopUntil > 0f) { _hitStopUntil = 0f; GameTime.Release("hitstop"); }
                if (_slowUntil > 0f) { _slowUntil = 0f; GameTime.Release("combatslow"); }
            }
            // Engaged enemies nearby keep the combat camera out.
            PlayerController p = Game.Player;
            if (p != null && !p.IsDead)
            {
                Vector3 pp = p.transform.position;
                foreach (NpcController n in NpcController.All)
                {
                    if (n.IsDead || n.State != NpcState.Fight || n.Target != p) continue;
                    if ((n.transform.position - pp).sqrMagnitude < 32f * 32f) { _lastPlayerHostile = Time.time; break; }
                }
                for (int i = 0; i < Creature.All.Count; i++)
                {
                    Creature c = Creature.All[i];
                    if (!c.IsEngaged || c.Mode == CreatureMode.Stalk) continue;
                    float r = 32f + c.BodyRadius;
                    if ((c.transform.position - pp).sqrMagnitude < r * r) { _lastPlayerHostile = Time.time; break; }
                }
            }
            float want = InCombat && p != null && !p.InVehicle ? 1f : 0f;
            _combat = Mathf.MoveTowards(_combat, want, Time.unscaledDeltaTime * (want > _combat ? 1.2f : 0.45f));
        }

        // ------------------------------------------------------------------ melee resolution

        /// <summary>
        /// Resolves the active frames of a melee move: everyone inside the arc and range (with a clear line) takes the
        /// hit once per swing. Returns how many characters were hit.
        /// </summary>
        public static int Strike(CharacterBase attacker, MeleeMove move, float baseDamage, float multiplier, WeaponId weapon, List<CharacterBase> alreadyHit, out bool killed)
        {
            killed = false;
            if (attacker == null) return 0;
            Transform t = attacker.transform;
            Vector3 origin = t.position + Vector3.up * 1.1f * (attacker.Rig != null ? attacker.Rig.Scale : 1f);
            Vector3 fwd = t.forward;
            // Humanoid creatures (golems, the Titan) reach and stand further than people: scale by their rig.
            float scale = attacker.Rig != null && attacker.Rig.Layout != null && attacker.Rig.Layout.Creature != HumanoidCreature.None ? Mathf.Clamp(attacker.Rig.Scale, 0.5f, 3f) : 1f;
            float reach = move.Range + 0.45f * scale;
            float vertical = 1.6f * scale;
            // Big creature targets are found by their edge, not their centre: search a little wider.
            float search = reach + 3f;
            int n = Physics.OverlapSphereNonAlloc(origin + (move.Arc >= 300f ? Vector3.zero : fwd * reach * 0.4f), search, _overlap, (1 << Layers.Characters) | (1 << Layers.Ragdoll), QueryTriggerInteraction.Ignore);
            int hits = 0;
            _hitScratch.Clear();
            for (int i = 0; i < n; i++)
            {
                CharacterBase c = _overlap[i].GetComponentInParent<CharacterBase>();
                if (c == null || c == attacker || c.IsDead || _hitScratch.Contains(c)) continue;
                _hitScratch.Add(c);
                if (alreadyHit != null && alreadyHit.Contains(c)) continue;
                Vector3 to = MathUtil.Flat(c.transform.position - t.position);
                float dist = to.magnitude;
                if (dist - c.BodyRadius > reach) continue;
                float dy = c.transform.position.y - t.position.y;
                if (dy > vertical || dy < -(vertical + c.BodyHeight)) continue;
                if (move.Arc < 300f && dist > 0.3f + c.BodyRadius && Vector3.Angle(fwd, to) > move.Arc * 0.5f + Mathf.Atan2(c.BodyRadius, Mathf.Max(0.5f, dist)) * Mathf.Rad2Deg) continue;
                if (Physics.Linecast(origin, c.ChestPosition, Layers.WorldMask, QueryTriggerInteraction.Ignore)) continue;
                // Allies of an NPC attacker are not hit by its sweep.
                NpcController na = attacker as NpcController;
                if (na != null && !NpcCrystal04F.CanDamage(na, c)) continue;
                if (attacker.IsPlayer && WeaponController.CrystalProtected(c)) continue;
                if (na != null && na.CityActor04F == null && !c.IsPlayer && c.Team != null && attacker.Team != null && c.Team.Team == attacker.Team.Team) continue;
                if (alreadyHit != null) alreadyHit.Add(c);
                Vector3 dir = dist > 0.05f ? to / dist : fwd;
                DamageInfo info = DamageInfo.Make(baseDamage * move.Damage * multiplier, c.ChestPosition, dir, attacker.gameObject, DamageType.Melee);
                info.Force = move.Force;
                info.WeaponId = (int)weapon;
                info.Poise = baseDamage * move.Poise * multiplier * 0.6f;
                if (move.Knockdown) info.Flags |= DamageFlags.Knockdown;
                if (move.Unblockable) info.Flags |= DamageFlags.Unblockable;
                if (move.Kind == MoveKind.Heavy) info.Flags |= DamageFlags.Heavy;
                if (move.Kind == MoveKind.Riposte) info.Flags |= DamageFlags.Riposte;
                if (!attacker.IsPlayer) info.Flags |= DamageFlags.Parryable;
                bool wasAlive = c.Health.IsAlive;
                c.Health.TakeDamage(info);
                hits++;
                // Damage numbers come from the health component (DamageNumbers); creatures show their own crystal or smoke hit effects.
                bool creature = c.Team != null && c.Team.Team == Team.Creature;
                if (wasAlive && !c.Health.IsAlive) killed = true;
                if (Game.Fx != null && c.Health.LastBlocked == false && !creature)
                {
                    bool blade = weapon == WeaponId.Switchblade || weapon == WeaponId.VoltKatana;
                    if (blade) Game.Fx.Blood(info.Point, fwd, 6);
                    else Game.Fx.Sparks(info.Point, -fwd, 3);
                }
                if (attacker.IsPlayer)
                {
                    MarkCombat();
                    Threats.Report(t.position, 25f, attacker.gameObject, ThreatKind.Violence);
                }
            }
            // Melee also smacks cars and breakables in front.
            if (attacker.IsPlayer && move.Arc < 300f)
            {
                Vector3 hitPoint = origin + fwd * Mathf.Min(move.Range, 1.6f);
                int m = Physics.OverlapSphereNonAlloc(hitPoint, 0.5f, _overlap, Layers.ShootMask & ~((1 << Layers.Characters) | (1 << Layers.Ragdoll)), QueryTriggerInteraction.Ignore);
                for (int i = 0; i < m; i++)
                {
                    IDamageable d = _overlap[i].GetComponentInParent<IDamageable>();
                    if (d == null || !d.IsAlive || d is CharacterHealth) continue;
                    DamageInfo info = DamageInfo.Make(baseDamage * move.Damage * multiplier * 0.5f, hitPoint, fwd, attacker.gameObject, DamageType.Melee);
                    info.Force = move.Force;
                    d.TakeDamage(info);
                    hits++;
                    break;
                }
            }
            return hits;
        }
    }
}
