using UnityEngine;
using UnityEngine.Rendering;

namespace NeonSyndicate
{
    /// <summary>
    /// Travel power effects, all cheap and pooled: speed streaks that rush past at super speed and in fast flight,
    /// ground dust kicked up behind a Surge, the soft boom ring when super speed tops out, landing dust rings and the
    /// afterimage trail (fading see-through copies of the body, baked from its skinned mesh a few times a second).
    /// </summary>
    public sealed partial class Fx
    {
        private ParticleSystem _streaks;
        private AfterimageTrail _afterimages;

        /// <summary>The stretched additive emitter for speed streaks, made the first time it is needed.</summary>
        private ParticleSystem Streaks
        {
            get
            {
                if (_streaks == null)
                {
                    _streaks = Emitter("Travel Streaks", Mats.Glow(new Color(0.78f, 0.92f, 1f, 0.55f)), 260, 0f, true);
                    ParticleSystemRenderer r = _streaks.GetComponent<ParticleSystemRenderer>();
                    r.renderMode = ParticleSystemRenderMode.Stretch;
                    r.velocityScale = 0.06f;
                    r.lengthScale = 2f;
                }
                return _streaks;
            }
        }

        /// <summary>Speed streaks rushing past a moving point (super speed, fast flight). intensity 0..1 sets how many.</summary>
        public void TravelStreaks(Vector3 center, Vector3 velocity, float intensity)
        {
            float speed = velocity.magnitude;
            if (speed < 1f || intensity <= 0.01f) return;
            Vector3 dir = velocity / speed;
            Vector3 side = Vector3.Cross(dir, Vector3.up);
            if (side.sqrMagnitude < 0.01f) side = Vector3.right;
            side.Normalize();
            Vector3 up = Vector3.Cross(side, dir);
            int count = Mathf.Clamp(Mathf.RoundToInt(intensity * 4f), 1, 4);
            ParticleSystem ps = Streaks;
            ParticleSystem.EmitParams p = new ParticleSystem.EmitParams();
            for (int i = 0; i < count; i++)
            {
                float ang = Random.Range(0f, Mathf.PI * 2f);
                float rad = Random.Range(1.8f, 5.5f);
                Vector3 offset = (side * Mathf.Cos(ang) + up * Mathf.Sin(ang)) * rad;
                p.position = center + dir * Random.Range(4f, 18f) + offset;
                p.velocity = -dir * Random.Range(18f, 30f);
                p.startSize = Random.Range(0.025f, 0.05f);
                p.startLifetime = Random.Range(0.18f, 0.32f);
                p.startColor = new Color(0.85f, 0.95f, 1f, Mathf.Lerp(0.25f, 0.7f, intensity));
                ps.Emit(p, 1);
            }
        }

        /// <summary>Dust kicked up behind super speed: sand in the wilds, powder on snow, grey grit on city streets.</summary>
        public void TravelDust(Vector3 groundPos, Vector3 velocity, float intensity)
        {
            if (intensity <= 0.01f) return;
            if (CityLayout.IsSnowGround(groundPos.x, groundPos.z) && groundPos.y < 1.5f)
            {
                SnowBurst(groundPos, 0.2f + 0.4f * intensity, 2);
                return;
            }
            bool wild = CityLayout.IsWilderness(CityLayout.DistrictAt(groundPos));
            Color c = wild ? new Color(0.68f, 0.56f, 0.4f, 0.45f) : new Color(0.6f, 0.58f, 0.55f, 0.32f);
            Emit(_dust, groundPos, -velocity * 0.12f + Vector3.up * 1.1f, 0.35f + 0.5f * intensity, 0.9f, c, 1 + Mathf.RoundToInt(intensity * 2f), 0.8f);
        }

        /// <summary>The soft boom when super speed tops out: a ring of dust thrown outward, a pale flash and a few sparks.</summary>
        public void TravelBoom(Vector3 pos, Vector3 dir)
        {
            Color c = new Color(0.8f, 0.82f, 0.86f, 0.4f);
            for (int i = 0; i < 16; i++)
            {
                float a = i / 16f * Mathf.PI * 2f;
                Vector3 o = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                Emit(_dust, pos + o * 0.6f + Vector3.up * 0.3f, o * 9f + Vector3.up * 0.6f, 0.9f, 0.7f, c, 1, 0.5f);
            }
            Emit(_sparks, pos + Vector3.up, dir * 6f, 0.06f, 0.35f, new Color(0.8f, 0.95f, 1f), 14, 5f);
            Flash(pos + Vector3.up * 1.2f, new Color(0.75f, 0.9f, 1f), 3f, 9f, 0.15f);
        }

        /// <summary>A landing's dust ring: size about 1 for a heavy landing, about 0.3 for a soft one.</summary>
        public void TravelLanding(Vector3 pos, float size)
        {
            int n = Mathf.Clamp(Mathf.RoundToInt(6f + size * 10f), 4, 20);
            Color c = new Color(0.62f, 0.6f, 0.56f, 0.45f);
            for (int i = 0; i < n; i++)
            {
                float a = i / (float)n * Mathf.PI * 2f;
                Vector3 o = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                Emit(_dust, pos + o * 0.4f + Vector3.up * 0.2f, o * (2.5f + size * 6f) + Vector3.up * 0.5f, 0.5f + size * 0.6f, 0.9f, c, 1, 0.4f);
            }
            GroundDust(pos, 0.4f + size * 0.6f, Mathf.Max(2, n / 3));
            if (size > 0.7f) Emit(_debris, pos + Vector3.up * 0.2f, Vector3.up * 3.5f, 0.08f, 1.2f, Color.white, 6, 3f);
        }

        /// <summary>Leaves one afterimage of a body in this frame's pose, fading out over lifetime seconds.</summary>
        public void TravelAfterimage(SkinnedMeshRenderer body, float lifetime)
        {
            if (body == null) return;
            if (_afterimages == null)
            {
                GameObject go = new GameObject("Travel Afterimages");
                go.transform.SetParent(transform, false);
                _afterimages = go.AddComponent<AfterimageTrail>();
            }
            _afterimages.Spawn(body, lifetime);
        }
    }

    /// <summary>
    /// A small pool of afterimages: each is the body's skinned mesh baked into a plain mesh with a soft additive
    /// material, left behind where the body was and faded out. Six at most, so it stays cheap.
    /// </summary>
    public sealed class AfterimageTrail : MonoBehaviour
    {
        private const int Count = 6;
        private GameObject[] _ghosts;
        private Mesh[] _meshes;
        private Material[] _mats;
        private Color[] _full;
        private Color[] _fullTint;
        private float[] _life;
        private float[] _max;
        private int _next;

        private void Build()
        {
            _ghosts = new GameObject[Count];
            _meshes = new Mesh[Count];
            _mats = new Material[Count];
            _full = new Color[Count];
            _fullTint = new Color[Count];
            _life = new float[Count];
            _max = new float[Count];
            for (int i = 0; i < Count; i++)
            {
                GameObject go = new GameObject("Afterimage " + i);
                go.transform.SetParent(transform, false);
                Mesh m = new Mesh();
                m.name = "Afterimage " + i;
                m.MarkDynamic();
                go.AddComponent<MeshFilter>().sharedMesh = m;
                MeshRenderer r = go.AddComponent<MeshRenderer>();
                Material mat = Mats.Additive("Afterimage " + i, new Color(0.42f, 0.72f, 1f, 0.42f), null);
                r.sharedMaterial = mat;
                r.shadowCastingMode = ShadowCastingMode.Off;
                r.receiveShadows = false;
                _full[i] = mat.color;
                _fullTint[i] = mat.HasProperty("_TintColor") ? mat.GetColor("_TintColor") : mat.color;
                _ghosts[i] = go;
                _meshes[i] = m;
                _mats[i] = mat;
                go.SetActive(false);
            }
        }

        /// <summary>Bakes the body's current pose into the next pooled ghost.</summary>
        public void Spawn(SkinnedMeshRenderer body, float lifetime)
        {
            if (body == null || body.sharedMesh == null) return;
            if (_ghosts == null) Build();
            int i = _next;
            _next = (_next + 1) % Count;
            body.BakeMesh(_meshes[i], true);
            Transform t = body.transform;
            _ghosts[i].transform.SetPositionAndRotation(t.position, t.rotation);
            _ghosts[i].transform.localScale = Vector3.one;
            _life[i] = Mathf.Max(0.05f, lifetime);
            _max[i] = _life[i];
            SetFade(i, 1f);
            _ghosts[i].SetActive(true);
        }

        private void Update()
        {
            if (_ghosts == null) return;
            float dt = Time.deltaTime;
            for (int i = 0; i < Count; i++)
            {
                if (!_ghosts[i].activeSelf) continue;
                _life[i] -= dt;
                if (_life[i] <= 0f) _ghosts[i].SetActive(false);
                else SetFade(i, _life[i] / _max[i]);
            }
        }

        private void SetFade(int i, float f)
        {
            float k = f * f;
            Material m = _mats[i];
            m.color = _full[i] * k;
            if (m.HasProperty("_TintColor")) m.SetColor("_TintColor", _fullTint[i] * k);
        }
    }
}
