using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// The fight's owned hazards: floor rings (GroundRing fill then burst), floor lanes (a bar that grows, then
    /// bursts), burning ground (a disc that ticks while it lasts) and beams (a thin aim line that locks, then a bright
    /// beam that ticks; clipped at the first wall when made, so nothing passes through rock). Every hazard is drawn
    /// before it can hurt (at least UBossTiming.MinTell), hurts only the player standing in it at the moment it
    /// bursts (rings and lanes reach 3 m above their floor, beams reach a flyer in their path), respects the ring
    /// respite so a volley never chains a stagger lock, and goes away at once on every ending.
    /// </summary>
    public abstract partial class UnderBossFight
    {
        protected enum HazardKind
        {
            Ring,
            Lane,
            Pool,
            Beam
        }

        /// <summary>Visual flavour of a burst (effects and sounds).</summary>
        protected enum BurstFx
        {
            Fire,
            Steam,
            Crystal,
            Heat
        }

        protected sealed class Hazard
        {
            public HazardKind Kind;
            public int Serial;
            public float T;
            public float Delay;
            public float Fill;
            /// <summary>Seconds a pool burns or a beam fires after its wind-up.</summary>
            public float Active;
            public float Damage;
            public DamageType Type;
            public DamageFlags Flags;
            public Color Color;
            public BurstFx Fx;
            public Vector3 Center;
            public float Radius;
            public Lane04D Lane;
            public float Y;
            public Vector3 A;
            public Vector3 B;
            public float Width;
            public bool Shown;
            public bool Burst;
            public float NextTick;
            /// <summary>The burst leaves burning ground this many seconds (0 none).</summary>
            public float PoolSeconds;
            /// <summary>Game-specific mark (the Matron's nova checks it).</summary>
            public int Mark;
            public GroundRing Ring;
            public UBossLaneVis LaneVis;
            public UBossBeamVis BeamVis;
            public UBossDiscVis DiscVis;
            /// <summary>Called once when the hazard bursts (a ring under a shot that lands, a quench check).</summary>
            public Action<Hazard> OnBurst;
        }

        public const int MaxPools = 6;
        public const float RingReach = 3f;

        private readonly List<Hazard> _hazards = new List<Hazard>();
        private float _ringHitAt = -9f;
        private int _burstSoundFrame = -1;

        public int LiveHazards { get { return _hazards.Count; } }

        private float MinFill { get { return Diff == DifficultyLevel.Story ? 1f : UBossTiming.MinTell; } }

        /// <summary>A floor ring at c (snapped to the floor nearby) that fills after delay, then bursts.</summary>
        protected Hazard AddRing(Vector3 c, float radius, float delay, float fill, float damage, DamageType type, DamageFlags flags, BurstFx fx)
        {
            float y;
            if (UnderBossFx.Floor(c, 1.6f, 4f, out y)) c.y = y;
            Hazard h = new Hazard();
            h.Kind = HazardKind.Ring;
            h.Serial = MoveSerial;
            h.Center = c;
            h.Radius = Mathf.Max(0.5f, radius);
            h.Delay = Mathf.Max(0f, delay);
            h.Fill = Mathf.Max(MinFill, fill);
            h.Damage = damage;
            h.Type = type;
            h.Flags = flags;
            h.Fx = fx;
            h.Color = FxColor(fx);
            _hazards.Add(h);
            return h;
        }

        /// <summary>A floor lane (origin, direction, length, half width) at floor height y.</summary>
        protected Hazard AddLane(Lane04D geo, float y, float delay, float fill, float damage, DamageType type, DamageFlags flags, BurstFx fx)
        {
            Hazard h = new Hazard();
            h.Kind = HazardKind.Lane;
            h.Serial = MoveSerial;
            h.Lane = geo;
            h.Y = y;
            h.Delay = Mathf.Max(0f, delay);
            h.Fill = Mathf.Max(MinFill, fill);
            h.Damage = damage;
            h.Type = type;
            h.Flags = flags;
            h.Fx = fx;
            h.Color = FxColor(fx);
            h.LaneVis = UnderBossFx.TakeLane();
            UnderBossFx.PlaceLane(h.LaneVis, geo, y, 0f, h.Color, 0.6f);
            _hazards.Add(h);
            return h;
        }

        /// <summary>Burning ground at c for seconds (ticks every half second on a player standing in it). Capped at MaxPools.</summary>
        protected Hazard AddPool(Vector3 c, float radius, float seconds, float damagePerTick)
        {
            int pools = 0;
            for (int i = 0; i < _hazards.Count; i++) if (_hazards[i].Kind == HazardKind.Pool) pools++;
            if (pools >= MaxPools) return null;
            float y;
            if (UnderBossFx.Floor(c, 1.6f, 4f, out y)) c.y = y;
            Hazard h = new Hazard();
            h.Kind = HazardKind.Pool;
            h.Serial = 0;
            h.Center = c;
            h.Radius = Mathf.Max(0.5f, radius);
            h.Active = Mathf.Max(0.5f, seconds);
            h.Damage = damagePerTick;
            h.Type = DamageType.Fire;
            h.Fx = BurstFx.Fire;
            h.Color = FxColor(BurstFx.Fire);
            h.Shown = true;
            h.Burst = true;
            h.DiscVis = UnderBossFx.TakeDisc();
            UnderBossFx.PlaceDisc(h.DiscVis, c, h.Radius, h.Color, 0.5f);
            _hazards.Add(h);
            return h;
        }

        /// <summary>
        /// A beam from a toward b (cut at the first wall now): a thin aim line for warn seconds, then a bright beam for
        /// active seconds that ticks on a player it touches (feet to head, so a flyer in its path is hit too).
        /// </summary>
        protected Hazard AddBeam(Vector3 a, Vector3 b, float width, float warn, float active, float damagePerTick, DamageType type, BurstFx fx)
        {
            RaycastHit hit;
            if (Physics.Linecast(a, b, out hit, Layers.WorldMask, QueryTriggerInteraction.Ignore)) b = hit.point;
            Hazard h = new Hazard();
            h.Kind = HazardKind.Beam;
            h.Serial = MoveSerial;
            h.A = a;
            h.B = b;
            h.Width = Mathf.Max(0.2f, width);
            h.Fill = Mathf.Max(MinFill, warn);
            h.Active = Mathf.Max(0.2f, active);
            h.Damage = damagePerTick;
            h.Type = type;
            h.Fx = fx;
            h.Color = FxColor(fx);
            h.BeamVis = UnderBossFx.TakeBeam();
            UnderBossFx.PlaceBeam(h.BeamVis, a, b, 0.08f, h.Color, 0.5f);
            _hazards.Add(h);
            return h;
        }

        protected static Color FxColor(BurstFx fx)
        {
            switch (fx)
            {
                case BurstFx.Steam: return new Color(0.75f, 0.92f, 1f, 1f);
                case BurstFx.Crystal: return new Color(1f, 0.8f, 0.35f, 1f);
                case BurstFx.Heat: return new Color(1f, 0.25f, 0.08f, 1f);
                default: return new Color(1f, 0.48f, 0.12f, 1f);
            }
        }

        // ================================================================== frame

        private void UpdateHazards(float dt, float now)
        {
            for (int i = _hazards.Count - 1; i >= 0; i--)
            {
                Hazard h = _hazards[i];
                h.T += dt;
                bool done;
                switch (h.Kind)
                {
                    case HazardKind.Ring: done = TickRing(h); break;
                    case HazardKind.Lane: done = TickLane(h); break;
                    case HazardKind.Pool: done = TickPool(h, now); break;
                    default: done = TickBeam(h, now); break;
                }
                if (!done) continue;
                FreeVisuals(h, false);
                _hazards.RemoveAt(i);
            }
        }

        private bool TickRing(Hazard h)
        {
            if (!h.Shown)
            {
                if (h.T < h.Delay) return false;
                h.Shown = true;
                h.Ring = GroundRing.Show(h.Center, h.Radius, h.Fill, h.Color);
            }
            if (h.T < h.Delay + h.Fill) return false;
            if (h.Ring != null)
            {
                h.Ring.Burst();
                h.Ring = null;
            }
            BurstEffect(h.Center, h.Radius, h.Fx);
            PlayerController p = Game.Player;
            if (p != null && !p.IsDead)
            {
                Vector3 pp = PlayerFeet;
                if (Mathf.Abs(pp.y - h.Center.y) <= RingReach && MathUtil.FlatDistance(pp, h.Center) <= h.Radius + 0.3f && !SafeFromHazard(h, pp))
                    HitPlayer(h.Damage, h.Center, h.Type, h.Flags, 3f);
            }
            AfterBurst(h);
            return true;
        }

        private bool TickLane(Hazard h)
        {
            if (h.T < h.Delay) return false;
            float k = Mathf.Clamp01((h.T - h.Delay) / h.Fill);
            if (!h.Burst)
            {
                float pulse = k > 0.75f ? 0.75f + 0.25f * Mathf.Sin(Time.time * 30f) : 1f;
                UnderBossFx.PlaceLane(h.LaneVis, h.Lane, h.Y, k, h.Color, pulse);
                if (k < 1f) return false;
                h.Burst = true;
                h.T = h.Delay + h.Fill;
                Vector3 mid = h.Lane.Origin + h.Lane.Dir * (h.Lane.Length * 0.5f);
                mid.y = h.Y;
                BurstEffect(mid, h.Lane.HalfWidth * 2f, h.Fx);
                PlayerController p = Game.Player;
                if (p != null && !p.IsDead)
                {
                    Vector3 pp = PlayerFeet;
                    if (Mathf.Abs(pp.y - h.Y) <= RingReach && BossLanes04D.InLane(h.Lane, pp) && !SafeFromHazard(h, pp))
                    {
                        Vector3 o = new Vector3(h.Lane.Origin.x, h.Y + 1.2f, h.Lane.Origin.z);
                        if (!Physics.Linecast(o, PlayerAim, Layers.WorldMask, QueryTriggerInteraction.Ignore)) HitPlayer(h.Damage, o, h.Type, h.Flags, 3.5f);
                    }
                }
                AfterBurst(h);
                return false;
            }
            // The burst flash fades over a third of a second.
            float f = Mathf.Clamp01((h.T - h.Delay - h.Fill) / 0.33f);
            UnderBossFx.PlaceLane(h.LaneVis, h.Lane, h.Y, 1f, h.Color, 1.6f * (1f - f));
            return f >= 1f;
        }

        private bool TickPool(Hazard h, float now)
        {
            float k = Mathf.Clamp01(h.T / h.Active);
            float flicker = 0.42f + 0.12f * Mathf.Sin(now * 9f + h.Center.x);
            UnderBossFx.PlaceDisc(h.DiscVis, h.Center, h.Radius, h.Color, flicker * (1f - k * k));
            if (now >= h.NextTick)
            {
                h.NextTick = now + 0.5f;
                if (Game.Fx != null && UnityEngine.Random.value < 0.6f) Game.Fx.FirePuff(h.Center + new Vector3(UnityEngine.Random.Range(-h.Radius, h.Radius) * 0.6f, 0.2f, UnityEngine.Random.Range(-h.Radius, h.Radius) * 0.6f), 0.6f);
                PlayerController p = Game.Player;
                if (p != null && !p.IsDead && !p.InVehicle)
                {
                    Vector3 pp = PlayerFeet;
                    if (Mathf.Abs(pp.y - h.Center.y) <= 2f && MathUtil.FlatDistance(pp, h.Center) <= h.Radius) HitPlayer(h.Damage, h.Center, DamageType.Fire, DamageFlags.Unblockable, 0.5f);
                }
            }
            return h.T >= h.Active;
        }

        private bool TickBeam(Hazard h, float now)
        {
            if (h.T < h.Fill)
            {
                float k = h.T / h.Fill;
                float w = Mathf.Lerp(0.05f, 0.16f, k);
                float pulse = k > 0.75f ? 0.7f + 0.3f * Mathf.Sin(now * 28f) : 0.55f;
                UnderBossFx.PlaceBeam(h.BeamVis, h.A, h.B, w, h.Color, pulse);
                return false;
            }
            if (!h.Burst)
            {
                h.Burst = true;
                Underheart04ESounds.Play(UhSfx.BeamCharge, h.A, 0.8f, 1.2f);
                if (Game.Fx != null) Game.Fx.Flash(h.A, h.Color, 4f, 14f, 0.25f);
            }
            float a = h.T - h.Fill;
            UnderBossFx.PlaceBeam(h.BeamVis, h.A, h.B, h.Width * (0.85f + 0.15f * Mathf.Sin(now * 40f)), h.Color, 1.4f);
            if (now >= h.NextTick)
            {
                h.NextTick = now + 0.3f;
                PlayerController p = Game.Player;
                if (p != null && !p.IsDead)
                {
                    Vector3 feet = PlayerFeet;
                    float height = p.InVehicle ? 1.6f : 1.8f;
                    if (BossLanes04D.BeamHitsBody(h.A, h.B, h.Width * 0.5f + 0.35f, feet, height) && !SafeFromHazard(h, feet)) HitPlayer(h.Damage, h.A, h.Type, DamageFlags.None, 2f);
                }
            }
            return a >= h.Active;
        }

        /// <summary>A subclass may spare the player from a hazard (standing on a cooled plate during the cinder nova).</summary>
        protected virtual bool SafeFromHazard(Hazard h, Vector3 feet) { return false; }

        private void AfterBurst(Hazard h)
        {
            if (h.PoolSeconds > 0f)
            {
                Vector3 c = h.Kind == HazardKind.Lane ? h.Lane.Origin + h.Lane.Dir * (h.Lane.Length * 0.5f) : h.Center;
                if (h.Kind == HazardKind.Lane) c.y = h.Y;
                AddPool(c, h.Kind == HazardKind.Lane ? h.Lane.HalfWidth * 1.4f : h.Radius * 0.8f, h.PoolSeconds, h.Damage * 0.15f);
            }
            if (h.OnBurst == null) return;
            try { h.OnBurst(h); }
            catch (Exception e) { Log.Exception("Underheart boss hazard burst", e); }
        }

        private void BurstEffect(Vector3 at, float size, BurstFx fx)
        {
            if (Game.Fx != null)
            {
                Vector3 c = at + Vector3.up * 0.3f;
                switch (fx)
                {
                    case BurstFx.Steam:
                        for (int i = 0; i < 4; i++) Game.Fx.Smoke(c + UnityEngine.Random.insideUnitSphere * size * 0.3f, Vector3.up * 2.5f, 1.4f, 0.05f);
                        break;
                    case BurstFx.Crystal:
                        Game.Fx.CrystalBurst(c, Profile.Theme, 14, size * 0.4f);
                        break;
                    case BurstFx.Heat:
                        Game.Fx.RockBurst(c, Profile.Theme, 8, size * 0.3f);
                        for (int i = 0; i < 3; i++) Game.Fx.FirePuff(c + UnityEngine.Random.insideUnitSphere * size * 0.3f, 0.9f);
                        break;
                    default:
                        for (int i = 0; i < 4; i++) Game.Fx.FirePuff(c + UnityEngine.Random.insideUnitSphere * size * 0.3f, 0.8f);
                        Game.Fx.RockBurst(c, Profile.Theme, 6, size * 0.3f);
                        break;
                }
            }
            if (_burstSoundFrame != Time.frameCount)
            {
                _burstSoundFrame = Time.frameCount;
                if (fx == BurstFx.Steam) Underheart04ESounds.Play(UhSfx.SteamBurst, at, 0.8f, 1f);
                else if (Game.Audio != null) Game.Audio.PlayVar(SfxId.ExplosionSmall, at, 0.45f, 0.2f, 45f);
            }
        }

        /// <summary>Releases hazards of one move serial (a broken wind-up), or all of them (-1). Returns how many went.</summary>
        protected int ReleaseHazards(int serial)
        {
            int n = 0;
            for (int i = _hazards.Count - 1; i >= 0; i--)
            {
                Hazard h = _hazards[i];
                if (serial >= 0 && (h.Serial != serial || h.Burst || h.Kind == HazardKind.Pool)) continue;
                FreeVisuals(h, true);
                _hazards.RemoveAt(i);
                n++;
            }
            return n;
        }

        private static void FreeVisuals(Hazard h, bool cancelled)
        {
            if (h.Ring != null)
            {
                h.Ring.Release();
                h.Ring = null;
            }
            if (h.LaneVis != null)
            {
                UnderBossFx.FreeLane(h.LaneVis);
                h.LaneVis = null;
            }
            if (h.BeamVis != null)
            {
                UnderBossFx.FreeBeam(h.BeamVis);
                h.BeamVis = null;
            }
            if (h.DiscVis != null)
            {
                UnderBossFx.FreeDisc(h.DiscVis);
                h.DiscVis = null;
            }
        }

        /// <summary>True while any beam of the fight is firing (the beam loop plays).</summary>
        protected bool AnyBeamFiring
        {
            get
            {
                for (int i = 0; i < _hazards.Count; i++) if (_hazards[i].Kind == HazardKind.Beam && _hazards[i].Burst) return true;
                return false;
            }
        }

        /// <summary>True while any hazard of the current move has not burst yet.</summary>
        protected bool MovePending
        {
            get
            {
                for (int i = 0; i < _hazards.Count; i++)
                {
                    Hazard h = _hazards[i];
                    if (h.Serial != MoveSerial || h.Kind == HazardKind.Pool) continue;
                    if (!h.Burst || (h.Kind == HazardKind.Beam && h.T < h.Fill + h.Active)) return true;
                }
                return false;
            }
        }

        // ================================================================== hitting the player

        /// <summary>
        /// One boss hit on the player: the profile damage times mul, capped per hit by the difficulty like every creature
        /// hit; a vehicle takes it on its body. Hits of one volley respect the ring respite. The direction runs from the
        /// source to the player, so a raised ward catches it by angle. True when it landed.
        /// </summary>
        protected bool HitPlayer(float mul, Vector3 from, DamageType type, DamageFlags flags, float force)
        {
            PlayerController p = Game.Player;
            if (p == null || p.IsDead || !(mul > 0f)) return false;
            if (!CreatureFightRules.RingMayHit(Time.time, _ringHitAt)) return false;
            GameObject src = Body != null ? Body.gameObject : gameObject;
            if (p.InVehicle && p.CurrentVehicle != null)
            {
                _ringHitAt = Time.time;
                p.CurrentVehicle.ApplyDamage(Profile.BaseDamage * mul * 2f, src);
                return true;
            }
            float raw = CreatureBrainLogic.CapHit(Profile.BaseDamage * mul, p.Health.MaxHealth, (int)Settings.Difficulty, CombatDirector.PlayerDamageTaken);
            Vector3 dir = p.ChestPosition - from;
            DamageInfo info = DamageInfo.Make(raw, p.ChestPosition, dir.sqrMagnitude > 0.01f ? dir : Vector3.up, src, type);
            info.Force = force;
            info.Flags = flags & ~DamageFlags.Heavy;
            p.Health.TakeDamage(info);
            if (p.Health.LastBlocked) return false;
            _ringHitAt = Time.time;
            return true;
        }

        /// <summary>Damage for an aimed projectile (capped like a hit) for CreatureShot, which applies it itself.</summary>
        protected float ShotDamage(float mul)
        {
            PlayerController p = Game.Player;
            float max = p != null && p.Health != null ? p.Health.MaxHealth : 100f;
            return CreatureBrainLogic.CapHit(Profile.BaseDamage * mul, max, (int)Settings.Difficulty, CombatDirector.PlayerDamageTaken);
        }

        /// <summary>
        /// Fires an aimed projectile from a toward a target point fixed now (never a prediction): straight for a flyer or
        /// a vehicle, a lob for a target on the ground. Dodgeable and blockable like any ranged hit.
        /// </summary>
        protected void FireShot(Vector3 from, Vector3 target, float speed, bool lob, float mul, Element element, Color color)
        {
            Vector3 d = target - from;
            Vector3 v;
            float g = 0f;
            if (lob)
            {
                g = 12f;
                Vector3 flat = MathUtil.Flat(d);
                float dist = Mathf.Max(1f, flat.magnitude);
                float t = Mathf.Clamp(dist / speed, 0.6f, 2.4f);
                v = flat / t + Vector3.up * ((d.y + 0.5f * g * t * t) / t);
            }
            else v = d.sqrMagnitude > 0.01f ? d.normalized * speed : Body.forward * speed;
            CreatureShot.Fire(from, v, g, ShotDamage(mul), 0.32f, DamageType.Fire, element, color, Body != null ? Body.gameObject : gameObject, DamageFlags.None, 2.2f);
        }
    }
}
