using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace NeonSyndicate
{
    /// <summary>A real traversable fort with six sockets, a core and a challenge point. Optional 04A routes use separate bounded collision.</summary>
    public sealed partial class StrongholdView : IDisposable
    {
        public readonly StrongholdDefinition Definition;
        public readonly Vector3 Origin;
        public readonly GameObject Root;
        private readonly StrongholdSystem _system;
        private readonly StrongholdAssets _assets;
        private GameObject _art;
        private Transform _ring;
        private TextMesh _label;
        private Light _coreLight;
        private Blip _blip;
        public Vector3 CorePoint { get { return Origin + new Vector3(0f,.07f,2.65f); } }
        public Vector3 ChallengePoint { get { return Origin + new Vector3(0f,.07f,-20f); } }
        public StrongholdView(StrongholdDefinition def, Vector3 origin, StrongholdSystem system, StrongholdAssets assets)
        {
            Definition = def; Origin = origin; _system = system; _assets = assets;
            Root = new GameObject("Crystal Stronghold 02I / " + def.title);
            Root.transform.SetParent(system.transform,false); Root.transform.position = origin;
            Root.SetActive(false);
            try
            {
                Interactable core = Interactable.Create("02I / " + def.id + " / core",CorePoint,2.6f,"Attune crystal core",
                    delegate { system.UseCore(this); },Root.transform);
                core.HeightTolerance = 2f;
                core.Condition = delegate { return system.CanInteract(this,false); };
                core.DynamicPrompt = delegate { return system.CorePrompt(this); };
                if (!def.home)
                {
                    Interactable challenge = Interactable.Create("02I / " + def.id + " / challenge",ChallengePoint,2.6f,"Challenge the rival wardens",
                        delegate { system.Challenge(this); },Root.transform);
                    challenge.HeightTolerance = 2f;
                    challenge.Condition = delegate { return system.CanInteract(this,true); };
                    challenge.DynamicPrompt = delegate { return system.ChallengePrompt(this); };
                }
                GameObject label = new GameObject("02I / core sign"); label.transform.SetParent(Root.transform,false);
                label.transform.localPosition = new Vector3(0f,4.5f,5f);
                _label = label.AddComponent<TextMesh>(); _label.font = Mats.UIFont; _label.fontSize = 48; _label.characterSize = .065f;
                _label.anchor = TextAnchor.MiddleCenter; _label.alignment = TextAlignment.Center;
                MeshRenderer lr = label.GetComponent<MeshRenderer>();
                if (_label.font != null) lr.sharedMaterial = _label.font.material;
                GameObject light = new GameObject("02I / bounded core light"); light.transform.SetParent(Root.transform,false);
                light.transform.localPosition = new Vector3(0f,3.7f,5f);
                _coreLight = light.AddComponent<Light>(); _coreLight.type = LightType.Point;
                _coreLight.range = 10f; _coreLight.shadows = LightShadows.None; _coreLight.bounceIntensity = 0f; _coreLight.enabled = false;
                Rebuild(system.Book.Status(def.id),system.Book.Tier);
                _blip = Blip.Add(BlipKind.Activity,Root.transform,Origin,UIKit.Cyan,def.home?"K":"R",def.title,false);
                Root.SetActive(true);
                VisualRebirth04AQa.RegisterStronghold(Definition.id,Root.transform);
            }
            catch { Dispose(); throw; }
        }

        public void Rebuild(StrongholdSiteSave state,int tier)
        {
            if (Root == null || state == null) return;
            GameObject staged = new GameObject("02I / generated fort modules"); staged.transform.SetParent(Root.transform,false); staged.SetActive(false);
            try
            {
                FortBatch batch = new FortBatch(staged.transform,_assets,state.Theme);
                batch.Part("foundation","stone",Vector3.zero,Vector3.one,Quaternion.identity);
                Box(staged.transform,new Vector3(0f,-.08f,0f),new Vector3(32f,.30f,32f),Quaternion.identity);
                // Low perimeter cover encloses a real courtyard while the broad front gate stays open.
                for (int side = -1; side <= 1; side += 2)
                {
                    Vector3 edge = new Vector3(side * 15f,.07f,0f);
                    batch.Part("wall","stone",edge,new Vector3(5f,.70f,1f),Quaternion.Euler(0f,90f,0f));
                    Box(staged.transform,edge+Vector3.up*1.05f,new Vector3(.7f,2.1f,29f),Quaternion.identity);
                    Vector3 front = new Vector3(side * 9.2f,.07f,-14.85f);
                    batch.Part("wall","stone",front,new Vector3(1.75f,.70f,1f),Quaternion.identity);
                    Box(staged.transform,front+Vector3.up*1.05f,new Vector3(10.15f,2.1f,.7f),Quaternion.identity);
                }
                Vector3 rear = new Vector3(0f,.07f,15f);
                batch.Part("wall","stone",rear,new Vector3(5f,.70f,1f),Quaternion.identity);
                Box(staged.transform,rear+Vector3.up*1.05f,new Vector3(29f,2.1f,.7f),Quaternion.identity);
                Vector3 gate = new Vector3(0f,.07f,-14f);
                batch.Part("gate","stone",gate,Vector3.one,Quaternion.identity);
                batch.Part("gate_trim","gilt",gate,Vector3.one,Quaternion.identity);
                for (int side = -1; side <= 1; side += 2)
                {
                    Box(staged.transform,gate + new Vector3(side*3.7f,2.65f,0f),new Vector3(1.12f,5.3f,1.12f),Quaternion.identity);
                    for (int end = -1; end <= 1; end+=2)
                        batch.Part("spire","crystal",new Vector3(side*14.4f,.07f,end*13.3f),new Vector3(.60f,.70f,.60f),Quaternion.identity);
                    batch.Part("banner","cloth",gate + new Vector3(side*5.7f,5f,.1f),Vector3.one,Quaternion.identity);
                }
                for (int i = 0; i < 6; i++)
                {
                    Vector3 position = StrongholdPlacement.Socket(i);
                    FortSlotSave slot = state.Slots[i]; Quaternion turn = Quaternion.Euler(0f,slot.Rotation*90f,0f);
                    switch ((FortModule)slot.Module)
                    {
                        case FortModule.Watchtower:
                            batch.Part("tower","stone",position,Vector3.one,turn);
                            Cylinder(staged.transform,position+Vector3.up*.2f,1.32f,5.55f);
                            Box(staged.transform,position+Vector3.up*5.85f,new Vector3(4.65f,.30f,4.65f),turn);
                            for (int s=-1;s<=1;s+=2)
                                for(int z=-1;z<=1;z+=2)
                                {
                                    Vector3 ledge=position+turn*new Vector3(s*2.1f,6.4f,z*2.1f);
                                    Box(staged.transform,ledge,new Vector3(.9f,.8f,.9f),turn);
                                }
                            batch.Part("banner","cloth",position+turn*new Vector3(0f,5.65f,-1.35f),new Vector3(.7f,.8f,.7f),turn);
                            break;
                        case FortModule.Conservatory:
                            batch.Part("trellis","gilt",position,Vector3.one,turn);
                            batch.Part("canopy","leaf",position+Vector3.up*1.1f,Vector3.one,turn);
                            for(int s=-1;s<=1;s+=2) for(int z=-1;z<=1;z+=2)
                                Cylinder(staged.transform,position+turn*new Vector3(s*2f,0f,z*2f),.16f,3.1f);
                            batch.Part("inlay","crystal",position+Vector3.up*.04f,Vector3.one*1.5f,turn);
                            break;
                        case FortModule.Bulwark:
                            batch.Part("wall","stone",position,Vector3.one,turn);
                            Box(staged.transform,position+Vector3.up*1.5f,new Vector3(5.8f,3f,.7f),turn);
                            batch.Part("banner","cloth",position+turn*new Vector3(0f,3.2f,-.42f),new Vector3(.65f,.75f,.65f),turn);
                            break;
                        case FortModule.FlightDais:
                            batch.Part("dais","stone",position,Vector3.one,turn);
                            Cylinder(staged.transform,position,2.9f,.22f);
                            Box(staged.transform,position+Vector3.up*2.92f,new Vector3(5f,.24f,5f),turn);
                            Cylinder(staged.transform,position+Vector3.up*.22f,2.6f,.22f);
                            Cylinder(staged.transform,position+Vector3.up*.44f,2.35f,.15f);
                            for(int p=0;p<4;p++)
                            {
                                float a = p * Mathf.PI * .5f;
                                Cylinder(staged.transform,position+turn*new Vector3(Mathf.Cos(a)*1.6f,.59f,Mathf.Sin(a)*1.6f),.17f,2.2f);
                            }
                            batch.Part("inlay","crystal",position+Vector3.up*3.065f,Vector3.one*1.9f,turn);
                            break;
                    }
                    batch.Part("inlay","gilt",position+Vector3.up*.018f,new Vector3(2.8f,1f,2.8f),Quaternion.identity);
                }
                Vector3 podium = new Vector3(0f,.07f,5f);
                batch.Part("podium","stone",podium,Vector3.one,Quaternion.identity);
                Cylinder(staged.transform,podium,1.25f,.85f);
                batch.Part("heart","crystal",podium+Vector3.up*.92f,new Vector3(1f,1f+Mathf.Max(0,tier-1)*.10f,1f),Quaternion.identity);
                batch.Part("gate_trim","gilt",podium+new Vector3(0f,2.5f,0f),Vector3.one*.26f,Quaternion.identity);
                batch.Part("inlay","crystal",new Vector3(0f,.11f,-20f),Vector3.one*1.65f,Quaternion.identity);
                for(int i=0;i<5;i++) batch.Part("inlay","gilt",new Vector3(0f,.105f,-10f+i*2.3f),Vector3.one*.35f,Quaternion.identity);
                batch.Finish();
                GameObject rotating = new GameObject("02I / core orbit"); rotating.transform.SetParent(staged.transform,false);
                rotating.transform.localPosition = podium+Vector3.up*2.6f;
                rotating.AddComponent<MeshFilter>().sharedMesh = _assets.Model("inlay",false);
                rotating.AddComponent<MeshRenderer>().sharedMaterial = _assets.Surface(state.Theme,"gilt");
                rotating.transform.localScale = Vector3.one*2.2f;
                TryDecorateCitadel(staged.transform,state);
                TryDecorateRebirth04A(staged.transform,state);
                // Swap atomically, preventing old and new physics from coexisting.
                GameObject previous = _art; if(previous!=null) previous.SetActive(false);
                _art=staged; _ring=rotating.transform; staged.SetActive(true);
                if(previous!=null) UnityEngine.Object.Destroy(previous);
                Physics.SyncTransforms();
            }
            catch { UnityEngine.Object.Destroy(staged); throw; }
        }
        public void Tick(StrongholdSiteSave state,Transform viewer,bool visible,float dt,int tier)
        {
            if (Root==null || state==null) return;
            Root.SetActive(visible);
            if(_blip!=null)
            {
                _blip.Visible=visible; _blip.Color=state.Owned?UIKit.Cyan:UIKit.Gold;
                _blip.Letter=Definition.home||state.Owned?"K":"R";
                _blip.Label=Definition.title+(state.Owned?" (your fort)":(Definition.home?" (claim core)":" (optional PvE raid)"));
            }
            if(_coreLight!=null)
            {
                float distance = viewer!=null ? Vector3.Distance(viewer.position,_coreLight.transform.position) : 1000f;
                float night = Game.Atmo!=null ? Game.Atmo.NightFactor : 0f;
                _coreLight.color = CrystalCity02DMaterials.Glow((CrystalCityStyle)state.Theme);
                _coreLight.intensity = Mathf.Clamp01(1f-(distance-20f)/25f) * Mathf.Clamp01(night) * 1.3f;
                _coreLight.enabled = visible && Settings.Graphics!=GraphicsPreset.Low && _coreLight.intensity>.02f;
            }
            if(!visible)return;
            if(_ring!=null && dt>0f) _ring.Rotate(0f,9f*dt,0f,Space.Self);
            if(_label!=null)
            {
                bool near=viewer!=null && (viewer.position-_label.transform.position).sqrMagnitude<40f*40f;
                _label.gameObject.SetActive(near);
                if(near)
                {
                    _label.text=Definition.title.ToUpperInvariant()+"\n"+(state.Owned?"YOUR STRONGHOLD":(Definition.home?"CLAIM THE CRYSTAL CORE":"CHALLENGE AT FRONT SIGIL"));
                    if(_rebirth04AActive)_label.text+="\nVISUAL REBIRTH 04A";
                    else if(_citadelActive)_label.text+="\nCITADEL RENAISSANCE 03A2";
                    _label.color=state.Owned?UIKit.Cyan:UIKit.Gold;
                    _label.transform.rotation=Quaternion.LookRotation(_label.transform.position-viewer.position);
                }
            }
        }
        private static void Box(Transform parent,Vector3 position,Vector3 size,Quaternion rotation)
        {
            GameObject c=new GameObject("02I fort collision"); c.transform.SetParent(parent,false);
            c.transform.localPosition=position;c.transform.localRotation=rotation;c.layer=Layers.World;
            c.AddComponent<BoxCollider>().size=size;
        }
        private static void Cylinder(Transform parent,Vector3 basePoint,float radius,float height)
        {
            // A convex cylinder would need another physics mesh. Box support is conservative inside the solid plinth.
            Box(parent,basePoint+Vector3.up*height*.5f,new Vector3(radius*1.414f,height,radius*1.414f),Quaternion.identity);
        }
        public void Dispose()
        {
            if(_blip!=null){Blip.Remove(_blip);_blip=null;}
            if(Root!=null){Root.SetActive(false);UnityEngine.Object.Destroy(Root);}
            _citadelKit.Dispose();
        }
        private sealed class FortBatch
        {
            private readonly Transform _parent;
            private readonly StrongholdAssets _assets;
            private readonly int _theme;
            private readonly Dictionary<Material,List<CombineInstance>> _near=new Dictionary<Material,List<CombineInstance>>();
            private readonly Dictionary<Material,List<CombineInstance>> _far=new Dictionary<Material,List<CombineInstance>>();
            public FortBatch(Transform parent,StrongholdAssets assets,int theme){_parent=parent;_assets=assets;_theme=theme;}
            public void Part(string id,string role,Vector3 at,Vector3 scale,Quaternion rotation)
            {
                Material mat=_assets.Surface(_theme,role); Matrix4x4 trs=Matrix4x4.TRS(at,rotation,scale);
                Add(_near,mat,_assets.Model(id,Settings.Graphics==GraphicsPreset.Low),trs);
                Add(_far,mat,_assets.Model(id,true),trs);
            }
            private static void Add(Dictionary<Material,List<CombineInstance>> map,Material mat,Mesh mesh,Matrix4x4 m)
            {
                List<CombineInstance> parts; if(!map.TryGetValue(mat,out parts)){parts=new List<CombineInstance>();map.Add(mat,parts);}
                parts.Add(new CombineInstance{mesh=mesh,transform=m});
            }
            public void Finish()
            {
                Renderer[] n=Emit(_near),f=Emit(_far); LODGroup lod=_parent.gameObject.AddComponent<LODGroup>();
                lod.fadeMode=LODFadeMode.None;lod.SetLODs(new[]{new LOD(.075f,n),new LOD(.008f,f)});lod.RecalculateBounds();
            }
            private Renderer[] Emit(Dictionary<Material,List<CombineInstance>> groups)
            {
                List<Renderer> result=new List<Renderer>();
                foreach(KeyValuePair<Material,List<CombineInstance>> pair in groups)
                {
                    GameObject go=new GameObject("02I batched "+pair.Key.name);go.transform.SetParent(_parent,false);
                    Mesh mesh=OwnedMeshes.Track(go,new Mesh{name="Stronghold material batch",indexFormat=IndexFormat.UInt32});
                    mesh.CombineMeshes(pair.Value.ToArray(),true,true,false);mesh.RecalculateBounds();mesh.RecalculateTangents();
                    go.AddComponent<MeshFilter>().sharedMesh=mesh;MeshRenderer r=go.AddComponent<MeshRenderer>();r.sharedMaterial=pair.Key;
                    r.shadowCastingMode=Settings.Graphics>=GraphicsPreset.High?ShadowCastingMode.On:ShadowCastingMode.Off;r.receiveShadows=true;
                    mesh.UploadMeshData(true);result.Add(r);
                }
                return result.ToArray();
            }
        }
    }
}
