using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
namespace NeonSyndicate.Tests
{
    public sealed class CrystalKingdom04NTests
    {
        [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)]
        public void DistrictGeometryHasNearFarDetailsAndLeavesHeadroom(int identity)
        {
            int previous=0;
            foreach(bool near in new[]{false,true})
            {
                var a=new MeshBuilder(1);var b=new MeshBuilder(1);var c=new MeshBuilder(1);
                CityKingdomGeometry04N.Frontage(a,b,c,new RectF(-12,-12,12,12),0,12,identity,near);
                int count=a.VertexCount+b.VertexCount+c.VertexCount;
                Assert.That(count,Is.GreaterThan(previous).And.LessThan(6500));previous=count;
                foreach(var m in new[]{a,b,c})foreach(var v in m.Vertices)
                {Assert.That(v.y,Is.InRange(2.8f,11f));Assert.That(Mathf.Abs(v.x),Is.LessThan(12.6f));Assert.That(Mathf.Abs(v.z),Is.LessThan(12.6f));}
            }
        }
        [Test] public void DistrictsUseFourDifferentStructures()
        {
            var counts=new HashSet<string>();
            foreach(var d in new[]{District.LasVeyraCore,District.SunkenRow,District.OldSilver,District.GraveFrequency})
            {
                var a=new MeshBuilder(1);var b=new MeshBuilder(1);var c=new MeshBuilder(1);
                CityKingdomGeometry04N.Frontage(a,b,c,new RectF(-12,-12,12,12),0,12,CityKingdomGeometry04N.Identity(d),true);
                counts.Add(string.Join(";",a.Vertices)+"/"+string.Join(";",b.Vertices)+"/"+string.Join(";",c.Vertices));
            }
            Assert.AreEqual(4,counts.Count);
        }
        [Test] public void HeroTailoringKeepsOriginalRigAndBodyBudget()
        {
            var a=Appearance.Malik();var d=BodyBuilder.Build(a,RigLayout.From(a),2);
            Assert.That(d.Vertices.Count,Is.InRange(13000,13500));Assert.AreEqual(d.Vertices.Count,d.Weights.Count);
            foreach(var w in d.Weights)Assert.That(w.boneIndex0,Is.LessThan(RigLayout.BoneCount));
        }
    }
}
