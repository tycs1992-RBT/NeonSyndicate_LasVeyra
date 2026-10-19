using UnityEngine;

namespace NeonSyndicate
{
    public enum SagaSet04L { ManorFracture, GhostLights, Clinic, Ashline, Market, Transmitter, Heart }

    /// <summary>Physical set dressing attached to the original mission lifetime; no new colliders, actors, lights or spawner.</summary>
    public static class StoryStage04L
    {
        public static MeshBuilder Geometry(SagaSet04L kind)
        {
            MeshBuilder b = new MeshBuilder(2);
            if (kind == SagaSet04L.ManorFracture || kind == SagaSet04L.Heart)
            {
                for (int side = -1; side <= 1; side += 2)
                    for (int tier = 0; tier < 4; tier++)
                    {
                        Vector3 p = new Vector3(side * (2.1f + tier * .12f), .4f + tier * .8f, 0f);
                        b.Box(0,p,new Vector3(.9f,.72f,.65f),QMath.AY(side*7f*tier),UVMode.World);
                        b.Box(1,p+new Vector3(-side*.46f,.1f,.34f),new Vector3(.05f,.25f,.03f),QMath.AZ(side*23f),UVMode.World);
                    }
            }
            else if (kind == SagaSet04L.GhostLights)
            {
                for(int n=0;n<5;n++) for(int side=-1;side<=1;side+=2)
                {
                    Vector3 p=new Vector3(side*4.5f,.10f,n*3f);
                    b.Box(0,p,new Vector3(.5f,.14f,.8f),QMath.Identity,UVMode.World);
                    b.Box(1,p+Vector3.up*.09f,new Vector3(.18f,.04f,.5f),QMath.Identity,UVMode.World);
                }
            }
            else if (kind == SagaSet04L.Clinic)
            {
                // Wall-side shelter lanterns. The middle remains an open 4 m route.
                for(int side=-1;side<=1;side+=2)
                {
                    b.Box(0,new Vector3(side*2.5f,1.3f,0),new Vector3(.14f,2.6f,.14f),QMath.Identity,UVMode.World);
                    b.Box(0,new Vector3(side*2.5f,2.55f,0),new Vector3(.6f,.12f,.6f),QMath.Identity,UVMode.World);
                    b.Box(1,new Vector3(side*2.5f,2.3f,0),new Vector3(.28f,.36f,.28f),QMath.Identity,UVMode.World);
                }
            }
            else if(kind == SagaSet04L.Market)
            {
                for(int side=-1;side<=1;side+=2)
                {
                    b.Box(0,new Vector3(side*3.6f,1f,0),new Vector3(1.6f,.12f,1.2f),QMath.Identity,UVMode.World);
                    for(int j=-1;j<=1;j+=2) b.Box(0,new Vector3(side*3.6f+j*.7f,1.6f,0),new Vector3(.08f,3.2f,.08f),QMath.Identity,UVMode.World);
                    b.Box(0,new Vector3(side*3.6f,3.15f,0),new Vector3(1.8f,.16f,1.5f),QMath.AZ(side*6f),UVMode.World);
                    for(int j=0;j<3;j++) b.Box(1,new Vector3(side*3.6f+(j-1)*.3f,1.2f,0),new Vector3(.16f,.3f,.16f),QMath.AZ(j*12f),UVMode.World);
                }
            }
            else
            {
                for(int n=0;n<3;n++)
                {
                    float x=(n-1)*.48f;
                    b.Box(0,new Vector3(x,1f,0),new Vector3(.3f,2f,.35f),QMath.Identity,UVMode.World);
                    for(int j=0;j<4;j++)b.Box(1,new Vector3(x,.4f+j*.4f,.19f),new Vector3(.18f,.08f,.05f),QMath.Identity,UVMode.World);
                }
                b.Box(0,new Vector3(0,.13f,0),new Vector3(1.8f,.26f,.8f),QMath.Identity,UVMode.World);
            }
            return b;
        }
        public static GameObject Place(MissionContext c, Vector3 at, SagaSet04L kind, float yaw = 0f)
        {
            if(c==null || c.M==null)return null;
            GameObject root=new GameObject("04L " + kind);
            root.transform.SetParent(c.M.transform,false);
            root.transform.position=at;root.transform.rotation=QMath.AY(yaw);
            Color glow=kind==SagaSet04L.Clinic?new Color(1f,.65f,.24f):kind==SagaSet04L.Ashline?new Color(.9f,.25f,.08f):new Color(.24f,.7f,.9f);
            Shapes.MeshObject("04L staged geometry",OwnedMeshes.Track(root,Geometry(kind).ToMesh("04L "+kind)),
                new[]{Mats.Concrete,Mats.Emissive(glow,1.15f)},root.transform,false);
            c.Objects.Add(root);
            return root;
        }
        public static void Lantern(GameObject root)
        {
            MeshBuilder b=new MeshBuilder(2);
            for(int s=-1;s<=1;s+=2) b.Box(0,new Vector3(s*.06f,0,0),new Vector3(.015f,.18f,.1f),QMath.Identity,UVMode.World);
            b.Box(0,new Vector3(0,.1f,0),new Vector3(.15f,.02f,.12f),QMath.Identity,UVMode.World);
            b.Box(0,new Vector3(0,-.1f,0),new Vector3(.15f,.02f,.12f),QMath.Identity,UVMode.World);
            b.Sphere(1,Vector3.zero,new Vector3(.035f,.07f,.035f),3,6,QMath.Identity);
            Shapes.MeshObject("Luma crystal lantern",OwnedMeshes.Track(root,b.ToMesh("04L carried lantern")),new[]{Mats.Concrete,Mats.Emissive(new Color(.35f,.8f,1f),1.3f)},root.transform,false);
        }
    }
}
