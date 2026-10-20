using System;
using System.Collections.Generic;
using UnityEngine;
namespace NeonSyndicate
{
    /// <summary>Late visual retarget of the existing pose, with fail-closed restoration. Never imports a second controller.</summary>
    [DefaultExecutionOrder(2000)]
    public sealed class ImportedVisual04OD : MonoBehaviour
    {
        private CharacterBase _owner;
        private GameObject _visual;
        private Renderer _fallback;
        private Transform[] _source,_target;
        private Quaternion[] _sourceBind,_targetBind;
        private bool _originalEnabled,_ready;
        private Renderer[] _renderers;
        private readonly MaterialPropertyBlock _properties=new MaterialPropertyBlock();
        public static void Attach(CharacterBase owner)
        {if(owner!=null && owner.GetComponent<ImportedVisual04OD>()==null)owner.gameObject.AddComponent<ImportedVisual04OD>();}
        private void Start()
        {
            _owner=GetComponent<CharacterBase>();
            if(_owner==null){Destroy(this);return;}
            Creature creature=_owner as Creature;NpcController npc=_owner as NpcController;
            string identity=_owner.IsPlayer?"malik":creature!=null && creature.Species!=null?"creature:"+creature.Species.Id:
                npc!=null && npc.CreatureSpecies!=null?"creature:"+npc.CreatureSpecies.Id:"npc:"+_owner.DisplayName;
            ImportedModel04OD model=ImportedModelRegistry04OD.Find(identity);
            if(model==null){Destroy(this);return;}
            try
            {
                _source=_owner.Rig!=null?_owner.Rig.Bones:creature!=null && creature.CRig!=null?creature.CRig.Bones:null;
                _fallback=_owner.Rig!=null?(Renderer)_owner.Rig.Body:creature!=null && creature.CRig!=null?creature.CRig.Renderer:null;
                string reason;
                if(model.Prefab==null || _source==null || _fallback==null || !ImportedModelRegistry04OD.ValidMapping(_source.Length,model.Bones,model.Scale,out reason))throw new InvalidOperationException("Missing prefab, source rig or complete mapping.");
                _visual=Instantiate(model.Prefab,transform,false);_visual.name="Imported visual / "+identity;_visual.transform.localScale=Vector3.one*model.Scale;
                _visual.transform.localPosition=Vector3.zero;_visual.transform.localRotation=Quaternion.identity;
                if(_visual.GetComponentsInChildren<Collider>(true).Length>0 || _visual.GetComponentsInChildren<Rigidbody>(true).Length>0 || _visual.GetComponentsInChildren<MonoBehaviour>(true).Length>0)
                    throw new InvalidOperationException("Visual prefab must not contain physics or gameplay scripts.");
                foreach(Animator animator in _visual.GetComponentsInChildren<Animator>(true))animator.enabled=false;
                SkinnedMeshRenderer[] skins=_visual.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                if(skins.Length==0)throw new InvalidOperationException("No skinned mesh.");
                _target=new Transform[_source.Length];_sourceBind=new Quaternion[_source.Length];_targetBind=new Quaternion[_source.Length];
                foreach(ImportedBone04OD bone in model.Bones)
                {
                    Transform target=_visual.transform.Find(bone.TargetPath);int i=bone.SourceBone;
                    if(target==null)throw new InvalidOperationException("Missing target bone: "+bone.TargetPath);
                    _target[i]=target;_sourceBind[i]=_source[i].rotation;_targetBind[i]=target.rotation;
                }
                HashSet<Transform> mapped=new HashSet<Transform>(_target);
                foreach(SkinnedMeshRenderer skin in skins)
                {
                    if(skin.sharedMesh==null || skin.bones.Length==0)throw new InvalidOperationException("Mesh or skin bones missing.");
                    foreach(Transform bone in skin.bones)if(bone==null || !mapped.Contains(bone))throw new InvalidOperationException("Unmapped weighted bone; retain fallback until the full adapter is authored.");
                    foreach(Material material in skin.sharedMaterials)if(material==null || material.shader==null || !material.shader.isSupported)throw new InvalidOperationException("Unsupported material in current renderer.");
                }
                string[] required=_owner.Rig!=null?new[]{"hand_r","hand_l","foregrip","muzzle","blade_hilt","blade_tip","head","chest","fx","synergy"}:new[]{"head","chest","fx","synergy"};
                foreach(string role in required)
                {
                    bool found=false;
                    if(model.Sockets!=null)foreach(ImportedSocket04OD socket in model.Sockets)if(socket!=null && socket.Role==role && _visual.transform.Find(socket.Path)!=null)found=true;
                    if(!found)throw new InvalidOperationException("Missing visual socket: "+role);
                }
                foreach(Transform child in _visual.GetComponentsInChildren<Transform>(true))child.gameObject.layer=Layers.IgnoreRaycast;
                _renderers=_visual.GetComponentsInChildren<Renderer>(true);
                _originalEnabled=_fallback.enabled;_fallback.enabled=false;_ready=true;
            }
            catch(Exception e){Log.Warn("Imported model fallback: "+identity+": "+e.Message);Restore();Destroy(this);}
        }
        private void LateUpdate()
        {
            if(!_ready || GameTime.Paused)return;
            if(_owner==null || _visual==null || _source==null || _fallback==null){Restore();enabled=false;return;}
            if(_owner.IsDead || _owner.IsRagdolled){Restore();enabled=false;return;}
            for(int i=0;i<_source.Length;i++)
            {
                if(_source[i]==null || _target[i]==null){Restore();enabled=false;return;}
                _target[i].position=_source[i].position;
                _target[i].rotation=_source[i].rotation*Quaternion.Inverse(_sourceBind[i])*_targetBind[i];
            }
            // The original rig owns visibility, flashes and telegraphs. Copy its current property block.
            _fallback.GetPropertyBlock(_properties);
            foreach(Renderer renderer in _renderers)if(renderer!=null)renderer.SetPropertyBlock(_properties);
        }
        private void Restore()
        {
            if(_fallback!=null && _ready)_fallback.enabled=_originalEnabled;
            _ready=false;if(_visual!=null){_visual.SetActive(false);Destroy(_visual);_visual=null;}
        }
        private void OnDisable(){Restore();}
        private void OnDestroy(){Restore();}
    }
}
