namespace NeonSyndicate
{
    /// <summary>Three earned confirmations per regional art in the original companion milestone list.</summary>
    public static class RegionalResonance04OE
    {
        public static void ResetStatics() { }
        public static int Count(CompanionRecord companion,int region)
        {int count=0;if(companion==null || companion.Milestones==null || region<1 || region>2)return 0;for(int i=1;i<=3;i++)if(companion.Milestones.Contains(Key(region,i)))count++;return count;}
        private static string Key(int region,int level){return "res04oe_"+(region==1?"oath":"storm")+"_"+level;}
        public static bool Credit(CompanionRecord companion,int region,bool confirmedApplied)
        {
            if(!confirmedApplied || companion==null || companion.Uid<=0 || companion.Rank<BondIds.RankBonded || region<1 || region>2)return false;
            if(companion.Milestones==null)companion.Milestones=new System.Collections.Generic.List<string>();
            for(int i=1;i<=3;i++)if(!companion.Milestones.Contains(Key(region,i))){companion.Milestones.Add(Key(region,i));return true;}return false;
        }
        public static bool OwnsWeapon(int region)
        {
            if(Game.Loot==null)return false;int unique=region==1?RegionalArsenal04OE.Oathblade:RegionalArsenal04OE.StormCrescent;
            foreach(LootGun item in Game.Loot.Slots)if(item!=null && item.Uid>0 && item.Unique==unique)return true;
            foreach(LootGun item in Game.Loot.Backpack)if(item!=null && item.Uid>0 && item.Unique==unique)return true;
            return false;
        }
        public static string Progress(CompanionRecord companion)
        {return "\nRegional confirmations: Oath "+Count(companion,1)+"/3; Storm "+Count(companion,2)+"/3. Match the earned regional weapon, quest, partner and focus. Three confirmations refine that art; no new focus or companion is granted.";}
    }
}
