using NUnit.Framework;
using UnityEngine;

namespace NeonSyndicate.Tests
{
    /// <summary>
    /// Children and elders of the city (FamilyRules, Appearance.Child and Elder, the age rules of LiveCrowdRules): the
    /// child's spot beside the grown-up, its catch-up speed, the share of families and elders, the child and elder
    /// looks, and which crowd body each age class may wear. Engine-free; native Unity is NOT RUN here.
    /// </summary>
    public sealed class FamilyRulesTests
    {
        [Test]
        public void TheChildWalksOnTheRightJustBehind()
        {
            Vector3 spot = FamilyRules.Spot(new Vector3(10f, 0f, 5f), Vector3.forward, Vector3.right);
            Assert.That(spot.x, Is.EqualTo(10f + FamilyRules.SideOffset).Within(1e-4f));
            Assert.That(spot.z, Is.EqualTo(5f - FamilyRules.BackOffset).Within(1e-4f));
        }

        [Test]
        public void TheChildStopsKeepsPaceAndRunsToCatchUp()
        {
            Assert.That(FamilyRules.Speed(0.2f, 1.3f, 1.3f, 4.6f), Is.EqualTo(0f), "settled beside the grown-up");
            Assert.That(FamilyRules.Speed(1f, 1.3f, 1.3f, 4.6f), Is.GreaterThan(1.3f), "a little faster than the grown-up, to close the gap");
            Assert.That(FamilyRules.Speed(1f, 0f, 1.3f, 4.6f), Is.GreaterThan(0f), "still steps to the spot when the grown-up stands");
            Assert.That(FamilyRules.Speed(6f, 1.3f, 1.3f, 4.6f), Is.EqualTo(4.6f), "runs when left behind");
            Assert.That(FamilyRules.Speed(1f, 9f, 1.3f, 4.6f), Is.LessThanOrEqualTo(4.6f), "never faster than a run");
            Assert.That(FamilyRules.Speed(float.NaN, 1f, 1.3f, 4.6f), Is.EqualTo(0f));
        }

        [Test]
        public void FamiliesAndEldersAreAMinority()
        {
            int families = 0, elders = 0;
            for (int i = 0; i < 1000; i++)
            {
                bool family;
                AgeClass age = FamilyRules.Pick(i / 1000.0, out family);
                if (family) { families++; Assert.That(age, Is.EqualTo(AgeClass.Adult), "the grown-up of a family"); }
                if (age == AgeClass.Elder) elders++;
            }
            Assert.That(families, Is.EqualTo(120));
            Assert.That(elders, Is.EqualTo(140));
        }

        [Test]
        public void ChildAndElderLooks()
        {
            System.Random r = new System.Random(7);
            for (int i = 0; i < 200; i++)
            {
                Appearance c = Appearance.Child(r);
                Assert.That(c.Age, Is.EqualTo(AgeClass.Child));
                Assert.That(c.Height, Is.InRange(0.66f, 0.78f));
                Assert.That(c.Beard || c.Glasses || c.Implant != 0 || c.Glow != 0, Is.False);
                Assert.That(LiveCrowdRules.ProportionsFit(c.Height, c.Bulk, AgeClass.Child), Is.True, "every child fits a child body");
                Assert.That(LiveCrowdRules.ProportionsFit(c.Height, c.Bulk), Is.False, "no child fits a grown-up body");
                Appearance e = Appearance.Elder(r);
                Assert.That(e.Age, Is.EqualTo(AgeClass.Elder));
                Assert.That(e.Hair, Is.EqualTo(4), "grey or white hair");
                Assert.That(LiveCrowdRules.ProportionsFit(e.Height, e.Bulk, AgeClass.Elder), Is.True);
            }
            Assert.That(new Appearance { Age = AgeClass.Child }.SameAs(new Appearance()), Is.False, "the age is part of the look");
        }

        [Test]
        public void TheDropInsServeTheirOwnAgeAndTheStudiesTheRest()
        {
            int civilians = LiveCrowdRules.RoleBit(NpcRole.Civilian) | LiveCrowdRules.RoleBit(NpcRole.Clerk);
            LiveCastRow boy = LiveCrowdRulesTests.Crowd("veyran-boy", false, LiveCrowdRules.RoleBit(NpcRole.Civilian));
            boy.CrowdAge = AgeClass.Child;
            LiveCastRow elder = LiveCrowdRulesTests.Crowd("veyran-elder-woman", true, civilians);
            elder.CrowdAge = AgeClass.Elder;
            LiveCastRow heavy = LiveCrowdRulesTests.Crowd("syndicate-heavy", false, LiveCrowdRules.RoleBit(NpcRole.Gang) | LiveCrowdRules.RoleBit(NpcRole.Enemy));
            LiveCastRow male = LiveCrowdRulesTests.Crowd("court-defender-h8r", false, LiveCrowdRules.CrowdRoles);
            LiveCastRow female = LiveCrowdRulesTests.Crowd("guild-artisan-h8r", true, LiveCrowdRules.CrowdRoles);
            System.Collections.Generic.List<LiveCastRow> rows = new System.Collections.Generic.List<LiveCastRow> { boy, elder, heavy, male, female };
            Assert.AreEqual(0, LiveCrowdRules.FindCrowd(rows, NpcRole.Civilian, false, AgeClass.Child, false, false, 0.72f, 0.9f), "a boy takes the boy");
            Assert.AreEqual(-1, LiveCrowdRules.FindCrowd(rows, NpcRole.Civilian, true, AgeClass.Child, false, false, 0.72f, 0.9f), "a girl without a girl body stays procedural");
            Assert.AreEqual(3, LiveCrowdRules.FindCrowd(rows, NpcRole.Civilian, false, AgeClass.Adult, false, false, 1f, 1f), "grown-up men keep the study");
            Assert.AreEqual(1, LiveCrowdRules.FindCrowd(rows, NpcRole.Civilian, true, AgeClass.Elder, false, false, 0.95f, 1f), "an old woman takes the elder body");
            Assert.AreEqual(4, LiveCrowdRules.FindCrowd(rows, NpcRole.Civilian, true, AgeClass.Adult, false, false, 1f, 1f), "grown-up women keep the study");
            Assert.AreEqual(3, LiveCrowdRules.FindCrowd(rows, NpcRole.Civilian, false, AgeClass.Elder, false, false, 0.95f, 1f), "an old man wears the grown-up study");
            Assert.AreEqual(2, LiveCrowdRules.FindCrowd(rows, NpcRole.Gang, false, AgeClass.Adult, false, false, 1f, 1f), "gang men take the heavy");
            Assert.AreEqual(3, LiveCrowdRules.FindCrowd(rows, NpcRole.Police, false, AgeClass.Adult, false, false, 1f, 1f), "police keep the study");
        }

        [Test]
        public void EachAgeWearsItsOwnBody()
        {
            Assert.That(LiveCrowdRules.AgeFits(AgeClass.Child, AgeClass.Child), Is.True);
            Assert.That(LiveCrowdRules.AgeFits(AgeClass.Adult, AgeClass.Child), Is.False, "a child never wears a grown-up body");
            Assert.That(LiveCrowdRules.AgeFits(AgeClass.Child, AgeClass.Adult), Is.False, "a grown-up never wears a child body");
            Assert.That(LiveCrowdRules.AgeFits(AgeClass.Elder, AgeClass.Adult), Is.False, "an elder body never draws a grown-up");
            Assert.That(LiveCrowdRules.AgeFits(AgeClass.Adult, AgeClass.Elder), Is.True, "elders wear the grown-up body until an elder body exists");
            AgeClass age;
            Assert.That(LiveCrowdRules.ParseAge("Child", out age) && age == AgeClass.Child, Is.True);
            Assert.That(LiveCrowdRules.ParseAge("", out age) && age == AgeClass.Adult, Is.True);
            Assert.That(LiveCrowdRules.ParseAge("elder", out age) && age == AgeClass.Elder, Is.True);
            Assert.That(LiveCrowdRules.ParseAge("teen", out age), Is.False);
            Assert.That(LiveCrowdRules.PeopleWord(true, AgeClass.Child), Is.EqualTo("girls"));
        }
    }
}
