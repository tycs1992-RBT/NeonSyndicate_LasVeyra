using NUnit.Framework;
using UnityEngine;

namespace NeonSyndicate.Tests
{
    public sealed class AscensionCombat04ITests
    {
        [TestCase(30)] [TestCase(60)] [TestCase(144)] [TestCase(240)]
        public void WalkingCompanionDetectsAnObstructionAtEveryFrameRate(int fps)
        {
            StuckMeter meter=new StuckMeter(); float dt=1f/fps;
            for(int i=0;i<fps*4;i++) meter.Update(0f,.8f*dt,dt);
            Assert.GreaterOrEqual(meter.Attempts,3);
            Assert.IsTrue(meter.ShouldRecall);
            for(int i=0;i<fps*2;i++) meter.Update(.8f*dt,.8f*dt,dt);
            Assert.AreEqual(0,meter.Attempts);
        }
        [TestCase(WeaponId.VoltKatana,MeleeClass.Katana)]
        [TestCase(WeaponId.Switchblade,MeleeClass.Blade)]
        [TestCase(WeaponId.CrystalMaul,MeleeClass.Maul)]
        public void ContactWitnessIsFiniteAndAllFramePartitionsAgree(WeaponId weapon,MeleeClass style)
        {
            foreach(MeleeMove move in MeleeMoves.Lights(style))
            foreach(int fps in new[]{30,60,144,240})
            {
                Vector3 low=new Vector3(0f,.3f,move.Range*.8f),high=low+Vector3.up*1.2f;
                bool full=MeleeContact04H.Touches(weapon,move,0f,1f,false,low,high,.3f),split=false;
                for(int i=0;i<fps;i++)
                {
                    Vector3 h,c;
                    bool hit=MeleeContact04H.Contact04I(weapon,move,(float)i/fps,(float)(i+1)/fps,false,low,high,.3f,out h,out c);
                    split|=hit;
                    if(hit) {Assert.IsFalse(float.IsNaN(c.sqrMagnitude));Assert.Less((c-h).magnitude,move.Range+1f);}
                }
                Assert.AreEqual(full,split);
            }
        }
        [Test] public void ContactRejectsInvalidWindowsWithoutProducingAWitness()
        {
            Vector3 h,c; MeleeMove m=MeleeMoves.Lights(MeleeClass.Katana)[0];
            Assert.IsFalse(MeleeContact04H.Contact04I(WeaponId.VoltKatana,m,0f,float.PositiveInfinity,false,Vector3.zero,Vector3.up,.3f,out h,out c));
            Assert.AreEqual(Vector3.zero,c);
        }
    }
}
