using UnityEngine;
using UnityEngine.Rendering;

namespace NeonSyndicate
{
    /// <summary>
    /// A sustained or pulsed beam presentation (owner: models and audio): a narrow bright core line inside a wider
    /// translucent shimmering sleeve, a soft glow where it leaves the emitter and a contact glow where it stops. A
    /// redirected Mirrorwake pulse draws a second core and sleeve from the focus to the target, with a glint at the bend.
    /// Prism Thread is sustained: the weapons runtime calls Show every frame, Hide cuts it instantly, and a beam nobody
    /// has refreshed for 0.25 s real time hides itself (a safety net, never a substitute for Hide). Mirrorwake is a pulse:
    /// call Show or ShowTwo once per shot and it fades out by itself over 0.25 s (Hide still cuts it at once).
    /// Shared cached materials; per-frame changes only touch positions, widths and vertex colours.
    /// </summary>
    public class CrystalBeamVisual : MonoBehaviour
    {
        public CrystalBehavior Behavior;

        /// <summary>Seconds a Mirrorwake pulse takes to fade after its last Show.</summary>
        public const float PulseFade = 0.25f;
        /// <summary>Real seconds without a Show after which a sustained beam hides itself.</summary>
        public const float StaleSeconds = 0.25f;

        private LineRenderer _coreA, _sleeveA, _coreB, _sleeveB;
        private Transform _glowStart, _glowEnd, _glowFocus;
        private MeshRenderer _glowStartR, _glowEndR, _glowFocusR;
        private bool _built;
        private CrystalBehavior _builtFor;
        private bool _visible, _two;
        private Vector3 _from, _focus, _to;
        private float _width, _intensity;
        private float _lastShowReal;
        private float _age;
        private float _t;

        /// <summary>Shows one segment (from the muzzle to the legal endpoint).</summary>
        public virtual void Show(Vector3 from, Vector3 to, float width, float intensity)
        {
            Ensure();
            _from = from;
            _to = to;
            _two = false;
            Begin(width, intensity);
        }

        /// <summary>Shows a redirected pulse: muzzle to focus, focus to target.</summary>
        public virtual void ShowTwo(Vector3 from, Vector3 focus, Vector3 to, float width, float intensity)
        {
            Ensure();
            _from = from;
            _focus = focus;
            _to = to;
            _two = true;
            Begin(width, intensity);
        }

        /// <summary>Hides every part of the beam at once (cancel, swap, pause, death).</summary>
        public virtual void Hide()
        {
            _visible = false;
            if (!_built) return;
            SetEnabled(_coreA, false);
            SetEnabled(_sleeveA, false);
            SetEnabled(_coreB, false);
            SetEnabled(_sleeveB, false);
            if (_glowStartR != null) _glowStartR.enabled = false;
            if (_glowEndR != null) _glowEndR.enabled = false;
            if (_glowFocusR != null) _glowFocusR.enabled = false;
        }

        private void Begin(float width, float intensity)
        {
            _width = Mathf.Clamp(width, 0.005f, 1f);
            _intensity = Mathf.Clamp(intensity, 0f, 2f);
            _visible = true;
            _age = 0f;
            _lastShowReal = Time.unscaledTime;
            SetEnabled(_coreA, true);
            SetEnabled(_sleeveA, true);
            SetEnabled(_coreB, _two);
            SetEnabled(_sleeveB, _two);
            if (_glowStartR != null) _glowStartR.enabled = true;
            if (_glowEndR != null) _glowEndR.enabled = true;
            if (_glowFocusR != null) _glowFocusR.enabled = _two;
            Apply();
        }

        private void OnDisable()
        {
            Hide();
        }

        private void LateUpdate()
        {
            if (!_visible) return;
            float dt = Time.deltaTime;
            _t += dt;
            if (Behavior == CrystalBehavior.Mirrorwake)
            {
                _age += dt;
                if (_age >= PulseFade)
                {
                    Hide();
                    return;
                }
            }
            else if (Time.unscaledTime - _lastShowReal > StaleSeconds)
            {
                Hide();
                return;
            }
            Apply();
        }

        /// <summary>Positions, widths, colours and glow cards for the current frame.</summary>
        private void Apply()
        {
            float fade = Behavior == CrystalBehavior.Mirrorwake ? 1f - Mathf.Clamp01(_age / PulseFade) : 1f;
            float k = _intensity * fade;
            float shimmer = 1f + 0.12f * Mathf.Sin(_t * 23f) + 0.06f * Mathf.Sin(_t * 41f);
            float coreW = Mathf.Max(0.012f, _width * 0.42f) * (Behavior == CrystalBehavior.Mirrorwake ? Mathf.Lerp(0.5f, 1.3f, fade) : 1f);
            float sleeveW = Mathf.Max(0.04f, _width * 1.9f) * shimmer;
            Color coreC = new Color(1f, 1f, 1f, Mathf.Clamp01(0.95f * k));
            Color sleeveC = new Color(1f, 1f, 1f, Mathf.Clamp01(0.32f * k));
            Vector3 endA = _two ? _focus : _to;
            Line(_coreA, _from, endA, coreW, coreC);
            Line(_sleeveA, _from, endA, sleeveW, sleeveC);
            if (_two)
            {
                Line(_coreB, _focus, _to, coreW, coreC);
                Line(_sleeveB, _focus, _to, sleeveW, sleeveC);
            }
            Transform cam = Game.Cam != null ? Game.Cam.transform : (Camera.main != null ? Camera.main.transform : null);
            Card(_glowStart, _from, (_width * 4f + 0.06f) * Mathf.Max(0.3f, k), cam);
            float contact = (_width * 7f + 0.12f) * Mathf.Max(0.3f, k) * (1f + 0.15f * Mathf.Sin(_t * 31f));
            Card(_glowEnd, _to, contact, cam);
            if (_two) Card(_glowFocus, _focus, (_width * 6f + 0.1f) * Mathf.Max(0.3f, k) * (1f + 0.2f * Mathf.Sin(_t * 17f)), cam);
        }

        private static void Line(LineRenderer l, Vector3 a, Vector3 b, float w, Color c)
        {
            if (l == null) return;
            l.SetPosition(0, a);
            l.SetPosition(1, b);
            l.startWidth = w;
            l.endWidth = w;
            l.startColor = c;
            l.endColor = c;
        }

        private static void Card(Transform t, Vector3 at, float size, Transform cam)
        {
            if (t == null) return;
            t.position = at;
            if (cam != null) t.rotation = cam.rotation;
            t.localScale = new Vector3(size, size, size);
        }

        private static void SetEnabled(LineRenderer l, bool on)
        {
            if (l != null) l.enabled = on;
        }

        /// <summary>Builds the lines and glow cards the first time (or again if the family changed).</summary>
        private void Ensure()
        {
            if (_built && _builtFor == Behavior) return;
            if (_built) Teardown();
            _builtFor = Behavior;
            Material core = CrystalFocusModels.BeamMaterial(Behavior, true);
            Material sleeve = CrystalFocusModels.BeamMaterial(Behavior, false);
            Material glow = CrystalFocusModels.BeamGlowMaterial(Behavior);
            _sleeveA = NewLine("Sleeve A", sleeve);
            _coreA = NewLine("Core A", core);
            _sleeveB = NewLine("Sleeve B", sleeve);
            _coreB = NewLine("Core B", core);
            _glowStart = NewCard("Emitter Glow", glow, out _glowStartR);
            _glowEnd = NewCard("Contact Glow", glow, out _glowEndR);
            _glowFocus = NewCard("Focus Glint", glow, out _glowFocusR);
            _built = true;
            Hide();
        }

        private void Teardown()
        {
            Transform[] kids = new Transform[transform.childCount];
            for (int i = 0; i < kids.Length; i++) kids[i] = transform.GetChild(i);
            for (int i = 0; i < kids.Length; i++) if (kids[i] != null) Destroy(kids[i].gameObject);
            _built = false;
        }

        private LineRenderer NewLine(string n, Material m)
        {
            GameObject go = new GameObject(n);
            go.transform.SetParent(transform, false);
            LineRenderer l = go.AddComponent<LineRenderer>();
            l.positionCount = 2;
            l.useWorldSpace = true;
            l.sharedMaterial = m;
            l.textureMode = LineTextureMode.Stretch;
            l.numCapVertices = 2;
            l.shadowCastingMode = ShadowCastingMode.Off;
            l.receiveShadows = false;
            l.enabled = false;
            return l;
        }

        private Transform NewCard(string n, Material m, out MeshRenderer r)
        {
            GameObject go = Shapes.MeshObject(n, Shapes.QuadMesh, new[] { m }, transform, false);
            r = go.GetComponent<MeshRenderer>();
            r.receiveShadows = false;
            r.enabled = false;
            return go.transform;
        }
    }
}
