using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    internal sealed class ForgeFreightView : MonoBehaviour
    {
        private readonly Transform[] _joints=new Transform[ForgeFreightModel.Count];
        private readonly ForgeFreightModel.JointPose[] _poses=new ForgeFreightModel.JointPose[ForgeFreightModel.Count];
        private readonly Dictionary<ForgeFreightModel.Finish,Material> _materials=new Dictionary<ForgeFreightModel.Finish,Material>();
        private readonly Collider[] _hits=new Collider[64];
        private readonly RaycastHit[] _floor=new RaycastHit[24];

        public static ForgeFreightView Prepare(Transform parent)
        {
            GameObject root=new GameObject("Dante mineral freight");root.SetActive(false);root.transform.SetParent(parent,false);
            ForgeFreightView view=root.AddComponent<ForgeFreightView>();
            try
            {
                Shader shader=Mats.StandardShader;
                if(shader==null || !shader.isSupported)throw new InvalidOperationException("Freight Lit shader unavailable");
                Rigidbody body=root.AddComponent<Rigidbody>();body.isKinematic=true;body.useGravity=false;
                ForgeFreightModel model=new ForgeFreightModel();
                for(int i=0;i<view._joints.Length;i++)
                {
                    GameObject joint=new GameObject("Freight joint "+i);joint.transform.SetParent(root.transform,false);view._joints[i]=joint.transform;
                }
                foreach(ForgeFreightModel.Piece part in model.Pieces)
                {
                    if(!view._materials.TryGetValue(part.Surface,out Material material))
                    {
                        material=new Material(shader);view._materials.Add(part.Surface,material);
                        material.name="Freight / "+part.Surface;
                        material.SetColor(RenderPath.IsURP?"_BaseColor":"_Color",ForgeFreightModel.Tint(part.Surface));
                        bool metal=part.Surface==ForgeFreightModel.Finish.Bronze || part.Surface==ForgeFreightModel.Finish.Iron;
                        material.SetFloat("_Metallic",metal?.55f:0);
                        material.SetFloat(RenderPath.IsURP?"_Smoothness":"_Glossiness",metal?.46f:.18f);
                        if(part.Surface==ForgeFreightModel.Finish.Dampener)
                        {material.EnableKeyword("_EMISSION");material.SetColor("_EmissionColor",new Color(.018f,.08f,.07f));}
                    }
                    GameObject shape=new GameObject(part.Surface.ToString());shape.transform.SetParent(view._joints[part.Joint],false);
                    Mesh mesh=OwnedMeshes.Track(root,part.Mesh.ToMesh("Dante freight / "+part.Surface));
                    shape.AddComponent<MeshFilter>().sharedMesh=mesh;shape.AddComponent<MeshRenderer>().sharedMaterial=material;
                }
                view.Box(ForgeFreightModel.Body,new Vector3(0,1.53f,-.03f),new Vector3(1.48f,1.40f,2.75f));
                view.Box(ForgeFreightModel.Head,new Vector3(0,.15f,.40f),new Vector3(.85f,.68f,1.12f));
                view.Box(ForgeFreightModel.Wagon,new Vector3(0,.97f,0),new Vector3(1.72f,.85f,2.68f));
                for(int i=3;i<=4;i++)
                {
                    GameObject rim=new GameObject("Wheel contact");rim.transform.SetParent(view._joints[i],false);
                    rim.transform.localScale=new Vector3(.19f,1,1);
                    MeshBuilder disc=new MeshBuilder(1);disc.Cylinder(0,Vector3.left*.50f,.64f,.64f,1,16,QMath.AZ(-90),true);
                    MeshCollider collider=rim.AddComponent<MeshCollider>();collider.sharedMesh=OwnedMeshes.Track(root,disc.ToMesh("Freight wheel contact"));collider.convex=true;
                }
                view.Box(ForgeFreightModel.Wagon,new Vector3(0,.86f,1.87f),new Vector3(.22f,.16f,1.46f));
                return view;
            }
            catch {view.Dispose();throw;}
        }
        private void Box(int joint,Vector3 centre,Vector3 size)
        { BoxCollider collider=_joints[joint].gameObject.AddComponent<BoxCollider>();collider.center=centre;collider.size=size; }

        public void Show(ForgeFreightRun run,ForgeFreightFrame frame,float effort,float clock)
        {
            transform.position=frame.Animal;
            ForgeFreightModel.Pose(run,frame,effort,clock,_poses);
            for(int i=0;i<_poses.Length;i++)
            {
                _joints[i].SetPositionAndRotation(_poses[i].Position,_poses[i].Rotation);
                _joints[i].localScale=_poses[i].Scale;
            }
        }

        // Fail closed on saturated queries. The road is below the swept boxes. Only our own compound is ignored.
        private bool Free(Vector3 centre,Vector3 half,Vector3 forward,Vehicle source)
        {
            Quaternion q=QMath.LookRotation(forward,Vector3.up);
            int mask=~((1<<Layers.IgnoreRaycast)|(1<<Layers.Triggers)|(1<<Layers.Minimap));
            int n=Physics.OverlapBoxNonAlloc(centre,half,_hits,q,mask,QueryTriggerInteraction.Ignore);
            if(n==_hits.Length)return false;
            for(int i=0;i<n;i++)
            {
                Collider c=_hits[i];if(c.transform.IsChildOf(transform))continue;
                if(source!=null && (c.transform.IsChildOf(source.transform) || c.transform.IsChildOf(source.FreightShell.transform)))continue;
                return false;
            }
            // The player's capsule can be temporarily disabled by a travel mode.
            if(Game.Player!=null)
            {
                Vector3 p=QMath.Inverse(q)*(Game.PlayerPosition+Vector3.up*.85f-centre);
                if(Mathf.Abs(p.x)<half.x+.34f && Mathf.Abs(p.y)<half.y+.85f && Mathf.Abs(p.z)<half.z+.34f)return false;
            }
            return true;
        }

        public bool EnvelopeFree(ForgeFreightFrame frame,Vehicle source)
        {
            if(!Free(frame.Animal+Vector3.up*1.47f+frame.Forward*.28f,new Vector3(1.09f,1.32f,2.17f),frame.Forward,source))return false;
            return Free(frame.Axle+Vector3.up*.91f+frame.WagonForward*.60f,new Vector3(1.30f,.77f,2.21f),frame.WagonForward,source);
        }
        public bool VanFree(Vector3 road,Vector3 dir,Vehicle van)
        {
            return Free(road+Vector3.up*(van.Def.Height*.5f+.15f),new Vector3(van.Def.Width*.5f+.36f,van.Def.Height*.5f,van.Def.Length*.5f+.55f),dir,van);
        }

        public bool Supported(Vector3 plan,out float height)
        {
            int n=Physics.RaycastNonAlloc(plan+Vector3.up*1.1f,Vector3.down,_floor,1.7f,Layers.WorldMask,QueryTriggerInteraction.Ignore);
            height=float.NegativeInfinity;if(n==_floor.Length)return false;
            for(int i=0;i<n;i++)
            {
                RaycastHit hit=_floor[i];
                if(hit.collider.transform.IsChildOf(transform))continue;
                if(hit.normal.y<.95f || Mathf.Abs(hit.point.y-plan.y)>.22f)return false;
                height=Mathf.Max(height,hit.point.y);
            }
            return !float.IsNegativeInfinity(height);
        }
        public bool Ground(ref ForgeFreightFrame f)
        {
            Vector3 side=new Vector3(f.WagonForward.z,0,-f.WagonForward.x)*ForgeFreightRun.Track;
            if(!Supported(f.Animal+f.Forward*1.1f,out float front) || !Supported(f.Animal-f.Forward,out float hind) ||
                !Supported(f.Axle-side,out float left) || !Supported(f.Axle+side,out float right))return false;
            if(Mathf.Max(front,hind,left,right)-Mathf.Min(front,hind,left,right)>.12f)return false;
            f.Animal.y=(front+hind)*.5f;f.Axle.y=(left+right)*.5f;return true;
        }
        public void Dispose()
        {
            gameObject.SetActive(false);
            foreach(Material m in _materials.Values)if(m!=null)Destroy(m);
            _materials.Clear();Destroy(gameObject);
        }
        private void OnDestroy(){foreach(Material m in _materials.Values)if(m!=null)Destroy(m);}
    }
}
