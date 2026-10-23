using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// H8S follow spacing for the live companion body, called from FollowStep (CompanionController.Motion): the hurt
    /// companion's sheltered slot behind the player, keeping its body out of the camera's line to the player (another
    /// side, a wider slot, or a lower hover in a tight corridor), and stepping out of the lane the player walks along
    /// (aside when there is room, ahead to lead the way when there is not). Every move it chooses is checked with the
    /// existing ClearPath and heat tests and steered by the existing SteerTo sweep, so it never passes through walls.
    /// Decisions are the pure CompanionSpacingRulesH8S; checks run a few times a second, not every frame.
    /// </summary>
    public sealed partial class CompanionController
    {
        /// <summary>Set by the bond system: hurt in a fight, it keeps the player between itself and ShelterFromH8S.</summary>
        public bool ShelterH8S;
        /// <summary>The threat it shelters from (a world point), valid while ShelterH8S is set.</summary>
        public Vector3 ShelterFromH8S;
        /// <summary>True while it steps aside for the player or leads the way (the card reads it).</summary>
        public bool YieldingH8S { get { return _yieldH8S && Mode == CompanionMode.Follow; } }

        private bool _tightH8S;
        private float _viewTimerH8S;
        private bool _viewAltH8S;
        private Vector3 _viewOffsetH8S;
        private float _yieldTimerH8S;
        private bool _yieldH8S;
        private Vector3 _yieldPointH8S;

        /// <summary>The follow slot: the sheltered slot while hurt in a fight, the usual one beside the player otherwise.</summary>
        private Vector3 FollowSlotH8S(PlayerController pl, Vector3 pp)
        {
            if (ShelterH8S)
                return CompanionSpacingRulesH8S.ShelterSlot(pp, ShelterFromH8S, pl.transform.forward, Profile.Side, Profile.FollowDistance + CompanionSpacingRulesH8S.ShelterExtra);
            return CompanionFollowRules.Slot(pp, pl.transform.forward, Profile.Side, Profile.FollowDistance + (Combat ? -0.4f : 0f));
        }

        /// <summary>The height of the body's centre above a floor point (hovering bodies at their current hover).</summary>
        private float CentreHeightH8S()
        {
            if (Profile.Locomotion == CompanionLocomotion.Hover)
                return CompanionSpacingRulesH8S.HoverFor(Profile.HoverHeight, _tightH8S, Profile.BodyHeight) + Profile.BodyHeight * 0.4f;
            return Mathf.Max(0.2f, Profile.BodyHeight * 0.55f);
        }

        /// <summary>
        /// Keeps a follow target out of the camera's line to the player. Re-decided every 0.2 s: when the target would
        /// block the view it tries (allowSide, direct steering only) the mirrored slot and then a wider one, each a clear,
        /// cool path; with no such spot (a narrow corridor, or while walking the breadcrumb trail, which must keep to the
        /// player's own path) a hovering body flies low instead. Returns the target to steer to.
        /// </summary>
        private Vector3 KeepViewClearH8S(Vector3 target, Vector3 pos, Vector3 pp, PlayerController pl, float dt, bool allowSide)
        {
            Camera cam = Game.Cam != null ? Game.Cam.Cam : null;
            if (cam == null || pl == null)
            {
                _viewAltH8S = false;
                _tightH8S = false;
                return target;
            }
            _viewTimerH8S -= dt;
            if (_viewTimerH8S <= 0f)
            {
                _viewTimerH8S = 0.2f;
                _viewAltH8S = false;
                _tightH8S = false;
                Vector3 camPos = cam.transform.position;
                Vector3 chest = pl.ChestPosition;
                float r = Mathf.Max(0.15f, Profile.BodyRadius);
                if (CompanionSpacingRulesH8S.OccludesView(camPos, chest, target + Vector3.up * CentreHeightH8S(), r))
                {
                    Vector3 f = MathUtil.Flat(pl.transform.forward);
                    f = f.sqrMagnitude > 1e-4f ? f.normalized : Vector3.forward;
                    Vector3 right = new Vector3(f.z, 0f, -f.x);
                    float side = Profile.Side >= 0 ? 1f : -1f;
                    // While sheltering the slot behind the player is the point: no side spots then (a flyer flies low instead).
                    for (int k = 0; allowSide && !ShelterH8S && k < 3 && !_viewAltH8S; k++)
                    {
                        // The other side first, then both sides a little wider.
                        float s = k == 0 ? -side : (k == 1 ? side : -side);
                        float width = k == 0 ? 1.2f : 1.9f;
                        Vector3 c = pp - f * 0.6f + right * (s * (width + r));
                        if (CompanionSpacingRulesH8S.OccludesView(camPos, chest, c + Vector3.up * CentreHeightH8S(), r)) continue;
                        if (!ClearPath(pos, c) || HazardPoint(c)) continue;
                        _viewOffsetH8S = c - pp;
                        _viewAltH8S = true;
                    }
                    if (!_viewAltH8S && Profile.Locomotion == CompanionLocomotion.Hover) _tightH8S = true;
                }
                else if (_narrowSlot04I && Profile.Locomotion == CompanionLocomotion.Hover) _tightH8S = true;
            }
            return _viewAltH8S && !ShelterH8S ? pp + _viewOffsetH8S : target;
        }

        /// <summary>
        /// Started while idle beside the player and kept until the player's lane is clear: when the player walks toward
        /// it (it stands in their lane) it steps aside, the side it leans to first, or leads the way ahead (inside its idle
        /// radius) when neither side is clear. Walkers also need floor at the destination. Re-decided every 0.15 s. True
        /// with the point to move to while it should yield.
        /// </summary>
        private bool YieldH8S(Vector3 pos, Vector3 pp, PlayerController pl, float dt, out Vector3 point)
        {
            _yieldTimerH8S -= dt;
            if (_yieldTimerH8S <= 0f)
            {
                _yieldTimerH8S = 0.15f;
                _yieldH8S = false;
                Vector3 v = pl.Velocity;
                float half = 0.45f + Mathf.Max(0.15f, Profile.BodyRadius);
                if (!pl.InVehicle && CompanionSpacingRulesH8S.InPlayerLane(pp, v, pos, half, CompanionSpacingRulesH8S.LaneLookAhead))
                {
                    Vector3 dir = MathUtil.Flat(v);
                    dir = dir.sqrMagnitude > 1e-4f ? dir.normalized : MathUtil.Flat(pl.transform.forward).normalized;
                    for (int k = 0; k < 2 && !_yieldH8S; k++)
                    {
                        Vector3 c = CompanionSpacingRulesH8S.SidestepPoint(pp, dir, pos, Profile.Side, half, k == 1);
                        c.y = pos.y;
                        if (!YieldSpotOkH8S(pos, c)) continue;
                        _yieldPointH8S = c;
                        _yieldH8S = true;
                    }
                    if (!_yieldH8S)
                    {
                        Vector3 lead = CompanionSpacingRulesH8S.LeadPoint(pp, dir, CompanionSpacingRulesH8S.LeadFor(Profile.FollowDistance));
                        lead.y = pos.y;
                        if (YieldSpotOkH8S(pos, lead))
                        {
                            _yieldPointH8S = lead;
                            _yieldH8S = true;
                        }
                    }
                }
            }
            point = _yieldH8S ? _yieldPointH8S : pos;
            return _yieldH8S;
        }

        /// <summary>
        /// A spot to step to: a clear, cool path from here and, for walkers, solid floor at the spot itself (not only at the
        /// midpoint ClearPath samples), level with where it stands, so leading the way never steps off a drop.
        /// </summary>
        private bool YieldSpotOkH8S(Vector3 from, Vector3 spot)
        {
            if (!ClearPath(from, spot) || HazardPoint(spot)) return false;
            if (CanFly) return true;
            RaycastHit hit;
            if (!Physics.Raycast(new Vector3(spot.x, from.y + 1f, spot.z), Vector3.down, out hit, 2.6f, Layers.WorldMask, QueryTriggerInteraction.Ignore)) return false;
            return hit.normal.y > 0.7f && Mathf.Abs(hit.point.y - from.y) < 0.8f;
        }

        /// <summary>Drops a yield outside Follow mode, so no stale point or card text survives a wait, a goal or a rescue.</summary>
        private void ClearYieldH8S()
        {
            _yieldH8S = false;
            _yieldTimerH8S = 0f;
        }

        /// <summary>Forgets the spacing decisions (teleports, mode changes): the next checks start fresh.</summary>
        private void ResetSpacingH8S()
        {
            _viewTimerH8S = 0f;
            _viewAltH8S = false;
            _tightH8S = false;
            _yieldTimerH8S = 0f;
            _yieldH8S = false;
        }
    }
}
