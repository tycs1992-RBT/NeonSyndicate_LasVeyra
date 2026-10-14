using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace NeonSyndicate
{
    /// <summary>
    /// The ground ring that marks an area attack: an outline appears where the blow will land and a disc fills it over
    /// the wind-up, so the player sees exactly where not to stand and how long is left. It flashes when it bursts.
    /// Pooled flat additive quads, no colliders.
    /// </summary>
    public sealed class GroundRing : MonoBehaviour
    {
        private static readonly List<GroundRing> _free = new List<GroundRing>();
        private static Transform _root;
        private static Material _ringMat, _fillMat;
        private static readonly int TintId = Shader.PropertyToID("_TintColor");
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        private Transform _ring, _fill;
        private MeshRenderer _ringR, _fillR;
        private MaterialPropertyBlock _mpb;
        private float _t, _fillTime, _radius, _burstT;
        private Color _color;
        private int _state;

        public static void ResetStatics()
        {
            _free.Clear();
            _root = null;
            _ringMat = null;
            _fillMat = null;
        }

        /// <summary>Shows a ring of radius at center (ground point) that fills over fillSeconds.</summary>
        public static GroundRing Show(Vector3 center, float radius, float fillSeconds, Color color)
        {
            GroundRing r = null;
            while (_free.Count > 0 && r == null)
            {
                r = _free[_free.Count - 1];
                _free.RemoveAt(_free.Count - 1);
            }
            if (r == null) r = Create();
            r.gameObject.SetActive(true);
            r.transform.position = center + Vector3.up * 0.07f;
            r._radius = Mathf.Max(0.3f, radius);
            r._fillTime = Mathf.Max(0.05f, fillSeconds);
            r._color = color;
            r._t = 0f;
            r._state = 0;
            r.Apply(0f, 1f);
            return r;
        }

        private static GroundRing Create()
        {
            if (_root == null) _root = new GameObject("Creature Telegraphs").transform;
            if (_ringMat == null) _ringMat = Mats.Additive("Telegraph Ring", Color.white, UIKit.Ring);
            if (_fillMat == null) _fillMat = Mats.Additive("Telegraph Fill", Color.white, UIKit.Circle);
            GameObject go = new GameObject("Ground Ring");
            go.transform.SetParent(_root, false);
            GroundRing r = go.AddComponent<GroundRing>();
            GameObject ring = Shapes.MeshObject("Ring", Shapes.QuadMesh, new[] { _ringMat }, go.transform, false);
            ring.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            GameObject fill = Shapes.MeshObject("Fill", Shapes.QuadMesh, new[] { _fillMat }, go.transform, false);
            fill.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            fill.transform.localPosition = Vector3.up * 0.01f;
            r._ring = ring.transform;
            r._fill = fill.transform;
            r._ringR = ring.GetComponent<MeshRenderer>();
            r._fillR = fill.GetComponent<MeshRenderer>();
            r._ringR.receiveShadows = false;
            r._fillR.receiveShadows = false;
            r._ringR.shadowCastingMode = ShadowCastingMode.Off;
            r._fillR.shadowCastingMode = ShadowCastingMode.Off;
            r._mpb = new MaterialPropertyBlock();
            return r;
        }

        /// <summary>The blow lands: a bright flash, then the ring fades away.</summary>
        public void Burst()
        {
            if (_state != 0) return;
            _state = 1;
            _burstT = 0f;
        }

        /// <summary>Hides the ring at once (the attack was cancelled).</summary>
        public void Release()
        {
            if (_state == 2) return;
            _state = 2;
            if (this != null)
            {
                gameObject.SetActive(false);
                if (!_free.Contains(this)) _free.Add(this);
            }
        }

        private void Apply(float fill, float alpha)
        {
            float d = _radius * 2f;
            _ring.localScale = new Vector3(d, d, 1f);
            float f = Mathf.Max(0.02f, fill) * d;
            _fill.localScale = new Vector3(f, f, 1f);
            SetTint(_ringR, _color * (0.9f * alpha));
            SetTint(_fillR, _color * (0.35f * alpha * (0.6f + 0.4f * fill)));
        }

        private void SetTint(MeshRenderer r, Color c)
        {
            c.a = Mathf.Clamp01(c.a);
            _mpb.Clear();
            _mpb.SetColor(TintId, c);
            _mpb.SetColor(BaseColorId, c);
            _mpb.SetColor(ColorId, c);
            r.SetPropertyBlock(_mpb);
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            if (_state == 0)
            {
                _t += dt;
                float k = Mathf.Clamp01(_t / _fillTime);
                float pulse = k > 0.75f ? 0.75f + 0.25f * Mathf.Sin(Time.time * 30f) : 1f;
                Apply(k, pulse);
                // Left alone (its creature died mid wind-up): fade out on its own.
                if (_t > _fillTime + 2f) Release();
            }
            else if (_state == 1)
            {
                _burstT += dt;
                float k = Mathf.Clamp01(_burstT / 0.35f);
                float d = _radius * 2f * (1f + 0.15f * k);
                _ring.localScale = new Vector3(d, d, 1f);
                _fill.localScale = new Vector3(d, d, 1f);
                SetTint(_ringR, _color * (1.6f * (1f - k)));
                SetTint(_fillR, new Color(1f, 0.95f, 0.85f, 1f) * (0.8f * (1f - k)));
                if (k >= 1f) Release();
            }
        }
    }

    /// <summary>
    /// Thrown and spat creature projectiles (the wyrm's ember spit, golem shards and packed sand): pooled glowing balls
    /// that arc under gravity and burst on the ground, a wall, a car, a person or the player (creatures, the thrower
    /// among them, let them pass), and can be dodged or blocked like any ranged hit.
    /// </summary>
    public sealed class CreatureShot : MonoBehaviour
    {
        private static readonly List<CreatureShot> _free = new List<CreatureShot>();
        private static Transform _root;
        // One glowing material per shot colour for the session (no string key or closure per throw).
        private static readonly Dictionary<int, Material> _mats = new Dictionary<int, Material>();
        private static readonly RaycastHit[] _rayHits = new RaycastHit[8];

        /// <summary>What a shot stops on: the world, cars and characters.</summary>
        private static int ShotMask { get { return Layers.WorldMask | (1 << Layers.Vehicles) | (1 << Layers.Characters); } }

        private Vector3 _vel;
        private float _gravity;
        private float _damage;
        private float _radius;
        private float _splash;
        private float _life;
        private DamageType _type;
        private Element _element;
        private DamageFlags _flags;
        private Color _color;
        private GameObject _source;
        private MeshRenderer _renderer;
        private float _trail;

        public static void ResetStatics()
        {
            _free.Clear();
            _root = null;
            _mats.Clear();
        }

        /// <summary>The glowing material for a shot colour (made once per colour, then reused).</summary>
        private static Material MatFor(Color color)
        {
            Color32 c = new Color(color.r, color.g, color.b, 1f);
            int key = (c.r << 16) | (c.g << 8) | c.b;
            Material m;
            if (_mats.TryGetValue(key, out m) && m != null) return m;
            m = Mats.Emissive(new Color(color.r, color.g, color.b, 1f), 2.5f);
            _mats[key] = m;
            return m;
        }

        public static void Fire(Vector3 from, Vector3 velocity, float gravity, float damage, float radius, DamageType type, Element element, Color color, GameObject source, DamageFlags flags, float splash)
        {
            CreatureShot s = null;
            while (_free.Count > 0 && s == null)
            {
                s = _free[_free.Count - 1];
                _free.RemoveAt(_free.Count - 1);
            }
            if (s == null)
            {
                if (_root == null) _root = new GameObject("Creature Shots").transform;
                GameObject go = Shapes.MeshObject("Creature Shot", Shapes.SphereMesh, new[] { Mats.Emissive(Color.white, 2f) }, _root, false);
                go.layer = Layers.IgnoreRaycast;
                s = go.AddComponent<CreatureShot>();
                s._renderer = go.GetComponent<MeshRenderer>();
            }
            s.gameObject.SetActive(true);
            s.transform.position = from;
            s.transform.localScale = Vector3.one * Mathf.Max(0.15f, radius * 1.6f);
            s._vel = velocity;
            s._gravity = gravity;
            s._damage = damage;
            s._radius = Mathf.Max(0.1f, radius);
            s._splash = splash;
            s._type = type;
            s._element = element;
            s._flags = flags;
            s._color = color;
            s._source = source;
            s._life = 4f;
            s._renderer.sharedMaterial = MatFor(color);
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            _life -= dt;
            Vector3 p0 = transform.position;
            _vel += Vector3.down * _gravity * dt;
            Vector3 p1 = p0 + _vel * dt;
            if (_life <= 0f) { Burst(p1, false); return; }
            PlayerController p = Game.Player;
            if (p != null && !p.IsDead && HitsPlayer(p, p0, p1))
            {
                Hit(p);
                Burst(p1, true);
                return;
            }
            if (Stopped(p0, p1)) return;
            transform.position = p1;
            _trail -= dt;
            if (_trail <= 0f && Game.Fx != null)
            {
                _trail = 0.04f;
                Game.Fx.BreathPuff(p1, -_vel * 0.05f, _color, 0.5f, _element == Element.Ember);
            }
        }

        private bool HitsPlayer(PlayerController p, Vector3 a, Vector3 b)
        {
            if (p.InVehicle)
            {
                Vector3 c = p.CurrentVehicle.transform.position + Vector3.up;
                return (b - c).sqrMagnitude < 2.4f * 2.4f;
            }
            Vector3 lo = p.transform.position + Vector3.up * 0.35f, hi = p.transform.position + Vector3.up * 1.6f;
            float r = 0.4f + _radius;
            Vector3 mid = (a + b) * 0.5f;
            float t;
            return (MathUtil.ClosestPointOnSegment(lo, hi, b, out t) - b).sqrMagnitude < r * r || (MathUtil.ClosestPointOnSegment(lo, hi, mid, out t) - mid).sqrMagnitude < r * r;
        }

        /// <summary>
        /// This frame's flight from a to b meets something: the nearest wall, car or character that is not a creature.
        /// A person is hit like the player; a car takes the blow on its body. True when the shot burst.
        /// </summary>
        private bool Stopped(Vector3 a, Vector3 b)
        {
            Vector3 seg = b - a;
            float len = seg.magnitude;
            if (len < 1e-5f) return false;
            int n = Physics.RaycastNonAlloc(a, seg / len, _rayHits, len, ShotMask, QueryTriggerInteraction.Ignore);
            int best = -1;
            float bd = float.MaxValue;
            CharacterBase body = null;
            for (int i = 0; i < n; i++)
            {
                RaycastHit h = _rayHits[i];
                if (h.collider == null || h.distance >= bd) continue;
                CharacterBase c = h.collider.GetComponentInParent<CharacterBase>();
                if (c != null && (c.IsDead || c.gameObject == _source || (c.Team != null && c.Team.Team == Team.Creature))) continue;
                best = i;
                bd = h.distance;
                body = c;
            }
            if (best < 0) return false;
            RaycastHit hit = _rayHits[best];
            if (body != null)
            {
                PlayerController p = body as PlayerController;
                if (p != null) Hit(p);
                else HitBody(body, hit.point);
            }
            else
            {
                Vehicle v = hit.collider.GetComponentInParent<Vehicle>();
                if (v != null && !v.IsDestroyed) v.ApplyDamage(_damage * 2f, _source);
            }
            Burst(hit.point, body != null && body.IsPlayer);
            return true;
        }

        /// <summary>A person (or a companion) struck by the shot.</summary>
        private void HitBody(CharacterBase c, Vector3 at)
        {
            if (c.InVehicle && c.CurrentVehicle != null)
            {
                c.CurrentVehicle.ApplyDamage(_damage * 2f, _source);
                return;
            }
            Vector3 dir = _vel.sqrMagnitude > 0.01f ? _vel.normalized : Vector3.forward;
            DamageInfo info = DamageInfo.Make(_damage, at, dir, _source, _type);
            info.Force = 3f;
            info.Flags = _flags;
            c.Health.TakeDamage(info);
            if (!c.Health.LastBlocked && _element == Element.Ember) c.Health.Burning = Mathf.Max(c.Health.Burning, 1f);
        }

        private void Hit(PlayerController p)
        {
            if (p.InVehicle)
            {
                p.CurrentVehicle.ApplyDamage(_damage * 2f, _source);
                return;
            }
            Vector3 dir = _vel.sqrMagnitude > 0.01f ? _vel.normalized : Vector3.forward;
            DamageInfo info = DamageInfo.Make(_damage, p.ChestPosition, dir, _source, _type);
            info.Force = 3f;
            info.Flags = _flags;
            p.Health.TakeDamage(info);
            if (!p.Health.LastBlocked && _element == Element.Ember) p.Health.Burning = Mathf.Max(p.Health.Burning, 1f);
        }

        private void Burst(Vector3 at, bool direct)
        {
            if (Game.Fx != null)
            {
                Game.Fx.CrystalBurst(at, _color, 14, 1f);
                if (_element == Element.Ember) for (int i = 0; i < 4; i++) Game.Fx.FirePuff(at + Random.insideUnitSphere * 0.5f, 0.7f);
                else Game.Fx.GroundDust(at, 0.6f, 4);
            }
            if (Game.Audio != null) Game.Audio.PlayVar(SfxId.ExplosionSmall, at, 0.35f, 0.2f, 40f);
            // Near misses on the ground still splash the player a little (not through a wall).
            PlayerController p = Game.Player;
            if (!direct && p != null && !p.IsDead && _splash > 0f && !p.InVehicle)
            {
                float d = Vector3.Distance(p.transform.position + Vector3.up * 0.8f, at);
                if (d < _splash && !Physics.Linecast(at + Vector3.up * 0.3f, p.ChestPosition, Layers.WorldMask, QueryTriggerInteraction.Ignore))
                {
                    DamageInfo info = DamageInfo.Make(_damage * 0.4f * (1f - d / _splash), p.ChestPosition, p.transform.position - at, _source, _type);
                    info.Force = 2f;
                    p.Health.TakeDamage(info);
                }
            }
            gameObject.SetActive(false);
            if (!_free.Contains(this)) _free.Add(this);
        }
    }
}
