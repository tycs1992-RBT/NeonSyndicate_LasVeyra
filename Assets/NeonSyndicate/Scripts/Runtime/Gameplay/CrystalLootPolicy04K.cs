namespace NeonSyndicate
{
    /// <summary>The existing 04J random-caster cap, shared with executable tests. Explicit rewards retain their rarity.</summary>
    public static class CrystalLootPolicy04K
    {
        public static Rarity AmbientRarity(Rarity rolled,bool crystalCaster,int unique)
        {return crystalCaster && unique<0 && rolled==Rarity.Mythic ? Rarity.Legendary : rolled;}
    }
}
