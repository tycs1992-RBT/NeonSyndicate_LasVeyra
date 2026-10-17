using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>Why a fusion or separation request was refused. Values are stable: append new reasons at the end.</summary>
    public enum FusionDenial
    {
        None = 0,
        NoGarage = 1,
        NoBonds = 2,
        PlayerUnavailable = 3,
        NotInVehicle = 4,
        NotDriver = 5,
        NotABike = 6,
        PoliceBike = 7,
        NotPersonal = 8,
        VehicleWrecked = 9,
        VehicleDamaged = 10,
        NotUpright = 11,
        NotGrounded = 12,
        Busy = 13,
        AlreadyFused = 14,
        Cooldown = 15,
        StoryMission = 16,
        MissionForbids = 17,
        MissionVehicle = 18,
        OtherJob = 19,
        InVeil = 20,
        NoCompanion = 21,
        CompanionRank = 22,
        CompanionDowned = 23,
        CompanionUnavailable = 24,
        NoEntitlement = 25,
        FormLocked = 26,
        Passengers = 27,
        TooFast = 28,
        RegionTight = 29,
        LowHeadroom = 30,
        Blocked = 31,
        NotFused = 32,
        NoRoomForCompanion = 33,
        BuildFailed = 34,
        UnknownForm = 35
    }

    /// <summary>
    /// Everything the fusion validation needs to know, gathered by the runtime from the live world in one place so the
    /// decision itself is pure and testable. Physical checks (overhead clearance and the swept box) run after this
    /// passes, against the real colliders.
    /// </summary>
    public struct FusionFacts
    {
        public bool HasGarage, HasBonds;
        public bool PlayerAlive, PlayerControllable, InVehicle, IsDriver;
        public bool IsBike, IsPolice, IsPersonal;
        /// <summary>The garage's plain reason when the vehicle is not a personal chassis ("" when it is).</summary>
        public string PersonalReason;
        public bool VehicleDestroyed, OnFire, Upright;
        public float VehicleHealth01;
        public int GroundedRays;
        public FusionState State;
        public float CooldownLeft;
        public bool StoryMission, MissionForbidsVehicles, VehicleMissionCritical, OtherJob;
        public bool InVeil;
        public bool HasCompanion;
        public string CompanionArchetype, CompanionName;
        public int CompanionRank;
        public bool CompanionDowned;
        public bool BondsCanFuse;
        public string BondsReason;
        public bool FusionUnlocked, FormUnlocked;
        /// <summary>The first-fusion quest (Two Hearts, One Chassis) is running.</summary>
        public bool FirstFusionQuest;
        /// <summary>The chassis stands inside Jax's bay.</summary>
        public bool InBay;
        public int Passengers;
        public float Speed;
        public bool Underground, VehicleSpace;
        /// <summary>Clear height above the floor reported by the Underheart (0 when unknown, for example open sky).</summary>
        public float Headroom;
        /// <summary>The height the fused form needs (its transition envelope top plus a margin).</summary>
        public float RequiredHeadroom;
    }

    /// <summary>The validation outcome: allowed or the first reason in the fixed order, plus what the fusion will do.</summary>
    public struct FusionVerdict
    {
        public bool Ok;
        public FusionDenial Code;
        public string Message;
        public string Form;
        /// <summary>This is the first fusion, allowed by the quest exception inside Jax's bay (no entitlement yet).</summary>
        public bool FirstFusion;
        /// <summary>The form is not unlocked yet and this fusion in Jax's bay calibrates (unlocks) it.</summary>
        public bool CalibratesForm;

        public static FusionVerdict Deny(FusionDenial code, string message)
        {
            return new FusionVerdict { Ok = false, Code = code, Message = message ?? "", Form = "" };
        }
    }

    /// <summary>
    /// Handling the fusion system hands to a hover bike (Vehicle.FusionHandling, read in BikeStep). Every multiplier is 1
    /// and glide is off by default. Active is true only while a fused form rides the chassis (it gates the damage scale).
    /// </summary>
    public struct FusedHandling
    {
        public bool Active;
        public float Drive, TopSpeed, Grip, Brake, Steer;
        /// <summary>Gravity share while gliding (1 = no glide). Applied only in the air.</summary>
        public float GlideGravity;
        /// <summary>Fastest sink speed in m/s while gliding.</summary>
        public float GlideMaxSink;
        /// <summary>Air yaw authority multiplier while gliding.</summary>
        public float AirSteer;
        /// <summary>Extra deceleration in m/s^2 when braking in the air (Vent Brake profile).</summary>
        public float AirBrake;
        /// <summary>Incoming chassis damage multiplier (explicit, never zero).</summary>
        public float DamageScale;

        public static FusedHandling None
        {
            get
            {
                return new FusedHandling { Active = false, Drive = 1f, TopSpeed = 1f, Grip = 1f, Brake = 1f, Steer = 1f, GlideGravity = 1f, GlideMaxSink = 0f, AirSteer = 1f, AirBrake = 0f, DamageScale = 1f };
            }
        }
    }

    /// <summary>A form's signature ability: costs, limits and effect sizes (explicit numbers, tested).</summary>
    public struct FormAbilitySpec
    {
        public string Name;
        public string Hint;
        /// <summary>Heat added per use (0..1 scale; 1 overheats).</summary>
        public float HeatPerUse;
        /// <summary>Heat per second while a sustained effect runs (the Ventwing glide).</summary>
        public float HeatPerSecond;
        /// <summary>Heat lost per second on the ground and in the air when not sustaining.</summary>
        public float CoolGround, CoolAir;
        /// <summary>While overheated nothing fires until heat falls to this level.</summary>
        public float OverheatRelease;
        public float Cooldown;
        /// <summary>Stored charges (0 = no charges, limited by heat and cooldown instead).</summary>
        public int MaxCharges;
        public float RechargeSeconds;
        /// <summary>Velocity change in m/s along the chassis (forward) and up.</summary>
        public float ForwardDeltaV, UpDeltaV;
        /// <summary>The impulse never pushes flat speed above top speed times this.</summary>
        public float SpeedCapFactor;
        /// <summary>Damage dealt to each hostile caught, the reach and the stagger (bosses get the shorter one).</summary>
        public float Damage, Radius, Stagger, BossStagger;
        /// <summary>Seconds a timed effect lasts (the Kilnback brace).</summary>
        public float Duration;
        /// <summary>The ability needs at least this much clear height above the chassis (vent burst hop).</summary>
        public float NeedsHeadroom;
    }

    /// <summary>One form's live meters. They change with time and use only; fusing and separating never touch them.</summary>
    public struct FormMeters
    {
        public float Heat;
        public float Cooldown;
        public int Charges;
        public float Recharge;
        public bool Overheated;
        /// <summary>Seconds left on a timed effect (brace).</summary>
        public float Active;

        public static FormMeters Fresh(FormAbilitySpec spec)
        {
            return new FormMeters { Heat = 0f, Cooldown = 0f, Charges = Mathf.Max(0, spec.MaxCharges), Recharge = 0f, Overheated = false, Active = 0f };
        }
    }

    /// <summary>
    /// Pure rules of creature-vehicle fusion (Underheart and Bonds 04E): the single state machine's legal transitions,
    /// the validation order, handling profiles, explicit damage multipliers, ability specs and meters, and the
    /// compatibility matrix. Engine-free (Mathf and Vector3 only), so EditMode tests run offline.
    /// </summary>
    public static class FusionRules
    {
        /// <summary>Hold N (or LB + D-pad Down) this long to fuse or separate.</summary>
        public const float HoldSeconds = 0.6f;
        /// <summary>Fastest flat speed for an ordinary fusion, and for the parked first fusion or a calibration in Jax's bay.</summary>
        public const float MaxFuseSpeed = 3f, MaxBayFuseSpeed = 0.8f;
        /// <summary>Fastest flat speed for a voluntary separation.</summary>
        public const float MaxSeparateSpeed = 6f;
        /// <summary>The chassis must have at least this share of its health to take the coupler load.</summary>
        public const float MinHealth01 = 0.15f;
        /// <summary>Transition lengths in seconds (the form unfolds or folds while the chassis is held).</summary>
        public const float TransitionInSeconds = 0.85f, TransitionOutSeconds = 0.6f;
        /// <summary>Cooldowns before the next fusion: after a voluntary separation and after a forced recovery.</summary>
        public const float CooldownAfterSeparate = 6f, CooldownAfterRecovery = 15f;
        /// <summary>Overhead margin added to the form's envelope top.</summary>
        public const float HeadroomMargin = 0.45f;
        /// <summary>The Ventwing handling profiles (QuestIds04E choice "fusion_profile").</summary>
        public const string ProfileUpdraft = "updraft", ProfileVentBrake = "ventbrake";
        /// <summary>The handling sidegrade tune a Jax side quest unlocks.</summary>
        public const string TuneGrip = "tune_grip";

        public static readonly string[] Profiles = { ProfileUpdraft, ProfileVentBrake };

        public static bool IsProfile(string id) { return id == ProfileUpdraft || id == ProfileVentBrake; }

        public static string ProfileTitle(string id)
        {
            switch (id)
            {
                case ProfileUpdraft: return "Updraft";
                case ProfileVentBrake: return "Vent Brake";
                default: return "Standard";
            }
        }

        public static string ProfileBlurb(string id)
        {
            switch (id)
            {
                case ProfileUpdraft: return "Longer, floatier glides. Ordinary brakes.";
                case ProfileVentBrake: return "Shorter glides. The vents brake hard on the ground and in the air.";
                default: return "";
            }
        }

        // ================================================================== state machine

        /// <summary>True when the single authoritative state may move from one state to another.</summary>
        public static bool IsLegal(FusionState from, FusionState to)
        {
            if (from == to) return false;
            switch (from)
            {
                case FusionState.Separate: return to == FusionState.FusionRequested;
                case FusionState.FusionRequested: return to == FusionState.Validating || to == FusionState.Separate;
                case FusionState.Validating: return to == FusionState.TransitionIn || to == FusionState.Separate;
                case FusionState.TransitionIn: return to == FusionState.Fused || to == FusionState.Recovery;
                case FusionState.Fused: return to == FusionState.SeparationRequested || to == FusionState.Recovery;
                case FusionState.SeparationRequested: return to == FusionState.TransitionOut || to == FusionState.Fused || to == FusionState.Recovery;
                case FusionState.TransitionOut: return to == FusionState.Separate || to == FusionState.Recovery;
                case FusionState.Recovery: return to == FusionState.Separate;
                default: return false;
            }
        }

        /// <summary>True for every state in which a fused representation (or its build) exists.</summary>
        public static bool Engaged(FusionState s)
        {
            return s == FusionState.TransitionIn || s == FusionState.Fused || s == FusionState.SeparationRequested || s == FusionState.TransitionOut;
        }

        // ================================================================== validation

        /// <summary>The headroom a fused form needs, from its transition envelope top.</summary>
        public static float RequiredHeadroom(float envelopeTop)
        {
            return Mathf.Max(2.2f, envelopeTop + HeadroomMargin);
        }

        /// <summary>
        /// Validates a fusion request in the fixed order of the brief (driver seat, personal chassis, entitlement or the
        /// first-fusion exception at Jax's bay, an Attuned companion that is not downed, the form, the region, speed,
        /// passengers, mission restrictions, cooldown). Returns the first refusal with a plain reason, or Ok.
        /// Nothing changes on a refusal.
        /// </summary>
        public static FusionVerdict Validate(FusionFacts f)
        {
            if (!f.HasGarage) return FusionVerdict.Deny(FusionDenial.NoGarage, "The personal garage did not load, so no chassis can couple.");
            if (!f.HasBonds) return FusionVerdict.Deny(FusionDenial.NoBonds, "Bonded companions did not load in this build.");
            if (!f.PlayerAlive || !f.PlayerControllable) return FusionVerdict.Deny(FusionDenial.PlayerUnavailable, "You can't do that right now.");
            if (!f.InVehicle) return FusionVerdict.Deny(FusionDenial.NotInVehicle, "Get on your own bike first.");
            if (!f.IsDriver) return FusionVerdict.Deny(FusionDenial.NotDriver, "Only the rider in the front seat can fuse.");
            if (!f.IsBike) return FusionVerdict.Deny(FusionDenial.NotABike, "Fusion needs a hover bike chassis. Cars are not supported.");
            if (f.IsPolice) return FusionVerdict.Deny(FusionDenial.PoliceBike, "A police bike can never be a personal chassis.");
            if (f.VehicleMissionCritical) return FusionVerdict.Deny(FusionDenial.MissionVehicle, "This bike belongs to the job. It can't take a coupler.");
            if (!f.IsPersonal) return FusionVerdict.Deny(FusionDenial.NotPersonal, string.IsNullOrEmpty(f.PersonalReason) ? "This bike isn't yours. Phone > Bring my bike delivers your own chassis." : f.PersonalReason);
            if (f.VehicleDestroyed) return FusionVerdict.Deny(FusionDenial.VehicleWrecked, "The chassis is wrecked.");
            if (f.OnFire || f.VehicleHealth01 < MinHealth01) return FusionVerdict.Deny(FusionDenial.VehicleDamaged, "The chassis is too damaged to carry the coupler. Repair it at Jax's garage first.");
            if (!f.Upright) return FusionVerdict.Deny(FusionDenial.NotUpright, "Get the bike upright first.");
            if (f.GroundedRays < 2) return FusionVerdict.Deny(FusionDenial.NotGrounded, "Settle the bike on solid ground first.");
            if (f.State == FusionState.Fused) return FusionVerdict.Deny(FusionDenial.AlreadyFused, "You are already fused.");
            if (f.State != FusionState.Separate) return FusionVerdict.Deny(FusionDenial.Busy, "The coupler is still settling.");
            if (f.CooldownLeft > 0.01f) return FusionVerdict.Deny(FusionDenial.Cooldown, "The coupler is cooling down: " + Mathf.CeilToInt(f.CooldownLeft) + " s.");
            if (f.StoryMission) return FusionVerdict.Deny(FusionDenial.StoryMission, "Not during a story mission.");
            if (f.MissionForbidsVehicles || f.OtherJob) return FusionVerdict.Deny(f.MissionForbidsVehicles ? FusionDenial.MissionForbids : FusionDenial.OtherJob, "Not during this job.");
            if (f.InVeil) return FusionVerdict.Deny(FusionDenial.InVeil, "The Veil scrambles the bond. Step back into the city first.");
            string name = string.IsNullOrEmpty(f.CompanionName) ? "Your companion" : f.CompanionName;
            if (!f.HasCompanion) return FusionVerdict.Deny(FusionDenial.NoCompanion, "No bonded companion is with you.");
            string form = FusionIds.FormFor(f.CompanionArchetype);
            if (string.IsNullOrEmpty(form)) return FusionVerdict.Deny(FusionDenial.UnknownForm, name + " has no fusion profile.");
            if (f.CompanionRank < BondIds.FusionRank)
                return FusionVerdict.Deny(FusionDenial.CompanionRank, name + " needs the Attuned bond rank to fuse (now " + RankName(f.CompanionRank) + ").");
            if (f.CompanionDowned) return FusionVerdict.Deny(FusionDenial.CompanionDowned, name + " is resting and can't fuse yet.");
            if (!f.BondsCanFuse) return FusionVerdict.Deny(FusionDenial.CompanionUnavailable, string.IsNullOrEmpty(f.BondsReason) ? name + " is not ready to fuse." : f.BondsReason);
            bool first = false;
            if (!f.FusionUnlocked)
            {
                if (!(f.FirstFusionQuest && f.InBay && form == FusionIds.Ventwing))
                    return FusionVerdict.Deny(FusionDenial.NoEntitlement, f.FirstFusionQuest ? "Park inside Jax's bay for the first fusion." : "Fusion is not unlocked yet. Jax Calder makes the chassis safe first.");
                first = true;
            }
            bool calibrate = false;
            if (!f.FormUnlocked && !first)
            {
                if (!f.InBay) return FusionVerdict.Deny(FusionDenial.FormLocked, FusionIds.Title(form) + " is not calibrated. Bring " + name + " to Jax's bay to tune the coupler.");
                calibrate = true;
            }
            if (f.Passengers > 0) return FusionVerdict.Deny(FusionDenial.Passengers, "Your passenger has to get off first: the fused frame has one seat.");
            float limit = first || calibrate ? MaxBayFuseSpeed : MaxFuseSpeed;
            if (f.Speed > limit) return FusionVerdict.Deny(FusionDenial.TooFast, first || calibrate ? "Stop the bike in the bay first." : "Slow down to fuse.");
            if (f.Underground && !f.VehicleSpace) return FusionVerdict.Deny(FusionDenial.RegionTight, "Too tight down here. Fuse in a broad transit tunnel.");
            if (f.Headroom > 0.01f && f.Headroom < f.RequiredHeadroom) return FusionVerdict.Deny(FusionDenial.LowHeadroom, "Not enough headroom for the fused frame here.");
            return new FusionVerdict { Ok = true, Code = FusionDenial.None, Message = "", Form = form, FirstFusion = first, CalibratesForm = calibrate };
        }

        /// <summary>Validates a voluntary separation (the companion spot is searched by the runtime first).</summary>
        public static FusionVerdict ValidateSeparate(FusionState state, float speed, int groundedRays, bool companionSpotFound)
        {
            if (state != FusionState.Fused) return FusionVerdict.Deny(state == FusionState.Separate ? FusionDenial.NotFused : FusionDenial.Busy, state == FusionState.Separate ? "You are not fused." : "The coupler is still settling.");
            if (speed > MaxSeparateSpeed) return FusionVerdict.Deny(FusionDenial.TooFast, "Slow down to separate safely.");
            if (groundedRays < 2) return FusionVerdict.Deny(FusionDenial.NotGrounded, "Land first, then separate.");
            if (!companionSpotFound) return FusionVerdict.Deny(FusionDenial.NoRoomForCompanion, "No safe ground beside the bike for your companion here. Find open ground.");
            return new FusionVerdict { Ok = true, Code = FusionDenial.None, Message = "", Form = "" };
        }

        public static string RankName(int rank)
        {
            return rank >= 0 && rank < BondIds.RankNames.Length ? BondIds.RankNames[rank] : "unknown";
        }

        // ================================================================== handling and damage

        /// <summary>The handling a form gives (profile only matters for the Ventwing; the tune is the Jax sidegrade).</summary>
        public static FusedHandling Handling(string form, string profile, string tune)
        {
            FusedHandling h = TuneHandling(tune);
            h.Active = true;
            switch (form)
            {
                case FusionIds.Ventwing:
                    if (profile == ProfileVentBrake)
                    {
                        h.Brake *= 1.25f;
                        h.GlideGravity = 0.45f;
                        h.GlideMaxSink = 4.5f;
                        h.AirSteer = 1.4f;
                        h.AirBrake = 6f;
                    }
                    else
                    {
                        h.GlideGravity = 0.32f;
                        h.GlideMaxSink = 3.2f;
                        h.AirSteer = 1.6f;
                        h.AirBrake = 0f;
                    }
                    h.DamageScale = 1f;
                    break;
                case FusionIds.Kilnback:
                    h.Drive *= 0.85f;
                    h.TopSpeed *= 0.88f;
                    h.Grip *= 1.08f;
                    h.Brake *= 1.05f;
                    h.Steer *= 0.85f;
                    h.DamageScale = ChassisDamageScale(form, false);
                    break;
                case FusionIds.Coilrunner:
                    h.Grip *= 1.05f;
                    h.Brake *= 1.35f;
                    h.Steer *= 0.92f;
                    h.DamageScale = ChassisDamageScale(form, false);
                    break;
                default:
                    return TuneHandling(tune);
            }
            // Glide is engaged per frame by the runtime; the stored values describe it, GlideGravity 1 keeps it off.
            return h;
        }

        /// <summary>The handling of a separate personal chassis with a tune (the sidegrade trades top speed for grip).</summary>
        public static FusedHandling TuneHandling(string tune)
        {
            FusedHandling h = FusedHandling.None;
            if (tune == TuneGrip)
            {
                h.Grip = 1.08f;
                h.TopSpeed = 0.97f;
            }
            return h;
        }

        /// <summary>The glide-ready copy of a handling value with the glide switched on or off for this frame.</summary>
        public static FusedHandling WithGlide(FusedHandling h, string form, string profile, bool gliding)
        {
            if (!gliding || form != FusionIds.Ventwing)
            {
                h.GlideGravity = 1f;
                return h;
            }
            FusedHandling g = Handling(form, profile, "");
            h.GlideGravity = g.GlideGravity;
            h.GlideMaxSink = g.GlideMaxSink;
            h.AirSteer = g.AirSteer;
            return h;
        }

        /// <summary>Incoming chassis damage multiplier while fused (explicit; the Kilnback brace hardens it further).</summary>
        public static float ChassisDamageScale(string form, bool bracing)
        {
            switch (form)
            {
                case FusionIds.Ventwing: return 1f;
                case FusionIds.Kilnback: return bracing ? 0.4f : 0.7f;
                case FusionIds.Coilrunner: return 1.1f;
                default: return 1f;
            }
        }

        /// <summary>Incoming damage multiplier for the fused rider (bounded: never invulnerable). frontal is a hit within
        /// 70 degrees of the chassis heading.</summary>
        public static float RiderDamageScale(string form, bool bracing, bool frontal)
        {
            switch (form)
            {
                case FusionIds.Kilnback: return bracing && frontal ? 0.6f : 0.85f;
                default: return 1f;
            }
        }

        /// <summary>The ability of a form (the Ventwing profile changes its glide, not its burst).</summary>
        public static FormAbilitySpec Ability(string form)
        {
            switch (form)
            {
                case FusionIds.Ventwing:
                    return new FormAbilitySpec
                    {
                        Name = "Vent burst", Hint = "U: vent burst. Hold U in the air to glide.",
                        HeatPerUse = 0.34f, HeatPerSecond = 0.16f, CoolGround = 0.22f, CoolAir = 0.06f, OverheatRelease = 0.35f,
                        Cooldown = 2.2f, MaxCharges = 0, RechargeSeconds = 0f,
                        ForwardDeltaV = 3f, UpDeltaV = 5.2f, SpeedCapFactor = 1f,
                        Damage = 18f, Radius = 3.2f, Stagger = 0f, BossStagger = 0f, Duration = 0f, NeedsHeadroom = 4f
                    };
                case FusionIds.Kilnback:
                    return new FormAbilitySpec
                    {
                        Name = "Brace ram", Hint = "U: brace ram (hardens the shell and shoves what is ahead).",
                        HeatPerUse = 0.3f, HeatPerSecond = 0f, CoolGround = 0.15f, CoolAir = 0.15f, OverheatRelease = 0.4f,
                        Cooldown = 6f, MaxCharges = 0, RechargeSeconds = 0f,
                        ForwardDeltaV = 3.5f, UpDeltaV = 0f, SpeedCapFactor = 1f,
                        Damage = 55f, Radius = 2.8f, Stagger = 0.7f, BossStagger = 0.25f, Duration = 1.2f, NeedsHeadroom = 0f
                    };
                case FusionIds.Coilrunner:
                    return new FormAbilitySpec
                    {
                        Name = "Arc dash", Hint = "U: arc dash (a short burst, then a stunning discharge).",
                        HeatPerUse = 0f, HeatPerSecond = 0f, CoolGround = 0.2f, CoolAir = 0.2f, OverheatRelease = 0.4f,
                        Cooldown = 0.8f, MaxCharges = 2, RechargeSeconds = 5f,
                        ForwardDeltaV = 6.5f, UpDeltaV = 0f, SpeedCapFactor = 1.12f,
                        Damage = 32f, Radius = 7f, Stagger = 0.5f, BossStagger = 0.2f, Duration = 0f, NeedsHeadroom = 0f
                    };
                default:
                    return new FormAbilitySpec { Name = "", Hint = "", SpeedCapFactor = 1f, OverheatRelease = 0.4f };
            }
        }

        // ================================================================== meters

        /// <summary>
        /// Advances a form's meters by time (always, fused or not): cooldown and timed effects count down, heat cools
        /// (or builds while sustaining), charges come back one at a time. The only ways a meter improves.
        /// </summary>
        public static FormMeters Step(FormMeters m, FormAbilitySpec spec, float dt, bool airborne, bool sustaining)
        {
            if (dt <= 0f) return m;
            m.Cooldown = Mathf.Max(0f, m.Cooldown - dt);
            m.Active = Mathf.Max(0f, m.Active - dt);
            if (sustaining && !m.Overheated) m.Heat = Mathf.Min(1f, m.Heat + spec.HeatPerSecond * dt);
            else m.Heat = Mathf.Max(0f, m.Heat - (airborne ? spec.CoolAir : spec.CoolGround) * dt);
            if (m.Heat >= 1f) m.Overheated = true;
            else if (m.Overheated && m.Heat <= spec.OverheatRelease) m.Overheated = false;
            if (spec.MaxCharges > 0)
            {
                if (m.Charges < spec.MaxCharges)
                {
                    m.Recharge += dt;
                    while (m.Recharge >= spec.RechargeSeconds && m.Charges < spec.MaxCharges && spec.RechargeSeconds > 0f)
                    {
                        m.Recharge -= spec.RechargeSeconds;
                        m.Charges++;
                    }
                    if (m.Charges >= spec.MaxCharges) m.Recharge = 0f;
                }
                else m.Recharge = 0f;
            }
            return m;
        }

        /// <summary>Why the ability can't fire now ("" when it can).</summary>
        public static string WhyNotUse(FormMeters m, FormAbilitySpec spec)
        {
            if (string.IsNullOrEmpty(spec.Name)) return "This form has no ability.";
            if (m.Overheated) return "Vents overheated. Let them cool.";
            if (m.Cooldown > 0.01f) return spec.Name + " recharging: " + m.Cooldown.ToString("0.0") + " s.";
            if (spec.MaxCharges > 0 && m.Charges <= 0) return "No charges left. One returns every " + spec.RechargeSeconds.ToString("0") + " s.";
            if (spec.HeatPerUse > 0f && m.Heat + spec.HeatPerUse > 1.0001f) return "Too hot for another " + spec.Name.ToLowerInvariant() + ".";
            return "";
        }

        /// <summary>Spends one use: heat, cooldown, a charge and the timed effect. Returns false (unchanged) when it can't fire.</summary>
        public static bool TryUse(ref FormMeters m, FormAbilitySpec spec)
        {
            if (WhyNotUse(m, spec).Length > 0) return false;
            m.Heat = Mathf.Min(1f, m.Heat + spec.HeatPerUse);
            if (m.Heat >= 1f) m.Overheated = true;
            m.Cooldown = spec.Cooldown;
            if (spec.MaxCharges > 0) m.Charges = Mathf.Max(0, m.Charges - 1);
            m.Active = spec.Duration;
            return true;
        }

        /// <summary>
        /// The flat velocity after an ability impulse: the boost is added along the heading, but it never lifts flat speed
        /// above top speed times the cap (and never slows a chassis already faster than that).
        /// </summary>
        public static Vector3 CapImpulse(Vector3 velocity, Vector3 forward, float deltaV, float topSpeed, float capFactor)
        {
            Vector3 flatFwd = new Vector3(forward.x, 0f, forward.z);
            if (flatFwd.sqrMagnitude < 1e-6f) flatFwd = new Vector3(0f, 0f, 1f);
            flatFwd.Normalize();
            Vector3 flat = new Vector3(velocity.x, 0f, velocity.z);
            float before = flat.magnitude;
            Vector3 after = flat + flatFwd * deltaV;
            float cap = Mathf.Max(before, Mathf.Max(0f, topSpeed) * Mathf.Max(0f, capFactor));
            if (after.magnitude > cap && after.sqrMagnitude > 1e-8f) after = after.normalized * cap;
            return new Vector3(after.x, velocity.y, after.z);
        }

        // ================================================================== compatibility

        /// <summary>The FusionIds chassis class of a bike model name ("" when the model can't be a personal chassis).</summary>
        public static string ChassisClassOf(string vehicleName)
        {
            if (vehicleName == VehicleDb.SportBikeName) return FusionIds.Sport;
            if (vehicleName == VehicleDb.CruiserBikeName) return FusionIds.Cruiser;
            if (vehicleName == VehicleDb.CourierBikeName) return FusionIds.Skiff;
            return "";
        }

        /// <summary>The bike model for a chassis class.</summary>
        public static string VehicleNameOf(string chassisClass)
        {
            switch (chassisClass)
            {
                case FusionIds.Cruiser: return VehicleDb.CruiserBikeName;
                case FusionIds.Skiff: return VehicleDb.CourierBikeName;
                default: return VehicleDb.SportBikeName;
            }
        }

        /// <summary>
        /// The implemented compatibility matrix: every companion archetype with a form couples with each of the three
        /// personal bike classes. Cars, the police bike and unknown models are not supported, with the reason.
        /// </summary>
        public static bool Compatible(string archetype, string vehicleName, bool isBike, bool isPolice, out string reason)
        {
            string form = FusionIds.FormFor(archetype);
            if (string.IsNullOrEmpty(form)) { reason = "This companion has no fusion profile."; return false; }
            if (!isBike) { reason = "Cars are not supported: fusion couples with a hover bike chassis."; return false; }
            if (isPolice) { reason = "The police bike is never a personal chassis."; return false; }
            if (string.IsNullOrEmpty(ChassisClassOf(vehicleName))) { reason = "This bike model has no coupler mount."; return false; }
            reason = "";
            return true;
        }

        /// <summary>A short line naming what a form does, for menus and the HUD.</summary>
        public static string FormBlurb(string form)
        {
            switch (form)
            {
                case FusionIds.Ventwing: return "Crystal ribs and articulated wings: a vent burst hop and a controlled glide that heats the vents.";
                case FusionIds.Kilnback: return "Layered mineral shell and a forward brace: slower and heavier, armoured, with a brace ram.";
                case FusionIds.Coilrunner: return "Fins, spines and a coil path: strong brakes and an arc dash that ends in a stunning discharge.";
                default: return "";
            }
        }
    }
}
