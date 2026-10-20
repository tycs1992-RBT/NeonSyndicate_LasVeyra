using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
namespace NeonSyndicate.Tests
{
    public sealed class LivingFrontier04OGTests
    {
        [Test] public void WardHasTellProtectionAndPunishableRecovery()
        {
            FrontierWard04OG ward=new FrontierWard04OG();Assert.That(ward.Telling,Is.True);
            ward.Tick(1.2f,true,false);Assert.That(ward.Guarding,Is.True);
            ward.Tick(2.2f,true,false);Assert.That(ward.Guarding,Is.False);
            ward.Tick(3,true,false);Assert.That(ward.Telling,Is.True);
            ward.Tick(1.2f,true,false);ward.Tick(.1f,true,true);Assert.That(ward.Exposed,Is.EqualTo(2.8f));Assert.That(ward.Guarding,Is.False);
            ward.Tick(.5f,true,false);Assert.That(ward.Exposed,Is.EqualTo(2.3f).Within(.0001));
            ward.Tick(0,true,false);Assert.That(ward.Exposed,Is.EqualTo(2.3f).Within(.0001));
            ward.Tick(.1f,false,false);Assert.That(ward.Exposed,Is.Zero);Assert.That(ward.Clock,Is.Zero);
        }
        [TestCase(DamageType.Bullet,.45f)][TestCase(DamageType.Melee,.45f)][TestCase(DamageType.Power,.65f)]
        public void WardNeverMakesAnyOrdinaryHitInvulnerable(DamageType type,float factor)
        {
            Assert.That(FrontierCombat04OG.WardScale(true,true,false,false,1,type),Is.EqualTo(factor));
            Assert.That(FrontierCombat04OG.WardScale(true,false,false,false,1,type),Is.EqualTo(1),"Flank is open");
            Assert.That(FrontierCombat04OG.WardScale(true,true,false,true,1,type),Is.EqualTo(1),"Heavy breaks ward");
            Assert.That(FrontierCombat04OG.WardScale(true,true,false,false,22,type),Is.EqualTo(1));
            Assert.That(FrontierCombat04OG.WardScale(true,true,true,false,1,type),Is.EqualTo(1),"Broken original core stays open");
            Assert.That(FrontierCombat04OG.WardScale(false,true,false,false,1,type),Is.EqualTo(1));
        }
        [Test] public void TeamCannotBeginTwoAttacksAndDeadSlotsDoNotStall()
        {
            FrontierTurn04OG turn=new FrontierTurn04OG();bool[] live={true,true,true,false};turn.Tick(0,live);
            Assert.That(turn.CanBegin(0,0),Is.True);Assert.That(turn.Commit(0,0,1.5f),Is.True);
            Assert.That(turn.Commit(0,0,1.5f),Is.False);Assert.That(turn.CanBegin(1,1),Is.False);
            turn.Tick(1,live);Assert.That(turn.Slot,Is.EqualTo(-1));turn.Tick(2,live);Assert.That(turn.Slot,Is.EqualTo(1));
            live[1]=false;turn.Tick(2.1f,live);Assert.That(turn.Slot,Is.EqualTo(2));
            turn.Close();turn.Tick(99,live);Assert.That(turn.Slot,Is.EqualTo(-1));Assert.That(turn.Commit(2,99,1),Is.False);
        }
        [Test] public void UnavailableRangeOrStaggerCannotHoldOneTurnForever()
        {
            FrontierTurn04OG turn=new FrontierTurn04OG();bool[] live={true,true};turn.Tick(0,live);turn.Tick(2.3f,live);
            Assert.That(turn.Slot,Is.EqualTo(1));Assert.That(turn.Commit(1,2.3f,float.NaN),Is.False);
            Assert.That(turn.Commit(1,2.3f,100),Is.True);Assert.That(turn.LeaseUntil,Is.EqualTo(7.8f).Within(.001));
            turn.Tick(float.NaN,live);Assert.That(turn.Slot,Is.EqualTo(-1));live[0]=false;live[1]=false;turn.Tick(99,live);Assert.That(turn.Slot,Is.EqualTo(-1));
        }
        [Test] public void RolesHaveDifferentBoundedEngagementPositionsAndActualWardGeometry()
        {
            Vector3 home=new Vector3(-210,.15f,28),player=home+Vector3.back*12,caster=home+Vector3.forward*4;
            Vector3 guard=FrontierCombat04OG.Position(false,0,home,player,caster);
            Vector3 ranged=FrontierCombat04OG.Position(false,1,home,player,caster);
            Vector3 flank=FrontierCombat04OG.Position(true,2,home,player,caster);
            Assert.That(guard.z,Is.LessThan(caster.z));Assert.That(ranged,Is.Not.EqualTo(flank));
            for(int i=0;i<6;i++)Assert.That(MathUtil.FlatDistance(home,FrontierCombat04OG.Position(true,i,home,home+Vector3.left*200,caster)),Is.LessThanOrEqualTo(22.001f));
            MeshBuilder mesh=new MeshBuilder(1);FrontierCombat04OG.Shield(mesh);Assert.That(mesh.VertexCount,Is.GreaterThan(100));
            foreach(Vector3 vertex in mesh.Vertices)Assert.That(MathUtil.IsFinite(vertex),Is.True);
        }
        [Test] public void ForagingIsAnActualRoundTripAndPausesForAnObserver()
        {
            FrontierForage04OG cycle=new FrontierForage04OG();
            cycle.Tick(1,false,false,false,false);Assert.That(cycle.State,Is.EqualTo(FrontierForageState04OG.Travel));
            cycle.Tick(1,true,false,false,false);Assert.That(cycle.State,Is.EqualTo(FrontierForageState04OG.Feed));
            cycle.Tick(4,true,false,true,false);Assert.That(cycle.State,Is.EqualTo(FrontierForageState04OG.Watch));
            cycle.Tick(0,true,false,false,false);Assert.That(cycle.State,Is.EqualTo(FrontierForageState04OG.Watch));
            cycle.Tick(5,true,false,false,false);Assert.That(cycle.State,Is.EqualTo(FrontierForageState04OG.Return));
            cycle.Tick(1,false,true,false,false);Assert.That(cycle.State,Is.EqualTo(FrontierForageState04OG.Rest));
            cycle.Tick(10,false,true,false,true);Assert.That(cycle.State,Is.EqualTo(FrontierForageState04OG.Rest));
            cycle.Tick(1,false,true,false,false);Assert.That(cycle.State,Is.EqualTo(FrontierForageState04OG.Travel));
            cycle.Tick(1,false,false,false,true);Assert.That(cycle.State,Is.EqualTo(FrontierForageState04OG.Return));
        }
        [Test] public void RevisionSixCannotReadOrRewriteAnOlderBank()
        {
            Assert.That(SaveSystem.SlotFileName04OB(2,6),Is.EqualTo("lasveyra_frontier04og_save_2.json"));
            for(int revision=0;revision<=5;revision++)
            {Assert.That(SaveSystem.BankMatches04OC(revision,6),Is.False);Assert.That(SaveSystem.BankMatches04OC(6,revision),Is.False);}
            Assert.That(SaveSystem.BankMatches04OC(6,6),Is.True);Assert.That(new SaveData().WorldRevision04O,Is.Zero);
        }
        private static CityLayout World(int seed,int revision)
        {
            CityLayout layout=new CityLayout(seed);layout.Generate();
            List<VenueBinding04C> venues=InhabitedCityBindings04C.Select(layout,InhabitedCityBindings04CTests.ReadCatalog());
            Assert.That(layout.ApplyProfile04O((CityRevision04O)revision,venues),Is.True);return layout;
        }
        private static List<int> Triangles(MeshBuilder mesh)
        {return ((List<int>[])typeof(MeshBuilder).GetField("_tris",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(mesh))[0];}
        private static bool Intersects(Vector3 from,Vector3 to,Vector3 a,Vector3 b,Vector3 c)
        {
            Vector3 delta=to-from,e1=b-a,e2=c-a,p=Vector3.Cross(delta,e2);float det=Vector3.Dot(e1,p);
            if(Mathf.Abs(det)<.00001f)return false;
            float inv=1/det;Vector3 t=from-a;float u=Vector3.Dot(t,p)*inv;if(u<-.0001f || u>1.0001f)return false;
            Vector3 q=Vector3.Cross(t,e1);float v=Vector3.Dot(delta,q)*inv;if(v<-.0001f || u+v>1.0001f)return false;
            float distance=Vector3.Dot(e2,q)*inv;return distance>.00001f && distance<.99999f;
        }
        [TestCase(61724)] [TestCase(1)] [TestCase(2016)] [TestCase(77231)] [TestCase(92017)]
        public void FrontierRoutesHaveRealFloorsClearBodyCorridorsAndReachableAnchors(int seed)
        {
            CityLayout old=World(seed,5),world=World(seed,6);
            Assert.That(old.Frontier04OG,Is.False);Assert.That(old.RealmRoutes04OB.Any(r=>r.Id.StartsWith("04og/")),Is.False);
            Assert.That(old.ApplyProfile04O(CityRevision04O.LivingFrontier04OG,null),Is.False);
            Assert.That(old.CreatureSpots,Is.EqualTo(world.CreatureSpots));Assert.That(old.CrystalShardSpots,Is.EqualTo(world.CrystalShardSpots));
            for(int prior=0;prior<old.RealmRoutes04OB.Count;prior++)
                Assert.That(world.RealmRoutes04OB[prior].Points,Is.EqualTo(old.RealmRoutes04OB[prior].Points),"Preserve all old authored spans, including repeated IDs");
            MeshBuilder combined=new MeshBuilder(1);
            List<RealmRoute04OB> routes=world.RealmRoutes04OB.Where(r=>r.Id.StartsWith("04og/")).ToList();Assert.That(routes.Count,Is.EqualTo(7));
            foreach(RealmRoute04OB route in routes)FrontierGeometry04OG.Build(combined,combined,combined,route);
            List<int> indices=Triangles(combined);
            Assert.That(combined.Vertices.All(MathUtil.IsFinite),Is.True);
            foreach(RealmRoute04OB route in routes)for(int segment=1;segment<route.Points.Count;segment++)
            {
                Vector3 a=route.Points[segment-1],b=route.Points[segment],side=MathUtil.RightOf(MathUtil.Flat(b-a).normalized);
                Assert.That(Mathf.Abs(b.y-a.y)/Mathf.Max(.1f,MathUtil.Flat(b-a).magnitude),Is.LessThan(.18f),route.Id);
                Assert.That(world.ClearPassage04O(a,b,1.8f),Is.True,route.Id);
                // Inspect the actual emitted colliding triangles, not an ideal route graph or a mesh count.
                for(int step=0;step<=12;step++)for(int lane=-1;lane<=1;lane++)
                {
                    Vector3 point=Vector3.Lerp(a,b,step/12f)+side*lane*.85f;bool floor=false;
                    for(int i=0;i<indices.Count;i+=3)
                    {Vector3 x=combined.Vertices[indices[i]],y=combined.Vertices[indices[i+1]],z=combined.Vertices[indices[i+2]];
                        if(Vector3.Cross(y-x,z-x).y>.0001f && Intersects(point+Vector3.up*.08f,point-Vector3.up*.03f,x,y,z)){floor=true;break;}}
                    Assert.That(floor,Is.True,route.Id+" floor "+segment+"/"+step+" lane "+lane);
                }
                for(int lane=-1;lane<=1;lane++)foreach(float height in new[]{.45f,1.2f,1.9f})
                {
                    Vector3 from=a+side*lane*.85f+Vector3.up*height,to=b+side*lane*.85f+Vector3.up*height;
                    for(int i=0;i<indices.Count;i+=3)
                        if(Intersects(from,to,combined.Vertices[indices[i]],combined.Vertices[indices[i+1]],combined.Vertices[indices[i+2]]))Assert.Fail(route.Id+" body path "+segment+" at "+height+" triangle "+i);
                }
            }
            foreach(string token in new[]{"Realm:oath-seal","Realm:oath-witness","Realm:storm-crossing","Realm:storm-spring","Realm:storm-vein"})
            {Vector3 point;Assert.That(world.ResolveRealmAnchor04OC(token,out point),Is.True);Assert.That(old.ResolveRealmAnchor04OC(token,out point),Is.False);world.ResolveRealmAnchor04OC(token,out point);Assert.That(world.FindWalkRoute04O(world.FindPoi("Veyra Lofts").Position,point).Count,Is.GreaterThan(1),token);}
        }
        [Test] public void BothInvestigationsUseOriginalRunnerPhysicalInteractionsAndFiniteRewards()
        {
            List<string> errors=new List<string>();QuestJsonTrace trace=new QuestJsonTrace();
            List<KeyValuePair<string,QuestFile>> quests=QuestTestData.Load<QuestFile>("Quests",errors,trace);
            List<KeyValuePair<string,EventFile>> events=QuestTestData.Load<EventFile>("Events",errors,trace);
            QuestReport report=QuestValidator.ValidateAll(quests,events,QuestRefs.Build(World(61724,6)),trace);
            Assert.That(errors,Is.Empty);Assert.That(report.Errors,Is.Empty,report.Summary());
            QuestFile file=quests.First(p=>p.Key.Contains("living_frontier_04og")).Value;
            Assert.That(file.Quests.Count,Is.EqualTo(2));
            foreach(QuestDef quest in file.Quests)
            {Assert.That(quest.Requires.WorldRevisions,Is.EqualTo(new[]{6}));Assert.That(quest.Requires.Quests.Count,Is.EqualTo(1));Assert.That(quest.Phases.SelectMany(p=>p.Steps).Count(s=>s.Type=="Interact"),Is.GreaterThanOrEqualTo(2));Assert.That(quest.Reward.Cash,Is.EqualTo(120));Assert.That(quest.Reward.Materials[0].Count,Is.EqualTo(2));}
        }
        [TestCase(1,1,1,true)] [TestCase(-1,1,1,false)] [TestCase(1,2,1,false)] [TestCase(0,0,0,false)] [TestCase(.01f,.01f,.01f,true)]
        public void ImportedSkinRejectsMirroredCollapsedAndShearedTransforms(float x,float y,float z,bool valid)
        {Assert.That(ImportedModelRegistry04OD.ValidLocalScale04OG(new Vector3(x,y,z)),Is.EqualTo(valid));Assert.That(ImportedModelRegistry04OD.ValidLocalScale04OG(new Vector3(float.NaN,1,1)),Is.False);}
        [Test] public void NewPreviewGuidanceAndEvidenceDoNotConflateRequestedWithRun()
        {
            Assert.That(CinematicAcceptance04OF.ProfileWarning(6),Does.Contain("Isolated frontier bank"));
            Assert.That(CinematicAcceptance04OF.ProfileWarning(5),Does.Contain("existing Unbound"));
            Assert.That(CinematicAcceptance04OF.SafeEvidenceFile("lasveyra_frontier04og_save_0.json"),Is.False);
            Assert.That(CinematicAcceptance04OF.Checklist().Length,Is.EqualTo(21));
        }
        [Test] public void FormationCannotReopenAfterEventCleanupAndRoleMovesAreIndependent()
        {
            FrontierFormation04OG formation=new FrontierFormation04OG(new List<CharacterBase>(),Vector3.zero,false);
            Assert.That(formation.CanBegin(0),Is.False);formation.Activate();Assert.That(formation.Active,Is.True);
            formation.Close();formation.Activate();Assert.That(formation.Active,Is.False);Assert.That(formation.CanBegin(0),Is.False);
            CreatureAttack[] leader=FrontierCombat04OG.AzureRoles(0),flanker=FrontierCombat04OG.AzureRoles(2);
            Assert.That(leader[0].Windup,Is.GreaterThan(flanker[0].Windup));Assert.That(leader[0].MaxRange,Is.GreaterThan(flanker[0].MaxRange));
            flanker[0].Damage=999;Assert.That(FrontierCombat04OG.AzureRoles(2)[0].Damage,Is.LessThan(1));
        }
        [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)]
        public void SovereignRootsAreActualIndependentWeightedRigAttachments(int index)
        {
            MeshBuilder wood=new MeshBuilder(1),crystal=new MeshBuilder(1);FrontierCombat04OG.RootFan(wood,crystal,index);
            Assert.That(wood.VertexCount,Is.GreaterThan(40));Assert.That(crystal.VertexCount,Is.GreaterThan(20));
            Assert.That(wood.Vertices.All(MathUtil.IsFinite),Is.True);Assert.That(wood.Vertices.Min(v=>v.y),Is.LessThan(-.6f));
        }
    }
}
