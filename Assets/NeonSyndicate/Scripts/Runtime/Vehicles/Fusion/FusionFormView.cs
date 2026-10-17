using UnityEngine;
using UnityEngine.Rendering;

namespace NeonSyndicate
{
    /// <summary>
    /// The live fused form on a hover bike: the blueprint's parts as renderers under the bike's lean pivot (so they bank,
    /// bob and carry the rider anchors untouched), posed every frame from FormPose, plus the bounded effects of each
    /// form (glide trails at the Ventwing wing tips, the Coilrunner discharge arc, the Kilnback's flaring seams) and the
    /// form hum loop. Everything is a child of this object, so destroying it ends every effect and sound.
    /// No colliders: the physics body of the bike is unchanged, so a form can never wedge or pass through geometry.
    /// </summary>
    public sealed class FusionFormView : MonoBehaviour
    {
        public FormBlueprint Blueprint { get; private set; }
        public Vehicle Vehicle { get; private set; }
        /// <summary>The pose inputs the fusion system writes every frame.</summary>
        public FormPoseInput Pose;
        public int PartCount { get { return _parts != null ? _parts.Length : 0; } }

        private Transform[] _parts;
        private MeshRenderer[] _renderers;
        private TrailRenderer[] _trails;
        private LineRenderer _arc;
        private float _arcUntil;
        private float _arcTimer;
        private Vector3[] _arcTargets = new Vector3[0];
        private int _hum;
        private bool _hot;
        private float _smoothSpread, _smoothGlide, _smoothBrace, _smoothFlare;

        /// <summary>
        /// Builds a form on a bike, hidden (inactive) until Show. Throws when the bike has no lean pivot or a part fails,
        /// after removing whatever it built, so the caller can roll back.
        /// </summary>
        public static FusionFormView Build(Vehicle v, string form, bool low)
        {
            if (v == null || v.Parts == null || v.Parts.LeanRoot == null) throw new System.InvalidOperationException("the chassis has no lean pivot");
            string cls = FusionRules.ChassisClassOf(v.Def != null ? v.Def.Name : "");
            if (string.IsNullOrEmpty(cls)) throw new System.InvalidOperationException("this bike model has no coupler mount");
            FormBlueprint bp = FusionArt.Blueprint(form, cls, low, false);
            if (bp.Parts.Count == 0 || bp.VertexCount == 0) throw new System.InvalidOperationException("the " + form + " blueprint is empty");
            GameObject root = new GameObject("Fused Form " + FusionIds.Title(form));
            root.SetActive(false);
            root.transform.SetParent(v.Parts.LeanRoot, false);
            FusionFormView view = root.AddComponent<FusionFormView>();
            try
            {
                view.Setup(v, bp, low);
            }
            catch
            {
                Object.Destroy(root);
                throw;
            }
            return view;
        }

        private void Setup(Vehicle v, FormBlueprint bp, bool low)
        {
            Vehicle = v;
            Blueprint = bp;
            Material[] palette = FusionArt.Palette(bp.Form);
            _parts = new Transform[bp.Parts.Count];
            _renderers = new MeshRenderer[bp.Parts.Count];
            for (int i = 0; i < bp.Parts.Count; i++)
            {
                FormPart part = bp.Parts[i];
                Transform parent = part.Parent >= 0 && part.Parent < i ? _parts[part.Parent] : transform;
                Mesh mesh = FusionArt.PartMesh(bp, i);
                if (mesh == null || mesh.vertexCount == 0) throw new System.InvalidOperationException("part " + part.Name + " has no geometry");
                GameObject go = Shapes.MeshObject(part.Name, mesh, palette, parent, part.CastShadows && !low && Settings.Graphics >= GraphicsPreset.Medium);
                go.layer = Layers.Vehicles;
                go.transform.localPosition = part.Position;
                go.transform.localRotation = part.Rotation;
                _parts[i] = go.transform;
                _renderers[i] = go.GetComponent<MeshRenderer>();
            }
            if (bp.Form == FusionIds.Ventwing && bp.TrailParts.Count > 0)
            {
                _trails = new TrailRenderer[bp.TrailParts.Count];
                for (int t = 0; t < bp.TrailParts.Count; t++)
                {
                    int pi = bp.TrailParts[t];
                    FormPart tip = bp.Parts[pi];
                    GameObject tg = new GameObject("Glide Trail");
                    tg.transform.SetParent(_parts[pi], false);
                    tg.transform.localPosition = new Vector3(tip.Side * 0.4f * bp.Frame.Length / 2.15f, 0f, -0.12f);
                    TrailRenderer tr = tg.AddComponent<TrailRenderer>();
                    tr.time = 0.35f;
                    tr.minVertexDistance = 0.25f;
                    tr.widthMultiplier = 0.07f;
                    tr.sharedMaterial = FusionArt.Streak(new Color(0.6f, 0.9f, 1f, 0.7f));
                    tr.shadowCastingMode = ShadowCastingMode.Off;
                    tr.receiveShadows = false;
                    tr.emitting = false;
                    _trails[t] = tr;
                }
            }
            if (bp.Form == FusionIds.Coilrunner)
            {
                GameObject ag = new GameObject("Discharge Arc");
                ag.transform.SetParent(transform, false);
                _arc = ag.AddComponent<LineRenderer>();
                _arc.useWorldSpace = true;
                _arc.positionCount = 0;
                _arc.widthMultiplier = 0.035f;
                _arc.sharedMaterial = FusionArt.Streak(new Color(0.7f, 0.65f, 1f, 1f));
                _arc.shadowCastingMode = ShadowCastingMode.Off;
                _arc.receiveShadows = false;
                _arc.enabled = false;
            }
            Pose = FormPoseInput.Parked;
            Pose.Unfold = 0f;
            Apply(0f);
        }

        /// <summary>Shows the form at a transition progress (0 folded and small, 1 complete). Destroying the view hides it.</summary>
        public void Show(float unfold)
        {
            Pose.Unfold = Mathf.Clamp01(unfold);
            if (!gameObject.activeSelf) gameObject.SetActive(true);
            Apply(0f);
        }

        /// <summary>Starts the hum loop once the form is complete.</summary>
        public void StartHum()
        {
            if (_hum == 0) _hum = Underheart04ESounds.StartLoop(UhLoop.FormHum, transform.position, 0.45f);
        }

        /// <summary>Plays the Coilrunner discharge arc along the coil path to the struck targets for a moment.</summary>
        public void Discharge(Vector3[] targets)
        {
            if (_arc == null) return;
            _arcTargets = targets ?? new Vector3[0];
            _arcUntil = Time.time + 0.35f;
            _arcTimer = 0f;
            _arc.enabled = true;
        }

        /// <summary>Kilnback: the ember seams flare while bracing.</summary>
        public void SetHot(bool hot)
        {
            if (hot == _hot || Blueprint == null || Blueprint.Form != FusionIds.Kilnback) return;
            _hot = hot;
            Material[] palette = FusionArt.Palette(Blueprint.Form);
            Material[] mats = (Material[])palette.Clone();
            if (hot) mats[FormSlots.Crystal] = FusionArt.HotCrystal;
            for (int i = 0; i < _renderers.Length; i++) if (_renderers[i] != null) _renderers[i].sharedMaterials = mats;
        }

        /// <summary>Glide trails on or off (Ventwing).</summary>
        public void SetTrails(bool on)
        {
            if (_trails == null) return;
            for (int i = 0; i < _trails.Length; i++) if (_trails[i] != null) _trails[i].emitting = on;
        }

        private void LateUpdate()
        {
            Apply(Time.deltaTime);
            UpdateArc();
            if (_hum != 0)
            {
                float speed = Vehicle != null ? Vehicle.Speed : 0f;
                Underheart04ESounds.UpdateLoop(_hum, transform.position, 0.35f + Mathf.Min(0.3f, speed * 0.01f), 0.85f + Mathf.Min(0.5f, speed * 0.012f));
            }
        }

        /// <summary>Applies the pose (smoothed) to every part and the root scale.</summary>
        private void Apply(float dt)
        {
            if (_parts == null || Blueprint == null) return;
            float k = dt > 0f ? QMath.Damp(7f, dt) : 1f;
            _smoothSpread = Mathf.Lerp(_smoothSpread, Pose.Spread, k);
            _smoothGlide = Mathf.Lerp(_smoothGlide, Pose.Glide, k);
            _smoothBrace = Mathf.Lerp(_smoothBrace, Pose.Brace, dt > 0f ? QMath.Damp(14f, dt) : 1f);
            _smoothFlare = Mathf.Lerp(_smoothFlare, Pose.Flare, k);
            FormPoseInput p = Pose;
            p.Spread = _smoothSpread;
            p.Glide = _smoothGlide;
            p.Brace = _smoothBrace;
            p.Flare = _smoothFlare;
            for (int i = 0; i < _parts.Length; i++)
            {
                Transform t = _parts[i];
                if (t == null) continue;
                FormPart part = Blueprint.Parts[i];
                if (part.Role == FormPartRole.Static) continue;
                Vector3 move; Quaternion turn; float s;
                FormPose.Offset(part.Role, part.Side, part.Index, p, out move, out turn, out s);
                t.localPosition = part.Position + move;
                t.localRotation = part.Rotation * turn;
                t.localScale = Vector3.one * s;
            }
        }

        private void UpdateArc()
        {
            if (_arc == null || !_arc.enabled) return;
            if (Time.time > _arcUntil)
            {
                _arc.enabled = false;
                _arc.positionCount = 0;
                return;
            }
            _arcTimer -= Time.deltaTime;
            if (_arcTimer > 0f) return;
            _arcTimer = 0.05f;
            // A jagged line along the coil path, then out to each struck target (bounded point count).
            int pathCount = Blueprint.ArcPath.Count;
            int count = Mathf.Min(40, pathCount * 2 + _arcTargets.Length * 3);
            _arc.positionCount = count;
            int n = 0;
            for (int i = 0; i < pathCount && n < count; i++)
            {
                Vector3 w = transform.TransformPoint(Blueprint.ArcPath[i]);
                _arc.SetPosition(n++, w);
                if (n < count && i + 1 < pathCount)
                {
                    Vector3 next = transform.TransformPoint(Blueprint.ArcPath[i + 1]);
                    _arc.SetPosition(n++, Vector3.Lerp(w, next, 0.5f) + Random.insideUnitSphere * 0.05f);
                }
            }
            Vector3 emitter = pathCount > 0 ? transform.TransformPoint(Blueprint.ArcPath[pathCount - 1]) : transform.position;
            for (int t = 0; t < _arcTargets.Length && n + 2 < count; t++)
            {
                _arc.SetPosition(n++, Vector3.Lerp(emitter, _arcTargets[t], 0.5f) + Random.insideUnitSphere * 0.3f);
                _arc.SetPosition(n++, _arcTargets[t]);
                _arc.SetPosition(n++, emitter);
            }
            while (n < count) { _arc.SetPosition(n, emitter); n++; }
        }

        private void OnDestroy()
        {
            if (_hum != 0)
            {
                Underheart04ESounds.StopLoop(_hum);
                _hum = 0;
            }
        }
    }
}
