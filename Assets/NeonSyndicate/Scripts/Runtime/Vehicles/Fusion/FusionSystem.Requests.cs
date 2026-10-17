using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Fusion requests: the live facts behind the pure validation, the physical checks (overhead clearance and the swept
    /// transition box against real colliders), the verified spot where a returning companion stands, and the two
    /// adapters the state machine drives (Game.Bonds for the companion, the form view on the bike).
    /// </summary>
    public sealed partial class FusionSystem
    {
        private const int BlockerMask = (1 << Layers.Vehicles) | (1 << Layers.VehicleShell) | (1 << Layers.Debris);
        private readonly Collider[] _overlap = new Collider[24];
        private float _canFuseAt = -1f;
        private bool _canFuseCached;
        private string _canFuseReason = "";

        /// <summary>The first-fusion quest id (Two Hearts, One Chassis).</summary>
        public const string FirstFusionQuest = "uh05_05_chassis";

        /// <summary>True when the companion can fuse with the player's current chassis right now (validation and the
        /// physical checks, refreshed at most four times a second).</summary>
        public bool CanFuse(out string reason)
        {
            if (Time.unscaledTime - _canFuseAt < 0.25f && _canFuseAt >= 0f)
            {
                reason = _canFuseReason;
                return _canFuseCached;
            }
            _canFuseAt = Time.unscaledTime;
            Vehicle v;
            CompanionRecord c;
            FusionVerdict verdict = FusionRules.Validate(GatherFacts(out v, out c));
            if (verdict.Ok)
            {
                string why;
                if (!PhysicalCheck(v, verdict.Form, out why)) verdict = FusionVerdict.Deny(FusionDenial.Blocked, why);
            }
            _canFuseCached = verdict.Ok;
            _canFuseReason = verdict.Ok ? "" : verdict.Message;
            reason = _canFuseReason;
            return verdict.Ok;
        }

        /// <summary>Starts a fusion (validation first; nothing changes unless every check passes).</summary>
        public bool RequestFuse(out string reason)
        {
            _canFuseAt = -1f;
            Vehicle v;
            CompanionRecord comp;
            FusionVerdict verdict = FusionRules.Validate(GatherFacts(out v, out comp));
            _firstFusion = verdict.FirstFusion;
            _calibrates = verdict.CalibratesForm;
            _formPort.Prepare(v);
            bool ok = _machine.RequestFuse(verdict, comp != null ? comp.Uid : 0, _formPort, _companionPort, out reason);
            if (!ok)
            {
                _formPort.Prepare(null);
                if (verdict.Ok) Log.Info("FUSION_04E fuse refused after validation: " + reason);
                return false;
            }
            _vehicle = v;
            _announcedFused = false;
            _gliding = false;
            Underheart04ESounds.Play(UhSfx.FuseStart, v.transform.position, 1f, 1f);
            if (Game.Fx != null) Game.Fx.Flash(v.transform.position + Vector3.up * 1f, FormColor(verdict.Form), 2.2f, 7f, 0.5f);
            Toast("Fusing with " + CompanionName() + "...", false);
            return true;
        }

        /// <summary>Starts a safe separation (verifies support and clearance for the returned companion).</summary>
        public bool RequestSeparate(out string reason)
        {
            reason = "";
            if (State != FusionState.Fused)
            {
                reason = State == FusionState.Separate ? "You are not fused." : "The coupler is still settling.";
                return false;
            }
            SpotResult spot = FindCompanionSpot(_vehicle != null ? _vehicle.transform : null);
            FusionVerdict sv = FusionRules.ValidateSeparate(State, _vehicle != null ? _vehicle.Speed : 99f, _vehicle != null ? _vehicle.GroundedWheels : 0, spot.Found);
            if (!sv.Ok)
            {
                // The machine records the refusal and stays fused.
                _machine.RequestSeparate(sv, out reason);
                return false;
            }
            _companionPort.PrepareSpot(spot);
            bool ok = _machine.RequestSeparate(sv, out reason);
            if (ok)
            {
                _gliding = false;
                Underheart04ESounds.Play(UhSfx.SeparateStart, _vehicle.transform.position, 1f, 1f);
                if (_view != null) _view.SetTrails(false);
            }
            return ok;
        }

        /// <summary>Everything the validation needs, read from the live world in one place.</summary>
        private FusionFacts GatherFacts(out Vehicle v, out CompanionRecord comp)
        {
            FusionFacts f = new FusionFacts();
            PlayerController p = Game.Player;
            v = p != null ? p.CurrentVehicle : null;
            comp = Game.Bonds != null ? Game.Bonds.Active : null;
            f.HasGarage = Game.Garage != null;
            f.HasBonds = Game.Bonds != null;
            f.PlayerAlive = p != null && !p.IsDead && !p.IsRagdolled;
            f.PlayerControllable = p != null && p.ControlsEnabled && Game.IsPlaying;
            f.InVehicle = v != null;
            f.IsDriver = p != null && v != null && p.Seat == 0;
            f.State = State;
            f.CooldownLeft = _machine.Cooldown;
            if (v != null)
            {
                f.IsBike = v.Def != null && v.Def.IsBike;
                f.IsPolice = v.IsPolice;
                f.IsPersonal = Game.Garage != null && Game.Garage.IsPersonal(v);
                f.PersonalReason = Game.Garage != null ? Game.Garage.WhyNotPersonal(v) : "";
                f.VehicleDestroyed = v.IsDestroyed;
                f.OnFire = v.OnFire;
                f.Upright = !v.IsUpsideDown && v.transform.up.y > 0.7f;
                f.VehicleHealth01 = v.Def != null && v.Def.Health > 0f ? v.Health / v.Def.Health : 0f;
                f.GroundedRays = v.GroundedWheels;
                f.VehicleMissionCritical = v.MissionCritical || (Game.Missions != null && Game.Missions.ProtectsVehicle(v));
                f.Passengers = Mathf.Max(0, v.OccupantCount() - 1);
                f.Speed = MathUtil.Flat(v.Velocity).magnitude;
                Vector3 pos = v.transform.position;
                f.Underground = WorldBounds.IsUnderground(pos);
                float headroom = 0f;
                f.VehicleSpace = Game.Underheart == null ? !f.Underground : Game.Underheart.VehicleSpaceAt(pos, out headroom);
                f.Headroom = headroom;
                f.InBay = InBay(v);
                string form = comp != null ? FusionIds.FormFor(comp.Archetype) : "";
                if (!string.IsNullOrEmpty(form) && f.IsBike)
                {
                    string cls = FusionRules.ChassisClassOf(v.Def.Name);
                    if (!string.IsNullOrEmpty(cls))
                    {
                        Vector3 min, max;
                        FusionArt.TransitionEnvelope(form, cls, FusionArt.UseLow, out min, out max);
                        f.RequiredHeadroom = FusionRules.RequiredHeadroom(max.y);
                    }
                }
            }
            MissionContext m = Game.Missions != null ? Game.Missions.Active : null;
            if (m != null)
            {
                f.StoryMission = m.Def != null && m.Def.Story;
                f.MissionForbidsVehicles = m.ForbidVehicles;
                // Campaign quests and contracts run through the quest runtime and may use fusion; other jobs (races,
                // taxi work, activities) may not.
                f.OtherJob = !f.StoryMission && (Game.Quests == null || Game.Quests.Running == null);
            }
            f.InVeil = Game.Inner != null && Game.Inner.IsInner;
            f.HasCompanion = comp != null;
            if (comp != null)
            {
                f.CompanionArchetype = comp.Archetype;
                f.CompanionName = string.IsNullOrEmpty(comp.Name) ? BondIds.DefaultName(comp.Archetype) : comp.Name;
                f.CompanionRank = Game.Bonds.RankOf(comp.Uid);
                f.CompanionDowned = comp.State == (int)CompanionState.Downed;
                string bondsReason;
                f.BondsCanFuse = Game.Bonds.CanFuse(comp.Uid, out bondsReason);
                f.BondsReason = bondsReason;
                string form = FusionIds.FormFor(comp.Archetype);
                f.FormUnlocked = Game.Garage != null && Game.Garage.FormUnlocked(form);
            }
            f.FusionUnlocked = Game.Garage != null && Game.Garage.FusionUnlocked;
            f.FirstFusionQuest = Game.Quests != null && Game.Quests.ActiveId == FirstFusionQuest;
            return f;
        }

        /// <summary>
        /// The physical checks against real colliders: open sky or enough ceiling over the chassis, and nothing solid in
        /// the box the form sweeps while it unfolds (sampled over the fold-to-parked poses; the ground and kerbs below
        /// 0.3 m are ignored because the form has no colliders).
        /// </summary>
        private bool PhysicalCheck(Vehicle v, string form, out string reason)
        {
            reason = "";
            if (v == null || v.Def == null) { reason = "No chassis."; return false; }
            string cls = FusionRules.ChassisClassOf(v.Def.Name);
            Vector3 min, max;
            FusionArt.TransitionEnvelope(form, cls, FusionArt.UseLow, out min, out max);
            float need = FusionRules.RequiredHeadroom(max.y);
            Vector3 up = v.transform.up;
            RaycastHit hit;
            if (Physics.Raycast(v.transform.position + up * 0.5f, up, out hit, need - 0.5f, Layers.WorldMask, QueryTriggerInteraction.Ignore))
            {
                reason = "Not enough headroom here for the " + FusionIds.Title(form) + " frame.";
                return false;
            }
            Vector3 lo = new Vector3(min.x - 0.05f, Mathf.Max(0.3f, min.y), min.z - 0.05f);
            Vector3 hi = max + new Vector3(0.05f, 0.05f, 0.05f);
            if (hi.y <= lo.y) hi.y = lo.y + 0.2f;
            Vector3 center = v.transform.TransformPoint((lo + hi) * 0.5f);
            Vector3 half = (hi - lo) * 0.5f;
            int n = Physics.OverlapBoxNonAlloc(center, half, _overlap, v.transform.rotation, Layers.WorldMask | BlockerMask, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                Collider c = _overlap[i];
                if (c == null || BelongsTo(c, v)) continue;
                reason = "Not enough room: the " + FusionIds.Title(form) + " frame would hit " + Describe(c) + ". Find open ground.";
                return false;
            }
            return true;
        }

        private static bool BelongsTo(Collider c, Vehicle v)
        {
            if (c.attachedRigidbody != null && c.attachedRigidbody == v.Body) return true;
            VehicleShellLink link = c.GetComponentInParent<VehicleShellLink>();
            if (link != null && link.Vehicle == v) return true;
            return c.transform.IsChildOf(v.transform);
        }

        private static string Describe(Collider c)
        {
            if (c.attachedRigidbody != null && c.attachedRigidbody.GetComponent<Vehicle>() != null) return "a vehicle";
            if (c.GetComponentInParent<VehicleShellLink>() != null) return "a vehicle";
            return c.gameObject.layer == Layers.Debris ? "debris" : "a wall or prop";
        }

        // ================================================================== the returning companion's spot

        /// <summary>A verified place for the companion: ground under it, room for its body, no hazard, same level.</summary>
        public struct SpotResult
        {
            public bool Found;
            public Vector3 Position;
            public float Yaw;
        }

        /// <summary>
        /// Searches beside, behind and ahead of the chassis (or around the rider when the chassis is gone), then a ring,
        /// for ground with room for the companion's body. Underground it falls back to the nearest rest point.
        /// </summary>
        public static SpotResult FindCompanionSpot(Transform anchor)
        {
            SpotResult r = new SpotResult();
            Transform a = anchor != null ? anchor : (Game.Player != null ? Game.Player.transform : null);
            if (a == null) return r;
            CompanionRecord comp = Game.Bonds != null ? Game.Bonds.Active : null;
            float radius = comp != null && comp.Archetype == BondIds.AshWarden ? 0.6f : 0.42f;
            float height = comp != null && comp.Archetype == BondIds.AshWarden ? 1.2f : 0.9f;
            Vector3 origin = a.position;
            Vector3 fwd = MathUtil.Flat(a.forward);
            if (fwd.sqrMagnitude < 0.01f) fwd = Vector3.forward;
            fwd.Normalize();
            Vector3 right = Vector3.Cross(Vector3.up, fwd);
            Vector3[] tries =
            {
                origin + right * 1.6f, origin - right * 1.6f, origin - fwd * 2.4f, origin + fwd * 2.4f,
                origin + (right - fwd).normalized * 2.2f, origin + (-right - fwd).normalized * 2.2f,
                origin + right * 2.6f, origin - right * 2.6f, origin + (right + fwd).normalized * 2.6f, origin + (-right + fwd).normalized * 2.6f
            };
            int mask = Layers.WorldMask | BlockerMask;
            foreach (Vector3 t in tries)
            {
                RaycastHit hit;
                if (!Physics.Raycast(t + Vector3.up * 1.2f, Vector3.down, out hit, 2.6f, Layers.WorldMask, QueryTriggerInteraction.Ignore)) continue;
                if (hit.normal.y < 0.7f) continue;
                Vector3 g = hit.point;
                if (!WorldBounds.SameLayer(g, origin)) continue;
                if (Physics.CheckCapsule(g + Vector3.up * (radius + 0.08f), g + Vector3.up * Mathf.Max(radius + 0.1f, height - radius), radius, mask, QueryTriggerInteraction.Ignore)) continue;
                float heat;
                if (Game.Underheart != null && Game.Underheart.HazardAt(g, out heat)) continue;
                // The way from the chassis to the spot must be open (no stepping off through a wall).
                if (Physics.Linecast(origin + Vector3.up * 0.8f, g + Vector3.up * 0.8f, Layers.WorldMask, QueryTriggerInteraction.Ignore)) continue;
                r.Found = true;
                r.Position = g;
                r.Yaw = MathUtil.YawOf(fwd);
                return r;
            }
            return r;
        }

        /// <summary>The fallback for a forced recovery with no verified spot: the nearest rest point underground.</summary>
        private static bool FallbackSpot(Vector3 near, out Vector3 pos, out float yaw)
        {
            pos = Vector3.zero;
            yaw = 0f;
            return WorldBounds.IsUnderground(near) && Game.Underheart != null && Game.Underheart.TryNearestRestPoint(near, out pos, out yaw);
        }

        // ================================================================== adapters for the state machine

        /// <summary>The companion side through Game.Bonds: snapshot, hide through its own lifecycle, restore exactly.</summary>
        private sealed class BondsCompanionPort : IFusionCompanionPort
        {
            private SpotResult _spot;

            public void PrepareSpot(SpotResult spot) { _spot = spot; }

            public CompanionSnapshot Snapshot(int uid)
            {
                if (Game.Bonds == null) throw new System.InvalidOperationException("bonds unavailable");
                return Game.Bonds.Snapshot(uid);
            }

            public void SetFused(int uid, bool fused)
            {
                if (Game.Bonds == null) return;
                Game.Bonds.SetFused(uid, fused);
                if (fused) return;
                // The returning body stands on the verified spot (or, with none, the nearest rest point underground;
                // otherwise the companion's own recall brings it to the rider).
                Transform body = Game.Bonds.ActiveBody;
                if (body == null) return;
                Vector3 pos; float yaw;
                if (_spot.Found)
                {
                    body.position = _spot.Position;
                    body.rotation = Quaternion.Euler(0f, _spot.Yaw, 0f);
                }
                else if (FallbackSpot(Game.PlayerPosition, out pos, out yaw))
                {
                    body.position = pos;
                    body.rotation = Quaternion.Euler(0f, yaw, 0f);
                }
                _spot = new SpotResult();
            }

            public void Restore(CompanionSnapshot snapshot)
            {
                if (Game.Bonds != null) Game.Bonds.RestoreSnapshot(snapshot);
            }
        }

        /// <summary>The form on the bike: built hidden, checked against the world, shown by progress, removed cleanly.</summary>
        private sealed class FormViewPort : IFusionFormPort
        {
            private readonly FusionSystem _owner;
            private Vehicle _vehicle;

            public FormViewPort(FusionSystem owner) { _owner = owner; }

            public void Prepare(Vehicle v) { _vehicle = v; }

            public bool Build(string form, out string error)
            {
                error = "";
                if (_vehicle == null) { error = "No chassis."; return false; }
                FusionFormView view = FusionFormView.Build(_vehicle, form, FusionArt.UseLow);
                _owner._view = view;
                string why;
                if (!_owner.PhysicalCheck(_vehicle, form, out why))
                {
                    error = why;
                    return false;
                }
                if (view.PartCount == 0)
                {
                    error = "The " + FusionIds.Title(form) + " form has no parts.";
                    return false;
                }
                return true;
            }

            public void Show(float unfold)
            {
                if (_owner._view != null) _owner._view.Show(unfold);
            }

            public void Destroy()
            {
                if (_owner._view != null) Object.Destroy(_owner._view.gameObject);
                _owner._view = null;
            }
        }
    }
}
