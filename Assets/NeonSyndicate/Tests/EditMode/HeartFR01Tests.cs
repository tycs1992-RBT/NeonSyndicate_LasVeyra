using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
namespace NeonSyndicate.Tests
{
    public sealed class HeartFR01Tests
    {
        private static CityLayout Layout(int seed=61724)
        {CityLayout l=new CityLayout(seed);l.Generate();l.ApplyProfile04O(CityRevision04O.GothicCivicH5,InhabitedCityBindings04C.Select(l,InhabitedCityBindings04CTests.ReadCatalog()));return l;}
        [Test] public void EveryActivationCombinationFailsClosed()
        {for(int bits=0;bits<16;bits++)Assert.That(HeartPlanFR01.Active((bits&1)!=0,(bits&2)!=0,(bits&4)!=0,(bits&8)!=0),Is.EqualTo(bits==9));}
        [TestCase(61724)][TestCase(1)][TestCase(2016)] public void OriginalIdentitiesAndNavigationAreNotMutated(int seed)
        {
            CityLayout l=Layout(seed);int walks=l.WalkNodes.Count,links=l.WalkLinks.Count,routes=l.RealmRoutes04OB.Count;List<string> before=new List<string>();
            foreach(Block block in l.Blocks)foreach(Lot lot in block.Lots)foreach(Building b in lot.Buildings)before.Add(b.Seed+"/"+b.Footprint+"/"+b.Height);
            Vector3 at;float yaw;HeartPlanFR01.Gate(l,out at,out yaw);
            foreach(Block block in l.Blocks)foreach(Lot lot in block.Lots)foreach(Building b in lot.Buildings)HeartPlanFR01.Eligible(l,b);
            List<string> after=new List<string>();foreach(Block block in l.Blocks)foreach(Lot lot in block.Lots)foreach(Building b in lot.Buildings)after.Add(b.Seed+"/"+b.Footprint+"/"+b.Height);
            Assert.That(after,Is.EqualTo(before));Assert.That(l.WalkNodes.Count,Is.EqualTo(walks));Assert.That(l.WalkLinks.Count,Is.EqualTo(links));Assert.That(l.RealmRoutes04OB.Count,Is.EqualTo(routes));Assert.That((int)l.Revision04O,Is.EqualTo(10));
        }
        [Test] public void OriginalHomeRouteGivesAnOpenSafeGateway()
        {
            CityLayout l=Layout();Vector3 at,again;float yaw,yaw2;Assert.That(HeartPlanFR01.Gate(l,out at,out yaw),Is.True);
            Assert.That(HeartPlanFR01.Gate(l,out again,out yaw2),Is.True);Assert.That(again,Is.EqualTo(at));Assert.That(yaw2,Is.EqualTo(yaw));
            Assert.That(HeartPlanFR01.Clear(l,at+Vector3.right*22,3.4f),Is.True);Assert.That(HeartPlanFR01.Clear(l,at-Vector3.right*22,3.4f),Is.True);
        }
        [Test] public void OldWorldAndOutsideSliceAreRefused()
        {CityLayout l=new CityLayout(61724);l.Generate();Vector3 p;float y;Assert.That(HeartPlanFR01.Gate(l,out p,out y),Is.False);Assert.That(HeartPlanFR01.InSlice(Layout(),new Vector3(1300,0,0)),Is.False);}
        [Test] public void PublicRouteCentreAndServiceDoorCannotAdmitSideProps()
        {CityLayout l=Layout();foreach(RealmRoute04OB r in l.RealmRoutes04OB)if(r.Id=="home-court")Assert.That(HeartPlanFR01.Clear(l,r.Points[1],1),Is.False);Assert.That(HeartPlanFR01.Clear(l,l.FindPoi("Veyra Lofts").Position,2),Is.False);}
        [Test] public void ReceiptAndProtectedRoofRefuseReplacement()
        {CityLayout l=Layout();Building b=new Building{Footprint=new RectF(100,-25,120,-1),Height=12,GothicFamily04OB=4,Roof=RoofType.Flat};Assert.That(HeartPlanFR01.Eligible(l,b),Is.True);l.DataChipSpots.Add(b.Footprint.Center(1));Assert.That(HeartPlanFR01.Eligible(l,b),Is.False);l.DataChipSpots.Clear();b.RoofPad04OB=true;Assert.That(HeartPlanFR01.Eligible(l,b),Is.False);b.RoofPad04OB=false;b.GothicFamily04OB=8;Assert.That(HeartPlanFR01.Eligible(l,b),Is.False);}
        [TestCase(-2,2)][TestCase(0,0)][TestCase(12,2)]public void SegmentClearanceUsesClosestPoint(float x,float expected)
        {Assert.That(HeartPlanFR01.SegmentDistance(new Vector3(x,0,0),Vector3.zero,new Vector3(10,0,0)),Is.EqualTo(expected).Within(.001));}
        [Test,Category("NeedsEngine")] public void NativePreparedArchitectureIsPassiveAndHasActualLods()
        {HeartCatalogFR01 c=Resources.Load<HeartCatalogFR01>("HeartPreparedFR01/Catalog");Assert.That(c,Is.Not.Null,"Prepare Gothic Heart Renaissance first.");Assert.That(c.Ready,Is.True);foreach(HeartModuleFR01 m in c.Modules){Assert.That(m.Lods.Length,Is.GreaterThanOrEqualTo(1));foreach(GameObject p in m.Lods){Assert.That(p.GetComponentsInChildren<Collider>(true),Is.Empty);Assert.That(p.GetComponentsInChildren<MonoBehaviour>(true),Is.Empty);foreach(MeshFilter f in p.GetComponentsInChildren<MeshFilter>(true)){Assert.That(f.sharedMesh.uv.Length,Is.EqualTo(f.sharedMesh.vertexCount));Assert.That(f.sharedMesh.normals.Length,Is.EqualTo(f.sharedMesh.vertexCount));}}}}
    }
}
