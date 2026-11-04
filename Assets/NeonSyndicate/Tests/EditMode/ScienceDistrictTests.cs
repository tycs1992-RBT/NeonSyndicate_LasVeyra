using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace NeonSyndicate.Tests
{
    public sealed class ScienceDistrictTests
    {
        [TestCase("lofts")] [TestCase("forge")] [TestCase("archive")] [TestCase("home-court")]
        public void ActualMeshesAreFiniteBoundedAndHaveNoDegenerateTriangles(string id)
        {
            ScienceDistrictArt art = Art(id);
            int triangles = 0;
            foreach(MeshBuilder mesh in art.Surfaces.Values)
            {
                foreach(Vector3 p in mesh.Vertices) { Assert.True(MathUtil.IsFinite(p)); Assert.Less(Mathf.Abs(p.x),35); Assert.Less(p.y,18); Assert.Less(Mathf.Abs(p.z),65); }
                foreach(Vector3 n in mesh.Normals) Assert.That(n.sqrMagnitude,Is.InRange(.99f,1.01f));
                IReadOnlyList<int> indices=mesh.Triangles(0);
                for(int i=0;i<indices.Count;i+=3)
                {
                    Vector3 a=mesh.Vertices[indices[i]],b=mesh.Vertices[indices[i+1]],c=mesh.Vertices[indices[i+2]];
                    Assert.Greater(Vector3.Cross(b-a,c-a).sqrMagnitude,1e-12f,id+" collapsed face");triangles++;
                }
            }
            Assert.Greater(triangles,3000);Assert.Less(triangles,45000,"Bound the actual architecture budget.");
        }

        [TestCase("lofts")] [TestCase("forge")] [TestCase("archive")] [TestCase("home-court")]
        public void EveryVisibleSolidTriangleHasTheSamePhysicalTriangle(string id)
        {
            ScienceDistrictArt art=Art(id); MeshBuilder solid=art.Solid();int offset=0;
            foreach(KeyValuePair<string,MeshBuilder> part in art.Surfaces)
            {
                if(part.Key=="leaf")continue;
                foreach(int index in part.Value.Triangles(0))
                    Assert.AreEqual(part.Value.Vertices[index],solid.Vertices[solid.Triangles(0)[offset++]]);
            }
            Assert.AreEqual(offset,solid.Triangles(0).Count);
        }

        [TestCase("lofts")] [TestCase("forge")]
        public void DoorAndExistingServiceApproachRemainOpen(string id)
        {
            MeshBuilder solid=Art(id).Solid();
            // An explicit 2.8m wide, 2.2m high walking prism from the public threshold to the rear of the room.
            for(float x=-1.4f;x<=1.4f;x+=.35f)for(float y=.2f;y<=2.2f;y+=.4f)
                Assert.False(Hits(solid,new Vector3(x,y,-3),Vector3.forward,21),id+" walking prism "+x+","+y);
            Assert.False(Hits(solid,new Vector3(-4.5f,.8f,4),Vector3.forward,4),"Existing anvil approach");
            Assert.False(Hits(solid,new Vector3(5.3f,.8f,4),Vector3.forward,5),"Existing staff approach");
        }

        [Test] public void TerraceLandingSeatAndRailShareTheSwitchedMesh()
        {
            MeshBuilder mesh=Art("lofts").Solid();
            List<Bounds> zones=HeartPlanFR01.ScienceChangedZones(new MeshBuilder(1),mesh);
            // A 4 by 4 m open-sky flight landing clear above the existing two-storey room.
            foreach(float x in new[]{-6f,-4f,-2f})foreach(float z in new[]{1f,3f,5f})
            {
                Vector3 feet=new Vector3(x,9.19f,z);
                Assert.True(Hits(mesh,feet,Vector3.down,.08f),"Level landing "+feet);
                Assert.False(Hits(mesh,feet,Vector3.up,20),"Open sky "+feet);
                Assert.True(zones.Exists(b=>Contains(b,feet)),"Occupied landing is in the transition guard.");
            }
            Assert.True(Hits(mesh,new Vector3(-4,10.25f,1),Vector3.back,3),"Court-facing guardrail");
            Assert.False(Hits(mesh,new Vector3(-4,10.4f,.48f),Vector3.right,8),"Clear passage to shaded bench");
            Assert.True(Hits(mesh,new Vector3(4,9.85f,-.59f),Vector3.down,.45f),"Bench has a seat");
            Assert.True(Hits(mesh,new Vector3(4,11.2f,-.59f),Vector3.up,4),"Canopy covers seat above head height");
        }

        [Test] public void ForgeThroatRemainsOpenThroughTheCrown()
        {
            ScienceDistrictArt art=Art("forge");
            MeshBuilder mesh=art.Solid();
            foreach(float x in new[]{4.5f,5.1f,5.7f})foreach(float z in new[]{8.4f,9f,9.6f})
                Assert.False(Hits(mesh,new Vector3(x,6.4f,z),Vector3.up,14),"Open exhaust bore");
            Assert.True(Hits(mesh,new Vector3(5.1f,12.55f,9),Vector3.right,2),"Lined physical bore");
            Assert.True(Hits(mesh,new Vector3(0,15,10),Vector3.down,10),"Connected lower hall roof");
            Assert.True(Hits(mesh,new Vector3(5.1f,12.46f,6.2f),Vector3.forward,1.0f),"Recessed public lens");
            Assert.Greater(art.Surfaces["forge-lens"].VertexCount,0);
            Assert.False(Art("lofts").Surfaces.ContainsKey("forge-lens"));
            List<Bounds> zones=HeartPlanFR01.ScienceChangedZones(new MeshBuilder(1),mesh);
            Assert.True(zones.Exists(b=>Contains(b,new Vector3(8.25f,16.4f,9))),"Occupied exhaust rim must delay restoration");
        }

        [Test] public void CourtRetainsItsCentralNorthSouthWalkingCorridor()
        {
            MeshBuilder mesh=HeartPlanFR01.ScienceCourt().Solid();
            for(float x=-8;x<=12;x+=2)for(float y=.35f;y<=2.4f;y+=.4f)
                Assert.False(Hits(mesh,new Vector3(x,y,-26),Vector3.forward,100));
        }

        [Test] public void ExistingHomeSupportIsIncludedWithoutMovingVertices()
        {
            MeshBuilder structure=new MeshBuilder(1),porch=new MeshBuilder(1);
            structure.Box(0,new Vector3(10,.25f,8),new Vector3(6,.5f,8),UVMode.World);
            porch.Box(0,new Vector3(10,.2f,2),new Vector3(4,.4f,4),UVMode.World);
            MeshBuilder result=HeartPlanFR01.ScienceService("lofts",structure,porch).Surfaces["masonry"];
            for(int i=0;i<structure.VertexCount;i++)Assert.AreEqual(structure.Vertices[i],result.Vertices[i]);
            for(int i=0;i<porch.VertexCount;i++)Assert.AreEqual(porch.Vertices[i],result.Vertices[i+structure.VertexCount]);
        }

        [Test] public void UnrelatedServiceOrChangedFrameCannotBeReplaced()
        {
            Assert.True(HeartPlanFR01.ScienceServiceFrame("lofts",30,30));Assert.True(HeartPlanFR01.ScienceServiceFrame("forge",30,30));
            Assert.True(HeartPlanFR01.ScienceServiceFrame("archive",30,48));Assert.False(HeartPlanFR01.ScienceServiceFrame("archive",30,30));Assert.False(HeartPlanFR01.ScienceServiceFrame("lofts",31,30));
            Assert.False(HeartPlanFR01.ScienceServiceFrame("forge",float.NaN,30));
        }

        [Test] public void OrdinaryNightIsSeparateFromEarnedResonance()
        {
            Assert.Less(HeartPlanFR01.ScienceGlow(1,false,false),.5f);
            Assert.Greater(HeartPlanFR01.ScienceGlow(0,true,false),HeartPlanFR01.ScienceGlow(1,false,false)*4);
            Assert.AreEqual(HeartPlanFR01.ScienceGlow(1,false,true),HeartPlanFR01.ScienceGlow(1,true,true));
        }

        [TestCase(true)] [TestCase(false)]
        public void PreparationFailureCannotBecomeSuccessWhenOtherArchitectureIsActive(bool active)
        {
            FantasyProofSnapshotV1 proof=new FantasyProofSnapshotV1();
            proof.build=new FantasyBuildStampV1{gitCommit=new string('a',40)};
            proof.unityVersion="6000.3.16f1";proof.worldRevision=10;
            proof.fr01.prepared=false;proof.fr01.firstRefusal="FR01 prepared catalog missing";
            proof.worldRewrite.records=new[]{new FantasyRewriteRecordWR01{family="ScienceDistrictSF01",admitted=true,active=active,legacySuppressed=active}};
            string result=FantasyProofRulesV1.Summary(proof);
            StringAssert.Contains("COULD NOT LOAD",result);StringAssert.Contains("catalog missing",result);
            Assert.AreEqual(active,result.StartsWith("FANTASY WORLD PARTIAL"));
            Assert.False(proof.fr01.prepared);
        }

        [Test] public void ArchiveRetainsActualDaisGalleryAndRoofReceiptWithoutDisplacement()
        {
            VenueBinding04C binding=new VenueBinding04C{Definition=new VenueDefinition04C{Id="archive"},Building=new Building{Height=24,RoofPad04OB=true},Width=30,Depth=48};
            MeshBuilder structure=new MeshBuilder(1),mineral=new MeshBuilder(1);
            GothicCivicH5.Structure(structure,binding,mineral);
            ScienceDistrictArt art=HeartPlanFR01.ScienceService("archive",structure,new MeshBuilder(1));
            for(int i=0;i<structure.VertexCount;i++)Assert.AreEqual(structure.Vertices[i],art.Surfaces["masonry"].Vertices[i]);
            // The original room, stairs at z=36 and dais at z=43 remain. Test the level public approach before the stairs.
            MeshBuilder solid=art.Solid();
            for(float x=-1.3f;x<=1.3f;x+=.65f)for(float y=.3f;y<=2.4f;y+=.7f)
                Assert.False(Hits(solid,new Vector3(x,y,-9),Vector3.forward,43));
            Assert.True(Hits(solid,new Vector3(0,4,43.5f),Vector3.down,4),"Original dais remains physical.");
            Assert.True(Hits(solid,new Vector3(-11.75f,26,34.56f),Vector3.down,3),"Original roof receipt pad remains.");
        }

        [Test] public void ClearanceExcludesRetainedRoomsAndIncludesOldTallRoofs()
        {
            MeshBuilder before=new MeshBuilder(1),after=new MeshBuilder(1);
            before.Box(0,new Vector3(0,1,10),new Vector3(4,2,4),UVMode.World);after.Append(before);
            Assert.IsEmpty(HeartPlanFR01.ScienceChangedZones(before,after));
            before.Box(0,new Vector3(0,37,12),new Vector3(8,.4f,9),UVMode.World);
            after.Box(0,new Vector3(0,15,12),new Vector3(8,.4f,9),UVMode.World);
            List<Bounds> zones=HeartPlanFR01.ScienceChangedZones(before,after);
            Assert.True(zones.Exists(b=>Contains(b,new Vector3(0,37.35f,12))),"Feet above removed tall roof must wait.");
            Assert.True(zones.Exists(b=>Contains(b,new Vector3(0,15.35f,12))),"Restoration from new roof must wait.");
            Assert.False(zones.Exists(b=>Contains(b,new Vector3(0,1,10))),"Unchanged occupied room must not permanently veto preview.");
        }

        [Test] public void ApproachGeometryKeepsTheExistingConnectedRouteWalkingWidths()
        {
            ScienceDistrictArt art=new ScienceDistrictArt();HeartPlanFR01.ScienceApproach(art,null);MeshBuilder solid=art.Solid();
            Vector3[][] routes={new[]{new Vector3(75,0,96),new Vector3(74,0,78),new Vector3(42,0,68),new Vector3(12,0,66),new Vector3(12,0,92),new Vector3(-6,0,118)},
                new[]{new Vector3(-38,0,72),new Vector3(-38,0,92),new Vector3(-76,0,92)}};
            foreach(Vector3[] route in routes)for(int i=1;i<route.Length;i++)
            {
                Vector3 delta=route[i]-route[i-1],direction=delta.normalized,side=new Vector3(direction.z,0,-direction.x);
                for(float lane=-3.5f;lane<=3.5f;lane+=.7f)foreach(float y in new[]{.4f,1.2f,2.6f})
                    Assert.False(Hits(solid,route[i-1]+side*lane+Vector3.up*y,direction,delta.magnitude),"Existing route prism "+route[i-1]+" to "+route[i]);
            }
        }

        [Test] public void CanonicalDistrictAdmitsConnectedApproachSites()
        {
            CityLayout world=new CityLayout(61724);world.Generate();
            List<VenueBinding04C> venues=InhabitedCityBindings04C.Select(world,InhabitedCityBindings04CTests.ReadCatalog());
            Assert.True(world.ApplyProfile04O(CityRevision04O.GothicCivicH5,venues));
            ScienceDistrictArt art=new ScienceDistrictArt();HeartPlanFR01.ScienceApproach(art,world);
            ScienceDistrictArt expected=new ScienceDistrictArt();HeartPlanFR01.ScienceApproach(expected,null);
            MeshBuilder actual=art.Solid(),old=expected.Solid(),frontier=HeartPlanFR01.ScienceFrontier(world).Solid();
            MeshBuilder grotto=HeartPlanFR01.ScienceGrotto(world).Solid();
            Assert.AreEqual(old.VertexCount+frontier.VertexCount+grotto.VertexCount+HeartPlanFR01.ScienceCircuit(world).Solid().VertexCount,actual.VertexCount,"All three loggias, the frontier and the admitted grotto and circuit must survive integration.");
            HashSet<Vector3> retained=new HashSet<Vector3>(actual.Vertices);
            for(int i=0;i<old.VertexCount;i++)Assert.True(retained.Contains(old.Vertices[i]),"Retained loggia vertex");
            foreach(Vector3 p in frontier.Vertices)Assert.True(retained.Contains(p),"Retained frontier vertex");
        }

        [Test] public void AdmittedLoggiasKeepActualNeighbourRoutesOpen()
        {
            CityLayout world=new CityLayout(61724);world.Generate();
            List<VenueBinding04C> venues=InhabitedCityBindings04C.Select(world,InhabitedCityBindings04CTests.ReadCatalog());
            Assert.True(world.ApplyProfile04O(CityRevision04O.GothicCivicH5,venues));
            ScienceDistrictArt art=new ScienceDistrictArt();HeartPlanFR01.ScienceApproach(art,world);MeshBuilder solid=art.Solid();int samples=0;
            foreach(RealmRoute04OB route in world.RealmRoutes04OB)for(int i=1;i<route.Points.Count;i++)
            {
                Vector3 a=MathUtil.Flat(route.Points[i-1]),delta=MathUtil.Flat(route.Points[i]-a);if(delta.sqrMagnitude<.001f)continue;
                bool nearby=false;
                foreach(Vector3 site in HeartPlanFR01.ScienceApproachSites())
                {
                    float t=Mathf.Clamp01(Vector3.Dot(MathUtil.Flat(site)-a,delta)/delta.sqrMagnitude);
                    if(MathUtil.FlatDistance(site,a+delta*t)<20)nearby=true;
                }
                if(!nearby)continue;
                Vector3 direction=delta.normalized,side=new Vector3(direction.z,0,-direction.x);
                foreach(float lane in new[]{-route.Width*.5f+.35f,0,route.Width*.5f-.35f})foreach(float y in new[]{.4f,1.2f,2.6f})
                {
                    Assert.False(Hits(solid,a+side*lane+Vector3.up*y,direction,delta.magnitude),route.Id+" lane "+lane);samples++;
                }
            }
            Assert.Greater(samples,36,"Check actual neighbour paths as well as the main circuit.");
        }

        private static bool Contains(Bounds box,Vector3 point)
        { Vector3 low=box.min,high=box.max;return point.x>=low.x && point.x<=high.x && point.y>=low.y && point.y<=high.y && point.z>=low.z && point.z<=high.z; }

        private static ScienceDistrictArt Art(string id)
        { return id=="home-court"?HeartPlanFR01.ScienceCourt():HeartPlanFR01.ScienceService(id,new MeshBuilder(1),new MeshBuilder(1)); }

        private static bool Hits(MeshBuilder mesh,Vector3 origin,Vector3 direction,float length)
        {
            IReadOnlyList<int> indices=mesh.Triangles(0);
            for(int i=0;i<indices.Count;i+=3)
            {
                Vector3 a=mesh.Vertices[indices[i]],e1=mesh.Vertices[indices[i+1]]-a,e2=mesh.Vertices[indices[i+2]]-a;
                Vector3 p=Vector3.Cross(direction,e2);float determinant=Vector3.Dot(e1,p);if(Mathf.Abs(determinant)<1e-7f)continue;
                Vector3 t=origin-a;float u=Vector3.Dot(t,p)/determinant;if(u<0 || u>1)continue;
                Vector3 q=Vector3.Cross(t,e1);float v=Vector3.Dot(direction,q)/determinant;if(v<0 || u+v>1)continue;
                float d=Vector3.Dot(e2,q)/determinant;if(d>=0 && d<=length)return true;
            }
            return false;
        }
    }
}
