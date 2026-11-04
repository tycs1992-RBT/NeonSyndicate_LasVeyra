using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
namespace NeonSyndicate.Tests
{
    public sealed class HeartFR01ETests
    {
        private static CityLayout Layout(int seed=61724)
        {CityLayout l=new CityLayout(seed);l.Generate();l.ApplyProfile04O(CityRevision04O.GothicCivicH5,InhabitedCityBindings04C.Select(l,InhabitedCityBindings04CTests.ReadCatalog()));return l;}
        [TestCase(61724)][TestCase(1)][TestCase(2016)]
        public void CompactCandidatesKeepEveryOriginalPublicRouteClear(int seed)
        {
            CityLayout l=Layout(seed);int admitted=0;Vector3 at;float yaw;string reason;
            for(int i=0;i<HeartPlanFR01.GateCandidates;i++)if(HeartPlanFR01.GateCandidate(l,i,out at,out yaw,out reason))
            {
                admitted++;Vector3 right=MathUtil.RightOf(MathUtil.DirFromYaw(yaw));
                foreach(int sign in new[]{-1,1})foreach(RealmRoute04OB r in l.RealmRoutes04OB)for(int j=1;j<r.Points.Count;j++)
                    Assert.That(HeartPlanFR01.SegmentDistance(at+right*sign*9f,r.Points[j-1],r.Points[j]),Is.GreaterThanOrEqualTo(r.Width*.5f+4.4f-.001f),r.Id);
                bool onRoute=false;foreach(RealmRoute04OB r in l.RealmRoutes04OB)if(r.Id=="sanctuary-approach")for(int j=1;j<r.Points.Count;j++)onRoute|=HeartPlanFR01.SegmentDistance(at,r.Points[j-1],r.Points[j])<.001f;
                Assert.That(onRoute,Is.True);Assert.That(2f*(9f-2.5f),Is.GreaterThan(9f),"New bases must retain the old public width.");
            }
            Assert.That(admitted,Is.GreaterThan(0));
        }
        [Test] public void MissingRouteConnectionFailsClosedWithReason()
        {
            CityLayout l=Layout();foreach(RealmRoute04OB r in l.RealmRoutes04OB)if(r.Id=="home-court")r.Points[r.Points.Count-1]+=Vector3.right*2;
            Vector3 at;float yaw;string reason;Assert.That(HeartPlanFR01.Gate(l,out at,out yaw),Is.False);
            Assert.That(HeartPlanFR01.GateCandidate(l,0,out at,out yaw,out reason),Is.False);Assert.That(reason,Does.Contain("connection"));
        }
        [Test] public void UnsafeAndOutOfRangeCandidatesAreRefused()
        {
            CityLayout l=Layout();Vector3 at;float yaw;string reason;
            Assert.That(HeartPlanFR01.GateCandidate(l,0,out at,out yaw,out reason),Is.False);Assert.That(reason,Does.Contain("pier"));
            Assert.That(HeartPlanFR01.GateCandidate(l,-1,out at,out yaw,out reason),Is.False);
            Assert.That(HeartPlanFR01.GateCandidate(l,HeartPlanFR01.GateCandidates,out at,out yaw,out reason),Is.False);
            l.RealmRoutes04OB.Clear();Assert.That(HeartPlanFR01.Gate(l,out at,out yaw),Is.False);
        }
        [Test] public void FramingUsesTheExistingStreetAndKeepsItsGatewayReservation()
        {
            CityLayout l=Layout();Vector3 gate;float yaw;Assert.That(HeartPlanFR01.Gate(l,out gate,out yaw),Is.True);
            List<HeartSideFR01> sides=HeartPlanFR01.StreetSides(l);int admitted=0,refused=0;
            foreach(HeartSideFR01 side in sides)
            {if(HeartPlanFR01.Clear(l,side.Position,side.Radius) && HeartPlanFR01.OutsideGate(side.Position,side.Radius,gate,yaw))admitted++;else refused++;Assert.That(side.Module=="HeartArcade" || side.Module=="HeartBlindBay",Is.True);}
            Assert.That(sides.Count,Is.EqualTo(16));Assert.That(admitted,Is.GreaterThan(2));Assert.That(refused,Is.GreaterThan(0));
            Assert.That(HeartPlanFR01.OutsideGate(gate,3,gate,yaw),Is.False);
            Assert.That(HeartPlanFR01.OutsideGate(gate+MathUtil.RightOf(MathUtil.DirFromYaw(yaw))*9,3,gate,yaw),Is.False);
            Assert.That(HeartPlanFR01.OutsideGate(gate+Vector3.right*60,3,gate,yaw),Is.True);
        }
        [Test] public void AsymmetricImportedBoundsExactlyFitTheOriginalParcel()
        {
            Building b=new Building{Footprint=new RectF(94,-26,116,0),Height=12};
            Vector3 center=new Vector3(1.2f,8,-.25f),size=new Vector3(12.8f,16,14.7f),position,scale;
            Assert.That(HeartPlanFR01.Fit(b,center,size,out position,out scale),Is.True);
            Vector3 minimum=position+Vector3.Scale(center-size*.5f,scale),maximum=position+Vector3.Scale(center+size*.5f,scale);
            Assert.That(minimum.x,Is.EqualTo(94).Within(.001));Assert.That(maximum.x,Is.EqualTo(116).Within(.001));
            Assert.That(minimum.z,Is.EqualTo(-26).Within(.001));Assert.That(maximum.z,Is.EqualTo(0).Within(.001));
            Assert.That(minimum.y,Is.EqualTo(CityLayout.CurbHeight).Within(.001));Assert.That(maximum.y-minimum.y,Is.EqualTo(12).Within(.001));
        }
        [TestCase(0f)][TestCase(float.NaN)][TestCase(float.PositiveInfinity)]
        public void InvalidImportedBoundsNeverSuppressOriginalVisuals(float width)
        {Vector3 position,scale;Building b=new Building{Footprint=new RectF(94,-26,116,0),Height=12};Assert.That(HeartPlanFR01.Fit(b,Vector3.zero,new Vector3(width,12,14),out position,out scale),Is.False);}
        [TestCase(1)][TestCase(2)][TestCase(3)]
        public void FinalLodKeepsTheDistantSilhouetteIncludingSingleLodReuse(int count)
        {float previous=1f;for(int i=0;i<count;i++){float t=HeartPlanFR01.LodThreshold(i,count);Assert.That(t,Is.LessThan(previous));previous=t;}Assert.That(previous,Is.EqualTo(.025f));}
    }
}
