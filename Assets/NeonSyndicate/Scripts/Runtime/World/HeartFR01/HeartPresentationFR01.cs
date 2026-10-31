using System;
using System.Collections.Generic;
using UnityEngine;
namespace NeonSyndicate
{
    /// <summary>Bounded visual lifetime owned by CityBuilder and LiveWorld. Never owns saves, actors or original collision.</summary>
    public sealed partial class HeartPresentationFR01
    {
        private sealed class Pair { public GameObject Legacy,Art; public bool Failed; }
        private readonly CityLayout _layout;
        private readonly Transform _parent;
        private readonly HeartCatalogFR01 _catalog;
        private readonly Dictionary<Building,Pair> _buildings=new Dictionary<Building,Pair>();
        private readonly HashSet<Building> _preserved=new HashSet<Building>();
        private string _families="";
        private readonly List<GameObject> _dressing=new List<GameObject>();
        private bool _selected,_compare,_suspended=true,_last,_built;
        private string _reason="Prepared catalog absent; original visuals retained.";
        private Vector3 _approach;
        private bool _gate;
        private Vector3 _gatePosition;
        private float _gateYaw;
        private string _gateReason="Gateway not staged.";
        private int _refused;
        public HeartPresentationFR01(CityLayout layout,Transform parent)
        { _layout=layout;_parent=parent;_catalog=Resources.Load<HeartCatalogFR01>("HeartPreparedFR01/Catalog");_livingCatalog=Resources.Load<HeartCatalogFR01>("HeartPreparedFR02A/Catalog"); }
        public bool Selected { get { return FantasyProofRulesV1.Selection(_selected,_fantasyPreviewV1,_previewHeartV1,_proofComparisonV1,_proofLegacyV1); } }
        public string Status { get { int usable=0;foreach(Pair p in _buildings.Values)if(!p.Failed)usable++;return "FR01 "+(Selected?"ON":"OFF")+"; staged masses "+usable+", showing "+(_last?usable:0)+"; preserved in-slice emitters "+_preserved.Count+"; families "+_families+"; refused placements "+_refused+"; route modules "+_dressing.Count+". "+_gateReason+" "+_reason; } }
        private static bool Passive(GameObject source)
        {
            if(source==null)return false;
            foreach(Component c in source.GetComponentsInChildren<Component>(true))if(c==null || !(c is Transform || c is MeshFilter || c is MeshRenderer))return false;
            foreach(MeshFilter f in source.GetComponentsInChildren<MeshFilter>(true))if(f.sharedMesh==null || f.sharedMesh.vertexCount<3)return false;
            return source.GetComponentsInChildren<MeshRenderer>(true).Length>0;
        }
        private GameObject Model(string id,Vector3 position,float yaw,Vector3 scale,HeartCatalogFR01 library=null)
        {
            HeartCatalogFR01 source=library!=null?library:_catalog;
            HeartModuleFR01 m=source!=null?source.Find(id):null;
            if(m==null || m.Lods==null || m.Lods.Length<1)throw new InvalidOperationException("Missing prepared module "+id);
            foreach(GameObject p in m.Lods)if(!Passive(p))throw new InvalidOperationException("Nonpassive or invalid model "+id);
            GameObject root=new GameObject("FR01 / "+id);root.SetActive(false);root.transform.SetParent(_parent,false);
            try
            {
                root.transform.position=position;root.transform.rotation=Quaternion.Euler(0,yaw,0);root.transform.localScale=scale;
                LOD[] levels=new LOD[m.Lods.Length];
                for(int i=0;i<m.Lods.Length;i++)
                {
                    GameObject copy=UnityEngine.Object.Instantiate(m.Lods[i],root.transform,false);copy.SetActive(true);
                    foreach(Transform t in copy.GetComponentsInChildren<Transform>(true))t.gameObject.layer=Layers.IgnoreRaycast;
                    levels[i]=new LOD(HeartPlanFR01.LodThreshold(i,m.Lods.Length),copy.GetComponentsInChildren<Renderer>(true));
                }
                LODGroup group=root.AddComponent<LODGroup>();group.SetLODs(levels);group.RecalculateBounds();return root;
            }
            catch { UnityEngine.Object.Destroy(root);throw; }
        }
        public bool Capture(Building b,MeshBuilder source,Material material)
        {
            if (HeartPlanFR01.SupportedLotKeyWR02J(_layout, b)) return CaptureSupportedLotWR02J(b, source, material);
            if(CaptureRewriteBuildingWR01(b,source,material))return true;
            if(_catalog==null || !_catalog.Ready || !HeartPlanFR01.Eligible(_layout,b)){if(HeartPlanFR01.InSlice(_layout,b.Footprint.Center(0)))_preserved.Add(b);return false;}
            Pair pair;
            if(!_buildings.TryGetValue(b,out pair))
            {
                GameObject art=null;
                try
                {
                    string id=(b.Seed&1)==0?"HeartHouseA":"HeartHouseB";HeartModuleFR01 module=_catalog.Find(id);
                    Vector3 position,scale;if(module==null || !HeartPlanFR01.Fit(b,module.Center,module.Size,out position,out scale)){RefusalV1(false,"Building "+b.Seed,"Prepared module absent or incompatible with original building bounds.");return false;}
                    art=Model(id,position,0,scale);
                    string modelProblem;
                    if(!ValidPreparedModelV1(art,out modelProblem))throw new InvalidOperationException("Building "+b.Seed+" / "+id+": "+modelProblem);
                    GameObject legacy=new GameObject("FR01 isolated original / "+b.Seed);legacy.transform.SetParent(_parent,false);
                    pair=new Pair{Legacy=legacy,Art=art};_buildings.Add(b,pair);_families+=id+" seed "+b.Seed+"; ";
                }
                catch(Exception e){if(art!=null)UnityEngine.Object.Destroy(art);_reason="Original fallback: "+e.Message;RefusalV1(false,"Building "+b.Seed,e.Message);return false;}
            }
            // Every original submesh remains independently restorable; never disable a shared city chunk.
            if(pair.Failed)return false;
            try
            {
            Mesh mesh=source.ToMesh("FR01 retained source "+b.Seed);
            GameObject old=Shapes.MeshObject("Original material",mesh,new[]{material},pair.Legacy.transform,true);OwnedMeshes.Track(old,mesh);
            return true;
            }
            catch(Exception e){pair.Failed=true;pair.Art.SetActive(false);pair.Legacy.SetActive(true);_reason="Building rolled back: "+e.Message;RefusalV1(false,"Building rollback "+b.Seed,e.Message);return false;}
        }
        public void BuildRoute()
        {
            if(_built)return;Physics.SyncTransforms();_built=true;BuildSurfaceWR02();BuildWorldRewriteWR01();BuildLiving();BuildCompositionWR02B();if(_catalog==null || !_catalog.Ready){_reason=_catalog!=null?_catalog.Reason:"FR01 prepared catalog missing.";RefusalV1(false,"FR01 preparation",_reason,false,false);return;}
            List<GameObject> pending=new List<GameObject>();
            try
            {
                Vector3 at;float yaw;
                for(int attempt=0;attempt<HeartPlanFR01.GateCandidates;attempt++)
                {
                    string sourceReason;
                    if(!HeartPlanFR01.GateCandidate(_layout,attempt,out at,out yaw,out sourceReason)){_gateReason=sourceReason;RefusalV1(false,"Gateway source candidate "+attempt,sourceReason);continue;}
                    // Compact 14m opening; no X stretch. Probe the full towers and the overhead arch as well as the bases.
                    bool clear=GateSpace(at,yaw,out sourceReason);Quaternion rot=Quaternion.Euler(0,yaw,0);
                    if(clear)
                    {
                        GameObject g=Model("HeartGate",at,yaw,Vector3.one);pending.Add(g);
                        // New gate bases only. No carving mesh is used as a collider and no original collider is altered.
                        foreach(int sign in new[]{-1,1}){GameObject solid=new GameObject("Gate base proxy");solid.transform.SetParent(g.transform,false);solid.transform.localPosition=new Vector3(sign*HeartPlanFR01.GatePierOffset,6,0);solid.layer=Layers.World;solid.AddComponent<BoxCollider>().size=new Vector3(5,12,5);}
                        _approach=at-rot*Vector3.forward*12;_gate=true;_gatePosition=at;_gateYaw=yaw;_gateReason="Gateway staged at "+at.ToString("F1")+", candidate "+attempt+".";break;
                    }
                    _gateReason="Candidate "+attempt+" refused: "+sourceReason;NativeRefusalV1(false,"Gateway native candidate "+attempt,sourceReason);Log.Info("FR01 "+_gateReason);
                }
                // Paving overlays only the admitted existing public routes, no new collision or route identity.
                if(!TryStreetWR02D(pending))foreach(RealmRoute04OB r in _layout.RealmRoutes04OB)
                {
                    if(r.Id!="home-court" && r.Id!="forge-market" && r.Id!="court-crossing" && r.Id!="sanctuary-approach")continue;
                    for(int i=1;i<r.Points.Count;i++)
                    {
                        Vector3 a=r.Points[i-1],b=r.Points[i],d=MathUtil.Flat(b-a);float length=d.magnitude;
                        int n=Mathf.CeilToInt(length/4);for(int j=0;j<n;j++)
                        {Vector3 p=Vector3.Lerp(a,b,(j+.5f)/n)+Vector3.up*.021f;pending.Add(Model("HeartStreet",p,MathUtil.YawOf(d),new Vector3(r.Width/4,1,length/n/4)));}
                    }
                }
                // Side dressing is admitted only outside original routes, parcels and receipt clearances.
                foreach(int sign in new[]{-1,1})for(int i=0;i<3;i++)
                {
                    Vector3 p=_layout.RealmCourt04OB+new Vector3(sign*(34+i*3),0,(i-1)*20);
                    AddSide(p,i%2==0?"HeartRootCourt":"HeartIvyRuin",sign*20,pending,3);
                }
                if(_catalog.Find("ReuseShardwoodElder")!=null)AddSide(_layout.RealmCourt04OB+new Vector3(-38,0,20),"ReuseShardwoodElder",25,pending,3);
                // Genuine arcade bays frame the existing street, never a new navigation lane.
                foreach(HeartSideFR01 side in HeartPlanFR01.StreetSides(_layout))AddSide(side.Position,side.Module,side.Yaw,pending,side.Radius);
                // The real sanctuary remains H5. The votive sits beside, never on, its original approach.
                foreach(RealmRoute04OB route in _layout.RealmRoutes04OB)if(route.Id=="sanctuary-approach" && route.Points.Count>0)
                {Vector3 p=route.Points[route.Points.Count-1];AddSide(p+Vector3.right*11,"HeartFocus",0,pending,2);break;}
                _dressing.AddRange(pending);_selected=PlayerPrefs.GetInt("NSLV_heart_fr01",0)==1;
                _reason=(_gate?"Original public walking corridors retained. ":"Gateway FAILED admission, original route retained. ")+"Original H3/H5 Archive, Lofts, Forge, actors and collision remain. Native review pending.";
            }
            catch(Exception e){foreach(GameObject p in pending)if(p!=null)UnityEngine.Object.Destroy(p);_gate=false;_reason="Route rolled back: "+e.Message;RefusalV1(false,"Route rollback",e.Message,false,false);}
            Log.Info(Status);
        }
        private void AddSide(Vector3 p,string module,float yaw,List<GameObject> pending,float radius)
        {
            if(_gate && !HeartPlanFR01.OutsideGate(p,radius,_gatePosition,_gateYaw)){_refused++;RefusalV1(false,"Side "+module,"Original gateway clearance.");return;}
            if(!HeartPlanFR01.Clear(_layout,p,radius)){_refused++;RefusalV1(false,"Side "+module,"Original route, parcel or receipt clearance.");return;}
            if(Physics.CheckBox(p+Vector3.up*3.6f,new Vector3(radius,3.3f,radius),Quaternion.identity,Layers.WorldMask,QueryTriggerInteraction.Collide)){_refused++;NativeRefusalV1(false,"Side "+module,"Original solid overlaps the dressing reservation.");return;}
            foreach(Collider c in Physics.OverlapBox(p+Vector3.up*3,new Vector3(radius,3,radius),Quaternion.identity,~0,QueryTriggerInteraction.Collide))if(c.isTrigger){_refused++;NativeRefusalV1(false,"Side "+module,"Protected trigger "+c.name);return;}
            pending.Add(Model(module,p,yaw,Vector3.one));
        }
        private static bool GateSpace(Vector3 at,float yaw,out string reason)
        {
            Quaternion rotation=Quaternion.Euler(0,yaw,0);reason="";
            foreach(int sign in new[]{-1,1})if(!Probe(at+rotation*new Vector3(sign*HeartPlanFR01.GatePierOffset,10.6f,0),new Vector3(3.2f,10.4f,3.4f),rotation,out reason))return false;
            return Probe(at+Vector3.up*10.9f,new Vector3(7.9f,5.3f,1.8f),rotation,out reason);
        }
        private static bool Probe(Vector3 center,Vector3 extents,Quaternion rotation,out string reason)
        {
            foreach(Collider c in Physics.OverlapBox(center,extents,rotation,~0,QueryTriggerInteraction.Collide))
            {
                if(c.isTrigger){reason="protected trigger "+c.name+" at "+center.ToString("F1")+".";return false;}
                if(c.gameObject.layer==Layers.World){reason="original solid "+c.name+" at "+center.ToString("F1")+".";return false;}
            }
            reason="";return true;
        }
        public bool Route(out Vector3 point){point=_approach;return _gate;}
        public void Toggle()
        {
            if(_proofComparisonV1)return;
            bool next=!Selected;
            // Never turn a new gate base on around an actor already occupying its volume.
            if(next)foreach(GameObject g in _dressing)foreach(BoxCollider c in g.GetComponentsInChildren<BoxCollider>(true))
            {
                Vector3 p=c.transform.TransformPoint(c.center);Vector3 ext=Vector3.Scale(c.size*.5f,c.transform.lossyScale);
                foreach(Collider hit in Physics.OverlapBox(p,ext,c.transform.rotation,~0,QueryTriggerInteraction.Collide))if(hit.GetComponentInParent<CharacterBase>()!=null || hit.GetComponentInParent<Vehicle>()!=null){_reason="Move people and vehicles away from the gate bases before enabling.";NativeRefusalV1(false,"Gateway activation",_reason,true);return;}
            }
            if(_fantasyPreviewV1)_previewHeartV1=next;else{_selected=next;PlayerPrefs.SetInt("NSLV_heart_fr01",next?1:0);PlayerPrefs.Save();}Apply();Log.Info(Status);
        }
        public void State(bool compare,bool suspended){_requestedCompareV1=compare;bool effective=_proofComparisonV1?_proofLegacyV1:compare;if(_compare==effective && _suspended==suspended)return;_compare=effective;_suspended=suspended;Apply();}
        private void Apply()
        {
            ApplySurfaceWR02();
            ApplyCompositionWR02B();
            ApplyWorldRewriteWR01();
            ApplyLiving();
            bool active=HeartPlanFR01.Active(Selected,_compare,_suspended,_catalog!=null && _catalog.Ready);if(active==_last)return;
            if(active)foreach(GameObject g in _dressing)foreach(BoxCollider c in g.GetComponentsInChildren<BoxCollider>(true))
            {foreach(Collider hit in Physics.OverlapBox(c.transform.TransformPoint(c.center),Vector3.Scale(c.size*.5f,c.transform.lossyScale),c.transform.rotation,~0,QueryTriggerInteraction.Collide))if(hit.GetComponentInParent<CharacterBase>()!=null || hit.GetComponentInParent<Vehicle>()!=null){_reason="Appearance refused: an actor or vehicle occupies a gate base. Move clear, then toggle OFF/ON.";NativeRefusalV1(false,"Gateway activation",_reason,true);return;}}
            // New models already exist. Switching never recooks or releases original physical geometry.
            foreach(KeyValuePair<Building,Pair> entry in _buildings)
            {
                Pair p=entry.Value;
                string problem;
                if(!TryShowHouseFR01(p.Art,p.Legacy,active && !p.Failed,out problem))
                {
                    p.Failed=true;_reason="Building original retained: "+problem;
                    RefusalV1(false,"Building activation "+entry.Key.Seed,problem,true,false);
                }
            }
            foreach(GameObject g in _dressing)if(g!=null)g.SetActive(active);_last=active;
        }
        private static bool TryShowHouseFR01(GameObject art,GameObject legacy,bool active,out string problem)
        {
            problem="";
            if(active && (legacy==null || !ValidPreparedModelV1(art,out problem)))
            {
                if(legacy==null)problem="Original building visual is missing.";
                if(art!=null)art.SetActive(false);if(legacy!=null)legacy.SetActive(true);return false;
            }
            if(art!=null)art.SetActive(active);
            if(legacy!=null)legacy.SetActive(!active);
            return true;
        }
    }
}
