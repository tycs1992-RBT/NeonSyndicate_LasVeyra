using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace NeonSyndicate.Tests
{
    public sealed class ScienceCircuitTests
    {
        private static CityLayout World()
        {
            CityLayout world = new CityLayout(61724); world.Generate();
            List<VenueBinding04C> venues = InhabitedCityBindings04C.Select(world,InhabitedCityBindings04CTests.ReadCatalog());
            Assert.True(world.ApplyProfile04O(CityRevision04O.GothicCivicH5,venues));
            return world;
        }

        [Test] public void SouthCutStationGardenAndBothConnectionsAreAdmitted()
        {
            ScienceDistrictArt art=HeartPlanFR01.ScienceCircuit(World());
            Assert.True(Has(art,"basalt",-119,57.5f,4)); Assert.True(Has(art,"masonry",-99,50,4));
            Assert.True(Has(art,"azure",-86,42,2)); Assert.True(Has(art,"masonry",-65,39,4));
            Assert.True(Has(art,"basalt",-110,66,4)); Assert.True(Has(art,"masonry",-13,90,3));
        }

        [Test] public void SillAndPromenadeHaveContinuousGentleThreeLaneSupport()
        {
            MeshBuilder solid=HeartPlanFR01.ScienceCircuit(World()).Solid();
            foreach(bool sill in new[]{true,false})foreach(float offset in new[]{-1.3f,0,1.3f})
            {
                float last=.164f;
                for(int i=1;i<100;i++)
                {
                    float t=i/100f,wave=Mathf.Sin(t*Mathf.PI);
                    Vector3 point=sill?new Vector3(-110+offset,1,60+10*t):new Vector3(12-50*t,1,92-2*wave*wave+offset);
                    float y=Top(solid,point,1.1f);
                    Assert.That(y,Is.InRange(.163f,.194f));Assert.Less(Mathf.Abs(y-last),.009f);last=y;
                    Assert.True(Hit(solid,new Vector3(point.x,-.2f,point.z),Vector3.up,.18f),"Solid foundation below grade");
                }
            }
        }

        [Test] public void ExistingReturnRoutesAndSixteenMetreQuestCourtRemainOpen()
        {
            CityLayout world=World();MeshBuilder solid=HeartPlanFR01.ScienceCircuit(world).Solid();
            foreach(RealmRoute04OB route in world.RealmRoutes04OB)
            for(int i=1;i<route.Points.Count;i++)
            {
                Vector3 a=route.Points[i-1],b=route.Points[i],delta=MathUtil.Flat(b-a);
                if(Mathf.Min(a.x,b.x)>20||Mathf.Max(a.x,b.x)<-135||Mathf.Min(a.z,b.z)>102||Mathf.Max(a.z,b.z)<0)continue;
                Vector3 side=MathUtil.RightOf(delta.normalized);
                for(float lane=-route.Width*.5f+.15f;lane<route.Width*.5f;lane+=.65f)
                    foreach(float y in new[]{.35f,1.7f,3.2f})Assert.False(Hit(solid,MathUtil.Flat(a)+side*lane+Vector3.up*y,delta.normalized,delta.magnitude),route.Id);
            }
            foreach(Vector3 v in solid.Vertices)if(v.y>.3f)Assert.Greater(MathUtil.FlatDistance(v,world.RealmThreshold04OB),16f);
        }

        [Test] public void OpeningInEitherWalkingBedRefusesThatEntireBed()
        {
            CityLayout world=World();
            foreach(bool sill in new[]{true,false})
            {
                Vector3 p=sill?new Vector3(-110,0,62):new Vector3(-13,0,90);
                WorldApertures.Register("circuit-opening",new RectF(p.x-.3f,p.z-.3f,p.x+.3f,p.z+.3f));
                try
                {
                    MeshBuilder solid=HeartPlanFR01.ScienceCircuit(world).Solid();
                    Vector3 other=sill?new Vector3(-110,1,68):new Vector3(-32,1,92);
                    Assert.False(Hit(solid,other,Vector3.down,1.2f));
                }
                finally{WorldApertures.Unregister("circuit-opening");}
            }
            world.RealmRoutes04OB.Find(route=>route.Id=="shardwood-threshold").Points[2]+=Vector3.right;
            Assert.IsEmpty(HeartPlanFR01.ScienceCircuit(world).Surfaces);
        }

        [Test] public void PhysicalMeshMatchesAllSolidRolesAndCoversOccupiedBeds()
        {
            ScienceDistrictArt art=HeartPlanFR01.ScienceCircuit(World());MeshBuilder solid=art.Solid();int vertex=0,index=0;
            foreach(KeyValuePair<string,MeshBuilder> part in art.Surfaces)
            {
                MeshBuilder mesh=part.Value;IReadOnlyList<int> triangles=mesh.Triangles(0);
                for(int i=0;i<triangles.Count;i+=3)
                {
                    Vector3 a=mesh.Vertices[triangles[i]],b=mesh.Vertices[triangles[i+1]],c=mesh.Vertices[triangles[i+2]];
                    Assert.True(MathUtil.IsFinite(a)&&MathUtil.IsFinite(b)&&MathUtil.IsFinite(c));
                    Assert.Greater(Vector3.Cross(b-a,c-a).sqrMagnitude,1e-12f,part.Key);
                }
                if(part.Key=="leaf")continue;
                for(int i=0;i<mesh.VertexCount;i++)Assert.AreEqual(mesh.Vertices[i],solid.Vertices[vertex+i]);
                for(int i=0;i<triangles.Count;i++)Assert.AreEqual(triangles[i]+vertex,solid.Triangles(0)[index+i]);
                vertex+=mesh.VertexCount;index+=triangles.Count;
            }
            Assert.AreEqual(index,solid.Triangles(0).Count);
            List<Bounds> zones=HeartPlanFR01.ScienceChangedZones(new MeshBuilder(1),solid);
            Assert.True(zones.Exists(b=>Contains(b,new Vector3(-110,.25f,65))));
            Assert.True(zones.Exists(b=>Contains(b,new Vector3(-13,.25f,90))));
            Assert.False(zones.Exists(b=>Contains(b,new Vector3(-108,1.4f,28))));
        }
        [Test] public void SixSurveyedCourtBaysLeaveAllRealRouteLanesClear()
        {
            CityLayout world=World();ScienceDistrictArt art=HeartPlanFR01.ScienceCourt(world);
            foreach(Vector2 site in new[]{new Vector2(-24,-19),new Vector2(-24,32),new Vector2(16,48),new Vector2(28,0),new Vector2(28,26),new Vector2(24,52)})
                Assert.True(Has(art,"slate",site.x,site.y,3),"Admitted court bay "+site);
            MeshBuilder solid=art.Solid();
            foreach(RealmRoute04OB route in world.RealmRoutes04OB)for(int i=1;i<route.Points.Count;i++)
            {
                Vector3 a=route.Points[i-1],b=route.Points[i];
                if(Mathf.Min(a.x,b.x)>36||Mathf.Max(a.x,b.x)<-36||Mathf.Min(a.z,b.z)>65||Mathf.Max(a.z,b.z)<-35)continue;
                Vector3 delta=MathUtil.Flat(b-a),side=MathUtil.RightOf(delta.normalized);
                for(float lane=-route.Width*.5f+.12f;lane<route.Width*.5f;lane+=.65f)
                    foreach(float y in new[]{.38f,1.4f,2.9f})Assert.False(Hit(solid,MathUtil.Flat(a)+side*lane+Vector3.up*y,delta.normalized,delta.magnitude),route.Id);
            }
            WorldApertures.Register("court-bay-test",new RectF(-25,-20,-23,-18));
            try{Assert.False(Has(HeartPlanFR01.ScienceCourt(world),"slate",-24,-19,3));}
            finally{WorldApertures.Unregister("court-bay-test");}
        }

        [Test] public void SharedVaultVerticesKeepContinuousMetreScaleUvs()
        {
            foreach(string id in new[]{"lofts","forge","archive"})
            {
                MeshBuilder roof=HeartPlanFR01.ScienceService(id,new MeshBuilder(1),new MeshBuilder(1)).Surfaces["slate"];
                Dictionary<Vector3,Vector2> seen=new Dictionary<Vector3,Vector2>();int repeated=0;float min=float.MaxValue,max=float.MinValue;
                for(int i=0;i<roof.VertexCount;i++)
                {
                    Vector3 point=roof.Vertices[i];Vector2 uv=roof.UVs[i];Vector2 previous;
                    if(seen.TryGetValue(point,out previous)){Assert.AreEqual(previous,uv,id+" vault seam");repeated++;}
                    else seen.Add(point,uv);
                    min=Mathf.Min(min,uv.y);max=Mathf.Max(max,uv.y);
                }
                Assert.Greater(repeated,100);Assert.Greater(max-min,8,"Texture spans multiple repeats along the occupied hall");
            }
        }

        [Test] public void ShelterInteriorAndPathEndShareOneProtectedArrivalBed()
        {
            CityLayout world=World();Vector3 end=new Vector3(-83.56732f,0,84.46526f),centre=new Vector3(-82,0,78);
            Vector3 inside=centre-(end-centre).normalized;
            Assert.True(Hit(HeartPlanFR01.ScienceFrontier(world).Surfaces["earth"],inside+Vector3.up,Vector3.down,1));
            WorldApertures.Register("shelter-end-test",new RectF(end.x-.2f,end.z-.2f,end.x+.2f,end.z+.2f));
            try{Assert.False(Hit(HeartPlanFR01.ScienceFrontier(world).Surfaces["earth"],inside+Vector3.up,Vector3.down,1));}
            finally{WorldApertures.Unregister("shelter-end-test");}
        }

        [Test] public void StationApronReachesBothTheRoadAndTheTerrace()
        {
            MeshBuilder mesh=HeartPlanFR01.ScienceCircuit(World()).Solid();
            for(int i=0;i<=20;i++)
            {
                float z=Mathf.Lerp(29.375f,39.57f,i/20f);
                foreach(float lane in new[]{-1.2f,0,1.2f})
                    Assert.That(Top(mesh,new Vector3(-86+lane,1,z),1.1f),Is.InRange(.14f,.172f));
            }
            // The turning fan is low enough to retain the existing observation court and overlaps neither actor nor quest logic.
            Assert.That(Top(mesh,new Vector3(-104,1,31.0f),1.1f),Is.InRange(.10f,.20f));
        }

        [Test] public void OpeningInTurningCourtOrStationApronLeavesNoPartialFloor()
        {
            CityLayout world=World();
            WorldApertures.Register("circuit-fan",new RectF(-104.6f,31.3f,-104.1f,31.8f));
            WorldApertures.Register("circuit-turnout",new RectF(-86.3f,34.2f,-85.7f,35));
            try
            {
                MeshBuilder mesh=HeartPlanFR01.ScienceCircuit(world).Solid();
                Assert.False(Hit(mesh,new Vector3(-104,1,31),Vector3.down,1.1f));
                Assert.False(Hit(mesh,new Vector3(-86,1,37),Vector3.down,1.1f));
            }
            finally{WorldApertures.Unregister("circuit-turnout");WorldApertures.Unregister("circuit-fan");}
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
    /// <summary>Requires the real Unity mesh cooker, materials and GameObjects.</summary>
    [Category("NeedsEngine")]
    public sealed class ScienceCircuitNativeTests
    {
        [Test] public void PreparingAndSwitchingTheSameParentKeepsOnePhysicalPair()
        {
            GameObject root=new GameObject("Circuit native replay");
            try
            {
                HeartPresentationFR01 heart=new HeartPresentationFR01(null,root.transform);heart.State(false,false);
                MeshBuilder source=new MeshBuilder(1);source.Box(0,Vector3.zero,new Vector3(4,.25f,4),UVMode.World);
                Dictionary<string,MeshBuilder> old=new Dictionary<string,MeshBuilder>{{"masonry",source}};
                ScienceDistrictArt art=new ScienceDistrictArt();art.Surface("masonry").Box(0,Vector3.zero,new Vector3(5,.25f,4),UVMode.World);
                System.Reflection.MethodInfo stage=typeof(HeartPresentationFR01).GetMethod("StageScienceDistrict",
                    System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
                object[] inputs={"circuit-test",root.transform,0,old,old,source,art,false};
                for(int pass=0;pass<3;pass++)
                {
                    Assert.True((bool)stage.Invoke(heart,inputs),"The caller must not emit a second fallback");
                    Assert.AreEqual(2,root.GetComponentsInChildren<MeshCollider>(true).Length);
                    Transform original=HeartPresentationFR01.FindSciencePhysicalOwner(root.transform,"circuit-test",false);
                    Transform candidate=HeartPresentationFR01.FindSciencePhysicalOwner(root.transform,"circuit-test",true);
                    Assert.NotNull(original);Assert.NotNull(candidate);
                    heart.BeginFantasyPreviewV1();Assert.True(candidate.gameObject.activeSelf);Assert.False(original.gameObject.activeSelf);
                    heart.EndFantasyPreviewV1();Assert.True(original.gameObject.activeSelf);Assert.False(candidate.gameObject.activeSelf);
                }
            }
            finally{UnityEngine.Object.DestroyImmediate(root);}
        }
    }

}
