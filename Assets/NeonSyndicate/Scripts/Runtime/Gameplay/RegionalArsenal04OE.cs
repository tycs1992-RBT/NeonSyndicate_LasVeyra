using UnityEngine;
namespace NeonSyndicate
{
    /// <summary>Append-only unique identities in the original UID armory. No new inventory, save block or focus.</summary>
    public static class RegionalArsenal04OE
    {
        public const int Oathblade=23, StormCrescent=24;
        public static readonly string[] RewardBosses={"golem_foreman","mirage_djinn","wraith_queen","veilglass_castellan","stormroot_sovereign"};
        public static void ResetStatics() { }
        public static bool IsUnique(int unique){return unique==Oathblade || unique==StormCrescent;}
        public static bool IsWeapon(LootGun item){return item!=null && IsUnique(item.Unique);}
        public static int SignatureFor(string boss)
        {return boss=="veilglass_castellan"?Oathblade:boss=="stormroot_sovereign"?StormCrescent:CrystalFocusDb.SignatureFor(boss);}
        public static Vector3 Tip(int unique){return unique==Oathblade?new Vector3(0,0,1.18f):new Vector3(-.045f,.025f,1.08f);}
        public static WeaponDef Definition(LootGun item,WeaponId slot)
        {
            bool oath=item.Unique==Oathblade;
            // Bounded level/ascension growth uses the already saved item fields and original paid Forge transactions.
            float growth=1f+.012f*(Mathf.Clamp(item.Level,1,LootGenerator.MaxLevel)-1)+.04f*Mathf.Clamp(item.Ascensions,0,5);
            return new WeaponDef{Id=slot,Name=item.Name,Blurb=item.Flavor,Slot=WeaponSlot.Melee,Ammo=AmmoType.None,
                Fire=FireKind.Melee,Hold=HoldType.Melee,Loot=item,Damage=(oath?38f:33f)*growth,Interval=oath?.66f:.46f,
                MeleeRange=oath?2.45f:2.3f,MeleeForce=oath?4f:2.5f,Range=oath?2.45f:2.3f,
                Sound=SfxId.KnifeSlash,Element=Element.None};
        }
        public static MeleeMove Move(MeleeMove source,LootGun item)
        {
            if(source==null || !IsWeapon(item))return source;
            bool oath=item.Unique==Oathblade,heavy=source.Kind==MoveKind.Heavy;
            return new MeleeMove{Name=heavy?(oath?"Castellan's Oath Thrust":"Stormroot Rising Sweep"):source.Name,Kind=source.Kind,
                Anim=heavy?(oath?MeleeAnim.Thrust:MeleeAnim.SlashLeft):source.Anim,
                Windup=heavy?(oath?.48f:.28f):source.Windup,Active=heavy?(oath?.15f:.19f):source.Active,
                Recovery=heavy?(oath?.58f:.32f):source.Recovery,Damage=heavy?(oath?1.65f:1.18f):source.Damage*(oath?1f:.94f),
                Poise=heavy?(oath?2.8f:1.35f):source.Poise*(oath?1.15f:.85f),Stamina=heavy?(oath?28f:19f):source.Stamina,
                Range=heavy?(oath?2.6f:2.4f):source.Range,Arc=heavy?(oath?65f:155f):source.Arc,
                Lunge=heavy?(oath?.8f:.65f):source.Lunge,Force=heavy?(oath?5f:3f):source.Force,
                ChargeMax=heavy?(oath?.6f:.3f):source.ChargeMax,CancelAt=source.CancelAt,
                Knockdown=false,HyperArmor=false,Unblockable=false,Area=false};
        }
        public static LootArmory.CardStats Card(LootGun item)
        {
            WeaponDef definition=Definition(item,WeaponId.Loot1);
            MeleeMove heavy=Move(MeleeMoves.Heavy(MeleeClass.Katana),item);
            return new LootArmory.CardStats{Valid=true,RegionalMelee=true,Damage=definition.Damage,Mag=heavy.Stamina,
                Reload=heavy.Recovery,Crit=heavy.Poise,Range=definition.MeleeRange};
        }
        public static string Description(int unique)
        {return unique==Oathblade?"Broad amethyst blade. Slow committed heavy thrust with high poise damage; no ranged hit or free shield.":
            "Hooked turquoise crescent. Faster rising heavy sweep with lower damage and poise; contact uses the held blade.";}
    }
    public sealed partial class CrystalArsenal
    {
        private bool RegionalCollection04OE(int unique)
        {
            if(Game.Player==null || Game.Player.IsDead || Game.Player.InVehicle)return false;
            Vector3 player=Game.PlayerPosition,point;float yaw;string why;
            if(_casePlaced && Vector3.Distance(player,_casePos)<5f)return true;
            if(_caseFailed && _homeResolved && Vector3.Distance(player,_arrival)<5f)return true;
            if(CampaignPlacesRuntime04E.TryResolve("Venue:forge.exit",out point,out yaw,out why) && Vector3.Distance(player,point)<9f)return true;
            if(CampaignPlacesRuntime04E.TryResolve("Venue:forge.service",out point,out yaw,out why) && Vector3.Distance(player,point)<9f)return true;
            return _layout!=null && _layout.ResolveRealmAnchor04OC(unique==RegionalArsenal04OE.Oathblade?"Realm:castellan":"Realm:sovereign",out point)
                && Vector3.Distance(player,point)<22f;
        }
    }
}
