using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// The Ash Burrower's role (Armoured): the burrow ambush. Tell: it rattles its plates and digs in for 0.7 s (a stagger
    /// breaks the dig), then a dust trail runs under the floor toward the player; where the trail stops an ash ring fills
    /// for 1.1 s; then it bursts up there (knockdown, unblockable: step out of the ring) and rears open for 1.6 s (a
    /// lightning hit stuns it longer). While it is under the floor it cannot be hit and is off the lock-on list. Badly hurt
    /// it dives once and surfaces away from the player to recover. A broken digging beak ends all of this.
    /// </summary>
    public sealed partial class CreatureRoleDriver04E
    {
        private enum BurrowState { None, Digging, Tunnel, Ring, Rearing }

        public const float BurrowDig = 0.7f;
        public const float BurrowTunnelSpeed = 6.5f;
        public const float BurrowTunnelMax = 2.4f;
        public const float BurrowRingRadius = 2.6f;

        private BurrowState _bState;
        private float _bT;
        private float _bFx;
        private float _bRumble;
        private bool _bRetreat;
        private Vector3 _bTarget;
        private Vector3 _bLastSafe;
        private readonly OwnedRing04E _bRing = new OwnedRing04E();

        private static readonly Color AshRing = new Color(1f, 0.55f, 0.25f, 1f);

        private bool BurrowControlling { get { return _bState == BurrowState.Tunnel || _bState == BurrowState.Ring; } }

        private void ThinkBurrower(bool engaged)
        {
            if (_bState != BurrowState.None || !FreeForSignature || PartBroken(PartEffect.NoAbility)) return;
            if (!_retreated && HealthFraction < Def.RetreatHealth)
            {
                _retreated = true;
                StartBurrow(true);
                return;
            }
            Vector3 pp = PlayerGround;
            if (!WorldBounds.SameLayer(pp, Body.transform.position)) return;
            float d = MathUtil.FlatDistance(pp, Body.transform.position);
            if (!UnderheartRoles04E.InSignatureRange(Def, d)) return;
            if (!ArenaOrLoose.TryClaimSignature(Time.time, BurrowDig + BurrowTunnelMax + Def.TellSeconds + Def.ActiveSeconds)) return;
            StartBurrow(false);
        }

        private void StartBurrow(bool retreat)
        {
            _bState = BurrowState.Digging;
            _bT = 0f;
            _bRetreat = retreat;
            _bLastSafe = Body.transform.position;
            Body.RoleCancelAttack();
            Body.RoleCry();
            Underheart04ESounds.Play(UhSfx.BurrowerEmerge, Body.transform.position, 0.7f, 0.75f);
        }

        private void FrameBurrower(float dt)
        {
            switch (_bState)
            {
                case BurrowState.Digging:
                    _bT += dt;
                    Dust(dt, Body.transform.position, 0.12f);
                    if (Body.IsStaggered)
                    {
                        // Broken out of the dig: the ambush is lost and it stands there reeling.
                        _bState = BurrowState.None;
                        _nextSignatureAt = Time.time + Def.CooldownMin * 0.5f;
                        Say("Dig broken", new Color(1f, 0.8f, 0.45f));
                        return;
                    }
                    if (_bT < BurrowDig) return;
                    Body.RoleSetHidden(true);
                    Body.enabled = false;
                    if (Game.Fx != null) Game.Fx.GroundDust(Body.transform.position, 1.2f, 12);
                    _bState = BurrowState.Tunnel;
                    _bT = 0f;
                    return;
                case BurrowState.Tunnel:
                    TickTunnel(dt);
                    return;
                case BurrowState.Ring:
                    _bT += dt;
                    Dust(dt, _bTarget, 0.25f);
                    if (_bT >= Def.TellSeconds) Erupt(true);
                    return;
                case BurrowState.Rearing:
                    _bT += dt;
                    if (_bT >= Def.PunishSeconds) _bState = BurrowState.None;
                    return;
            }
        }

        private void TickTunnel(float dt)
        {
            _bT += dt;
            Vector3 pos = Body.transform.position;
            Vector3 goal = _bRetreat ? RetreatPoint(pos) : PlayerGround;
            goal = InsideArena(goal, 2f);
            Vector3 to = MathUtil.Flat(goal - pos);
            float d = to.magnitude;
            if (d > 0.05f) pos += to / d * Mathf.Min(d, BurrowTunnelSpeed * dt);
            float y;
            if (Floor(pos, out y) && Mathf.Abs(y - pos.y) < 2.5f)
            {
                pos.y = y;
                if (SafeFromHeat(pos)) _bLastSafe = pos;
            }
            else pos = _bLastSafe;
            Body.transform.position = pos;
            Dust(dt, pos, 0.12f);
            _bRumble -= dt;
            if (_bRumble <= 0f)
            {
                _bRumble = 0.8f;
                CreatureAudio.Play(Body.Species, CreatureCry.Rumble, pos, Body.Size);
            }
            bool arrived = d < 0.8f || _bT >= BurrowTunnelMax;
            if (!arrived) return;
            if (_bRetreat)
            {
                _bTarget = _bLastSafe;
                Erupt(false);
                return;
            }
            _bTarget = _bLastSafe;
            _bRing.Show(_bTarget, BurrowRingRadius, Def.TellSeconds, AshRing);
            _bState = BurrowState.Ring;
            _bT = 0f;
        }

        /// <summary>A surfacing point 10 m from the player on the far side of the burrower (inside the arena).</summary>
        private Vector3 RetreatPoint(Vector3 from)
        {
            Vector3 away = MathUtil.Flat(from - PlayerGround);
            if (away.sqrMagnitude < 0.01f) away = Body.transform.forward;
            return PlayerGround + away.normalized * 10f;
        }

        /// <summary>Bursts up at the tunnel's end: the ring bursts, the player in it is knocked down, and it rears open.</summary>
        private void Erupt(bool damage)
        {
            _bRing.Burst();
            Body.enabled = true;
            Body.RoleSetHidden(false);
            Vector3 to = MathUtil.Flat(PlayerGround - _bTarget);
            Body.RolePlace(_bTarget, to.sqrMagnitude > 0.01f ? MathUtil.YawOf(to) : Body.RoleYaw);
            if (Game.Fx != null)
            {
                for (int i = 0; i < 6; i++)
                {
                    float a = i / 6f * Mathf.PI * 2f;
                    Game.Fx.GroundDust(_bTarget + new Vector3(Mathf.Cos(a), 0.1f, Mathf.Sin(a)) * 1.6f, 1f, 4);
                }
                Game.Fx.CrystalBurst(_bTarget + Vector3.up * 0.4f, Body.Health.SurfaceTint, 18, 1.4f);
            }
            Underheart04ESounds.Play(UhSfx.BurrowerEmerge, _bTarget, 1f, 1f);
            if (Game.Cam != null && MathUtil.FlatDistance(Game.Cam.transform.position, _bTarget) < 20f) Game.Cam.Shake(0.25f);
            if (damage)
            {
                PlayerController p = Game.Player;
                if (p != null && !p.IsDead)
                {
                    Vector3 pp = PlayerGround;
                    float reach = BurrowRingRadius + 0.3f + Mathf.Max(0.3f, p.BodyRadius);
                    if (Mathf.Abs(pp.y - _bTarget.y) < 2.6f && MathUtil.FlatDistance(pp, _bTarget) <= reach)
                        HitPlayer(1.4f, _bTarget, DamageFlags.Knockdown | DamageFlags.Unblockable | DamageFlags.Heavy, DamageType.Melee, 10f);
                }
                Body.Stagger(Def.PunishSeconds);
            }
            else
            {
                // The retreat dive: it surfaces away from you with its balance back.
                Body.Poise = Body.MaxPoise;
                Body.RoleCry();
            }
            _bState = BurrowState.Rearing;
            _bT = 0f;
            _nextSignatureAt = Time.time + UnderheartRoles04E.Cooldown(Def, Random.value);
        }

        private void Dust(float dt, Vector3 at, float every)
        {
            _bFx -= dt;
            if (_bFx > 0f || Game.Fx == null) return;
            _bFx = every;
            if (MathUtil.FlatDistance(at, Game.PlayerPosition) > 60f) return;
            Game.Fx.GroundDust(at + Vector3.up * 0.1f, 0.7f, 3);
        }

        private void HurtBurrower(DamageInfo info)
        {
            // Lightning while it rears: a longer stun (its weakness made plain).
            if (_bState == BurrowState.Rearing && info.Type == DamageType.Power && Body.Health.LastApplied > 0f)
            {
                Body.Stagger(1f);
                Say("Stunned", new Color(0.7f, 0.85f, 1f));
            }
        }

        private void DeathBurrower()
        {
            _bRing.Release();
            if (_bState == BurrowState.Tunnel || _bState == BurrowState.Ring)
            {
                // Killed under the floor (a burn): it comes up where it is, so its death plays and the body clears.
                Body.enabled = true;
                Body.RoleSetHidden(false);
                Body.RolePlace(_bLastSafe, Body.RoleYaw);
            }
            _bState = BurrowState.None;
        }

        private void StopBurrower()
        {
            _bRing.Release();
            if ((_bState == BurrowState.Tunnel || _bState == BurrowState.Ring) && Body != null && !Body.IsDead)
            {
                // The fight ended while it was under the floor: it surfaces harmlessly where it is.
                Body.enabled = true;
                Body.RoleSetHidden(false);
                Body.RolePlace(_bLastSafe, Body.RoleYaw);
            }
            if (_bState != BurrowState.Rearing) _bState = BurrowState.None;
        }
    }
}
