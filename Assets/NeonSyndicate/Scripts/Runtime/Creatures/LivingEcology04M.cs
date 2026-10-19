using UnityEngine;
namespace NeonSyndicate
{
    public enum HabitatActivity04M { Roam, Forage, Rest, Watch }
    public static class LivingEcology04M
    {
        public static HabitatActivity04M Activity(float hour,float seconds,int seed,bool storm,bool observer)
        {
            if(observer)return HabitatActivity04M.Watch;
            if(storm || Mathf.Repeat(hour,24f)<6f)return HabitatActivity04M.Rest;
            return Mathf.Repeat(seconds+(seed&31),48f)<14f?HabitatActivity04M.Forage:HabitatActivity04M.Roam;
        }
        public static int MusterCount(bool low){return low?4:6;}
        public static string MusterSpecies(int slot)
        {return slot==0?"shard_golem":slot==1?"hollow_shade":slot==2?"neon_jackal":"glass_mite";}
        public static Vector3 MusterOffset(int slot)
        {
            // Bulwark holds the centre, the shade stands behind, jackal has a lateral approach, mites spread on the other flank.
            switch(slot){case 0:return new Vector3(0,0,1);case 1:return new Vector3(0,0,7);case 2:return new Vector3(-7,0,3);
                default:return new Vector3(5+(slot-3)*1.4f,0,3+(slot%2)*2);}
        }
        public static bool RallyInterrupted(bool leaderAlive,float lastDamage,float started)
        {return !leaderAlive || lastDamage>=started;}
    }
}
