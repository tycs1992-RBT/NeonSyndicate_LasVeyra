using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
namespace NeonSyndicate.Tests
{
    public sealed class ScienceGrottoTests
    {
        private static CityLayout World()
        {
            CityLayout w=new CityLayout(61724);w.Generate();
            List<VenueBinding04C> v=InhabitedCityBindings04C.Select(w,InhabitedCityBindings04CTests.ReadCatalog());
            Assert.True(w.ApplyProfile04O(CityRevision04O.GothicCivicH5,v));return w;
        }
        [Test] public void CanonicalOwnerAndThreePartCompositionArePresent()
        {
            CityLayout w=World();Building owner=HeartPlanFR01.ScienceGrottoOwner(w);
            Assert.NotNull(owner);Assert.AreEqual(961824,owner.Seed);
            Assert.True(HeartPlanFR01.ScienceGrottoEnvelopeClear(w,owner));
            ScienceDistrictArt a=HeartPlanFR01.ScienceGrotto(w);
            Assert.True(Has(a,"basalt",-109,97,5),"Mineral approach");
            Assert.True(Has(a,"slate",-110,82,5),"Thick mouth collar");
            Assert.True(Has(a,"warm",-116,76,2),"Arrival bay");
            Assert.True(Has(a,"azure",-116.5f,70.4f,1),"Contained fixture beside retained mineral");
            Assert.That(a.Solid().Triangles(0).Count/3,Is.InRange(6000,28000));
        }
        [Test] public void EveryVisibleSolidHasFiniteNondegenerateMatchingCollision()
        {
            ScienceDistrictArt a=HeartPlanFR01.ScienceGrotto(World());MeshBuilder solid=a.Solid();int count=0,offset=0;
            foreach(KeyValuePair<string,MeshBuilder> part in a.Surfaces)
            {
                MeshBuilder m=part.Value;IReadOnlyList<int> t=m.Triangles(0);
                foreach(Vector3 v in m.Vertices){Assert.True(MathUtil.IsFinite(v));Assert.That(v.x,Is.InRange(-122,-97));Assert.That(v.z,Is.InRange(69,105));Assert.That(v.y,Is.InRange(-.3f,13));}
                for(int i=0;i<t.Count;i+=3)Assert.Greater(Vector3.Cross(m.Vertices[t[i+1]]-m.Vertices[t[i]],m.Vertices[t[i+2]]-m.Vertices[t[i]]).sqrMagnitude,1e-12f);
                if(part.Key=="leaf")continue;count+=t.Count;
                for(int i=0;i<m.VertexCount;i++)Assert.AreEqual(m.Vertices[i],solid.Vertices[offset+i]);offset+=m.VertexCount;
            }
            Assert.AreEqual(count,solid.Triangles(0).Count);
        }
        [Test] public void ActualCrossingAndThroughRoutesKeepFullWidthHeadroom()
        {
            CityLayout w=World();MeshBuilder solid=HeartPlanFR01.ScienceGrotto(w).Solid();int samples=0;
            foreach(RealmRoute04OB route in w.RealmRoutes04OB)for(int i=1;i<route.Points.Count;i++)
            {
                Vector3 a=route.Points[i-1],b=route.Points[i],d=MathUtil.Flat(b-a),side=MathUtil.RightOf(d.normalized);
                if(Mathf.Max(a.x,b.x)<-130||Mathf.Min(a.x,b.x)>-90||Mathf.Max(a.z,b.z)<64||Mathf.Min(a.z,b.z)>105)continue;
                for(float lane=-route.Width*.5f+.15f;lane<=route.Width*.5f-.15f;lane+=.6f)
                    foreach(float y in new[]{.3f,1.4f,3.2f})
                    {Assert.False(Hit(solid,MathUtil.Flat(a)+side*lane+Vector3.up*y,d.normalized,d.magnitude),route.Id+" lane "+lane+" y "+y);samples++;}
            }
            Assert.Greater(samples,200);
        }
        [Test] public void ExistingQuestGatheringRadiusAndBothMouthsStayOpen()
        {
            MeshBuilder solid=HeartPlanFR01.ScienceGrotto(World()).Solid();
            for(int i=0;i<36;i++)foreach(float y in new[]{.3f,1.4f,2.8f})
            {
                float angle=i*Mathf.PI/18;Vector3 d=new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle));
                Assert.False(Hit(solid,new Vector3(-110,y,74)-d*4,d,8));
            }
            foreach(float lane in new[]{-4.5f,0,4.5f})foreach(float y in new[]{.3f,1.4f,3f})
                Assert.False(Hit(solid,new Vector3(-110+lane,y,64),Vector3.forward,22));
        }
        [Test] public void ArrivalFloorIsContinuousSupportedAndGentlyTapered()
        {
            MeshBuilder solid=HeartPlanFR01.ScienceGrotto(World()).Solid();
            foreach(float lane in new[]{-4f,0,4f})
            {
                float previous=.164f;
                for(int n=1;n<72;n++)
                {
                    Vector3 p=new Vector3(-110+lane,.5f,70+n*.2f);
                    float y=Top(solid,p,.5f);Assert.That(y,Is.InRange(.163f,.201f));
                    Assert.Less(Mathf.Abs(y-previous),.025f);previous=y;
                    Assert.True(Hit(solid,new Vector3(p.x,-.2f,p.z),Vector3.up,.3f),"Closed lower face");
                }
            }
        }
        [Test] public void MouthHasThickCeilingAndLandingOccupancyCoverage()
        {
            MeshBuilder solid=HeartPlanFR01.ScienceGrotto(World()).Solid();
            float roof=Top(solid,new Vector3(-110,12,80.2f),12);
            Assert.That(roof,Is.InRange(7.5f,7.8f));
            Assert.True(Hit(solid,new Vector3(-110,6.9f,80.2f),Vector3.up,.3f),"Visible underside is solid");
            List<Bounds> zones=HeartPlanFR01.ScienceChangedZones(new MeshBuilder(1),solid);
            Assert.True(zones.Exists(b=>Contains(b,new Vector3(-110,roof+.1f,80.2f))));
            Assert.True(zones.Exists(b=>Contains(b,new Vector3(-110,.3f,74))));
            Assert.False(zones.Exists(b=>Contains(b,new Vector3(-110,1.5f,74))));
        }
        [Test] public void ChangedLayoutOpeningAndNewCrossingAreRefused()
        {
            Assert.IsEmpty(HeartPlanFR01.ScienceGrotto(null).Surfaces);
            CityLayout w=World();Building cave=HeartPlanFR01.ScienceGrottoOwner(w);
            w.RealmRoutes04OB.Find(r=>r.Id=="grotto-through-route").Points[4]+=Vector3.right;
            Assert.IsEmpty(HeartPlanFR01.ScienceGrotto(w).Surfaces);
            w=World();cave=HeartPlanFR01.ScienceGrottoOwner(w);
            WorldApertures.Register("grotto-test",new RectF(-117,79,-115,81));
            try{Assert.False(HeartPlanFR01.ScienceGrottoEnvelopeClear(w,cave));Assert.False(Has(HeartPlanFR01.ScienceGrotto(w),"slate",-110,82,4));}
            finally{WorldApertures.Unregister("grotto-test");}
            RealmRoute04OB crossing=new RealmRoute04OB{Id="test-crossing",Width=5};crossing.Points.Add(new Vector3(-125,0,78));crossing.Points.Add(new Vector3(-95,0,78));w.RealmRoutes04OB.Add(crossing);
            Assert.False(HeartPlanFR01.ScienceGrottoEnvelopeClear(w,cave));
        }
        [Test] public void RepeatedPlanningIsStableAndSwitchingRetainsOccupiedSurfaces()
        {
            CityLayout w=World();ScienceDistrictArt a=HeartPlanFR01.ScienceGrotto(w),b=HeartPlanFR01.ScienceGrotto(w);
            CollectionAssert.AreEquivalent(a.Surfaces.Keys,b.Surfaces.Keys);
            foreach(string role in a.Surfaces.Keys){CollectionAssert.AreEqual(a.Surfaces[role].Vertices,b.Surfaces[role].Vertices);CollectionAssert.AreEqual(a.Surfaces[role].Triangles(0),b.Surfaces[role].Triangles(0));}
            for(int i=0;i<12;i++)
            {
                Assert.AreEqual(ScienceDistrictSelection.WaitForClearance,HeartPlanFR01.ScienceSelection(true,true,false,true));
                Assert.AreEqual(ScienceDistrictSelection.Prepared,HeartPlanFR01.ScienceSelection(true,true,false,false));
                Assert.AreEqual(ScienceDistrictSelection.WaitForClearance,HeartPlanFR01.ScienceSelection(false,true,true,true));
                Assert.AreEqual(ScienceDistrictSelection.Retained,HeartPlanFR01.ScienceSelection(false,true,true,false));
                Assert.AreEqual(ScienceDistrictSelection.Retained,HeartPlanFR01.ScienceSelection(true,false,true,true));
            }
        }
        private static float Top(MeshBuilder m,Vector3 p,float distance)
        {
            Assert.True(Hit(m,p,Vector3.down,distance));float low=p.y-distance,high=p.y;
            for(int i=0;i<21;i++){float middle=(low+high)*.5f;if(Hit(m,p,Vector3.down,p.y-middle))low=middle;else high=middle;}
            return (low+high)*.5f;
        }
        private static bool Has(ScienceDistrictArt a,string role,float x,float z,float radius)
        {MeshBuilder m;return a.Surfaces.TryGetValue(role,out m)&&m.Vertices.Exists(p=>MathUtil.FlatDistance(p,new Vector3(x,0,z))<radius);}
        private static bool Contains(Bounds b,Vector3 p)
        {Vector3 lo=b.min,hi=b.max;return p.x>=lo.x&&p.x<=hi.x&&p.y>=lo.y&&p.y<=hi.y&&p.z>=lo.z&&p.z<=hi.z;}
        private static bool Hit(MeshBuilder m,Vector3 origin,Vector3 direction,float length)
        {
            IReadOnlyList<int> t=m.Triangles(0);for(int i=0;i<t.Count;i+=3)
            {
                Vector3 a=m.Vertices[t[i]],e=m.Vertices[t[i+1]]-a,f=m.Vertices[t[i+2]]-a,p=Vector3.Cross(direction,f);
                float det=Vector3.Dot(e,p);if(Mathf.Abs(det)<1e-7f)continue;
                Vector3 delta=origin-a;float u=Vector3.Dot(delta,p)/det;if(u<0||u>1)continue;
                Vector3 q=Vector3.Cross(delta,e);float v=Vector3.Dot(direction,q)/det;if(v<0||u+v>1)continue;
                float d=Vector3.Dot(f,q)/det;if(d>=0&&d<=length)return true;
            }return false;
        }
    }
}
