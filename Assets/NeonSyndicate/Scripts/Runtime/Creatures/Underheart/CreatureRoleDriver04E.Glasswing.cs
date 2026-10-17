using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// The Glasswing Ambusher's role (Ambusher): where a ceiling 3.2 to 14 m up exists over its post it hangs there upside
    /// down with its glass wings spread flat (the Third Eye makes it glow). It drops when the player passes under it,
    /// when it is hurt, or after 25 s of the fight anyway. Tell: glass dust trickles down and a white ring fills on the
    /// floor for 0.9 s with a screech; it lands in the ring (knockdown) and stands open for 0.8 s. Shot off the ceiling it
    /// lands reeling for 1.6 s instead and hurts nobody. Later (or badly hurt) it screeches, spreads its wings and climbs
    /// back up slowly (1.1 s, still hittable: a hit drops it), at most twice. Cracked wings end the climbing. With no
    /// ceiling (the open surface) it simply fights on foot.
    /// </summary>
    public sealed partial class CreatureRoleDriver04E
    {
        private enum GlassState { Ground, Perched, DropTell, Falling, Climbing }

        public const float GlassTriggerRadius = 6f;
        public const float GlassPatience = 25f;
        public const float GlassRingRadius = 1.8f;
        public const float GlassClimbSeconds = 1.1f;
        public const float GlassClimbTell = 0.6f;
        public const int GlassMaxClimbs = 2;

        private GlassState _gState = GlassState.Ground;
        private float _gT;
        private float _gFx;
        private Vector3 _perch;
        private Vector3 _climbFrom;
        private float _gFloorY;
        private bool _gShotDown;
        private int _climbs;
        private float _groundedFor;
        private readonly OwnedRing04E _gRing = new OwnedRing04E();

        private static readonly Color GlassRing = new Color(0.85f, 0.95f, 1f, 1f);

        private bool GlassControlling { get { return _gState != GlassState.Ground; } }

        private void ReadyGlasswing()
        {
            float ceiling;
            if (FindCeiling(Post, out ceiling)) Perch(Post, ceiling);
        }

        /// <summary>The ceiling over a floor point: 3.2 to 14 m up (false in the open or under a low roof).</summary>
        private static bool FindCeiling(Vector3 floorPoint, out float y)
        {
            y = 0f;
            RaycastHit hit;
            if (!Physics.Raycast(floorPoint + Vector3.up * 1f, Vector3.up, out hit, 13f, Layers.WorldMask, QueryTriggerInteraction.Ignore)) return false;
            float h = hit.point.y - floorPoint.y;
            if (h < 3.2f || h > 14f) return false;
            y = hit.point.y;
            return true;
        }

        private void Perch(Vector3 floorPoint, float ceilingY)
        {
            _gFloorY = floorPoint.y;
            _perch = new Vector3(floorPoint.x, ceilingY - 0.1f, floorPoint.z);
            Body.enabled = false;
            Body.RoleKeepListed();
            Body.transform.SetPositionAndRotation(_perch, Quaternion.Euler(0f, Body.RoleYaw, 180f));
            _gState = GlassState.Perched;
            _gT = 0f;
        }

        private void FrameGlasswing(float dt, bool engaged)
        {
            switch (_gState)
            {
                case GlassState.Perched:
                {
                    bool eye = Game.Abilities != null && Game.Abilities.ThirdEyeActive;
                    if (Body.CRig != null) Body.CRig.SetGlow(eye ? 3f : 1.1f);
                    Body.RoleKeepListed();
                    PlayerController p = Game.Player;
                    if (p == null || p.IsDead || ArenaOrLoose.Ending || _withdrawn) return;
                    Vector3 pp = PlayerGround;
                    float flat = MathUtil.FlatDistance(pp, _perch);
                    bool live = engaged || (Arena == null && Body.Options != null && Body.Options.Hostile);
                    if (!live) return;
                    if (UnderheartRoles04E.AmbusherDrops(flat, _perch.y - pp.y, false, _engagedFor, GlassTriggerRadius, GlassPatience)
                        && ArenaOrLoose.TryClaimSignature(Time.time, Def.TellSeconds + Def.ActiveSeconds)) BeginDropTell();
                    return;
                }
                case GlassState.DropTell:
                    _gT += dt;
                    _gFx -= dt;
                    if (_gFx <= 0f && Game.Fx != null)
                    {
                        _gFx = 0.22f;
                        Game.Fx.GlassBurst(_perch + Vector3.down * 0.3f, Body.Health.SurfaceTint, 4);
                    }
                    if (_gT >= Def.TellSeconds) Fall(false);
                    return;
                case GlassState.Falling:
                    _gT += dt;
                    if ((Body.RoleGrounded && _gT > 0.15f) || _gT > 2.5f) Land();
                    return;
                case GlassState.Climbing:
                    TickClimb(dt);
                    return;
                default:
                    if (Body.RoleGrounded) _groundedFor += dt;
                    return;
            }
        }

        private void BeginDropTell()
        {
            RaycastHit hit;
            Vector3 floor = new Vector3(_perch.x, _gFloorY, _perch.z);
            if (Physics.Raycast(_perch + Vector3.down * 0.6f, Vector3.down, out hit, 16f, Layers.WorldMask, QueryTriggerInteraction.Ignore)) floor = hit.point;
            _gFloorY = floor.y;
            _gRing.Show(floor, GlassRingRadius, Def.TellSeconds, GlassRing);
            Underheart04ESounds.Play(UhSfx.GlasswingScreech, _perch, 0.9f, 1f);
            _gState = GlassState.DropTell;
            _gT = 0f;
            _gFx = 0f;
        }

        /// <summary>Lets go of the ceiling (or the climb): upright, back under its own motion, falling to the floor.</summary>
        private void Fall(bool shotDown)
        {
            _gShotDown = shotDown;
            if (shotDown) _gRing.Release();
            Vector3 at = Body.transform.position;
            float hang = Mathf.Max(0.4f, Body.Size) + 0.2f;
            Vector3 start = _gState == GlassState.Climbing ? at - Vector3.up * hang : new Vector3(_perch.x, _perch.y - hang, _perch.z);
            Body.enabled = true;
            Body.RolePlace(start, Body.RoleYaw);
            _gState = GlassState.Falling;
            _gT = 0f;
        }

        private void Land()
        {
            Vector3 at = Body.transform.position;
            if (Game.Fx != null) Game.Fx.GlassBurst(at + Vector3.up * 0.3f, Body.Health.SurfaceTint, 16);
            if (!_gShotDown)
            {
                _gRing.Burst();
                PlayerController p = Game.Player;
                if (p != null && !p.IsDead)
                {
                    Vector3 pp = PlayerGround;
                    if (Mathf.Abs(pp.y - at.y) < 2.2f && MathUtil.FlatDistance(pp, at) <= GlassRingRadius + 0.4f + Mathf.Max(0.3f, p.BodyRadius))
                        HitPlayer(1.3f, at, DamageFlags.Knockdown, DamageType.Melee, 6f);
                }
                Body.Stagger(Def.PunishSeconds);
            }
            else
            {
                _gRing.Release();
                Body.Stagger(1.6f);
                Say("Shot down", new Color(0.85f, 0.95f, 1f));
            }
            _gState = GlassState.Ground;
            _groundedFor = 0f;
            _nextSignatureAt = Time.time + UnderheartRoles04E.Cooldown(Def, Random.value);
            PlayerController pl = Game.Player;
            if (pl != null && !pl.IsDead && (ArenaOrLoose.PlayerEngaged || (Body.Options != null && Body.Options.Hostile))) Body.RoleEngage(pl);
        }

        private void ThinkGlasswing(bool engaged)
        {
            if (_gState != GlassState.Ground || !HuntingPlayer || Body.RoleAttacking || Body.IsStaggered || !Body.RoleGrounded) return;
            bool retreat = !_retreated && HealthFraction < Def.RetreatHealth;
            if (PartBroken(PartEffect.NoAbility) || _climbs >= GlassMaxClimbs)
            {
                if (retreat)
                {
                    _retreated = true;
                    Body.RoleRetreat(PlayerGround, Def.RetreatSeconds);
                }
                return;
            }
            if (!retreat && (Time.time < _nextSignatureAt || _groundedFor < 10f)) return;
            float ceiling;
            if (!FindCeiling(Body.transform.position, out ceiling)) return;
            if (!ArenaOrLoose.TryClaimSignature(Time.time, GlassClimbTell + GlassClimbSeconds)) return;
            if (retreat) _retreated = true;
            _perch = new Vector3(Body.transform.position.x, ceiling - 0.1f, Body.transform.position.z);
            _gFloorY = Body.transform.position.y;
            Body.RoleCancelAttack();
            Body.RoleCry();
            Underheart04ESounds.Play(UhSfx.GlasswingScreech, Body.HeadPosition, 0.7f, 1.15f);
            _gState = GlassState.Climbing;
            _gT = 0f;
        }

        private void TickClimb(float dt)
        {
            _gT += dt;
            if (_gT < GlassClimbTell) return;
            if (Body.enabled)
            {
                // The tell is over: it lets its own motion go and hauls itself up.
                Body.enabled = false;
                Body.RoleKeepListed();
                _climbFrom = Body.transform.position;
            }
            float k = Mathf.Clamp01((_gT - GlassClimbTell) / GlassClimbSeconds);
            float e = k * k * (3f - 2f * k);
            Vector3 pos = Vector3.Lerp(_climbFrom, _perch, e);
            float roll = Mathf.Lerp(0f, 180f, Mathf.Clamp01((k - 0.7f) / 0.3f));
            Body.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, Body.RoleYaw, roll));
            Body.RoleKeepListed();
            if (k < 1f) return;
            _climbs++;
            _gState = GlassState.Perched;
            _gT = 0f;
            _engagedFor = 0f;
        }

        private void HurtGlasswing(DamageInfo info)
        {
            if (Body.Health.LastApplied <= 0f) return;
            if (_gState == GlassState.Perched || _gState == GlassState.DropTell) Fall(true);
            else if (_gState == GlassState.Climbing && _gT >= GlassClimbTell) Fall(true);
        }

        private void DeathGlasswing()
        {
            _gRing.Release();
            if (!Body.enabled)
            {
                // Killed on the ceiling or mid-climb: upright again so its death drops it to the floor and it dissolves.
                Vector3 at = Body.transform.position - Vector3.up * (Mathf.Max(0.4f, Body.Size) + 0.2f);
                Body.enabled = true;
                Body.RolePlace(at, Body.RoleYaw);
            }
            _gState = GlassState.Ground;
        }

        private void StopGlasswing()
        {
            _gRing.Release();
            if (_gState == GlassState.DropTell) _gState = GlassState.Perched;
        }
    }
}
