using System;
using UnityEngine;
namespace NeonSyndicate
{
    /// <summary>Original solid kit. Identical vertices supply visible stone/crystal and their exact collision meshes.</summary>
    public static partial class GothicRealmGeometry04OB
    {
        public static void ResetStatics() { }
        public static float FamilyHeight(int family){return family==2?16f:family==3?18f:family==4?12f:family==5?13f:family==6?8f:11f;}
        private static Vector3 At(Vector3 origin,float yaw,Vector3 local){return InhabitedCityBindings04C.LocalToWorld(origin,yaw,local);}
        private static void Quad(MeshBuilder m,Vector3 a,Vector3 b,Vector3 c,Vector3 d,Vector3 inside)
        {
            Vector3 normal=Vector3.Cross(b-a,c-a).normalized;
            if(Vector3.Dot(normal,(a+b+c+d)*.25f-inside)<0f){Vector3 swap=b;b=d;d=swap;normal=Vector3.Cross(b-a,c-a).normalized;}
            Func<Vector3,Vector2> uv=p=>Mathf.Abs(normal.y)>.6f?new Vector2(p.x,p.z)*.3f:Mathf.Abs(normal.x)>.6f?new Vector2(p.z,p.y)*.3f:new Vector2(p.x,p.y)*.3f;
            Vector3 second=Vector3.Cross(c-a,d-a).normalized;
            if(Vector3.Dot(normal,second)>.9999f)m.Quad(0,a,b,c,d,normal,uv(a),uv(b),uv(c),uv(d));
            else
            {
                // Irregular rock/quartz quads are not necessarily coplanar. Each real triangle needs its own face normal.
                m.Triangle(0,a,b,c,normal,uv(a),uv(b),uv(c));
                m.Triangle(0,a,c,d,second,uv(a),uv(c),uv(d));
            }
        }
        public static void Hex(MeshBuilder m,Vector3[] p)
        {
            Vector3 inside=Vector3.zero;foreach(Vector3 v in p)inside+=v;inside/=8f;
            Quad(m,p[0],p[1],p[2],p[3],inside);Quad(m,p[4],p[7],p[6],p[5],inside);
            Quad(m,p[0],p[4],p[5],p[1],inside);Quad(m,p[1],p[5],p[6],p[2],inside);
            Quad(m,p[2],p[6],p[7],p[3],inside);Quad(m,p[3],p[7],p[4],p[0],inside);
        }
        public static void Box(MeshBuilder m,Vector3 origin,float yaw,Vector3 center,Vector3 size)
        {
            Vector3[] p=new Vector3[8];
            for(int i=0;i<4;i++)
            {
                float x=(i==0||i==3)?-1f:1f,z=i<2?-1f:1f;
                p[i]=At(origin,yaw,center+new Vector3(x*size.x*.5f,-size.y*.5f,z*size.z*.5f));
                p[i+4]=At(origin,yaw,center+new Vector3(x*size.x*.5f,size.y*.5f,z*size.z*.5f));
            }
            Hex(m,p);
        }
        private static float ArchY(float t,float spring,float rise)
        {
            float u=2f*Mathf.Min(t,1f-t);
            return spring+rise*Mathf.Sqrt(Mathf.Max(0f,1f-(1f-.5f*u)*(1f-.5f*u)))/.8660254f;
        }
        /// <summary>A closed pointed barrel, with an open walkable volume under it, not a solid wedge.</summary>
        public static void Vault(MeshBuilder m,Vector3 origin,float yaw,float width,float depth,float spring,float rise,float thickness,int segments=12)
        {
            for(int i=0;i<segments;i++)
            {
                float t0=i/(float)segments,t1=(i+1)/(float)segments;
                float x0=Mathf.Lerp(-width*.5f,width*.5f,t0),x1=Mathf.Lerp(-width*.5f,width*.5f,t1);
                float y0=ArchY(t0,spring,rise),y1=ArchY(t1,spring,rise);
                Vector3[] p={new Vector3(x0,y0,0),new Vector3(x1,y1,0),new Vector3(x1,y1,depth),new Vector3(x0,y0,depth),
                    new Vector3(x0,y0+thickness,0),new Vector3(x1,y1+thickness,0),new Vector3(x1,y1+thickness,depth),new Vector3(x0,y0+thickness,depth)};
                for(int n=0;n<8;n++)p[n]=At(origin,yaw,p[n]);Hex(m,p);
            }
        }
        public static void Arch(MeshBuilder m,Vector3 origin,float yaw,float width,float spring,float rise,float rib,float depth,int segments=12)
        {
            for(int side=-1;side<=1;side+=2)
            {
                Box(m,origin,yaw,new Vector3(side*(width*.5f+rib*.5f),spring*.5f,depth*.5f),new Vector3(rib,spring,depth));
                Box(m,origin,yaw,new Vector3(side*(width*.5f+rib*.5f),.25f,depth*.5f),new Vector3(rib+ .65f,.5f,depth+.5f));
                Box(m,origin,yaw,new Vector3(side*(width*.5f+rib*.5f),spring-.15f,depth*.5f),new Vector3(rib+.35f,.3f,depth+.35f));
            }
            Vault(m,origin,yaw,width,depth,spring,rise,rib,segments);
        }
        public static void Crystal(MeshBuilder mesh,Vector3 root,Vector3 size,float lean,int seed)
        {
            // Unequal six-sided mineral column, leaning from a broad root. Vertical facets carry real mass.
            int sides=6;Vector3[] bottom=new Vector3[sides],shoulder=new Vector3[sides];
            Vector3 tip=root+new Vector3(lean*size.y,size.y,size.z*.12f);
            for(int i=0;i<sides;i++)
            {
                float angle=i*Mathf.PI*2f/sides;float variation=1f+((seed+i*7)%5)*.035f;
                bottom[i]=root+new Vector3(Mathf.Cos(angle)*size.x*.5f*variation,0f,Mathf.Sin(angle)*size.z*.5f);
                shoulder[i]=bottom[i]+new Vector3(lean*size.y*.65f,size.y*(.65f+(i%2)*.04f),size.z*.06f);
            }
            Vector3 inside=root+Vector3.up*size.y*.4f;
            for(int i=0;i<sides;i++)
            {
                int j=(i+1)%sides;Quad(mesh,bottom[i],bottom[j],shoulder[j],shoulder[i],inside);
                Tri(mesh,shoulder[i],shoulder[j],tip,inside);Tri(mesh,root,bottom[j],bottom[i],inside);
            }
        }
        private static void Tri(MeshBuilder m,Vector3 a,Vector3 b,Vector3 c,Vector3 inside)
        {
            Vector3 normal=Vector3.Cross(b-a,c-a).normalized;
            if(Vector3.Dot(normal,(a+b+c)/3f-inside)<0f){Vector3 swap=b;b=c;c=swap;normal=-normal;}
            m.Triangle(0,a,b,c,normal,new Vector2(a.x,a.y)*.3f,new Vector2(b.x,b.y)*.3f,new Vector2(c.x,c.y)*.3f);
        }
        public static void Rock(MeshBuilder mesh,Vector3 root,Vector3 size,int seed)
        {
            // Irregular closed mineral escarpment, not an identical roof diamond or visual-only rock.
            const int sides=9;Vector3[] bottom=new Vector3[sides],upper=new Vector3[sides];
            for(int i=0;i<sides;i++)
            {
                float angle=i*Mathf.PI*2f/sides;float k=.76f+Math.Abs((seed+i*31)%13)*.025f;
                bottom[i]=root+new Vector3(Mathf.Cos(angle)*size.x*.5f,0f,Mathf.Sin(angle)*size.z*.5f);
                upper[i]=root+new Vector3(Mathf.Cos(angle)*size.x*.5f*k+size.x*.09f,size.y*(.66f+Math.Abs((seed+i*17)%9)*.035f),Mathf.Sin(angle)*size.z*.5f*k);
            }
            Vector3 middle=root+Vector3.up*size.y*.4f,top=root+new Vector3(size.x*.12f,size.y,0f);
            for(int i=0;i<sides;i++){int j=(i+1)%sides;Quad(mesh,bottom[i],bottom[j],upper[j],upper[i],middle);Tri(mesh,upper[i],upper[j],top,middle);Tri(mesh,root,bottom[j],bottom[i],middle);}
        }
        public static void Stairs(MeshBuilder stone,Vector3 root,float yaw,float width,float rise,float run,int steps)
        {
            for(int i=0;i<steps;i++)Box(stone,root,yaw,new Vector3(0,rise*(i+1)/steps*.5f,run*(i+.5f)/steps),new Vector3(width,rise*(i+1)/steps,run/steps));
        }
        public static void Family(MeshBuilder stone,MeshBuilder trim,MeshBuilder mineral,Building building,bool detailed)
        {
            RectF f=building.Footprint;float w=f.Width,d=f.Depth,h=building.Height;int kind=building.GothicFamily04OB;
            Vector3 origin=new Vector3(f.CenterX,CityLayout.CurbHeight,f.Z0);
            if(kind==1) // U-shaped cloister: inhabited wings, a genuine open court and an arcade.
            {
                for(int side=-1;side<=1;side+=2)
                {
                    float x=side*w*.35f;
                    Hall(stone,trim,mineral,origin+new Vector3(x,0,d*.12f),0,w*.26f,d*.84f,h*.68f,h*.32f,false);
                    for(int bay=0;bay<3;bay++)Arch(trim,origin+new Vector3(side*w*.17f,0,d*(.22f+bay*.26f)),90f,3.5f,2.3f,1.4f,.3f,.65f,8);
                }
                Hall(stone,trim,mineral,origin+new Vector3(0,0,d*.76f),0,w*.5f,d*.24f,h*.64f,h*.25f,detailed);
                Box(stone,origin,0f,new Vector3(0,.04f,d*.45f),new Vector3(w*.38f,.08f,d*.66f));
                Arch(trim,origin,0,w*.35f,3.2f,2f,.45f,1.2f,10);
            }
            else if(kind==2)Hall(stone,trim,mineral,origin,0,w,d,Mathf.Min(8f,h*.5f),h*.5f,detailed);
            else if(kind==3) // Ribbed forge nave with an offset mineral furnace.
            {
                Hall(stone,trim,mineral,origin+Vector3.left*w*.12f,0,w*.7f,d,5.5f,5.5f,detailed);
                Rock(stone,origin+new Vector3(w*.30f,0,d*.64f),new Vector3(w*.3f,h,d*.38f),building.Seed);
                Crystal(mineral,origin+new Vector3(w*.33f,h*.6f,d*.64f),new Vector3(w*.2f,h*.43f,d*.19f),-.13f,building.Seed);
                for(int i=0;i<3;i++)Box(trim,origin,0,new Vector3(-w*.27f+i*w*.14f,1f,d*.8f),new Vector3(1.3f,2f,2.3f));
            }
            else if(kind==4) // Flood compound: open ground, supported dwelling and a real stair.
            {
                for(int side=-1;side<=1;side+=2)for(int end=0;end<3;end++)
                {Vector3 at=origin+new Vector3(side*w*.36f,0,d*(.15f+end*.35f));Rock(stone,at,new Vector3(2.2f,4.5f,2.5f),building.Seed+end);Crystal(mineral,at,new Vector3(1.2f,5f,1.2f),side*.05f,end);}
                Hall(stone,trim,mineral,origin+new Vector3(0,4.2f,d*.25f),0,w*.85f,d*.72f,3.6f,3.5f,detailed);
                Stairs(stone,origin+new Vector3(0,0,0),0,3.2f,4.2f,d*.25f,22);
                for(int side=-1;side<=1;side+=2)Railing(trim,origin+new Vector3(side*1.68f,0,0),origin+new Vector3(side*1.68f,4.2f,d*.25f));
            }
            else if(kind==5) // A dwelling carved against an asymmetric mineral back, with usable ledges.
            {
                Rock(stone,origin+new Vector3(w*.13f,0,d*.68f),new Vector3(w*.58f,h,d*.6f),building.Seed);
                Hall(stone,trim,mineral,origin+new Vector3(-w*.17f,0,d*.05f),0,w*.55f,d*.65f,4f,4f,detailed);
                Box(stone,origin,0,new Vector3(-w*.22f,.6f,d*.78f),new Vector3(w*.44f,1.2f,d*.23f));
                Stairs(stone,origin+new Vector3(-w*.36f,0,d*.55f),0,2.4f,1.2f,d*.12f,6);
                Crystal(mineral,origin+new Vector3(w*.22f,0,d*.4f),new Vector3(2f,h*.8f,2.6f),-.15f,building.Seed);
            }
            else // A broken gate with interrupted roofline and a low mineral scar.
            {
                Arch(stone,origin+new Vector3(-w*.18f,0,d*.45f),0,w*.5f,3.2f,3.6f,Mathf.Min(1.1f,w*.06f),d*.16f,9);
                Box(stone,origin,0,new Vector3(w*.33f,h*.34f,d*.75f),new Vector3(w*.22f,h*.68f,d*.36f));
                Rock(stone,origin+new Vector3(w*.32f,0,d*.78f),new Vector3(w*.28f,h,d*.34f),building.Seed);
                Crystal(mineral,origin+new Vector3(-w*.35f,0,d*.68f),new Vector3(w*.13f,h*.7f,d*.16f),.16f,building.Seed);
            }
        }
        public static void Hall(MeshBuilder stone,MeshBuilder trim,MeshBuilder mineral,Vector3 origin,float yaw,float width,float depth,float spring,float rise,bool detailed)
        {
            for(int side=-1;side<=1;side+=2)
            {
                PiercedWall(stone,trim,At(origin,yaw,new Vector3(side*(width*.5f-.4f)-.4f,0f,depth*.5f)),yaw+90f,depth,spring,.8f,detailed);
                for(int rib=0;rib<4;rib++)
                {
                    float z=depth*(rib+.5f)/4f;
                    Box(stone,origin,yaw,new Vector3(side*(width*.5f-1f),spring*.4f,z),new Vector3(1.2f,spring*.8f,1.6f));
                    if(detailed)Crystal(mineral,At(origin,yaw,new Vector3(side*(width*.5f-.7f),0,z)),new Vector3(1.4f,spring*.7f,1.5f),side*.05f,rib);
                }
            }
            // Recessed entry and back wall. The central five-metre doorway is genuinely open.
            float door=Mathf.Min(5f,width*.4f);
            for(int side=-1;side<=1;side+=2)Box(stone,origin,yaw,new Vector3(side*(width+door)*.25f,spring*.5f,.6f),new Vector3((width-door)*.5f,spring,1.2f));
            PiercedWall(stone,trim,At(origin,yaw,new Vector3(0,0,depth-.8f)),yaw,width,spring,.8f,detailed);
            Vault(stone,origin,yaw,width,depth,spring,rise,.45f,detailed?16:8);
            for(int rib=0;rib<5;rib++)Arch(trim,At(origin,yaw,new Vector3(0,0,depth*rib/4f)),yaw,width-1.1f,spring,rise-.2f,.25f,.55f,detailed?16:8);
            Box(stone,origin,yaw,new Vector3(0,-.05f,depth*.5f),new Vector3(width,.1f,depth));
        }
        /// <summary>New exterior and ribbed roof around the exact existing service room/actors and stair contracts.</summary>
        public static void Venue(MeshBuilder stone,MeshBuilder trim,MeshBuilder mineral,float width,float depth,float top,string id,bool detailed,bool roofPad=false)
        {
            bool sanctuary=id=="archive";float roomDepth=sanctuary?depth:18f;
            if(roofPad)
            {
                // One real supported landing on the service wing, under the original ordered rooftop receipts.
                Box(stone,Vector3.zero,0,new Vector3(-11.75f,top-.2f,depth*.72f),new Vector3(5f,.4f,6f));
                for(int side=-1;side<=1;side+=2)for(int end=-1;end<=1;end+=2)
                    Box(stone,Vector3.zero,0,new Vector3(-11.75f+side*1.9f,top*.5f,depth*.72f+end*2.4f),new Vector3(.7f,top,.7f));
            }
            float spring=sanctuary?10f:9.1f,rise=Mathf.Max(2.4f,top-spring-.55f);
            // The center is never filled by a legacy upper cuboid. Two small work/residential wings frame it.
            float wing=(width-17f)*.5f;
            for(int side=-1;side<=1;side+=2)
            {
                Box(stone,Vector3.zero,0,new Vector3(side*(8.5f+wing*.5f),3.5f,depth*.53f),new Vector3(wing,7f,depth*.82f));
                Vault(stone,new Vector3(side*(8.5f+wing*.5f),0,depth*.12f),0,wing,depth*.82f,7f,2.8f,.35f,detailed?10:6);
                for(int bay=0;bay<4;bay++)
                {
                    float z=2.5f+bay*(roomDepth-4f)/3f;
                    Box(stone,Vector3.zero,0,new Vector3(side*7.45f,1.15f,z),new Vector3(.65f,2.3f,.85f));
                    Crystal(mineral,new Vector3(side*7.45f,2f,z),new Vector3(.8f,spring-1.9f,1f),side*.025f,bay);
                }
            }
            Vault(stone,Vector3.zero,0,16f,roomDepth,spring,rise,.45f,detailed?20:10);
            int ribs=sanctuary?8:4;
            for(int i=0;i<ribs;i++)Arch(trim,new Vector3(0,0,.8f+(roomDepth-1.6f)*i/(ribs-1)),0,15.1f,spring,rise-.15f,.3f,.65f,detailed?20:10);
            Arch(trim,new Vector3(0,0,-1.2f),0,id=="garage"?7f:5.2f,3.4f,2.8f,.65f,2.2f,14);
            if(sanctuary)
            {
                SanctuaryGallery(stone,trim);
                Box(stone,Vector3.zero,0,new Vector3(0,-.1f,depth*.5f),new Vector3(16f,.2f,depth));
                Box(stone,Vector3.zero,0,new Vector3(0,5f,depth-.3f),new Vector3(16f,10f,.6f));
                for(int side=-1;side<=1;side+=2)Box(stone,Vector3.zero,0,new Vector3(side*7.85f,4.5f,depth*.5f),new Vector3(.3f,9f,depth));
                Box(trim,Vector3.zero,0,new Vector3(-5f,.7f,28f),new Vector3(1.1f,1.4f,.9f));
                Box(trim,Vector3.zero,0,new Vector3(0f,1.85f,43f),new Vector3(1.3f,1.3f,1f));
                Stairs(stone,new Vector3(0,0,36f),0,7f,1.2f,4.2f,6);
                Box(stone,Vector3.zero,0,new Vector3(0,.6f,43.5f),new Vector3(7f,1.2f,7f));
                Crystal(mineral,new Vector3(0,1.2f,46f),new Vector3(2.8f,12f,2f),.06f,92);
            }
        }
    }
}
