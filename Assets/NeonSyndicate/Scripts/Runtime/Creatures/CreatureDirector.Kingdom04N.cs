using System.Collections.Generic;
using UnityEngine;
namespace NeonSyndicate
{
    public static class CreatureAdmission04N
    {
        public static float Room(float cap,float ambient,float allocated){return Mathf.Max(0,cap-Mathf.Max(ambient,allocated));}
        public static Vector3 Probe(int n){return n==0?Vector3.zero:MathUtil.DirFromYaw((n-1)*360f/7f)*2.5f;}
    }
    public sealed partial class CreatureDirector
    {
        private readonly HashSet<string> _extraGroups04N=new HashSet<string>();private int _quietRejected04N;
        public int AllocatedGroups04N
        {
            get
            {
                int groups;float bodies;CountAmbient(out groups,out bodies);_extraGroups04N.Clear();
                foreach(var body in _live)
                {
                    if(body==null || body.IsDead || IsAmbient(body))continue;
                    Creature c=body as Creature;NpcController n=body as NpcController;
                    string tag=c!=null && c.Options!=null?c.Options.Tag:n!=null && n.CreatureOptions!=null?n.CreatureOptions.Tag:null;
                    _extraGroups04N.Add(string.IsNullOrEmpty(tag)?"body:"+body.GetInstanceID():tag);
                }
                return groups+_extraGroups04N.Count;
            }
        }
        /// <summary>F2 tracking only. Same ground/road/quiet/collision policy, deterministic probes, no spawning or teleport.</summary>
        private bool TrackCityRift04N()
        {
            if(Game.Gps==null)return false;
            int checkedNodes=0;Vector3 pp=Game.PlayerPosition;var sp=CreatureSpeciesDb.Get("hollow");
            foreach(var node in _cityRiftWalks04I)
            {
                float dist=MathUtil.FlatDistance(pp,node);if(dist<SpawnMin || dist>SpawnMax)continue;
                if(++checkedNodes>8)break;
                for(int i=0;i<8;i++)
                {
                    Vector3 at=node+CreatureAdmission04N.Probe(i);float y;
                    if(!GroundFor(SpawnSpot.CityRift,at,pp,out y))continue;at.y=y;
                    if(!GroundOk(SpawnSpot.CityRift,sp,at))continue;
                    if(Game.Gps!=null)Game.Gps.SetWaypoint(at);return true;
                }
            }
            return false;
        }
    }
}
