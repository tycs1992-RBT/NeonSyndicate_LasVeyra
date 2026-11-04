using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
namespace NeonSyndicate.Tests
{
    public sealed class HeartFR02ATests
    {
        private static CityLayout Layout(int seed=61724)
        {CityLayout l=new CityLayout(seed);l.Generate();Assert.That(l.ApplyProfile04O(CityRevision04O.GothicCivicH5,InhabitedCityBindings04C.Select(l,InhabitedCityBindings04CTests.ReadCatalog())),Is.True);return l;}
        private static string Identities(CityLayout l)
        {return string.Join(";",l.Blocks.SelectMany(b=>b.Lots).SelectMany(lot=>lot.Buildings).Select(b=>b.Seed+"|"+b.Height+"|"+b.Footprint))+"/"+l.WalkNodes.Count+"/"+l.WalkLinks.Count+"/"+string.Join(";",l.CrystalShardSpots);}
        [Test] public void LegacyWorldAndMissingWorldNeverReceiveAProposal()
        {Assert.That(HeartPlanFR01.LivingProposals(null),Is.Empty);CityLayout l=new CityLayout(61724);l.Generate();Assert.That(HeartPlanFR01.LivingProposals(l),Is.Empty);}
        [TestCase(0,0,0,0,true)][TestCase(0,0,-50,0,true)][TestCase(0,0,50,0,true)]
        [TestCase(0,20,-50,20,false)][TestCase(45,0,-50,0,true)][TestCase(90,20,-50,20,false)]
        public void OrientedReservationClipsWholeSegmentsIncludingEndpoints(float yaw,float ax,float az,float bx,bool expected)
        {HeartLivingFR02A p=new HeartLivingFR02A{Size=new Vector3(8,12,10),Yaw=yaw};Assert.That(HeartPlanFR01.LivingCrosses(p,new Vector3(ax,0,az),new Vector3(bx,0,0),0),Is.EqualTo(expected));}
        [Test] public void AngledFootprintRetainsAllFourCornersAndTheSafetyMargin()
        {
            HeartLivingFR02A p=new HeartLivingFR02A{Position=new Vector3(12,.15f,40),Size=new Vector3(8,12,10),Yaw=37};RectF r=HeartPlanFR01.LivingFootprint(p,1);
            Vector3 forward=MathUtil.DirFromYaw(p.Yaw),right=MathUtil.RightOf(forward);
            foreach(int x in new[]{-1,1})foreach(int z in new[]{-1,1})Assert.That(r.Contains(p.Position+right*x*4+forward*z*5),Is.True);
            Assert.That(r.Width,Is.GreaterThan(10));Assert.That(r.Depth,Is.GreaterThan(12));
        }
        [TestCase(61724)][TestCase(1)][TestCase(2016)]
        public void CoveragePlanningIsDeterministicAndCannotChangeAnyOriginalIdentity(int seed)
        {
            CityLayout l=Layout(seed);string before=Identities(l);List<HeartLivingFR02A> a=HeartPlanFR01.LivingProposals(l),b=HeartPlanFR01.LivingProposals(l);
            Assert.That(a.Select(p=>p.Key).Distinct().Count(),Is.EqualTo(a.Count));Assert.That(a.Select(p=>p.Key+"/"+p.Module+"/"+p.Position),Is.EqualTo(b.Select(p=>p.Key+"/"+p.Module+"/"+p.Position)));
            List<HeartLivingFR02A> admitted=new List<HeartLivingFR02A>();string reason;
            foreach(HeartLivingFR02A p in a)if(HeartPlanFR01.LivingClear(l,p,admitted,out reason))
            {
                foreach(RealmRoute04OB route in l.RealmRoutes04OB)for(int i=1;i<route.Points.Count;i++)Assert.That(HeartPlanFR01.LivingCrosses(p,route.Points[i-1],route.Points[i],route.Width*.5f+2),Is.False,route.Id);
                admitted.Add(p);
            }
            Assert.That(admitted.Count,Is.GreaterThan(3));Assert.That(Identities(l),Is.EqualTo(before));
        }
        [Test] public void ServiceOwnersAndReceiptRoofsCannotBeCountedAsSafeReplacements()
        {
            CityLayout l=Layout();int service=0,review=0;
            foreach(Block block in l.Blocks)foreach(Lot lot in block.Lots)foreach(Building b in lot.Buildings)if(HeartPlanFR01.LivingSlice(l,b.Footprint.Center(0)))
            {
                string result=HeartPlanFR01.LivingClassification(l,lot,b);if(b.GothicFamily04OB==0 || b.GothicFamily04OB>=7){Assert.That(result,Is.EqualTo("Protected gameplay/service owner"));service++;}else {Assert.That(result,Is.EqualTo("Requires a native collision review"));review++;}
            }
            Assert.That(service,Is.GreaterThanOrEqualTo(12));Assert.That(review,Is.GreaterThanOrEqualTo(2));
        }
        [Test] public void DuplicateReservationsAndTheOriginalWalkCannotBeOccupied()
        {
            CityLayout l=Layout();string reason;HeartLivingFR02A found=null;
            foreach(HeartLivingFR02A p in HeartPlanFR01.LivingProposals(l))if(HeartPlanFR01.LivingClear(l,p,null,out reason)){found=p;break;}
            Assert.That(found,Is.Not.Null);Assert.That(HeartPlanFR01.LivingClear(l,found,new[]{found},out reason),Is.False);Assert.That(reason,Does.Contain("Another"));
            HeartLivingFR02A bad=new HeartLivingFR02A{Position=l.RealmRoutes04OB.Find(r=>r.Id=="home-court").Points[2],Size=new Vector3(8,12,10)};
            Assert.That(HeartPlanFR01.LivingClear(l,bad,null,out reason),Is.False);
        }
        [TestCase(float.NaN)][TestCase(float.PositiveInfinity)][TestCase(-1f)][TestCase(0f)]
        public void InvalidReservationsFailClosed(float x)
        {CityLayout l=Layout();string reason;Assert.That(HeartPlanFR01.LivingClear(l,new HeartLivingFR02A{Position=l.RealmCourt04OB,Size=new Vector3(x,12,10)},null,out reason),Is.False);}
    }
}
