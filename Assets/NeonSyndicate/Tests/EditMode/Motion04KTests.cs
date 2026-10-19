using NUnit.Framework;
using UnityEngine;

namespace NeonSyndicate.Tests
{
    public sealed class Motion04KTests
    {
        [Test]
        public void StrikePresentationSamplesTheSameActionTimeAtAllFrameRates()
        {
            foreach(float t in new[]{0f,.15f,.3f,.4f,.5f,.8f,1f})
            {
                float contact=MotionBeat04K.Contact(t,.3f,.2f);
                Assert.That(contact,Is.InRange(-.0001f,1.0001f));
                foreach(float fps in new[]{30f,60f,144f})
                    Assert.AreEqual(contact,MotionBeat04K.Contact(t,.3f,.2f),"Action progress owns the pose; FPS "+fps);
            }
            Assert.That(MotionBeat04K.Contact(.4f,.3f,.2f),Is.EqualTo(1f).Within(.0001f));
            Assert.AreEqual(0f,MotionBeat04K.Contact(float.NaN,.3f,.2f));
            Assert.AreEqual(0f,MotionBeat04K.Contact(.4f,float.PositiveInfinity,.2f));
            Assert.AreEqual(0f,MotionBeat04K.Recover(float.PositiveInfinity,.3f,.2f));
        }

        [Test]
        public void HeroProductionGeometryStaysSkinnedAndBudgeted()
        {
            Appearance a=Appearance.Malik();
            for(int detail=0;detail<=2;detail++)
            {
                RigLayout rig=RigLayout.From(a);BodyData d=BodyBuilder.Build(a,rig,detail);
                Assert.That(d.Vertices.Count,Is.LessThan(detail==2?14200:detail==1?8600:5000));
                Assert.AreEqual(d.Vertices.Count,d.Weights.Count);
                Assert.AreEqual(d.Vertices.Count,d.Normals.Count);
                foreach(BoneWeight w in d.Weights)
                    Assert.That(w.weight0+w.weight1+w.weight2+w.weight3,Is.EqualTo(1f).Within(.001f));
                foreach(int index in d.Triangles) Assert.That(index,Is.InRange(0,d.Vertices.Count-1));
            }
        }
    }
}
