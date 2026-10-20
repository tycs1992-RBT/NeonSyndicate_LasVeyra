using UnityEngine;
namespace NeonSyndicate
{
    /// <summary>Paid regional commissions produce ordinary persistent GearItems, not timed buffs or new ownership records.</summary>
    public static class RegionalArmor04OE
    {
        public const int AmethystSeed=406501,AzureSeed=406502;
        public static void ResetStatics() { }
        public static int Region(GearItem item)
        {return item==null || item.Slot!=GearSlot.Shell?0:item.Seed==AmethystSeed && item.Name=="Veilglass Wardcoat"?1:item.Seed==AzureSeed && item.Name=="Stormroot Travelcoat"?2:0;}
        public static GearOrder Order(int region)
        {return new GearOrder{Regional=region,Slot=GearSlot.Shell,Rarity=Rarity.Rare,Set=-1,Element=Element.None};}
        public static bool Valid(GearOrder order)
        {return order.Regional>=1 && order.Regional<=2 && order.Slot==GearSlot.Shell && order.Rarity==Rarity.Rare && order.Set==-1 && order.Element==Element.None;}
        public static string Quest(int region){return region==1?"fg_04od_castellan_oath":"fg_04od_stormroot";}
        public static GearItem Build(int region,int power)
        {
            bool amethyst=region==1;
            GearItem item=GearSystem.GenerateSeeded(GearSlot.Shell,Rarity.Rare,power,amethyst?AmethystSeed:AzureSeed,
                amethyst?new[]{AffixType.Armor,AffixType.MeleeResist,AffixType.GuardEfficiency}:new[]{AffixType.MaxStamina,AffixType.DodgeFrames,AffixType.MoveSpeed},Element.None,-1);
            item.Name=amethyst?"Veilglass Wardcoat":"Stormroot Travelcoat";return item;
        }
        public static void Surcharge(ForgeCost cost,int region)
        {cost.Cash+=240;cost.Add(region==1?"glass_chitin":"seed_core",2).Add(region==1?"shard_chunk":"mite_dust",4);}
        public static void Shell(MeshBuilder mesh,int region,int upgrade)
        {
            for(int side=-1;side<=1;side+=2)
            {
                int count=region==1?4:3;
                for(int i=0;i<count;i++)
                {
                    Vector3 at=new Vector3(side*(region==1?.14f:.19f),-.035f-i*.055f,region==1?-.15f:.11f);
                    mesh.Box(0,at,new Vector3(region==1?.14f:.065f,.05f,.035f),QMath.AZ(side*(region==1?10:28)),UVMode.Unit);
                    if(i<=upgrade)GothicRealmGeometry04OB.Crystal(mesh,at+Vector3.back*.025f,new Vector3(.035f,.065f,.025f),side*.1f,809+i);
                }
            }
        }
    }
}
