using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// 04E Underheart role seam on the existing creature brain (same pattern as Creature.StarterHabitat and
    /// Creature.Territory04B: no second combat AI). The role and resident drivers in Creatures/Underheart steer through
    /// these few controls: hold a post or engage, retreat without the leash heal, delay or cancel attacks, take a goal,
    /// hide while tunnelling, and (for bodies the driver flies or hangs itself) place and animate the rig directly.
    /// Body, attacks, damage, parts, loot and death stay the creature's own.
    /// </summary>
    public sealed partial class Creature
    {
        private bool _roleHolding;
        /// <summary>Ranged roles: the distance it keeps from its target while hunting (0 = the usual melee dance). Read by
        /// HuntSteer in Creature.Motion.cs.</summary>
        private float _roleKeepRange;

        /// <summary>Sets the distance a ranged role keeps from its target (0 restores the melee dance).</summary>
        public void RoleKeepRange(float metres)
        {
            _roleKeepRange = Mathf.Max(0f, metres);
        }

        /// <summary>Starts the creature's first call (a roar, rattle or screech) when it is free: a role's audible tell.</summary>
        public bool RoleCry()
        {
            if (_dead || !Ready || _atk >= 0 || IsStaggered) return false;
            int call = FindAttack(AttackShape.Call);
            if (call < 0) return false;
            StartAttack(call);
            return true;
        }

        /// <summary>True while a driver keeps this creature at its post (it ignores the player until released).</summary>
        public bool RoleHolding { get { return _roleHolding; } }
        /// <summary>An attack is playing (wind-up, active or recovery).</summary>
        public bool RoleAttacking { get { return _atk >= 0; } }
        /// <summary>The creature's heading in degrees.</summary>
        public float RoleYaw { get { return _yaw; } }
        /// <summary>Seconds the territorial warning has run (0 when it is not warning).</summary>
        public float RoleWarnElapsed { get { return Mode == CreatureMode.Warn ? _warnedFor : 0f; } }
        /// <summary>True when the creature is on the ground (not falling).</summary>
        public bool RoleGrounded { get { return _grounded; } }
        /// <summary>The attack set this creature plays.</summary>
        public CreatureAttack[] RoleAttacks { get { return _attacks; } }

        /// <summary>
        /// Keeps the creature at a post: it stops fighting, forgets its target and walks back, and stays calm (it does not
        /// notice the player) until RoleEngage. A hit from the player still provokes it as usual.
        /// </summary>
        public void RoleHold(Vector3 post)
        {
            if (_dead || Options == null) return;
            Home = post;
            if (_roleHolding && Mode != CreatureMode.Hunt) return;
            _roleHolding = true;
            Options.CalmUntilProvoked = true;
            Provoked = false;
            _plannedHostile = false;
            Target = null;
            CancelAttack();
            ReleaseTokens();
            SetMode(CreatureMode.Wander);
            SetGoal(post, _sp != null ? Mathf.Max(_sp.WalkSpeed * 1.6f, _sp.RunSpeed * 0.6f) : 3f);
        }

        /// <summary>Releases a held creature into the fight against target (the player).</summary>
        public void RoleEngage(CharacterBase target)
        {
            if (_dead || Options == null) return;
            bool was = _roleHolding;
            _roleHolding = false;
            Options.CalmUntilProvoked = false;
            _plannedHostile = Options.Hostile;
            if (target != null && (was || Mode != CreatureMode.Hunt || Target != target)) BeginHunt(target);
        }

        /// <summary>
        /// Hands a held creature back to its own temperament without starting a fight (a kiln crab home at its nest
        /// warns again as usual; calm keeps it a calm resident).
        /// </summary>
        public void RoleRelease(bool calm)
        {
            if (_dead || Options == null) return;
            _roleHolding = false;
            Options.CalmUntilProvoked = calm;
            Provoked = false;
            _plannedHostile = Options.Hostile;
            Target = null;
            CancelAttack();
            ReleaseTokens();
            SetMode(CreatureMode.Wander);
        }

        /// <summary>
        /// Runs away from a point for some seconds (a role's retreat). Unlike the leash it never heals: the driver turns
        /// it back into the fight when the time is up.
        /// </summary>
        public void RoleRetreat(Vector3 from, float seconds)
        {
            if (_dead || !Ready) return;
            Flee(from, Mathf.Max(0.5f, seconds));
        }

        /// <summary>True while a role retreat (or any flight) runs; Return is where the brain would start its leash heal.</summary>
        public bool RoleFleeing { get { return Mode == CreatureMode.Flee; } }
        public bool RoleReturning { get { return Mode == CreatureMode.Return; } }

        /// <summary>Ends a retreat or a leash walk at once and goes back to fighting the target.</summary>
        public void RoleResume(CharacterBase target)
        {
            if (_dead || target == null) return;
            BeginHunt(target);
        }

        /// <summary>Holds its next attack back for at least seconds (a scavenger that is being watched).</summary>
        public void RoleDelayAttacks(float seconds)
        {
            _nextAttackAt = Mathf.Max(_nextAttackAt, Time.time + Mathf.Max(0f, seconds));
        }

        /// <summary>Stops the attack winding up now (its ring fades, its tokens are released).</summary>
        public void RoleCancelAttack()
        {
            CancelAttack();
        }

        /// <summary>Puts an attack of its set on cooldown for at least seconds (a doused furnace, a broken gland).</summary>
        public void RoleCooldown(AttackShape shape, float seconds)
        {
            float until = Time.time + Mathf.Max(0f, seconds);
            for (int i = 0; i < _attacks.Length && i < _readyAt.Length; i++)
                if (_attacks[i].Shape == shape) _readyAt[i] = Mathf.Max(_readyAt[i], until);
        }

        /// <summary>Steers toward a point at a speed (the brain drops it when the goal is reached or it changes its mind).</summary>
        public void RoleGoal(Vector3 goal, float speed)
        {
            if (_dead || !Ready) return;
            SetGoal(goal, Mathf.Max(0.3f, speed));
        }

        /// <summary>Hides or shows the body (tunnelling underground): renderer and hit volumes together.</summary>
        public void RoleSetHidden(bool hidden)
        {
            if (CRig == null) return;
            if (CRig.Renderer != null) CRig.Renderer.enabled = !hidden;
            CRig.SetCollidersEnabled(!hidden && !_dead);
        }

        /// <summary>Places the body at once (after a tunnel, a drop or a climb): ground cache, motion and feet reset.</summary>
        public void RolePlace(Vector3 pos, float yaw)
        {
            if (!Ready) return;
            _yaw = yaw;
            _vel = Vector3.zero;
            _vy = 0f;
            _push = Vector3.zero;
            _lastPos = pos;
            _hasGoal = false;
            transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, yaw, 0f));
            ProbeGround(pos, 0f, true);
            CRig.Pose.Reset(pos, yaw);
        }

        /// <summary>Re-lists a creature its driver has paused (enabled = false) so lock-on and the HUD still see it.</summary>
        public void RoleKeepListed()
        {
            if (Ready && !_dead && !All.Contains(this)) All.Add(this);
        }

        /// <summary>
        /// Animates a creature whose driver moves it (the component paused): flying (a drifting moth) or hanging still
        /// (a glasswing on the ceiling). velocity and altitude shape the pose; the glow follows.
        /// </summary>
        public void RoleAnimate(float dt, Vector3 velocity, bool flying, float altitude, float glow)
        {
            if (!Ready || CRig == null || dt <= 0f) return;
            CreaturePoseInput p = CreaturePoseInput.Idle(transform.position, _yaw);
            p.Velocity = velocity;
            p.Grounded = !flying;
            p.Flying = flying;
            p.Altitude = altitude;
            p.Alert = flying ? 0f : 0.3f;
            p.Hurt = _hurt;
            p.HurtDirection = _hurtDir;
            p.GroundHeight = _groundFn;
            p.Time = Time.time;
            CRig.Pose.Solve(ref p, Mathf.Min(dt, 0.1f));
            CRig.ApplyPose();
            CRig.SetGlow(glow);
            _hurt = Mathf.MoveTowards(_hurt, 0f, dt * 3f);
        }

        /// <summary>Turns the creature to a heading (used while the driver moves it).</summary>
        public void RoleSetYaw(float yaw)
        {
            _yaw = yaw;
        }
    }
}
