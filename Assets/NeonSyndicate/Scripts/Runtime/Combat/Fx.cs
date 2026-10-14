using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace NeonSyndicate
{
    public enum ImpactSurface
    {
        Concrete,
        Metal,
        Flesh,
        Glass,
        Dirt,
        Water,
        Wood,
        /// <summary>Crystal creatures and golems: sparks in the creature's glow colour (never blood).</summary>
        Crystal,
        /// <summary>Shadow creatures (Hollows, shades): puffs of dark smoke with violet sparks.</summary>
        Shadow
    }

    /// <summary>
    /// Pooled visual effects: impacts, blood, tracers, muzzle flashes, decals, explosions, smoke, fire and water.
    /// Particles are emitted from a handful of shared world-space systems so effects cost almost nothing.
    /// </summary>
    public sealed partial class Fx : MonoBehaviour
    {
        private ParticleSystem _sparks;
        private ParticleSystem _dust;
        private ParticleSystem _blood;
        private ParticleSystem _smoke;
        private ParticleSystem _fire;
        private ParticleSystem _debris;
        private ParticleSystem _water;
        private readonly List<LineRenderer> _tracers = new List<LineRenderer>();
        private readonly List<float> _tracerLife = new List<float>();
        private int _tracerNext;
        private readonly List<GameObject> _muzzles = new List<GameObject>();
        private readonly List<float> _muzzleLife = new List<float>();
        private int _muzzleNext;
        private readonly List<GameObject> _decals = new List<GameObject>();
        private int _decalNext;
        private readonly List<Light> _flashes = new List<Light>();
        private readonly List<float> _flashLife = new List<float>();
        private readonly List<float> _flashMax = new List<float>();
        private readonly List<float> _flashIntensity = new List<float>();
        private Mesh _starMesh;
        private const int MaxDecals = 140;

        public void Init()
        {
            _sparks = Emitter("Sparks", Mats.Spark, 600, 1.2f, true);
            _dust = Emitter("Dust", Mats.Dust, 400, 0.05f, false);
            _blood = Emitter("Blood", Mats.BloodPuff, 400, 0.9f, false);
            _smoke = Emitter("Smoke", Mats.Smoke, 600, -0.15f, false);
            _fire = Emitter("Fire", Mats.Fire, 500, -0.3f, false);
            _debris = Emitter("Debris", Mats.Solid(new Color(0.12f, 0.12f, 0.12f), 0.2f), 300, 1.6f, false);
            _water = Emitter("Water", Mats.WaterSpray, 800, 1f, false);
            ParticleSystemRenderer sr = _sparks.GetComponent<ParticleSystemRenderer>();
            sr.renderMode = ParticleSystemRenderMode.Stretch;
            sr.velocityScale = 0.05f;
            sr.lengthScale = 1.5f;
            _starMesh = BuildStar();
            for (int i = 0; i < 28; i++) _tracers.Add(NewTracer(i));
            for (int i = 0; i < 28; i++) _tracerLife.Add(0f);
            for (int i = 0; i < 14; i++)
            {
                _muzzles.Add(NewMuzzle(i));
                _muzzleLife.Add(0f);
            }
            for (int i = 0; i < 8; i++)
            {
                GameObject go = new GameObject("Flash " + i);
                go.transform.SetParent(transform, false);
                Light l = go.AddComponent<Light>();
                l.type = LightType.Point;
                l.enabled = false;
                l.shadows = LightShadows.None;
                _flashes.Add(l);
                _flashLife.Add(0f);
                _flashMax.Add(1f);
                _flashIntensity.Add(0f);
            }
        }

        private ParticleSystem Emitter(string name, Material mat, int max, float gravity, bool additive)
        {
            GameObject go = new GameObject("FX " + name);
            go.transform.SetParent(transform, false);
            ParticleSystem ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ParticleSystem.MainModule m = ps.main;
            m.loop = false;
            m.playOnAwake = false;
            m.maxParticles = max;
            m.simulationSpace = ParticleSystemSimulationSpace.World;
            m.gravityModifier = gravity;
            m.startLifetime = 1f;
            m.startSpeed = 0f;
            ParticleSystem.EmissionModule e = ps.emission;
            e.enabled = false;
            ParticleSystem.ShapeModule s = ps.shape;
            s.enabled = false;
            ParticleSystem.SizeOverLifetimeModule sol = ps.sizeOverLifetime;
            sol.enabled = true;
            AnimationCurve grow = gravity < 0f ? AnimationCurve.Linear(0f, 0.6f, 1f, 1.6f) : AnimationCurve.Linear(0f, 1f, 1f, 0.3f);
            sol.size = new ParticleSystem.MinMaxCurve(1f, grow);
            ParticleSystem.ColorOverLifetimeModule col = ps.colorOverLifetime;
            col.enabled = true;
            Gradient g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                      new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.8f, 0.5f), new GradientAlphaKey(0f, 1f) });
            col.color = g;
            ParticleSystemRenderer r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
            ps.Play();
            return ps;
        }

        private void Emit(ParticleSystem ps, Vector3 pos, Vector3 vel, float size, float life, Color color, int count, float spread)
        {
            if (ps == null) return;
            ParticleSystem.EmitParams p = new ParticleSystem.EmitParams();
            for (int i = 0; i < count; i++)
            {
                p.position = pos + Random.insideUnitSphere * (size * 0.2f);
                p.velocity = vel + Random.insideUnitSphere * spread;
                p.startSize = size * Random.Range(0.7f, 1.3f);
                p.startLifetime = life * Random.Range(0.7f, 1.2f);
                p.startColor = color;
                p.rotation = Random.Range(0f, 360f);
                ps.Emit(p, 1);
            }
        }

        private Mesh BuildStar()
        {
            MeshBuilder b = new MeshBuilder(1);
            for (int i = 0; i < 3; i++)
            {
                Quaternion q = Quaternion.Euler(0f, 0f, i * 60f);
                Vector3 a = q * new Vector3(-0.5f, -0.12f, 0f), bb = q * new Vector3(-0.5f, 0.12f, 0f), c = q * new Vector3(0.5f, 0.12f, 0f), d = q * new Vector3(0.5f, -0.12f, 0f);
                b.Quad(0, a, bb, c, d, Vector3.back, new Vector2(0f, 0.3f), new Vector2(0f, 0.7f), new Vector2(1f, 0.7f), new Vector2(1f, 0.3f));
                b.Quad(0, d, c, bb, a, Vector3.forward, new Vector2(1f, 0.3f), new Vector2(1f, 0.7f), new Vector2(0f, 0.7f), new Vector2(0f, 0.3f));
            }
            // Side-on blade so the flash reads from behind the shooter too.
            b.Quad(0, new Vector3(0f, -0.15f, -0.1f), new Vector3(0f, 0.15f, -0.1f), new Vector3(0f, 0.15f, 0.9f), new Vector3(0f, -0.15f, 0.9f), Vector3.left, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(1f, 0f));
            b.Quad(0, new Vector3(0f, -0.15f, 0.9f), new Vector3(0f, 0.15f, 0.9f), new Vector3(0f, 0.15f, -0.1f), new Vector3(0f, -0.15f, -0.1f), Vector3.right, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(0f, 0f));
            return b.ToMesh("Muzzle Star");
        }

        private LineRenderer NewTracer(int i)
        {
            GameObject go = new GameObject("Tracer " + i);
            go.transform.SetParent(transform, false);
            LineRenderer lr = go.AddComponent<LineRenderer>();
            lr.sharedMaterial = Mats.Tracer;
            lr.positionCount = 2;
            lr.useWorldSpace = true;
            lr.widthMultiplier = 0.035f;
            lr.shadowCastingMode = ShadowCastingMode.Off;
            lr.receiveShadows = false;
            lr.numCapVertices = 0;
            lr.enabled = false;
            return lr;
        }

        private GameObject NewMuzzle(int i)
        {
            GameObject go = Shapes.MeshObject("Muzzle " + i, _starMesh, new[] { Mats.Muzzle }, transform, false);
            Light l = go.AddComponent<Light>();
            l.type = LightType.Point;
            l.range = 7f;
            l.intensity = 3.5f;
            l.color = new Color(1f, 0.75f, 0.4f);
            l.shadows = LightShadows.None;
            go.SetActive(false);
            return go;
        }

        // ------------------------------------------------------------------ public api

        public void Tracer(Vector3 from, Vector3 to)
        {
            LineRenderer lr = _tracers[_tracerNext];
            _tracerLife[_tracerNext] = 0.07f;
            _tracerNext = (_tracerNext + 1) % _tracers.Count;
            // Draw only a streak segment for a convincing bullet look.
            Vector3 d = to - from;
            float len = d.magnitude;
            Vector3 a = from + d * Random.Range(0.05f, 0.25f);
            Vector3 b = from + d * Mathf.Min(1f, Random.Range(0.45f, 0.9f));
            if (len < 6f) { a = from; b = to; }
            lr.SetPosition(0, a);
            lr.SetPosition(1, b);
            lr.enabled = true;
        }

        public void Muzzle(Vector3 pos, Vector3 dir, float size)
        {
            GameObject go = _muzzles[_muzzleNext];
            _muzzleLife[_muzzleNext] = 0.045f;
            _muzzleNext = (_muzzleNext + 1) % _muzzles.Count;
            go.transform.position = pos;
            go.transform.rotation = Quaternion.LookRotation(dir) * Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
            go.transform.localScale = Vector3.one * size * Random.Range(0.8f, 1.2f);
            go.SetActive(true);
            Emit(_smoke, pos + dir * 0.2f, dir * 1.5f + Vector3.up * 0.3f, 0.35f * size, 0.6f, new Color(0.6f, 0.6f, 0.6f, 0.35f), 1, 0.2f);
        }

        public void Impact(Vector3 point, Vector3 normal, ImpactSurface surface, bool decal, Transform hitTransform)
        {
            switch (surface)
            {
                case ImpactSurface.Metal:
                    Emit(_sparks, point, normal * 4f, 0.06f, 0.35f, new Color(1f, 0.8f, 0.4f), 8, 3f);
                    break;
                case ImpactSurface.Flesh:
                    Blood(point, normal, 6);
                    break;
                case ImpactSurface.Glass:
                    Emit(_sparks, point, normal * 2f, 0.04f, 0.4f, new Color(0.8f, 0.9f, 1f), 10, 2.5f);
                    break;
                case ImpactSurface.Water:
                    Emit(_water, point, Vector3.up * 3f, 0.25f, 0.6f, Color.white, 10, 1.5f);
                    break;
                case ImpactSurface.Dirt:
                    Emit(_dust, point, normal * 1.2f, 0.35f, 0.8f, new Color(0.55f, 0.45f, 0.32f, 0.6f), 4, 0.8f);
                    break;
                case ImpactSurface.Crystal:
                case ImpactSurface.Shadow:
                    CreatureHit(point, normal, surface, SurfaceTintOf(hitTransform, surface), 5);
                    break;
                default:
                    Emit(_dust, point, normal * 1.5f, 0.25f, 0.6f, new Color(0.65f, 0.62f, 0.58f, 0.55f), 3, 0.7f);
                    Emit(_sparks, point, normal * 3f, 0.04f, 0.18f, new Color(1f, 0.85f, 0.5f), 2, 2f);
                    break;
            }
            if (decal && surface != ImpactSurface.Flesh && surface != ImpactSurface.Water && surface != ImpactSurface.Crystal && surface != ImpactSurface.Shadow && hitTransform != null && hitTransform.gameObject.layer == Layers.World && hitTransform.GetComponentInParent<Rigidbody>() == null)
            {
                Decal(point, normal, Mats.BulletHole, Random.Range(0.12f, 0.18f));
            }
        }

        public void Blood(Vector3 point, Vector3 dir, int amount)
        {
            Emit(_blood, point, dir * 1.5f, 0.18f, 0.5f, new Color(0.6f, 0.03f, 0.03f, 0.95f), amount, 1.2f);
        }

        public void BloodPool(Vector3 groundPoint)
        {
            RaycastHit hit;
            if (Physics.Raycast(groundPoint + Vector3.up * 1f, Vector3.down, out hit, 3f, Layers.WorldMask, QueryTriggerInteraction.Ignore))
                Decal(hit.point, hit.normal, Mats.Blood, Random.Range(0.9f, 1.6f));
        }

        public void Decal(Vector3 point, Vector3 normal, Material mat, float size)
        {
            GameObject go;
            if (_decals.Count < MaxDecals)
            {
                go = Shapes.MeshObject("Decal", Shapes.QuadMesh, new[] { mat }, transform, false);
                go.GetComponent<MeshRenderer>().receiveShadows = true;
                _decals.Add(go);
            }
            else
            {
                go = _decals[_decalNext];
                _decalNext = (_decalNext + 1) % MaxDecals;
                go.GetComponent<MeshRenderer>().sharedMaterial = mat;
            }
            go.SetActive(true);
            go.transform.position = point + normal * 0.012f;
            go.transform.rotation = Quaternion.LookRotation(-normal) * Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
            go.transform.localScale = new Vector3(size, size, 1f);
        }

        public void Flash(Vector3 pos, Color color, float intensity, float range, float duration)
        {
            int best = 0;
            for (int i = 0; i < _flashes.Count; i++)
            {
                if (_flashLife[i] <= 0f) { best = i; break; }
                if (_flashLife[i] < _flashLife[best]) best = i;
            }
            Light l = _flashes[best];
            l.transform.position = pos;
            l.color = color;
            l.range = range;
            l.intensity = intensity;
            l.enabled = true;
            _flashLife[best] = duration;
            _flashMax[best] = duration;
            _flashIntensity[best] = intensity;
        }

        public void ExplosionFx(Vector3 pos, float radius)
        {
            float s = Mathf.Clamp(radius * 0.45f, 1f, 6f);
            Emit(_fire, pos, Vector3.up * 2f, s * 1.6f, 0.7f, new Color(1f, 0.6f, 0.25f, 1f), 18, radius * 0.9f);
            Emit(_fire, pos, Vector3.up * 4f, s, 0.45f, new Color(1f, 0.9f, 0.5f, 1f), 10, radius * 1.4f);
            Emit(_smoke, pos + Vector3.up * 1f, Vector3.up * 3f, s * 2f, 3.5f, new Color(0.12f, 0.11f, 0.11f, 0.8f), 16, radius * 0.5f);
            Emit(_debris, pos, Vector3.up * 8f, 0.15f, 2.2f, Color.white, 20, radius * 1.6f);
            Emit(_sparks, pos, Vector3.up * 6f, 0.08f, 0.9f, new Color(1f, 0.7f, 0.3f), 40, radius * 2f);
            Flash(pos + Vector3.up, new Color(1f, 0.6f, 0.25f), 8f, radius * 5f, 0.45f);
            RaycastHit hit;
            if (Physics.Raycast(pos + Vector3.up * 1.5f, Vector3.down, out hit, 4f, Layers.WorldMask, QueryTriggerInteraction.Ignore))
                Decal(hit.point, hit.normal, Mats.Scorch, radius * 0.9f);
        }

        public void Smoke(Vector3 pos, Vector3 vel, float size, float darkness)
        {
            Emit(_smoke, pos, vel, size, 2.2f, new Color(darkness, darkness, darkness, 0.7f), 1, 0.3f);
        }

        public void FirePuff(Vector3 pos, float size)
        {
            Emit(_fire, pos, Vector3.up * 1.5f, size, 0.6f, new Color(1f, 0.55f, 0.2f, 1f), 1, 0.4f);
        }

        public void Splash(Vector3 pos, float size)
        {
            Emit(_water, pos, Vector3.up * 5f, 0.4f * size, 1f, Color.white, 30, 2.5f * size);
            if (Game.Audio != null) Game.Audio.PlayAt(SfxId.Splash, pos, Mathf.Clamp01(size));
        }

        public void Dust(Vector3 pos, Vector3 vel, float size)
        {
            Emit(_dust, pos, vel, size, 1.2f, new Color(0.62f, 0.52f, 0.38f, 0.45f), 1, 0.6f);
        }

        public void Sparks(Vector3 pos, Vector3 vel, int count)
        {
            Emit(_sparks, pos, vel, 0.05f, 0.4f, new Color(1f, 0.8f, 0.4f), count, 2.5f);
        }

        public void WaterGeyser(Vector3 pos, float seconds)
        {
            GameObject go = new GameObject("Geyser");
            go.transform.position = pos;
            Geyser g = go.AddComponent<Geyser>();
            g.Remaining = seconds;
            g.Fx = this;
        }

        internal void GeyserTick(Vector3 pos)
        {
            Emit(_water, pos, Vector3.up * 9f, 0.35f, 1.3f, Color.white, 3, 1.4f);
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            for (int i = 0; i < _tracers.Count; i++)
            {
                if (_tracerLife[i] <= 0f) continue;
                _tracerLife[i] -= dt;
                if (_tracerLife[i] <= 0f) _tracers[i].enabled = false;
            }
            for (int i = 0; i < _muzzles.Count; i++)
            {
                if (_muzzleLife[i] <= 0f) continue;
                _muzzleLife[i] -= dt;
                if (_muzzleLife[i] <= 0f) _muzzles[i].SetActive(false);
            }
            for (int i = 0; i < _flashes.Count; i++)
            {
                if (_flashLife[i] <= 0f) continue;
                _flashLife[i] -= dt;
                Light l = _flashes[i];
                l.intensity = _flashIntensity[i] * Mathf.Clamp01(_flashLife[i] / Mathf.Max(0.01f, _flashMax[i]));
                if (_flashLife[i] <= 0f) l.enabled = false;
            }
        }
    }

    /// <summary>Hydrant water fountain after a car knocks a hydrant over.</summary>
    public sealed class Geyser : MonoBehaviour
    {
        public float Remaining = 15f;
        public Fx Fx;
        private AudioSource _loop;

        private void Start()
        {
            if (Game.Audio != null) _loop = Game.Audio.CreateLoop(transform, SfxId.WaterSpray, 0.5f, 40f);
        }

        private void Update()
        {
            Remaining -= Time.deltaTime;
            if (Fx != null) Fx.GeyserTick(transform.position);
            if (Remaining <= 0f) Destroy(gameObject);
        }
    }
}
