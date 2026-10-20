using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace NeonSyndicate.Tests
{
    public sealed class CityRebirth04OTests
    {
        public static CityLayout Generate(int seed,bool revised)
        {
            CityLayout layout=new CityLayout(seed);layout.Generate();
            if(revised) Assert.That(layout.ApplyProfile04O(CityRevision04O.Crystalline04O,InhabitedCityBindings04C.Select(layout,InhabitedCityBindings04CTests.ReadCatalog())),Is.True);
            return layout;
        }
        private static string Signature(CityLayout layout)
        {
            return string.Join(";",layout.Blocks.SelectMany(b=>b.Lots).SelectMany(l=>l.Buildings).Select(b=>b.Seed+"/"+b.Footprint+"/"+b.Height+"/"+b.RebirthFamily04O))+
                "|"+string.Join(";",layout.Passages04O.Select(p=>p.BlockId+"/"+p.A+"/"+p.B));
        }
        [Test] public void RevisionFailsClosedWithoutServiceBindings()
        {
            CityLayout layout=Generate(61724,false);string before=Signature(layout);
            Assert.That(layout.ApplyProfile04O(CityRevision04O.Crystalline04O,null),Is.False);
            Assert.That(layout.ApplyProfile04O((CityRevision04O)99,new List<VenueBinding04C>()),Is.False);
            Assert.That(Signature(layout),Is.EqualTo(before));Assert.That(layout.Revision04O,Is.EqualTo(CityRevision04O.Legacy04N));
            Assert.That(new SaveData().WorldRevision04O,Is.Zero);Assert.That(CityRebirthRules04O.Known(99),Is.False);
        }
        [TestCase(61724)][TestCase(1)][TestCase(2016)][TestCase(77231)][TestCase(92017)]
        public void RevisedWorldIsDeterministicAndPreservesEveryServiceAndSavedSpatialReceipt(int seed)
        {
            CityLayout layout=Generate(seed,false);VenueCatalog04C catalog=InhabitedCityBindings04CTests.ReadCatalog();
            List<VenueBinding04C> venues=InhabitedCityBindings04C.Select(layout,catalog);
            string pois=string.Join(";",layout.Pois.Select(p=>p.Name+"/"+p.Position+"/"+p.Facing+"/"+p.Type));
            string sites=string.Join(";",layout.Sites.Select(p=>p.Key+"/"+p.Position));
            string lanes=string.Join(";",layout.Lanes.Select(p=>p.Id+"/"+p.Length+"/"+string.Join(",",p.Next)));
            List<Vector3>[] points={layout.DataChipSpots,layout.CrystalShardSpots,layout.SpellStoneSpots,layout.CreatureSpots,layout.PickupSpots,layout.ParkingSpots};
            string saved=string.Join(";",points.Select(p=>string.Join(",",p)));
            Assert.That(layout.ApplyProfile04O(CityRevision04O.Crystalline04O,venues),Is.True);
            Assert.That(string.Join(";",layout.Pois.Select(p=>p.Name+"/"+p.Position+"/"+p.Facing+"/"+p.Type)),Is.EqualTo(pois));
            Assert.That(string.Join(";",layout.Sites.Select(p=>p.Key+"/"+p.Position)),Is.EqualTo(sites));
            Assert.That(string.Join(";",layout.Lanes.Select(p=>p.Id+"/"+p.Length+"/"+string.Join(",",p.Next))),Is.EqualTo(lanes));
            Assert.That(string.Join(";",points.Select(p=>string.Join(",",p))),Is.EqualTo(saved));
            List<VenueBinding04C> after=InhabitedCityBindings04C.Select(layout,catalog);
            for(int i=0;i<venues.Count;i++)
            {
                Assert.That(after[i].Building,Is.SameAs(venues[i].Building),venues[i].Definition.Id);
                Assert.That(after[i].Outside,Is.EqualTo(venues[i].Outside),venues[i].Definition.Id);
            }
            Assert.That(layout.RebuiltLots04O,Is.GreaterThan(50));
            Assert.That(layout.RevisedBuiltArea04O,Is.LessThan(layout.OriginalBuiltArea04O*.8f));
            Assert.That(Signature(layout),Is.EqualTo(Signature(Generate(seed,true))));
            string stable=Signature(layout);Assert.That(layout.ApplyProfile04O(CityRevision04O.Crystalline04O,venues),Is.True);
            Assert.That(Signature(layout),Is.EqualTo(stable),"Repeated application cannot erase parcels twice.");
            Assert.That(layout.ApplyProfile04O(CityRevision04O.Legacy04N,venues),Is.False,"Changing back requires the original scene, never an inverse mutation.");
        }
        [TestCase(61724)][TestCase(77231)][TestCase(2016)]
        public void GroundPassagesHavePhysicalClearanceAndJoinTheOriginalWalkGraph(int seed)
        {
            CityLayout layout=Generate(seed,true);Assert.That(layout.Passages04O.Count,Is.GreaterThan(0));
            foreach(CityPassage04O passage in layout.Passages04O)
            {
                Assert.That(layout.ClearPassage04O(passage.A,passage.B,2.8f),Is.True);
                List<Vector3> route=layout.FindWalkRoute04O(passage.A,passage.B);
                Assert.That(route.Count,Is.GreaterThanOrEqualTo(2));
                Assert.That(Length(route),Is.LessThanOrEqualTo(Vector3.Distance(passage.A,passage.B)+.1f));
            }
        }
        [Test] public void HeroServicesKeepWalkingConnectionsAndFourDistinctSpineAnchors()
        {
            CityLayout layout=Generate(61724,true);List<VenueBinding04C> venues=InhabitedCityBindings04C.Select(layout,InhabitedCityBindings04CTests.ReadCatalog());
            VenueBinding04C home=venues.Find(v=>v.Definition.Id=="lofts");
            foreach(string id in new[]{VenueIds04E.Dispatch,VenueIds04E.Pressure,"clinic","forge"})
            {
                VenueBinding04C venue=venues.Find(v=>v.Definition.Id==id);Assert.That(venue,Is.Not.Null);
                Assert.That(layout.FindWalkRoute04O(home.Outside,venue.Outside),Is.Not.Empty,id);
            }
            Assert.That(layout.Spine04O,Has.Count.EqualTo(4));
            Assert.That(layout.Spine04O.Select(p=>p.Id).Distinct().Count(),Is.EqualTo(4));
            foreach(SpineSite04O site in layout.Spine04O)Assert.That(site.Height,Is.GreaterThan(30f));
        }
        [TestCase(1)][TestCase(2)][TestCase(3)][TestCase(4)][TestCase(5)][TestCase(6)]
        public void StructuralBodiesAndCrystalsHaveFiniteBoundedGeometry(int family)
        {
            Building building=new Building{Footprint=new RectF(-9,-12,9,12),Height=CityRebirthRules04O.Height((MassingFamily04O)family,61724,0),RebirthFamily04O=family};
            List<CityMassPiece04O> solids=CityRebirthGeometry04O.Solids(building);Assert.That(solids.Count,Is.InRange(2,6));
            foreach(CityMassPiece04O solid in solids)
            {
                Assert.That(solid.Size.x>0f && solid.Size.y>0f && solid.Size.z>0f,Is.True);
                Assert.That(solid.Center.y-solid.Size.y*.5f,Is.GreaterThanOrEqualTo(CityLayout.CurbHeight-.001f));
                Assert.That(solid.Center.y+solid.Size.y*.5f,Is.LessThanOrEqualTo(building.Height+CityLayout.CurbHeight+.001f));
            }
            MeshBuilder a=new MeshBuilder(1),b=new MeshBuilder(1),c=new MeshBuilder(1);
            CityRebirthGeometry04O.Building(a,b,c,building,true);
            Assert.That(a.VertexCount+b.VertexCount+c.VertexCount,Is.InRange(100,4000));
            foreach(Vector3 vertex in a.Vertices.Concat(b.Vertices).Concat(c.Vertices))Assert.That(MathUtil.IsFinite(vertex),Is.True);
            MeshBuilder far=new MeshBuilder(1),farB=new MeshBuilder(1),farC=new MeshBuilder(1);
            CityRebirthGeometry04O.Building(far,farB,farC,building,false);
            Assert.That(far.VertexCount+farB.VertexCount+farC.VertexCount,Is.LessThan(a.VertexCount+b.VertexCount+c.VertexCount));
        }
        [Test] public void SixFamiliesAreDifferentSolidsNotPaletteSwaps()
        {
            HashSet<string> shapes=new HashSet<string>();
            for(int i=1;i<=6;i++)
            {
                Building building=new Building{Footprint=new RectF(-9,-12,9,12),Height=24f,RebirthFamily04O=i};
                shapes.Add(string.Join(";",CityRebirthGeometry04O.Solids(building).Select(p=>p.Center+"/"+p.Size)));
            }
            Assert.That(shapes.Count,Is.EqualTo(6));
        }
        [Test] public void UndergroundAndUnknownEndpointsNeverReceiveAnInventedSurfaceShortcut()
        {
            CityLayout layout=Generate(61724,true);
            Assert.That(layout.FindWalkRoute04O(new Vector3(0,-50,0),Vector3.zero),Is.Empty);
            Assert.That(layout.FindWalkRoute04O(new Vector3(5000,0,5000),Vector3.zero),Is.Empty);
        }
        [Test] public void MineralPrismsAreClosedAndTheirTrianglesFaceTheirNormals()
        {
            MeshBuilder mesh=new MeshBuilder(1);CityRebirthGeometry04O.Shard(mesh,Vector3.zero,new Vector3(5f,40f,3f),.08f);
            List<int>[] tris=(List<int>[])typeof(MeshBuilder).GetField("_tris",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(mesh);
            Dictionary<string,int> edges=new Dictionary<string,int>();
            List<int> indices=tris[0];
            for(int i=0;i<indices.Count;i+=3)
            {
                Vector3 a=mesh.Vertices[indices[i]],b=mesh.Vertices[indices[i+1]],c=mesh.Vertices[indices[i+2]];
                Assert.That(Vector3.Dot(Vector3.Cross(b-a,c-a).normalized,mesh.Normals[indices[i]]),Is.GreaterThan(.99f));
                for(int n=0;n<3;n++)
                {
                    string x=mesh.Vertices[indices[i+n]].ToString("F4"),y=mesh.Vertices[indices[i+(n+1)%3]].ToString("F4");
                    string edge=string.CompareOrdinal(x,y)<0?x+"/"+y:y+"/"+x;
                    int count;edges.TryGetValue(edge,out count);edges[edge]=count+1;
                }
            }
            Assert.That(edges.Values.All(v=>v==2),Is.True,"Every mineral surface edge is paired, including the underside collider cap.");
        }
        [Test] public void FringeUsesSmallLowSolidsAndCannotBecomeAnotherTallBox()
        {
            Building ruin=new Building{Footprint=new RectF(0,0,10,9),Height=3.2f,RebirthFamily04O=7};
            List<CityMassPiece04O> solids=CityRebirthGeometry04O.Solids(ruin);
            Assert.That(solids.Count,Is.EqualTo(2));
            Assert.That(solids.All(p=>p.Size.y>0 && p.Size.y<=3.2f && MathUtil.IsFinite(p.Center)),Is.True);
            MeshBuilder a=new MeshBuilder(1),b=new MeshBuilder(1),c=new MeshBuilder(1);CityRebirthGeometry04O.Building(a,b,c,ruin,true);
            Assert.That(a.VertexCount+b.VertexCount+c.VertexCount,Is.InRange(50,500));
        }
        [Test] public void ProfileRejectsForeignOrIncompleteServiceInventoryBeforeMutation()
        {
            CityLayout layout=Generate(61724,false);string before=Signature(layout);
            List<VenueBinding04C> partial=InhabitedCityBindings04C.Select(layout,InhabitedCityBindings04CTests.ReadCatalog());
            partial.RemoveAt(0);Assert.That(layout.ApplyProfile04O(CityRevision04O.Crystalline04O,partial),Is.False);
            CityLayout foreign=Generate(1,false);
            Assert.That(layout.ApplyProfile04O(CityRevision04O.Crystalline04O,InhabitedCityBindings04C.Select(foreign,InhabitedCityBindings04CTests.ReadCatalog())),Is.False);
            Assert.That(Signature(layout),Is.EqualTo(before));
        }
        [Test] public void RetainedRoofUsesRealPiersAndKeepsItsExactLandingHeight()
        {
            Building building=new Building{Footprint=new RectF(-12,-15,12,15),Height=90f,RebirthFamily04O=8};
            List<CityMassPiece04O> pieces=CityRebirthGeometry04O.Solids(building);
            Assert.That(pieces.Count,Is.EqualTo(6));
            CityMassPiece04O roof=pieces[1];
            Assert.That(roof.Center.y+roof.Size.y*.5f,Is.EqualTo(90f+CityLayout.CurbHeight).Within(.001f));
            Assert.That(roof.Size.x,Is.EqualTo(24f));Assert.That(roof.Size.z,Is.EqualTo(30f));
            Assert.That(pieces.Skip(2).All(p=>p.Center.y-p.Size.y*.5f<=6f+CityLayout.CurbHeight+.001f && p.Size.y==84f),Is.True);
            Assert.That(pieces.Skip(2).Sum(p=>p.Size.x*p.Size.z),Is.LessThan(building.Footprint.Area*.1f));
        }
        [Test] public void OpeningSpineIsAheadOfTheHomeAndWithinTheHeroArea()
        {
            CityLayout layout=Generate(61724,true);Poi home=layout.FindPoi("Veyra Lofts");
            SpineSite04O scar=layout.Spine04O.Find(v=>v.Id=="civic_scar");
            Vector3 offset=MathUtil.Flat(scar.Position-home.Position);
            Assert.That(Vector3.Dot(offset.normalized,home.Facing),Is.GreaterThan(.8f));
            Assert.That(offset.magnitude,Is.LessThan(150f));
        }
        [Test] public void FourSpineSegmentsShareARealRetainedRoadRoute()
        {
            CityLayout layout=Generate(61724,true);Assert.That(layout.SpineLinks04O,Has.Count.EqualTo(3));
            foreach(SpineLink04O link in layout.SpineLinks04O)
            {
                Assert.That(link.Points.Count,Is.GreaterThanOrEqualTo(2));
                foreach(Vector3 point in link.Points)
                {
                    Assert.That(MathUtil.IsFinite(point),Is.True);
                    Assert.That(layout.IsInsideBuilding(point,.1f),Is.False);
                    Assert.That(layout.IsOnRoadOrIntersection(point),Is.True);
                }
            }
        }
        [TestCase(1)][TestCase(2)]
        public void FacetedCollisionSolidsAreClosedAndUseOutwardFaces(int shape)
        {
            MeshBuilder mesh=new MeshBuilder(1);CityRebirthGeometry04O.Piece(mesh,new CityMassPiece04O(new Vector3(10,20,30),new Vector3(12,18,16),shape));
            List<int> indices=((List<int>[])typeof(MeshBuilder).GetField("_tris",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(mesh))[0];
            Dictionary<string,int> edges=new Dictionary<string,int>();
            for(int i=0;i<indices.Count;i+=3)
            {
                Vector3 a=mesh.Vertices[indices[i]],b=mesh.Vertices[indices[i+1]],c=mesh.Vertices[indices[i+2]];
                Vector3 n=Vector3.Cross(b-a,c-a).normalized;
                Assert.That(Vector3.Dot(n,(a+b+c)/3f-new Vector3(10,20,30)),Is.GreaterThan(0f));
                Assert.That(Vector3.Dot(n,mesh.Normals[indices[i]]),Is.GreaterThan(.99f));
                for(int j=0;j<3;j++)
                {
                    string x=mesh.Vertices[indices[i+j]].ToString("F4"),y=mesh.Vertices[indices[i+(j+1)%3]].ToString("F4");
                    string edge=string.CompareOrdinal(x,y)<0?x+"/"+y:y+"/"+x;int count;edges.TryGetValue(edge,out count);edges[edge]=count+1;
                }
            }
            Assert.That(edges.Values.All(v=>v==2),Is.True);
        }
        public static float Length(IList<Vector3> route) {float sum=0f;for(int i=1;i<route.Count;i++)sum+=Vector3.Distance(route[i-1],route[i]);return sum;}
    }
}
