using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
namespace NeonSyndicate.Tests
{
    public sealed class CrystalUnbound04OETests
    {
        [TestCase("veilglass_castellan",23)] [TestCase("stormroot_sovereign",24)]
        public void FirstVictoryRebuildsOnePermanentMeleeClaim(string boss,int unique)
        {
            List<CrystalEntitlementSave> ledger=new List<CrystalEntitlementSave>();
            string token=CrystalEntitlementRules.BossToken(boss);
            CrystalEntitlementSave earned=CrystalEntitlementRules.Create(ledger,token,RegionalArsenal04OE.SignatureFor(boss),8,null,false);
            Assert.That(CrystalEntitlementRules.ExpectedUnique(token),Is.EqualTo(unique));
            Assert.That(CrystalEntitlementRules.IsPending(earned),Is.True);Assert.That(earned.Uid,Is.Zero);
            LootGun item=CrystalEntitlementRules.BuildGun(earned),again=CrystalEntitlementRules.BuildGun(earned);
            Assert.That(item.Parts,Is.EqualTo(again.Parts));Assert.That(item.Name,Is.EqualTo(again.Name));
            WeaponDef definition=LootGenerator.ToWeaponDef(item,WeaponId.Loot2);
            Assert.That(definition.Fire,Is.EqualTo(FireKind.Melee));Assert.That(definition.Slot,Is.EqualTo(WeaponSlot.Melee));
            Assert.That(definition.Hold,Is.EqualTo(HoldType.Melee));Assert.That(definition.IsGun,Is.False);Assert.That(definition.UsesAmmo,Is.False);
            Assert.That(CrystalCombatPolicy04F.LootBehavior(item),Is.EqualTo(CrystalBehavior.None));
            Assert.That(CrystalFocusDb.BehaviorOfUnique(unique),Is.EqualTo(CrystalBehavior.None));Assert.That(CrystalFocusDb.UniqueCount,Is.EqualTo(6));
            Assert.That(CrystalEntitlementRules.Create(ledger,token,unique,8,null,false),Is.Null);
        }
        [TestCase(23)] [TestCase(24)]
        public void ClaimedUpgradeRoundTripsThroughOriginalRecordsAndNeverRemintsAfterSale(int unique)
        {
            List<CrystalEntitlementSave> ledger=new List<CrystalEntitlementSave>();string boss=unique==23?"veilglass_castellan":"stormroot_sovereign";
            CrystalEntitlementSave reward=CrystalEntitlementRules.Create(ledger,CrystalEntitlementRules.BossToken(boss),unique,9,null,false);
            LootGun item=CrystalEntitlementRules.BuildGun(reward);item.Uid=402;item.Favorite=true;
            Assert.That(CrystalEntitlementRules.MarkGranted(reward,item.Uid),Is.True);
            LootGun upgraded=LootForgeMath.Ascended(item),restored=LootRecords.FromSave(LootRecords.ToSave(upgraded));
            Assert.That(LootForgeMath.AscendCost(item).Cash,Is.GreaterThan(0));Assert.That(upgraded.Ascensions,Is.EqualTo(1));
            Assert.That(CrystalForgeRules.KeepsBehavior(item,upgraded),Is.True);Assert.That(restored.Uid,Is.EqualTo(402));Assert.That(restored.Unique,Is.EqualTo(unique));
            Assert.That(restored.Favorite,Is.True);Assert.That(restored.Ascensions,Is.EqualTo(1));Assert.That(restored.Traits,Is.Empty);
            Assert.That(RegionalArsenal04OE.Definition(restored,WeaponId.Loot1).Damage,Is.GreaterThan(RegionalArsenal04OE.Definition(item,WeaponId.Loot1).Damage));
            List<CrystalEntitlementSave> reloaded=CrystalEntitlementRules.CopyAll(ledger);
            CrystalEntitlementRules.Reconcile(reloaded,new List<int>(),new List<int>());
            Assert.That(CrystalEntitlementRules.IsGranted(reloaded[0]),Is.True);Assert.That(CrystalEntitlementRules.MarkGranted(reloaded[0],403),Is.False);
            Assert.That(CrystalEntitlementRules.CreateRetro(reloaded,boss,9,new int[0],new int[0]),Is.Null);
        }
        [TestCase("veilglass_castellan",23)] [TestCase("stormroot_sovereign",24)]
        public void EarlierLegitimateVictoryGetsOnePendingClaimAndMayAdoptAnExistingCopy(string boss,int unique)
        {
            List<CrystalEntitlementSave> ledger=new List<CrystalEntitlementSave>();
            CrystalEntitlementSave claim=CrystalEntitlementRules.CreateRetro(ledger,boss,6,new[]{67},new[]{unique});
            Assert.That(claim.Retro,Is.True);Assert.That(claim.Uid,Is.EqualTo(67));Assert.That(CrystalEntitlementRules.IsGranted(claim),Is.True);
            Assert.That(CrystalEntitlementRules.CreateRetro(ledger,boss,6,new[]{67},new[]{unique}),Is.Null);
            ledger.Clear();claim=CrystalEntitlementRules.CreateRetro(ledger,boss,6,new int[0],new int[0]);
            Assert.That(CrystalEntitlementRules.IsPending(claim),Is.True);Assert.That(claim.Unique,Is.EqualTo(unique));
        }
        [Test] public void PermanentUniquesNeverEnterRandomOrMerchantPools()
        {
            foreach(Rarity rarity in new[]{Rarity.Common,Rarity.Rare,Rarity.Legendary,Rarity.Mythic})
                Assert.That(LootUniques.OfRarity(rarity,-1).Any(RegionalArsenal04OE.IsUnique),Is.False);
            Assert.That(LootUniques.ForBoss("veilglass_castellan"),Is.EqualTo(-1));Assert.That(LootUniques.ForBoss("stormroot_sovereign"),Is.EqualTo(-1));
            Assert.That(RegionalArsenal04OE.SignatureFor("golem_foreman"),Is.EqualTo(CrystalFocusDb.SunderheartUnique));
            Assert.That(RegionalArsenal04OE.SignatureFor("glacier_titan"),Is.EqualTo(-1));
            for(int seed=1;seed<100;seed++)Assert.That(RegionalArsenal04OE.IsWeapon(LootEconomy.ShopGun(seed,0,12)),Is.False);
        }
        [Test] public void FullPackPreservesPendingRewardAndProtectsReplacement()
        {
            CrystalCapacity full=new CrystalCapacity{FreeSlot=-1,BackpackFull=true,InSlot=-1,SlotOccupied=new[]{true,true,true,true},SlotFavorite=new[]{true,false,false,false}};
            string reason;Assert.That(CrystalOfferRules.Allowed(CrystalClaimAction.EquipFree,-1,full,out reason),Is.False);
            Assert.That(CrystalOfferRules.Allowed(CrystalClaimAction.ToBackpack,-1,full,out reason),Is.False);
            Assert.That(CrystalOfferRules.Allowed(CrystalClaimAction.EquipReplace,0,full,out reason),Is.False);
            Assert.That(CrystalOfferRules.Allowed(CrystalClaimAction.EquipReplace,1,full,out reason),Is.False);
            full.BackpackFull=false;Assert.That(CrystalOfferRules.Allowed(CrystalClaimAction.EquipReplace,1,full,out reason),Is.True);
        }
        [TestCase(23)] [TestCase(24)]
        public void PermanentMovesUseDistinctBoundedTimingAndNeverMutateSharedAnimations(int unique)
        {
            LootGun item=LootGenerator.Generate(15,5,Rarity.Legendary,-1,-1,unique);
            MeleeMove source=MeleeMoves.Heavy(MeleeClass.Katana);float windup=source.Windup,poise=source.Poise;
            MeleeMove move=RegionalArsenal04OE.Move(source,item);
            Assert.That(move,Is.Not.SameAs(source));Assert.That(source.Windup,Is.EqualTo(windup));Assert.That(source.Poise,Is.EqualTo(poise));
            Assert.That(move.Windup,Is.GreaterThan(.25f));Assert.That(move.Recovery,Is.GreaterThan(.3f));Assert.That(move.Stamina,Is.InRange(19,28));
            Assert.That(move.Area || move.Unblockable || move.HyperArmor || move.Knockdown,Is.False);
            Assert.That(LootArmory.StatsOf(item).RegionalMelee,Is.True);Assert.That(LootArmory.StatsOf(item).Focus,Is.False);
            Weapons.SetLoot(0,LootGenerator.ToWeaponDef(item,WeaponId.Loot1));
            try{Vector3 h,t;float radius;Assert.That(MeleeMoves.ClassOf(WeaponId.Loot1),Is.EqualTo(MeleeClass.Katana));Assert.That(BladeSweep04N.Local(WeaponId.Loot1,out h,out t,out radius),Is.True);Assert.That(t,Is.EqualTo(RegionalArsenal04OE.Tip(unique)));}
            finally{Weapons.SetLoot(0,null);}
        }
        [TestCase(23)] [TestCase(24)]
        public void AuthoredWeaponGeometryHasThicknessAndUpgradeInlays(int unique)
        {
            MeshBuilder baseMesh=new MeshBuilder(3),upgraded=new MeshBuilder(3);
            WeaponModels.BuildRegional04OE(baseMesh,unique,0);WeaponModels.BuildRegional04OE(upgraded,unique,1);
            Assert.That(baseMesh.VertexCount,Is.InRange(500,5000));Assert.That(upgraded.VertexCount,Is.GreaterThan(baseMesh.VertexCount));
            Assert.That(baseMesh.Vertices.All(MathUtil.IsFinite),Is.True);Assert.That(baseMesh.Vertices.Max(p=>p.z),Is.EqualTo(unique==23?1.18f:1.08f).Within(.01f));
            Assert.That(baseMesh.Vertices.Max(p=>p.y)-baseMesh.Vertices.Min(p=>p.y),Is.GreaterThan(.02f));
            Assert.That(baseMesh.HasGeometry(0) && baseMesh.HasGeometry(1) && baseMesh.HasGeometry(2),Is.True);
        }
        [TestCase(23)] [TestCase(24)]
        public void UnsupportedInfusionsAndTraitsAreRefusedBeforePayment(int unique)
        {
            LootGun item=LootGenerator.Generate(41,4,Rarity.Legendary,-1,-1,unique);string note;
            Assert.That(LootForgeMath.CanInfuse(item,Element.Volt),Is.False);Assert.That(CrystalForgeRules.Check(item,CrystalForgeOp.Infuse,out note),Is.False);
            Assert.That(CrystalForgeRules.Check(item,CrystalForgeOp.RaiseLevel,out note),Is.True);
            item.Traits.Add(LootTrait.ChargeShot);item.Element=Element.Volt;LootGenerator.Refresh(item);
            Assert.That(item.Traits,Is.Empty);Assert.That(item.Element,Is.EqualTo(Element.None));Assert.That(CrystalForgeRules.NeedsGrantedOnceConfirm(item),Is.True);
        }
    }
}
