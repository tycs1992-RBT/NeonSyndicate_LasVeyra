using System;
using System.Collections.Generic;
using UnityEngine;
namespace NeonSyndicate
{
    /// <summary>Late visual retarget of the existing pose, with fail-closed restoration. Never imports a second controller.</summary>
    [DefaultExecutionOrder(2000)]
    public sealed partial class ImportedVisual04OD : MonoBehaviour
    {
        private CharacterBase _owner;
        private CompanionController _companion;
        private bool _waitingForBond;
        private GameObject _visual;
        private Renderer _fallback;
        private Transform[] _source,_target;
        private Quaternion[] _sourceBind,_targetBind;
        private int[] _parentFirst04OF;
        private bool _originalEnabled,_ready;
        private Renderer[] _renderers;
        private MaterialPropertyBlock _properties;
        private bool _surfaceH6;
        private Color[] _emissionH6;
        public static void Attach(CharacterBase owner)
        {if(owner!=null && owner.GetComponent<ImportedVisual04OD>()==null)owner.gameObject.AddComponent<ImportedVisual04OD>();}
        public static void Attach(CompanionController companion)
        {if(companion!=null && companion.GetComponent<ImportedVisual04OD>()==null)companion.gameObject.AddComponent<ImportedVisual04OD>();}
        private void Start()
        {
            if(_ready)return;
            _owner=GetComponent<CharacterBase>();
            _companion=GetComponent<CompanionController>();
            if(_owner==null && _companion==null){Destroy(this);return;}
            if(_companion!=null && _companion.Uid<=0){_waitingForBond=true;return;}
            Initialize();
        }
        private void Initialize()
        {
            Creature creature=_owner as Creature;NpcController npc=_owner as NpcController;
            string identity=_companion!=null?"companion:"+_companion.Archetype:_owner.IsPlayer?"malik":creature!=null && creature.Species!=null?"creature:"+creature.Species.Id:
                npc!=null && npc.CreatureSpecies!=null?"creature:"+npc.CreatureSpecies.Id:"npc:"+_owner.DisplayName;
            ImportedModel04OD model=ImportedModelRegistry04OD.Find(identity);
#if UNITY_EDITOR
            if(_reviewH6!=null)model=_reviewH6;
#endif
            if(model==null){Destroy(this);return;}
            try
            {
                // MaterialPropertyBlock.CreateImpl is a native call. Component construction and
                // deserialization can happen off the main thread, before Unity permits it.
                // Initialize runs from Start (or LateUpdate for a newly earned companion).
                _properties=new MaterialPropertyBlock();
                _source=_companion!=null && _companion.Rig!=null?_companion.Rig.Bones:_owner!=null && _owner.Rig!=null?_owner.Rig.Bones:creature!=null && creature.CRig!=null?creature.CRig.Bones:null;
                _fallback=_companion!=null && _companion.Rig!=null?(Renderer)_companion.Rig.Renderer:_owner!=null && _owner.Rig!=null?(Renderer)_owner.Rig.Body:creature!=null && creature.CRig!=null?creature.CRig.Renderer:null;
                string reason;
                if(model.Prefab==null || _source==null || _fallback==null || !ImportedModelRegistry04OD.ValidMapping(_source.Length,model.Bones,model.Scale,out reason))throw new InvalidOperationException("Missing prefab, source rig or complete mapping.");
                if(!ImportedModelRegistry04OD.ValidPassivePaths(model.PassiveBonePaths))throw new InvalidOperationException("Invalid or duplicate passive bone paths.");
                foreach(Transform node in model.Prefab.GetComponentsInChildren<Transform>(true))
                    if(!ImportedModelRegistry04OD.ValidLocalScale04OG(node.localScale))throw new InvalidOperationException("Mirrored, collapsed or nonuniform visual transform: "+node.name+". Re-export with applied transforms.");
                // Inspect the source before cloning: Instantiate may already execute Awake/OnEnable on scripts.
                foreach(Component component in model.Prefab.GetComponentsInChildren<Component>(true))
                {
                    if(component==null || !ImportedArt04OH.VisualComponent(component.GetType()))
                        throw new InvalidOperationException("Visual prefab contains an unsupported component before instantiation. Keep only transforms, mesh renderers, mesh filters, disabled Animator and LOD groups.");
                    Animator animation=component as Animator;
                    if(animation!=null && animation.runtimeAnimatorController!=null)
                        throw new InvalidOperationException("Remove the Animator controller before cloning. Its state behaviours must not execute beside the original pose owner.");
                }
                _visual=Instantiate(model.Prefab,transform,false);_visual.name="Imported visual / "+identity;_visual.transform.localScale=Vector3.one*model.Scale;
                _visual.transform.localPosition=Vector3.zero;_visual.transform.localRotation=Quaternion.identity;
                if(_visual.GetComponentsInChildren<Collider>(true).Length>0 || _visual.GetComponentsInChildren<Rigidbody>(true).Length>0 || _visual.GetComponentsInChildren<MonoBehaviour>(true).Length>0)
                    throw new InvalidOperationException("Visual prefab must not contain physics or gameplay scripts.");
                foreach(Animator animator in _visual.GetComponentsInChildren<Animator>(true)){animator.enabled=false;animator.applyRootMotion=false;}
                if(model.PreserveInfluencesH5 && QualitySettings.skinWeights!=SkinWeights.Unlimited)throw new InvalidOperationException("Complete-influence H5 reference requires an explicitly reviewed unlimited-skin quality configuration. Keep fallback; no global quality setting was changed.");
                SkinnedMeshRenderer[] skins=_visual.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                if(skins.Length==0)throw new InvalidOperationException("No skinned mesh.");
                _target=new Transform[_source.Length];_sourceBind=new Quaternion[_source.Length];_targetBind=new Quaternion[_source.Length];
                foreach(ImportedBone04OD bone in model.Bones)
                {
                    Transform target=_visual.transform.Find(bone.TargetPath);int i=bone.SourceBone;
                    if(target==null)throw new InvalidOperationException("Missing target bone: "+bone.TargetPath);
                    // Both original skeletons have identity bind rotations. Do not mistake the first animated frame
                    // for bind pose, which would permanently subtract an idle/cast from the imported performance.
                    _target[i]=target;_sourceBind[i]=transform.rotation;_targetBind[i]=target.rotation;
                }
                if(model.AlignBindAxesH3)
                {
                    if(_owner==null || _owner.Rig==null || _owner.Rig.Layout==null || _source.Length!=RigLayout.BoneCount)throw new InvalidOperationException("H3 bind alignment requires the complete original humanoid rig. Keep fallback on unsupported detail/creature rigs.");
                    Vector3[] bindPositions=new Vector3[_target.Length];for(int i=0;i<_target.Length;i++)bindPositions[i]=_target[i].position;
                    _targetBind=ImportedBindH3.Align(_owner.Rig.Layout.World,RigLayout.Parent,bindPositions,_targetBind,transform.rotation);
                }
                int[] depths=new int[_target.Length];
                for(int i=0;i<_target.Length;i++){Transform parent=_target[i].parent;while(parent!=null && parent!=_visual.transform){depths[i]++;parent=parent.parent;}}
                _parentFirst04OF=ImportedModelRegistry04OD.ParentFirst04OF(depths);
                HashSet<Transform> mapped=new HashSet<Transform>(_target);
                foreach(SkinnedMeshRenderer skin in skins)
                {
                    if(skin.sharedMesh==null || skin.bones.Length==0 || skin.sharedMesh.bindposes.Length!=skin.bones.Length)throw new InvalidOperationException("Mesh, skin bones or matching bind poses missing.");
                    if(model.PreserveInfluencesH5)skin.quality=SkinQuality.Auto;
                    bool[] referenced=model.PreserveInfluencesH5?SkinWeightsH5.Referenced(skin.sharedMesh,skin.bones.Length):model.AlignBindAxesH3?ImportedBindH3.ReferencedBones(skin.sharedMesh.boneWeights,skin.bones.Length):null;
                    for(int index=0;index<skin.bones.Length;index++)
                    {
                        if(referenced!=null && !referenced[index])continue;
                        Transform bone=skin.bones[index];
                        if(bone==null || (!mapped.Contains(bone) && !PassiveBone04OE(bone,model,mapped)))throw new InvalidOperationException("Unmapped weighted bone; author a mapped or explicit passive descendant adapter.");
                    }
                    foreach(Material material in skin.sharedMaterials)if(material==null || material.shader==null || !material.shader.isSupported)throw new InvalidOperationException("Unsupported material in current renderer.");
                }
                string[] required=_owner!=null && _owner.Rig!=null?new[]{"hand_r","hand_l","foregrip","muzzle","blade_hilt","blade_tip","head","chest","fx","synergy"}:new[]{"head","chest","fx","synergy"};
                foreach(string role in required)
                {
                    bool found=false;
                    if(model.Sockets!=null)foreach(ImportedSocket04OD socket in model.Sockets)if(socket!=null && socket.Role==role && CinematicAcceptance04OF.RelativeBonePath(socket.Path) && _visual.transform.Find(socket.Path)!=null)found=true;
                    if(!found)throw new InvalidOperationException("Missing visual socket: "+role);
                }
                foreach(Transform child in _visual.GetComponentsInChildren<Transform>(true))child.gameObject.layer=Layers.IgnoreRaycast;
                _renderers=_visual.GetComponentsInChildren<Renderer>(true);
                _surfaceH6=model.ArtProof!=null && (model.ArtProof.AssetId==MalikReviewH6.AssetId || model.ArtProof.AssetId==MalikReviewH7.AssetId);
                _emissionH6=new Color[_renderers.Length];
                for(int i=0;i<_renderers.Length;i++)
                {Material material=_renderers[i].sharedMaterial;if(material!=null && material.HasProperty("_EmissionColor"))_emissionH6[i]=material.GetColor("_EmissionColor");}
                _originalEnabled=_fallback.enabled;_fallback.enabled=false;_ready=true;
            }
            catch(Exception e){Log.Warn("Imported model fallback: "+identity+": "+e.Message);Restore();Destroy(this);}
        }
        private void LateUpdate()
        {
            if(_waitingForBond && _companion!=null && _companion.Uid>0){_waitingForBond=false;Initialize();}
            if(!_ready || GameTime.Paused)return;
            if((_owner==null && _companion==null) || _visual==null || _source==null || _fallback==null){Restore();enabled=false;return;}
            if(_owner!=null && (_owner.IsDead || _owner.IsRagdolled)){Restore();enabled=false;return;}
            if(_companion!=null)_visual.SetActive(!_companion.Hidden && _companion.Mode!=CompanionMode.Downed);
            // SetHidden(false) on the original companion can re-enable its body. Keep one visible visual owner.
            _fallback.enabled=false;
#if UNITY_EDITOR
            PrepareMotionH7();
#endif
            foreach(int i in _parentFirst04OF)
            {
                if(_source[i]==null || _target[i]==null){Restore();enabled=false;return;}
                _target[i].position=_source[i].position;
                Quaternion pose=ImportedModelRegistry04OD.BindRotation04OF(_source[i].rotation,_sourceBind[i],_targetBind[i]);
#if UNITY_EDITOR
                pose=ComposeMotionH7(i,pose);
#endif
                _target[i].rotation=pose;
            }
            // The original rig owns visibility, flashes and telegraphs. Copy its current property block.
            _fallback.GetPropertyBlock(_properties);
            for(int i=0;i<_renderers.Length;i++)if(_renderers[i]!=null)
            {
                if(_surfaceH6)_properties.SetColor("_EmissionColor",MalikReviewH6.SurfaceEmission(_emissionH6[i],_properties.GetFloat("_Glow"),_properties.GetFloat("_HitFlash")));
                _renderers[i].SetPropertyBlock(_properties);
            }
        }
        private bool PassiveBone04OE(Transform bone,ImportedModel04OD model,HashSet<Transform> mapped)
        {
            if(model.PassiveBonePaths==null)return false;
            bool declared=false;
            foreach(string path in model.PassiveBonePaths)if(!string.IsNullOrWhiteSpace(path) && _visual.transform.Find(path)==bone){declared=true;break;}
            if(!declared)return false;
            Transform parent=bone.parent;
            while(parent!=null && parent!=_visual.transform){if(mapped.Contains(parent))return true;parent=parent.parent;}
            return false;
        }
        private void Restore()
        {
            if(_fallback!=null && _ready)_fallback.enabled=_companion!=null?!_companion.Hidden:_originalEnabled;
            _ready=false;if(_visual!=null){_visual.SetActive(false);Destroy(_visual);_visual=null;}
#if UNITY_EDITOR
            ReleaseReviewH6();
#endif
        }
        private void OnDisable(){Restore();}
        private void OnDestroy(){Restore();}
    }
}
