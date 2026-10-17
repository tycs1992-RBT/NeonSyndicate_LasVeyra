using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace NeonSyndicate
{
    /// <summary>
    /// The visible result of an exploration assist: a line of small glowing motes drifting along the revealed route
    /// (they appear one after another from the companion outward, bob, pulse forward along the way and fade after a
    /// while) and a soft light pillar at the revealed place. One shared pool of shadowless additive quads and one
    /// update for all of them: no per-mote scripts, no colliders, no lights.
    /// </summary>
    public sealed class CompanionTrailFx : MonoBehaviour
    {
        private const int MaxMotes = 48;
        private const float Spacing = 1.4f;

        private struct Mote
        {
            public Vector3 Pos;
            public float Delay;
            public float Phase;
        }

        private static CompanionTrailFx _instance;
        private static Material _mat;
        private static readonly int TintId = Shader.PropertyToID("_TintColor");
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        private readonly Mote[] _motes = new Mote[MaxMotes];
        private Transform[] _quads;
        private MeshRenderer[] _renderers;
        private Transform _pillar;
        private MeshRenderer _pillarR;
        private MaterialPropertyBlock _mpb;
        private int _count;
        private float _t, _life;
        private Color _color;
        private bool _beacon;

        public static void ResetStatics()
        {
            _instance = null;
            _mat = null;
        }

        /// <summary>True while a revealed trail is showing.</summary>
        public static bool Showing { get { return _instance != null && _instance._count > 0 && _instance._t < _instance._life; } }

        /// <summary>Shows a trail along route points (world, floor height) for seconds, with a pillar at the last point.</summary>
        public static void Show(IList<Vector3> points, Color color, float seconds, bool beacon)
        {
            if (points == null || points.Count == 0) return;
            CompanionTrailFx fx = Ensure();
            if (fx == null) return;
            fx.Fill(points, color, seconds, beacon);
        }

        public static void Clear()
        {
            if (_instance != null) _instance.Hide();
        }

        private static CompanionTrailFx Ensure()
        {
            if (_instance != null) return _instance;
            GameObject go = new GameObject("Companion Trail Fx");
            _instance = go.AddComponent<CompanionTrailFx>();
            _instance.Build();
            return _instance;
        }

        private void Build()
        {
            if (_mat == null) _mat = Mats.Additive("Companion Trail", Color.white, UIKit.Circle);
            _mpb = new MaterialPropertyBlock();
            _quads = new Transform[MaxMotes];
            _renderers = new MeshRenderer[MaxMotes];
            for (int i = 0; i < MaxMotes; i++)
            {
                GameObject q = Shapes.MeshObject("Mote", Shapes.QuadMesh, new[] { _mat }, transform, false);
                q.layer = Layers.IgnoreRaycast;
                _quads[i] = q.transform;
                _renderers[i] = q.GetComponent<MeshRenderer>();
                _renderers[i].shadowCastingMode = ShadowCastingMode.Off;
                _renderers[i].receiveShadows = false;
                q.SetActive(false);
            }
            GameObject p = Shapes.MeshObject("Reveal Pillar", Shapes.CylinderMesh, new[] { _mat }, transform, false);
            p.layer = Layers.IgnoreRaycast;
            _pillar = p.transform;
            _pillarR = p.GetComponent<MeshRenderer>();
            _pillarR.shadowCastingMode = ShadowCastingMode.Off;
            _pillarR.receiveShadows = false;
            p.SetActive(false);
        }

        private void Fill(IList<Vector3> points, Color color, float seconds, bool beacon)
        {
            _color = color;
            _life = Mathf.Max(2f, seconds);
            _t = 0f;
            _beacon = beacon;
            _count = 0;
            float carry = 0f;
            for (int i = 0; i < points.Count - 1 && _count < MaxMotes; i++)
            {
                Vector3 a = points[i], b = points[i + 1];
                float seg = (b - a).magnitude;
                float s = carry;
                while (s < seg && _count < MaxMotes)
                {
                    Vector3 p = Vector3.Lerp(a, b, seg > 1e-4f ? s / seg : 0f);
                    _motes[_count] = new Mote { Pos = p + Vector3.up * 0.4f, Delay = _count * 0.05f, Phase = _count * 0.6f };
                    _count++;
                    s += Spacing;
                }
                carry = s - seg;
            }
            if (_count == 0)
            {
                _motes[0] = new Mote { Pos = points[0] + Vector3.up * 0.4f, Delay = 0f, Phase = 0f };
                _count = 1;
            }
            for (int i = 0; i < MaxMotes; i++) _quads[i].gameObject.SetActive(i < _count);
            Vector3 end = points[points.Count - 1];
            _pillar.gameObject.SetActive(beacon);
            if (beacon)
            {
                _pillar.position = end + Vector3.up * 1.6f;
                _pillar.localScale = new Vector3(0.35f, 1.6f, 0.35f);
            }
        }

        private void Hide()
        {
            _count = 0;
            if (_quads != null) for (int i = 0; i < _quads.Length; i++) if (_quads[i] != null) _quads[i].gameObject.SetActive(false);
            if (_pillar != null) _pillar.gameObject.SetActive(false);
        }

        private void LateUpdate()
        {
            if (_count == 0) return;
            _t += Time.deltaTime;
            if (_t >= _life)
            {
                Hide();
                return;
            }
            Transform cam = Game.Cam != null ? Game.Cam.transform : null;
            float fadeOut = Mathf.Clamp01((_life - _t) / 1.5f);
            float wave = _t * 9f;
            for (int i = 0; i < _count; i++)
            {
                Mote m = _motes[i];
                float on = Mathf.Clamp01((_t - m.Delay) / 0.35f);
                float bob = Mathf.Sin(_t * 2.2f + m.Phase) * 0.08f;
                Transform q = _quads[i];
                q.position = m.Pos + Vector3.up * bob;
                if (cam != null) q.rotation = cam.rotation;
                float pulse = 0.55f + 0.45f * Mathf.Max(0f, Mathf.Sin(wave - i * 0.5f));
                float size = 0.22f + 0.1f * pulse;
                q.localScale = new Vector3(size, size, size);
                Tint(_renderers[i], _color * (on * fadeOut * pulse));
            }
            if (_beacon && _pillar.gameObject.activeSelf)
            {
                float a = fadeOut * (0.35f + 0.15f * Mathf.Sin(_t * 3f));
                Tint(_pillarR, _color * a);
            }
        }

        private void Tint(MeshRenderer r, Color c)
        {
            c.a = Mathf.Clamp01(c.a);
            _mpb.Clear();
            _mpb.SetColor(TintId, c);
            _mpb.SetColor(BaseColorId, c);
            _mpb.SetColor(ColorId, c);
            r.SetPropertyBlock(_mpb);
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }
    }
}
