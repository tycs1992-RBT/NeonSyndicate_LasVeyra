using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
namespace NeonSyndicate.Tests
{
    public sealed class FracturedWilds04OCTests
    {
        private CityLayout _wild,_old;
        private List<VenueBinding04C> _venues,_previous;
        private static CityLayout World(CityRevision04O revision,int seed,out List<VenueBinding04C> venues)
        {CityLayout l=new CityLayout(seed);l.Generate();venues=InhabitedCityBindings04C.Select(l,InhabitedCityBindings04CTests.ReadCatalog());Assert.That(l.ApplyProfile04O(revision,venues),Is.True);return l;}
        [OneTimeSetUp] public void Prepare()
        {_wild=World(CityRevision04O.FracturedWilds04OC,61724,out _venues);_old=World(CityRevision04O.GothicRealm04OB,61724,out _previous);}
        private static float Distance(CityLayout l,Vector3 a,Vector3 b)
        {List<Vector3> path=l.FindWalkRoute04O(a,b);Assert.That(path,Is.Not.Empty,"No path "+a+" to "+b);float n=0;for(int i=1;i<path.Count;i++)n+=Vector3.Distance(path[i-1],path[i]);return n;}
        private static Vector3 Spawn(CityLayout l){Poi p=l.FindPoi("Veyra Lofts");return p.Position+p.Facing*3.5f;}
        [Test] public void RevisionTwoRemainsExactlyInItsOwnGeography()
        {
            Assert.That(_old.RealmThreshold04OB,Is.EqualTo(new Vector3(12,.15f,288)));Assert.That(_old.RealmGrotto04OB,Is.EqualTo(new Vector3(12,.15f,316)));
            Assert.That(_old.WildPatches04OC,Is.Empty);Assert.That(_old.WildCoverage04OC,Is.Empty);
            Assert.That(_old.ApplyProfile04O(CityRevision04O.FracturedWilds04OC,_previous),Is.False,"No mutation of an existing saved profile");
            Assert.That(CityRebirthRules04O.Known(4),Is.False);
        }
        [Test] public void EverySaveBankHasIndependentPrimaryBackupAndTemporaryFiles()
        {
            foreach(int slot in new[]{0,1,2,3})
            {
                HashSet<string> names=new HashSet<string>();
                foreach(int revision in new[]{0,2,3})
                {string name=SaveSystem.SlotFileName04OB(slot,revision);Assert.That(names.Add(name),Is.True);Assert.That(names.Add(SaveFileTransaction.BackupPath(name)),Is.True);Assert.That(names.Add(SaveFileTransaction.TempPath(name)),Is.True);}
            }
            for(int stored=-1;stored<=4;stored++)for(int selected=0;selected<=3;selected++)
                Assert.That(SaveSystem.BankMatches04OC(stored,selected),Is.EqualTo(stored>=0&&stored<=3&&(stored<2?0:stored)==(selected<2?0:selected)));
        }
        [Test] public void CloseNaturalRoutesAreRealGraphRoutesToTheMovedGeometry()
        {
            Assert.That(Distance(_wild,Spawn(_wild),_wild.RealmThreshold04OB),Is.LessThanOrEqualTo(180f));
            Assert.That(Distance(_wild,Spawn(_wild),_wild.RealmGrotto04OB),Is.LessThanOrEqualTo(250f));
            Building cave=_wild.RealmBuildings04OB.Single(b=>b.GothicFamily04OB==8);
            Assert.That(cave.Footprint.Contains(_wild.RealmGrotto04OB),Is.True);
            Assert.That(GothicRealmGeometry04OB.BlocksGround04OB(cave,_wild.RealmGrotto04OB,1.8f),Is.False);
        }
        [Test] public void OriginalServiceRoutesNeverRegressByMoreThanTenPercent()
        {
            foreach(VenueBinding04C v in _venues)
            {VenueBinding04C old=_previous.Single(p=>p.Definition.Id==v.Definition.Id);Assert.That(v.Origin,Is.EqualTo(old.Origin),v.Definition.Id);Assert.That(Distance(_wild,Spawn(_wild),v.Outside),Is.LessThanOrEqualTo(Distance(_old,Spawn(_old),old.Outside)*1.1f),v.Definition.Id);}
        }
        [Test] public void OuterUnownedContinuousBuildingGridIsActuallyRemoved()
        {
            int removed=_wild.WildCoverage04OC.Count(r=>r.Disposition.StartsWith("natural terrain, former"));
            Assert.That(removed,Is.GreaterThan(500));
            Assert.That(_wild.WildCoverage04OC.All(r=>!string.IsNullOrEmpty(r.Disposition)),Is.True);
            foreach(Block block in _wild.Blocks)foreach(Lot lot in block.Lots)foreach(Building b in lot.Buildings)
                if(!CityLayout.InSettlement04OC(b.Footprint.Center(0)))Assert.That(_wild.WildCoverage04OC.Any(r=>r.Seed==b.Seed&&r.AfterHeight>0),Is.True,"Unclassified exposed structure");
            Assert.That(_wild.WildCoverage04OC.Count(r=>r.Disposition.StartsWith("preserved real service")),Is.EqualTo(12));
        }
        [Test] public void AllNewAuthoredPathsHaveGroundAndCompanionClearance()
        {
            foreach(RealmRoute04OB route in _wild.RealmRoutes04OB.Where(r=>!r.Id.StartsWith("neighbor-path/")))
            for(int i=1;i<route.Points.Count;i++)Assert.That(_wild.ClearPassage04O(route.Points[i-1],route.Points[i],1.8f),Is.True,route.Id+" segment "+i);
            Assert.That(Distance(_wild,_wild.RealmGrotto04OB,_wild.AzureGlade04OC),Is.GreaterThan(0f));
        }
        [Test] public void ChallengeGladesRemainOutsideOriginalStarterProtection()
        {
            foreach(Vector3 p in new[]{_wild.RealmChallenge04OC,_wild.AzureGlade04OC})
            {
                Assert.That(StarterSafetyRules.InRadius(p,_wild.FindPoi("Veyra Lofts").Position,StarterSafetyRules.LoftsRadius),Is.False);
                Assert.That(StarterSafetyRules.InRadius(p,_wild.FindPoi("Mara Solane Street Clinic").Position,StarterSafetyRules.ClinicRadius),Is.False);
                Assert.That(StarterSafetyRules.InPromenade(p),Is.False);Assert.That(_wild.IsInsideBuilding(p,18f),Is.False);
            }
        }
        [Test] public void TerrainRisesOnlyOnAdmissibleGroundAndHasFiniteUpwardSurface()
        {
            Assert.That(_wild.WildPatches04OC.Count,Is.GreaterThan(100));
            foreach(WildPatch04OC patch in _wild.WildPatches04OC.Take(30))
            {
                MeshBuilder m=new MeshBuilder(1);GothicRealmGeometry04OB.WildTerrain04OC(m,_wild,patch);
                foreach(Vector3 p in m.Vertices){Assert.That(MathUtil.IsFinite(p),Is.True);Assert.That(p.y,Is.GreaterThanOrEqualTo(.017f));Assert.That(p.y-.018f,Is.LessThanOrEqualTo(_wild.WildClearance04OC(p)*.22f+.001f));}
                foreach(Vector3 n in m.Normals)Assert.That(n.y,Is.GreaterThan(.5f));
            }
            foreach(RealmRoute04OB route in _wild.RealmRoutes04OB)foreach(Vector3 p in route.Points)Assert.That(_wild.WildClearance04OC(p),Is.Zero);
        }
        [Test] public void OrderedOriginalReceiptsAndRoadArteriesArePreserved()
        {
            foreach((List<Vector3>,List<Vector3>) pair in new[]{(_old.DataChipSpots,_wild.DataChipSpots),(_old.CrystalShardSpots,_wild.CrystalShardSpots),(_old.SpellStoneSpots,_wild.SpellStoneSpots)})
            {Assert.That(pair.Item2.Count,Is.EqualTo(pair.Item1.Count));for(int i=0;i<pair.Item1.Count;i++){Assert.That(pair.Item2[i].x,Is.EqualTo(pair.Item1[i].x));Assert.That(pair.Item2[i].z,Is.EqualTo(pair.Item1[i].z));Assert.That(pair.Item2[i].y,Is.LessThanOrEqualTo(pair.Item1[i].y));}}
            Assert.That(_wild.Edges.Count,Is.EqualTo(_old.Edges.Count));Assert.That(_wild.Lanes.Count,Is.EqualTo(_old.Lanes.Count));
            foreach(RoadEdge e in _wild.Edges)Assert.That(e.Id,Is.InRange(0,_wild.Edges.Count-1));
            Assert.That(_wild.DesertLots.Count,Is.EqualTo(_old.DesertLots.Count));
        }
        [Test] public void ReceiptRoofIdentityFollowsItsAdmittedRevisionThreeLanding()
        {
            foreach(Building b in _wild.RealmBuildings04OB.Where(b=>b.GothicFamily04OB==9))
            {
                WildStructure04OC row=_wild.WildCoverage04OC.Single(r=>r.Seed==b.Seed&&r.Before.Equals(b.Footprint));
                Assert.That(b.Height,Is.LessThanOrEqualTo(row.BeforeHeight));Assert.That(b.Footprint,Is.EqualTo(row.Before));
                MeshBuilder stone=new MeshBuilder(1),trim=new MeshBuilder(1),crystal=new MeshBuilder(1);GothicRealmGeometry04OB.Family(stone,trim,crystal,b,false);
                Assert.That(stone.Vertices.Any(v=>Mathf.Abs(v.y-(CityLayout.CurbHeight+b.Height))<.001f),Is.True);
            }
        }
        [Test] public void SemanticQuestPlacesResolveThroughBothSavedLayouts()
        {
            foreach(CityLayout l in new[]{_old,_wild})foreach(string at in new[]{"Realm:threshold","Realm:grotto"})
            {Vector3 a,b;Assert.That(l.ResolveRealmAnchor04OC(at,out a),Is.True);Assert.That(QuestPlaces.Resolve(l,at,0,0,Vector3.zero,Vector3.zero,false,Vector3.zero,0,out b),Is.True);Assert.That(a,Is.EqualTo(b));}
            Assert.That(QuestLogic.WorldMatches(new QuestRequirements{WorldRevisions=new List<int>{2,3}},0),Is.False);
            Assert.That(QuestLogic.WorldMatches(new QuestRequirements{WorldRevisions=new List<int>{2,3}},3),Is.True);
        }
        [Test] public void RegionalBattleHasDifferentBodiesAndDamageWindowsNotOnlyColour()
        {
            Assert.That(FracturedWildsCombat04OC.Species("ev_04oc_azure_watch",0),Is.EqualTo("neon_jackal"));
            Assert.That(FracturedWildsCombat04OC.Species("ev_04ob_threshold_watch",0),Is.EqualTo("shard_golem"));
            Assert.That(FracturedWildsCombat04OC.Caster("ev_04oc_azure_watch",1),Is.False);
            Assert.That(FracturedWildsCombat04OC.Caster("ev_04ob_threshold_watch",1),Is.True);
            foreach(CreatureAttack a in FracturedWildsCombat04OC.AzureAttacks())
            {Assert.That(a.Windup,Is.GreaterThanOrEqualTo(.6f));Assert.That(a.Recovery,Is.GreaterThanOrEqualTo(.9f));Assert.That(a.Damage,Is.LessThanOrEqualTo(1f));}
            CreatureLook baseLook=CreatureLooks.For("neon_jackal",1),variant=FracturedWildsCombat04OC.AzureLook(baseLook);
            Assert.That(variant.Spines,Is.GreaterThan(baseLook.Spines));Assert.That(baseLook.Plates,Is.Zero);Assert.That(variant.Plates,Is.EqualTo(1));
        }
        [Test] public void RegionalPaidTreatmentsAreEarnedExclusiveCostedAndExpiring()
        {
            foreach(string id in new[]{"brew_04oc_amethyst_return","brew_04oc_shardwood_conductor"})
            {
                RecipeDef recipe=ForgeDb.All.Single(r=>r.Id==id);Assert.That(recipe.Cost.Cash,Is.GreaterThan(0));Assert.That(recipe.Cost.Materials,Is.Not.Empty);Assert.That(recipe.Seconds,Is.InRange(60f,480f));
                Assert.That(recipe.Rules.Any(r=>r.Kind==UnlockKind.QuestDone&&r.Arg=="fg_04oc_roots_remember"),Is.True);
                Dictionary<string,float> buffs=new Dictionary<string,float>{{ForgeTreatments04M.Ids[0],100f}};ForgeTreatments04M.Activate(buffs,id,10f);Assert.That(buffs.Count,Is.EqualTo(1));Assert.That(buffs[id],Is.EqualTo(10f));
                MeshBuilder mesh=new MeshBuilder(2);GearFittings04N.Bracer(mesh,Array.IndexOf(ForgeTreatments04M.Ids,id));Assert.That(mesh.VertexCount,Is.GreaterThan(0));
            }
        }
        [Test] public void RegionalFirstWinReceiptsSurviveRestoreAndRejectNewOccurrences()
        {
            CityRewardLedger04F ledger=new CityRewardLedger04F();
            foreach(string id in new[]{"ev_04ob_threshold_watch","ev_04oc_azure_watch"})
            {
                string first=FracturedWildsCombat04OC.RewardToken(id,0,3);Assert.That(ledger.TryClaim(first),Is.True);
                Assert.That(ledger.TryClaim(FracturedWildsCombat04OC.RewardToken(id,1,3)),Is.False);
                CityRewardLedger04F restored=new CityRewardLedger04F();restored.Restore(ledger.Tokens);
                Assert.That(restored.TryClaim(FracturedWildsCombat04OC.RewardToken(id,99,3)),Is.False);
                Assert.That(FracturedWildsCombat04OC.RewardToken(id,0,2),Is.Not.EqualTo(FracturedWildsCombat04OC.RewardToken(id,1,2)),"Older worlds retain occurrence receipts");
            }
        }
        [Test] public void SourceCaptureWaypointsAppendWithoutChangingOriginalViews()
        {
            CityLayout legacy=new CityLayout(61724);legacy.Generate();List<GoldenView> before=GoldenViews.Build(legacy),after=GoldenViews.Build(_wild);
            Assert.That(after.Count,Is.EqualTo(before.Count+8));
            for(int i=0;i<before.Count;i++){Assert.That(after[i].Id,Is.EqualTo(before[i].Id));Assert.That(after[i].Position,Is.EqualTo(before[i].Position));}
            Assert.That(after.Select(v=>v.Id).Distinct().Count(),Is.EqualTo(after.Count));
        }
        [Test] public void ForestAndReliefExistBesideRatherThanInsideTheAuthoredPaths()
        {
            Assert.That(_wild.WildScenery04OC.Count(s=>s.Kind==0),Is.GreaterThan(50));
            Assert.That(_wild.WildScenery04OC.Any(s=>s.Region==0),Is.True);Assert.That(_wild.WildScenery04OC.Any(s=>s.Region==1),Is.True);
            foreach(WildScenery04OC item in _wild.WildScenery04OC)Assert.That(_wild.WildClearance04OC(item.Position),Is.GreaterThanOrEqualTo(5f));
            Assert.That(_wild.WildPatches04OC.Any(p=>_wild.WildHeight04OC(p,p.Area.Center(0))>1f),Is.True);
            Assert.That(_wild.WildCoverage04OC.Count(r=>r.AfterHeight>30f),Is.LessThan(25),"Unowned high-rise heights reappeared");
        }
        [Test] public void NewQuestsPassTheRealValidatorAndKeepFiniteOriginalOwnerRewards()
        {
            List<string> validationErrors=new List<string>();QuestJsonTrace trace=new QuestJsonTrace();
            List<KeyValuePair<string,QuestFile>> quests=QuestTestData.Load<QuestFile>("Quests",validationErrors,trace);
            List<KeyValuePair<string,EventFile>> events=QuestTestData.Load<EventFile>("Events",validationErrors,trace);
            QuestReport report=QuestValidator.ValidateAll(quests,events,QuestRefs.Build(_wild),trace);
            Assert.That(validationErrors,Is.Empty);
            Assert.That(report.Errors,Is.Empty,report.Summary());
            List<string> errors=new List<string>();QuestFile file=QuestJson.Read<QuestFile>(File.ReadAllText(QuestTestData.ResourcesDir+"/Quests/fractured_wilds_04oc.json"),"04oc",errors,null);
            Assert.That(errors,Is.Empty);QuestDef q=file.Quests.Single();Assert.That(q.VenueGiver,Is.EqualTo("forge"));
            Assert.That(q.Requires.Quests,Does.Contain("fg_04ob_stone_not_silence"));Assert.That(q.Requires.WorldRevision,Is.EqualTo(3));
            Assert.That(q.Phases.SelectMany(p=>p.Steps).Count(s=>s.Type=="Interact"),Is.EqualTo(4));
            Assert.That(q.Phases.SelectMany(p=>p.Steps).Any(s=>s.Type=="GiveGun"||s.Type=="Claim"||s.Type=="Collect"),Is.False);
            Assert.That(q.Reward.Cash,Is.EqualTo(90));Assert.That(q.Reward.Materials.Sum(m=>m.Count),Is.EqualTo(6));
        }
    }
}
