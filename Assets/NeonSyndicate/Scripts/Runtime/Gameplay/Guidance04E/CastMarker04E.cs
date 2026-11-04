using UnityEngine;
using UnityEngine.Rendering;

namespace NeonSyndicate
{
    /// <summary>
    /// The marker over a cast member's head (or their message terminal): one shape per giver state, so the state reads
    /// without colour. A spinning gold diamond for a main quest, a cyan ring for a side quest, an orange arrow pointing
    /// down for the active objective, a green check for a hand-in, a grey bar (only when near) for "not right now".
    /// Completed and no-work givers show nothing. The flat shapes turn to face the camera and grow a little with
    /// distance so they stay readable. Meshes are shared by every marker; materials come from the shared cache.
    /// </summary>
    public sealed class CastMarker04E : MonoBehaviour
    {
        private static Mesh _diamond, _ring, _check, _bar, _arrow;

        public static void ResetStatics()
        {
            _diamond = null;
            _ring = null;
            _check = null;
            _bar = null;
            _arrow = null;
        }

        private Transform _pivot;
        private MeshFilter _filter;
        private MeshRenderer _renderer;
        private MarkerShape04E _shape = MarkerShape04E.None;
        private GiverState _state = GiverState.None;
        private float _phase;
        private TextMesh _nameplate;
        private string _person = "";
        private string _quest = "";
        private bool _terminal;
        private string _plateText = "";
        private string _progressH8S = "";
        private bool _contrastH8S;

        public GiverState State { get { return _state; } }
        public MarkerShape04E Shape { get { return _shape; } }

        public static CastMarker04E Create(Transform parent, float height)
        {
            GameObject go = new GameObject("Giver Marker 04E");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(0f, height, 0f);
            CastMarker04E m = go.AddComponent<CastMarker04E>();
            GameObject shape = new GameObject("Shape");
            shape.transform.SetParent(go.transform, false);
            m._pivot = shape.transform;
            m._filter = shape.AddComponent<MeshFilter>();
            m._renderer = shape.AddComponent<MeshRenderer>();
            m._renderer.shadowCastingMode = ShadowCastingMode.Off;
            m._renderer.receiveShadows = false;
            shape.SetActive(false);
            m._phase = Random.value * 6f;
            if (Mats.UIFont != null)
            {
                GameObject label = new GameObject("Cast name and current quest");
                label.transform.SetParent(go.transform, false);
                label.transform.localPosition = new Vector3(0f, 0.52f, 0f);
                m._nameplate = label.AddComponent<TextMesh>();
                m._nameplate.font = Mats.UIFont;
                m._nameplate.fontSize = 48;
                m._nameplate.characterSize = 0.04f;
                m._nameplate.anchor = TextAnchor.MiddleCenter;
                m._nameplate.alignment = TextAlignment.Center;
                MeshRenderer textRenderer = label.GetComponent<MeshRenderer>();
                textRenderer.sharedMaterial = Mats.UIFont.material;
                textRenderer.shadowCastingMode = ShadowCastingMode.Off;
                textRenderer.receiveShadows = false;
                label.SetActive(false);
            }
            return m;
        }

        /// <summary>The same cast identity and offer chosen by CampaignDirector, not a second quest-state decision.</summary>
        public void SetPresentation(string person, string quest, bool terminal)
        {
            _person = person ?? "";
            _quest = quest ?? "";
            _terminal = terminal;
        }

        /// <summary>
        /// H8S: the title of this person's quest while it runs somewhere else ("" for none). The giver state itself is
        /// unchanged (it has no marker shape then); within 14 m the nameplate says IN PROGRESS with the quest's title.
        /// </summary>
        public void SetProgressNoteH8S(string questTitle)
        {
            _progressH8S = questTitle ?? "";
        }

        private void RefreshNameplate(float distance)
        {
            if (_nameplate == null) return;
            bool progress = _progressH8S.Length > 0 && distance <= 14f && !GuidanceRules04E.OverheadVisible(_state, distance);
            bool visible = progress || (distance <= 32f && GuidanceRules04E.OverheadVisible(_state, distance));
            _nameplate.gameObject.SetActive(visible);
            if (!visible) return;
            string text = progress ? _person + (_terminal ? " (terminal)" : "") + "\nIN PROGRESS: " + _progressH8S
                : _person + (_terminal ? " (terminal)" : "") + "\n" + GuidanceRules04E.LabelOf(_state) + (_quest.Length > 0 ? ": " + _quest : "");
            if (_plateText != text)
            {
                _plateText = text;
                _nameplate.text = text;
            }
            Color c = progress ? new Color(1f, 0.82f, 0.45f) : GuidanceRules04E.ColorOf(_state);
            _nameplate.color = AccessibilityRulesH8S.MarkerColor(c, Settings.HighContrastMarkersH8S);
        }

        /// <summary>Shows the state's shape (or nothing) for a viewer at this distance.</summary>
        public void Set(GiverState s, float distance)
        {
            _state = s;
            RefreshNameplate(distance);
            MarkerShape04E shape = GuidanceRules04E.OverheadVisible(s, distance) ? GuidanceRules04E.ShapeOf(s) : MarkerShape04E.None;
            if (shape == MarkerShape04E.DoneDot) shape = MarkerShape04E.None;
            bool contrast = Settings.HighContrastMarkersH8S;
            if (shape == _shape && contrast == _contrastH8S) return;
            _shape = shape;
            _contrastH8S = contrast;
            if (_pivot == null) return;
            if (shape == MarkerShape04E.None)
            {
                _pivot.gameObject.SetActive(false);
                return;
            }
            _filter.sharedMesh = MeshFor(shape);
            // H8S high contrast: the same hue at full brightness and a stronger glow.
            _renderer.sharedMaterial = Mats.Emissive(AccessibilityRulesH8S.MarkerColor(GuidanceRules04E.ColorOf(s), contrast), contrast ? 2.4f : 1.7f);
            _pivot.gameObject.SetActive(true);
        }

        private void LateUpdate()
        {
            if (_shape == MarkerShape04E.None || _pivot == null) return;
            _phase += Time.deltaTime;
            Camera cam = Game.Cam != null ? Game.Cam.Cam : null;
            Vector3 camPos = cam != null ? cam.transform.position : transform.position + Vector3.back;
            if (_nameplate != null && _nameplate.gameObject.activeSelf)
            {
                // TextMesh reads from local -Z. Keep text level, oriented toward the actual camera.
                float textYaw = QuestWorld.YawToward(_nameplate.transform.position, camPos) + 180f;
                _nameplate.transform.rotation = Quaternion.Euler(0f, textYaw, 0f);
            }
            float bob = Mathf.Sin(_phase * 2.4f) * 0.06f;
            _pivot.localPosition = new Vector3(0f, bob, 0f);
            float yaw = QuestWorld.YawToward(transform.position, camPos);
            _pivot.rotation = _shape == MarkerShape04E.Diamond ? Quaternion.Euler(0f, _phase * 90f, 0f) : Quaternion.Euler(0f, yaw, 0f);
            float d = Vector3.Distance(camPos, transform.position);
            float scale = Mathf.Clamp(0.8f + d * 0.022f, 0.85f, 2.2f) * AccessibilityRulesH8S.MarkerScale(_contrastH8S);
            _pivot.localScale = new Vector3(scale, scale, scale);
        }

        // ================================================================== shared meshes

        private static Mesh MeshFor(MarkerShape04E shape)
        {
            switch (shape)
            {
                case MarkerShape04E.Diamond: return _diamond != null ? _diamond : (_diamond = BuildDiamond());
                case MarkerShape04E.Ring: return _ring != null ? _ring : (_ring = BuildRing());
                case MarkerShape04E.Check: return _check != null ? _check : (_check = BuildCheck());
                case MarkerShape04E.Bar: return _bar != null ? _bar : (_bar = BuildBar());
                default: return _arrow != null ? _arrow : (_arrow = BuildArrow());
            }
        }

        /// <summary>An upright octahedron, 0.56 m tall.</summary>
        private static Mesh BuildDiamond()
        {
            MeshBuilder b = new MeshBuilder(1);
            Vector3 top = new Vector3(0f, 0.28f, 0f), bottom = new Vector3(0f, -0.28f, 0f);
            Vector3[] eq = { new Vector3(0.18f, 0f, 0f), new Vector3(0f, 0f, 0.18f), new Vector3(-0.18f, 0f, 0f), new Vector3(0f, 0f, -0.18f) };
            Vector2 uv = Vector2.zero;
            for (int i = 0; i < 4; i++)
            {
                Vector3 a = eq[i], c = eq[(i + 1) % 4];
                Vector3 nUp = Vector3.Cross(c - top, a - top).normalized;
                b.Triangle(0, top, c, a, nUp, uv, uv, uv);
                Vector3 nDown = Vector3.Cross(a - bottom, c - bottom).normalized;
                b.Triangle(0, bottom, a, c, nDown, uv, uv, uv);
            }
            return b.ToMesh("Giver Diamond 04E");
        }

        /// <summary>A ring in the XY plane (it faces the camera after the marker turns), 0.42 m across.</summary>
        private static Mesh BuildRing()
        {
            MeshBuilder b = new MeshBuilder(1);
            const int seg = 20, tube = 6;
            const float R = 0.18f, r = 0.035f;
            Vector2 uv = Vector2.zero;
            for (int i = 0; i < seg; i++)
            {
                float a0 = i * Mathf.PI * 2f / seg, a1 = (i + 1) * Mathf.PI * 2f / seg;
                for (int j = 0; j < tube; j++)
                {
                    float t0 = j * Mathf.PI * 2f / tube, t1 = (j + 1) * Mathf.PI * 2f / tube;
                    Vector3 p00 = Torus(a0, t0, R, r), p01 = Torus(a0, t1, R, r), p10 = Torus(a1, t0, R, r), p11 = Torus(a1, t1, R, r);
                    Vector3 n = (Torus(a0 + (a1 - a0) * 0.5f, t0 + (t1 - t0) * 0.5f, R, r) - Torus(a0 + (a1 - a0) * 0.5f, 0f, R, 0f)).normalized;
                    b.Quad(0, p00, p10, p11, p01, n, uv, uv, uv, uv);
                }
            }
            return b.ToMesh("Giver Ring 04E");
        }

        private static Vector3 Torus(float a, float t, float R, float r)
        {
            float c = R + r * Mathf.Cos(t);
            return new Vector3(Mathf.Cos(a) * c, Mathf.Sin(a) * c, r * Mathf.Sin(t));
        }

        /// <summary>A check mark: a short stroke and a long stroke.</summary>
        private static Mesh BuildCheck()
        {
            MeshBuilder b = new MeshBuilder(1);
            Stroke(b, new Vector3(-0.17f, 0.02f, 0f), new Vector3(-0.05f, -0.11f, 0f), 0.06f);
            Stroke(b, new Vector3(-0.07f, -0.11f, 0f), new Vector3(0.19f, 0.17f, 0f), 0.06f);
            return b.ToMesh("Giver Check 04E");
        }

        private static void Stroke(MeshBuilder b, Vector3 from, Vector3 to, float width)
        {
            Vector3 d = to - from;
            float angle = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
            b.Box(0, (from + to) * 0.5f, new Vector3(d.magnitude + width * 0.5f, width, 0.05f), QMath.AZ(angle), UVMode.Unit);
        }

        /// <summary>A flat grey bar, 0.38 m wide.</summary>
        private static Mesh BuildBar()
        {
            MeshBuilder b = new MeshBuilder(1);
            b.Box(0, Vector3.zero, new Vector3(0.38f, 0.075f, 0.05f), UVMode.Unit);
            return b.ToMesh("Giver Bar 04E");
        }

        /// <summary>An arrow pointing down at the person: a shaft and a triangular head, with thickness.</summary>
        private static Mesh BuildArrow()
        {
            MeshBuilder b = new MeshBuilder(1);
            b.Box(0, new Vector3(0f, 0.12f, 0f), new Vector3(0.09f, 0.22f, 0.05f), UVMode.Unit);
            Vector2 uv = Vector2.zero;
            Vector3 l = new Vector3(-0.16f, 0f, 0f), r = new Vector3(0.16f, 0f, 0f), tip = new Vector3(0f, -0.2f, 0f);
            Vector3 f = new Vector3(0f, 0f, 0.025f);
            b.Triangle(0, l + f, tip + f, r + f, Vector3.forward, uv, uv, uv);
            b.Triangle(0, l - f, r - f, tip - f, Vector3.back, uv, uv, uv);
            b.Quad(0, l - f, l + f, r + f, r - f, Vector3.up, uv, uv, uv, uv);
            Vector3 nl = Vector3.Cross(tip - l, Vector3.forward).normalized;
            b.Quad(0, l + f, l - f, tip - f, tip + f, nl, uv, uv, uv, uv);
            Vector3 nr = Vector3.Cross(r - tip, Vector3.forward).normalized;
            b.Quad(0, tip + f, tip - f, r - f, r + f, nr, uv, uv, uv, uv);
            return b.ToMesh("Giver Arrow 04E");
        }
    }
}
