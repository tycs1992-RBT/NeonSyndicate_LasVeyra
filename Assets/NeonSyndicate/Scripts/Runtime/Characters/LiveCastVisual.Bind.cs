using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// LiveCastVisual, binding: the live contract, the prefab inspection before cloning, the bone mapping with the original
    /// bind rules (ImportedBindH3.Align, BindRotation04OF, ParentFirst04OF), complete skin weights (SkinWeightsH5), the
    /// pipeline check of every material, the ten sockets, renderer and emission caches, the duplicated costume layers and
    /// the animation cadence. Runs once per binding (never per frame); any refusal keeps the procedural body.
    /// </summary>
    public sealed partial class LiveCastVisual
    {
        private bool Initialize()
        {
            string reason;
            bool ok;
            try { ok = Bind(out reason); }
            catch (Exception e)
            {
                ok = false;
                reason = e.Message;
            }
            if (ok) return true;
            Fail(reason);
            return false;
        }

        private bool Bind(out string reason)
        {
            if (!LiveCastRegistry.Contract(_row, out reason)) return false;
            ImportedModel04OD model = _row.Model;
            _rig = _owner.Rig;
            if (_rig == null || _rig.Bones == null || _rig.Layout == null || _rig.Body == null) { reason = "the procedural rig is not built."; return false; }
            if (_rig.Bones.Length != RigLayout.BoneCount)
            { reason = "this actor has the reduced 17-bone body (the Low graphics preset). Choose Medium or higher and let the character respawn."; return false; }
            if (model.Prefab == null) { reason = "the prepared prefab is missing (deleted, or the art was never prepared on this machine)."; return false; }
            _complete = model.PreserveInfluencesH5;
            if (_complete && LiveCastRules.SkinWeightStep(true, LiveCastQuality.IsUnlimited, false) == LiveCastSkinStep.Raise
                && LiveCastRules.SkinWeightStep(true, LiveCastQuality.Ensure(), true) == LiveCastSkinStep.FailClosed)
            { reason = SkinWeightsRefused; return false; }
            // Inspect the prefab before cloning: Instantiate may already run Awake and OnEnable on scripts.
            foreach (Transform node in model.Prefab.GetComponentsInChildren<Transform>(true))
                if (!ImportedModelRegistry04OD.ValidLocalScale04OG(node.localScale)) { reason = "mirrored, collapsed or nonuniform transform " + node.name + ". Re-export with applied transforms."; return false; }
            foreach (Component part in model.Prefab.GetComponentsInChildren<Component>(true))
            {
                if (part == null || !ImportedArt04OH.VisualComponent(part.GetType()))
                { reason = "the prefab contains an unsupported component. Only transforms, mesh renderers, mesh filters, a disabled Animator and the LOD group are allowed."; return false; }
                Animator animator = part as Animator;
                if (animator != null && animator.runtimeAnimatorController != null) { reason = "the prefab carries an Animator controller; the procedural pose is the only animation owner."; return false; }
            }
            _visual = Instantiate(model.Prefab, transform, false);
            _visual.name = "Live visual / " + Label;
            _visual.transform.localPosition = Vector3.zero;
            _visual.transform.localRotation = Quaternion.identity;
            _visual.transform.localScale = Vector3.one * model.Scale;
            if (_visual.GetComponentsInChildren<Collider>(true).Length > 0 || _visual.GetComponentsInChildren<Rigidbody>(true).Length > 0 || _visual.GetComponentsInChildren<MonoBehaviour>(true).Length > 0)
            { reason = "the visual must not contain physics or gameplay scripts."; return false; }
            foreach (Animator animator in _visual.GetComponentsInChildren<Animator>(true))
            {
                animator.enabled = false;
                animator.applyRootMotion = false;
            }
            SkinnedMeshRenderer[] skins = _visual.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            if (skins.Length == 0) { reason = "the visual has no skinned mesh."; return false; }
            if (!MapBones(model, out reason) || !CheckSkins(model, skins, out reason) || !CheckSockets(model, out reason)) return false;
            foreach (Transform child in _visual.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = Layers.IgnoreRaycast;
            CacheRenderers();
            _block = new MaterialPropertyBlock();
            _glowId = Shader.PropertyToID("_Glow");
            _flashId = Shader.PropertyToID("_HitFlash");
            _emissionId = Shader.PropertyToID("_EmissionColor");
            _source = _rig.Bones;
            _body = _rig.Body;
            _bodyForcedPrior = _body.forceRenderingOff;
            CaptureCostume();
            TakeCadence();
            _ready = true;
            _applied = false;
            _hidingBody = false;
            _needPose = true;
            _sentGlow = -1f;
            _sentFlash = -1f;
            Log.Info("Live character: " + Label + " now draws " + (_owner.IsPlayer ? "the player" : _identity) + " (" + _row.Origin + ", " + skins.Length + " skins). The procedural body stays the fallback.");
            reason = "";
            return true;
        }

        /// <summary>Maps every original pose bone to its imported bone and precomputes the constant bind offsets and order.</summary>
        private bool MapBones(ImportedModel04OD model, out string reason)
        {
            reason = "";
            int count = _rig.Bones.Length;
            _target = new Transform[count];
            Quaternion[] sourceBind = new Quaternion[count], targetBind = new Quaternion[count];
            Quaternion root = transform.rotation;
            foreach (ImportedBone04OD bone in model.Bones)
            {
                Transform target = _visual.transform.Find(bone.TargetPath);
                if (target == null) { reason = "missing target bone " + bone.TargetPath + "."; return false; }
                // Both skeletons have identity bind rotations at rest. Never take a posed frame as the bind pose.
                _target[bone.SourceBone] = target;
                sourceBind[bone.SourceBone] = root;
                targetBind[bone.SourceBone] = target.rotation;
            }
            for (int i = 0; i < count; i++)
                if (_target[i] == null) { reason = "original pose bone " + RigLayout.Names[i] + " is not mapped."; return false; }
            if (model.AlignBindAxesH3)
            {
                Vector3[] positions = new Vector3[count];
                for (int i = 0; i < count; i++) positions[i] = _target[i].position;
                targetBind = ImportedBindH3.Align(_rig.Layout.World, RigLayout.Parent, positions, targetBind, root);
            }
            // BindRotation04OF(source, rootBind, targetBind) = source * inverse(rootBind) * targetBind; the last two are constant.
            _offset = new Quaternion[count];
            for (int i = 0; i < count; i++) _offset[i] = ImportedModelRegistry04OD.BindRotation04OF(Quaternion.identity, sourceBind[i], targetBind[i]);
            int[] depths = new int[count];
            for (int i = 0; i < count; i++)
            {
                Transform parent = _target[i].parent;
                while (parent != null && parent != _visual.transform)
                {
                    depths[i]++;
                    parent = parent.parent;
                }
            }
            _order = ImportedModelRegistry04OD.ParentFirst04OF(depths);
            return true;
        }

        /// <summary>Complete, normalized weights; every weighted bone mapped or a declared passive bone below a mapped one;
        /// every material drawable under the active pipeline. The weight tables are measured once per row.</summary>
        private bool CheckSkins(ImportedModel04OD model, SkinnedMeshRenderer[] skins, out string reason)
        {
            reason = "";
            HashSet<Transform> mapped = new HashSet<Transform>(_target);
            HashSet<Transform> passive = new HashSet<Transform>();
            if (model.PassiveBonePaths != null)
                foreach (string path in model.PassiveBonePaths)
                {
                    Transform bone = string.IsNullOrWhiteSpace(path) ? null : _visual.transform.Find(path);
                    if (bone != null) passive.Add(bone);
                }
            bool[][] cache = _row.ReferencedCache;
            bool fresh = cache == null || cache.Length != skins.Length;
            if (fresh) cache = new bool[skins.Length][];
            bool urp = RenderPath.IsURP;
            for (int s = 0; s < skins.Length; s++)
            {
                SkinnedMeshRenderer skin = skins[s];
                Transform[] bones = skin.bones;
                if (skin.sharedMesh == null || bones.Length == 0 || skin.sharedMesh.bindposes.Length != bones.Length)
                { reason = "mesh, skin bones or matching bind poses are missing on " + skin.name + "."; return false; }
                if (model.PreserveInfluencesH5) skin.quality = SkinQuality.Auto;
                if (fresh || cache[s] == null || cache[s].Length != bones.Length)
                    cache[s] = model.PreserveInfluencesH5 ? SkinWeightsH5.Referenced(skin.sharedMesh, bones.Length) : ImportedBindH3.ReferencedBones(skin.sharedMesh.boneWeights, bones.Length);
                bool[] referenced = cache[s];
                for (int b = 0; b < bones.Length; b++)
                {
                    if (!referenced[b]) continue;
                    Transform bone = bones[b];
                    if (bone == null || (!mapped.Contains(bone) && !Rides(bone, passive, mapped)))
                    { reason = "weighted bone " + (bone != null ? bone.name : "(missing)") + " on " + skin.name + " is neither mapped nor a declared passive bone below a mapped one."; return false; }
                }
                foreach (Material material in skin.sharedMaterials)
                    if (material == null || material.shader == null || !LiveCastRules.ShaderFits(material.shader.name, material.shader.isSupported, urp))
                    { reason = "material " + (material != null ? material.name : "(missing)") + " cannot draw under the " + (urp ? "URP" : "Built-in") + " pipeline. Rebuild the live characters after switching pipelines."; return false; }
            }
            _row.ReferencedCache = cache;
            return true;
        }

        /// <summary>A declared passive bone rides a mapped ancestor with its bind-local offset.</summary>
        private bool Rides(Transform bone, HashSet<Transform> passive, HashSet<Transform> mapped)
        {
            if (!passive.Contains(bone)) return false;
            for (Transform parent = bone.parent; parent != null && parent != _visual.transform; parent = parent.parent)
                if (mapped.Contains(parent)) return true;
            return false;
        }

        /// <summary>All ten socket locators exist on the clone (the original humanoid adapter's requirement).</summary>
        private bool CheckSockets(ImportedModel04OD model, out string reason)
        {
            reason = "";
            foreach (string role in DanteReviewH8S.SocketRoles)
            {
                bool found = false;
                if (model.Sockets != null)
                    foreach (ImportedSocket04OD socket in model.Sockets)
                        if (socket != null && socket.Role == role && CinematicAcceptance04OF.RelativeBonePath(socket.Path) && _visual.transform.Find(socket.Path) != null) { found = true; break; }
                if (!found) { reason = "missing visual socket " + role + "."; return false; }
            }
            return true;
        }

        /// <summary>Caches every renderer and each material's authored emission (per material, so one glowing part stays alone).</summary>
        private void CacheRenderers()
        {
            _renderers = _visual.GetComponentsInChildren<Renderer>(true);
            _materialStart = new int[_renderers.Length];
            _materialCount = new int[_renderers.Length];
            Material[][] materials = new Material[_renderers.Length][];
            int total = 0;
            for (int r = 0; r < _renderers.Length; r++)
            {
                materials[r] = _renderers[r].sharedMaterials;
                _materialStart[r] = total;
                _materialCount[r] = materials[r].Length;
                total += materials[r].Length;
            }
            _emission = new Color[total];
            for (int r = 0; r < _renderers.Length; r++)
                for (int m = 0; m < materials[r].Length; m++)
                {
                    Material material = materials[r][m];
                    _emission[_materialStart[r] + m] = material != null && material.HasProperty("_EmissionColor") ? material.GetColor("_EmissionColor") : Color.black;
                }
        }

        /// <summary>
        /// Finds the procedural costume layers the row hides (CastCostumeBuilder04E parents each directly under its bone with
        /// a fixed name). The held tool is never collected, so the hammer stays in Dante's hand.
        /// </summary>
        private void CaptureCostume()
        {
            _costume = null;
            _costumeForcedPrior = null;
            if (_row.HiddenCostume == null || _row.HiddenCostume.Length == 0) return;
            List<MeshRenderer> found = new List<MeshRenderer>();
            for (int b = 0; b < _rig.Bones.Length; b++)
            {
                Transform bone = _rig.Bones[b];
                if (bone == null) continue;
                for (int c = 0; c < bone.childCount; c++)
                {
                    Transform child = bone.GetChild(c);
                    MeshRenderer layer;
                    if (LiveCastRules.HidesCostumeLayer(_row.HiddenCostume, child.name) && child.TryGetComponent(out layer)) found.Add(layer);
                }
            }
            _costume = found.ToArray();
            _costumeForcedPrior = new bool[_costume.Length];
            for (int i = 0; i < _costume.Length; i++) _costumeForcedPrior[i] = _costume[i].forceRenderingOff;
        }

        /// <summary>
        /// A forced-off procedural body reports unseen, and the original animator would then refresh an NPC's pose only every
        /// eighth frame. NPC bindings borrow the original cadence flag for their whole life (the player always animates).
        /// </summary>
        private void TakeCadence()
        {
            if (_owner == null || _owner.IsPlayer || _owner.Anim == null || _cadence.Owned) return;
            _owner.Anim.AlwaysAnimate = _cadence.Take(_owner.Anim.AlwaysAnimate);
        }

        /// <summary>Gives the cadence flag back. A review that borrowed it later restores it itself; never write underneath it.</summary>
        private void ReleaseCadence()
        {
            if (!_cadence.Owned) return;
            HumanoidAnimator anim = _owner != null ? _owner.Anim : null;
            bool restore = _cadence.Release(anim != null && anim.AlwaysAnimate);
            if (anim != null && !ReviewOwnsActor()) anim.AlwaysAnimate = restore;
        }
    }
}
