using UnityEngine;

namespace NeonSyndicate
{
    public static class CreatureTactics04L
    {
        /// <summary>Weighted selection among the original eligible moves. Never bypasses cooldown, range, arc or flight state.</summary>
        public static int Pick(CreatureAttack[] set, float[] ready, float now, float distance, float angle,
            AttackUse state, float roll, int previous, float lateralSpeed)
        {
            if(set==null)return -1;
            float total=0f;
            for(int i=0;i<set.Length;i++) if(CreatureAttackDb.Fits(set[i],ready,i,now,distance,angle,state)) total+=Weight(set[i],i==previous,lateralSpeed);
            if(total<=0f)return -1;
            float pick=Mathf.Clamp01(roll)*total;int last=-1;
            for(int i=0;i<set.Length;i++)
            {
                if(!CreatureAttackDb.Fits(set[i],ready,i,now,distance,angle,state))continue;
                last=i;pick-=Weight(set[i],i==previous,lateralSpeed);
                if(pick<0f)return i;
            }
            return last;
        }
        private static float Weight(CreatureAttack a,bool repeated,float lateral)
        {
            float w=a.Weight*(repeated?.35f:1f);
            // A circling player is harder to catch with a straight rush. Sweeps remain range/angle eligible.
            float moving=Mathf.Clamp01(Mathf.Abs(lateral)/5f);
            if(a.Shape==AttackShape.Line || a.Shape==AttackShape.Leap)w*=1f-moving*.65f;
            if(a.Shape==AttackShape.Ring || a.Arc>=240f)w*=1f+moving*.6f;
            return w;
        }
    }
}
