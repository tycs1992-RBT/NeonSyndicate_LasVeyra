using NUnit.Framework;
using UnityEngine;

namespace NeonSyndicate.Tests
{
    public sealed class SagaDirection04LTests
    {
        [TestCase("m00_broken_map",0)] [TestCase("m01_heat_mirrors",1)] [TestCase("m02_ghost_lanes",2)]
        [TestCase("m03_false_kings",3)] [TestCase("m04_flooded_grid",4)] [TestCase("m05_ashline_treaty",5)]
        [TestCase("m06_nocturne_market",6)] [TestCase("m07_glass_saints",7)] [TestCase("m08_signal_war",8)]
        [TestCase("m09_crownless_mile",9)] [TestCase("m10_long_switch",10)] [TestCase("m11_sunrise_debt",11)]
        [TestCase("uh05_04l_01_veil_memory",-1)] [TestCase(null,-1)]
        public void OriginalChapterIdentityIsReadWithoutChangingProgress(string id, int expected)
        { Assert.AreEqual(expected, SagaDirection04L.Chapter(id)); }

        [TestCase("Ion Solane (phone)")] [TestCase("Dante Rook (radio)")] [TestCase("Third Eye")]
        public void RemoteSpeakersNeverResolveToAnInPersonPerformance(string name)
        { Assert.IsTrue(SagaDirection04L.Remote(name)); }

        [Test]
        public void ReadingTimeAccommodatesLongLinesAndHasAFiniteLimit()
        {
            Assert.That(SagaDirection04L.ReadSeconds(new string('x',180)),Is.GreaterThan(10f));
            Assert.AreEqual(14f,SagaDirection04L.ReadSeconds(new string('x',9000)));
            Assert.That(SagaDirection04L.ReadSeconds(null),Is.GreaterThanOrEqualTo(2.5f));
        }
        [Test]
        public void ReducedCameraHasNoTimeDependentDriftForEveryShot()
        {
            foreach(SagaShot04L shot in System.Enum.GetValues(typeof(SagaShot04L)))
            {
                Vector3 p1,l1,p2,l2;float f1,f2;
                SagaDirection04L.Frame(new Vector3(0,1.7f,0),new Vector3(0,1.2f,3),Vector3.forward,shot,0,true,out p1,out l1,out f1);
                SagaDirection04L.Frame(new Vector3(0,1.7f,0),new Vector3(0,1.2f,3),Vector3.forward,shot,100,true,out p2,out l2,out f2);
                Assert.AreEqual(p1,p2);Assert.AreEqual(l1,l2);Assert.That(f1,Is.InRange(40f,55f));
            }
        }
        [TestCase(0f)] [TestCase(57f)] [TestCase(180f)] [TestCase(271f)]
        public void ReverseCloseAndShoulderShotsFrameTheSpeakerWithoutCrossingTheAxis(float yaw)
        {
            Vector3 player=new Vector3(5,1.7f,8);
            Vector3 axis=MathUtil.DirFromYaw(yaw);
            Vector3 actor=player+axis*3f-Vector3.up*.4f;
            Vector3 side=new Vector3(axis.z,0,-axis.x);
            foreach(SagaShot04L shot in new[]{SagaShot04L.Close,SagaShot04L.Shoulder})
                foreach(bool malik in new[]{false,true})
                {
                    Vector3 position,look;float fov;
                    SagaDirection04L.Frame(player,actor,axis,shot,0,true,out position,out look,out fov,malik);
                    Vector3 subject=malik?player:actor;
                    Vector3 listener=malik?actor:player;
                    Assert.That((look-subject).magnitude,Is.LessThan(.09f),"the speaking character fills the shot");
                    Assert.That(Vector3.Dot(position-(player+actor)*.5f,side),Is.GreaterThan(.7f),"both angles stay on the same side");
                    Assert.That((position-(shot==SagaShot04L.Close?subject:listener)).magnitude,Is.LessThan(2f));
                }
        }
        [Test]
        public void ChildGestureRemainsCloseToBodyAndAllSamplesAreBounded()
        {
            foreach(SagaEmotion04L mood in System.Enum.GetValues(typeof(SagaEmotion04L)))
                for(int i=0;i<=600;i++)
                {
                    Vector3 c,h,u,f;SagaDirection04L.Acting(mood,i/60f,true,true,out c,out h,out u,out f);
                    Assert.That(u.magnitude,Is.LessThan(45f));Assert.That(f.x,Is.InRange(-90f,-70f));
                    Assert.That(h.magnitude,Is.LessThan(15f));Assert.That(c.magnitude,Is.LessThan(10f));
                }
        }
        [Test]
        public void AcknowledgementsDoNotKnowFutureRescuesOrTransmitterOutcome()
        {
            Assert.IsNull(SagaDirection04L.Acknowledgement("Ion Solane",8));
            Assert.IsNotNull(SagaDirection04L.Acknowledgement("Ion Solane",9));
            Assert.That(SagaDirection04L.Acknowledgement("Mara Solane",4),Does.Not.Contain("couriers"));
            Assert.That(SagaDirection04L.Acknowledgement("Mara Solane",5),Does.Contain("couriers"));
        }
        [Test]
        public void FaceShapesTouchOnlySemanticFeaturesAndKeepOriginalRigAndBudgets()
        {
            foreach (Appearance a in new[] { Appearance.Malik(), Appearance.Luma() })
            {
                BodyData d=BodyBuilder.Build(a,RigLayout.From(a),2);
                Assert.AreEqual(3,d.FaceShapes04L.Count);
                Assert.That(d.Vertices.Count,Is.LessThan(13500));
                foreach(var shape in d.FaceShapes04L)
                {
                    Assert.AreEqual(d.Vertices.Count,shape.Value.Length);
                    int moved=0;
                    for(int n=0;n<shape.Value.Length;n++)
                    {
                        Vector3 delta=shape.Value[n];
                        Assert.That(delta.magnitude,Is.LessThan(.025f));
                        if(delta.sqrMagnitude<=0f)continue;
                        moved++;Assert.AreEqual(RigLayout.Head,d.Weights[n].boneIndex0);
                    }
                    Assert.That(moved,Is.GreaterThan(4).And.LessThan(300));
                }
                Assert.IsEmpty(BodyBuilder.Build(a,RigLayout.From(a),0).FaceShapes04L);
            }
        }
    }
}
