using UnityEngine;
using UnityEngine.Rendering;

namespace NeonSyndicate
{
    /// <summary>
    /// A skinned humanoid: 17 body and 30 articulated finger transforms (animated procedurally, turned into a physics ragdoll) and one
    /// SkinnedMeshRenderer with one material (one draw call). The body itself is a smooth lofted mesh from
    /// BodyBuilder with four-bone skinning, so joints bend like flesh instead of boxes.
    /// </summary>
    public sealed class HumanoidRig : MonoBehaviour
    {
        public Transform Hips, Spine, Chest, Neck, Head;
        public Transform UpperArmL, ForearmL, HandL, UpperArmR, ForearmR, HandR;
        public Transform ThighL, ShinL, FootL, ThighR, ShinR, FootR;
        public SkinnedMeshRenderer Body;
        public Appearance Look;
        public RigLayout Layout;
        public float Scale = 1f;
        public Transform[] Bones;
        public Vector3[] BindPositions;
        public Quaternion[] BindRotations;
        public float HipsHeight;
        /// <summary>Mesh detail: 0 crowd, 1 standard, 2 hero.</summary>
        public int Detail = 1;
        private MaterialPropertyBlock _mpb;
        private float _hitFlash;
        private float _glowPulse = 1f;

        public static HumanoidRig Build(GameObject root, Appearance look)
        {
            return Build(root, look, 1);
        }

        public static HumanoidRig Build(GameObject root, Appearance look, int detail)
        {
            HumanoidRig rig = root.AddComponent<HumanoidRig>();
            rig.Look = look;
            rig.Detail = detail;
            rig.Construct();
            return rig;
        }

        private Transform Bone(string name, Transform parent, Vector3 localPos)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.layer = gameObject.layer;
            return go.transform;
        }

        private void Construct()
        {
            Layout = RigLayout.From(Look);
            Scale = Layout.Scale;
            HipsHeight = Layout.Local[RigLayout.Hips].y;
            // Distant crowd and legacy humanoid monsters keep the original 17-transform budget.
            // Only detailed human meshes contain weighted digit vertices.
            int count = Detail == 0 || Look.Creature != HumanoidCreature.None ? RigLayout.BodyBoneCount : RigLayout.BoneCount;
            Bones = new Transform[count];
            for (int i = 0; i < count; i++)
            {
                Transform parent = RigLayout.Parent[i] < 0 ? transform : Bones[RigLayout.Parent[i]];
                Bones[i] = Bone(RigLayout.Names[i], parent, Layout.Local[i]);
            }
            Hips = Bones[RigLayout.Hips]; Spine = Bones[RigLayout.Spine]; Chest = Bones[RigLayout.Chest]; Neck = Bones[RigLayout.Neck]; Head = Bones[RigLayout.Head];
            UpperArmL = Bones[RigLayout.UpperArmL]; ForearmL = Bones[RigLayout.ForearmL]; HandL = Bones[RigLayout.HandL];
            UpperArmR = Bones[RigLayout.UpperArmR]; ForearmR = Bones[RigLayout.ForearmR]; HandR = Bones[RigLayout.HandR];
            ThighL = Bones[RigLayout.ThighL]; ShinL = Bones[RigLayout.ShinL]; FootL = Bones[RigLayout.FootL];
            ThighR = Bones[RigLayout.ThighR]; ShinR = Bones[RigLayout.ShinR]; FootR = Bones[RigLayout.FootR];
            BindPositions = new Vector3[Bones.Length];
            BindRotations = new Quaternion[Bones.Length];
            for (int i = 0; i < Bones.Length; i++)
            {
                BindPositions[i] = Bones[i].localPosition;
                BindRotations[i] = Bones[i].localRotation;
            }
            BuildMesh();
        }

        private void BuildMesh()
        {
            // Ambient characters usually arrive with their body already built on a worker thread.
            BodyData d = BodyFactory.TakePrebuilt(Look, Detail) ?? BodyBuilder.Build(Look, Layout, Detail);
            Mesh mesh = new Mesh();
            mesh.name = "Humanoid";
            if (d.Vertices.Count > 65000) mesh.indexFormat = IndexFormat.UInt32;
            mesh.SetVertices(d.Vertices);
            mesh.SetNormals(d.Normals);
            mesh.SetUVs(0, d.Palette);
            mesh.SetUVs(1, d.Detail);
            mesh.SetTangents(d.Tangents);
            mesh.boneWeights = d.Weights.ToArray();
            mesh.SetTriangles(d.Triangles, 0);
            mesh = OwnedMeshes.Track(gameObject, mesh);
            // Build while the rig is in its bind pose (identity rotations): that is what bind poses must capture.
            Matrix4x4[] bind = new Matrix4x4[Bones.Length];
            for (int i = 0; i < Bones.Length; i++) bind[i] = Bones[i].worldToLocalMatrix * transform.localToWorldMatrix;
            mesh.bindposes = bind;
            mesh.RecalculateBounds();

            GameObject skinGo = new GameObject("Body");
            skinGo.layer = gameObject.layer;
            skinGo.transform.SetParent(transform, false);
            Body = skinGo.AddComponent<SkinnedMeshRenderer>();
            Body.sharedMesh = mesh;
            Body.bones = Bones;
            Body.rootBone = Hips;
            Body.sharedMaterial = Mats.CharacterMaterial;
            Body.updateWhenOffscreen = false;
            // Creatures can be several times a person's size (and carry crests and tails), so their box grows with them.
            float boundsScale = Layout.Creature != HumanoidCreature.None ? Mathf.Max(1f, Layout.Scale * 1.15f) : 1f;
            Body.localBounds = new Bounds(Vector3.zero, new Vector3(2.4f, 2.6f, 2.4f) * boundsScale);
            Body.shadowCastingMode = ShadowCastingMode.On;
            Body.quality = Settings.Graphics >= GraphicsPreset.High || Detail == 2 ? SkinQuality.Bone4 : SkinQuality.Bone2;
            Body.skinnedMotionVectors = true;
            _sentFlash = -1f;
            _sentGlow = -1f;
        }

        /// <summary>Rebuilds the body mesh with a new appearance (clothing store, gear). Bones and pose are kept.</summary>
        public void ChangeLook(Appearance look)
        {
            // The player's equipped armour always shows, whatever outfit is worn.
            if (Game.Player != null && Game.Player.Rig == this && Game.Gear != null) look = Game.Gear.MergeVisuals(look);
            Quaternion[] pose = new Quaternion[Bones.Length];
            Vector3 hips = Hips.localPosition;
            for (int i = 0; i < Bones.Length; i++) pose[i] = Bones[i].localRotation;
            ResetPose();
            Look = look;
            if (Body != null)
            {
                Mesh old = Body.sharedMesh;
                Destroy(Body.gameObject);
                if (old != null) OwnedMeshes.Release(gameObject, old);
            }
            BuildMesh();
            for (int i = 0; i < Bones.Length; i++) Bones[i].localRotation = pose[i];
            Hips.localPosition = hips;
        }

        /// <summary>Restores the bind pose (used after a ragdoll gets back up).</summary>
        public void ResetPose()
        {
            for (int i = 0; i < Bones.Length; i++)
            {
                if (Bones[i] == null) continue;
                Bones[i].localPosition = BindPositions[i];
                Bones[i].localRotation = BindRotations[i];
                Bones[i].localScale = Vector3.one;
            }
        }

        public void SetVisible(bool visible, bool shadowsOnlyWhenHidden)
        {
            if (Body == null) return;
            if (visible)
            {
                Body.enabled = true;
                Body.shadowCastingMode = ShadowCastingMode.On;
            }
            else if (shadowsOnlyWhenHidden)
            {
                Body.enabled = true;
                Body.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
            }
            else
            {
                Body.enabled = false;
            }
        }

        /// <summary>A white-hot flash on the body when struck (decays on its own).</summary>
        public void HitFlash(float amount)
        {
            _hitFlash = Mathf.Max(_hitFlash, amount);
        }

        /// <summary>Implant and seam glow intensity (pulses for enemy wind-ups and elites).</summary>
        public void SetGlow(float k)
        {
            _glowPulse = k;
        }

        private float _sentFlash = -1f, _sentGlow = -1f;

        private void LateUpdate()
        {
            if (Body == null) return;
            _hitFlash = Mathf.MoveTowards(_hitFlash, 0f, Time.deltaTime * 6f);
            if (Mathf.Abs(_hitFlash - _sentFlash) < 0.01f && Mathf.Abs(_glowPulse - _sentGlow) < 0.01f) return;
            if (_mpb == null) _mpb = new MaterialPropertyBlock();
            _sentFlash = _hitFlash;
            _sentGlow = _glowPulse;
            Body.GetPropertyBlock(_mpb);
            _mpb.SetFloat("_HitFlash", _hitFlash);
            _mpb.SetFloat("_Glow", _glowPulse);
            Body.SetPropertyBlock(_mpb);
        }
    }
}
