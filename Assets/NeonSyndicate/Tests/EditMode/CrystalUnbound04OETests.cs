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
        [Test] public void RevisionFiveIsExplicitAndRejectsAllEarlierBanks()
        {
            Assert.That(new SaveData().WorldRevision04O,Is.Zero);
            Assert.That(SaveSystem.SlotFileName04OB(0,5),Is.EqualTo("lasveyra_unbound04oe_save_0.json"));
            for(int old=0;old<5;old++){Assert.That(SaveSystem.BankMatches04OC(old,5),Is.False);Assert.That(SaveSystem.BankMatches04OC(5,old),Is.False);}
        }
        [TestCase(61724)] [TestCase(1)] [TestCase(2016)] [TestCase(77231)] [TestCase(92017)]
        public void NewRaisedRoutesArePhysicalConnectedAndLeaveRevisionFourUnchanged(int seed)
        {
            CityLayout old=new CityLayout(seed);old.Generate();List<VenueBinding04C> before=InhabitedCityBindings04C.Select(old,InhabitedCityBindings04CTests.ReadCatalog());
            Assert.That(old.ApplyProfile04O(CityRevision04O.CrystalRealms04OD,before),Is.True);
            CityLayout world=new CityLayout(seed);world.Generate();List<VenueBinding04C> venues=InhabitedCityBindings04C.Select(world,InhabitedCityBindings04CTests.ReadCatalog());
            Assert.That(world.ApplyProfile04O(CityRevision04O.CrystalUnbound04OE,venues),Is.True);
            Assert.That(old.ApplyProfile04O(CityRevision04O.CrystalUnbound04OE,before),Is.False);
            Assert.That(old.Unbound04OE,Is.False);Assert.That(old.RealmRoutes04OB.Any(r=>r.Id.StartsWith("04oe/")),Is.False);
            Assert.That(venues.Select(v=>v.Origin),Is.EqualTo(before.Select(v=>v.Origin)));
            Assert.That(world.CrystalShardSpots,Is.EqualTo(old.CrystalShardSpots));Assert.That(world.CreatureSpots,Is.EqualTo(old.CreatureSpots));
            foreach(string token in new[]{"Realm:oath-terrace","Realm:storm-ledge","Realm:storm-cavern"})
            {
                Vector3 point;Assert.That(world.ResolveRealmAnchor04OC(token,out point),Is.True);Assert.That(old.ResolveRealmAnchor04OC(token,out point),Is.False);
                world.ResolveRealmAnchor04OC(token,out point);Assert.That(world.FindWalkRoute04O(world.FindPoi("Veyra Lofts").Position,point).Count,Is.GreaterThan(1),token);
            }
            foreach(RealmRoute04OB route in world.RealmRoutes04OB.Where(r=>r.Id.StartsWith("04oe/")))
            {
                MeshBuilder stone=new MeshBuilder(1),mineral=new MeshBuilder(1);GothicRealmGeometry04OB.UnboundRoute04OE(stone,mineral,route);
                Assert.That(stone.VertexCount,Is.GreaterThanOrEqualTo(route.Id.Contains("access")?24:100));Assert.That(stone.Vertices.All(MathUtil.IsFinite),Is.True);
                for(int i=1;i<route.Points.Count;i++)
                {
                    Vector3 delta=route.Points[i]-route.Points[i-1];Assert.That(Mathf.Abs(delta.y)/Mathf.Max(.1f,MathUtil.Flat(delta).magnitude),Is.LessThan(.2f));
                    Assert.That(world.ClearPassage04O(route.Points[i-1],route.Points[i],1.8f),Is.True);
                }
            }
        }
        [Test] public void NewExplorationQuestsUseRealPlacesAndOneCompletionPayment()
        {
            List<string> errors=new List<string>();QuestJsonTrace trace=new QuestJsonTrace();
            List<KeyValuePair<string,QuestFile>> quests=QuestTestData.Load<QuestFile>("Quests",errors,trace);
            List<KeyValuePair<string,EventFile>> events=QuestTestData.Load<EventFile>("Events",errors,trace);
            CityLayout world=new CityLayout(61724);world.Generate();QuestReport report=QuestValidator.ValidateAll(quests,events,QuestRefs.Build(world),trace);
            Assert.That(errors,Is.Empty);Assert.That(report.Errors,Is.Empty,report.Summary());
            QuestFile file=QuestJson.Read<QuestFile>(System.IO.File.ReadAllText(QuestTestData.ResourcesDir+"/Quests/crystal_unbound_04oe.json"),"04oe",errors,null);
            Assert.That(file.Quests.Count,Is.EqualTo(2));
            foreach(QuestDef quest in file.Quests)
            {Assert.That(quest.Requires.WorldRevisions,Is.EqualTo(new[]{5,6}));Assert.That(quest.Phases.SelectMany(p=>p.Steps).Any(s=>s.Type=="Interact"),Is.True);Assert.That(quest.Phases.SelectMany(p=>p.Steps).Any(s=>s.Type=="Give"),Is.False);Assert.That(quest.Reward.Cash,Is.EqualTo(120));}
        }
        [TestCase(1)] [TestCase(2)] public void RegionalCoatsUseExistingPaidGearAndDifferInActualAffixes(int region)
        {
            GearOrder order=RegionalArmor04OE.Order(region);Assert.That(RegionalArmor04OE.Valid(order),Is.True);
            ForgeCost price=ForgeMath.GearCost(order);int cash=price.Cash;RegionalArmor04OE.Surcharge(price,region);
            Assert.That(price.Cash,Is.GreaterThan(cash));Assert.That(price.CountOf(region==1?"glass_chitin":"seed_core"),Is.GreaterThanOrEqualTo(2));
            GearItem item=RegionalArmor04OE.Build(region,5),again=RegionalArmor04OE.Build(region,5);
            Assert.That(item.Name,Is.EqualTo(again.Name));Assert.That(item.Seed,Is.EqualTo(again.Seed));Assert.That(item.Uid,Is.Zero,"Only the original Gear owner adopts the UID after payment");
            Assert.That(RegionalArmor04OE.Region(item),Is.EqualTo(region));Assert.That(item.Affixes.Count,Is.EqualTo(3));
            Assert.That(item.Get(region==1?AffixType.GuardEfficiency:AffixType.MaxStamina),Is.GreaterThan(0));
            Assert.That(RegionalArmor04OE.Quest(region),Is.EqualTo(region==1?"fg_04od_castellan_oath":"fg_04od_stormroot"));
            MeshBuilder mesh=new MeshBuilder(1);RegionalArmor04OE.Shell(mesh,region,0);Assert.That(mesh.VertexCount,Is.GreaterThan(100));
            order.Set=0;Assert.That(RegionalArmor04OE.Valid(order),Is.False);
        }
        [Test] public void CompanionRegionalMasteryRequiresEarnedIdentityAndStopsAtThree()
        {
            CompanionRecord companion=new CompanionRecord{Uid=16,Rank=BondIds.RankBonded};
            Assert.That(RegionalResonance04OE.Credit(companion,1,false),Is.False);
            companion.Uid=0;Assert.That(RegionalResonance04OE.Credit(companion,1,true),Is.False);companion.Uid=16;
            for(int i=1;i<=3;i++){Assert.That(RegionalResonance04OE.Credit(companion,1,true),Is.True);Assert.That(RegionalResonance04OE.Count(companion,1),Is.EqualTo(i));}
            Assert.That(RegionalResonance04OE.Credit(companion,1,true),Is.False);Assert.That(RegionalResonance04OE.Count(companion,2),Is.Zero);
            CompanionRecord restored=new CompanionRecord{Uid=16,Rank=companion.Rank,Milestones=new List<string>(companion.Milestones)};
            Assert.That(RegionalResonance04OE.Count(restored,1),Is.EqualTo(3));Assert.That(RegionalResonance04OE.Credit(restored,1,true),Is.False);
        }
        [Test] public void PassiveRigPathsAreExplicitUniqueAndContained()
        {
            Assert.That(ImportedModelRegistry04OD.ValidPassivePaths(new[]{"root/chest/twist","root/tail/ornament"}),Is.True);
            foreach(string[] bad in new[]{new[]{"root/a","root/a"},new[]{""},new[]{"../other"},new[]{"/root"}})
                Assert.That(ImportedModelRegistry04OD.ValidPassivePaths(bad),Is.False);
        }
        [TestCase(false)] [TestCase(true)] public void JointedBossRegaliaHasActualDistinctGeometry(bool azure)
        {
            for(int anchor=0;anchor<=2;anchor++)
            {MeshBuilder stone=new MeshBuilder(1),crystal=new MeshBuilder(1);RegionalBossArt04OE.Build(stone,crystal,azure,anchor);Assert.That(stone.VertexCount+crystal.VertexCount,Is.GreaterThanOrEqualTo(anchor==2?72:100));Assert.That(stone.Vertices.All(MathUtil.IsFinite),Is.True);}
        }
        [Test] public void RaisedFloorIsClosedHasCorrectFacingAndCanBeClimbedWithoutAJump()
        {
            MeshBuilder floor=new MeshBuilder(1);GothicRealmGeometry04OB.WalkSpan04OE(floor,new Vector3(0,.15f,0),new Vector3(0,3.15f,24),5);
            List<int>[] triangles=(List<int>[])typeof(MeshBuilder).GetField("_tris",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(floor);
            Assert.That(floor.VertexCount,Is.EqualTo(24));Assert.That(triangles[0].Count,Is.EqualTo(36));
            for(int i=0;i<triangles[0].Count;i+=3)
            {int a=triangles[0][i],b=triangles[0][i+1],c=triangles[0][i+2];Vector3 face=Vector3.Cross(floor.Vertices[b]-floor.Vertices[a],floor.Vertices[c]-floor.Vertices[a]);Assert.That(Vector3.Dot(face,floor.Normals[a]),Is.GreaterThan(.0001f));}
        }
        [TestCase(1)] [TestCase(2)] public void MalikNewSkinLayerActuallyEmitsWithinOriginalBudget(int detail)
        {
            Appearance look=Appearance.Malik();BodyData body=BodyBuilder.Build(look,RigLayout.From(look),detail);
            Assert.That(body.UnboundTailoringVertices04OE,Is.GreaterThan(0),"A budget-skipped method is not visible implementation");
            Assert.That(body.Vertices.Count,Is.LessThan(detail==2?13500:8200));Assert.That(body.Weights.Count,Is.EqualTo(body.Vertices.Count));
        }
    }
}
