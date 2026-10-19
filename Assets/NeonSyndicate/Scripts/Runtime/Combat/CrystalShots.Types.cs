using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// One shot event's weapon snapshot, shared by every body it launched (one shard, a three-shard fan, a five-shard
    /// volley, a disc or an orb). It carries the one critical roll of the event, the per-actor hit counts of the volley
    /// budget and the actors that already had their element proc roll. Pooled by CrystalShots and reset on release.
    /// </summary>
    public sealed class CrystalShotSpec
    {
        public WeaponController Owner;
        public CrystalBehavior Behavior;
        /// <summary>The focus's stable inventory uid (heat and debug only; damage never depends on it).</summary>
        public int Uid;
        /// <summary>The WeaponId of the loot slot that fired (DamageInfo.WeaponId, so Resonance and kill credit work).</summary>
        public int WeaponId;
        public long ActionSerial04N;
        public int FocusUid04N;
        /// <summary>Primary damage per body (one shard, the disc outbound, the orb centre, the pulse).</summary>
        public float Damage;
        /// <summary>Secondary damage (fan shard, disc return, gravity splash, redirected pulse).</summary>
        public float Secondary;
        public Element Element;
        public float ElementChance;
        public float ElementPower;
        /// <summary>The event's single critical roll (applies to bodies, never to scenery).</summary>
        public bool Crit;
        public float CritMultiplier = 1f;
        /// <summary>Sunderheart: the concentrated variation.</summary>
        public bool Concentrated;
        /// <summary>Sunderheart: the pulse radius in metres.</summary>
        public float Radius;
        /// <summary>Hits this event has dealt to each actor (the Mirage Choir per-volley budget).</summary>
        public readonly Dictionary<int, int> HitsOnActor = new Dictionary<int, int>();
        /// <summary>Live bodies still using this spec (it returns to the pool at zero).</summary>
        public int Refs;
        /// <summary>True once the event's single element proc roll was taken.</summary>
        public bool ProcRolled;
        /// <summary>
        /// The player's skill and gear multiplier for this event (ProgressionHooks.GunMultiplier rolls the skill
        /// critical), taken once at the event's first hit and reused by its other bodies; below zero until then.
        /// </summary>
        public float SkillMul = -1f;

        /// <summary>The event's one element proc roll: true the first time only (fans, volleys and discs share it).</summary>
        public bool TakeProc()
        {
            if (ProcRolled) return false;
            ProcRolled = true;
            return true;
        }

        /// <summary>The index of this hit on the actor within the event (0 for the first), and counts it.</summary>
        public int CountHit(int actorId)
        {
            int n;
            HitsOnActor.TryGetValue(actorId, out n);
            HitsOnActor[actorId] = n + 1;
            return n;
        }

        public void Reset()
        {
            Owner = null;ActionSerial04N=0;FocusUid04N=0;
            Behavior = CrystalBehavior.None;
            Uid = 0;
            WeaponId = -1;
            Damage = 0f;
            Secondary = 0f;
            Element = Element.None;
            ElementChance = 0f;
            ElementPower = 0f;
            Crit = false;
            CritMultiplier = 1f;
            Concentrated = false;
            Radius = 0f;
            HitsOnActor.Clear();
            Refs = 0;
            ProcRolled = false;
            SkillMul = -1f;
        }
    }

    /// <summary>One hit along a swept query, already filtered, deduplicated and sorted (nearest first).</summary>
    public struct CrystalCastHit
    {
        public Collider Collider;
        /// <summary>The living damage receiver (null for a blocker).</summary>
        public IDamageable Target;
        /// <summary>The receiver's stable id (0 for a blocker); one per actor however many colliders it has.</summary>
        public int TargetId;
        public float Distance;
        public Vector3 Point;
        public Vector3 Normal;
        /// <summary>True for anything that stops the sweep without being a living receiver (walls, wrecks, props).</summary>
        public bool Blocker;
        /// <summary>True for static World-layer geometry (the only surfaces a disc banks off or a focus plants on).</summary>
        public bool StaticWorld;
    }

    /// <summary>A pooled projectile body from the art pass, with its trails and particles cached for a clean reset.</summary>
    public sealed class CrystalPooledVisual
    {
        public GameObject Go;
        public Transform T;
        public CrystalShotKind Kind;
        /// <summary>The body's own scale from the art pass (restored on every reuse).</summary>
        public Vector3 BaseScale = Vector3.one;
        public TrailRenderer[] Trails;
        public ParticleSystem[] Particles;

        /// <summary>Clears trails and particles so a reused body never draws its previous flight.</summary>
        public void ClearState()
        {
            if (Trails != null) for (int i = 0; i < Trails.Length; i++) if (Trails[i] != null) Trails[i].Clear();
            if (Particles != null) for (int i = 0; i < Particles.Length; i++) if (Particles[i] != null) Particles[i].Clear(true);
        }
    }

    /// <summary>
    /// One live crystal projectile: shot id, owner, weapon uid (through its spec), kind, position and velocity, swept
    /// radius, lifetime, travel, victim set and the reason it ended. Plain data; CrystalShots simulates it.
    /// </summary>
    public sealed class CrystalShot
    {
        public int Id;
        public bool Alive;
        public CrystalShotKind Kind;
        public CrystalShotSpec Spec;
        public Transform OwnerRoot;
        public Rigidbody OwnerVehicleBody;
        public bool OwnerIsPlayer;
        public Vector3 Pos;
        public Vector3 Vel;
        public float Speed;
        public float Radius;
        public float Gravity;
        public float Age;
        public float Lifetime;
        public float Travelled;
        public float MaxTravel;
        /// <summary>Mirage Choir: the locked actor this shard turns toward (null once lost: it then flies straight).</summary>
        public CharacterBase Lock;
        public float LockCheckAt;
        public float TurnRate;
        /// <summary>Gyre Crescent: banks, victims and the outbound budget.</summary>
        public readonly CrystalShotRules.DiscBook Disc = new CrystalShotRules.DiscBook();
        /// <summary>Actors this body already damaged (a body never strikes the same actor twice).</summary>
        public readonly HashSet<int> Victims = new HashSet<int>();
        /// <summary>Sunderheart: the contained pulse after impact.</summary>
        public bool Pulsing;
        public float PulseT;
        public Vector3 PulseCenter;
        public IDamageable Direct;
        public int DirectId;
        public Collider DirectCollider;
        public CrystalPooledVisual Visual;
        public float Spin;
        public string EndReason = "";

        public void Reset()
        {
            Id = 0;
            Alive = false;
            Kind = CrystalShotKind.Shard;
            Spec = null;
            OwnerRoot = null;
            OwnerVehicleBody = null;
            OwnerIsPlayer = false;
            Pos = Vector3.zero;
            Vel = Vector3.zero;
            Speed = 0f;
            Radius = 0f;
            Gravity = 0f;
            Age = 0f;
            Lifetime = 0f;
            Travelled = 0f;
            MaxTravel = 0f;
            Lock = null;
            LockCheckAt = 0f;
            TurnRate = 0f;
            Disc.Reset();
            Victims.Clear();
            Pulsing = false;
            PulseT = 0f;
            PulseCenter = Vector3.zero;
            Direct = null;
            DirectId = 0;
            DirectCollider = null;
            Visual = null;
            Spin = 0f;
            EndReason = "";
        }
    }
}
