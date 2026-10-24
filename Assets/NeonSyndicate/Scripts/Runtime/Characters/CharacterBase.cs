using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Shared body for the player and every NPC: procedural rig, animator, health, ragdoll, weapons, team, vehicle seat,
    /// knockdowns (vehicle hits, explosions, melee) and getting back up.
    /// </summary>
    public abstract class CharacterBase : MonoBehaviour
    {
        public HumanoidRig Rig;
        public HumanoidAnimator Anim;
        public CharacterHealth Health;
        public Ragdoll Ragdoll;
        /// <summary>Never knocked down, thrown or hurt (children of the city, NpcController.Family.cs); damage is refused by CharacterHealth.Invulnerable.</summary>
        public bool NeverHarmed;
        public WeaponController Weapons;
        public TeamTag Team;
        public Vehicle CurrentVehicle;
        public int Seat = -1;
        public string DisplayName = "Pedestrian";
        /// <summary>Bodies without a humanoid rig (creatures): the chest point enemies and the lock-on aim at.</summary>
        public Transform AimAnchor;
        /// <summary>Bodies without a humanoid rig (creatures): the head point (health bars, damage numbers).</summary>
        public Transform HeadAnchor;
        /// <summary>Horizontal radius of the body beyond a person's (0 for people): melee reach and lock-on measure to the edge.</summary>
        public float BodyRadius;
        /// <summary>Height of a big body (0 for people): lets melee reach a tall creature's flanks from below.</summary>
        public float BodyHeight;

        protected float RagdollTimer;
        protected float RagdollMinTime;
        private float _lastVehicleHit = -10f;

        public bool InVehicle { get { return CurrentVehicle != null; } }
        public bool IsRagdolled { get { return Ragdoll != null && Ragdoll.Active; } }
        public bool IsDead { get { return Health != null && !Health.IsAlive; } }
        public bool IsPlayer { get { return this is PlayerController; } }

        /// <summary>Current movement velocity (for animation, ragdoll inheritance and AI prediction).</summary>
        public abstract Vector3 Velocity { get; }

        /// <summary>World position of the chest (aim target for enemies).</summary>
        public Vector3 ChestPosition
        {
            get
            {
                if (IsRagdolled && Ragdoll.HipsBody != null) return Ragdoll.HipsBody.position + Vector3.up * 0.2f;
                if (Rig != null && Rig.Chest != null) return Rig.Chest.position + Vector3.up * 0.1f;
                if (AimAnchor != null) return AimAnchor.position;
                return transform.position + Vector3.up * 1.3f;
            }
        }

        public Vector3 HeadPosition
        {
            get
            {
                if (Rig != null && Rig.Head != null) return Rig.Head.position + Vector3.up * 0.1f;
                if (HeadAnchor != null) return HeadAnchor.position;
                return transform.position + Vector3.up * 1.7f;
            }
        }

        /// <summary>Builds the body and the shared components. Call once right after AddComponent.</summary>
        protected void BuildBody(Appearance look, Team team, int gangId)
        {
            BuildBody(look, team, gangId, BodyFactory.AmbientDetail);
        }

        /// <summary>detail: 0 crowd, 1 standard, 2 hero (player, companions, bosses).</summary>
        protected void BuildBody(Appearance look, Team team, int gangId, int detail)
        {
            gameObject.layer = Layers.Characters;
            Rig = HumanoidRig.Build(gameObject, look, detail);
            Anim = gameObject.AddComponent<HumanoidAnimator>();
            Anim.Rig = Rig;
            Health = gameObject.AddComponent<CharacterHealth>();
            Health.HeadHeight = 1.5f * Rig.Scale;
            Ragdoll = gameObject.AddComponent<Ragdoll>();
            Ragdoll.Init(Rig);
            Weapons = gameObject.AddComponent<WeaponController>();
            Weapons.Rig = Rig;
            Weapons.Anim = Anim;
            Team = gameObject.AddComponent<TeamTag>();
            Team.Team = team;
            Team.GangId = gangId;
            Health.Damaged += OnDamaged;
            Health.Damaged += FlashOnHit;
            Health.Died += OnDied;
            ImportedVisual04OD.Attach(this);
            // Wave 4 owner decision: owner-approved imported characters draw live over the procedural body.
            LiveCastVisual.Attach(this);
        }

        protected virtual void OnDamaged(DamageInfo info) { }

        /// <summary>A white-hot flash across the body on every hit, stronger for bigger hits.</summary>
        private void FlashOnHit(DamageInfo info)
        {
            if (Rig == null || info.Amount <= 0f) return;
            Rig.HitFlash(Mathf.Clamp(info.Amount / Mathf.Max(1f, Health.MaxHealth) * 4f, 0.35f, 1f));
        }

        protected virtual void OnDied(DamageInfo info)
        {
            if (InVehicle) ExitVehicleImmediate(true);
            if (!IsRagdolled)
            {
                Vector3 impulse = info.Direction * Mathf.Clamp(info.Force, 0.5f, 18f) * 0.6f;
                if (info.Type == DamageType.Explosion) impulse += Vector3.up * info.Force * 0.4f;
                Knockdown(impulse, info.Point, 999f);
            }
            if (Weapons != null) Weapons.SetHidden(true);
            if (Game.Audio != null) Game.Audio.PlayVar(SfxId.BodyFall, transform.position, 0.6f, 0.15f, 30f);
        }

        /// <summary>Throws the character to the ground as a ragdoll. They get up after minSeconds if still alive.</summary>
        public void Knockdown(Vector3 velocityChange, Vector3 point, float minSeconds)
        {
            KnockdownWithVelocity(Velocity, velocityChange, point, minSeconds);
        }

        public virtual void KnockdownWithVelocity(Vector3 inherited, Vector3 velocityChange, Vector3 point, float minSeconds)
        {
            if (Ragdoll == null || Rig == null || NeverHarmed) return;
            if (InVehicle) return;
            if (IsRagdolled)
            {
                if (Ragdoll.HipsBody != null && !Ragdoll.HipsBody.isKinematic) Ragdoll.HipsBody.AddForce(velocityChange * 0.5f, ForceMode.VelocityChange);
                RagdollMinTime = Mathf.Max(RagdollMinTime, minSeconds);
                return;
            }
            OnBeforeRagdoll();
            Anim.Frozen = true;
            Anim.MeleeT = -1f;
            Anim.ThrowT = -1f;
            Ragdoll.SetInheritedVelocity(inherited);
            Ragdoll.Activate(velocityChange, point);
            RagdollTimer = 0f;
            RagdollMinTime = minSeconds;
            if (Weapons != null)
            {
                Weapons.CancelActions();
                Weapons.SetHidden(true);
            }
        }

        /// <summary>Disable movement colliders/controllers before physics takes over.</summary>
        protected abstract void OnBeforeRagdoll();

        /// <summary>Re-enable movement after getting up.</summary>
        protected abstract void OnAfterRagdoll();

        protected void UpdateRagdollState(float dt)
        {
            if (!IsRagdolled) return;
            RagdollTimer += dt;
            if (IsDead)
            {
                if (Ragdoll.IsSettled && RagdollTimer > 3f) Ragdoll.Freeze();
                return;
            }
            bool settled = Ragdoll.IsSettled || RagdollTimer > RagdollMinTime + 6f;
            if (RagdollTimer >= RagdollMinTime && settled) GetUp();
        }

        public void GetUp()
        {
            if (!IsRagdolled || IsDead) return;
            Ragdoll.Deactivate();
            Anim.Frozen = false;
            if (Weapons != null) Weapons.SetHidden(InVehicle);
            OnAfterRagdoll();
        }

        /// <summary>Called by vehicles that drive into this character.</summary>
        public virtual void OnVehicleHit(Vehicle vehicle, Vector3 vehicleVelocity, float relativeSpeed)
        {
            if (InVehicle || IsDead && IsRagdolled) return;
            if (Time.time - _lastVehicleHit < 0.6f) return;
            _lastVehicleHit = Time.time;
            Vector3 away = MathUtil.Flat(transform.position - vehicle.transform.position).normalized;
            Vector3 fling = vehicleVelocity * 0.85f + away * relativeSpeed * 0.25f + Vector3.up * Mathf.Min(7f, relativeSpeed * 0.32f);
            float damage = Mathf.Max(0f, relativeSpeed - 3.5f) * 6.5f;
            GameObject source = vehicle.Driver != null ? vehicle.Driver.gameObject : vehicle.gameObject;
            if (damage > 0f)
            {
                DamageInfo info = DamageInfo.Make(damage, ChestPosition, vehicleVelocity, source, DamageType.Vehicle);
                info.Force = relativeSpeed;
                Health.TakeDamage(info);
            }
            Knockdown(fling, ChestPosition, 1.6f + relativeSpeed * 0.08f);
            if (Game.Audio != null) Game.Audio.PlayVar(relativeSpeed > 12f ? SfxId.CrashHeavy : SfxId.BodyFall, transform.position, 0.7f, 0.1f, 40f);
            if (Game.Fx != null && relativeSpeed > 8f)
            {
                if (Health == null || Health.Surface == ImpactSurface.Flesh) Game.Fx.Blood(ChestPosition, vehicleVelocity.normalized, 8);
                else Game.Fx.CreatureHit(ChestPosition, -vehicleVelocity.normalized, Health.Surface, Health.SurfaceTint, 10);
            }
        }

        // ------------------------------------------------------------------ vehicle seats

        /// <summary>Puts the character into a seat (no animation). Seat 0 is the driver.</summary>
        public virtual void EnterVehicleImmediate(Vehicle v, int seat)
        {
            if (v == null) return;
            if (IsRagdolled) GetUp();
            CurrentVehicle = v;
            Seat = seat;
            v.SetOccupant(seat, this);
            Transform s = v.SeatTransform(seat);
            transform.SetParent(s, false);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            Anim.SitWeight = 1f;
            Anim.Velocity = Vector3.zero;
            Anim.Aiming = false;
            if (Weapons != null)
            {
                Weapons.InVehicle = true;
                Weapons.IgnoreVehicle = v;
                Weapons.CancelActions();
                Weapons.SetHidden(true);
            }
            OnEnteredVehicle();
        }

        /// <summary>Removes the character from its seat and places it beside the door (or ragdolls it out when bailing).</summary>
        public virtual void ExitVehicleImmediate(bool tumble)
        {
            Vehicle v = CurrentVehicle;
            if (v == null) return;
            int seat = Seat;
            v.SetOccupant(seat, null);
            CurrentVehicle = null;
            Seat = -1;
            transform.SetParent(null, true);
            Vector3 exitPos = v.FindExitPoint(seat);
            transform.position = exitPos;
            Vector3 fwd = MathUtil.Flat(v.transform.forward);
            transform.rotation = Quaternion.LookRotation(fwd.sqrMagnitude > 0.01f ? fwd.normalized : Vector3.forward, Vector3.up);
            Anim.SitWeight = 0f;
            Anim.Steer = 0f;
            if (Weapons != null)
            {
                Weapons.InVehicle = false;
                Weapons.IgnoreVehicle = null;
                Weapons.SetHidden(false);
            }
            OnExitedVehicle(v);
            if (tumble)
            {
                Vector3 side = v.transform.right * (v.SeatSide(seat) < 0 ? -1f : 1f);
                Vector3 inherited = v.Body != null ? v.Body.linearVelocity : Vector3.zero;
                KnockdownWithVelocity(inherited, side * 3f + Vector3.up * 1.5f, ChestPosition, 1.4f);
            }
        }

        protected virtual void OnEnteredVehicle() { }
        protected virtual void OnExitedVehicle(Vehicle v) { }
    }
}
