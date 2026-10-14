using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// The small auto turret an Engineer drops: a procedural tripod with a turning head, a red sensor eye and a short
    /// barrel. It unfolds, tracks the player inside 30 m with line of sight and fires short bursts, then folds away after
    /// its battery runs out. Shooting it breaks it with sparks and a pop. Owned by CoverDirector (squad limits).
    /// </summary>
    public sealed class TacticalTurret : MonoBehaviour, IDamageable
    {
        public const float Range = 30f;
        public const float Lifetime = 34f;

        private Transform _head;
        private Transform _muzzle;
        private Light _eye;
        private GameObject _owner;
        private float _health = 90f;
        private float _life;
        private float _unfold;
        private float _burstLeft;
        private float _shotTimer;
        private float _pause;
        private float _yaw;
        private float _seeTimer;
        private bool _sees;
        private bool _dead;
        private readonly RaycastHit[] _hits = new RaycastHit[8];

        public bool IsAlive { get { return !_dead && _health > 0f; } }

        /// <summary>Builds a turret on the ground at pos facing yaw, owned by the Engineer who dropped it.</summary>
        public static TacticalTurret Deploy(Vector3 pos, float yaw, NpcController owner)
        {
            GameObject go = new GameObject("Auto Turret");
            go.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, yaw, 0f));
            go.layer = Layers.Debris;
            TacticalTurret t = go.AddComponent<TacticalTurret>();
            t._owner = owner != null ? owner.gameObject : null;
            t._yaw = yaw;
            t.Build();
            if (Settings.Difficulty == DifficultyLevel.Veteran) t._health = 120f;
            else if (Settings.Difficulty == DifficultyLevel.Story) t._health = 60f;
            if (Game.Audio != null) Game.Audio.PlayAt(SfxId.MetalHit, pos + Vector3.up * 0.4f, 0.6f, 1.3f, 40f);
            return t;
        }

        private void Build()
        {
            Transform root = transform;
            Material dark = Mats.DarkMetal;
            Material metal = Mats.Metal;
            for (int i = 0; i < 3; i++)
            {
                float a = i * 120f * Mathf.Deg2Rad;
                Vector3 foot = new Vector3(Mathf.Sin(a) * 0.32f, 0.16f, Mathf.Cos(a) * 0.32f);
                GameObject leg = Shapes.Box("Leg " + i, root, foot * 0.5f + new Vector3(0f, 0.12f, 0f), new Vector3(0.04f, 0.46f, 0.04f), dark, false);
                leg.transform.localRotation = Quaternion.LookRotation(new Vector3(foot.x, 0f, foot.z).normalized, Vector3.up) * Quaternion.Euler(-35f, 0f, 0f);
            }
            Shapes.Cyl("Mount", root, new Vector3(0f, 0.38f, 0f), new Vector3(0.16f, 0.1f, 0.16f), dark, false);
            GameObject head = new GameObject("Head");
            head.transform.SetParent(root, false);
            head.transform.localPosition = new Vector3(0f, 0.5f, 0f);
            _head = head.transform;
            Shapes.Box("Body", _head, new Vector3(0f, 0.05f, 0f), new Vector3(0.3f, 0.2f, 0.36f), metal, false);
            Shapes.Box("Plate", _head, new Vector3(0f, 0.17f, -0.02f), new Vector3(0.26f, 0.04f, 0.3f), dark, false);
            GameObject barrel = Shapes.Cyl("Barrel", _head, new Vector3(0.06f, 0.04f, 0.32f), new Vector3(0.05f, 0.16f, 0.05f), dark, false);
            barrel.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            Shapes.Ball("Eye", _head, new Vector3(-0.07f, 0.07f, 0.18f), new Vector3(0.07f, 0.07f, 0.04f), Mats.Glow(new Color(1f, 0.1f, 0.06f)), false);
            GameObject m = new GameObject("Muzzle");
            m.transform.SetParent(_head, false);
            m.transform.localPosition = new Vector3(0.06f, 0.04f, 0.5f);
            _muzzle = m.transform;
            GameObject eye = new GameObject("Eye Light");
            eye.transform.SetParent(_head, false);
            eye.transform.localPosition = new Vector3(-0.07f, 0.07f, 0.24f);
            _eye = eye.AddComponent<Light>();
            _eye.type = LightType.Point;
            _eye.range = 3f;
            _eye.intensity = 1.6f;
            _eye.color = new Color(1f, 0.12f, 0.08f);
            _eye.shadows = LightShadows.None;
            BoxCollider c = gameObject.AddComponent<BoxCollider>();
            c.center = new Vector3(0f, 0.42f, 0f);
            c.size = new Vector3(0.5f, 0.75f, 0.55f);
            Layers.SetRecursive(gameObject, Layers.Debris);
            _head.localScale = new Vector3(1f, 0.05f, 1f);
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f || _dead) return;
            _life += dt;
            _unfold = Mathf.MoveTowards(_unfold, _life < Lifetime ? 1f : 0f, dt * 2.5f);
            _head.localScale = new Vector3(1f, Mathf.Lerp(0.05f, 1f, _unfold), 1f);
            if (_life >= Lifetime + 0.5f)
            {
                Destroy(gameObject);
                return;
            }
            PlayerController p = Game.Player;
            if (p == null || p.IsDead || _unfold < 0.95f || !Game.IsPlaying)
            {
                if (_eye != null) _eye.intensity = 0.6f;
                return;
            }
            Vector3 target = p.InVehicle ? p.CurrentVehicle.Center : p.ChestPosition;
            Vector3 eye = _muzzle.position;
            Vector3 to = target - eye;
            float dist = to.magnitude;
            _seeTimer -= dt;
            if (_seeTimer <= 0f)
            {
                _seeTimer = 0.25f;
                _sees = dist < Range && !Physics.Linecast(eye, target, Layers.WorldMask, QueryTriggerInteraction.Ignore);
            }
            if (_eye != null) _eye.intensity = _sees ? 2.2f + Mathf.Sin(Time.time * 20f) * 0.6f : 1f;
            if (!_sees) return;
            CombatDirector.MarkCombat();
            // Turn the head toward the target (a limited turn rate, so strafing around it works).
            float wantYaw = MathUtil.YawOf(MathUtil.Flat(to));
            _yaw = Mathf.MoveTowardsAngle(_yaw, wantYaw, 110f * dt);
            float pitch = -Mathf.Asin(Mathf.Clamp(to.y / Mathf.Max(0.01f, dist), -0.6f, 0.6f)) * Mathf.Rad2Deg;
            _head.rotation = Quaternion.Euler(pitch, _yaw, 0f);
            if (Mathf.Abs(Mathf.DeltaAngle(_yaw, wantYaw)) > 12f) return;
            if (_pause > 0f)
            {
                _pause -= dt;
                return;
            }
            if (_burstLeft <= 0f) _burstLeft = Random.Range(4, 8);
            _shotTimer -= dt;
            if (_shotTimer > 0f) return;
            _shotTimer = 0.13f;
            Fire(target, dist);
            _burstLeft -= 1f;
            if (_burstLeft <= 0f) _pause = Random.Range(1f, 1.8f) * (Settings.Difficulty == DifficultyLevel.Story ? 1.5f : 1f);
        }

        private void Fire(Vector3 target, float dist)
        {
            Vector3 from = _muzzle.position;
            float spread = (2.6f + dist * 0.05f) * CombatDirector.EnemySpread * Abilities.EnemyAccuracyScale;
            Vector3 dir = (target - from).normalized;
            Vector2 r = Random.insideUnitCircle * Mathf.Tan(spread * Mathf.Deg2Rad);
            Vector3 right = Vector3.Cross(Vector3.up, dir).normalized;
            Vector3 up = Vector3.Cross(dir, right);
            dir = (dir + right * r.x + up * r.y).normalized;
            int n = Physics.RaycastNonAlloc(from, dir, _hits, Range + 10f, Layers.ShootMask, QueryTriggerInteraction.Ignore);
            float best = float.MaxValue;
            int bi = -1;
            for (int i = 0; i < n; i++)
            {
                if (_hits[i].collider == null || _hits[i].collider.transform.IsChildOf(transform)) continue;
                if (_hits[i].distance < best)
                {
                    best = _hits[i].distance;
                    bi = i;
                }
            }
            Vector3 end = from + dir * (Range + 10f);
            if (bi >= 0)
            {
                RaycastHit h = _hits[bi];
                end = h.point;
                IDamageable d = h.collider.GetComponentInParent<IDamageable>();
                if (d != null && d.IsAlive)
                {
                    DamageInfo info = DamageInfo.Make(7f, h.point, dir, _owner != null ? _owner : gameObject, DamageType.Bullet);
                    info.Force = 1f;
                    d.TakeDamage(info);
                }
                if (Game.Fx != null) Game.Fx.Impact(h.point, h.normal, WeaponController.SurfaceOf(h), Random.value < 0.3f, h.collider.transform);
            }
            if (Game.Fx != null)
            {
                Game.Fx.Tracer(from, end);
                Game.Fx.Muzzle(from, dir, 0.45f);
            }
            if (Game.Audio != null) Game.Audio.PlayVar(SfxId.SMG, from, 0.55f, 0.08f, 120f);
            Threats.Report(from, 40f, gameObject, ThreatKind.Gunshot);
        }

        public void TakeDamage(DamageInfo info)
        {
            if (_dead) return;
            if (info.Type == DamageType.Fall) return;
            _health -= info.Amount;
            if (Game.Fx != null) Game.Fx.Sparks(info.Point, -info.Direction * 2f, 6);
            if (info.FromPlayer && Game.Hud != null) Game.Hud.HitMarker(_health <= 0f);
            if (_health <= 0f) Break();
        }

        private void Break()
        {
            _dead = true;
            Vector3 p = transform.position + Vector3.up * 0.5f;
            if (Game.Fx != null)
            {
                Game.Fx.Sparks(p, Vector3.up * 3f, 24);
                Game.Fx.Flash(p, new Color(1f, 0.6f, 0.3f), 3f, 5f, 0.2f);
                Game.Fx.Smoke(p, Vector3.up * 1.5f, 1f, 0.7f);
            }
            if (Game.Audio != null) Game.Audio.PlayAt(SfxId.ExplosionSmall, p, 0.7f, 1.3f, 80f);
            Destroy(gameObject, 0.05f);
        }
    }
}
