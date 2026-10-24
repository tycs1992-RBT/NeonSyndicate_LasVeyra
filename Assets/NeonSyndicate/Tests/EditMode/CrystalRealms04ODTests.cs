using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
namespace NeonSyndicate.Tests
{
    public sealed class CrystalRealms04ODTests
    {
        [Test] public void RevisionFourHasAnIndependentBankAndRejectsEveryOldBank()
        {
            for(int slot=0;slot<=3;slot++)
            {
                string name=SaveSystem.SlotFileName04OB(slot,4);
                Assert.That(name,Is.EqualTo("lasveyra_realms04od_save_"+slot+".json"));
                foreach(int old in new[]{0,1,2,3})
                {Assert.That(SaveSystem.BankMatches04OC(4,old),Is.False);Assert.That(SaveSystem.BankMatches04OC(old,4),Is.False);Assert.That(name,Is.Not.EqualTo(SaveSystem.SlotFileName04OB(slot,old)));}
            }
            Assert.That(new SaveData().WorldRevision04O,Is.Zero);Assert.That(SaveSystem.BankMatches04OC(4,4),Is.True);
            Assert.Throws<ArgumentOutOfRangeException>(()=>SaveSystem.SlotFileName04OB(0,99));
        }
        [TestCase(61724)] [TestCase(1)] [TestCase(2016)] [TestCase(77231)] [TestCase(92017)]
        public void NewProfileKeepsEveryServiceAndReceiptAndConnectsBothBranches(int seed)
        {
            CityLayout old=new CityLayout(seed);old.Generate();List<VenueBinding04C> before=InhabitedCityBindings04C.Select(old,InhabitedCityBindings04CTests.ReadCatalog());
            Assert.That(old.ApplyProfile04O(CityRevision04O.FracturedWilds04OC,before),Is.True);
            CityLayout world=new CityLayout(seed);world.Generate();List<VenueBinding04C> after=InhabitedCityBindings04C.Select(world,InhabitedCityBindings04CTests.ReadCatalog());
            Assert.That(world.ApplyProfile04O(CityRevision04O.CrystalRealms04OD,after),Is.True);
            foreach(VenueBinding04C v in before)Assert.That(after.Single(a=>a.Definition.Id==v.Definition.Id).Origin,Is.EqualTo(v.Origin));
            Assert.That(world.DataChipSpots,Is.EqualTo(old.DataChipSpots));Assert.That(world.CrystalShardSpots,Is.EqualTo(old.CrystalShardSpots));
            Assert.That(world.SpellStoneSpots,Is.EqualTo(old.SpellStoneSpots));Assert.That(world.CreatureSpots,Is.EqualTo(old.CreatureSpots));Assert.That(world.PickupSpots,Is.EqualTo(old.PickupSpots));
            Assert.That(world.RealmRoutes04OB.Select(r=>r.Id).Distinct().Count(),Is.EqualTo(world.RealmRoutes04OB.Count));
            Assert.That(world.Edges.Count,Is.EqualTo(old.Edges.Count));Assert.That(world.Lanes.Count,Is.EqualTo(old.Lanes.Count));
            foreach(string token in new[]{"Realm:gallery","Realm:storm-shrine","Realm:castellan","Realm:sovereign"})
            {
                Vector3 point;Assert.That(world.ResolveRealmAnchor04OC(token,out point),Is.True,token);
                Assert.That(world.FindWalkRoute04O(world.FindPoi("Veyra Lofts").Position,point).Count,Is.GreaterThan(1),token);
                Assert.That(old.ResolveRealmAnchor04OC(token,out point),Is.False);
            }
            foreach(RealmRoute04OB route in world.RealmRoutes04OB.Where(r=>r.Id=="dominion-gallery" || r.Id=="storm-shrine" || r.Id=="amethyst-challenge" || r.Id=="azure-expedition" || r.Id=="azure-return"))
                for(int i=1;i<route.Points.Count;i++)Assert.That(world.ClearPassage04O(route.Points[i-1],route.Points[i],1.8f),Is.True,route.Id);
            Assert.That(old.ApplyProfile04O(CityRevision04O.CrystalRealms04OD,before),Is.False,"Never mutate a loaded revision 3.");
        }
        [Test] public void RegionalQuestsPassRealValidatorAndKeepAllGrantsAtCompletion()
        {
            List<string> errors=new List<string>();QuestJsonTrace trace=new QuestJsonTrace();
            List<KeyValuePair<string,QuestFile>> quests=QuestTestData.Load<QuestFile>("Quests",errors,trace);
            List<KeyValuePair<string,EventFile>> events=QuestTestData.Load<EventFile>("Events",errors,trace);
            CityLayout validationWorld=new CityLayout(61724);validationWorld.Generate();
            QuestReport validation=QuestValidator.ValidateAll(quests,events,QuestRefs.Build(validationWorld),trace);
            Assert.That(errors,Is.Empty);Assert.That(validation.Errors,Is.Empty,validation.Summary());
            QuestFile file=QuestJson.Read<QuestFile>(File.ReadAllText(QuestTestData.ResourcesDir+"/Quests/crystal_realms_04od.json"),"04od",errors,null);
            Assert.That(errors,Is.Empty);Assert.That(file.Quests.Count,Is.EqualTo(2));
            foreach(QuestDef q in file.Quests)
            {
                Assert.That(q.Requires.WorldRevisions,Is.EqualTo(new[]{4,5,6}));Assert.That(q.VenueGiver,Is.EqualTo("forge"));
                Assert.That(q.Phases.SelectMany(p=>p.Steps).Count(s=>s.Type=="Boss"),Is.EqualTo(1));
                Assert.That(q.Phases.SelectMany(p=>p.Steps).Any(s=>s.Type=="Give" || s.Type=="Claim" || s.Type=="GiveGun"),Is.False);
                Assert.That(q.Phases.Last().Steps.First().At,Is.EqualTo("Venue:forge.exit"));Assert.That(q.Reward.Materials.Count,Is.EqualTo(2));
                Assert.That(q.NoAmbient,Is.True);Assert.That(q.IgnoreCrimes,Is.True);
            }
        }
        [Test] public void BossAdmissionRespectsBodyAndGroupCapsWithoutEvictingOtherFights()
        {
            Assert.That(RegionalBossRules04OD.Admit(4,3,7,1,2),Is.True);
            Assert.That(RegionalBossRules04OD.Admit(5,3,7,1,2),Is.False);
            Assert.That(RegionalBossRules04OD.Admit(4,3,7,2,2),Is.False);
            Assert.That(RegionalBossRules04OD.Admit(4,float.NaN,7,1,2),Is.False);
        }
        [TestCase("veilglass_castellan")] [TestCase("stormroot_sovereign")]
        public void BossesHaveBoundedDistinctPatternsAndNoRepeatTrophy(string id)
        {
            CreatureSpecies species=CreatureSpeciesDb.Get(id);Assert.That(species.Boss,Is.True);Assert.That(species.Loot.Length,Is.EqualTo(1));Assert.That(species.Lumen,Is.GreaterThan(0));Assert.That(CreaturePartsDb.For(id).Length,Is.EqualTo(3));Assert.That(species.Habitats,Is.EqualTo(new[]{Habitat.Arena}));
            Assert.That(RegionalBossRules04OD.FirstWin(true,false),Is.True);Assert.That(RegionalBossRules04OD.FirstWin(true,true),Is.False);Assert.That(RegionalBossRules04OD.FirstWin(false,false),Is.False);
            Assert.That(RegionalBossRules04OD.Phase(.9f),Is.Zero);Assert.That(RegionalBossRules04OD.Phase(.65f),Is.EqualTo(1));Assert.That(RegionalBossRules04OD.Phase(.3f),Is.EqualTo(2));
            for(int phase=0;phase<3;phase++)
            {
                int count=RegionalBossRules04OD.Marks(id,phase);Assert.That(count,Is.InRange(2,5));
                HashSet<Vector3> positions=new HashSet<Vector3>();
                for(int i=0;i<count;i++){Vector3 mark=RegionalBossRules04OD.Mark(id,phase,i,Vector3.zero,new Vector3(0,0,9));Assert.That(MathUtil.IsFinite(mark),Is.True);Assert.That(positions.Add(mark),Is.True);}
                Assert.That(RegionalBossRules04OD.Guard(id,phase,true),Is.GreaterThan(RegionalBossRules04OD.Guard(id,phase,false)));
            }
        }
        [Test] public void PaidRegionalModifiersHaveCostsUnlocksTradeoffsAndOneExclusiveTimer()
        {
            Dictionary<string,float> buffs=new Dictionary<string,float>();
            for(int index=10;index<14;index++)
            {
                string id=ForgeTreatments04M.Ids[index];RecipeDef recipe=ForgeDb.Get(id);
                Assert.That(recipe.Cost.Cash,Is.InRange(170,190));Assert.That(recipe.Cost.Materials.Count,Is.EqualTo(2));Assert.That(recipe.Seconds,Is.EqualTo(480));
                Assert.That(recipe.Rules.Any(r=>r.Kind==UnlockKind.QuestDone && r.Arg==(index<12?"fg_04od_castellan_oath":"fg_04od_stormroot")),Is.True);
                ForgeTreatments04M.Activate(buffs,id,480);Assert.That(buffs.Count,Is.EqualTo(1));
                MeshBuilder mesh=new MeshBuilder(2);GearFittings04N.Bracer(mesh,index);GearFittings04N.Collar(mesh,index);Assert.That(mesh.VertexCount,Is.GreaterThan(100));
            }
            buffs[ForgeTreatments04M.Ids[10]]=float.NaN;ForgeTreatments04M.Normalize(buffs);Assert.That(buffs.Count,Is.EqualTo(1));
        }
        [Test] public void RegionalBladesChangeRealHeavyMotionWithoutMutatingOriginalMovesOrGrantingFocus()
        {
            MeleeMove original=MeleeMoves.Heavy(MeleeClass.Blade);float before=original.Poise;
            MeleeMove echo=RegionalGear04OD.Build(original,MeleeClass.Blade,1),storm=RegionalGear04OD.Build(original,MeleeClass.Blade,2);
            Assert.That(echo.Poise,Is.GreaterThan(original.Poise));Assert.That(echo.Windup,Is.GreaterThan(original.Windup));Assert.That(storm.Recovery,Is.LessThan(original.Recovery));
            Assert.That(storm.Anim,Is.Not.EqualTo(echo.Anim));Assert.That(storm.Damage,Is.LessThan(original.Damage));Assert.That(original.Poise,Is.EqualTo(before));
            Assert.That(RegionalGear04OD.Build(original,MeleeClass.Fists,1),Is.SameAs(original));Assert.That(RegionalGear04OD.Build(original,MeleeClass.Blade,0),Is.SameAs(original));
            Assert.That(CrystalFocusDb.UniqueCount,Is.EqualTo(6));
        }
        [Test] public void RegionalSynergyRequiresEarnedPartnerFocusQuestAndTerritory()
        {
            Assert.That(RegionalGear04OD.Synergy("ash_warden",CrystalBehavior.Mirrorwake,true,false,0),Is.EqualTo(1));
            Assert.That(RegionalGear04OD.Synergy("storm_skimmer",CrystalBehavior.GyreCrescent,false,true,1),Is.EqualTo(2));
            Assert.That(RegionalGear04OD.Synergy("ash_warden",CrystalBehavior.Mirrorwake,false,false,0),Is.Zero);
            Assert.That(RegionalGear04OD.Synergy("storm_skimmer",CrystalBehavior.None,false,true,1),Is.Zero);
            Assert.That(RegionalGear04OD.Synergy("wild_drake",CrystalBehavior.GyreCrescent,true,true,1),Is.Zero);
            Assert.That(RegionalGear04OD.Synergy("ash_warden",CrystalBehavior.Mirrorwake,true,true,1),Is.Zero);
        }
        [TestCase(float.NaN)] [TestCase(float.PositiveInfinity)] [TestCase(0f)]
        public void ImportAdapterRejectsUnsafeScale(float scale)
        {string reason;Assert.That(ImportedModelRegistry04OD.ValidMapping(1,new[]{new ImportedBone04OD{SourceBone=0,TargetPath="root"}},scale,out reason),Is.False);Assert.That(reason,Is.Not.Empty);}
        [Test] public void ImportAdapterRequiresEverySourceBoneAndUniqueTarget()
        {
            string reason;ImportedBone04OD[] map={new ImportedBone04OD{SourceBone=0,TargetPath="root"},new ImportedBone04OD{SourceBone=1,TargetPath="chest"}};
            Assert.That(ImportedModelRegistry04OD.ValidMapping(2,map,1,out reason),Is.True);
            Assert.That(ImportedModelRegistry04OD.ValidMapping(3,map,1,out reason),Is.False);
            map[1].TargetPath="root";Assert.That(ImportedModelRegistry04OD.ValidMapping(2,map,1,out reason),Is.False);
            map[1].TargetPath="chest";map[1].SourceBone=0;Assert.That(ImportedModelRegistry04OD.ValidMapping(2,map,1,out reason),Is.False);
        }
        [TestCase(.016f)] [TestCase(.1f)] [TestCase(.4f)]
        public void ActualBossCycleCommitsOnceThenRecoversAndCancelsWithoutLateImpact(float dt)
        {
            RegionalBossCycle04OD cycle=new RegionalBossCycle04OD();
            cycle.Tick(3f,false);Assert.That(cycle.Begin(1.5f),Is.True);Assert.That(cycle.Begin(1.5f),Is.False);
            int impacts=0;for(int i=0;i<Mathf.CeilToInt(4f/dt);i++)if(cycle.Tick(dt,false)==RegionalBeatEvent04OD.Impact)impacts++;
            Assert.That(impacts,Is.EqualTo(1));Assert.That(cycle.Ready,Is.False);
            cycle.Tick(10f,false);Assert.That(cycle.Begin(1.5f),Is.True);
            Assert.That(cycle.Tick(dt,true),Is.EqualTo(RegionalBeatEvent04OD.Interrupted));Assert.That(cycle.Tell,Is.Zero);Assert.That(cycle.Recovery,Is.GreaterThan(0));
            for(int i=0;i<100;i++)Assert.That(cycle.Tick(dt,false),Is.Not.EqualTo(RegionalBeatEvent04OD.Impact));
            cycle.Close();Assert.That(cycle.Begin(1.5f),Is.False);Assert.That(cycle.Tick(100,false),Is.EqualTo(RegionalBeatEvent04OD.None));
        }
        [Test] public void IntegratedRegionalSolidsLeaveEveryAuthoredApproachClear()
        {
            CityLayout world=new CityLayout(61724);world.Generate();List<VenueBinding04C> venues=InhabitedCityBindings04C.Select(world,InhabitedCityBindings04CTests.ReadCatalog());
            Assert.That(world.ApplyProfile04O(CityRevision04O.CrystalRealms04OD,venues),Is.True);
            MeshBuilder mesh=new MeshBuilder(1);GothicRealmGeometry04OB.DominionCourt04OD(mesh,mesh,world.RealmChallenge04OC,true,world);GothicRealmGeometry04OB.StormGrove04OD(mesh,mesh,world.AzureGlade04OC,true,world);
            List<RectF> lowFaces=new List<RectF>();
            foreach(List<int> indices in (List<int>[])typeof(MeshBuilder).GetField("_tris",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(mesh))
            for(int i=0;i<indices.Count;i+=3)
            {
                Vector3 a=mesh.Vertices[indices[i]],b=mesh.Vertices[indices[i+1]],c=mesh.Vertices[indices[i+2]];
                if(Mathf.Min(a.y,b.y,c.y)>3.7f || Mathf.Max(a.y,b.y,c.y)<.3f)continue;
                lowFaces.Add(new RectF(Mathf.Min(a.x,b.x,c.x),Mathf.Min(a.z,b.z,c.z),Mathf.Max(a.x,b.x,c.x),Mathf.Max(a.z,b.z,c.z)));
            }
            foreach(RealmRoute04OB route in world.RealmRoutes04OB.Where(r=>!r.Id.StartsWith("neighbor-path/")))
            for(int i=1;i<route.Points.Count;i++)
            {
                int samples=Mathf.CeilToInt(Vector3.Distance(route.Points[i-1],route.Points[i]));
                for(int n=0;n<=samples;n++)
                {
                    Vector3 point=Vector3.Lerp(route.Points[i-1],route.Points[i],n/(float)Mathf.Max(1,samples));
                    foreach(RectF face in lowFaces)Assert.That(face.Inset(-1.8f).Contains(point),Is.False,route.Id+" intersects regional solid at "+point);
                }
            }
        }
        [Test] public void BothRegionsEmitRealFiniteNondegenerateGeometry()
        {
            foreach(bool azure in new[]{false,true})
            {
                MeshBuilder stone=new MeshBuilder(1),mineral=new MeshBuilder(1);
                if(azure)GothicRealmGeometry04OB.StormGrove04OD(stone,mineral,Vector3.zero,true);else GothicRealmGeometry04OB.DominionCourt04OD(stone,mineral,Vector3.zero,true);
                Assert.That(stone.VertexCount,Is.InRange(200,30000));Assert.That(mineral.VertexCount,Is.InRange(200,30000));
                foreach(Vector3 vertex in stone.Vertices.Concat(mineral.Vertices))Assert.That(MathUtil.IsFinite(vertex),Is.True);
                Assert.That(stone.Vertices.Max(v=>v.y),Is.GreaterThan(8));Assert.That(mineral.Vertices.Max(v=>v.y),Is.GreaterThan(18));
            }
        }
    }
}
