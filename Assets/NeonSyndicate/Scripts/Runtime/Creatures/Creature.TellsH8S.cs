using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// H8S tells on the live creature (owner: the creature's attack code). The timing it plays an attack with
    /// (CreatureAttackDb.TimingH8S: class floors and time to leave an area); the ground mark of the blow in the tell
    /// colour and at the hit test's true reach (a ring that follows the body for an all-round sweep, a lane for a pounce
    /// or a ground charge); the aim lock that fixes a travelling blow's direction at CombatTellsH8S.AimLockFraction of its
    /// wind-up, so its lane shows exactly where it will go and stepping out of it works; and the impact cadence
    /// (CombatCadenceH8S) that keeps blows on the player a moment apart. Damage, ranges and cooldowns are untouched.
    /// </summary>
    public sealed partial class Creature
    {
        private GroundLaneH8S _laneH8S;
        private GroundRing _sweepH8S;
        private bool _aimLockedH8S;
        private Vector3 _lockedAimH8S = Vector3.forward;
        private float _lockedLeapH8S;
        private bool _cadenceHeldH8S;

        /// <summary>
        /// H8S: whether this creature counts as the player's foe, still readable after its death (IsHostileToPlayer answers
        /// false once dead): every creature except the starter habitat's calm residents.
        /// </summary>
        public bool FoeOfPlayerH8S { get { return Ready && !CalmHabitatResident; } }

        /// <summary>The timing this creature plays an attack with: difficulty, body size, the H8S floors, then the Vanguard rule.</summary>
        private AttackTiming TimingForH8S(CreatureAttack a)
        {
            AttackTiming t = CreatureAttackDb.TimingH8S(a, CombatDirector.EnemyWindupScale, Size, Settings.Difficulty, Radius, PartMelee);
            if (Options != null && Options.Vanguard04K)
                t = VanguardRules04K.Timing(t, Health.Health / Mathf.Max(1f, Health.MaxHealth));
            return t;
        }

        // ------------------------------------------------------------------ ground marks and the aim lock

        /// <summary>Starts the ground mark of the attack that just began (StartAttack calls it once its timing is set).</summary>
        private void BeginTellH8S(CreatureAttack a)
        {
            EndTellH8S(false);
            _aimLockedH8S = false;
            _lockedAimH8S = MathUtil.DirFromYaw(_yaw);
            _lockedLeapH8S = 0f;
            if (a == null || _flying) return;
            // A creature's blow only ever hits its own target, so a fight with someone else (police, a gang, a companion)
            // draws no lane or sweep ring that would warn the player of nothing. The aim lock still applies.
            if (Target == null || !Target.IsPlayer) return;
            TellShapeH8S shape = CombatTellsH8S.ShapeOf(a);
            Color c = CombatTellsH8S.ColorOf(CombatTellsH8S.SeverityOf(a.Flags));
            if (shape == TellShapeH8S.Sweep)
            {
                // Ring and Under areas keep their own ring (StartAttack); an all-round sweep gets one around the body.
                _sweepH8S = GroundRing.ShowHit(GroundPointH8S(), CombatTellsH8S.SweepHitRadius(Radius, a.Reach * PartMelee), _timing.Windup, c, c);
                return;
            }
            if (CombatTellsH8S.MarkFor(shape) != TellMarkH8S.Lane) return;
            Vector3 dir;
            float length;
            LanePathH8S(a, shape, out dir, out length);
            _laneH8S = GroundLaneH8S.Show(GroundPointH8S(), dir, length, CombatTellsH8S.LaneHalfWidth(Radius, a.Reach * PartMelee), _timing.Windup, c);
        }

        /// <summary>Each frame of the wind-up: the sweep ring follows the body, a lane follows the aim until it locks.</summary>
        private void UpdateTellH8S(CreatureAttack a)
        {
            if (_committed || a == null) return;
            if (_sweepH8S != null) _sweepH8S.Follow(GroundPointH8S());
            TellShapeH8S shape = CombatTellsH8S.ShapeOf(a);
            if (shape != TellShapeH8S.Lunge && shape != TellShapeH8S.Charge) return;
            if (!_aimLockedH8S && CombatTellsH8S.AimLocked(_atkT, _timing.Windup))
            {
                _aimLockedH8S = true;
                _lockedAimH8S = AimNowH8S();
                _lockedLeapH8S = Target != null ? MathUtil.FlatDistance(Target.transform.position, transform.position) : 0f;
            }
            if (_laneH8S == null || _laneH8S.Locked) return;
            Vector3 dir;
            float length;
            LanePathH8S(a, shape, out dir, out length);
            _laneH8S.Aim(GroundPointH8S(), dir, length);
            if (_aimLockedH8S) _laneH8S.Lock();
        }

        /// <summary>The wind-up is over: the mark flashes as the blow commits, or vanishes when it was broken.</summary>
        private void EndTellH8S(bool burst)
        {
            if (_laneH8S != null)
            {
                if (burst) _laneH8S.Burst();
                else _laneH8S.Release();
                _laneH8S = null;
            }
            if (_sweepH8S != null)
            {
                if (burst) _sweepH8S.Burst();
                else _sweepH8S.Release();
                _sweepH8S = null;
            }
        }

        /// <summary>
        /// The commit direction of a ground pounce or charge whose aim locked during the wind-up (false when it did not
        /// lock, flies, or is another kind of blow; Commit then aims as it always did).
        /// </summary>
        private bool LockedTravelAimH8S(CreatureAttack a, out Vector3 dir)
        {
            dir = _lockedAimH8S;
            if (!_aimLockedH8S || _flying || a == null) return false;
            TellShapeH8S s = CombatTellsH8S.ShapeOf(a);
            return s == TellShapeH8S.Lunge || s == TellShapeH8S.Charge;
        }

        /// <summary>The target distance a pounce launches for (CombatTellsH8S.LeapDistance: capped at its lane once locked).</summary>
        private float LeapDistanceH8S(float liveDistance)
        {
            return CombatTellsH8S.LeapDistance(liveDistance, _aimLockedH8S && !_flying, _lockedLeapH8S);
        }

        /// <summary>The direction a travelling blow would take right now (toward the target within MaxAimSnap of the facing).</summary>
        private Vector3 AimNowH8S()
        {
            Vector3 fwd = MathUtil.DirFromYaw(_yaw);
            if (Target == null) return fwd;
            return CombatTellsH8S.AimFor(fwd, Target.transform.position - transform.position, CombatTellsH8S.MaxAimSnap);
        }

        private void LanePathH8S(CreatureAttack a, TellShapeH8S shape, out Vector3 dir, out float length)
        {
            dir = _aimLockedH8S ? _lockedAimH8S : AimNowH8S();
            float dist = Target != null ? MathUtil.FlatDistance(Target.transform.position, transform.position) : 0f;
            length = CombatTellsH8S.LaneLength(shape, a.Speed, _timing.Active, dist, Radius, a.Reach * PartMelee);
        }

        /// <summary>The ground under the body (the cached foot plane, no ray).</summary>
        private Vector3 GroundPointH8S()
        {
            Vector3 p = transform.position;
            p.y = GroundY(p.x, p.z);
            return p;
        }

        // ------------------------------------------------------------------ impact cadence

        /// <summary>
        /// True when this blow on the player has to wait because it would land too close to another blow
        /// (CombatCadenceH8S). The creature keeps circling and tries again a moment later; nothing is reserved.
        /// </summary>
        private bool CadenceWaitsH8S(CreatureAttack a)
        {
            if (a == null || !CombatTellsH8S.LandsAtCommit(CombatTellsH8S.ShapeOf(a))) return false;
            if (CombatCadenceH8S.Fits(this, TimingForH8S(a).Windup)) return false;
            _nextAttackAt = Time.time + CombatCadenceH8S.RetrySeconds;
            return true;
        }

        /// <summary>Plans the landing moment of the blow that is starting (StartAttack, once its timing is set).</summary>
        private void ReserveCadenceH8S(CreatureAttack a)
        {
            _cadenceHeldH8S = false;
            if (a == null || Target == null || !Target.IsPlayer) return;
            if (_mite && Group != null && Group.Swarm) return;
            if (!CombatTellsH8S.LandsAtCommit(CombatTellsH8S.ShapeOf(a))) return;
            CombatCadenceH8S.Reserve(this, _timing.Windup);
            _cadenceHeldH8S = true;
        }

        /// <summary>A blow broken before it landed gives its planned moment back at once.</summary>
        private void ReleaseCadenceH8S()
        {
            if (!_cadenceHeldH8S) return;
            _cadenceHeldH8S = false;
            if (!_committed) CombatCadenceH8S.Release(this);
        }
    }
}
