using System;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// LiveCastVisual, crowd bodies (owner direction in chat 2026-10-23: swap NPCs and enemies for realistic bodies). An NPC
    /// that no named row serves may be served by a crowd row (LiveCrowdRules: by role and sex, never a creature, companion,
    /// named cast actor or extreme build). A crowd adapter waits for the budget (LiveCrowd) before it binds, binds a pooled
    /// body when one is free (measured once, re-posed to rest, only the person's bind offsets recomputed), gives the body
    /// back when the budget withdraws it, carries the person's own colours in the same property blocks as the hit flash
    /// and glow, and hides the armour pieces on plain roles. Everything else (pose copy, mirroring, review yield, failure)
    /// is the ordinary live adapter.
    /// </summary>
    public sealed partial class LiveCastVisual
    {
        private bool _crowd, _crowdGranted;
        private NpcRole _crowdRole;
        private LiveCrowdPuppet _puppet;
        private bool[] _partShown;
        private Color[] _tints;
        private int _colorId, _baseColorId;

        /// <summary>True while this adapter draws (or is bound to draw) a crowd body; the budget counts it as a holder.</summary>
        internal bool CrowdBound { get { return _crowd && _ready; } }

        /// <summary>The person's distance from the camera for the budget.</summary>
        internal float CrowdDistance(Vector3 eye)
        {
            return (transform.position - eye).magnitude;
        }

        /// <summary>The budget's verdict for this tick: bind (or keep) the body, or give it back.</summary>
        internal void CrowdGrant(bool granted)
        {
            _crowdGranted = granted;
        }

        /// <summary>
        /// Finds the crowd row for an NPC that no named row serves. People with the reduced 17-bone body (the Low preset)
        /// are declined silently and counted; a served person joins the budget.
        /// </summary>
        private bool ResolveCrowd(LiveCastRegistry registry, bool creature, string castId)
        {
            if (_npc == null || !registry.HasCrowdRow) return false;
            HumanoidRig rig = _owner.Rig;
            if (rig == null || rig.Bones == null) return false;
            if (rig.Bones.Length != RigLayout.BoneCount)
            {
                LiveCrowd.DeclineLowDetail();
                return false;
            }
            Appearance look = rig.Look;
            LiveCastRow row = registry.FindCrowd(_npc.Role, look.Female, look.Age, creature, !string.IsNullOrEmpty(castId), look.Height, look.Bulk);
            if (row == null) return false;
            _row = row;
            _crowd = true;
            _crowdRole = _npc.Role;
            LiveCrowd.Register(this);
            return true;
        }

        /// <summary>Binds a crowd body: a pooled one when the pool has it, otherwise a fresh clone measured like any live visual.</summary>
        private bool BindCrowd(out string reason)
        {
            if (!LiveCastRegistry.Contract(_row, out reason)) return false;
            ImportedModel04OD model = _row.Model;
            if (!CheckRig(model, out reason)) return false;
            _puppet = LiveCrowd.Acquire(_row);
            bool reused = _puppet != null;
            if (!reused)
            {
                if (!_row.PrefabChecked)
                {
                    if (!InspectPrefab(model, out reason)) return false;
                    _row.PrefabChecked = true;
                }
                _visual = Instantiate(model.Prefab, transform, false);
                _visual.name = "Live crowd body / " + Label;
                _visual.transform.localPosition = Vector3.zero;
                _visual.transform.localRotation = Quaternion.identity;
                _visual.transform.localScale = Vector3.one * model.Scale;
                if (!CheckInstance(out reason)) return false;
                SkinnedMeshRenderer[] skins = _visual.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                if (skins.Length == 0) { reason = "the crowd body has no skinned mesh."; return false; }
                if (!MapBones(model, out reason) || !CheckSkins(model, skins, out reason) || !CheckSockets(model, out reason)) return false;
                foreach (Transform child in _visual.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = Layers.IgnoreRaycast;
                // Hair, beard and headwear: measure this body's skull once per prefab (at rest) and add the three slots
                // under its Head bone before the renderers are cached, so they share presentation, tints and the flash.
                if (_row.CrowdHair)
                {
                    _kit = EnsureKit(model, _target[RigLayout.Head], _target[RigLayout.Chest]);
                    if (_kit.Fit.Valid && !CreateHeadSlots(_target[RigLayout.Head])) LiveCrowd.NoteHairProblem(Label, LiveHairMaterials.Why);
                }
                CacheRenderers();
                FindHeadSlots();
                LiveCrowdSurface[] surfaces = Surfaces();
                SlotSurfaces(surfaces);
                _puppet = LiveCrowd.Wrap(_row, _visual, _target, _renderers, _materialStart, _materialCount, _emission, surfaces, ArmourFlags());
                _puppet.BaseColor = BaseColors(_renderers, _materialStart, _emission.Length);
            }
            else
            {
                _visual = _puppet.Root;
                _visual.transform.SetParent(transform, false);
                _visual.transform.localPosition = Vector3.zero;
                _visual.transform.localRotation = Quaternion.identity;
                _visual.transform.localScale = Vector3.one * model.Scale;
                _puppet.RestoreRest();
                _visual.SetActive(true);
                _target = _puppet.Targets;
                _renderers = _puppet.Renderers;
                _materialStart = _puppet.MaterialStart;
                _materialCount = _puppet.MaterialCount;
                _emission = _puppet.Emission;
                _kit = _row.CrowdHair ? LiveHairKits.Find(model.Prefab) : null;
                FindHeadSlots();
                ComputeOffsets(model);
            }
            // The body takes this person's height (the bones stay where the rig puts them; the skin is sized to match).
            ScaleToHeight(model);
            _block = new MaterialPropertyBlock();
            _glowId = Shader.PropertyToID("_Glow");
            _flashId = Shader.PropertyToID("_HitFlash");
            _emissionId = Shader.PropertyToID("_EmissionColor");
            _colorId = Shader.PropertyToID("_Color");
            _baseColorId = Shader.PropertyToID("_BaseColor");
            _source = _rig.Bones;
            _body = _rig.Body;
            _bodyForcedPrior = _body.forceRenderingOff;
            _costume = null;
            _costumeForcedPrior = null;
            PrepareCrowdLook();
            TakeCadence();
            _ready = true;
            _applied = false;
            _hidingBody = false;
            _needPose = true;
            _sentGlow = -1f;
            _sentFlash = -1f;
            LiveCrowd.NoteBind(reused);
            if (!_row.Announced)
            {
                _row.Announced = true;
                Log.Info("Live crowd: " + Label + " now draws " + LiveCrowdRules.RoleList(_row.CrowdRoles) + " (" + LiveCrowdRules.PeopleWord(_row.CrowdFemale, _row.CrowdAge) + ", " + _row.Origin + ", " + _renderers.Length + " renderers each). The procedural body stays the fallback and takes over beyond the budget.");
            }
            reason = "";
            return true;
        }

        /// <summary>The surface of every material of the fresh clone, by material index (parallel to the emission table).</summary>
        private LiveCrowdSurface[] Surfaces()
        {
            LiveCrowdSurface[] surfaces = new LiveCrowdSurface[_emission.Length];
            for (int r = 0; r < _renderers.Length; r++)
            {
                Material[] materials = _renderers[r].sharedMaterials;
                for (int m = 0; m < materials.Length && m < _materialCount[r]; m++)
                    surfaces[_materialStart[r] + m] = materials[m] != null ? LiveCrowdRules.Surface(materials[m].name) : LiveCrowdSurface.Unknown;
            }
            return surfaces;
        }

        /// <summary>Which renderers of the fresh clone are armour: the paths the generator recorded, else the names and steel surfaces.</summary>
        private bool[] ArmourFlags()
        {
            bool[] armour = new bool[_renderers.Length];
            string[] paths = _row.CrowdArmourPaths;
            for (int r = 0; r < _renderers.Length; r++)
            {
                Renderer renderer = _renderers[r];
                if (renderer == null) continue;
                if (paths != null && paths.Length > 0)
                {
                    string path = PathBelow(renderer.transform, _visual.transform);
                    for (int i = 0; i < paths.Length; i++)
                        if (string.Equals(paths[i], path, StringComparison.Ordinal)) { armour[r] = true; break; }
                    continue;
                }
                Material[] materials = renderer.sharedMaterials;
                for (int m = 0; m < materials.Length; m++)
                    if (LiveCrowdRules.ArmourPart(renderer.name, materials[m] != null ? LiveCrowdRules.Surface(materials[m].name) : LiveCrowdSurface.Unknown)) { armour[r] = true; break; }
            }
            return armour;
        }

        /// <summary>The transform path of node below root ("Art/Merged steel armour"), the way the generator records it.</summary>
        private static string PathBelow(Transform node, Transform root)
        {
            string path = node.name;
            for (Transform parent = node.parent; parent != null && parent != root; parent = parent.parent) path = parent.name + "/" + path;
            return path;
        }

        /// <summary>
        /// This person's colours and pieces: the tint per material from LiveCrowdRules.Tint (skin, hair on the brows, coat,
        /// leather, bronze trim; faction colours for gangs and police; an amethyst trim glow for a Shard-Touched elite, folded
        /// into the authored emission so the glow and flash path scales it), which renderers show for the role, and the
        /// head (hair, beard, headwear) from the kit. A solid material keeps its own colour under the tint; the property
        /// block replaces the colour rather than multiplying it, so the bronze and steel would otherwise turn white.
        /// </summary>
        private void PrepareCrowdLook()
        {
            GangDef gang = _npc != null ? Gangs.Get(_npc.GangId) : null;
            bool elite = _npc != null && (_npc.Elite || _npc.IsChampion);
            Color gangColor = gang != null ? gang.MapColor : Color.black;
            LiveCrowdTint tint = LiveCrowdRules.Tint(_crowdRole, _rig.Look, gangColor, gang != null, elite);
            int total = _puppet.Emission.Length;
            _tints = new Color[total];
            _emission = new Color[total];
            for (int i = 0; i < total; i++)
            {
                _emission[i] = _puppet.Emission[i];
                Color c;
                switch (_puppet.Surfaces[i])
                {
                    case LiveCrowdSurface.Skin: c = tint.Skin; break;
                    case LiveCrowdSurface.Brows: c = tint.Hair; break;
                    case LiveCrowdSurface.Wool: c = tint.Cloth; break;
                    case LiveCrowdSurface.Leather: c = tint.Leather; break;
                    case LiveCrowdSurface.Bronze:
                        c = tint.Bronze;
                        _emission[i] = _puppet.Emission[i] + tint.Glow;
                        break;
                    default: c = Color.white; break;
                }
                _tints[i] = _puppet.BaseColor != null && i < _puppet.BaseColor.Length ? Multiply(_puppet.BaseColor[i], c) : c;
            }
            _partShown = new bool[_renderers.Length];
            for (int r = 0; r < _renderers.Length; r++) _partShown[r] = LiveCrowdRules.PartShown(_puppet.Armour[r], _crowdRole);
            DressHead(_crowdRole, _rig.Look, gangColor, gang != null, null, _puppet.Surfaces, _partShown, _tints, _emission);
        }

        /// <summary>Gives a crowd body back to the pool (the visual is parked, never destroyed) and forgets this person's look.</summary>
        private void ReleaseCrowdBody()
        {
            if (_puppet != null)
            {
                LiveCrowd.Release(_row, _puppet);
                _puppet = null;
            }
            else if (_visual != null)
            {
                _visual.SetActive(false);
                Destroy(_visual);
            }
            _visual = null;
            _partShown = null;
            _tints = null;
        }
    }
}
