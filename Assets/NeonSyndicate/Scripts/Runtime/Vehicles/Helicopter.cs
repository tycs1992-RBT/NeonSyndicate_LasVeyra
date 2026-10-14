using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// LVPD air unit: circles the suspect above the rooftops, lights them up with a searchlight at night, keeps
    /// the police informed while it has eyes on you, fields a marksman at four stars and can be shot down.
    /// </summary>
    public sealed class Helicopter : MonoBehaviour, IDamageable
    {
        public float Health = 1500f;
        public bool Armed;
        public bool IsDestroyed { get; private set; }
        private float _destroyedTime;
        private Transform _rotor;
        private Transform _tailRotor;
        private Light _spot;
        private AudioSource _loop;
        private Rigidbody _rb;
        private Vector3 _vel;
        private float _orbit;
        private bool _leaving;
        private float _shotTimer = 3f;
        private float _sightTimer;
        private Vector3 _spotTarget;
        private float _smokeTimer;

        public float DestroyedFor { get { return IsDestroyed ? Time.time - _destroyedTime : 0f; } }
        public bool IsAlive { get { return !IsDestroyed; } }

        public static Helicopter Spawn(Vector3 pos, bool armed)
        {
            GameObject go = new GameObject("LVPD Air Unit");
            go.layer = Layers.Vehicles;
            go.transform.position = pos;
            Helicopter h = go.AddComponent<Helicopter>();
            h.Armed = armed;
            h.Build();
            return h;
        }

        private void Build()
        {
            MeshBuilder b = new MeshBuilder(4);
            b.FixedUV = new Vector2(0.5f, 0.5f);
            // Cabin, tail boom, fin, skids.
            b.Sphere(0, new Vector3(0f, 1.3f, 0.3f), new Vector3(1.1f, 1.05f, 1.9f), 6, 10, Quaternion.identity);
            b.Sphere(1, new Vector3(0f, 1.45f, 1.35f), new Vector3(0.85f, 0.75f, 0.9f), 5, 8, Quaternion.identity);
            b.Box(0, new Vector3(0f, 1.55f, -2.6f), new Vector3(0.35f, 0.35f, 3.6f), UVMode.Fixed);
            b.Box(2, new Vector3(0f, 2.05f, -4.3f), new Vector3(0.1f, 1.1f, 0.6f), UVMode.Fixed);
            b.Box(2, new Vector3(0f, 1.55f, -4.1f), new Vector3(1.4f, 0.08f, 0.4f), UVMode.Fixed);
            b.Box(3, new Vector3(-0.8f, 0.12f, 0.3f), new Vector3(0.08f, 0.08f, 2.8f), UVMode.Fixed);
            b.Box(3, new Vector3(0.8f, 0.12f, 0.3f), new Vector3(0.08f, 0.08f, 2.8f), UVMode.Fixed);
            for (int s = -1; s <= 1; s += 2)
                for (int z = -1; z <= 1; z += 2)
                    b.Box(3, new Vector3(s * 0.7f, 0.4f, 0.3f + z * 0.8f), new Vector3(0.06f, 0.6f, 0.06f), UVMode.Fixed);
            b.Box(2, new Vector3(0f, 2.45f, 0.2f), new Vector3(0.5f, 0.3f, 0.9f), UVMode.Fixed);
            GameObject body = Shapes.MeshObject("Body", OwnedMeshes.Track(gameObject, b.ToMesh("Helicopter")), new[] { Mats.CarPaint(new Color(0.08f, 0.08f, 0.1f)), VehicleModels.GlassMaterial, Mats.Solid(new Color(0.9f, 0.9f, 0.92f), 0.5f), Mats.DarkMetal }, transform, true);
            body.layer = Layers.Vehicles;
            GameObject rotor = Shapes.MeshObject("Rotor", Shapes.CubeMesh, new[] { Mats.DarkMetal }, transform, true);
            rotor.transform.localPosition = new Vector3(0f, 2.7f, 0.2f);
            rotor.transform.localScale = new Vector3(10.5f, 0.05f, 0.35f);
            _rotor = rotor.transform;
            GameObject tail = Shapes.MeshObject("Tail Rotor", Shapes.CubeMesh, new[] { Mats.DarkMetal }, transform, true);
            tail.transform.localPosition = new Vector3(0.15f, 2.05f, -4.4f);
            tail.transform.localScale = new Vector3(0.04f, 1.6f, 0.15f);
            _tailRotor = tail.transform;
            BoxCollider col = gameObject.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 1.4f, -0.5f);
            col.size = new Vector3(2f, 2f, 6f);
            _rb = gameObject.AddComponent<Rigidbody>();
            _rb.isKinematic = true;
            _rb.mass = 3200f;
            _rb.interpolation = RigidbodyInterpolation.None;
            GameObject spot = new GameObject("Searchlight");
            spot.transform.SetParent(transform, false);
            spot.transform.localPosition = new Vector3(0f, 0.3f, 1.8f);
            _spot = spot.AddComponent<Light>();
            _spot.type = LightType.Spot;
            _spot.range = 140f;
            _spot.spotAngle = 22f;
            _spot.intensity = 6f;
            _spot.color = new Color(0.95f, 0.97f, 1f);
            _spot.shadows = LightShadows.None;
            if (Game.Audio != null) _loop = Game.Audio.CreateLoop(transform, SfxId.Helicopter, 1f, 260f);
        }

        public void Pursue()
        {
            _leaving = false;
        }

        public void Leave()
        {
            _leaving = true;
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            if (_rotor != null) _rotor.Rotate(0f, (IsDestroyed ? 500f : 1300f) * dt, 0f, Space.Self);
            if (_tailRotor != null) _tailRotor.Rotate(1800f * dt, 0f, 0f, Space.Self);
            if (IsDestroyed)
            {
                _smokeTimer -= dt;
                if (_smokeTimer <= 0f && Game.Fx != null)
                {
                    _smokeTimer = 0.05f;
                    Game.Fx.Smoke(transform.position + Vector3.up, Vector3.up * 2f, 2f, 0.1f);
                    if (DestroyedFor < 15f) Game.Fx.FirePuff(transform.position + Vector3.up * 1.2f, 1.3f);
                }
                return;
            }
            Vector3 p = Game.PlayerPosition;
            Vector3 target;
            if (_leaving)
            {
                target = transform.position + MathUtil.Flat(transform.position - p).normalized * 60f + Vector3.up * 20f;
                if (MathUtil.FlatDistance(transform.position, p) > 420f) { Destroy(gameObject); return; }
            }
            else
            {
                _orbit += dt * 0.25f;
                float radius = 38f;
                target = p + new Vector3(Mathf.Sin(_orbit) * radius, 0f, Mathf.Cos(_orbit) * radius);
                float ground = Ceiling(target);
                target.y = Mathf.Max(p.y + 38f, ground + 26f);
            }
            Vector3 to = target - transform.position;
            Vector3 desiredVel = Vector3.ClampMagnitude(to * 0.6f, _leaving ? 30f : 38f);
            Vector3 accel = Vector3.ClampMagnitude(desiredVel - _vel, 12f * dt * 1.5f);
            _vel += accel;
            Vector3 next = transform.position + _vel * dt;
            // Never sink into buildings.
            float c = Ceiling(next);
            if (next.y < c + 18f) next.y = Mathf.Lerp(next.y, c + 18f, dt * 3f);
            Vector3 face = MathUtil.Flat(p - next);
            if (face.sqrMagnitude < 1f) face = MathUtil.Flat(_vel);
            float yaw = face.sqrMagnitude > 0.01f ? MathUtil.YawOf(face) : transform.eulerAngles.y;
            Vector3 localAcc = Quaternion.Euler(0f, -yaw, 0f) * (accel / Mathf.Max(dt, 0.001f));
            Quaternion rot = Quaternion.Euler(Mathf.Clamp(localAcc.z * 1.5f, -18f, 18f), yaw, Mathf.Clamp(-localAcc.x * 1.5f, -20f, 20f));
            transform.SetPositionAndRotation(next, Quaternion.Slerp(transform.rotation, rot, MathUtil.Damp(2.5f, dt)));

            // Searchlight follows the suspect with a little lag; only at night.
            bool night = Game.Atmo != null && Game.Atmo.NightFactor > 0.35f;
            _spot.enabled = night && !_leaving;
            _spotTarget = Vector3.Lerp(_spotTarget == Vector3.zero ? p : _spotTarget, p + Random.insideUnitSphere * 1.5f, MathUtil.Damp(2.5f, dt));
            _spot.transform.rotation = Quaternion.LookRotation(_spotTarget - _spot.transform.position);

            if (_leaving) return;
            _sightTimer -= dt;
            if (_sightTimer <= 0f)
            {
                _sightTimer = 0.3f;
                if (CanSee(p) && Game.Wanted != null) Game.Wanted.ReportSightingAt(transform.position);
            }
            if (Armed)
            {
                _shotTimer -= dt;
                if (_shotTimer <= 0f)
                {
                    _shotTimer = Random.Range(1.4f, 2.6f);
                    Marksman(p);
                }
            }
        }

        private bool CanSee(Vector3 p)
        {
            if (MathUtil.FlatDistance(transform.position, p) > 160f) return false;
            // Hiding under a roof, bridge or underpass breaks line of sight.
            if (Physics.Raycast(p + Vector3.up * 2f, Vector3.up, 40f, Layers.WorldMask, QueryTriggerInteraction.Ignore)) return false;
            return !Physics.Linecast(transform.position + Vector3.down, p + Vector3.up, Layers.WorldMask, QueryTriggerInteraction.Ignore);
        }

        private void Marksman(Vector3 p)
        {
            if (Game.Player == null || !CanSee(p)) return;
            Vector3 muzzle = transform.position + transform.right * 1.2f + Vector3.up * 0.8f;
            Vector3 aim = Game.Player.ChestPosition + Game.Player.Velocity * 0.15f + Random.insideUnitSphere * (2.2f + Game.Player.Velocity.magnitude * 0.12f);
            Vector3 dir = (aim - muzzle).normalized;
            RaycastHit hit;
            if (Physics.Raycast(muzzle, dir, out hit, 250f, Layers.ShootMask, QueryTriggerInteraction.Ignore))
            {
                IDamageable d = hit.collider.GetComponentInParent<IDamageable>();
                if (d != null && d.IsAlive && hit.collider.GetComponentInParent<Helicopter>() != this)
                {
                    DamageInfo info = DamageInfo.Make(26f, hit.point, dir, gameObject, DamageType.Bullet);
                    info.Force = 2f;
                    d.TakeDamage(info);
                }
                if (Game.Fx != null)
                {
                    Game.Fx.Impact(hit.point, hit.normal, WeaponController.SurfaceOf(hit), true, hit.collider.transform);
                    Game.Fx.Tracer(muzzle, hit.point);
                }
            }
            if (Game.Audio != null) Game.Audio.PlayVar(SfxId.Sniper, muzzle, 0.9f, 0.05f, 300f);
            if (Game.Fx != null) Game.Fx.Muzzle(muzzle, dir, 0.6f);
        }

        private static float Ceiling(Vector3 p)
        {
            RaycastHit hit;
            if (Physics.Raycast(new Vector3(p.x, 400f, p.z), Vector3.down, out hit, 500f, Layers.WorldMask, QueryTriggerInteraction.Ignore)) return hit.point.y;
            return 0f;
        }

        public void TakeDamage(DamageInfo info)
        {
            if (IsDestroyed) return;
            float amount = info.Amount * (info.Type == DamageType.Explosion ? 3f : 1f);
            Health -= amount;
            if (Health <= 0f) Crash(info.Source);
        }

        private void Crash(GameObject source)
        {
            IsDestroyed = true;
            _destroyedTime = Time.time;
            _rb.isKinematic = false;
            _rb.interpolation = RigidbodyInterpolation.Interpolate;
            _rb.linearVelocity = _vel;
            _rb.angularVelocity = new Vector3(0f, 4f, 0.5f);
            _spot.enabled = false;
            if (Game.Explosions != null) Game.Explosions.Explode(transform.position, 6f, 120f, source, ExplosionKind.Vehicle);
            if (Game.IsPlayer(source))
            {
                if (Game.Stats != null) Game.Stats.Add(StatId.HelicoptersDowned, 1);
                if (Game.Wanted != null) Game.Wanted.ReportCrime(CrimeType.DestroyCopCar, transform.position, gameObject);
                if (Game.Hud != null) Game.Hud.Notify("Air unit down!", 2.5f);
            }
        }

        private void OnCollisionEnter(Collision c)
        {
            if (!IsDestroyed || _groundExploded) return;
            _groundExploded = true;
            if (Game.Explosions != null) Game.Explosions.Explode(transform.position, 9f, 260f, null, ExplosionKind.Vehicle);
            if (_loop != null) _loop.Stop();
        }

        private bool _groundExploded;
    }
}
