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
        [Test] public void CantorHasARealRangedTokenAttackAndIndependentCooldowns()
        {
            var a=CrystalCantor04N.Attacks();var b=CrystalCantor04N.Attacks();
            Assert.AreEqual(AttackShape.Projectile,a[0].Shape);Assert.IsFalse(a[0].Melee);
            Assert.That(a[0].Windup,Is.GreaterThanOrEqualTo(1f));Assert.That(a[0].Recovery,Is.GreaterThan(.8f));
            Assert.That(a[0].Cooldown,Is.GreaterThan(a[0].Total));Assert.That(a[0].MinRange,Is.GreaterThan(a[1].MaxRange));
            a[0].Damage=999;Assert.AreNotEqual(a[0].Damage,b[0].Damage);
            Assert.AreEqual("hollow_shade",LivingEcology04M.MusterSpecies(1));
            Assert.That(CreatureSpeciesDb.Get("hollow_shade").Attacks[0].Shape,Is.Not.EqualTo(AttackShape.Projectile));
        }
        [TestCase(DamageType.Bullet)] [TestCase(DamageType.Melee)] [TestCase(DamageType.Power)]
        public void CantorCanOnlyBeInterruptedByRealDamageBeforeCommit(DamageType type)
        {
            Assert.IsTrue(CrystalCantor04N.Interrupt(true,0,1,type));
            Assert.IsFalse(CrystalCantor04N.Interrupt(true,1,1,type));
            Assert.IsFalse(CrystalCantor04N.Interrupt(true,0,0,type));
            Assert.IsFalse(CrystalCantor04N.Interrupt(false,0,1,type));
        }
        [Test] public void StreetGroundAllowsPavementBelowAwningButRejectsRoofsAndUnderworld()
        {
            Assert.IsFalse(CityRiftGround04N.Eligible(6f,1));Assert.IsTrue(CityRiftGround04N.Eligible(.2f,1));
            Assert.IsFalse(CityRiftGround04N.Eligible(-50f,1));Assert.IsFalse(CityRiftGround04N.Eligible(.2f,.4f));
            Assert.IsFalse(CityRiftGround04N.Eligible(float.NaN,1));
        }
        [Test] public void AmbientAdmissionCountsExistingPersistentMusterBodiesWithoutDoubleCountingAmbientBodies()
        {
            Assert.AreEqual(2,CreatureAdmission04N.Room(10,6,8));Assert.AreEqual(4,CreatureAdmission04N.Room(10,6,6));
            Assert.AreEqual(0,CreatureAdmission04N.Room(10,12,8));Assert.AreEqual(0,CreatureAdmission04N.Room(10,0,12));
        }
        [Test] public void DiagnosticProbesAreDeterministicBoundedAndDoNotMoveOrCreateAnActor()
        {
            Assert.AreEqual(Vector3.zero,CreatureAdmission04N.Probe(0));var points=new System.Collections.Generic.HashSet<Vector3>();
            for(int i=0;i<8;i++){var p=CreatureAdmission04N.Probe(i);Assert.That(p.magnitude,Is.LessThanOrEqualTo(2.501f));Assert.AreEqual(0,p.y);points.Add(p);}Assert.AreEqual(8,points.Count);
        }
        [Test] public void CantorSupportHasAFiniteReadableNoDamageCallOutsideOrdinaryAttackSelection()
        {
            var call=CrystalCantor04N.Attacks()[2];Assert.AreEqual(AttackShape.Call,call.Shape);Assert.AreEqual(0,call.Damage);
            Assert.AreEqual(0,call.Weight);Assert.That(call.Windup,Is.GreaterThanOrEqualTo(1));Assert.That(call.Active,Is.GreaterThan(.1f));
        }
    }
}
