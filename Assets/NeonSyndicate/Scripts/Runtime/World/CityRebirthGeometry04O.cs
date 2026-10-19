using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    public struct CityMassPiece04O
    {
        public Vector3 Center, Size;
        public CityMassPiece04O(Vector3 center,Vector3 size) { Center=center;Size=size; }
    }
    /// <summary>Six actual structural grammars. Solid pieces are shared by rendering and collision.</summary>
    public static class CityRebirthGeometry04O
    {
        public static void ResetStatics() { }
        public static List<CityMassPiece04O> Solids(Building building)
        {
            RectF f=building.Footprint; float h=building.Height; float y=CityLayout.CurbHeight;
            List<CityMassPiece04O> pieces=new List<CityMassPiece04O>();
            int family=building.RebirthFamily04O;
            // A human-height lower storey, then narrower asymmetric upper construction opens the skyline.
            pieces.Add(new CityMassPiece04O(f.Center(y+3f),new Vector3(f.Width,6f,f.Depth)));
            float w=f.Width, d=f.Depth, x=f.CenterX, z=f.CenterZ;
            if(family==2) // broad inhabited terraces, offset toward the back of the court
            {
                pieces.Add(new CityMassPiece04O(new Vector3(x,y+6f+(h-6f)*.25f,z+d*.10f),new Vector3(w*.83f,(h-6f)*.5f,d*.75f)));
                pieces.Add(new CityMassPiece04O(new Vector3(x-w*.09f,y+6f+(h-6f)*.75f,z+d*.18f),new Vector3(w*.56f,(h-6f)*.5f,d*.5f)));
            }
            else if(family==3) // forge nave and off-axis furnace tower
            {
                pieces.Add(new CityMassPiece04O(new Vector3(x-w*.14f,y+(h+6f)*.5f,z),new Vector3(w*.68f,h-6f,d*.78f)));
                pieces.Add(new CityMassPiece04O(new Vector3(x+w*.34f,y+(h+6f)*.5f,z+d*.27f),new Vector3(w*.25f,h-6f,d*.35f)));
            }
            else if(family==4) // long elevated flood-control house with back terraces
            {
                pieces.Add(new CityMassPiece04O(new Vector3(x,y+(h+6f)*.5f,z+d*.16f),new Vector3(w*.84f,h-6f,d*.59f)));
            }
            else if(family==5) // broken signal wings deliberately leave a deep central notch
            {
                pieces.Add(new CityMassPiece04O(new Vector3(x-w*.27f,y+(h+6f)*.5f,z+d*.1f),new Vector3(w*.31f,h-6f,d*.71f)));
                pieces.Add(new CityMassPiece04O(new Vector3(x+w*.26f,y+6f+(h-6f)*.32f,z-d*.04f),new Vector3(w*.3f,(h-6f)*.64f,d*.61f)));
            }
            else if(family==6) // garden towers are narrow stepped stacks, not full-width curtain walls
            {
                pieces.Add(new CityMassPiece04O(new Vector3(x,y+6f+(h-6f)*.3f,z),new Vector3(w*.71f,(h-6f)*.6f,d*.70f)));
                pieces.Add(new CityMassPiece04O(new Vector3(x+w*.10f,y+6f+(h-6f)*.8f,z+d*.1f),new Vector3(w*.45f,(h-6f)*.4f,d*.45f)));
            }
            else // civic mineral hall: a broad shoulder and tall recessed central hall
            {
                pieces.Add(new CityMassPiece04O(new Vector3(x,y+6f+(h-6f)*.28f,z),new Vector3(w*.89f,(h-6f)*.56f,d*.82f)));
                pieces.Add(new CityMassPiece04O(new Vector3(x,y+6f+(h-6f)*.78f,z+d*.13f),new Vector3(w*.57f,(h-6f)*.44f,d*.54f)));
            }
            return pieces;
        }
        public static void Building(MeshBuilder stone,MeshBuilder metal,MeshBuilder crystal,Building building,bool near)
        {
            RectF f=building.Footprint; float y=CityLayout.CurbHeight;
            foreach(CityMassPiece04O piece in Solids(building)) stone.Box(0,piece.Center,piece.Size,UVMode.World);
            int family=building.RebirthFamily04O;
            foreach(CityMassPiece04O piece in Solids(building))
            {
                Vector3 roof=piece.Center+Vector3.up*piece.Size.y*.5f;
                metal.Box(0,roof,new Vector3(piece.Size.x,.16f,piece.Size.z),UVMode.World);
                if(near && family==6) // physical planted roof trays on the setback ledges
                    for(int n=0;n<3;n++) stone.Box(0,roof+new Vector3((n-1)*piece.Size.x*.24f,.3f,-piece.Size.z*.35f),new Vector3(piece.Size.x*.17f,.6f,1.2f),UVMode.World);
            }
            int ribs=near?4:2;
            for(int side=-1;side<=1;side+=2) for(int i=0;i<ribs;i++)
            {
                float z=Mathf.Lerp(f.Z0+1f,f.Z1-1f,(i+.5f)/ribs);
                float height=family==3?building.Height*.65f:family==5?building.Height*.8f:4.8f;
                Vector3 at=new Vector3(side<0?f.X0+.25f:f.X1-.25f,y,z);
                // Faceted mineral ribs replace repeated window registers. They stay inside the solid ground storey.
                Shard(stone,at,new Vector3(.46f,Mathf.Min(5.5f,height),.85f),.14f*side);
                if(near) crystal.Box(0,at+new Vector3(0f,2.7f,0f),new Vector3(.5f,.13f,.5f),UVMode.World);
            }
            // Only a handful of deep-looking glazing slits, never a city-height repeating window texture.
            int bays=near?3:1;
            for(int i=0;i<bays;i++)
            {
                float x=Mathf.Lerp(f.X0+1f,f.X1-1f,(i+.5f)/bays);
                crystal.Box(0,new Vector3(x,y+2.7f,f.Z0-.012f),new Vector3(.34f,1.3f,.03f),UVMode.World);
                metal.Box(0,new Vector3(x,y+3.7f,f.Z0-.025f),new Vector3(.75f,.14f,.12f),UVMode.World);
            }
            if(family==3 || family==5)
            {
                Vector3 roof=f.Center(y+building.Height);
                for(int n=0;n<(near?3:2);n++) Shard(metal,roof+new Vector3((n-1)*f.Width*.12f,0f,0f),new Vector3(f.Width*.09f,2f+n*1.5f,f.Depth*.12f),family==5?.3f:0f);
            }
        }
        /// <summary>Closed triangular mineral prism; tilted tip changes silhouette without glow.</summary>
        public static void Shard(MeshBuilder mesh,Vector3 origin,Vector3 size,float lean)
        {
            Vector3[] bottom={origin+new Vector3(-size.x*.5f,0f,-size.z*.5f),origin+new Vector3(size.x*.5f,0f,-size.z*.5f),origin+new Vector3(0f,0f,size.z*.5f)};
            Vector3[] top={origin+new Vector3(-size.x*.18f+lean*size.y,size.y*.82f,-size.z*.18f),origin+new Vector3(size.x*.18f+lean*size.y,size.y*.82f,-size.z*.18f),origin+new Vector3(lean*size.y,size.y*.82f,size.z*.2f)};
            Vector3 tip=origin+new Vector3(lean*size.y,size.y,0f);
            for(int i=0;i<3;i++)
            {
                int j=(i+1)%3; Vector3 normal=Vector3.Cross(top[i]-bottom[i],bottom[j]-bottom[i]).normalized;
                mesh.Quad(0,bottom[i],top[i],top[j],bottom[j],normal,Vector2.zero,Vector2.up,Vector2.one,Vector2.right);
                Vector3 tn=Vector3.Cross(tip-top[i],top[j]-top[i]).normalized;
                mesh.Triangle(0,top[i],tip,top[j],tn,Vector2.zero,Vector2.up,Vector2.right);
            }
            mesh.Triangle(0,bottom[2],bottom[1],bottom[0],Vector3.down,Vector2.zero,Vector2.right,Vector2.up);
        }
        public static void Spine(MeshBuilder stone,MeshBuilder metal,MeshBuilder crystal,SpineSite04O site,bool near)
        {
            Vector3 p=site.Position; float r=site.Radius,h=site.Height;
            // The same three primary ribs exist at every preset and distance.
            for(int i=0;i<3;i++)
            {
                float angle=(i*120f+site.Character*29f)*Mathf.Deg2Rad;
                Vector3 foot=p+new Vector3(Mathf.Cos(angle)*r*.4f,0f,Mathf.Sin(angle)*r*.4f);
                Shard(i==1?crystal:stone,foot,new Vector3(r*.55f,h*(i==0?1f:i==1?.73f:.46f),r*.48f),Mathf.Cos(angle)*.07f);
            }
            int rings=site.Character==1?4:2;
            for(int ring=0;ring<rings;ring++) for(int arm=0;arm<(near?8:4);arm++)
            {
                float angle=arm*(near?45f:90f)*Mathf.Deg2Rad;
                Vector3 at=p+new Vector3(Mathf.Cos(angle)*r*.68f,h*(.2f+ring*.15f),Mathf.Sin(angle)*r*.68f);
                metal.Box(0,at,new Vector3(r*.3f,.5f,r*.18f),QMath.AY(-angle*Mathf.Rad2Deg),UVMode.World);
            }
        }
    }
}
