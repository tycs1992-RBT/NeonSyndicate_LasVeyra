using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
namespace NeonSyndicate.Tests
{
    public sealed class KingdomWorld04NTests
    {
        private static void Graph(out List<WalkNode> nodes,out List<WalkLink> links)
        {
            nodes=new List<WalkNode>();for(int i=0;i<4;i++)nodes.Add(new WalkNode{Id=i,Pos=new Vector3(i*10,0,0)});
            links=new List<WalkLink>();for(int i=0;i<3;i++){links.Add(new WalkLink{Id=i,A=i,B=i+1,Crosswalk=i==1});nodes[i].Links.Add(i);nodes[i+1].Links.Add(i);}
        }
        [Test] public void CommuteUsesExistingLinksInBothDirectionsAndCanWaitAtHome()
        {
            List<WalkNode> n;List<WalkLink> l;Graph(out n,out l);
            Assert.AreEqual(0,CivilianCommute04N.FirstLink(n,l,0,3));Assert.AreEqual(2,CivilianCommute04N.FirstLink(n,l,3,0));
            Assert.AreEqual(-2,CivilianCommute04N.FirstLink(n,l,3,3));Assert.IsTrue(l[1].Crosswalk);
        }
        [Test] public void CommutesRejectUnreachableFloorsRadiusAndSearchOverflow()
        {
            List<WalkNode> n;List<WalkLink> l;Graph(out n,out l);
            Assert.AreEqual(-1,CivilianCommute04N.FirstLink(n,l,0,3,1));Assert.AreEqual(-1,CivilianCommute04N.FirstLink(n,l,0,3,128,15));
            n[2].Pos+=Vector3.up;Assert.AreEqual(-1,CivilianCommute04N.FirstLink(n,l,0,3));Assert.AreEqual(-1,CivilianCommute04N.FirstLink(n,l,-1,3));
        }
        [Test] public void HomewardTravelRespondsToRestWeatherAndLocalDanger()
        {Assert.IsTrue(CivilianCommute04N.GoHome(23,0,false,false));Assert.IsTrue(CivilianCommute04N.GoHome(10,0,true,false));Assert.IsTrue(CivilianCommute04N.GoHome(10,0,false,true));Assert.IsFalse(CivilianCommute04N.GoHome(10,0,false,false));}
        [Test] public void GardenCircuitRequiresPhysicalStationsInTheCluedOrderAndResetsSafely()
        {
            var p=new GardenCircuit04N();Assert.IsFalse(p.Touch(1));Assert.AreEqual(0,p.Step);Assert.IsFalse(p.Touch(0));Assert.AreEqual(1,p.Step);
            Assert.IsFalse(p.Touch(1));Assert.AreEqual(0,p.Step);Assert.IsFalse(p.Touch(0));Assert.IsFalse(p.Touch(2));Assert.IsTrue(p.Touch(1));
            Assert.AreEqual(0,p.Step);Assert.IsFalse(p.Touch(1));p.Touch(0);p.Reset();Assert.AreEqual(0,p.Step);Assert.IsFalse(p.Touch(999));
        }
        [TestCase(false)] [TestCase(true)] public void UnderheartGeometryKeepsEveryNewPartAboveTheWalkingVolume(bool workshop)
        {
            var a=new MeshBuilder(1);var b=new MeshBuilder(1);var c=new MeshBuilder(1);
            UnderheartKingdom04N.Room(a,b,c,new RectF(-12,-12,12,12),-50,8,workshop);
            Assert.That(a.VertexCount+b.VertexCount+c.VertexCount,Is.GreaterThan(400).And.LessThan(2500));
            foreach(var mesh in new[]{a,b,c})foreach(var v in mesh.Vertices){Assert.That(v.y,Is.GreaterThan(-47.3f));Assert.That(Mathf.Abs(v.x),Is.GreaterThan(10.5f));}
        }
        [Test] public void ThreeWildernessLandmarksUseDistinctBoundedGeometry()
        {
            var kinds=new HashSet<string>();for(int i=0;i<3;i++){var a=new MeshBuilder(1);var b=new MeshBuilder(1);UnderheartKingdom04N.Waystone(a,b,i);kinds.Add(string.Join(";",a.Vertices));foreach(var v in a.Vertices)Assert.That(v.y,Is.InRange(-.2f,4f));}Assert.AreEqual(3,kinds.Count);
        }
        [Test] public void EverySyndicateAidUsesOrdinaryMaterialsAndPassageNeverLowersAStoryFloor()
        {
            for(int i=0;i<8;i++){Assert.IsNotNull(MaterialDb.Get(NeighborhoodRules04N.Material(i)));Assert.AreEqual(.85f,NeighborhoodRules04N.PassageHeat(.88f,.85f));}
            Assert.AreEqual(.3f,NeighborhoodRules04N.PassageHeat(.4f,0),.0001f);
        }
        [TestCase(HumanoidCreature.GolemForeman)] [TestCase(HumanoidCreature.MirageDjinn)]
        public void NamedBossCounterplayOnlyRewardsARealInterruptedTell(HumanoidCreature kind)
        {Assert.IsTrue(KingdomCombatRules04N.ExposeInterruptedBoss(kind,true,true));Assert.IsFalse(KingdomCombatRules04N.ExposeInterruptedBoss(kind,false,true));Assert.IsFalse(KingdomCombatRules04N.ExposeInterruptedBoss(kind,true,false));}
        [Test] public void CompanionPlanningMovementAndRecallCanShareVehicleShellAndDebrisClearance()
        {int mask=KingdomCombatRules04N.CompanionObstacleMask;foreach(int layer in new[]{Layers.World,Layers.Vehicles,Layers.VehicleShell,Layers.Debris})Assert.AreNotEqual(0,mask&(1<<layer));Assert.AreEqual(0,mask&(1<<Layers.Characters));}
        [Test] public void FourNewGuildRequestsGeneratePhysicalObjectivesThroughTheOriginalQuestOwner()
        {
            var errors=new List<string>();var files=QuestTestData.Load<QuestFile>("Quests",errors,new QuestJsonTrace());Assert.That(errors,Is.Empty);
            int count=0;var guilds=new HashSet<string>();
            foreach(var file in files)foreach(var contract in file.Value.Contracts)
            {
                if(!contract.Id.StartsWith("c_04n_"))continue;count++;guilds.Add(contract.Guild);
                var faction=FactionDb.Get(contract.Guild);Assert.IsNotNull(faction);Vector3 at;
                Assert.IsTrue(QuestPlaces.GiverPosition(TestData.City,faction.Contact,0,0,out at));
                foreach(var species in contract.Species)Assert.IsNotNull(CreatureSpeciesDb.Get(species));
                var job=QuestLogic.MakeContract(contract,TestData.City,at,"Guild board",77,0);Assert.IsNotNull(job);
                Assert.That(job.Quest.Phases[0].Steps.Count,Is.GreaterThanOrEqualTo(2));Assert.That(job.Reward.Rep.Count,Is.GreaterThan(0));
            }
            Assert.AreEqual(4,count);Assert.AreEqual(4,guilds.Count);
        }
    }
}
