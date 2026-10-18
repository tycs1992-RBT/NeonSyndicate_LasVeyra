using NUnit.Framework;
using UnityEngine;

namespace NeonSyndicate.Tests
{
    public sealed class AscensionVehicle04ITests
    {
        [TestCase(false)] [TestCase(true)]
        public void CountersteerCorrectsSlidesWithoutOverridingTheDriver(bool bike)
        {
            Assert.Less(CityDrive04I.SteerIntent(0f,15f,5f,bike,false,.7f),0f);
            Assert.Greater(CityDrive04I.SteerIntent(0f,-15f,5f,bike,false,.7f),0f);
            Assert.AreEqual(.8f,CityDrive04I.SteerIntent(.8f,15f,5f,bike,false,.7f),.0001f);
            Assert.AreEqual(.2f,CityDrive04I.SteerIntent(.2f,15f,5f,bike,true,.7f),.0001f);
            Assert.AreEqual(0f,CityDrive04I.SteerIntent(0f,1f,5f,bike,false,.7f),.0001f);
            Assert.That(Mathf.Abs(CityDrive04I.SteerIntent(0f,20f,100f,bike,false,.7f)),Is.LessThanOrEqualTo(.18f));
        }
        [Test] public void RecoveryAssistFadesAndRejectsNonfinitePhysicsState()
        {
            float normal=CityDrive04I.SteerIntent(0f,15f,4f,false,false,0f);
            float recovering=CityDrive04I.SteerIntent(0f,15f,4f,false,false,.7f);
            Assert.Greater(Mathf.Abs(recovering),Mathf.Abs(normal));
            Assert.AreEqual(0f,CityDrive04I.SteerIntent(float.NaN,15f,4f,false,false,0f));
            Assert.AreEqual(0f,CityDrive04I.SteerIntent(0f,float.PositiveInfinity,4f,false,false,0f));
        }
    }
}
