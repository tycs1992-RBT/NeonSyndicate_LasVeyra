using System;
using System.Collections.Generic;
using UnityEngine;
namespace NeonSyndicate
{
    /// <summary>Bounded visual lifetime owned by CityBuilder and LiveWorld. Never owns saves, actors or original collision.</summary>
    public sealed class HeartPresentationFR01
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
        private int _refused;
        public HeartPresentationFR01(CityLayout layout,Transform parent)
        { _layout=layout;_parent=parent;_catalog=Resources.Load<HeartCatalogFR01>("HeartPreparedFR01/Catalog"); }
        public bool Selected { get { return _selected; } }
        public string Status { get { return "FR01 "+(_selected?"ON":"OFF")+"; staged masses "+_buildings.Count+", showing "+(_last?_buildings.Count:0)+"; preserved in-slice emitters "+_preserved.Count+"; families "+_families+"; refused placements "+_refused+"; route modules "+_dressing.Count+". "+_reason; } }
        private static bool Passive(GameObject source)
        {
            if(source==null)return false;
            foreach(Component c in source.GetComponentsInChildren<Component>(true))if(c==null || !(c is Transform || c is MeshFilter || c is MeshRenderer))return false;
            foreach(MeshFilter f in source.GetComponentsInChildren<MeshFilter>(true))if(f.sharedMesh==null || f.sharedMesh.vertexCount<3)return false;
            return source.GetComponentsInChildren<MeshRenderer>(true).Length>0;
        }
        private GameObject Model(string id,Vector3 position,float yaw,Vector3 scale)
        {
            HeartModuleFR01 m=_catalog!=null?_catalog.Find(id):null;
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
                    levels[i]=new LOD(i==0?.45f:i==1?.15f:.025f,copy.GetComponentsInChildren<Renderer>(true));
                }
                LODGroup group=root.AddComponent<LODGroup>();group.SetLODs(levels);group.RecalculateBounds();return root;
            }
            catch { UnityEngine.Object.Destroy(root);throw; }
        }
        public bool Capture(Building b,MeshBuilder source,Material material)
        {
            if(_catalog==null || !_catalog.Ready || !HeartPlanFR01.Eligible(_layout,b)){if(HeartPlanFR01.InSlice(_layout,b.Footprint.Center(0)))_preserved.Add(b);return false;}
            Pair pair;
            if(!_buildings.TryGetValue(b,out pair))
            {
                GameObject art=null;
                try
                {
                    string id=(b.Seed&1)==0?"HeartHouseA":"HeartHouseB";HeartModuleFR01 module=_catalog.Find(id);
                    Vector3 size=module.Size;if(size.x<=0 || size.y<=0 || size.z<=0)return false;
                    art=Model(id,b.Footprint.Center(CityLayout.CurbHeight),0,new Vector3(b.Footprint.Width/size.x,b.Height/size.y,b.Footprint.Depth/size.z));
                    GameObject legacy=new GameObject("FR01 isolated original / "+b.Seed);legacy.transform.SetParent(_parent,false);
                    pair=new Pair{Legacy=legacy,Art=art};_buildings.Add(b,pair);_families+=id+" seed "+b.Seed+"; ";
                }
                catch(Exception e){if(art!=null)UnityEngine.Object.Destroy(art);_reason="Original fallback: "+e.Message;return false;}
            }
            // Every original submesh remains independently restorable; never disable a shared city chunk.
            if(pair.Failed)return false;
            try
            {
            Mesh mesh=source.ToMesh("FR01 retained source "+b.Seed);
            GameObject old=Shapes.MeshObject("Original material",mesh,new[]{material},pair.Legacy.transform,true);OwnedMeshes.Track(old,mesh);
            return true;
            }
            catch(Exception e){pair.Failed=true;pair.Art.SetActive(false);pair.Legacy.SetActive(true);_reason="Building rolled back: "+e.Message;return false;}
        }
        public void BuildRoute()
        {
            if(_built)return;_built=true;if(_catalog==null || !_catalog.Ready)return;
            List<GameObject> pending=new List<GameObject>();
            try
            {
                Vector3 at;float yaw;
                for(int attempt=0;attempt<9;attempt++)
                if(HeartPlanFR01.Gate(_layout,out at,out yaw,attempt))
                {
                    // Original walk width is 9m; the fitted opening is over 39m. Piers must also pass real static/trigger probes.
                    bool clear=true;Quaternion rot=Quaternion.Euler(0,yaw,0);
                    foreach(int sign in new[]{-1,1})
                    {
                        Vector3 p=at+rot*new Vector3(sign*22,6,0);
                        if(Physics.CheckBox(p,new Vector3(3.2f,5.9f,2.6f),rot,Layers.WorldMask,QueryTriggerInteraction.Collide))clear=false;
                        foreach(Collider c in Physics.OverlapBox(p,new Vector3(3.5f,6,3),rot,~0,QueryTriggerInteraction.Collide))if(c.isTrigger)clear=false;
                    }
                    if(clear)
                    {
                        GameObject g=Model("HeartGate",at,yaw,new Vector3(11f/9f,1,1));pending.Add(g);
                        // New gate bases only. No carving mesh is used as a collider and no original collider is altered.
                        foreach(int sign in new[]{-1,1}){GameObject solid=new GameObject("Gate base proxy");solid.transform.SetParent(g.transform,false);solid.transform.localPosition=new Vector3(sign*18,6,0);solid.layer=Layers.World;solid.AddComponent<BoxCollider>().size=new Vector3(5,12,5);}
                        _approach=at-rot*Vector3.forward*12;_gate=true;break;
                    }
                    else _reason="Gate refused: original solid or trigger in a pier envelope.";
                }
                // Paving overlays only the admitted existing public routes, no new collision or route identity.
                foreach(RealmRoute04OB r in _layout.RealmRoutes04OB)
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
                foreach(int sign in new[]{-1,1})for(int i=0;i<4;i++)
                {
                    Vector3 p=_layout.RealmCourt04OB+new Vector3(sign*15,0,12+i*7);
                    AddSide(p,i%2==0?"HeartArcade":"HeartBlindBay",sign*90,pending,3);
                }
                // The real sanctuary remains H5. The votive sits beside, never on, its original approach.
                foreach(RealmRoute04OB route in _layout.RealmRoutes04OB)if(route.Id=="sanctuary-approach" && route.Points.Count>0)
                {Vector3 p=route.Points[route.Points.Count-1];AddSide(p+Vector3.right*11,"HeartFocus",0,pending,2);break;}
                _dressing.AddRange(pending);_selected=PlayerPrefs.GetInt("NSLV_heart_fr01",0)==1;
                _reason=(_gate?"Gateway staged, original 9m walking corridor retained. ":"Gate refused by safety checks. ")+"Original H3/H5 Archive, Lofts, Forge, actors and collision remain. Native review pending.";
            }
            catch(Exception e){foreach(GameObject p in pending)if(p!=null)UnityEngine.Object.Destroy(p);_gate=false;_reason="Route rolled back: "+e.Message;}
            Log.Info(Status);
        }
        private void AddSide(Vector3 p,string module,float yaw,List<GameObject> pending,float radius)
        {
            if(!HeartPlanFR01.Clear(_layout,p,radius) || Physics.CheckBox(p+Vector3.up*3.6f,new Vector3(radius,3.3f,radius),Quaternion.identity,Layers.WorldMask,QueryTriggerInteraction.Collide)){_refused++;return;}
            foreach(Collider c in Physics.OverlapBox(p+Vector3.up*3,new Vector3(radius,3,radius),Quaternion.identity,~0,QueryTriggerInteraction.Collide))if(c.isTrigger){_refused++;return;}
            pending.Add(Model(module,p,yaw,Vector3.one));
        }
        public bool Route(out Vector3 point){point=_approach;return _gate;}
        public void Toggle()
        {
            bool next=!_selected;
            // Never turn a new gate base on around an actor already occupying its volume.
            if(next)foreach(GameObject g in _dressing)foreach(BoxCollider c in g.GetComponentsInChildren<BoxCollider>(true))
            {
                Vector3 p=c.transform.TransformPoint(c.center);Vector3 ext=Vector3.Scale(c.size*.5f,c.transform.lossyScale);
                foreach(Collider hit in Physics.OverlapBox(p,ext,c.transform.rotation,~0,QueryTriggerInteraction.Collide))if(hit.GetComponentInParent<CharacterBase>()!=null){_reason="Move people away from the gate bases before enabling.";return;}
            }
            _selected=next;PlayerPrefs.SetInt("NSLV_heart_fr01",next?1:0);PlayerPrefs.Save();Apply();Log.Info(Status);
        }
        public void State(bool compare,bool suspended){if(_compare==compare && _suspended==suspended)return;_compare=compare;_suspended=suspended;Apply();}
        private void Apply()
        {
            bool active=HeartPlanFR01.Active(_selected,_compare,_suspended,_catalog!=null && _catalog.Ready);if(active==_last)return;
            if(active)foreach(GameObject g in _dressing)foreach(BoxCollider c in g.GetComponentsInChildren<BoxCollider>(true))
            {foreach(Collider hit in Physics.OverlapBox(c.transform.TransformPoint(c.center),Vector3.Scale(c.size*.5f,c.transform.lossyScale),c.transform.rotation,~0,QueryTriggerInteraction.Collide))if(hit.GetComponentInParent<CharacterBase>()!=null){_reason="Appearance refused: an actor occupies a gate base. Move clear, then toggle OFF/ON.";return;}}
            // New models already exist. Switching never recooks or releases original physical geometry.
            foreach(Pair p in _buildings.Values){p.Art.SetActive(active && !p.Failed);p.Legacy.SetActive(!active || p.Failed);}
            foreach(GameObject g in _dressing)if(g!=null)g.SetActive(active);_last=active;
        }
    }
}
