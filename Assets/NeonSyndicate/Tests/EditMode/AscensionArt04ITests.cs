using NUnit.Framework;
using UnityEngine;

namespace NeonSyndicate.Tests
{
    public sealed class AscensionArt04ITests
    {
        [TestCase("lofts")] [TestCase("dispatch")] [TestCase("pressure")] [TestCase("bondkeeper")]
        public void ArrivalArchitectureKeepsFloorRoutesAndBounds(string id)
        {
            foreach(GraphicsPreset preset in new[]{GraphicsPreset.Low,GraphicsPreset.Medium,GraphicsPreset.High,GraphicsPreset.Ultra})
            {
                MeshBuilder a=new MeshBuilder(1),b=new MeshBuilder(1),c=new MeshBuilder(1);
                CityAscensionGeometry04I.Venue(id,a,b,c,true,preset);
                Assert.That(a.VertexCount+b.VertexCount+c.VertexCount,Is.InRange(1000,6500));
                foreach(MeshBuilder mesh in new[]{a,b,c})
                for(int i=0;i<mesh.VertexCount;i++)
                {
                    Vector3 p=mesh.Vertices[i];
                    Assert.IsFalse(float.IsNaN(p.sqrMagnitude));
                    Assert.That(p.x,Is.InRange(-7.8f,7.8f));
                    Assert.That(p.z,Is.InRange(-.8f,17f));
                    Assert.That(p.y,Is.InRange(.3f,4.9f));
                    if(Mathf.Abs(p.x)<1.5f && p.z<.3f) Assert.GreaterOrEqual(p.y,3.03f,"Original doorway stays clear");
                    if(Mathf.Abs(p.x)<7.3f && p.z>.3f) Assert.GreaterOrEqual(p.y,3.1f,"Original floor, stair and NPC routes stay clear");
                    Assert.That(mesh.Normals[i].sqrMagnitude,Is.InRange(.99f,1.01f));
                }
            }
        }
        [TestCase("lofts")] [TestCase("dispatch")] [TestCase("pressure")] [TestCase("bondkeeper")]
        public void DetailedCivicSigilsStayWithinExistingNearMeshBudget(string id)
        {
            MeshBuilder a=new MeshBuilder(1),b=new MeshBuilder(1),c=new MeshBuilder(1);
            CityAscensionGeometry04I.Venue(id,a,b,c,true,GraphicsPreset.Ultra);
            int detailed=a.VertexCount+b.VertexCount+c.VertexCount;
            Assert.That(detailed,Is.InRange(4000,6500));
            MeshBuilder lowA=new MeshBuilder(1),lowB=new MeshBuilder(1),lowC=new MeshBuilder(1);
            CityAscensionGeometry04I.Venue(id,lowA,lowB,lowC,false,GraphicsPreset.Ultra);
            Assert.Greater(detailed,lowA.VertexCount+lowB.VertexCount+lowC.VertexCount);
            Assert.GreaterOrEqual(Mathf.Min(a.Vertices[0].y,b.Vertices[0].y),.3f);
        }

        [TestCase("quartz_guide")] [TestCase("ash_warden")] [TestCase("storm_skimmer")]
        public void EveryCompanionStageRetainsSkinStreamsAndBoneBounds(string id)
        {
            for(int evo=0;evo<=2;evo++)
            for(int detail=0;detail<=2;detail++)
            {
                CreatureBody body=CompanionBodyFactory.BuildNow(id,61724,evo,"",detail);
                BodyData d=body.Mesh; int n=d.Vertices.Count;
                Assert.That(n,Is.InRange(500,60000));
                Assert.AreEqual(n,d.Weights.Count); Assert.AreEqual(n,d.Normals.Count);
                Assert.AreEqual(n,d.Palette.Count); Assert.AreEqual(n,d.Detail.Count); Assert.AreEqual(n,d.Tangents.Count);
                for(int i=0;i<n;i++)
                {
                    Assert.IsFalse(float.IsNaN(d.Vertices[i].sqrMagnitude));
                    BoneWeight w=d.Weights[i];
                    Assert.That(w.weight0+w.weight1+w.weight2+w.weight3,Is.EqualTo(1f).Within(.001f));
                    Assert.That(w.boneIndex0,Is.InRange(0,body.Skeleton.Count-1));
                }
                foreach(int index in d.Triangles) Assert.That(index,Is.InRange(0,n-1));
            }
        }
    }
}
