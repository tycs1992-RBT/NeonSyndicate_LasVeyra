using NUnit.Framework;
using UnityEngine;

namespace NeonSyndicate.Tests
{
    public sealed class Renaissance04HTests
    {
        [TestCase(WeaponId.VoltKatana,MeleeClass.Katana)]
        [TestCase(WeaponId.Switchblade,MeleeClass.Blade)]
        [TestCase(WeaponId.CrystalMaul,MeleeClass.Maul)]
        public void EntireActiveWindowAndPartitionedFramesAgree(WeaponId weapon,MeleeClass style)
        {
            foreach(MeleeMove move in MeleeMoves.Lights(style))
            for(int x=-4;x<=4;x++) for(int z=1;z<=7;z++)
            {
                Vector3 low=new Vector3(x*.4f,.35f,z*.4f),high=low+Vector3.up*1.1f;
                bool full=MeleeContact04H.Touches(weapon,move,0f,1f,false,low,high,.25f);
                bool split=false;
                for(int slice=0;slice<24;slice++) split|=MeleeContact04H.Touches(weapon,move,slice/24f,(slice+1)/24f,false,low,high,.25f);
                Assert.AreEqual(full,split,"A hitch must not lose or expand contact at "+low);
            }
        }
        [Test] public void BladeCannotHitOutsideThePhysicalReachOrBehindItsOwner()
        {
            MeleeMove move=MeleeMoves.Lights(MeleeClass.Katana)[0];
            Assert.IsFalse(MeleeContact04H.Touches(WeaponId.VoltKatana,move,0f,1f,false,new Vector3(0f,.3f,-2f),new Vector3(0f,1.6f,-2f),.3f));
            Assert.IsFalse(MeleeContact04H.Touches(WeaponId.VoltKatana,move,0f,1f,false,new Vector3(0f,.3f,20f),new Vector3(0f,1.6f,20f),.3f));
        }
        [Test] public void ContactDistanceHandlesParallelCrossingAndDegenerateSegments()
        {
            Assert.AreEqual(0f,MeleeContact04H.SegmentDistanceSq(Vector3.left,Vector3.right,Vector3.down,Vector3.up),.0001f);
            Assert.AreEqual(1f,MeleeContact04H.SegmentDistanceSq(Vector3.zero,Vector3.right,Vector3.up,Vector3.one),.0001f);
            Assert.AreEqual(1f,MeleeContact04H.SegmentDistanceSq(Vector3.zero,Vector3.zero,Vector3.up,Vector3.up),.0001f);
        }
        [Test] public void InjuredDrakeRetreatHasHysteresisAndCannotReviveOrHealThroughHits()
        {
            Assert.IsTrue(EventDrakeBehavior04H.Retreat(false,34f));
            Assert.IsTrue(EventDrakeBehavior04H.Retreat(true,44f));
            Assert.IsFalse(EventDrakeBehavior04H.Retreat(true,52f));
            Assert.AreEqual(0f,EventDrakeBehavior04H.Recovery(0f,.35f,10f));
            Assert.AreEqual(30f,EventDrakeBehavior04H.Recovery(30f,.35f,5.9f));
            Assert.AreEqual(33f,EventDrakeBehavior04H.Recovery(30f,99f,10f));
            Assert.AreEqual(60f,EventDrakeBehavior04H.Recovery(59f,.35f,10f));
        }
        [Test] public void QuietBeatHasAReadableWindowAndRejectsInvalidTime()
        {
            Assert.IsFalse(Attunement04H.Quiet(0f));
            Assert.IsTrue(Attunement04H.Quiet(.8f));
            Assert.IsTrue(Attunement04H.Quiet(1.6f));
            Assert.IsFalse(Attunement04H.Quiet(1.7f));
            Assert.AreEqual(1,Attunement04H.Cycle(2.6f));
            Assert.IsTrue(Attunement04H.Quiet(2.6f));
            Assert.IsFalse(Attunement04H.Quiet(float.NaN));
            Assert.IsFalse(Attunement04H.Quiet(float.PositiveInfinity));
        }
        [Test] public void RepairRecipeConsumesRealResourcesAndUsesExistingModifiers()
        {
            RecipeDef recipe=ForgeDb.Get("brew_stillwater_04h");
            Assert.AreEqual(RecipeKind.Brew,recipe.Kind);
            Assert.AreEqual(120,recipe.Cost.Cash);
            Assert.AreEqual(3,recipe.Cost.CountOf("shard_chunk"));
            Assert.AreEqual(1,recipe.Cost.CountOf("coolant_salt"));
            Assert.AreEqual(UnlockKind.QuestDone,recipe.Rules[0].Kind);
            Assert.AreEqual("uh05_04h_01_quiet_circuit",recipe.Rules[0].Arg);
            Assert.IsTrue(GuidanceRules04F.IsInteriorContract(recipe.Rules[0].Arg));
        }
        [TestCase(GraphicsPreset.Low)] [TestCase(GraphicsPreset.Medium)]
        [TestCase(GraphicsPreset.High)] [TestCase(GraphicsPreset.Ultra)]
        public void FacadeTraceryStaysInsideTheExistingEnvelopeAndBounded(GraphicsPreset preset)
        {
            MeshBuilder trim=new MeshBuilder(1),metal=new MeshBuilder(1);
            RectF f=new RectF(-10f,-8f,10f,8f);
            VisualRebirth04AArchitecture.Tracery04H(trim,metal,100,f,0f,12f,2,preset!=GraphicsPreset.Low,preset);
            Assert.That(trim.VertexCount+metal.VertexCount,Is.InRange(200,2400));
            foreach(MeshBuilder mesh in new[]{trim,metal}) foreach(Vector3 v in mesh.Vertices)
            {
                Assert.IsFalse(float.IsNaN(v.sqrMagnitude));
                Assert.That(v.y,Is.InRange(2f,10f),"Ground routes and roof remain clear");
                Assert.That(v.x,Is.InRange(-10.12f,10.12f));
                Assert.That(v.z,Is.InRange(-8.12f,8.12f));
            }
        }
        [Test] public void RenaissanceRetainsHeroBudgetAndValidNormals()
        {
            Appearance look=Appearance.Malik();
            BodyData body=BodyBuilder.Build(look,RigLayout.From(look),2);
            Assert.Less(body.Vertices.Count,13500);
            for(int i=0;i<body.Vertices.Count;i++)
            {
                Assert.IsFalse(float.IsNaN(body.Vertices[i].sqrMagnitude));
                Assert.That(body.Normals[i].sqrMagnitude,Is.InRange(.95f,1.05f));
                BoneWeight w=body.Weights[i];
                Assert.AreEqual(1f,w.weight0+w.weight1+w.weight2+w.weight3,.001f);
            }
        }
    }
}
