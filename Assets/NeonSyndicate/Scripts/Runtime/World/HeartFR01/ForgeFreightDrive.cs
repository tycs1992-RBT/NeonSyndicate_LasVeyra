using System;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>One source/candidate pair. A failed activation restores the prior pair synchronously.</summary>
    public sealed class ForgeFreightLease
    {
        public bool Active { get; private set; }
        public bool Select(bool team,bool ready,bool occupied,Action<bool> source,Action<bool> candidate)
        {
            if(team==Active)return true;
            if(occupied || (team && !ready))return false;
            try
            {
                if(team){source(false);candidate(true);}
                else {candidate(false);source(true);}
                Active=team;return true;
            }
            catch
            {
                if(Active){source(false);candidate(true);}
                else {candidate(false);source(true);}
                throw;
            }
        }
    }

    public struct ForgeFreightEnvelope
    {
        public Vector3 Centre,Half,Forward;
        public ForgeFreightEnvelope(Vector3 centre,Vector3 half,Vector3 forward)
        { Centre=centre;Half=half;Forward=forward; }

        public bool Contains(Vector3 point,float radius,float halfHeight)
        {
            Vector3 delta=point-Centre,right=new Vector3(Forward.z,0,-Forward.x);
            return Mathf.Abs(Vector3.Dot(right,delta))<Half.x+radius && Mathf.Abs(delta.y)<Half.y+halfHeight &&
                Mathf.Abs(Vector3.Dot(Forward,delta))<Half.z+radius;
        }
        public static ForgeFreightEnvelope Animal(ForgeFreightFrame f)
        {return new ForgeFreightEnvelope(f.Animal+Vector3.up*1.50f+f.Forward*.28f,new Vector3(1.10f,1.36f,2.23f),f.Forward);}
        public static ForgeFreightEnvelope Wagon(ForgeFreightFrame f)
        {return new ForgeFreightEnvelope(f.Axle+Vector3.up*.91f+f.WagonForward*.60f,new Vector3(1.30f,.77f,2.21f),f.WagonForward);}
    }

    /// <summary>Distance, planted gait, reaction and wheel odometry are shared by the live team and pose exporter.</summary>
    public sealed class ForgeFreightDrive
    {
        public delegate bool Support(ref ForgeFreightFrame frame);
        private readonly ForgeFreightRun _run;
        private readonly Func<ForgeFreightFrame,bool> _free;
        private readonly Support _support;
        private float _hold,_clearDelay;
        private bool _departed;
        public ForgeFreightFrame Frame {get;private set;}
        public float Speed {get;private set;}
        public float Effort {get;private set;}
        public float Clock {get;private set;}
        public bool Waiting {get;private set;}

        public ForgeFreightDrive(ForgeFreightRun run,ForgeFreightFrame start,Func<ForgeFreightFrame,bool> free,Support support)
        {
            _run=run;Frame=start;_free=free;_support=support;_departed=start.Distance>.5f;_hold=_departed?0:2.5f;
        }
        public void Halt(){Speed=0;Effort=0;}
        public bool Step(float dt,bool observer)
        {
            if(dt<=0)return false;
            Clock+=dt;_departed|=observer;
            if(_departed)_hold=Mathf.Max(0,_hold-dt);
            float goal=_departed && _hold<=0?Mathf.Min(1.85f,Mathf.Sqrt(1.6f*Mathf.Max(0,_run.Length-Frame.Distance))):0;
            float speed=Mathf.MoveTowards(Speed,goal,.76f*dt);
            float before=Frame.Distance;
            if(speed>.001f)
            {
                bool clear=_free(Frame);
                // Retain the full restart envelope while waiting. Shrinking it with stopped speed would
                // allow repeated little steps toward the same standing pedestrian.
                float probeSpeed=Waiting?1.85f:speed;
                float look=.65f+probeSpeed*probeSpeed/1.6f;
                ForgeFreightFrame probe=Frame;
                for(float d=0;clear && d<look;d+=.36f)
                {
                    probe=_run.Advance(probe,Mathf.Min(.36f,look-d));clear=_free(probe);
                }
                ForgeFreightFrame next=_run.Advance(Frame,speed*dt);
                if(!clear || !_support(ref next))
                {speed=0;_clearDelay=.50f;Waiting=true;}
                else if(_clearDelay>0)
                {speed=0;_clearDelay=Mathf.Max(0,_clearDelay-dt);}
                else {Frame=next;Waiting=false;}
            }
            Speed=speed;Effort=Mathf.MoveTowards(Effort,Mathf.Clamp01(speed*1.8f),dt*3);
            return Mathf.FloorToInt(Frame.Distance*4/ForgeFreightRun.Stride)>Mathf.FloorToInt(before*4/ForgeFreightRun.Stride);
        }
    }
}
