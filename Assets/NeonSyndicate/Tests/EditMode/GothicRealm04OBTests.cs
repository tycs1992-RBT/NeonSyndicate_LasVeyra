using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
namespace NeonSyndicate.Tests
{
    public sealed class GothicRealm04OBTests
    {
        private static CityLayout World(int seed,out List<VenueBinding04C> venues)
        {
            CityLayout layout=new CityLayout(seed);layout.Generate();venues=InhabitedCityBindings04C.Select(layout,InhabitedCityBindings04CTests.ReadCatalog());
            Assert.That(layout.ApplyProfile04O(CityRevision04O.GothicRealm04OB,venues),Is.True);return layout;
        }
        private static float Length(IList<Vector3> path){float n=0f;for(int i=1;i<path.Count;i++)n+=Vector3.Distance(path[i-1],path[i]);return n;}
        [TestCase(61724)][TestCase(1)][TestCase(2016)][TestCase(77231)][TestCase(92017)]
        public void RevisionTwoPreservesCanonicalIdentitiesAndOrderedReceipts(int seed)
        {
            CityLayout old=new CityLayout(seed);old.Generate();List<VenueBinding04C> venues=InhabitedCityBindings04C.Select(old,InhabitedCityBindings04CTests.ReadCatalog());
            string pois=string.Join(";",old.Pois.Select(p=>p.Id+"/"+p.Name+"/"+p.Type+"/"+p.District));
            string sites=string.Join(";",old.Sites.Select(p=>p.Key+"/"+p.Name));
            int[] counts={old.DataChipSpots.Count,old.CrystalShardSpots.Count,old.SpellStoneSpots.Count,old.CreatureSpots.Count,old.PickupSpots.Count,old.StuntJumps.Count};
            Building[] hosts=venues.Select(v=>v.Building).ToArray();
            Assert.That(old.ApplyProfile04O(CityRevision04O.GothicRealm04OB,venues),Is.True,string.Join(";",venues.Select(v=>v.Definition.Id+" bound="+(v.Building!=null)+" lot="+(v.Lot!=null))));
            Assert.That(string.Join(";",old.Pois.Select(p=>p.Id+"/"+p.Name+"/"+p.Type+"/"+p.District)),Is.EqualTo(pois));
            Assert.That(string.Join(";",old.Sites.Select(p=>p.Key+"/"+p.Name)),Is.EqualTo(sites));
            Assert.That(new[]{old.DataChipSpots.Count,old.CrystalShardSpots.Count,old.SpellStoneSpots.Count,old.CreatureSpots.Count,old.PickupSpots.Count,old.StuntJumps.Count},Is.EqualTo(counts));
            for(int i=0;i<hosts.Length;i++)
            {
                if(hosts[i]!=null)Assert.That(venues[i].Building,Is.SameAs(hosts[i]));
                Assert.That(venues.Count(v=>v.Building==venues[i].Building),Is.EqualTo(1));
                Assert.That(old.IsInsideBuilding(venues[i].Outside,.8f),Is.False,venues[i].Definition.Id);
                if(venues[i].Poi!=null)Assert.That(old.Blocks[venues[i].Poi.BlockId].Lots,Does.Contain(venues[i].Lot));
            }
            List<VenueBinding04C> other;CityLayout again=World(seed,out other);
            Assert.That(string.Join(";",old.Blocks.SelectMany(b=>b.Lots).SelectMany(l=>l.Buildings).Select(b=>b.Seed+"/"+b.Footprint+"/"+b.Height)),
                Is.EqualTo(string.Join(";",again.Blocks.SelectMany(b=>b.Lots).SelectMany(l=>l.Buildings).Select(b=>b.Seed+"/"+b.Footprint+"/"+b.Height))));
        }
        [Test] public void EveryOriginalServiceJoinsThePhysicalCourtNetwork()
        {
            List<VenueBinding04C> venues;CityLayout l=World(61724,out venues);Vector3 home=venues.Find(v=>v.Definition.Id=="lofts").Outside;
            foreach(VenueBinding04C venue in venues)Assert.That(l.FindWalkRoute04O(home,venue.Outside),Is.Not.Empty,venue.Definition.Id);
            foreach(RealmRoute04OB route in l.RealmRoutes04OB)for(int i=1;i<route.Points.Count;i++)
                Assert.That(l.ClearPassage04O(route.Points[i-1],route.Points[i],1.8f),Is.True,route.Id+" segment "+i);
            Assert.That(l.FindWalkRoute04O(home,l.RealmThreshold04OB),Is.Not.Empty);
        }
        [Test] public void CompressionMeasuresActualRecurringRoutesWithoutChangingMovement()
        {
            CityLayout legacy=new CityLayout(61724);legacy.Generate();List<VenueBinding04C> before=InhabitedCityBindings04C.Select(legacy,InhabitedCityBindings04CTests.ReadCatalog());
            List<VenueBinding04C> after;CityLayout compact=World(61724,out after);
            foreach(string id in new[]{"clinic","forge","garage"})
            {
                List<Vector3> a=legacy.FindWalkRoute04O(before.Find(v=>v.Definition.Id=="lofts").Outside,before.Find(v=>v.Definition.Id==id).Outside);
                List<Vector3> b=compact.FindWalkRoute04O(after.Find(v=>v.Definition.Id=="lofts").Outside,after.Find(v=>v.Definition.Id==id).Outside);
                Assert.That(a,Is.Not.Empty,id);Assert.That(b,Is.Not.Empty,id);Assert.That(Length(b),Is.LessThan(Length(a)*.6f),id);
            }
            Assert.That(compact.Edges.Count,Is.LessThan(legacy.Edges.Count*.5f));
        }
        [Test] public void RevisionsCannotBeMutatedIntoAnotherSavedGeography()
        {
            Assert.That(new SaveData().WorldRevision04O,Is.Zero);
            foreach(int known in new[]{0,1,2})Assert.That(CityRebirthRules04O.Known(known),Is.True);
            Assert.That(CityRebirthRules04O.Known(3),Is.False);
            CityLayout l=new CityLayout(61724);l.Generate();List<VenueBinding04C> v=InhabitedCityBindings04C.Select(l,InhabitedCityBindings04CTests.ReadCatalog());
            Assert.That(l.ApplyProfile04O(CityRevision04O.GothicRealm04OB,null),Is.False);
            Assert.That(l.ApplyProfile04O(CityRevision04O.Crystalline04O,v),Is.True);
            Assert.That(l.ApplyProfile04O(CityRevision04O.GothicRealm04OB,v),Is.False);
            List<VenueBinding04C> other;CityLayout compact=World(61724,out other);
            Assert.That(compact.ApplyProfile04O(CityRevision04O.GothicRealm04OB,other),Is.True);
            Assert.That(compact.ApplyProfile04O(CityRevision04O.Legacy04N,other),Is.False);
        }
        [Test] public void VehicleGraphHasValidConnectionsAndRequiredTerrainCrossing()
        {
            List<VenueBinding04C> venues;CityLayout l=World(61724,out venues);
            foreach(RoadEdge e in l.Edges){Assert.That(e.Id,Is.InRange(0,l.Edges.Count-1));Assert.That(l.Nodes[e.A].Edges,Does.Contain(e.Id));Assert.That(l.Nodes[e.B].Edges,Does.Contain(e.Id));}
            foreach(LanePath lane in l.Lanes)
            {Assert.That(lane.Next,Is.Not.Empty,"lane "+lane.Id);foreach(int id in lane.Next)Assert.That(id,Is.InRange(0,l.Lanes.Count-1));}
            Assert.That(l.Edges.Any(e=>e.Name=="Crystal realm approach" && e.HasBridge),Is.True);
        }
        [TestCase(1)][TestCase(2)][TestCase(3)][TestCase(4)][TestCase(5)][TestCase(6)]
        public void FamilySolidsHaveFiniteOutwardGeometryAndBoundedBudgets(int family)
        {
            Building b=new Building{Seed=61724,Footprint=new RectF(-11,0,11,26),Height=GothicRealmGeometry04OB.FamilyHeight(family),GothicFamily04OB=family};
            MeshBuilder a=new MeshBuilder(1),t=new MeshBuilder(1),c=new MeshBuilder(1);GothicRealmGeometry04OB.Family(a,t,c,b,true);
            Assert.That(a.VertexCount+t.VertexCount+c.VertexCount,Is.InRange(300,15000));
            foreach(MeshBuilder m in new[]{a,t,c})CheckMesh(m);
            // Unlike a solid frontal box, hall/cloister galleries leave real eye-level empty central space.
            if(family==1||family==2)Assert.That(HitTriangle(a,new Vector3(0,2f,-2),Vector3.forward,15f),Is.False);
        }
        [Test] public void ClosedVaultDoesNotFillItsPlayerAndCompanionAperture()
        {
            MeshBuilder arch=new MeshBuilder(1);GothicRealmGeometry04OB.Arch(arch,Vector3.zero,0f,7f,4f,4f,.6f,2f,16);
            CheckMesh(arch);Assert.That(HitTriangle(arch,new Vector3(0,1.7f,-2),Vector3.forward,6f),Is.False);
            Assert.That(HitTriangle(arch,new Vector3(3.8f,1.7f,-2),Vector3.forward,6f),Is.True);
            Assert.That(HitTriangle(arch,new Vector3(0,5f,1),Vector3.up,5f),Is.True);
        }
        [Test] public void SanctuaryRetainsEveryOriginalAnchorAndAddsAUsableDeepDestination()
        {
            List<string> errors=new List<string>();string path="Assets/NeonSyndicate/Resources/InhabitedCity04C/Layouts/archive.json";
            VenueLayout04C source=VenueLayoutData04E.Layout("archive");
            VenueExpanded04C expanded=InhabitedCity04CComposition.Expand(source,30f,48f,22f);
            string[] ids=source.anchors.Select(a=>a.id).ToArray();float old=source.anchors.First(a=>a.id=="record3").position[2];
            GothicRoomComposition04OB.Prepare(expanded);
            Assert.That(expanded.anchors.Select(a=>a.id),Is.EqualTo(ids));Assert.That(source.anchors.First(a=>a.id=="record3").position[2],Is.EqualTo(old));
            Assert.That(expanded.anchors.First(a=>a.id=="record3").position,Is.EqualTo(new[]{0f,1.2f,43f}));
            Assert.That(expanded.roomDepth,Is.EqualTo(48f));Assert.That(expanded.nodes.Any(n=>n.kind=="upper04A"||n.id=="rear_room_wall"),Is.False);
        }
        [TestCase(61724)][TestCase(1)][TestCase(2016)][TestCase(77231)][TestCase(92017)]
        public void TheOriginalUnderheartPlannerFitsTheRelocatedPressureFrame(int seed)
        {
            List<VenueBinding04C> venues;CityLayout l=World(seed,out venues);VenueBinding04C pressure=venues.Find(v=>v.Definition.Id==VenueIds04E.Pressure);
            UnderheartPlanner.Choice choice=UnderheartPlanner.Choose(l,new UnderheartFrame(pressure.Origin.x,pressure.Origin.z,pressure.Yaw),true);
            Assert.That(choice.Layout,Is.Not.Null,string.Join(";",choice.Rejected));
            Assert.That(UnderheartRules.Validate(choice.Layout),Is.Empty);
            Assert.That(choice.Layout.Anchor("service.clamp_hollow"),Is.Not.Null);
        }
        [Test] public void SharedBatchAppendPreservesOffsetTriangleIndices()
        {
            MeshBuilder a=new MeshBuilder(1),b=new MeshBuilder(1);GothicRealmGeometry04OB.Box(a,Vector3.zero,0,Vector3.zero,Vector3.one);
            GothicRealmGeometry04OB.Crystal(b,new Vector3(5,0,2),new Vector3(2,4,2),.2f,1);
            int count=a.VertexCount;int[] expected=Indices(b).Select(i=>i+count).ToArray();a.Append(b);
            Assert.That(Indices(a).Skip(36),Is.EqualTo(expected));CheckMesh(a);
        }
        private static List<int> Indices(MeshBuilder m){return ((List<int>[])typeof(MeshBuilder).GetField("_tris",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(m))[0];}
        private static void CheckMesh(MeshBuilder m)
        {
            List<int> t=Indices(m);Assert.That(t.Count%3,Is.Zero);
            foreach(Vector3 p in m.Vertices)Assert.That(MathUtil.IsFinite(p),Is.True);
            for(int i=0;i<t.Count;i+=3)
            {
                Vector3 a=m.Vertices[t[i]],b=m.Vertices[t[i+1]],c=m.Vertices[t[i+2]];
                Assert.That(Vector3.Cross(b-a,c-a).sqrMagnitude,Is.GreaterThan(1e-10f));
                Assert.That(Vector3.Dot(Vector3.Cross(b-a,c-a).normalized,m.Normals[t[i]]),Is.GreaterThan(.98f));
            }
        }
        private static bool HitTriangle(MeshBuilder m,Vector3 origin,Vector3 direction,float range)
        {
            List<int> t=Indices(m);
            for(int i=0;i<t.Count;i+=3)
            {
                Vector3 a=m.Vertices[t[i]],e1=m.Vertices[t[i+1]]-a,e2=m.Vertices[t[i+2]]-a;
                Vector3 h=Vector3.Cross(direction,e2);float determinant=Vector3.Dot(e1,h);if(Mathf.Abs(determinant)<1e-7f)continue;
                float f=1f/determinant;Vector3 s=origin-a;float u=f*Vector3.Dot(s,h);if(u<0||u>1)continue;
                Vector3 q=Vector3.Cross(s,e1);float v=f*Vector3.Dot(direction,q);if(v<0||u+v>1)continue;
                float distance=f*Vector3.Dot(e2,q);if(distance>=0 && distance<=range)return true;
            }
            return false;
        }
    }
}
