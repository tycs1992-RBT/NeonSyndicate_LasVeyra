using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>Editable articulated freight team. All poses and exported images use the same rigid mesh parts.</summary>
    public sealed class ForgeFreightModel
    {
        public const int Body=0, Head=1, Wagon=2, LeftWheel=3, RightWheel=4, FirstLeg=5, Trace=17, Count=19;
        public enum Finish { Hide, Mane, Muzzle, Horn, Hoof, Leather, Bronze, Timber, Iron, Mineral, Dampener }
        public sealed class Piece
        {
            public int Joint; public Finish Surface; public MeshBuilder Mesh=new MeshBuilder(1);
        }
        public readonly List<Piece> Pieces=new List<Piece>();
        private MeshBuilder Part(int joint,Finish role)
        {
            foreach(Piece part in Pieces)if(part.Joint==joint && part.Surface==role)return part.Mesh;
            Piece next=new Piece{Joint=joint,Surface=role};Pieces.Add(next);return next.Mesh;
        }

        public static Color Tint(Finish role)
        {
            switch(role)
            {
                case Finish.Hide:return new Color(.47f,.295f,.18f);
                case Finish.Mane:return new Color(.235f,.135f,.09f);
                case Finish.Muzzle:return new Color(.64f,.45f,.30f);
                case Finish.Horn:return new Color(.81f,.70f,.49f);
                case Finish.Leather:return new Color(.31f,.135f,.067f);
                case Finish.Bronze:return new Color(.64f,.43f,.22f);
                case Finish.Timber:return new Color(.43f,.29f,.17f);
                case Finish.Iron:return new Color(.21f,.235f,.22f);
                case Finish.Mineral:return new Color(.37f,.43f,.46f);
                case Finish.Dampener:return new Color(.22f,.60f,.61f);
                default:return new Color(.115f,.095f,.08f);
            }
        }

        private static void Oval(MeshBuilder mesh,Vector3 p,Vector3 size)
        { mesh.Sphere(0,p,size,9,16,Quaternion.identity); }
        private static void Rod(MeshBuilder mesh,Vector3 a,Vector3 b,float r,float tip,int sides=12)
        { Vector3 axis=b-a;mesh.Cylinder(0,a,r,tip,axis.magnitude,sides,QMath.FromTo(Vector3.up,axis),true); }
        private static void Plank(MeshBuilder mesh,Vector3 p,Vector3 size)
        { mesh.Box(0,p,size,UVMode.World); }

        // Slice rows give the rump, belly tuck, withers and brisket a single continuous skin.
        // Row entries are longitudinal position, centre height, horizontal radius and vertical radius.
        private static void Coat(MeshBuilder mesh,Vector4[] rows)
        {
            const int sides=24;
            for(int i=0;i<rows.Length-1;i++)for(int j=0;j<sides;j++)
            {
                float a=j*Mathf.PI*2/sides,b=(j+1)*Mathf.PI*2/sides;
                Vector3 p=Ring(rows[i],a),q=Ring(rows[i],b),r=Ring(rows[i+1],b),s=Ring(rows[i+1],a);
                Vector3 na=SkinNormal(rows,i,a),nb=SkinNormal(rows,i,b);
                mesh.QuadSmooth(0,p,q,r,s,na,nb,SkinNormal(rows,i+1,b),SkinNormal(rows,i+1,a),
                    new Vector2(j/(float)sides,rows[i].x),new Vector2((j+1)/(float)sides,rows[i].x),
                    new Vector2((j+1)/(float)sides,rows[i+1].x),new Vector2(j/(float)sides,rows[i+1].x));
                if(i==0 || i==rows.Length-2)
                {
                    int k=i==0?0:i+1;Vector3 c=new Vector3(0,rows[k].y,rows[k].x);
                    mesh.Triangle(0,c,i==0?q:s,i==0?p:r,i==0?Vector3.back:Vector3.forward,Vector2.zero,Vector2.right,Vector2.up);
                }
            }
        }
        private static Vector3 Ring(Vector4 row,float angle)
        { return new Vector3(row.z*Mathf.Cos(angle),row.y+row.w*Mathf.Sin(angle),row.x); }
        private static Vector3 SkinNormal(Vector4[] rows,int i,float a)
        {
            Vector3 tangent=new Vector3(-rows[i].z*Mathf.Sin(a),rows[i].w*Mathf.Cos(a),0);
            return Vector3.Cross(tangent,Ring(rows[Mathf.Min(rows.Length-1,i+1)],a)-Ring(rows[Mathf.Max(0,i-1)],a)).normalized;
        }

        public ForgeFreightModel()
        {
            MeshBuilder hide=Part(Body,Finish.Hide),mane=Part(Body,Finish.Mane);
            Coat(hide,new[]{new Vector4(-1.56f,1.57f,.12f,.19f),new Vector4(-1.42f,1.58f,.48f,.52f),
                new Vector4(-1.09f,1.60f,.64f,.65f),new Vector4(-.61f,1.54f,.62f,.61f),new Vector4(-.20f,1.57f,.66f,.60f),
                new Vector4(.35f,1.69f,.78f,.78f),new Vector4(.74f,1.79f,.83f,.87f),new Vector4(1.02f,1.82f,.72f,.79f),
                new Vector4(1.35f,1.87f,.51f,.57f),new Vector4(1.54f,1.94f,.36f,.39f)});
            Coat(mane,new[]{new Vector4(.07f,2.20f,.09f,.12f),new Vector4(.40f,2.32f,.43f,.26f),
                new Vector4(.74f,2.46f,.52f,.29f),new Vector4(1.05f,2.41f,.44f,.29f),new Vector4(1.40f,2.26f,.25f,.18f)});
            for(int side=-1;side<=1;side+=2)
            {
                for(int i=0;i<5;i++)
                {
                    float z=.22f+i*.21f,x=side*(.64f+.095f*Mathf.Sin(i*.72f));
                    Rod(mane,new Vector3(x,2.19f,z),new Vector3(x+side*.055f,1.76f+.06f*(i%3),z-.16f),.16f,.018f);
                }
                // Thick breast collar rides around the withers; traces attach at separate load rings.
                Vector3 last=new Vector3(0,2.72f,1.03f);
                for(int i=1;i<=12;i++)
                {
                    float a=i*Mathf.PI/12;
                    Vector3 p=new Vector3(side*.794f*Mathf.Sin(a),1.82f+.9f*Mathf.Cos(a),1.03f+.15f*Mathf.Sin(a));
                    Rod(Part(Body,Finish.Leather),last,p,.11f,.11f);last=p;
                }
                Oval(Part(Body,Finish.Bronze),new Vector3(side*.80f,1.60f,1.13f),new Vector3(.12f,.14f,.15f));
            }
            Rod(hide,new Vector3(0,1.88f,-1.35f),new Vector3(.09f,.85f,-1.63f),.12f,.07f);
            Oval(mane,new Vector3(.1f,.77f,-1.64f),new Vector3(.13f,.21f,.13f));
            BuildHead();BuildWagon();
            for(int i=0;i<4;i++)BuildLeg(i);
            for(int i=0;i<2;i++)Rod(Part(Trace+i,Finish.Leather),Vector3.zero,Vector3.up,.042f,.045f,8);
        }

        private void BuildHead()
        {
            MeshBuilder hide=Part(Head,Finish.Hide);
            Coat(hide,new[]{new Vector4(-.2f,.13f,.23f,.30f),new Vector4(.12f,.19f,.44f,.45f),
                new Vector4(.38f,.17f,.48f,.43f),new Vector4(.59f,.04f,.34f,.32f),new Vector4(.83f,-.11f,.27f,.23f)});
            Oval(Part(Head,Finish.Muzzle),new Vector3(0,-.12f,.83f),new Vector3(.32f,.21f,.24f));
            for(int side=-1;side<=1;side+=2)
            {
                Oval(Part(Head,Finish.Hoof),new Vector3(side*.15f,-.10f,1.046f),new Vector3(.065f,.045f,.035f));
                Oval(Part(Head,Finish.Hoof),new Vector3(side*.408f,.235f,.485f),new Vector3(.055f,.050f,.067f));
                Oval(hide,new Vector3(side*.49f,.26f,.07f),new Vector3(.22f,.12f,.15f));
                Vector3 a=new Vector3(side*.31f,.46f,.21f), b=new Vector3(side*.65f,.51f,.14f),
                    c=new Vector3(side*.86f,.65f,-.01f),d=new Vector3(side*.91f,.85f,.065f);
                MeshBuilder horn=Part(Head,Finish.Horn);
                Rod(horn,a,b,.16f,.115f);Rod(horn,b,c,.115f,.07f);Rod(horn,c,d,.07f,.012f);
            }
            Oval(Part(Head,Finish.Mane),new Vector3(0,-.15f,.19f),new Vector3(.33f,.39f,.27f));
        }

        private void BuildLeg(int i)
        {
            int id=FirstLeg+3*i;
            Oval(Part(id,Finish.Hide),new Vector3(0,.43f,0),new Vector3(i<2?.235f:.255f,.62f,.28f));
            Rod(Part(id+1,Finish.Mane),Vector3.zero,Vector3.up,.15f,.097f);
            for(int toe=-1;toe<=1;toe+=2)
                Oval(Part(id+2,Finish.Hoof),new Vector3(toe*.075f,0,.035f),new Vector3(.083f,.13f,.195f));
        }

        private void BuildWagon()
        {
            MeshBuilder wood=Part(Wagon,Finish.Timber),iron=Part(Wagon,Finish.Iron);
            for(int i=0;i<7;i++)Plank(wood,new Vector3((i-3)*.236f,.79f,0),new Vector3(.225f,.13f,2.67f));
            foreach(float x in new[]{-.67f,.67f})
            {
                Plank(iron,new Vector3(x,.65f,0),new Vector3(.13f,.16f,2.78f));
                for(int i=0;i<3;i++)Plank(iron,new Vector3(x*1.21f,1.08f,i-1.02f),new Vector3(.09f,.58f,.09f));
                Plank(wood,new Vector3(x*1.22f,1.24f,0),new Vector3(.10f,.19f,2.64f));
                Rod(wood,new Vector3(x,.75f,.9f),new Vector3(0,.93f,ForgeFreightRun.Tongue),.077f,.084f);
            }
            Rod(iron,new Vector3(-1.22f,.64f,0),new Vector3(1.22f,.64f,0),.105f,.105f);
            Rod(iron,new Vector3(-.71f,.93f,ForgeFreightRun.Tongue),new Vector3(.71f,.93f,ForgeFreightRun.Tongue),.065f,.065f);
            MeshBuilder ore=Part(Wagon,Finish.Mineral);
            for(int i=0;i<6;i++)
            {
                float x=(i%2-.5f)*.64f,z=(i/2-1)*.74f;
                ore.Sphere(0,new Vector3(x,1.08f,z),new Vector3(.39f,.33f+.09f*(i%3),.45f),4,7,QMath.AY(i*29));
            }
            foreach(float z in new[]{-.73f,.68f})
            {
                Vector3 a=new Vector3(-.83f,.86f,z),b=new Vector3(-.48f,1.47f,z);
                Vector3 c=new Vector3(.41f,1.48f,z),d=new Vector3(.83f,.86f,z);
                MeshBuilder lash=Part(Wagon,Finish.Leather);Rod(lash,a,b,.037f,.037f);Rod(lash,b,c,.037f,.037f);Rod(lash,c,d,.037f,.037f);
            }
            // One crystal in a protected bronze socket at the drawbar pivot.
            Oval(Part(Wagon,Finish.Bronze),new Vector3(0,.99f,2.39f),new Vector3(.18f,.12f,.23f));
            Part(Wagon,Finish.Dampener).Sphere(0,new Vector3(0,1.14f,2.39f),new Vector3(.115f,.20f,.12f),4,6,Quaternion.identity);
            for(int wheel=LeftWheel;wheel<=RightWheel;wheel++)
            {
                MeshBuilder rim=Part(wheel,Finish.Iron),spokes=Part(wheel,Finish.Timber);
                const int n=32;
                for(int i=0;i<n;i++)
                {
                    float a=i*2*Mathf.PI/n,b=(i+1)*2*Mathf.PI/n;
                    Vector3 u=new Vector3(0,Mathf.Cos(a),Mathf.Sin(a)),v=new Vector3(0,Mathf.Cos(b),Mathf.Sin(b));
                    Vector3 l=Vector3.left*.075f,r=-l;
                    LinkFace(rim,l+u*.64f,r+u*.64f,r+v*.64f,l+v*.64f,u+v);
                    LinkFace(rim,l+u*.56f,r+u*.56f,r+v*.56f,l+v*.56f,-u-v);
                    foreach(Vector3 side in new[]{l,r})LinkFace(rim,side+u*.64f,side+v*.64f,side+v*.56f,side+u*.56f,side);
                    if(i%4==0)Rod(spokes,u*.11f,u*.58f,.045f,.037f,8);
                }
                Rod(Part(wheel,Finish.Bronze),Vector3.left*.14f,Vector3.right*.14f,.145f,.145f);
            }
        }
        private static void LinkFace(MeshBuilder m,Vector3 a,Vector3 b,Vector3 c,Vector3 d,Vector3 n)
        {
            if(Vector3.Dot(Vector3.Cross(b-a,c-a),n)<0){Vector3 v=b;b=d;d=v;}
            m.Quad(0,a,b,c,d,Vector3.Cross(b-a,c-a).normalized,Vector2.zero,Vector2.right,Vector2.one,Vector2.up);
        }

        public struct JointPose
        {
            public Vector3 Position,Scale;public Quaternion Rotation;
            public JointPose(Vector3 p,Quaternion r){Position=p;Rotation=r;Scale=Vector3.one;}
        }
        private static JointPose Span(Vector3 a,Vector3 b)
        {
            Vector3 delta=b-a;JointPose pose=new JointPose(a,QMath.FromTo(Vector3.up,delta));
            pose.Scale.y=delta.magnitude;return pose;
        }

        public static void Pose(ForgeFreightRun run,ForgeFreightFrame f,float effort,float clock,JointPose[] joints)
        {
            Quaternion animal=QMath.LookRotation(f.Forward,Vector3.up),wagon=QMath.LookRotation(f.WagonForward,Vector3.up);
            float phase=f.Distance*2*Mathf.PI/ForgeFreightRun.Stride,breath=.018f*Mathf.Sin(clock*1.7f);
            Vector3 sway=new Vector3(.038f*Mathf.Sin(phase)*effort,breath+.040f*Mathf.Cos(phase*2)*effort,0);
            Vector3 body=f.Animal+animal*sway;
            Quaternion coat=animal*QMath.AZ(1.5f*Mathf.Sin(phase)*effort);
            joints[Body]=new JointPose(body,coat);
            joints[Head]=new JointPose(body+coat*new Vector3(0,1.98f,1.27f),coat*QMath.AX(5+3.3f*Mathf.Cos(phase)*effort+breath*20));
            joints[Wagon]=new JointPose(f.Axle,wagon);
            for(int side=0;side<2;side++)
            {
                int sign=side==0?-1:1;float roll=side==0?f.LeftRoll:f.RightRoll;
                joints[LeftWheel+side]=new JointPose(f.Axle+wagon*new Vector3(sign*ForgeFreightRun.Track,ForgeFreightRun.WheelRadius,0),wagon*QMath.AX(roll*Mathf.Rad2Deg));
                joints[Trace+side]=Span(body+coat*new Vector3(sign*.80f,1.60f,1.13f),
                    f.Axle+wagon*new Vector3(sign*.69f,.93f,ForgeFreightRun.Tongue));
            }
            for(int i=0;i<4;i++)
            {
                Vector3 hip=body+coat*new Vector3((i%2==0?-1:1)*(i<2?.54f:.48f),i<2?1.66f:1.57f,i<2?.89f:-.87f);
                Vector3 foot=run.WorldFoot(i,f.Distance,effort);
                foot.y+=f.Animal.y;
                Vector3 localFoot=QMath.Inverse(animal)*(foot-f.Animal);
                Vector3 knee=f.Animal+animal*ForgeFreightRun.Knee(QMath.Inverse(animal)*(hip-f.Animal),localFoot,i<2);
                int joint=FirstLeg+i*3;joints[joint]=Span(hip,knee);joints[joint+1]=Span(knee,foot);
                joints[joint+2]=new JointPose(foot,animal);
            }
        }
    }
}
