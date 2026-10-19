using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// The creature's mind: perception (distance, a view cone, crouching and stillness, SkillMods.CreatureSense, line of
    /// sight; gunshots, explosions and violence through Threats; engines for the Dune Wyrm), the temperaments
    /// (Aggressive attacks on sight, Territorial warns first, Stalker creeps to your blind side, Swarm acts as one, Apex
    /// roars and fights), leashing to its home, fleeing when a prey-like species is badly hurt, and choosing attacks
    /// from the species table while taking turns through CombatDirector's attack tokens.
    /// </summary>
    public sealed partial class Creature
    {
        private int _threatSerial;
        private float _lastThink = -1f;
        private float _lastSeen = -99f;
        private Vector3 _lastKnown;
        private float _warnedFor;
        private float _calmTimer;
        private float _modeTime;
        private float _nextAttackAt;
        private bool _hasToken;
        private bool _hasRangedToken;
        private float _circleDir = 1f;
        private float _circleSwap;
        private float _wanderTimer;
        private float _stalkSeen;
        private float _fleeUntil;
        private Vector3 _fleeFrom;
        private bool _roared;
        private float _nextCry;
        private float _lastNoise = -99f;
        private float _stillTimer;
        // A stalker with no pounce: time spent hanging behind the player, and how long it waits there before it strikes.
        private float _behindFor;
        private float _strikeAfter = 2.5f;

        private void SetMode(CreatureMode m)
        {
            if (Mode == m) return;
            Mode = m;
            _modeTime = 0f;
            _hasGoal = false;
            _warnedFor = 0f;
            _calmTimer = 0f;
            if (m == CreatureMode.Stalk)
            {
                _behindFor = 0f;
                _strikeAfter = UnityEngine.Random.Range(2.1f, 3.2f);
            }
            if (m != CreatureMode.Hunt)
            {
                ReleaseTokens();
                if (!_crashing) _landing = false;
            }
        }

        /// <summary>
        /// Called by the pack when another member spots or is hurt by the target. A member running away or heading home
        /// to heal keeps going (hurting it directly still turns it round).
        /// </summary>
        public void JoinHunt(CharacterBase target)
        {
            if (_dead || !Ready || target == null) return;
            if (Mode == CreatureMode.Hunt && Target == target) return;
            if (Mode == CreatureMode.Flee || Mode == CreatureMode.Return) return;
            BeginHunt(target);
        }

        private void BeginHunt(CharacterBase t)
        {
            if (t == null) return;
            if (CalmHabitatResident || PracticeDefeated || PracticePlayerDefeated) return;
            if (t.IsPlayer && Ambient && !Persistent && !Provoked && StarterSafety.BlockAmbientAggression(t.transform.position)) return;
            Target = t;
            _lastSeen = Time.time;
            _lastKnown = t.transform.position;
            SetMode(CreatureMode.Hunt);
            _nextAttackAt = Mathf.Max(_nextAttackAt, Time.time + UnityEngine.Random.Range(0.4f, 1.1f));
            if (Group != null) Group.Alert(t, this);
            if (_flies && !_flying && _sp != null && _sp.Temperament != Temperament.Territorial) TakeOff();
        }

        private float HealthFraction { get { return Health.MaxHealth > 0f ? Health.Health / Health.MaxHealth : 0f; } }

        private void Think(float playerDist)
        {
            float now = Time.time;
            float tdt = _lastThink < 0f ? 0.2f : Mathf.Min(1f, now - _lastThink);
            _lastThink = now;
            _modeTime += tdt;
            PlayerController p = Game.Player;
            if (PracticeDefeated || PracticePlayerDefeated) return;
            bool playerOk = p != null && !p.IsDead && Game.IsPlaying;
            if (ThinkHabitat(tdt)) return;
            if (playerOk && Ambient && !Persistent && !Provoked && StarterSafety.BlockAmbientAggression(p.transform.position))
            {
                Target = null;
                CancelAttack();
                SetMode(CreatureMode.Wander);
                ThinkCalm(p, false, playerDist, tdt);
                return;
            }
            bool sees = playerOk && Perceive(p, playerDist);
            if (sees)
            {
                _lastSeen = now;
                _lastKnown = p.transform.position;
            }
            if (playerOk) Listen(p, playerDist);
            if (_serpent && playerOk) SenseEngines(p, playerDist, tdt);
            if ((_plannedHostile || Provoked) && playerOk && Mode != CreatureMode.Hunt && Mode != CreatureMode.Flee && Mode != CreatureMode.Return)
            {
                BeginHunt(p);
                return;
            }
            switch (Mode)
            {
                case CreatureMode.Idle:
                case CreatureMode.Wander: ThinkCalm(p, sees, playerDist, tdt); break;
                case CreatureMode.Warn: ThinkWarn(p, sees, playerDist, tdt); break;
                case CreatureMode.Stalk: ThinkStalk(p, sees, playerDist, tdt); break;
                case CreatureMode.Hunt: ThinkHunt(p, sees, playerDist, tdt); break;
                case CreatureMode.Flee:
                    if (now > _fleeUntil) SetMode(CreatureMode.Return);
                    break;
                case CreatureMode.Return:
                    Health.Heal(Health.MaxHealth * 0.12f * tdt);
                    if (MathUtil.FlatDistance(transform.position, Home) < Mathf.Max(4f, Radius * 2f) || _modeTime > 40f)
                    {
                        Provoked = false;
                        SetMode(CreatureMode.Wander);
                        if (Group != null) Group.MemberCalmed();
                    }
                    break;
            }
            // A surfaced wyrm dives back under once its time above is up, whatever it is doing.
            if (_serpent) ThinkSurfacing();
        }

        // ------------------------------------------------------------------ senses

        /// <summary>Sight: notice range from the species' sense and the player's stealth, the view cone and line of sight.</summary>
        private bool Perceive(PlayerController p, float dist)
        {
            if (_sp == null) return false;
            bool fog = Game.Atmo != null && Game.Atmo.Current == Weather.Fog;
            float night = Game.Atmo != null ? Game.Atmo.NightFactor : 0f;
            bool nocturnal = _sp.Night && !_sp.Day;
            float range = CreatureBrainLogic.NoticeRange(_sp.Sense, SkillMods.CreatureSense, p.Crouched, p.InVehicle ? p.CurrentVehicle.Speed : p.FlatVelocity.magnitude, p.InVehicle, night, nocturnal, fog);
            if (Mode == CreatureMode.Hunt || Mode == CreatureMode.Stalk || Provoked) range = Mathf.Max(range * 1.6f, 25f);
            if (dist > range) return false;
            Vector3 to = MathUtil.Flat(p.transform.position - transform.position);
            float angle = to.sqrMagnitude > 0.01f ? Vector3.Angle(MathUtil.DirFromYaw(_yaw), to) : 0f;
            if (!CreatureBrainLogic.CanNotice(dist, angle, range)) return false;
            if (_serpent && _burrowed) return dist < range * 0.6f;
            Vector3 eye = HeadPosition + Vector3.up * 0.1f;
            if (Physics.Linecast(eye, p.ChestPosition, Layers.WorldMask, QueryTriggerInteraction.Ignore)) return dist < range * 0.25f;
            return true;
        }

        /// <summary>Hearing: gunshots, explosions, powers and fights through the shared Threats memory.</summary>
        private void Listen(PlayerController p, float playerDist)
        {
            ThreatEvent e;
            if (!Threats.Query(transform.position, _threatSerial, out e))
            {
                _threatSerial = Threats.LatestSerial;
                return;
            }
            _threatSerial = Threats.LatestSerial;
            float reach = CreatureBrainLogic.NoiseReach(e.Kind, _sp != null ? _sp.Sense : 30f) * Mathf.Clamp(SkillMods.CreatureSense, 0.3f, 2f);
            float d = Vector3.Distance(e.Position, transform.position);
            if (d > reach) return;
            bool fromPlayer = Game.IsPlayer(e.Source) || (p.InVehicle && e.Source == p.CurrentVehicle.gameObject);
            if (!fromPlayer) return;
            _lastNoise = Time.time;
            _lastKnown = e.Position;
            if (Mode == CreatureMode.Hunt || Mode == CreatureMode.Flee) return;
            Temperament t = _sp != null ? _sp.Temperament : Temperament.Aggressive;
            if (FleesFromNoise && e.Kind == ThreatKind.Explosion)
            {
                Flee(e.Position, 6f);
                return;
            }
            if (t == Temperament.Territorial && _sp.WarnRadius > 0f && Mode != CreatureMode.Warn) SetMode(CreatureMode.Warn);
            else if (t == Temperament.Stalker && Mode != CreatureMode.Stalk) { Target = p; SetMode(CreatureMode.Stalk); }
            else if (t != Temperament.Territorial) BeginHunt(p);
        }

        private bool FleesFromNoise { get { return _sp != null && (_sp.Id == "neon_jackal" || _sp.Id == "glass_mite") && Mode != CreatureMode.Hunt; } }

        /// <summary>The Dune Wyrm hunts by vibration: running engines draw it from far away; a player who stands still loses it.</summary>
        private void SenseEngines(PlayerController p, float playerDist, float tdt)
        {
            Vehicle v = p.CurrentVehicle;
            bool engine = v != null && v.EngineRunning && v.Speed > 2.5f;
            float reach = 95f * Mathf.Clamp(SkillMods.CreatureSense, 0.3f, 2f);
            if (engine && playerDist < reach && CityLayout.IsWilderness(CityLayout.DistrictAt(p.transform.position)))
            {
                _lastSeen = Time.time;
                _lastKnown = p.transform.position;
                _stillTimer = 0f;
                if (Mode != CreatureMode.Hunt) BeginHunt(p);
                return;
            }
            if (Mode != CreatureMode.Hunt || !_burrowed || Target == null || !Target.IsPlayer) return;
            // Standing still (or sneaking) on foot, with no shots fired, makes no vibration to follow.
            float speed = p.InVehicle ? (v != null && v.EngineRunning ? v.Speed : 0f) : p.FlatVelocity.magnitude;
            bool quiet = speed < 0.9f || (p.Crouched && speed < 2.5f);
            if (quiet && Time.time - _lastNoise > 3f) _stillTimer += tdt;
            else _stillTimer = 0f;
            if (_stillTimer > 3.5f)
            {
                _stillTimer = 0f;
                Provoked = false;
                _plannedHostile = false;
                SetMode(CreatureMode.Wander);
                if (Game.Hud != null && playerDist < 60f) Game.Hud.CombatText("The wyrm lost you", new Color(1f, 0.8f, 0.45f));
            }
        }

        // ------------------------------------------------------------------ calm, warning, stalking

        private void ThinkCalm(PlayerController p, bool sees, float dist, float tdt)
        {
            if (sees && _sp != null)
            {
                switch (_sp.Temperament)
                {
                    case Temperament.Territorial:
                        if (_sp.WarnRadius <= 0f) BeginHunt(p);
                        else SetMode(CreatureMode.Warn);
                        return;
                    case Temperament.Stalker:
                        Target = p;
                        SetMode(CreatureMode.Stalk);
                        return;
                    default:
                        BeginHunt(p);
                        if (_sp.Temperament == Temperament.Apex && !_roared) OpeningCry();
                        else if (_sp.Id == "neon_jackal" && Group != null && Group.Alive > 1 && UnityEngine.Random.value < 0.5f) OpeningCry();
                        return;
                }
            }
            _wanderTimer -= tdt;
            if (_wanderTimer <= 0f) PickWander();
        }

        private void ThinkWarn(PlayerController p, bool sees, float dist, float tdt)
        {
            if (p == null || p.IsDead) { SetMode(CreatureMode.Wander); return; }
            Target = p;
            TerritorialStep s = CreatureBrainLogic.Territorial(dist, _sp.WarnRadius, _sp.StrikeRadius, _warnedFor, 5f, Provoked);
            if (s == TerritorialStep.Attack)
            {
                Provoked = true;
                BeginHunt(p);
                return;
            }
            if (s == TerritorialStep.Calm || (!sees && Time.time - _lastSeen > 3f))
            {
                _calmTimer += tdt;
                if (_calmTimer > 2.5f) SetMode(CreatureMode.Wander);
                return;
            }
            _calmTimer = 0f;
            _warnedFor += tdt;
            if (Time.time >= _nextCry && _atk < 0) OpeningCry();
        }

        private void ThinkStalk(PlayerController p, bool sees, float dist, float tdt)
        {
            if (p == null || p.IsDead || (!sees && Time.time - _lastSeen > 10f))
            {
                SetMode(CreatureMode.Wander);
                return;
            }
            Target = p;
            // Is the player looking this way? The camera is their eyes.
            Vector3 look = Game.Cam != null ? Game.Cam.FlatForward : p.transform.forward;
            Vector3 toMe = MathUtil.Flat(transform.position - p.transform.position);
            float a = toMe.sqrMagnitude > 0.01f ? Vector3.Angle(look, toMe) : 0f;
            bool watched = a < 45f && sees && Visibility > 0.5f;
            _stalkSeen = watched ? _stalkSeen + tdt : Mathf.Max(0f, _stalkSeen - tdt * 0.5f);
            if (_stalkSeen > 1.6f || Provoked || dist < Mathf.Max(2.5f, Radius + 2f))
            {
                BeginHunt(p);
                return;
            }
            bool behind = a > 110f;
            int leap = FindAttack(AttackShape.Leap);
            if (behind && leap >= 0 && dist > _attacks[leap].MinRange && dist < _attacks[leap].MaxRange && Time.time >= _readyAt[leap])
            {
                BeginHunt(p);
                _nextAttackAt = 0f;
                return;
            }
            // No pounce (the Hollow Shade): it hangs behind you for a couple of seconds, then closes in to strike.
            _behindFor = behind && sees ? _behindFor + tdt : Mathf.Max(0f, _behindFor - tdt);
            if (CreatureFightRules.StalkerStrikes(leap >= 0, _behindFor, _strikeAfter))
            {
                BeginHunt(p);
                _nextAttackAt = 0f;
            }
        }

        private int FindAttack(AttackShape shape)
        {
            for (int i = 0; i < _attacks.Length; i++) if (_attacks[i].Shape == shape) return i;
            return -1;
        }

        private void PickWander()
        {
            _wanderTimer = UnityEngine.Random.Range(4f, 9f);
            if (_serpent)
            {
                // The wyrm roams far under the sand.
                Vector2 r = UnityEngine.Random.insideUnitCircle * 60f;
                Vector3 g = Home + new Vector3(r.x, 0f, r.y);
                if (CityLayout.IsWilderness(CityLayout.DistrictAt(g))) SetGoal(g, _sp.WalkSpeed);
                return;
            }
            if (UnityEngine.Random.value < 0.35f && !_flying)
            {
                _hasGoal = false;
                return;
            }
            float radius = _sp != null && _sp.Temperament == Temperament.Apex ? 40f : (_mite ? 6f : 18f);
            for (int i = 0; i < 4; i++)
            {
                Vector2 r = UnityEngine.Random.insideUnitCircle * radius;
                Vector3 g = Home + new Vector3(r.x, 0f, r.y);
                if (Game.Layout != null && Game.Layout.IsInsideBuilding(g, Radius + 1f)) continue;
                if (!CityLayout.IsWilderness(CityLayout.DistrictAt(g)) && Game.Layout != null && Game.Layout.RoadAt(g, 1f) != null && !InnerOnly) continue;
                // Stay on this level: no strolls off a cliff edge (the drake's canyon roost, the quarry rim).
                float gy;
                if (!_flying && CreatureDirector.GroundAt(g, 20f, out gy) && Mathf.Abs(gy - transform.position.y) > 3f) continue;
                SetGoal(g, _sp != null ? _sp.WalkSpeed : 1.5f);
                return;
            }
        }

        /// <summary>A roar, howl or screech: warns intruders, calls the pack, opens a boss fight.</summary>
        private void OpeningCry()
        {
            _roared = true;
            _nextCry = Time.time + UnityEngine.Random.Range(3f, 4.5f);
            int call = FindAttack(AttackShape.Call);
            if (call >= 0 && _atk < 0 && !IsStaggered) StartAttack(call);
            else if (Game.Audio != null) CreatureAudio.Play(_sp, CreatureCry.Roar, HeadPosition, Size);
            if (Group != null && Target != null) Group.Alert(Target, this);
        }

        // ------------------------------------------------------------------ the fight

        private void ThinkHunt(PlayerController p, bool sees, float dist, float tdt)
        {
            if (Target == null || Target.IsDead)
            {
                Target = p != null && !p.IsDead && (Provoked || _plannedHostile) ? p : null;
                if (Target == null)
                {
                    SetMode(CreatureMode.Return);
                    return;
                }
            }
            bool lost = Time.time - _lastSeen > (Provoked || _plannedHostile ? 14f : 8f);
            if (lost && !_plannedHostile && !(_serpent && Time.time - _lastSeen < 20f))
            {
                SetMode(CreatureMode.Return);
                return;
            }
            float leash = _sp != null ? _sp.Leash : 70f;
            if (CreatureBrainLogic.ShouldLeash(MathUtil.FlatDistance(transform.position, Home), leash, Persistent || _plannedHostile || Options.Hostile))
            {
                SetMode(CreatureMode.Return);
                return;
            }
            float flee = _sp != null ? CreatureBrainLogic.FleeHealth(_sp.Id, Group != null && Group.LeaderLost) : 0f;
            if (flee > 0f && HealthFraction < flee && !_plannedHostile && !IsBossLike)
            {
                Flee(Target.transform.position, UnityEngine.Random.Range(7f, 11f));
                return;
            }
            if (_flies) ThinkFlight(dist, tdt);
            if (_atk < 0 && !IsStaggered && Time.time >= _nextAttackAt) TryAttack();
        }

        private void Flee(Vector3 from, float seconds)
        {
            _fleeFrom = from;
            _fleeUntil = Time.time + seconds;
            CancelAttack();
            SetMode(CreatureMode.Flee);
            if (_flies && !_flying) TakeOff();
        }

        /// <summary>Distance from the body's edge to the target's body (metres).</summary>
        private float EdgeDistance(CharacterBase t)
        {
            if (t == null) return 999f;
            Vector3 tp = t.InVehicle ? t.CurrentVehicle.transform.position : t.transform.position;
            float d = MathUtil.FlatDistance(tp, transform.position) - Radius - (t.InVehicle ? 1.2f : Mathf.Max(0.3f, t.BodyRadius));
            return Mathf.Max(0f, d);
        }

        /// <summary>Degrees between the creature's facing and the direction to the target.</summary>
        private float AngleTo(Vector3 p)
        {
            Vector3 to = MathUtil.Flat(p - transform.position);
            if (to.sqrMagnitude < 0.01f) return 0f;
            return Vector3.SignedAngle(MathUtil.DirFromYaw(_yaw), to, Vector3.up);
        }

        private void TryAttack()
        {
            if (Target == null || _attacks.Length == 0) return;
            Vector3 tp = Target.transform.position;
            float edge = EdgeDistance(Target);
            float angle = AngleTo(tp);
            AttackUse state = _burrowed ? AttackUse.Under : (_flying ? AttackUse.Air : AttackUse.Ground);
            // A swarm is one attacker: its mites bite only during a wave, while the swarm holds the token, and only a
            // few at a time. A mite with no swarm takes a token for each bite like any other creature.
            bool swarm = _mite && Group != null && Group.Swarm;
            if (swarm)
            {
                if (Target.IsPlayer && !Group.WaveActive) return;
                if (Group.Biting() >= 4) return;
            }
            Vector3 forward04L = MathUtil.DirFromYaw(_yaw);
            Vector3 side04L = new Vector3(forward04L.z, 0f, -forward04L.x);
            float lateral04L = Vector3.Dot(Target.Velocity, side04L);
            int idx = CreatureTactics04L.Pick(_attacks, _readyAt, Time.time, edge, angle, state,
                UnityEngine.Random.value, _previousAttack04L, lateral04L);
            if (idx < 0) return;
            CreatureAttack a = _attacks[idx];
            if (PartBlocks(a))
            {
                // A broken gland or crown gem: that attack is gone, try again soon with another.
                _readyAt[idx] = Time.time + 3f;
                return;
            }
            if (Target.IsPlayer && !swarm)
            {
                if (a.Melee)
                {
                    if (!_hasToken)
                    {
                        if (!CombatDirector.TryCreatureToken(this)) return;
                        _hasToken = true;
                    }
                }
                else if (a.Shape != AttackShape.Call)
                {
                    if (!CombatDirector.TryCreatureRanged(this)) return;
                    _hasRangedToken = true;
                }
            }
            StartAttack(idx);
        }

        /// <summary>True while this creature may close in to strike (it holds a token, or one is free).</summary>
        private bool MayClose
        {
            get
            {
                if (Target == null || !Target.IsPlayer) return true;
                if (_mite && Group != null && Group.Swarm) return true;
                return _hasToken || CombatDirector.CreatureTokenFree(this);
            }
        }

        private void ReleaseTokens()
        {
            if (_hasToken) CombatDirector.ReleaseCreatureToken(this);
            if (_hasRangedToken) CombatDirector.ReleaseCreatureRanged(this);
            _hasToken = false;
            _hasRangedToken = false;
        }

        /// <summary>Called by CombatDirector when it prunes this creature's token.</summary>
        internal void TokenPruned()
        {
            _hasToken = false;
        }

        /// <summary>True while this creature wants to keep its token (a living fighter whose target is the player).</summary>
        internal bool WantsToken { get { return Ready && !_dead && Mode == CreatureMode.Hunt && Target != null && Target.IsPlayer; } }

        /// <summary>
        /// 0 hidden to 1 solid: Hollow Shades drift almost unseen until they are close, attacking, hurt or the Third Eye
        /// is open; everything else is always solid.
        /// </summary>
        public float Visibility
        {
            get
            {
                if (!_wisp || _dead) return 1f;
                if (Game.Abilities != null && Game.Abilities.ThirdEyeActive) return 1f;
                if (_atk >= 0 || IsStaggered || _hurt > 0.05f) return 1f;
                float d = Vector3.Distance(transform.position, Game.PlayerPosition);
                return Mathf.Lerp(1f, 0.12f, Mathf.InverseLerp(5f, 13f, d));
            }
        }
    }
}
