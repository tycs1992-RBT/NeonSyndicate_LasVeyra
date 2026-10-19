using UnityEngine;
namespace NeonSyndicate
{
    public static class LivingEconomy04M
    {
        // Fence has two 15% ranks; Forgewright Family adds 20%. Reserve the full legitimate 50% resale bonus before discounts.
        public const float MaximumResaleBonus=.5f;
        public static int RefugePrice(string material,int count,int listed,FactionRank rank)
        {
            MaterialDef definition=MaterialDb.Get(material);if(definition==null || count<1)return 0;
            int ceiling=ForgeMath.SalvagePrice(definition,MaximumResaleBonus)*count;
            float factor=rank==FactionRank.Family?.88f:rank==FactionRank.Trusted?.90f:rank==FactionRank.Friendly?.95f:
                rank==FactionRank.Hostile?1.10f:rank==FactionRank.Hunted?1.20f:1f;
            float basePrice=Mathf.Max(listed,definition.Value*count*1.85f);
            return Mathf.Max(ceiling+10,Mathf.CeilToInt(basePrice*factor/10f)*10);
        }
    }
}
