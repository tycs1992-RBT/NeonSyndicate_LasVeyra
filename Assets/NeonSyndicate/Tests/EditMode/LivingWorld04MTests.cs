using NUnit.Framework;
using UnityEngine;
namespace NeonSyndicate.Tests
{
    public sealed class LivingWorld04MTests
    {
        [Test]
        public void CorridorUsesActualEndpointsAndDoesNotDependOnAltitude()
        {
            Assert.IsTrue(CityLivingGeometry04M.InCorridor(new Vector3(50,90,5),Vector3.zero,new Vector3(100,0,0),10));
            Assert.IsFalse(CityLivingGeometry04M.InCorridor(new Vector3(50,0,11),Vector3.zero,new Vector3(100,0,0),10));
            Assert.IsFalse(CityLivingGeometry04M.InCorridor(new Vector3(120,0,0),Vector3.zero,new Vector3(100,0,0),10));
        }
        [TestCase(false)] [TestCase(true)]
        public void StorefrontDetailsAreBoundedAndFarMeshIsCheaper(bool canal)
        {
            int previous=0;
            foreach(bool near in new[]{false,true})
            {
                var a=new MeshBuilder(1);var b=new MeshBuilder(1);var c=new MeshBuilder(1);
                CityLivingGeometry04M.Frontage(a,b,c,new RectF(-9,-9,9,9),0,10,near,canal);
                int count=a.VertexCount+b.VertexCount+c.VertexCount;
                Assert.That(count,Is.GreaterThan(previous).And.LessThan(8000));previous=count;
                foreach(var m in new[]{a,b,c})foreach(var v in m.Vertices)
                {Assert.That(v.y,Is.InRange(.1f,5.3f));Assert.That(Mathf.Abs(v.x),Is.LessThan(10f));Assert.That(Mathf.Abs(v.z),Is.LessThan(10f));}
            }
        }
        [TestCase("dispatch")] [TestCase("pressure")] [TestCase("bondkeeper")]
        public void ThreeWorkingRoomsKeepAllNewEquipmentOutsideTheirWalkingAndStairVolumes(string id)
        {
            var a=new MeshBuilder(1);var b=new MeshBuilder(1);var c=new MeshBuilder(1);
            CityLivingGeometry04M.Room(id,a,b,c,true);
            Assert.That(a.VertexCount+b.VertexCount+c.VertexCount,Is.GreaterThan(300).And.LessThan(2500));
            foreach(var m in new[]{a,b,c})foreach(var v in m.Vertices)
            {Assert.That(Mathf.Abs(v.x),Is.GreaterThan(7.2f));Assert.That(v.z,Is.InRange(3f,15f));Assert.That(v.y,Is.InRange(1.3f,3.2f));}
        }
    }
}
