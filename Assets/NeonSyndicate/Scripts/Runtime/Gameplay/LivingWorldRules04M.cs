using UnityEngine;
namespace NeonSyndicate
{
    public enum ResidentShift04M { Work, Break, Rest, Shelter }
    public static class LivingWorldRules04M
    {
        public static ResidentShift04M Shift(float hour,int seed,bool storm)
        {
            if(storm)return ResidentShift04M.Shelter;
            float h=Mathf.Repeat(hour-(seed&3)*.75f,24f);
            if(h<6f || h>=20f)return ResidentShift04M.Rest;
            if(h>=12f && h<14f)return ResidentShift04M.Break;
            return ResidentShift04M.Work;
        }
        public static bool Parley(bool dead,bool fighting,bool mission,bool wanted,float heat,float floor)
        {return !dead && !fighting && !mission && !wanted && heat<.6f && floor<.6f;}
        public static Appearance Dress(int gang,Appearance look)
        {
            // Eight deterministic silhouettes over the original saved faction palette; no stat/hostility changes.
            look.Jacket=(gang&1)==0;
            look.LongSleeves=gang!=2 && gang!=7;
            look.Headwear=gang==0?Headwear.Bandana:gang==1?Headwear.Helmet:gang==3?Headwear.Beanie:
                gang==4?Headwear.Cap:Headwear.None;
            look.Glasses=gang==2 || gang==6;
            look.SuitStyle=gang==6?2:1;
            if(gang==7)look.HairStyle=HairStyle.Braids;
            return look;
        }
        public static string TradeDescription(int gang)
        {
            switch(gang)
            {
                case 0:return "Court crews keep Ashline heat exchangers running. Aid buys supplies, never amnesty for the treaty.";
                case 1:return "Badlands crews mark quarry routes. Ordinary shards come from the rock and the creatures, not a royal gift.";
                case 2:return "Lotus couriers work the Strip. Your contribution keeps neighborhood deliveries moving.";
                case 3:return "Union crews maintain the Projects. Their mutual-aid fund buys tools, not a new crown.";
                case 4:return "Saints monitor Grave Frequency. Their scanners flag rifts; the Order still owns attunement.";
                case 5:return "Choir crews maintain canal pumps. Mara's clinic remains the place for treatment.";
                case 6:return "Jury clerks keep Glass Heights records. Community aid cannot change a verdict.";
                default:return "Oni spotters watch Skyrail. Travel powers and bonded mounts must still be earned.";
            }
        }
    }
}
