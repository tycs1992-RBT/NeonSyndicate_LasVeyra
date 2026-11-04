using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>The freight animal uses an arc-length sampled curb departure and arrival on one existing traffic lane.</summary>
    public sealed class ForgeFreightRun
    {
        public readonly LanePath Lane;
        public readonly Vector3 Load, Receive;
        private readonly Vector3[] path;
        private readonly float[] lengths;
        public float Length { get { return lengths[lengths.Length-1]; } }
        public const float Tongue=2.6f, WheelRadius=.64f, Track=1.05f, Stride=1.46f, Duty=.64f;

        private ForgeFreightRun(LanePath lane,Vector3 load,Vector3 receive)
        {
            Lane=lane;Load=load;Receive=receive;
            int steps=Mathf.CeilToInt((receive.x-load.x)*2);
            path=new Vector3[steps+1];lengths=new float[steps+1];
            for(int i=0;i<=steps;i++)
            {
                float x=Mathf.Lerp(load.x,receive.x,i/(float)steps);
                float blend=Mathf.Min(Mathf.Clamp01((x-load.x)/14),Mathf.Clamp01((receive.x-x)/19));
                float offset=1.45f*(1-blend*blend*(3-2*blend));
                path[i]=lane.PointAt(x-lane.PointAt(0).x)+Vector3.back*offset;
                if(i>0)lengths[i]=lengths[i-1]+Vector3.Distance(path[i-1],path[i]);
            }
        }

        public static ForgeFreightRun Find(CityLayout world)
        {
            if(world==null || !world.GothicCivicH5)return null;
            foreach(LanePath lane in world.Lanes)
            {
                if(lane.IsConnector || lane.Kind!=RoadKind.Street || lane.EdgeId<0 || lane.EdgeId>=world.Edges.Count)continue;
                RoadEdge road=world.Edges[lane.EdgeId];
                Vector3 a=lane.PointAt(0),b=lane.PointAt(lane.Length);
                if(road.Name!="Mineral service circuit" || road.HasBridge || road.Width<12 ||
                    b.x-a.x<275 || Mathf.Abs(a.z+151.75f)>.1f || Mathf.Abs(a.z-b.z)>.01f ||
                    Mathf.Abs(a.y-b.y)>.01f)continue;
                PropSpot load=world.Props.Find(p=>p.Kind==PropKind.Hydrant && Mathf.Abs(p.Pos.x+10.3f)<.6f && Mathf.Abs(p.Pos.z+160.58f)<.6f);
                PropSpot end=world.Props.Find(p=>p.Kind==PropKind.Bench && Mathf.Abs(p.Pos.x-120.3f)<.6f && Mathf.Abs(p.Pos.z+163.13f)<.6f);
                if(load.Kind!=PropKind.Hydrant || end.Kind!=PropKind.Bench || a.x+10>=load.Pos.x || b.x-10<=end.Pos.x)return null;
                return new ForgeFreightRun(lane,load.Pos,end.Pos);
            }
            return null;
        }

        public Vector3 Point(float distance,out Vector3 forward)
        {
            int lo=0,hi=lengths.Length-1;
            while(hi-lo>1){int mid=(lo+hi)/2;if(lengths[mid]<=distance)lo=mid;else hi=mid;}
            forward=(path[hi]-path[lo]).normalized;
            return path[lo]+forward*Mathf.Clamp(distance-lengths[lo],0,lengths[hi]-lengths[lo]);
        }

        public ForgeFreightFrame Start()
        {
            ForgeFreightFrame f=new ForgeFreightFrame();
            f.Animal=Point(0,out f.Forward);f.WagonForward=f.Forward;
            f.Axle=f.Hitch-f.Forward*Tongue;return f;
        }

        public ForgeFreightFrame Advance(ForgeFreightFrame old,float metres)
        {
            ForgeFreightFrame f=old;f.Distance=Mathf.Min(Length,old.Distance+Mathf.Max(0,metres));
            f.Animal=Point(f.Distance,out f.Forward);
            f.WagonForward=MathUtil.Flat(f.Hitch-old.Axle).normalized;
            f.Axle=f.Hitch-f.WagonForward*Tongue;
            float roll=Vector3.Dot(f.Axle-old.Axle,(f.WagonForward+old.WagonForward).normalized);
            float turn=Mathf.Atan2(Vector3.Cross(old.WagonForward,f.WagonForward).y,Vector3.Dot(old.WagonForward,f.WagonForward));
            f.LeftRoll+= (roll+Track*turn)/WheelRadius; f.RightRoll+=(roll-Track*turn)/WheelRadius;
            return f;
        }

        // Four-beat walk. During stance, hoof offset retreats one metre per metre travelled.
        public static Vector3 Foot(int leg,float distance,float lift)
        {
            bool front=leg<2;float side=(leg%2==0?-1:1);
            float phase=Mathf.Repeat(distance/Stride+(leg==0?.25f:leg==1?.75f:leg==2?0:.5f),1);
            float dz=Stride*(Duty*.5f-phase),y=0;
            if(phase>Duty)
            {
                float t=(phase-Duty)/(1-Duty);dz+=Stride*t*t*(3-2*t);
                float wave=Mathf.Sin(Mathf.PI*t);y=.235f*wave*wave*lift;
            }
            return new Vector3(side*(front?.54f:.48f),.13f+y,(front?.89f:-.87f)+dz);
        }

        private Vector3 PlantedAt(int leg,float distance)
        {
            Vector3 p=Point(distance,out Vector3 forward);
            p+=forward*(distance-Mathf.Clamp(distance,0,Length));
            Vector3 right=new Vector3(forward.z,0,-forward.x);
            Vector3 foot=Foot(leg,0,0);
            return p+right*foot.x+forward*((leg<2?.89f:-.87f)+Stride*Duty*.5f)+Vector3.up*.13f;
        }

        public Vector3 WorldFoot(int leg,float distance,float lift)
        {
            float phase=Mathf.Repeat(distance/Stride+(leg==0?.25f:leg==1?.75f:leg==2?0:.5f),1);
            float touchdown=distance-phase*Stride;
            Vector3 planted=PlantedAt(leg,touchdown);
            if(phase<=Duty)return planted;
            float t=(phase-Duty)/(1-Duty);
            Vector3 next=PlantedAt(leg,touchdown+Stride);
            float wave=Mathf.Sin(Mathf.PI*t);
            return Vector3.Lerp(planted,next,t*t*(3-2*t))+Vector3.up*(.235f*wave*wave*lift);
        }

        public ForgeFreightFrame FrameAtX(float x)
        {
            ForgeFreightFrame frame=Start();
            float lo=0,hi=Length;
            for(int i=0;i<18;i++)
            {
                float mid=(lo+hi)*.5f;
                if(Point(mid,out Vector3 direction).x<x)lo=mid;else hi=mid;
            }
            for(float d=0;d<lo;d+=.25f)frame=Advance(frame,Mathf.Min(.25f,lo-d));
            return frame;
        }

        public static Vector3 Knee(Vector3 hip,Vector3 foot,bool front)
        {
            Vector3 delta=foot-hip;float d=delta.magnitude;
            Vector3 axis=delta/d;
            const float upper=.89f,lower=.89f;
            float along=(upper*upper-lower*lower+d*d)/(2*d);
            Vector3 bend=new Vector3(0,-axis.z,axis.y)*(front?1:-1);
            return hip+axis*along+bend*Mathf.Sqrt(Mathf.Max(0,upper*upper-along*along));
        }
    }

    public struct ForgeFreightFrame
    {
        public Vector3 Animal,Forward,Axle,WagonForward;
        public float Distance,LeftRoll,RightRoll;
        public Vector3 Hitch { get { return Animal-Forward*1.85f; } }
    }
}
