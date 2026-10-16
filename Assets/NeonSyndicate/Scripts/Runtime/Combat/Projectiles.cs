using UnityEngine;
using UnityEngine.Rendering;

namespace NeonSyndicate
{
    /// <summary>
    /// Rockets, 40mm grenades, thrown frag grenades and fire bottles, plus the homing micro rockets of loot guns.
    /// Movement is integrated by hand with sphere sweeps every physics step (no Rigidbody), so fast rockets never
    /// tunnel and grenades bounce predictably. Loot launchers add their element and traits after the blast.
    /// </summary>
    public sealed class Projectile : MonoBehaviour
    {
        public enum Kind
        {
            Rocket,
            Grenade40,
            Frag,
            FireBottle,
            Sunbolt,
            /// <summary>A loot gun's small homing rocket: a tiny blast that only hurts foes.</summary>
            MicroRocket
        }

        public Kind Type { get; private set; }
        private WeaponDef _def;
        private GameObject _owner;
        private Vector3 _vel;
        private float _age;
        private float _fuse;
        private float _travelled;
        private float _trailTimer;
        private bool _done;
        private bool _resting;
        private Light _light;
        private AudioSource _loop;
        private Transform _visual;
        private Transform _homing;
        private readonly RaycastHit[] _hits = new RaycastHit[12];
        private const float Radius = 0.09f;

        // ------------------------------------------------------------------ factories

        public static Projectile Rocket(Vector3 pos, Vector3 dir, WeaponDef d, GameObject owner)
        {
            Projectile p = Create("Rocket", pos, d, owner, Kind.Rocket);
            p._vel = dir.normalized * Mathf.Max(10f, d.ProjectileSpeed);
            p._fuse = 8f;
            Shapes.Cyl("Body", p._visual, Vector3.zero, new Vector3(0.12f, 0.45f, 0.12f), Mats.DarkMetal, false).transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            Shapes.Cyl("Nose", p._visual, new Vector3(0f, 0f, 0.5f), new Vector3(0.1f, 0.08f, 0.1f), Mats.RedPlastic, false).transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            p.AddLight(new Color(1f, 0.6f, 0.25f), 6f, 2.5f);
            if (Game.Audio != null)
            {
                p._loop = Game.Audio.CreateLoop(p.transform, SfxId.Fuse, 0.7f, 60f);
                if (p._loop != null) p._loop.pitch = 0.6f;
            }
            p.transform.rotation = Quaternion.LookRotation(p._vel);
            return p;
        }

        /// <summary>Malik's crystal power: a blazing orb that bursts on impact.</summary>
        public static Projectile Sunbolt(Vector3 pos, Vector3 dir, float damage, float radius, GameObject owner)
        {
            WeaponDef d = new WeaponDef { Id = WeaponId.Hammerhead, Name = "Sunbolt", Damage = damage, BlastRadius = radius, ProjectileSpeed = 48f };
            Projectile p = Create("Sunbolt", pos, d, owner, Kind.Sunbolt);
            p._vel = dir.normalized * d.ProjectileSpeed;
            p._fuse = 5f;
            Shapes.Ball("Orb", p._visual, Vector3.zero, Vector3.one * 0.45f, Mats.Emissive(new Color(1f, 0.75f, 0.25f), 3f), false);
            p.AddLight(new Color(1f, 0.7f, 0.3f), 9f, 3.5f);
            if (Game.Audio != null) Game.Audio.PlayAt(SfxId.Power, pos, 0.9f, 1.1f, 60f);
            return p;
        }

        /// <summary>A loot gun's micro rocket: steers toward target (when there is one) and bursts in a small blast.</summary>
        public static Projectile MicroRocket(Vector3 pos, Vector3 dir, float damage, Transform target, WeaponDef gun, GameObject owner)
        {
            WeaponDef d = new WeaponDef { Id = WeaponId.Hammerhead, Name = "Micro Rocket", Damage = damage, BlastRadius = 2f, ProjectileSpeed = 30f };
            if (gun != null)
            {
                d.Element = gun.Element;
                d.ElementPower = gun.ElementPower;
            }
            Projectile p = Create("Micro Rocket", pos, d, owner, Kind.MicroRocket);
            p._vel = dir.normalized * 22f;
            p._fuse = 3.5f;
            p._homing = target;
            Color glow = gun != null && gun.Element != Element.None ? GearSystem.ElementColor(gun.Element) : new Color(1f, 0.6f, 0.25f);
            Shapes.Cyl("Body", p._visual, Vector3.zero, new Vector3(0.05f, 0.14f, 0.05f), Mats.DarkMetal, false).transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            Shapes.Ball("Glow", p._visual, new Vector3(0f, 0f, -0.14f), Vector3.one * 0.08f, Mats.Emissive(glow, 3f), false);
            p.transform.rotation = Quaternion.LookRotation(p._vel);
            if (Game.Audio != null) Game.Audio.PlayAt(SfxId.Whoosh, pos, 0.35f, 1.6f, 40f);
            return p;
        }

        public static Projectile Launched(Vector3 pos, Vector3 velocity, WeaponDef d, GameObject owner)
        {
            Projectile p = Create("40mm Grenade", pos, d, owner, Kind.Grenade40);
            p._vel = velocity;
            p._fuse = 6f;
            Shapes.Ball("Shell", p._visual, Vector3.zero, new Vector3(0.09f, 0.09f, 0.14f), Mats.Solid(new Color(0.25f, 0.3f, 0.15f), 0.3f), false);
            return p;
        }

        public static Projectile Thrown(Vector3 pos, Vector3 velocity, WeaponDef d, GameObject owner)
        {
            bool fire = d.Id == WeaponId.FireBottle;
            Projectile p = Create(fire ? "Fire Bottle" : "Frag Grenade", pos, d, owner, fire ? Kind.FireBottle : Kind.Frag);
            p._vel = velocity;
            p._fuse = fire ? 10f : 3f;
            if (fire)
            {
                Shapes.Cyl("Bottle", p._visual, Vector3.zero, new Vector3(0.09f, 0.14f, 0.09f), Mats.Solid(new Color(0.2f, 0.45f, 0.2f, 1f), 0.9f), false);
                Shapes.Box("Rag", p._visual, new Vector3(0f, 0.17f, 0f), new Vector3(0.05f, 0.08f, 0.05f), Mats.Solid(new Color(0.75f, 0.68f, 0.5f), 0.1f), false);
                p.AddLight(new Color(1f, 0.55f, 0.2f), 4f, 1.2f);
            }
            else
            {
                Shapes.Ball("Frag", p._visual, Vector3.zero, new Vector3(0.09f, 0.11f, 0.09f), Mats.Solid(new Color(0.22f, 0.28f, 0.18f), 0.35f), false);
                Shapes.Cyl("Spoon", p._visual, new Vector3(0f, 0.07f, 0f), new Vector3(0.03f, 0.03f, 0.03f), Mats.Metal, false);
                if (Game.Audio != null) Game.Audio.PlayAt(SfxId.Pin, pos, 0.5f);
            }
            return p;
        }

        private static Projectile Create(string name, Vector3 pos, WeaponDef d, GameObject owner, Kind kind)
        {
            GameObject go = new GameObject(name);
            go.layer = Layers.IgnoreRaycast;
            go.transform.position = pos;
            Projectile p = go.AddComponent<Projectile>();
            p._def = d;
            p._owner = owner;
            p.Type = kind;
            GameObject vis = new GameObject("Visual");
            vis.layer = Layers.IgnoreRaycast;
            vis.transform.SetParent(go.transform, false);
            p._visual = vis.transform;
            return p;
        }

        private void AddLight(Color c, float range, float intensity)
        {
            _light = gameObject.AddComponent<Light>();
            _light.type = LightType.Point;
            _light.color = c;
            _light.range = range;
            _light.intensity = intensity;
            _light.shadows = LightShadows.None;
        }

        // ------------------------------------------------------------------ simulation

        private void FixedUpdate()
        {
            if (_done) return;
            float dt = Time.fixedDeltaTime;
            _age += dt;
            if (_age >= _fuse)
            {
                Detonate(transform.position, Vector3.up, null);
                return;
            }
            if (_resting) return;
            float gravity = Type == Kind.Rocket ? 0.6f : (Type == Kind.Sunbolt || Type == Kind.MicroRocket ? 0f : 9.81f);
            Vector3 start = transform.position;
            _vel += Vector3.down * gravity * dt;
            if (Type == Kind.Rocket) _vel = _vel.normalized * Mathf.Min(_vel.magnitude + 30f * dt, _def.ProjectileSpeed * 1.4f);
            if (Type == Kind.MicroRocket) SteerMicro(dt);
            Vector3 step = _vel * dt;
            float dist = step.magnitude;
            if (dist < 1e-5f) return;
            RaycastHit hit;
            if (Sweep(start, step / dist, dist, out hit))
            {
                OnImpact(hit);
                if (_done) return;
            }
            else
            {
                transform.position = start + step;
            }
            _travelled += dist;
            if (Type == Kind.Rocket || Type == Kind.MicroRocket)
            {
                if (_vel.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(_vel);
            }
            else if (_visual != null)
            {
                _visual.Rotate(new Vector3(540f, 120f, 0f) * dt, Space.Self);
            }
            if (transform.position.y < WorldBounds.KillY || !MathUtil.IsFinite(transform.position)) Destroy(gameObject);
        }

        private void Update()
        {
            if (_done || Game.Fx == null) return;
            _trailTimer -= Time.deltaTime;
            if (_trailTimer > 0f) return;
            switch (Type)
            {
                case Kind.Rocket:
                    _trailTimer = 0.015f;
                    Game.Fx.Smoke(transform.position - _vel.normalized * 0.5f, -_vel * 0.05f + Random.insideUnitSphere * 0.4f, 0.7f, 0.75f);
                    Game.Fx.FirePuff(transform.position - _vel.normalized * 0.45f, 0.35f);
                    break;
                case Kind.FireBottle:
                    _trailTimer = 0.05f;
                    Game.Fx.FirePuff(transform.position + Vector3.up * 0.18f, 0.18f);
                    break;
                case Kind.MicroRocket:
                    _trailTimer = 0.03f;
                    Game.Fx.Smoke(transform.position - _vel.normalized * 0.2f, Vector3.zero, 0.25f, 0.6f);
                    break;
                case Kind.Sunbolt:
                    _trailTimer = 0.012f;
                    Game.Fx.FirePuff(transform.position + Random.insideUnitSphere * 0.2f, 0.55f);
                    break;
                case Kind.Grenade40:
                    _trailTimer = 0.05f;
                    Game.Fx.Smoke(transform.position, Vector3.zero, 0.2f, 0.85f);
                    break;
                default:
                    _trailTimer = 1f;
                    break;
            }
        }

        private void SteerMicro(float dt)
        {
            float speed = Mathf.Min(_vel.magnitude + 40f * dt, _def.ProjectileSpeed);
            Vector3 dir = _vel.sqrMagnitude > 0.01f ? _vel.normalized : transform.forward;
            if (_homing != null && _age > 0.12f)
            {
                CharacterBase c = _homing.GetComponent<CharacterBase>();
                if (c != null && !c.IsDead)
                {
                    Vector3 want = (c.ChestPosition - transform.position).normalized;
                    dir = Vector3.RotateTowards(dir, want, 5f * dt, 0f);
                }
                else _homing = null;
            }
            _vel = dir * speed;
        }

        private bool Sweep(Vector3 from, Vector3 dir, float dist, out RaycastHit best)
        {
            best = new RaycastHit();
            int n = Physics.SphereCastNonAlloc(from, Radius, dir, _hits, dist, Layers.ShootMask, QueryTriggerInteraction.Ignore);
            float bestDist = float.MaxValue;
            bool found = false;
            for (int i = 0; i < n; i++)
            {
                RaycastHit h = _hits[i];
                if (h.collider == null) continue;
                if (_owner != null && (h.collider.transform == _owner.transform || h.collider.transform.IsChildOf(_owner.transform)) && _age < 0.6f) continue;
                if (h.distance <= 0f && h.point == Vector3.zero) continue; // started inside: ignore
                if (h.distance < bestDist)
                {
                    bestDist = h.distance;
                    best = h;
                    found = true;
                }
            }
            return found;
        }

        private void OnImpact(RaycastHit hit)
        {
            switch (Type)
            {
                case Kind.Rocket:
                case Kind.Sunbolt:
                case Kind.MicroRocket:
                    Detonate(hit.point + hit.normal * 0.25f, hit.normal, hit.collider);
                    return;
                case Kind.FireBottle:
                    Shatter(hit);
                    return;
                case Kind.Grenade40:
                    if (_travelled > 5f || hit.collider.GetComponentInParent<IDamageable>() != null)
                    {
                        Detonate(hit.point + hit.normal * 0.2f, hit.normal, hit.collider);
                        return;
                    }
                    Bounce(hit, 0.3f);
                    return;
                default:
                    Bounce(hit, 0.38f);
                    return;
            }
        }

        private void Bounce(RaycastHit hit, float restitution)
        {
            transform.position = hit.point + hit.normal * (Radius + 0.01f);
            Vector3 vn = Vector3.Dot(_vel, hit.normal) * hit.normal;
            Vector3 vt = _vel - vn;
            _vel = vt * 0.72f - vn * restitution;
            if (vn.magnitude > 2f && Game.Audio != null) Game.Audio.PlayVar(SfxId.MetalHit, hit.point, 0.25f, 0.2f, 25f);
            // Settle when slow on fairly flat ground.
            if (_vel.magnitude < 0.6f && hit.normal.y > 0.7f)
            {
                _vel = Vector3.zero;
                _resting = true;
            }
            Rigidbody rb = hit.collider.attachedRigidbody;
            if (rb != null && !rb.isKinematic) _resting = false;
        }

        private void Shatter(RaycastHit hit)
        {
            _done = true;
            Vector3 p = hit.point + hit.normal * 0.1f;
            if (Game.Audio != null) Game.Audio.PlayVar(SfxId.GlassBreak, p, 0.8f, 0.1f, 60f);
            if (Game.Fx != null)
            {
                Game.Fx.Flash(p + Vector3.up * 0.5f, new Color(1f, 0.55f, 0.2f), 4f, 9f, 0.4f);
                for (int i = 0; i < 10; i++) Game.Fx.FirePuff(p + Random.insideUnitSphere * 1.2f, 0.9f);
            }
            // A direct hit sets the victim alight.
            IDamageable direct = hit.collider.GetComponentInParent<IDamageable>();
            if (direct != null && direct.IsAlive)
            {
                DamageInfo info = DamageInfo.Make(_def.Damage, hit.point, _vel, _owner, DamageType.Fire);
                direct.TakeDamage(info);
            }
            Vector3 ground = p;
            RaycastHit g;
            if (Physics.Raycast(p + Vector3.up * 0.5f, Vector3.down, out g, 4f, Layers.WorldMask, QueryTriggerInteraction.Ignore)) ground = g.point;
            FireArea.Spawn(ground, _def.BlastRadius, 9f, _def.Damage, _owner);
            Threats.Report(p, 30f, _owner, ThreatKind.Fire);
            if (_owner != null && Game.Player != null && _owner == Game.Player.gameObject && Game.Wanted != null)
                Game.Wanted.ReportCrime(CrimeType.Arson, p, null);
            Destroy(gameObject);
        }

        private void Detonate(Vector3 pos, Vector3 normal, Collider directHit)
        {
            if (_done) return;
            _done = true;
            if (Type == Kind.FireBottle)
            {
                FireArea.Spawn(pos, _def.BlastRadius, 9f, _def.Damage, _owner);
                Destroy(gameObject);
                return;
            }
            if (Type == Kind.MicroRocket)
            {
                // A small blast that only hurts foes, so the shooter is never caught by their own rockets.
                if (Game.Fx != null) Game.Fx.ExplosionFx(pos, 1.1f);
                if (Game.Audio != null) Game.Audio.PlayVar(SfxId.ExplosionSmall, pos, 0.55f, 0.1f, 120f);
                CharacterBase struck = directHit != null ? directHit.GetComponentInParent<CharacterBase>() : null;
                if (struck != null && LootCombat.IsFoe(struck, _owner))
                {
                    DamageInfo hit = DamageInfo.Make(_def.Damage * 0.6f, pos, _vel, _owner, DamageType.Explosion);
                    hit.Force = 6f;
                    struck.Health.TakeDamage(hit);
                    if (_def.Element != Element.None) LootCombat.ApplyElement(struck, _def.Element, _def.ElementPower, _def.Damage, _owner);
                }
                LootCombat.Splash(pos, _def.BlastRadius, _def.Damage * 0.6f, _owner, struck);
                Destroy(gameObject);
                return;
            }
            CharacterBase directBody = null;
            bool directAlive = false;
            if (directHit != null)
            {
                IDamageable d = directHit.GetComponentInParent<IDamageable>();
                directBody = directHit.GetComponentInParent<CharacterBase>();
                directAlive = d != null && d.IsAlive;
                if (d != null && d.IsAlive)
                {
                    DamageInfo info = DamageInfo.Make(_def.Damage * 0.5f, pos, _vel, _owner, DamageType.Explosion);
                    info.Force = 14f;
                    d.TakeDamage(info);
                }
            }
            if (Game.Explosions != null)
                Game.Explosions.Explode(pos, _def.BlastRadius, _def.Damage, _owner, Type == Kind.Rocket ? ExplosionKind.Rocket : (Type == Kind.Sunbolt ? ExplosionKind.Power : ExplosionKind.Grenade));
            // Resonance: the player's Sunbolt leaves the Kindled mark on the hostiles it caught.
            if (Type == Kind.Sunbolt && Game.IsPlayer(_owner)) ResonanceSystem.PrimeAround(pos, _def.BlastRadius, 8, ResonanceState.Kindled, false);
            if (_def.IsLoot)
            {
                try { LootCombat.OnBlast(pos, _def, _owner, directBody, directAlive); }
                catch (System.Exception e) { Log.Exception("Loot blast", e); }
            }
            Destroy(gameObject);
        }
    }

    /// <summary>A patch of burning fuel: damages and ignites characters and vehicles inside it for a while.</summary>
    public sealed class FireArea : MonoBehaviour
    {
        public float Radius = 4f;
        public float Remaining = 9f;
        public float DamagePerTick = 6f;
        public GameObject Source;
        private float _tick;
        private float _fxTimer;
        private Light _light;
        private AudioSource _loop;
        private readonly Collider[] _buf = new Collider[48];
        private readonly System.Collections.Generic.HashSet<IDamageable> _burned = new System.Collections.Generic.HashSet<IDamageable>();
        private static int _active;
        public static int ActiveCount { get { return _active; } }

        public static void ResetStatics()
        {
            _active = 0;
        }

        public static FireArea Spawn(Vector3 pos, float radius, float seconds, float damage, GameObject source)
        {
            if (_active > 24) return null;
            GameObject go = new GameObject("Fire");
            go.transform.position = pos;
            FireArea f = go.AddComponent<FireArea>();
            f.Radius = Mathf.Max(1f, radius);
            f.Remaining = seconds;
            f.DamagePerTick = Mathf.Max(2f, damage * 0.35f);
            f.Source = source;
            return f;
        }

        private void Start()
        {
            _active++;
            _light = gameObject.AddComponent<Light>();
            _light.type = LightType.Point;
            _light.color = new Color(1f, 0.5f, 0.18f);
            _light.range = Radius * 3f;
            _light.intensity = 3f;
            _light.shadows = LightShadows.None;
            if (Game.Audio != null) _loop = Game.Audio.CreateLoop(transform, SfxId.FireLoop, 0.6f, 45f);
            if (Game.Fx != null) Game.Fx.Decal(transform.position + Vector3.up * 0.02f, Vector3.up, Mats.Scorch, Radius * 1.6f);
        }

        private void OnDestroy()
        {
            _active = Mathf.Max(0, _active - 1);
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            Remaining -= dt;
            float life = Mathf.Clamp01(Remaining / 2f);
            if (_light != null) _light.intensity = (2.4f + Mathf.PerlinNoise(Time.time * 9f, 0.3f) * 1.8f) * life;
            if (_loop != null) _loop.volume = 0.6f * life * Settings.SfxVolume;
            _fxTimer -= dt;
            if (_fxTimer <= 0f && Game.Fx != null)
            {
                _fxTimer = 0.03f;
                Vector2 r = Random.insideUnitCircle * Radius;
                Game.Fx.FirePuff(transform.position + new Vector3(r.x, 0.2f, r.y), Random.Range(0.6f, 1.3f) * Mathf.Max(0.3f, life));
                if (Random.value < 0.25f) Game.Fx.Smoke(transform.position + new Vector3(r.x, 1.5f, r.y), Vector3.up * 1.5f, 1.4f, 0.15f);
            }
            _tick -= dt;
            if (_tick <= 0f)
            {
                _tick = 0.3f;
                Burn();
            }
            if (Remaining <= 0f) Destroy(gameObject);
        }

        private void Burn()
        {
            int mask = (1 << Layers.Characters) | (1 << Layers.Ragdoll) | (1 << Layers.Vehicles) | (1 << Layers.Debris);
            int n = Physics.OverlapSphereNonAlloc(transform.position + Vector3.up * 0.5f, Radius, _buf, mask, QueryTriggerInteraction.Ignore);
            _burned.Clear();
            for (int i = 0; i < n; i++)
            {
                Collider c = _buf[i];
                if (c == null) continue;
                IDamageable d = c.GetComponentInParent<IDamageable>();
                if (d == null || !d.IsAlive || !_burned.Add(d)) continue;
                DamageInfo info = DamageInfo.Make(DamagePerTick, c.bounds.center, Vector3.up, Source, DamageType.Fire);
                d.TakeDamage(info);
            }
            Threats.Report(transform.position, Radius * 4f, Source, ThreatKind.Fire);
        }
    }
}
