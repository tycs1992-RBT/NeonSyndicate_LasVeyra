using NUnit.Framework;
using UnityEngine;
namespace NeonSyndicate.Tests
{
    public sealed class KingdomContact04NTests
    {
        private static BladeSweep04N Crossing()
        {return BladeSweep04N.Clip(new Vector3(-1,1,0),new Vector3(-1,1,1),new Vector3(1,1,0),new Vector3(1,1,1),.05f,0,1);}
        [Test] public void ATargetBetweenTwoActualPoseSamplesIsHitWithoutAFrontalCone()
        {
            Vector3 h,p;Assert.IsTrue(Crossing().Contact(new Vector3(0,.5f,.5f),new Vector3(0,1.5f,.5f),.15f,out h,out p));
            Assert.That(Mathf.Abs(p.x),Is.LessThan(.22f));
        }
        [Test] public void ATargetInsideABroadMeleeConeButOutsideThePhysicalBladeIsRejected()
        {Vector3 h,p;Assert.IsFalse(Crossing().Contact(new Vector3(0,.5f,2.5f),new Vector3(0,1.5f,2.5f),.15f,out h,out p));}
        [Test] public void WindupAndRecoveryTravelCanBeClippedAwayFromTheDamageWindow()
        {
            var full=Crossing();var clipped=BladeSweep04N.Clip(full.PreviousHilt,full.PreviousTip,full.Hilt,full.Tip,.05f,.6f,.8f);Vector3 h,p;
            Assert.IsTrue(full.Contact(new Vector3(-.7f,.5f,.5f),new Vector3(-.7f,1.5f,.5f),.08f,out h,out p));
            Assert.IsFalse(clipped.Contact(new Vector3(-.7f,.5f,.5f),new Vector3(-.7f,1.5f,.5f),.08f,out h,out p));
        }
        [Test] public void VerticalSeparationAndWorldTranslationPreserveTheCollisionResult()
        {
            var s=Crossing();Vector3 h,p;Assert.IsFalse(s.Contact(new Vector3(0,3,.5f),new Vector3(0,4,.5f),.2f,out h,out p));
            Vector3 shift=new Vector3(50,-60,30);s.PreviousHilt+=shift;s.PreviousTip+=shift;s.Hilt+=shift;s.Tip+=shift;
            Assert.IsTrue(s.Contact(new Vector3(0,.5f,.5f)+shift,new Vector3(0,1.5f,.5f)+shift,.15f,out h,out p));
        }
        [TestCase(WeaponId.VoltKatana,false)] [TestCase(WeaponId.Switchblade,true)]
        public void ContactEndpointsMatchTheAuthoredModelBladeLengths(WeaponId id,bool dagger)
        {
            var b=new MeshBuilder(3);WeaponModels.BuildBlade04F(b,dagger);Vector3 h,t;float radius;Assert.IsTrue(BladeSweep04N.Local(id,out h,out t,out radius));
            float end=0;foreach(var v in b.Vertices)end=Mathf.Max(end,v.z);Assert.AreEqual(end,t.z,.0001f);Assert.That(radius,Is.InRange(.03f,.1f));
        }
        [Test] public void TeleportsAndStaleOrReversedHistoryNeverBecomeLongBladeStrikes()
        {Assert.IsTrue(BladeSweep04N.HistoryValid(.016f,.3f));Assert.IsFalse(BladeSweep04N.HistoryValid(.21f,.3f));Assert.IsFalse(BladeSweep04N.HistoryValid(.016f,30));Assert.IsFalse(BladeSweep04N.HistoryValid(-1,.3f));Assert.IsFalse(BladeSweep04N.HistoryValid(float.NaN,.3f));}
        [TestCase(CrystalNpcRole04F.BladeWarden)] [TestCase(CrystalNpcRole04F.WardSentinel)] [TestCase(CrystalNpcRole04F.FocusWeaver)] [TestCase(CrystalNpcRole04F.RiftDuelist)] [TestCase(CrystalNpcRole04F.Marshal)]
        public void HumanWithdrawalIsOneFiniteOpportunityWithASharedSideLimit(CrystalNpcRole04F role)
        {
            Assert.IsTrue(HumanWithdrawal04N.Admit(role,.1f,false,false));Assert.IsFalse(HumanWithdrawal04N.Admit(role,.1f,true,false));
            Assert.IsFalse(HumanWithdrawal04N.Admit(role,.1f,false,true));Assert.IsFalse(HumanWithdrawal04N.Admit(role,.8f,false,false));Assert.IsFalse(HumanWithdrawal04N.Admit(role,0,false,false));
            Assert.That(HumanWithdrawal04N.Distance(role),Is.InRange(2.5f,6f));
        }
    }
}
