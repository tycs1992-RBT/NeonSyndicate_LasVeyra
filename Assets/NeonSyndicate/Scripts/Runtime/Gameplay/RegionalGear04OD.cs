using UnityEngine;
namespace NeonSyndicate
{
    /// <summary>Paid, temporary modifiers on legitimately held blades, not new focus entitlements.</summary>
    public static class RegionalGear04OD
    {
        public static void ResetStatics() { }
        public static int Synergy(string archetype,CrystalBehavior focus,bool amethystEarned,bool azureEarned,int region)
        {
            if(region==0 && amethystEarned && archetype=="ash_warden" && (focus==CrystalBehavior.Mirrorwake || focus==CrystalBehavior.Sunderheart))return 1;
            if(region==1 && azureEarned && archetype=="storm_skimmer" && (focus==CrystalBehavior.PrismThread || focus==CrystalBehavior.GyreCrescent))return 2;
            return 0;
        }
        public static MeleeMove Move(MeleeMove source,MeleeClass style,CraftingSystem forge)
        {
            bool echo=forge!=null && forge.BuffLeft("brew_04od_echo_blade")>0;
            bool storm=forge!=null && forge.BuffLeft("brew_04od_storm_crescent")>0;
            return Build(source,style,echo?1:storm?2:0);
        }
        public static MeleeMove Build(MeleeMove s,MeleeClass style,int modifier)
        {
            if(s==null || s.Kind!=MoveKind.Heavy || (style!=MeleeClass.Blade && style!=MeleeClass.Katana) || modifier<1 || modifier>2)return s;
            bool echo=modifier==1;
            return new MeleeMove{Name=echo?"Warden's Echo":"Stormwood Crescent",Kind=s.Kind,Anim=echo?MeleeAnim.Thrust:MeleeAnim.SlashRight,
                Windup=echo?.52f:.40f,Active=echo?.14f:.22f,Recovery=echo?.62f:.34f,
                Damage=s.Damage*(echo?1.03f:.82f),Poise=s.Poise*(echo?1.6f:.85f),Stamina=s.Stamina+4f,Range=s.Range,
                Arc=echo?60f:150f,Lunge=echo?1.5f:.8f,Force=s.Force,Knockdown=false,HyperArmor=s.HyperArmor,
                Unblockable=false,ChargeMax=s.ChargeMax,CancelAt=s.CancelAt,Area=false};
        }
    }
}
