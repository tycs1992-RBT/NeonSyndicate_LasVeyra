using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace NeonSyndicate
{
    /// <summary>
    /// Turns a CreatureBody into a live creature: one Transform per bone (identity bind rotations at the skeleton's
    /// offsets), one SkinnedMeshRenderer with the shared character material (one draw call), and a collider per hit
    /// volume on the Characters layer under a kinematic Rigidbody, so bullets, blades and explosions find it like they
    /// find people. Each frame the Creature hands it a solved CreaturePose to copy onto the bones (far creatures less
    /// often). Also the per-renderer hit flash and telegraph glow (MaterialPropertyBlock, like HumanoidRig), the Hollow
    /// Shade's near-invisible ghost look, and the shrink-and-sink of a death dissolve.
    /// </summary>
    public sealed class CreatureRig : MonoBehaviour
    {
        public CreatureBody Body;
        public CreaturePose Pose;
        public Transform[] Bones;
        public SkinnedMeshRenderer Renderer;
        public readonly List<Collider> Colliders = new List<Collider>();
        /// <summary>Hit volume index (into Body.HitVolumes) for each collider in Colliders.</summary>
        public readonly List<int> ColliderVolume = new List<int>();
        /// <summary>Indices into Body.HitVolumes that are weak points (glowing crystal cores).</summary>
        public int[] WeakVolumes = new int[0];
        public Transform AimPoint, HeadPoint, MouthPoint;
        public bool SharedMesh { get; private set; }

        private MaterialPropertyBlock _mpb;
        private float _flash;
        private float _glow = 1f;
        private float _sentFlash = -1f, _sentGlow = -1f, _sentVis = -1f;
        private float _visibility = 1f;
        private bool _ghost;
        private bool _castShadows = true;
        private Material _normalMat;
        private Color _ghostTint = new Color(0.7f, 0.5f, 1f, 1f);

        private static Material _ghostMat;
        private static readonly int FlashId = Shader.PropertyToID("_HitFlash");
        private static readonly int GlowId = Shader.PropertyToID("_Glow");
        private static readonly int TintId = Shader.PropertyToID("_TintColor");
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        public static void ResetStatics()
        {
            _ghostMat = null;
        }

        /// <summary>The faint additive look of a stealthed Hollow Shade (shared by every shade).</summary>
        private static Material GhostMaterial
        {
            get
            {
                if (_ghostMat == null) _ghostMat = Mats.Additive("Creature Ghost", new Color(1f, 1f, 1f, 1f), Tex.CharacterPalette());
                return _ghostMat;
            }
        }

        /// <summary>Builds the rig on root. sharedMesh may carry a ready mesh made from the same body (swarms share one).</summary>
        public static CreatureRig Build(GameObject root, CreatureBody body, Mesh sharedMesh, bool castShadows)
        {
            CreatureRig r = root.AddComponent<CreatureRig>();
            r.Construct(body, sharedMesh, castShadows);
            return r;
        }

        private void Construct(CreatureBody body, Mesh sharedMesh, bool castShadows)
        {
            Body = body;
            _castShadows = castShadows;
            CreatureSkeleton sk = body.Skeleton;
            int n = sk.Count;
            Bones = new Transform[n];
            int layer = gameObject.layer;
            for (int i = 0; i < n; i++)
            {
                int p = sk.Parents[i];
                Transform parent = p < 0 ? transform : Bones[p];
                GameObject go = new GameObject(sk.Names[i]);
                go.layer = layer;
                go.transform.SetParent(parent, false);
                go.transform.localPosition = sk.LocalPositions[i];
                Bones[i] = go.transform;
            }
            Mesh mesh = sharedMesh;
            SharedMesh = sharedMesh != null;
            if (mesh == null) mesh = OwnedMeshes.Track(gameObject, MakeMesh(body));
            GameObject skin = new GameObject("Body");
            skin.layer = layer;
            skin.transform.SetParent(transform, false);
            Renderer = skin.AddComponent<SkinnedMeshRenderer>();
            Renderer.sharedMesh = mesh;
            Renderer.bones = Bones;
            // The existing QuartzRegalia mesh gets a private 01B surface. Its bones, UVs and hit volumes stay intact.
            _normalMat = body.Look != null && body.Look.CrystalRegalia ? CrystalDrakeSurface.Get() : Mats.CharacterMaterial;
            Renderer.sharedMaterial = _normalMat;
            Renderer.updateWhenOffscreen = false;
            // The pose box holds the body in every pose the solver makes (wings up, coils, death sprawl), in the
            // creature's own space; with no root bone the renderer measures it from its own (identity) transform.
            Renderer.localBounds = body.PoseBounds.size.sqrMagnitude > 1e-6f ? body.PoseBounds : body.Bounds;
            Renderer.shadowCastingMode = castShadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
            Renderer.receiveShadows = true;
            Renderer.quality = Settings.Graphics >= GraphicsPreset.High ? SkinQuality.Bone4 : SkinQuality.Bone2;
            Renderer.skinnedMotionVectors = true;
            BuildColliders(body, layer);
            BuildAnchors(body);
            Pose = new CreaturePose(body);
        }

        /// <summary>The Unity mesh for a body. Bind poses come straight from the skeleton (identity rotations, so each is a translation).</summary>
        public static Mesh MakeMesh(CreatureBody body)
        {
            BodyData d = body.Mesh;
            Mesh mesh = new Mesh();
            mesh.name = "Creature";
            if (d.Vertices.Count > 65000) mesh.indexFormat = IndexFormat.UInt32;
            mesh.SetVertices(d.Vertices);
            mesh.SetNormals(d.Normals);
            mesh.SetUVs(0, d.Palette);
            mesh.SetUVs(1, d.Detail);
            if (d.Tangents.Count == d.Vertices.Count) mesh.SetTangents(d.Tangents);
            mesh.boneWeights = d.Weights.ToArray();
            mesh.SetTriangles(d.Triangles, 0);
            CreatureSkeleton sk = body.Skeleton;
            Matrix4x4[] bind = new Matrix4x4[sk.Count];
            for (int i = 0; i < sk.Count; i++) bind[i] = Matrix4x4.Translate(-sk.ModelPosition(i));
            mesh.bindposes = bind;
            mesh.RecalculateBounds();
            return mesh;
        }

        private void BuildColliders(CreatureBody body, int layer)
        {
            List<int> weak = new List<int>();
            for (int i = 0; i < body.HitVolumes.Count; i++)
            {
                CreatureHitVolume h = body.HitVolumes[i];
                if (h.Bone < 0 || h.Bone >= Bones.Length || h.Radius <= 0f) continue;
                GameObject go = new GameObject(h.WeakPoint ? "Weak Point" : "Hit");
                go.layer = layer;
                go.transform.SetParent(Bones[h.Bone], false);
                go.transform.localPosition = h.Center;
                Vector3 axis = h.Axis.sqrMagnitude > 1e-6f ? h.Axis.normalized : Vector3.up;
                go.transform.localRotation = QMath.FromTo(Vector3.up, axis);
                Collider c;
                if (h.Length <= h.Radius * 2.02f)
                {
                    SphereCollider s = go.AddComponent<SphereCollider>();
                    s.radius = h.Radius;
                    c = s;
                }
                else
                {
                    CapsuleCollider cap = go.AddComponent<CapsuleCollider>();
                    cap.direction = 1;
                    cap.radius = h.Radius;
                    cap.height = h.Length;
                    c = cap;
                }
                Colliders.Add(c);
                ColliderVolume.Add(i);
                if (h.WeakPoint) weak.Add(i);
            }
            WeakVolumes = weak.ToArray();
            Rigidbody rb = gameObject.GetComponent<Rigidbody>();
            if (rb == null) rb = gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.interpolation = RigidbodyInterpolation.None;
        }

        private void BuildAnchors(CreatureBody body)
        {
            CreatureSkeleton sk = body.Skeleton;
            AimPoint = Anchor("Aim", body.AimPoint, new[] { sk.Chest, sk.Pelvis, sk.Root, sk.Head });
            HeadPoint = Anchor("Head Point", body.HeadPoint, new[] { sk.Head, sk.Chest, sk.Root });
            MouthPoint = Anchor("Mouth", body.MouthPoint, new[] { sk.Jaw, sk.Head, sk.Chest, sk.Root });
        }

        /// <summary>An empty transform at a model-space point, parented to the nearest of the candidate bones.</summary>
        private Transform Anchor(string name, Vector3 model, int[] candidates)
        {
            int best = -1;
            float bd = float.MaxValue;
            for (int i = 0; i < candidates.Length; i++)
            {
                int b = candidates[i];
                if (b < 0 || b >= Bones.Length) continue;
                float d = (Body.Skeleton.ModelPosition(b) - model).sqrMagnitude;
                if (d < bd) { bd = d; best = b; }
            }
            GameObject go = new GameObject(name);
            go.layer = gameObject.layer;
            if (best >= 0)
            {
                go.transform.SetParent(Bones[best], false);
                go.transform.localPosition = model - Body.Skeleton.ModelPosition(best);
            }
            else
            {
                go.transform.SetParent(transform, false);
                go.transform.localPosition = model;
            }
            return go.transform;
        }

        // ================================================================== per frame

        /// <summary>Copies the solved pose onto the bone transforms.</summary>
        public void ApplyPose()
        {
            Quaternion[] r = Pose.LocalRotations;
            Vector3[] p = Pose.LocalPositions;
            for (int i = 0; i < Bones.Length; i++) Bones[i].SetLocalPositionAndRotation(p[i], r[i]);
        }

        /// <summary>A white-hot flash across the body on a hit (decays on its own).</summary>
        public void HitFlash(float amount)
        {
            _flash = Mathf.Max(_flash, Mathf.Clamp01(amount));
        }

        /// <summary>Seam and eye glow (1 normal; wind-ups ramp it up so attacks read from across a fight).</summary>
        public void SetGlow(float k)
        {
            _glow = k;
        }

        /// <summary>0 invisible to 1 solid. Below 1 the body draws as a faint additive ghost without shadows (Hollow Shade).</summary>
        public void SetVisibility(float v, Color ghostTint)
        {
            _visibility = Mathf.Clamp01(v);
            _ghostTint = ghostTint;
            bool ghost = _visibility < 0.97f;
            if (ghost == _ghost || Renderer == null) return;
            _ghost = ghost;
            Renderer.sharedMaterial = ghost ? GhostMaterial : _normalMat;
            Renderer.shadowCastingMode = ghost || !_castShadows ? ShadowCastingMode.Off : ShadowCastingMode.On;
            _sentFlash = -1f;
        }

        public bool IsGhost { get { return _ghost; } }

        public void SetCollidersEnabled(bool on)
        {
            for (int i = 0; i < Colliders.Count; i++) if (Colliders[i] != null) Colliders[i].enabled = on;
        }

        /// <summary>Death dissolve: shrinks the whole body toward its root bone (0 untouched, 1 gone).</summary>
        public void SetDissolve(float k)
        {
            if (Body == null || Body.Skeleton.Root < 0) return;
            float s = Mathf.Lerp(1f, 0.05f, Mathf.Clamp01(k));
            Bones[Body.Skeleton.Root].localScale = new Vector3(s, s, s);
        }

        /// <summary>World position of a hit volume's centre.</summary>
        public Vector3 VolumeCenter(int volume)
        {
            CreatureHitVolume h = Body.HitVolumes[volume];
            return Bones[h.Bone].TransformPoint(h.Center);
        }

        /// <summary>Distance from a world point to the surface of a hit volume (negative inside).</summary>
        public float DistanceToVolume(int volume, Vector3 world)
        {
            CreatureHitVolume h = Body.HitVolumes[volume];
            Transform b = Bones[h.Bone];
            Vector3 c = b.TransformPoint(h.Center);
            float half = Mathf.Max(0f, h.Length * 0.5f - h.Radius);
            Vector3 axis = b.TransformDirection(h.Axis.sqrMagnitude > 1e-6f ? h.Axis.normalized : Vector3.up);
            float t;
            Vector3 q = half > 0f ? MathUtil.ClosestPointOnSegment(c - axis * half, c + axis * half, world, out t) : c;
            return (world - q).magnitude - h.Radius * b.lossyScale.x;
        }

        /// <summary>True when a world point lies on (or within margin of) a weak point.</summary>
        public bool OnWeakPoint(Vector3 world, float margin)
        {
            for (int i = 0; i < WeakVolumes.Length; i++) if (DistanceToVolume(WeakVolumes[i], world) <= margin) return true;
            return false;
        }

        /// <summary>Bone index of the hit volume nearest to a world point (-1 when there are none).</summary>
        public int NearestVolumeBone(Vector3 world)
        {
            int best = -1;
            float bd = float.MaxValue;
            for (int i = 0; i < ColliderVolume.Count; i++)
            {
                float d = DistanceToVolume(ColliderVolume[i], world);
                if (d < bd) { bd = d; best = Body.HitVolumes[ColliderVolume[i]].Bone; }
            }
            return best;
        }

        private void LateUpdate()
        {
            if (Renderer == null) return;
            _flash = Mathf.MoveTowards(_flash, 0f, Time.deltaTime * 6f);
            if (Mathf.Abs(_flash - _sentFlash) < 0.01f && Mathf.Abs(_glow - _sentGlow) < 0.01f && Mathf.Abs(_visibility - _sentVis) < 0.01f) return;
            if (_mpb == null) _mpb = new MaterialPropertyBlock();
            _sentFlash = _flash;
            _sentGlow = _glow;
            _sentVis = _visibility;
            Renderer.GetPropertyBlock(_mpb);
            _mpb.SetFloat(FlashId, _flash);
            _mpb.SetFloat(GlowId, _glow);
            if (_ghost)
            {
                float a = Mathf.Clamp01(_visibility + _flash * 0.6f) * 0.55f;
                Color c = new Color(_ghostTint.r * a, _ghostTint.g * a, _ghostTint.b * a, a);
                _mpb.SetColor(TintId, c);
                _mpb.SetColor(BaseColorId, c);
                _mpb.SetColor(ColorId, c);
            }
            Renderer.SetPropertyBlock(_mpb);
        }
    }
}
