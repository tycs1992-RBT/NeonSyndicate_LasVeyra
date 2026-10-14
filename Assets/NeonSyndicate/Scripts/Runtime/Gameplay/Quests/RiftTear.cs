using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// A rift: a tall glowing tear in the air with a crystal core at its heart, ringed by orbiting shards. While the
    /// player is near it spits out Inner World creatures every few seconds (through a spawner its owner provides), up
    /// to a cap. Shooting, striking or blasting the core closes it. Standing near an open rift drives the colour
    /// grade's rift weight (PostGrade.Rift: violet fringing and a heavier vignette).
    /// </summary>
    public sealed class RiftTear : MonoBehaviour, IDamageable
    {
        /// <summary>Every rift that is still open (the grade weight is the strongest of them).</summary>
        public static readonly List<RiftTear> OpenRifts = new List<RiftTear>();

        public float Health = QuestDefaults.RiftCoreHealth;
        public float MaxHealth = QuestDefaults.RiftCoreHealth;
        /// <summary>Seconds between spawns while the player is in range.</summary>
        public float Interval = QuestDefaults.RiftInterval;
        public int MaxAlive = 4;
        /// <summary>The player must be this close for the rift to spawn anything.</summary>
        public float ActiveRange = 75f;
        /// <summary>Spawns one creature at a point (the owner tracks and cleans it up). Null: the rift spawns nothing.</summary>
        public Func<Vector3, CharacterBase> Spawner;
        /// <summary>Called once when the core breaks.</summary>
        public Action<RiftTear> OnClosed;

        public bool IsAlive { get { return Health > 0f; } }
        public bool Closed { get; private set; }
        public Vector3 CorePosition { get { return _core != null ? _core.position : transform.position + Vector3.up * 2.1f; } }

        private readonly List<CharacterBase> _spawned = new List<CharacterBase>();
        private readonly List<Transform> _shards = new List<Transform>();
        private Transform _tear;
        private Transform _core;
        private Light _light;
        private SphereCollider _coreCollider;
        private float _timer = 2f;
        private float _t;
        private float _collapse = -1f;
        private float _sparkTimer;

        public static void ResetStatics()
        {
            OpenRifts.Clear();
        }

        /// <summary>Builds a rift standing on the ground at a point, its face turned toward yaw.</summary>
        public static RiftTear Create(Vector3 ground, float yaw, float coreHealth, Transform parent)
        {
            GameObject go = new GameObject("Rift");
            go.transform.SetPositionAndRotation(ground, Quaternion.Euler(0f, yaw, 0f));
            if (parent != null) go.transform.SetParent(parent, true);
            RiftTear r = go.AddComponent<RiftTear>();
            r.MaxHealth = r.Health = coreHealth > 0f ? coreHealth : QuestDefaults.RiftCoreHealth;
            r.Build();
            return r;
        }

        private void Build()
        {
            GameObject tear = new GameObject("Tear");
            tear.transform.SetParent(transform, false);
            _tear = tear.transform;
            Mesh outer = OwnedMeshes.Track(gameObject, Almond(4.6f, 1.15f, 0.3f));
            Mesh inner = OwnedMeshes.Track(gameObject, Almond(4.0f, 0.5f, 0.6f));
            Shapes.MeshObject("Edge", outer, new[] { Mats.Emissive(new Color(0.72f, 0.12f, 1f), 3.2f) }, _tear, false);
            GameObject heart = Shapes.MeshObject("Heart", inner, new[] { Mats.Emissive(new Color(1f, 0.8f, 1f), 5f) }, _tear, false);
            heart.transform.localPosition = new Vector3(0f, 0.3f, 0f);
            GameObject core = new GameObject("Core");
            core.transform.SetParent(transform, false);
            core.transform.localPosition = new Vector3(0f, 2.1f, 0f);
            _core = core.transform;
            Shapes.Ball("Core Glow", _core, Vector3.zero, new Vector3(0.7f, 0.9f, 0.7f), Mats.Emissive(new Color(1f, 0.3f, 0.55f), 4.5f), false);
            _coreCollider = core.AddComponent<SphereCollider>();
            _coreCollider.radius = 0.6f;
            Material shardMat = Mats.Emissive(new Color(0.55f, 0.35f, 1f), 2.6f);
            for (int i = 0; i < 8; i++)
            {
                MeshBuilder b = new MeshBuilder(1);
                b.FixedUV = new Vector2(0.5f, 0.5f);
                b.Sphere(0, Vector3.zero, new Vector3(0.12f, 0.38f, 0.12f), 2, 4, Quaternion.identity);
                GameObject s = Shapes.MeshObject("Shard", OwnedMeshes.Track(gameObject, b.ToMesh("Rift Shard")), new[] { shardMat }, transform, false);
                _shards.Add(s.transform);
            }
            GameObject lg = new GameObject("Rift Light");
            lg.transform.SetParent(transform, false);
            lg.transform.localPosition = new Vector3(0f, 2.3f, 0.8f);
            _light = lg.AddComponent<Light>();
            _light.type = LightType.Point;
            _light.color = new Color(0.7f, 0.25f, 1f);
            _light.range = 20f;
            _light.intensity = 3f;
            _light.shadows = LightShadows.None;
        }

        /// <summary>A vertical lens shape (double sided), height h, half width w at the middle, standing at height y0.</summary>
        private static Mesh Almond(float h, float w, float y0)
        {
            MeshBuilder b = new MeshBuilder(1);
            b.FixedUV = new Vector2(0.5f, 0.5f);
            const int n = 14;
            for (int i = 0; i < n; i++)
            {
                float t0 = i / (float)n, t1 = (i + 1) / (float)n;
                float w0 = w * Mathf.Pow(Mathf.Sin(t0 * Mathf.PI), 0.8f), w1 = w * Mathf.Pow(Mathf.Sin(t1 * Mathf.PI), 0.8f);
                float y0a = y0 + t0 * h, y1a = y0 + t1 * h;
                // A slight S-curve makes it read as a tear rather than a cut-out.
                float s0 = Mathf.Sin(t0 * Mathf.PI * 2f) * 0.12f, s1 = Mathf.Sin(t1 * Mathf.PI * 2f) * 0.12f;
                Vector3 a = new Vector3(-w0 + s0, y0a, 0f), bR = new Vector3(w0 + s0, y0a, 0f);
                Vector3 c = new Vector3(w1 + s1, y1a, 0f), d = new Vector3(-w1 + s1, y1a, 0f);
                Vector2 uv = new Vector2(0.5f, 0.5f);
                b.Quad(0, a, d, c, bR, Vector3.back, uv, uv, uv, uv);
                b.Quad(0, bR, c, d, a, Vector3.forward, uv, uv, uv, uv);
            }
            return b.ToMesh("Rift Tear");
        }

        private void OnEnable()
        {
            if (!Closed && !OpenRifts.Contains(this)) OpenRifts.Add(this);
        }

        private void OnDisable()
        {
            OpenRifts.Remove(this);
            if (OpenRifts.Count == 0) PostGrade.Rift = 0f;
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            _t += dt;
            if (_collapse >= 0f)
            {
                _collapse += dt;
                float k = Mathf.Clamp01(1f - _collapse / 0.7f);
                if (_tear != null) _tear.localScale = new Vector3(k, Mathf.Lerp(0.2f, 1f, k), 1f);
                for (int i = 0; i < _shards.Count; i++) if (_shards[i] != null) _shards[i].localScale = Vector3.one * k;
                if (_light != null) _light.intensity = 6f * k;
                if (k <= 0f) gameObject.SetActive(false);
                return;
            }
            float pulse = 1f + Mathf.Sin(_t * 2.6f) * 0.05f + Mathf.Sin(_t * 7.1f) * 0.02f;
            if (_tear != null) _tear.localScale = new Vector3(pulse, 1f, 1f);
            if (_core != null) _core.localRotation = Quaternion.Euler(0f, _t * 90f, Mathf.Sin(_t * 1.7f) * 12f);
            for (int i = 0; i < _shards.Count; i++)
            {
                if (_shards[i] == null) continue;
                float a = _t * (0.6f + (i % 3) * 0.15f) + i * Mathf.PI * 2f / _shards.Count;
                float rr = 1.6f + Mathf.Sin(_t + i) * 0.25f;
                _shards[i].localPosition = new Vector3(Mathf.Cos(a) * rr, 2.2f + Mathf.Sin(a * 2f + i) * 1.1f, Mathf.Sin(a) * rr * 0.6f);
                _shards[i].localRotation = Quaternion.Euler(a * 57f, a * 90f, 0f);
            }
            if (_light != null) _light.intensity = 2.6f + Mathf.Sin(_t * 3.3f) * 0.7f + (MaxHealth > 0f ? (1f - Health / MaxHealth) * 2f : 0f);
            _sparkTimer -= dt;
            if (_sparkTimer <= 0f && Game.Fx != null)
            {
                _sparkTimer = 0.35f + UnityEngine.Random.value * 0.5f;
                Game.Fx.Sparks(CorePosition + UnityEngine.Random.insideUnitSphere * 0.6f, UnityEngine.Random.insideUnitSphere * 2f, 3);
            }
            TickSpawns(dt);
        }

        private void TickSpawns(float dt)
        {
            if (Spawner == null || Game.Player == null || !Game.IsPlaying) return;
            for (int i = _spawned.Count - 1; i >= 0; i--) if (QuestWorld.Gone(_spawned[i])) _spawned.RemoveAt(i);
            if (MathUtil.FlatDistance(Game.PlayerPosition, transform.position) > ActiveRange) return;
            _timer -= dt;
            if (_timer > 0f || _spawned.Count >= MaxAlive) return;
            _timer = Interval;
            float side = UnityEngine.Random.value < 0.5f ? -1f : 1f;
            Vector3 p = transform.position + transform.forward * UnityEngine.Random.Range(2.5f, 5f) + transform.right * side * UnityEngine.Random.Range(0.5f, 3f);
            CharacterBase c = null;
            try { c = Spawner(p); }
            catch (Exception e) { Log.Exception("Rift spawn", e); }
            if (c == null) return;
            _spawned.Add(c);
            if (Game.Fx != null) Game.Fx.Flash(p + Vector3.up, new Color(0.75f, 0.2f, 1f), 4f, 14f, 0.4f);
            if (Game.Audio != null) Game.Audio.PlayAt(SfxId.Whoosh, p, 0.7f, 0.6f, 80f);
        }

        private void LateUpdate()
        {
            // One open rift writes the grade weight for all of them: the strongest pull wins.
            if (OpenRifts.Count == 0 || OpenRifts[0] != this) return;
            float best = 0f;
            Vector3 pp = Game.PlayerPosition;
            for (int i = 0; i < OpenRifts.Count; i++)
            {
                RiftTear r = OpenRifts[i];
                if (r == null || r.Closed) continue;
                float d = MathUtil.FlatDistance(pp, r.transform.position);
                float w = Mathf.Clamp01(1f - (d - 6f) / 32f) * (0.75f + 0.25f * Mathf.Sin(_t * 3f));
                if (w > best) best = w;
            }
            PostGrade.Rift = best;
        }

        public void TakeDamage(DamageInfo info)
        {
            if (!IsAlive || Closed) return;
            float mult = info.Type == DamageType.Explosion || info.Type == DamageType.Power ? 1.5f : 1f;
            Health -= info.Amount * mult;
            if (Game.Fx != null) Game.Fx.Sparks(info.Point, Vector3.up * 2f, 6);
            if (Health > 0f)
            {
                if (Game.Hud != null && info.FromPlayer) Game.Hud.HitMarker(false);
                return;
            }
            Close();
        }

        /// <summary>Breaks the core: a flash and a chime, the tear collapses, and the owner hears about it.</summary>
        public void Close()
        {
            if (Closed) return;
            Closed = true;
            Health = 0f;
            OpenRifts.Remove(this);
            if (OpenRifts.Count == 0) PostGrade.Rift = 0f;
            if (_coreCollider != null) _coreCollider.enabled = false;
            if (_core != null) _core.gameObject.SetActive(false);
            Vector3 p = CorePosition;
            if (Game.Fx != null) Game.Fx.Flash(p, new Color(0.85f, 0.4f, 1f), 9f, 30f, 0.9f);
            if (Game.Audio != null)
            {
                Game.Audio.PlayAt(SfxId.GlassBreak, p, 1f, 0.5f, 140f);
                Game.Audio.PlayAt(SfxId.CrystalChime, p, 0.9f, 0.7f, 140f);
            }
            if (Game.Cam != null && MathUtil.FlatDistance(Game.PlayerPosition, p) < 40f) Game.Cam.Shake(0.35f);
            if (Game.Hud != null) Game.Hud.HitMarker(true);
            _collapse = 0f;
            if (OnClosed != null)
            {
                try { OnClosed(this); }
                catch (Exception e) { Log.Exception("Rift closed callback", e); }
            }
        }
    }
}
