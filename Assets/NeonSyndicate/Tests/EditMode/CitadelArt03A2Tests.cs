using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace NeonSyndicate.Tests
{
    /// <summary>Native EditMode tests to run in Unity. They are not represented as passed by the offline source audit.</summary>
    public sealed class CitadelArt03A2Tests
    {
        [Test]
        public void MeshesAreFiniteReadableAndHaveValidWinding()
        {
            using(CitadelKit03A2 kit=new CitadelKit03A2())
            {
                foreach(string id in CitadelGeometry03A2.ModelIds())for(int detail=0;detail<2;detail++)
                {
                    Mesh mesh=kit.Model(id,detail==1);
                    Assert.That(mesh.isReadable,Is.True,id);
                    Vector3[] v=mesh.vertices,n=mesh.normals;Vector2[] uv=mesh.uv;int[] t=mesh.triangles;
                    Assert.That(v.Length,Is.GreaterThan(0),id);
                    Assert.That(n.Length,Is.EqualTo(v.Length),id);Assert.That(uv.Length,Is.EqualTo(v.Length),id);
                    Assert.That(t.Length%3,Is.EqualTo(0),id);
                    for(int i=0;i<v.Length;i++)
                    {
                        Assert.That(Finite(v[i].x)&&Finite(v[i].y)&&Finite(v[i].z),Is.True,id);
                        Assert.That(Finite(uv[i].x)&&Finite(uv[i].y),Is.True,id);
                        Assert.That(n[i].sqrMagnitude,Is.InRange(.98f,1.02f),id);
                    }
                    for(int i=0;i<t.Length;i+=3)
                    {
                        for(int j=0;j<3;j++)Assert.That(t[i+j],Is.InRange(0,v.Length-1),id);
                        Vector3 cross=Vector3.Cross(v[t[i+1]]-v[t[i]],v[t[i+2]]-v[t[i]]);
                        Assert.That(cross.sqrMagnitude,Is.GreaterThanOrEqualTo(1e-12f),id);
                        Assert.That(Vector3.Dot(cross.normalized,n[t[i]]),Is.GreaterThan(.98f),id);
                    }
                    Assert.That(kit.Model(id,detail==1),Is.SameAs(mesh),id);
                }
            }
        }
        [Test]
        public void DistantMeshesNeverCostMore()
        {
            using(CitadelKit03A2 kit=new CitadelKit03A2())foreach(string id in CitadelGeometry03A2.ModelIds())
                Assert.That(kit.Model(id,true).vertexCount,Is.LessThanOrEqualTo(kit.Model(id,false).vertexCount),id);
        }
        [Test]
        public void HostileArtDoesNotRewriteOwnershipOrTheme()
        {
            StrongholdSiteSave state=new StrongholdSiteSave{Owned=false,Theme=1,Id="sunscar"};
            Assert.That(CitadelLayout03A2.Style("sunscar",state),Is.EqualTo(3));
            Assert.That(state.Owned,Is.False);Assert.That(state.Theme,Is.EqualTo(1));
            state.Owned=true;
            Assert.That(CitadelLayout03A2.Style("sunscar",state),Is.EqualTo(1));
        }
        [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)]
        public void OrnamentStaysInProbedPlotAndLeavesEntranceClear(int style)
        {
            List<FortSlotSave> slots=Slots(0,0);
            using(CitadelKit03A2 kit=new CitadelKit03A2())
            {
                List<CitadelPart03A2> parts=CitadelLayout03A2.Build(style,slots);
                Assert.That(parts.Count,Is.GreaterThan(60));
                foreach(CitadelPart03A2 part in parts)
                {
                    Matrix4x4 m=Matrix4x4.TRS(part.Position,Quaternion.Euler(part.Euler),part.Scale);
                    foreach(Vector3 v in kit.Model(part.Model,false).vertices)
                    {
                        Vector3 p=m.MultiplyPoint3x4(v);
                        Assert.That(Math.Abs(p.x),Is.LessThan(23f),part.Model);
                        Assert.That(Math.Abs(p.z),Is.LessThan(23f),part.Model);
                        Assert.That(p.y,Is.InRange(-.01f,16f),part.Model);
                        bool doorway=p.z > -17f && p.z < -11f && p.y < 2.2f && Math.Abs(p.x)<3f;
                        Assert.That(doorway,Is.False,part.Model);
                    }
                }
                foreach(FortSlotSave slot in slots){Assert.That(slot.Module,Is.EqualTo(0));Assert.That(slot.Rotation,Is.EqualTo(0));}
            }
        }
        [Test]
        public void EmptySocketsHaveNoSocketSpecificDecoration()
        {
            int empty=CitadelLayout03A2.Build(0,Slots(0,0)).Count;
            for(int module=1;module<=4;module++)
                Assert.That(CitadelLayout03A2.Build(0,Slots(module,0)).Count,Is.GreaterThan(empty));
        }
        [Test]
        public void AllModuleRotationsAreBoundedAndPreserveInput()
        {
            for(int module=0;module<=4;module++)for(int rotation=0;rotation<4;rotation++)
            {
                List<FortSlotSave> slots=Slots(module,rotation);
                List<CitadelPart03A2> parts=CitadelLayout03A2.Build(0,slots);
                foreach(CitadelPart03A2 part in parts)
                {
                    Assert.That(Finite(part.Position.x)&&Finite(part.Position.y)&&Finite(part.Position.z),Is.True);
                    Assert.That(part.Scale.x>0f&&part.Scale.y>0f&&part.Scale.z>0f,Is.True);
                }
                foreach(FortSlotSave slot in slots){Assert.That(slot.Module,Is.EqualTo(module));Assert.That(slot.Rotation,Is.EqualTo(rotation));}
            }
        }
        [Test]
        public void MaterialsHaveBoundedColors()
        {
            foreach(string role in new[]{"stone","trim","gilt","bark","leaf","cloth","crystal","bloom"})for(int style=0;style<4;style++)
            {
                Color c=CitadelKit03A2.Tint(style,role);
                Assert.That(c.r,Is.InRange(0f,1f));Assert.That(c.g,Is.InRange(0f,1f));Assert.That(c.b,Is.InRange(0f,1f));
            }
        }
        private static List<FortSlotSave> Slots(int module,int rotation)
        {
            List<FortSlotSave> result=new List<FortSlotSave>();
            for(int i=0;i<6;i++)result.Add(new FortSlotSave{Module=module,Rotation=rotation});return result;
        }
        private static bool Finite(float f){return !float.IsNaN(f)&&!float.IsInfinity(f);}
    }
}
