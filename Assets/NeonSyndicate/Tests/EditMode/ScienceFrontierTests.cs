using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace NeonSyndicate.Tests
{
    public sealed class ScienceFrontierTests
    {
        private static CityLayout World()
        {
            CityLayout world=new CityLayout(61724);world.Generate();
            List<VenueBinding04C> venues=InhabitedCityBindings04C.Select(world,InhabitedCityBindings04CTests.ReadCatalog());
            Assert.True(world.ApplyProfile04O(CityRevision04O.GothicCivicH5,venues));return world;
        }
        [Test] public void CanonicalCompositionContainsBoundaryMineralAndArrival()
        {
            ScienceDistrictArt art=HeartPlanFR01.ScienceFrontier(World());
            Assert.True(Has(art,"slate",-52,92,5));Assert.True(Has(art,"slate",-82,78,5));
            Assert.True(Has(art,"azure",-94,96,5));Assert.True(Has(art,"leaf",-80,70,5));
            int count=0;foreach(MeshBuilder m in art.Surfaces.Values)count+=m.Triangles(0).Count/3;
            Assert.That(count,Is.InRange(18000,50000),"Substantial composition with a finite mesh budget.");
        }
        [Test] public void FiniteNondegenerateBoundedGeometryAndSolidCorrespondence()
        {
            ScienceDistrictArt art=HeartPlanFR01.ScienceFrontier(World());int solids=0;
            foreach(KeyValuePair<string,MeshBuilder> part in art.Surfaces)
            {
                MeshBuilder m=part.Value;IReadOnlyList<int> t=m.Triangles(0);
                foreach(Vector3 p in m.Vertices){Assert.True(MathUtil.IsFinite(p));Assert.That(p.x,Is.InRange(-110,-35));Assert.That(p.z,Is.InRange(65,110));Assert.That(p.y,Is.InRange(-1,18));}
                for(int i=0;i<t.Count;i+=3)Assert.Greater(Vector3.Cross(m.Vertices[t[i+1]]-m.Vertices[t[i]],m.Vertices[t[i+2]]-m.Vertices[t[i]]).sqrMagnitude,1e-12f);
                if(part.Key!="leaf")solids+=t.Count;
            }
            MeshBuilder solid=art.Solid();Assert.AreEqual(solids,solid.Triangles(0).Count);
            int offset=0;foreach(KeyValuePair<string,MeshBuilder> part in art.Surfaces)if(part.Key!="leaf")
            {for(int i=0;i<part.Value.VertexCount;i++)Assert.AreEqual(part.Value.Vertices[i],solid.Vertices[offset+i]);offset+=part.Value.VertexCount;}
        }
        [Test] public void AllActualNearbyRoutesKeepWalkingAndVehicleHeadroom()
        {
            CityLayout w=World();MeshBuilder solid=HeartPlanFR01.ScienceFrontier(w).Solid();int samples=0;
            foreach(RealmRoute04OB route in w.RealmRoutes04OB)for(int i=1;i<route.Points.Count;i++)
            {
                Vector3 a=route.Points[i-1],b=route.Points[i],d=MathUtil.Flat(b-a),side=MathUtil.RightOf(d.normalized);
                if(Mathf.Max(a.x,b.x)<-115||Mathf.Min(a.x,b.x)>-35||Mathf.Max(a.z,b.z)<65||Mathf.Min(a.z,b.z)>110)continue;
                for(float lane=-route.Width*.5f+.15f;lane<=route.Width*.5f-.15f;lane+=.7f)
                    foreach(float y in new[]{.3f,1.4f,3f})
                    {Assert.False(Hit(solid,a+side*lane+Vector3.up*y,d.normalized,d.magnitude),route.Id+" lane "+lane);samples++;}
            }
            Assert.Greater(samples,100);
        }
        [Test] public void ClearanceIncludesAddedRoofAndExcludesOpenRouteCentre()
        {
            List<Bounds> zones=HeartPlanFR01.ScienceChangedZones(new MeshBuilder(1),HeartPlanFR01.ScienceFrontier(World()).Solid());
            Assert.True(zones.Exists(b=>Contains(b,new Vector3(-52,7.95f,92))));
            Assert.False(zones.Exists(b=>Contains(b,new Vector3(-52,1.2f,92))));
        }
        [Test] public void SiteGuardRejectsBuildingRouteAndRegisteredOpening()
        {
            CityLayout w=World();Assert.False(HeartPlanFR01.ScienceFrontierClear(w,new Vector3(-60,0,72),1,1));
            Assert.False(HeartPlanFR01.ScienceFrontierClear(w,new Vector3(-60,0,92),1,1));
            Vector3 at=new Vector3(-94,0,96);Assert.True(HeartPlanFR01.ScienceFrontierClear(w,at,3.1f,2.6f));
            WorldApertures.Register("frontier-test",new RectF(-96,94,-92,98));
            try{Assert.False(HeartPlanFR01.ScienceFrontierClear(w,at,3.1f,2.6f));}finally{WorldApertures.Unregister("frontier-test");}
        }
        [Test] public void UnsupportedOrChangedRouteCannotPlaceGuessedArchitecture()
        {
            Assert.IsEmpty(HeartPlanFR01.ScienceFrontier(null).Surfaces);
            CityLayout w=World();w.RealmRoutes04OB.Find(r=>r.Id=="sanctuary-wilds").Points[1]+=Vector3.right*6;
            Assert.IsEmpty(HeartPlanFR01.ScienceFrontier(w).Surfaces);
        }
        [Test] public void ArrivalApronHasContinuousGentleSupportToTheExistingPath()
        {
            MeshBuilder mesh=HeartPlanFR01.ScienceFrontier(World()).Surfaces["earth"];
            float previous=0;
            for(int sample=1;sample<20;sample++)
            {
                Vector3 at=Vector3.Lerp(new Vector3(-82,0,78),new Vector3(-83.56732f,0,84.46526f),sample/20f);
                // Find the actual top triangle by narrowing a vertical ray, independent of the grade formula.
                float low=0,high=.3f;
                Assert.True(Hit(mesh,at+Vector3.up*high,Vector3.down,high));
                for(int i=0;i<18;i++){float middle=(low+high)*.5f;if(Hit(mesh,at+Vector3.up*high,Vector3.down,high-middle))low=middle;else high=middle;}
                float y=(low+high)*.5f;
                Assert.That(y,Is.InRange(.02f,.175f));
                if(sample>1){Assert.GreaterOrEqual(y,previous-.001f);Assert.Less(y-previous,.02f);}
                previous=y;
            }
        }
        [Test] public void GroundSamplerMatchesExistingTerrainTriangles()
        {
            CityLayout w=World();WildPatch04OC p=new WildPatch04OC{Area=new RectF(-91,67,-79,79),Height=3,Seed=91};
            w.WildPatches04OC.Add(p);MeshBuilder terrain=new MeshBuilder(1);GothicRealmGeometry04OB.WildTerrain04OC(terrain,w,p);
            IReadOnlyList<int> t=terrain.Triangles(0);for(int i=0;i<t.Count;i+=3)
            {
                Vector3 centre=(terrain.Vertices[t[i]]+terrain.Vertices[t[i+1]]+terrain.Vertices[t[i+2]])/3;
                Assert.AreEqual(centre.y,HeartPlanFR01.ScienceFrontierGround(w,centre),.0001f);
            }
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
