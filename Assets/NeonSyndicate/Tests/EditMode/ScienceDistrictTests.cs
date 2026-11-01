using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace NeonSyndicate.Tests
{
    public sealed class ScienceDistrictTests
    {
        [TestCase("lofts")] [TestCase("forge")] [TestCase("home-court")]
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

        [TestCase("lofts")] [TestCase("forge")] [TestCase("home-court")]
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
            Assert.False(HeartPlanFR01.ScienceServiceFrame("archive",30,48));Assert.False(HeartPlanFR01.ScienceServiceFrame("lofts",31,30));
            Assert.False(HeartPlanFR01.ScienceServiceFrame("forge",float.NaN,30));
        }

        [Test] public void OrdinaryNightIsSeparateFromEarnedResonance()
        {
            Assert.Less(HeartPlanFR01.ScienceGlow(1,false,false),.5f);
            Assert.Greater(HeartPlanFR01.ScienceGlow(0,true,false),HeartPlanFR01.ScienceGlow(1,false,false)*4);
            Assert.AreEqual(HeartPlanFR01.ScienceGlow(1,false,true),HeartPlanFR01.ScienceGlow(1,true,true));
        }

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
