using NUnit.Framework;
using UnityEngine;

namespace NeonSyndicate.Tests
{
    public sealed class CreatureTactics04LTests
    {
        private static CreatureAttack Move(AttackShape shape,AttackUse use=AttackUse.Ground)
        {return new CreatureAttack { Shape=shape,Use=use,MaxRange=10f,Weight=1f };}
        [Test]
        public void TacticsNeverBypassesCooldownRangeFacingOrFlightEligibility()
        {
            var set=new[]{Move(AttackShape.Line),Move(AttackShape.Cone,AttackUse.Air),Move(AttackShape.Ring)};
            for(int i=0;i<100;i++)
            {
                int n=CreatureTactics04L.Pick(set,new[]{100f,0f,0f},1f,2f,90f,AttackUse.Ground,i/99f,-1,9f);
                Assert.AreEqual(2,n);
            }
            Assert.AreEqual(-1,CreatureTactics04L.Pick(set,null,1f,99f,0,AttackUse.Ground,.5f,-1,0));
        }
        [Test]
        public void CirclingFavoursEligibleSweepAndDiscouragesRepeatedRushWithoutRemovingIt()
        {
            var set=new[]{Move(AttackShape.Line),Move(AttackShape.Ring)};
            int rush=0,sweep=0;
            for(int i=0;i<1000;i++)
            {
                int n=CreatureTactics04L.Pick(set,null,1,2,0,AttackUse.Ground,(i+.5f)/1000f,0,8);
                if(n==0)rush++;else if(n==1)sweep++;
            }
            Assert.That(sweep,Is.GreaterThan(rush*5));Assert.That(rush,Is.GreaterThan(0));
            Assert.AreEqual(1f,set[0].Weight);Assert.AreEqual(1f,set[1].Weight);
        }
        [Test]
        public void EveryStorySetContainsBoundedPhysicalGeometry()
        {
            foreach(SagaSet04L set in System.Enum.GetValues(typeof(SagaSet04L)))
            {
                MeshBuilder mesh=StoryStage04L.Geometry(set);
                Assert.That(mesh.Vertices.Count,Is.GreaterThan(40).And.LessThan(1500));
                foreach(Vector3 v in mesh.Vertices)Assert.That(v.magnitude,Is.LessThan(20f));
            }
        }
        [Test]
        public void ArrivalTraceryIsCheaperAtLowDetailAndNeverEntersDoorHeight()
        {
            int previous=0;
            foreach(bool detailed in new[]{false,true})
            {
                MeshBuilder stone=new MeshBuilder(1),metal=new MeshBuilder(1);
                VisualRebirth04AArchitecture.ArrivalTracery04L(stone,metal,new RectF(-9,-7,9,7),0,15,detailed);
                int total=stone.VertexCount+metal.VertexCount;
                Assert.That(total,Is.GreaterThan(previous).And.LessThan(3000));previous=total;
                foreach(var mesh in new[]{stone,metal})
                    foreach(Vector3 v in mesh.Vertices)Assert.That(v.y,Is.GreaterThan(6f));
            }
            Assert.IsFalse(StoryMissions.HasContact04L("Luma"));
            Assert.IsFalse(StoryMissions.HasContact04L("Shard Warden"));
            Assert.IsFalse(StoryMissions.HasContact04L("Mara Solane (phone)"));
        }
    }
}
