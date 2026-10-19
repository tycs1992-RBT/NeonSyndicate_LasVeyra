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
        [Test]
        public void EightGangSilhouettesAreDistinctWithoutChangingPaletteOrHostility()
        {
            var signatures=new System.Collections.Generic.HashSet<string>();
            for(int i=0;i<8;i++)
            {
                float heat=Gangs.Heat[i],floor=Gangs.MinHeat[i];
                Appearance a=Gangs.Look(Gangs.All[i],new System.Random(61724));
                Assert.AreEqual(Gangs.All[i].ColorRow,a.ShirtRow);Assert.AreEqual(Gangs.All[i].ColorCol,a.Shirt);
                signatures.Add(a.Jacket+"/"+a.LongSleeves+"/"+a.Headwear+"/"+a.Glasses+"/"+a.SuitStyle+"/"+a.HairStyle);
                Assert.AreEqual(heat,Gangs.Heat[i]);Assert.AreEqual(floor,Gangs.MinHeat[i]);
                Assert.That(LivingWorldRules04M.TradeDescription(i).Length,Is.GreaterThan(50));
            }
            Assert.AreEqual(8,signatures.Count);
        }
        [Test]
        public void NeighborhoodAidCannotBypassMissionCombatWantedOrStoryFloors()
        {
            Assert.IsTrue(LivingWorldRules04M.Parley(false,false,false,false,0,0));
            Assert.IsFalse(LivingWorldRules04M.Parley(true,false,false,false,0,0));
            Assert.IsFalse(LivingWorldRules04M.Parley(false,true,false,false,0,0));
            Assert.IsFalse(LivingWorldRules04M.Parley(false,false,true,false,0,0));
            Assert.IsFalse(LivingWorldRules04M.Parley(false,false,false,true,0,0));
            Assert.IsFalse(LivingWorldRules04M.Parley(false,false,false,false,.61f,0));
            Assert.IsFalse(LivingWorldRules04M.Parley(false,false,false,false,0,.85f));
        }
        [TestCase(0)] [TestCase(1)] [TestCase(2)]
        public void ResidentShiftsCycleWorkBreakRestAndShelterResumesWithoutServiceOwnership(int seed)
        {
            var phases=new System.Collections.Generic.HashSet<ResidentShift04M>();
            for(int hour=0;hour<24;hour++)
            {
                var shift=LivingWorldRules04M.Shift(hour,seed,false);phases.Add(shift);
                Assert.AreEqual(shift,LivingWorldRules04M.Shift(hour+24,seed,false));
                Assert.AreEqual(ResidentShift04M.Shelter,LivingWorldRules04M.Shift(hour,seed,true));
            }
            Assert.IsTrue(phases.Contains(ResidentShift04M.Work));Assert.IsTrue(phases.Contains(ResidentShift04M.Break));Assert.IsTrue(phases.Contains(ResidentShift04M.Rest));
        }
    }
}
