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
        private readonly List<Mesh> _meshes=new List<Mesh>();
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
                    Mesh mesh=part.Mesh.ToMesh("Dante freight / "+part.Surface);view._meshes.Add(mesh);
                    shape.AddComponent<MeshFilter>().sharedMesh=mesh;shape.AddComponent<MeshRenderer>().sharedMaterial=material;
                }
                view.Box(ForgeFreightModel.Body,new Vector3(0,1.53f,-.03f),new Vector3(1.48f,1.40f,2.75f));
                view.Box(ForgeFreightModel.Head,new Vector3(0,.15f,.40f),new Vector3(.85f,.68f,1.12f));
                view.Box(ForgeFreightModel.Wagon,new Vector3(0,.97f,0),new Vector3(1.72f,.85f,2.68f));
                for(int i=3;i<=4;i++)
                {
                    // Sixteen narrow tangential segments approximate the actual annular tyre.
                    // The gaps between the spokes stay open and no convex cooking is required.
                    for(int segment=0;segment<16;segment++)
                    {
                        float angle=segment*22.5f;
                        GameObject contact=new GameObject("Rim contact");contact.transform.SetParent(view._joints[i],false);
                        contact.transform.localRotation=QMath.AX(angle);
                        contact.transform.localPosition=contact.transform.localRotation*Vector3.up*.592f;
                        contact.AddComponent<BoxCollider>().size=new Vector3(.15f,.084f,.233f);
                    }
                    view._joints[i].gameObject.AddComponent<SphereCollider>().radius=.145f;
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
        private bool Free(ForgeFreightEnvelope zone,Vehicle source)
        {
            Quaternion q=QMath.LookRotation(zone.Forward,Vector3.up);
            int mask=~((1<<Layers.IgnoreRaycast)|(1<<Layers.Triggers)|(1<<Layers.Minimap));
            int n=Physics.OverlapBoxNonAlloc(zone.Centre,zone.Half,_hits,q,mask,QueryTriggerInteraction.Ignore);
            if(n==_hits.Length)return false;
            for(int i=0;i<n;i++)
            {
                Collider c=_hits[i];if(c.transform.IsChildOf(transform))continue;
                if(source!=null && (c.transform.IsChildOf(source.transform) || c.transform.IsChildOf(source.FreightShell.transform)))continue;
                return false;
            }
            // The player's capsule can be temporarily disabled by a travel mode.
            return Game.Player==null || !zone.Contains(Game.PlayerPosition+Vector3.up*.85f,.34f,.85f);
        }

        public bool EnvelopeFree(ForgeFreightFrame frame,Vehicle source)
        {
            return Free(ForgeFreightEnvelope.Animal(frame),source) && Free(ForgeFreightEnvelope.Wagon(frame),source);
        }
        public bool VanFree(Vector3 road,Vector3 dir,Vehicle van)
        {
            return Free(new ForgeFreightEnvelope(road+Vector3.up*(van.Def.Height*.5f+.15f),
                new Vector3(van.Def.Width*.5f+.36f,van.Def.Height*.5f,van.Def.Length*.5f+.55f),dir),van);
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
            if(Mathf.Max(Mathf.Max(front,hind),Mathf.Max(left,right))-Mathf.Min(Mathf.Min(front,hind),Mathf.Min(left,right))>.12f)return false;
            f.Animal.y=(front+hind)*.5f;f.Axle.y=(left+right)*.5f;return true;
        }
        public void Dispose()
        {
            gameObject.SetActive(false);
            ReleaseAssets();Destroy(gameObject);
        }
        private void ReleaseAssets()
        {
            foreach(Material m in _materials.Values)if(m!=null)Destroy(m);
            foreach(Mesh mesh in _meshes)if(mesh!=null)Destroy(mesh);
            _materials.Clear();_meshes.Clear();
        }
        private void OnDestroy(){ReleaseAssets();}
    }
}
