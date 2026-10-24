using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Stealth awareness for people (gang members, posted enemies, and humanoid creatures through CreaturePerceives):
    /// awareness builds up with sight (a view cone, distance, line of sight, Malik crouching or sprinting, darkness) and
    /// with the sound of his movement, fades when nothing feeds it, turns the head toward the last stimulus when it is
    /// suspicious, raises the alarm and the nearby friends at one, and gives up a hunt that has lost sight of him for
    /// long enough. The numbers are in StealthRules; the HUD (StealthHud) reads AwarenessH8S. Takedowns hold the target
    /// still for the strike and end it silently (no threat report, so the friends are not alerted by the kill itself).
    /// </summary>
    public sealed partial class NpcController
    {
        /// <summary>How aware this person is of Malik: 0 unaware, StealthRules.Suspicious looking, 1 alarmed.</summary>
        public float AwarenessH8S { get; private set; }
        /// <summary>Where Malik was last sensed (what a suspicious person looks at).</summary>
        public Vector3 AwarenessSpotH8S { get { return _awareSpot; } }
        /// <summary>Seconds left of the alarm flash for the HUD.</summary>
        public float AlertFlashH8S { get { return Mathf.Max(0f, _alertFlashUntil - Time.time); } }
        /// <summary>This person is alarmed by Malik (awareness 1, or fighting him).</summary>
        public bool AlarmedH8S { get { return AwarenessH8S >= StealthRules.Alert || (State == NpcState.Fight && Target != null && Target.IsPlayer); } }
        /// <summary>Held still by a takedown in progress.</summary>
        public bool StealthHeldH8S { get { return Time.time < _heldUntil; } }
        /// <summary>Set by a takedown before the killing blow: the death reports no threat, so friends are not alerted by it.</summary>
        public bool SilentDeathH8S { get; private set; }

        private Vector3 _awareSpot;
        private float _awareLast = -99f;
        private float _awareSenseAt = -1f;
        private bool _alarmedH8S;
        private float _alertFlashUntil;
        private float _losLostFor;
        private float _heldUntil;

        /// <summary>A person who can be snuck up on at all: a hostile role on foot that is not a champion.</summary>
        public bool StealthTargetableH8S
        {
            get
            {
                if (IsDead || IsRagdolled || InVehicle || IsChampion) return false;
                return Role == NpcRole.Gang || Role == NpcRole.Enemy || Role == NpcRole.Police || Role == NpcRole.Swat;
            }
        }

        /// <summary>
        /// Sense Malik this think and report whether this person is alarmed by him. Replaces the plain line-of-sight test
        /// in Think for gang members and posted enemies, so they notice gradually and can be snuck past.
        /// </summary>
        private bool NoticesPlayerH8S(float range, float playerDist)
        {
            StealthSenseH8S(range, playerDist);
            return AwarenessH8S >= StealthRules.Alert;
        }

        /// <summary>
        /// Someone with no quarrel with Malik (a calm gang member, an officer with no warrant out) still notices him
        /// coming: awareness builds the same way but stops short of alarm, so a takedown on them needs their back turned.
        /// </summary>
        private void StealthWatchH8S(float range, float playerDist)
        {
            StealthSenseH8S(range, playerDist);
            if (AwarenessH8S > StealthRules.NoiseCap) AwarenessH8S = StealthRules.NoiseCap;
        }

        /// <summary>Sight for humanoid creatures (CreaturePerceives): the same gradual awareness with the species' range.</summary>
        private bool StealthCreatureSeesH8S(float range, float playerDist)
        {
            if (playerDist < range * 0.25f) return true;
            StealthSenseH8S(range, playerDist);
            return AwarenessH8S >= StealthRules.Alert;
        }

        /// <summary>One sensing step: sight within the cone with a clear line, and the noise of Malik's movement.</summary>
        private void StealthSenseH8S(float range, float playerDist)
        {
            float now = Time.time;
            float tdt = _awareSenseAt < 0f ? 0.2f : Mathf.Clamp(now - _awareSenseAt, 0.01f, 1f);
            _awareSenseAt = now;
            PlayerController p = Game.Player;
            if (p == null || p.IsDead)
            {
                AwarenessH8S = StealthRules.Step(AwarenessH8S, 0f, StealthRules.Alert, now - _awareLast, tdt);
                return;
            }
            Vector3 to = MathUtil.Flat(p.transform.position - transform.position);
            float angle = to.sqrMagnitude > 0.01f ? Vector3.Angle(transform.forward, to) : 0f;
            float cone = StealthRules.ConeFactor(playerDist, angle, range);
            float rate = 0f;
            float ceiling = StealthRules.Alert;
            if (cone > 0f && !Physics.Linecast(HeadPosition, p.ChestPosition, Layers.WorldMask, QueryTriggerInteraction.Ignore))
            {
                float night = Game.Atmo != null ? Game.Atmo.NightFactor : 0f;
                rate = StealthRules.SightRate(playerDist, range, cone, p.Crouched, p.FlatVelocity.magnitude, night);
            }
            float noise = p.StealthNoiseRadius;
            if (playerDist < noise)
            {
                float noiseRate = StealthRules.NoiseRate(playerDist, noise);
                if (noiseRate > rate)
                {
                    rate = noiseRate;
                    ceiling = StealthRules.NoiseCeiling(playerDist);
                }
            }
            if (rate > 0f)
            {
                _awareLast = now;
                _awareSpot = p.transform.position;
            }
            AwarenessH8S = StealthRules.Step(AwarenessH8S, rate, ceiling, now - _awareLast, tdt);
            if (AwarenessH8S >= StealthRules.Alert && !_alarmedH8S) RaiseAlarmH8S();
            else if (AwarenessH8S < StealthRules.Suspicious) _alarmedH8S = false;
        }

        /// <summary>Alarm: the flash for the HUD, and the friends nearby are put on their guard toward the same spot.</summary>
        private void RaiseAlarmH8S()
        {
            _alarmedH8S = true;
            _alertFlashUntil = Time.time + 1.2f;
            Vector3 here = transform.position;
            for (int i = 0; i < All.Count; i++)
            {
                NpcController n = All[i];
                if (n == null || n == this || n.IsDead || n.IsCreature) continue;
                if (n.Role != Role || (Role == NpcRole.Gang && n.GangId != GangId)) continue;
                if (MathUtil.FlatSqrDistance(n.transform.position, here) > StealthRules.ShoutRadius * StealthRules.ShoutRadius) continue;
                n.HearAlarmH8S(_awareSpot);
            }
        }

        /// <summary>A friend raised the alarm: look that way, most of the way to alarm.</summary>
        private void HearAlarmH8S(Vector3 spot)
        {
            if (AwarenessH8S >= StealthRules.ShoutAwareness) return;
            AwarenessH8S = StealthRules.ShoutAwareness;
            _awareSpot = spot;
            _awareLast = Time.time;
        }

        /// <summary>Set from the outside (a Surge boom, a loud landing right beside someone): awareness straight to alarm.</summary>
        public void StartleH8S(Vector3 spot)
        {
            if (IsDead || !StealthTargetableH8S) return;
            _awareSpot = spot;
            _awareLast = Time.time;
            if (AwarenessH8S < StealthRules.Alert)
            {
                AwarenessH8S = StealthRules.Alert;
                RaiseAlarmH8S();
            }
        }

        /// <summary>Every frame (from Update): fade stale awareness, and turn a suspicious idle person toward the last stimulus.</summary>
        private void StealthUpdateH8S(float dt)
        {
            if (IsCreature) return;
            if (State == NpcState.Fight && Target != null && Target.IsPlayer)
            {
                AwarenessH8S = StealthRules.Alert;
                return;
            }
            if (Time.time - _awareLast > StealthRules.DecayDelay && Time.time - _awareSenseAt > 0.5f)
                AwarenessH8S = StealthRules.Step(AwarenessH8S, 0f, StealthRules.Alert, Time.time - _awareLast, dt);
            if (AwarenessH8S >= StealthRules.Suspicious && AwarenessH8S < StealthRules.Alert && (State == NpcState.Idle || State == NpcState.Wander) && !StealthHeldH8S)
                FaceTowards(_awareSpot, dt);
        }

        /// <summary>Gang members staring down a nearby Malik only do so once they have noticed him.</summary>
        private bool StealthMayFaceH8S { get { return AwarenessH8S >= StealthRules.Suspicious * 0.6f; } }

        /// <summary>
        /// In a fight with Malik: out of sight for long enough and far enough, the hunter gives up, keeps watch near where
        /// he was last seen and can be snuck up on again. True when the fight ended this frame.
        /// </summary>
        private bool StealthLostH8S(float dt, float dist)
        {
            if (IsCreature || Target == null || !Target.IsPlayer || Role == NpcRole.Police || Role == NpcRole.Swat || Role == NpcRole.Companion)
            {
                _losLostFor = 0f;
                return false;
            }
            if (_hasLos)
            {
                _losLostFor = 0f;
                return false;
            }
            _losLostFor += dt;
            if (!StealthRules.LostTarget(_losLostFor, dist)) return false;
            _losLostFor = 0f;
            CancelAttack();
            CombatDirector.ReleaseShotToken(this);
            Target = null;
            AwarenessH8S = StealthRules.LostAwareness;
            _alarmedH8S = false;
            _awareSpot = _lastKnown;
            _awareLast = Time.time;
            if (Role == NpcRole.Enemy)
            {
                Hostile = false;
                GuardAt(_lastKnown);
            }
            else
            {
                Hostile = false;
                HostileUntil = 0f;
                SetState(NpcState.Idle);
            }
            if (Game.Player != null) Game.Player.OnHunterLostH8S();
            return true;
        }

        /// <summary>A takedown begins on this person: held still, facing away, for the strike.</summary>
        public void BeginHeldH8S(float seconds)
        {
            _heldUntil = Time.time + seconds;
            CancelAttack();
            _hasGoal = false;
            _vel = Vector3.zero;
            Anim.Velocity = Vector3.zero;
            Anim.Aiming = false;
        }

        /// <summary>The killing blow of a takedown: silent (no threat report), through the ordinary damage path.</summary>
        public void TakedownKillH8S(PlayerController by, Vector3 point, Vector3 direction)
        {
            if (IsDead || by == null) return;
            SilentDeathH8S = true;
            _heldUntil = 0f;
            DamageInfo info = DamageInfo.Make(StealthRules.LethalAmount(Health.Health, Health.Armor), point, direction, by.gameObject, DamageType.Melee);
            info.Flags = DamageFlags.Unblockable | DamageFlags.Knockdown | DamageFlags.Heavy;
            info.Force = 3f;
            info.Poise = 999f;
            Health.TakeDamage(info);
            if (Health.IsAlive)
            {
                // Something refused the kill (an invulnerable or scripted actor): no longer silent, and no longer held.
                SilentDeathH8S = false;
            }
        }
    }
}
